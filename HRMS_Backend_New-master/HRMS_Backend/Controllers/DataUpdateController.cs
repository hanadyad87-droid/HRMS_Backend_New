using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Enums;
using HRMS_Backend.Models;
using HRMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DataUpdateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public DataUpdateController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // ==================== 1. تقديم طلب تعديل بيانات (للموظف) ====================
        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromForm] CreateDataUpdateDto dto)
        {
            var userIdClaim = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = int.Parse(userIdClaim);
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (emp == null) return NotFound("الموظف غير موجود");

            var request = new DataUpdateRequest
            {
                EmployeeId = emp.Id,
                UpdateType = dto.UpdateType.ToString().Replace("_", " "),
                NewValue = dto.NewValue,
                Reason = dto.Reason,
                Status = "قيد_الانتظار",
                CreatedAt = DateTime.Now
            };

            _context.DataUpdateRequests.Add(request);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "تم إرسال طلب التعديل للإدارة المختصة بنجاح" });
        }

        // ==================== 2. طلباتي (للموظف) ====================
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound("الموظف غير موجود");

            var myRequests = await _context.DataUpdateRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.UpdateType,
                    r.NewValue,
                    r.Reason,
                    r.Status,
                    r.CreatedAt,
                    r.ClaimedAt,
                    r.CompletedAt,
                    r.VerifiedAt,
                    ClaimedByName = r.ClaimedBy != null ? r.ClaimedBy.FullName : null,
                    AssignedToName = r.AssignedTo != null ? r.AssignedTo.FullName : null,
                    CompletionNotes = r.CompletionNotes
                })
                .ToListAsync();

            return Ok(myRequests);
        }

        // ==================== 3. الطلبات المعلقة (للمدير/اللي عنده صلاحية) ====================
        [HttpGet("pending-for-my-dept")]
        public async Task<IActionResult> GetPending()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees
                .Include(e => e.AdministrativeData)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.DataUpdate);
            var targetSubDeptId = setting?.TargetSubDepartmentId;

            // تحديد SubDepartment للمستخدم (مدير أو موظف عادي)
            var userSubDeptId = await GetUserSubDepartmentIdAsync(currentEmp.Id);
            var isInTargetDept = userSubDeptId == targetSubDeptId;

            var hasPermission = await _context.UserPermissions
                .AnyAsync(p => p.UserId == userId && p.PermissionId == 18 && p.IsAllowed);

            var isSuperAdmin = User.IsInRole("SuperAdmin");

            // ✅ لو مش مسؤول/مش عنده صلاحية: نسمح له فقط برؤية الطلبات المُكلَّف بها (وليس كل الطلبات)
            var targetSubDept = targetSubDeptId.HasValue
                ? await _context.SubDepartments.AsNoTracking().FirstOrDefaultAsync(s => s.Id == targetSubDeptId.Value)
                : null;

            var isManagerOfTarget =
                (targetSubDeptId.HasValue && await _context.SubDepartments.AsNoTracking()
                    .AnyAsync(s => s.Id == targetSubDeptId.Value && s.ManagerEmployeeId == currentEmp.Id))
                || (targetSubDeptId.HasValue && await _context.Sections.AsNoTracking()
                    .AnyAsync(s => s.SubDepartmentId == targetSubDeptId.Value && s.ManagerEmployeeId == currentEmp.Id))
                || (targetSubDept != null && await _context.Departments.AsNoTracking()
                    .AnyAsync(d => d.Id == targetSubDept.DepartmentId && d.ManagerEmployeeId == currentEmp.Id));

            // ملاحظة: مجرد كون الموظف "داخل القسم" لا يعطيه حق الإدارة.
            _ = isInTargetDept;

            var canManage = isSuperAdmin || hasPermission || isManagerOfTarget;

            var baseQuery = _context.DataUpdateRequests
                .Include(r => r.Employee)
                .Include(r => r.ClaimedBy)
                .Include(r => r.AssignedTo)
                .Where(r =>
                    r.Status == "قيد_الانتظار" ||
                    r.Status == "قيد_التنفيذ" ||
                    r.Status == "في_انتظار_المصادقة" ||
                    r.Status == "تمت_العملية")
                .AsQueryable();

            if (!canManage)
            {
                // الموظف يشوف فقط ما كُلّف به، وفي حالات التنفيذ/المصادقة (مش كل القائمة)
                baseQuery = baseQuery.Where(r =>
                    r.AssignedToEmployeeId == currentEmp.Id &&
                    (r.Status == "قيد_التنفيذ" || r.Status == "في_انتظار_المصادقة"));
            }

            var requests = await baseQuery
                .OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.UpdateType,
                    r.NewValue,
                    r.Reason,
                    r.Status,
                    r.CreatedAt,
                    r.ClaimedAt,
                    r.CompletedAt,

                    Employee = new
                    {
                        fullName = r.Employee.FullName,
                        id = r.Employee.Id
                    },

                    ClaimedByName = r.ClaimedBy != null ? r.ClaimedBy.FullName : null,
                    AssignedToName = r.AssignedTo != null ? r.AssignedTo.FullName : null,

                    IsClaimed = r.ClaimedByEmployeeId != null,
                    IsClaimedByMe = r.ClaimedByEmployeeId == currentEmp.Id,
                    CanClaim = canManage && r.ClaimedByEmployeeId == null && r.Status == "قيد_الانتظار"
                })
                .ToListAsync();

            return Ok(requests);
        }

        // ==================== 4. استلام الطلب (First Come First Serve) ====================
        [HttpPost("claim/{id}")]
        public async Task<IActionResult> ClaimRequest(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees
                .Include(e => e.AdministrativeData)
                .FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.DataUpdate);
            var targetSubDeptId = setting?.TargetSubDepartmentId;

            // تحديد SubDepartment للمستخدم (مدير أو موظف عادي)
            var userSubDeptId = await GetUserSubDepartmentIdAsync(currentEmp.Id);
            var isInTargetDept = userSubDeptId == targetSubDeptId;

            var hasPermission = await _context.UserPermissions
                .AnyAsync(p => p.UserId == userId && p.PermissionId == 18 && p.IsAllowed);
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            // يكفي ان المدير يكون في القسم المستهدف OR عنده صلاحية الإدارة
            if (!isSuperAdmin && !isInTargetDept && !hasPermission)
                return Forbid();

            var request = await _context.DataUpdateRequests.FindAsync(id);
            if (request == null) return NotFound("الطلب غير موجود");

            if (request.ClaimedByEmployeeId != null)
                return BadRequest("الطلب مستلم بالفعل");

            if (request.Status != "قيد_الانتظار")
                return BadRequest("الطلب ليس في حالة الانتظار");

            request.ClaimedByEmployeeId = currentEmp.Id;
            request.ClaimedAt = DateTime.Now;
            request.Status = "قيد_التنفيذ";

            await _context.SaveChangesAsync();

            await _notifications.NotifyEmployeeWithTypeAsync(
                request.EmployeeId,
                "تحديث طلب تعديل البيانات",
                "تم استلام طلبك وهو قيد التنفيذ الآن",
                "claimed",
                request.Id, "data_update");

            return Ok(new { Message = "تم استلام الطلب بنجاح" });
        }

        // ==================== 5. تكليف موظف بالتنفيذ ====================
        [HttpPost("assign/{id}")]
        public async Task<IActionResult> AssignToEmployee(int id, int employeeId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var request = await _context.DataUpdateRequests.FindAsync(id);
            if (request == null) return NotFound("الطلب غير موجود");

            if (request.ClaimedByEmployeeId != currentEmp.Id)
                return StatusCode(403, new { message = "فقط من استلم الطلب يمكنه توزيع المهمة" });

            // تحديد SubDepartment للمُكَلِّف (من جداول الإدارة أو AdministrativeData)
            var claimantDeptId = await GetUserSubDepartmentIdAsync(currentEmp.Id);

            var assignedEmp = await _context.Employees
                .Include(e => e.AdministrativeData)
                .FirstOrDefaultAsync(e => e.Id == employeeId);
            if (assignedEmp == null) return NotFound("الموظف غير موجود");

            // تحديد SubDepartment للموظف المُكَلَّف
            var assignedDeptId = await GetUserSubDepartmentIdAsync(assignedEmp.Id);

            // التحقق: المُكَلَّف لازم يكون في نفس القسم
            if (assignedDeptId != claimantDeptId)
                return BadRequest("يمكنك فقط تكليف موظفين في نفس إدارتك/قسمك");

            request.AssignedToEmployeeId = employeeId;
            await _context.SaveChangesAsync();

            await _notifications.NotifyEmployeeWithTypeAsync(
                employeeId,
                "تكليف جديد",
                "تم تكليفك بتعديل بيانات موظف",
                "assignment",
                request.Id, "data_update");

            return Ok(new { Message = "تم تكليف " + assignedEmp.FullName + " بالتنفيذ" });
        }

        // ==================== 6. إعلان تمام التنفيذ ====================
        [HttpPost("complete/{id}")]
        public async Task<IActionResult> MarkAsCompleted(int id, [FromBody] CompleteRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var request = await _context.DataUpdateRequests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.AdministrativeData)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("الطلب غير موجود");

            if (request.ClaimedByEmployeeId != currentEmp.Id &&
                request.AssignedToEmployeeId != currentEmp.Id)
                return StatusCode(403, new { message = "ليس لديك صلاحية" });

            if (request.Status != "قيد_التنفيذ")
                return BadRequest("الطلب ليس قيد التنفيذ");

            // تنفيذ التعديل على البيانات
            var emp = request.Employee;
            var enumKey = request.UpdateType.Replace(" ", "_");

            if (Enum.TryParse(enumKey, out DataUpdateField field))
            {
                switch (field)
                {
                    case DataUpdateField.الاسم_الكامل:
                        emp.FullName = request.NewValue;
                        break;
                    case DataUpdateField.الرقم_الوطني:
                        emp.NationalId = request.NewValue;
                        break;
                    case DataUpdateField.رقم_الهاتف_الأول:
                        emp.Phone1 = request.NewValue;
                        break;
                    case DataUpdateField.رقم_الهاتف_الثاني:
                        emp.Phone2 = request.NewValue;
                        break;
                    case DataUpdateField.الإدارة:
                        if (int.TryParse(request.NewValue, out int deptId) && emp.AdministrativeData != null)
                            emp.AdministrativeData.SubDepartmentId = deptId;
                        break;
                    case DataUpdateField.المسمى_الوظيفي:
                        if (int.TryParse(request.NewValue, out int jobTitleId) && emp.AdministrativeData != null)
                            emp.AdministrativeData.JobTitleId = jobTitleId;
                        break;
                }
            }

            request.CompletedAt = DateTime.Now;
            request.CompletionNotes = dto?.Notes;
            request.Status = "في_انتظار_المصادقة";

            await _context.SaveChangesAsync();

            await _notifications.NotifyEmployeeWithTypeAsync(
                request.EmployeeId,
                "تم تنفيذ طلب تعديل البيانات",
                "تم تعديل بياناتك. يرجى التحقق والمصادقة.",
                "verification",
                request.Id, "data_update");

            return Ok(new { Message = "تم إعلان التنفيذ، في انتظار مصادقة صاحب الطلب" });
        }

        // ==================== 7. مصادقة صاحب الطلب ====================
        [HttpPost("verify/{id}")]
        public async Task<IActionResult> VerifyByRequester(int id, bool isVerified)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var request = await _context.DataUpdateRequests
                .Include(r => r.ClaimedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("الطلب غير موجود");

            if (request.EmployeeId != currentEmp.Id)
                return StatusCode(403, new { message = "فقط صاحب الطلب يمكنه المصادقة" });

            if (request.Status != "في_انتظار_المصادقة")
                return BadRequest("الطلب ليس في انتظار المصادقة");

            if (isVerified)
            {
                request.VerifiedAt = DateTime.Now;
                request.Status = "تمت_العملية";
                await _context.SaveChangesAsync();

                if (request.ClaimedByEmployeeId.HasValue)
                {
                    await _notifications.NotifyEmployeeWithTypeAsync(
                        request.ClaimedByEmployeeId.Value,
                        "تمت المصادقة",
                        "قام " + currentEmp.FullName + " بمصادقة تعديل البيانات",
                        "verified",
                        request.Id, "data_update");
                }

                return Ok(new { Message = "تمت المصادقة بنجاح" });
            }
            else
            {
                // لو ما صدق - نرجع البيانات للقديمة؟ أو نرجع للتنفيذ
                request.Status = "قيد_التنفيذ";
                request.CompletedAt = null;
                await _context.SaveChangesAsync();

                if (request.ClaimedByEmployeeId.HasValue)
                {
                    await _notifications.NotifyEmployeeWithTypeAsync(
                        request.ClaimedByEmployeeId.Value,
                        "تم رفض المصادقة",
                        "لم يتم مصادقة تعديل البيانات من " + currentEmp.FullName,
                        "rejected",
                        request.Id, "data_update");
                }

                return Ok(new { Message = "تم إرجاع الطلب للتنفيذ" });
            }
        }

        // ==================== 8. رفض الطلب ====================
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectRequest(int id, [FromBody] RejectRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var request = await _context.DataUpdateRequests.FindAsync(id);
            if (request == null) return NotFound("الطلب غير موجود");

            if (request.ClaimedByEmployeeId != currentEmp.Id)
                return Forbid();

            if (request.Status != "قيد_الانتظار" && request.Status != "قيد_التنفيذ")
                return BadRequest("لا يمكن رفض الطلب في هذه الحالة");

            request.Status = "مرفوض";
            await _context.SaveChangesAsync();

            await _notifications.NotifyEmployeeWithTypeAsync(
                request.EmployeeId,
                "تم رفض طلب تعديل البيانات",
                "سبب الرفض: " + (dto?.Reason ?? "غير محدد"),
                "rejected",
                request.Id, "data_update");

            return Ok(new { Message = "تم رفض الطلب" });
        }

        // ==================== 9. المهام المُكَلَّفة لي (للموظف المنفذ) ====================
        [HttpGet("my-assignments")]
        public async Task<IActionResult> GetMyAssignments()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var assignments = await _context.DataUpdateRequests
                .Include(r => r.Employee)
                .Where(r => r.AssignedToEmployeeId == currentEmp.Id && 
                           (r.Status == "قيد_التنفيذ" || r.Status == "في_انتظار_المصادقة"))
                .OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.UpdateType,
                    r.NewValue,
                    r.Reason,
                    r.Status,
                    r.CreatedAt,
                    r.ClaimedAt,
                    r.CompletedAt,
                    r.CompletionNotes,
                    RequesterName = r.Employee.FullName,
                    CanComplete = r.Status == "قيد_التنفيذ"
                })
                .ToListAsync();

            return Ok(assignments);
        }

        // ==================== دالة مساعدة: تحديد SubDepartment للمستخدم ====================
        private async Task<int?> GetUserSubDepartmentIdAsync(int employeeId)
        {
            // 1. التحقق إذا كان مدير إدارة فرعية
            var managedSubDept = await _context.SubDepartments
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ManagerEmployeeId == employeeId);
            if (managedSubDept != null)
                return managedSubDept.Id;

            // 2. التحقق إذا كان مدير قسم
            var managedSection = await _context.Sections
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ManagerEmployeeId == employeeId);
            if (managedSection != null)
                return managedSection.SubDepartmentId;

            // 3. التحقق إذا كان مدير إدارة عامة (يرجع أول إدارة فرعية تحتها)
            var managedDept = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.ManagerEmployeeId == employeeId);
            if (managedDept != null)
            {
                var firstSubDept = await _context.SubDepartments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.DepartmentId == managedDept.Id);
                return firstSubDept?.Id;
            }

            // 4. موظف عادي - من AdministrativeData
            var adminData = await _context.EmployeeAdministrativeDatas
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId);
            return adminData?.SubDepartmentId;
        }
    }
}