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
    public class SalaryCertificateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public SalaryCertificateController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // ==================== 1. تقديم طلب (للموظف) ====================
        [HttpPost("submit")]
        public async Task<IActionResult> Submit(CreateSalaryCertificateDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (emp == null) return NotFound("الموظف غير موجود");

            var request = new SalaryCertificateRequest
            {
                EmployeeId = emp.Id,
                Purpose = dto.Purpose,
                Status = "قيد_الانتظار"
            };

            _context.SalaryCertificateRequests.Add(request);
            await _context.SaveChangesAsync();
            return Ok("تم إرسال طلب شهادة المرتب بنجاح");
        }

        // ==================== 2. طلباتي (للموظف) ====================
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound("الموظف غير موجود");

            var myRequests = await _context.SalaryCertificateRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Purpose,
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
            var user = await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.AdministrativeData)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Employee == null)
                return Unauthorized();

            var setting = await _context.RequestSettings
                .FirstOrDefaultAsync(s => s.RequestType == RequestType.SalaryCertificate);

            if (setting == null)
                return BadRequest("ما فيش إعدادات لطلب شهادة المرتب");

            var targetSubDeptId = setting.TargetSubDepartmentId;

            // تحديد SubDepartment للمستخدم (مدير أو موظف عادي)
            var userSubDeptId = await GetUserSubDepartmentIdAsync(user.Employee.Id);
            var isInTargetDept = userSubDeptId == targetSubDeptId;

            var hasPermission =
                user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Any(rp => rp.Permission.PermissionName == "ManageSalaryCertificates")
                ||
                await _context.UserPermissions
                    .Include(up => up.Permission)
                    .AnyAsync(up =>
                        up.UserId == userId &&
                        up.Permission.PermissionName == "ManageSalaryCertificates" &&
                        up.IsAllowed);

            var isSuperAdmin = user.UserRoles.Any(r => r.Role.RoleName == "SuperAdmin");

            // يكفي ان المدير يكون في القسم المستهدف OR عنده صلاحية الإدارة
            if (!isSuperAdmin && !isInTargetDept && !hasPermission)
                return Forbid();

            var requests = await _context.SalaryCertificateRequests
                .Include(r => r.Employee)
                .Include(r => r.ClaimedBy)
                .Include(r => r.AssignedTo)
                .Where(r => r.Status == "قيد_الانتظار" || r.Status == "قيد_التنفيذ" || r.Status == "في_انتظار_المصادقة")
                .OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Purpose,
                    r.Status,
                    r.CreatedAt,
                    r.ClaimedAt,
                    r.CompletedAt,
                    r.ClaimedByEmployeeId,
                    r.AssignedToEmployeeId,
                    RequesterName = r.Employee.FullName,
                    ClaimedByName = r.ClaimedBy != null ? r.ClaimedBy.FullName : null,
                    AssignedToName = r.AssignedTo != null ? r.AssignedTo.FullName : null,
                    IsClaimed = r.ClaimedByEmployeeId != null,
                    IsClaimedByMe = r.ClaimedByEmployeeId == user.Employee.Id,
                    CanClaim = r.ClaimedByEmployeeId == null && r.Status == "قيد_الانتظار"
                })
                .ToListAsync();

            return Ok(requests);
        }

        // ==================== 4. استلام الطلب (First Come First Serve) ====================
        [HttpPost("claim/{id}")]
        public async Task<IActionResult> ClaimRequest(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var user = await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.AdministrativeData)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Employee == null)
                return Unauthorized();

            var setting = await _context.RequestSettings
                .FirstOrDefaultAsync(s => s.RequestType == RequestType.SalaryCertificate);
            if (setting == null) return BadRequest("ما فيش إعدادات");

            var targetSubDeptId = setting.TargetSubDepartmentId;

            // تحديد SubDepartment للمستخدم (مدير أو موظف عادي)
            var userSubDeptId = await GetUserSubDepartmentIdAsync(user.Employee.Id);
            var isInTargetDept = userSubDeptId == targetSubDeptId;

            var hasPermission =
                user.UserRoles.SelectMany(ur => ur.Role.RolePermissions)
                    .Any(rp => rp.Permission.PermissionName == "ManageSalaryCertificates")
                || await _context.UserPermissions
                    .AnyAsync(up => up.UserId == userId &&
                                   up.Permission.PermissionName == "ManageSalaryCertificates" &&
                                   up.IsAllowed);

            var isSuperAdmin = user.UserRoles.Any(r => r.Role.RoleName == "SuperAdmin");

            // يكفي ان المدير يكون في القسم المستهدف OR عنده صلاحية الإدارة
            if (!isSuperAdmin && !isInTargetDept && !hasPermission)
                return Forbid();

            var request = await _context.SalaryCertificateRequests.FindAsync(id);
            if (request == null) return NotFound("الطلب غير موجود");

            if (request.ClaimedByEmployeeId != null)
                return BadRequest("الطلب مستلم بالفعل");

            if (request.Status != "قيد_الانتظار")
                return BadRequest("الطلب ليس في حالة الانتظار");

            request.ClaimedByEmployeeId = user.Employee.Id;
            request.ClaimedAt = DateTime.Now;
            request.Status = "قيد_التنفيذ";

            await _context.SaveChangesAsync();

            await _notifications.NotifyEmployeeWithTypeAsync(
                request.EmployeeId,
                "تحديث طلب شهادة المرتب",
                "تم استلام طلبك وهو قيد التنفيذ الآن",
                "claimed",
                request.Id);

            return Ok(new { Message = "تم استلام الطلب بنجاح" });
        }

        // ==================== 5. تكليف موظف بالتنفيذ ====================
        [HttpPost("assign/{id}")]
        public async Task<IActionResult> AssignToEmployee(int id, int employeeId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue("UserId"));
                var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (currentEmp == null) return Unauthorized();

                var request = await _context.SalaryCertificateRequests.FindAsync(id);
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

                try
                {
                    await _notifications.NotifyEmployeeWithTypeAsync(
                        employeeId,
                        "تكليف جديد",
                        "تم تكليفك بإعداد شهادة المرتب: " + request.Purpose,
                        "assignment",
                        request.Id);
                }
                catch (Exception notifyEx)
                {
                    Console.WriteLine($"Notification error: {notifyEx.Message}");
                }

                return Ok(new { Message = "تم تكليف " + assignedEmp.FullName + " بالتنفيذ" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in AssignToEmployee: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==================== 6. إعلان تمام التنفيذ ====================
        [HttpPost("complete/{id}")]
        public async Task<IActionResult> MarkAsCompleted(int id, [FromBody] CompleteRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var request = await _context.SalaryCertificateRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("الطلب غير موجود");

            if (request.ClaimedByEmployeeId != currentEmp.Id &&
                request.AssignedToEmployeeId != currentEmp.Id)
                return StatusCode(403, new { message = "ليس لديك صلاحية" });

            if (request.Status != "قيد_التنفيذ")
                return BadRequest("الطلب ليس قيد التنفيذ");

            request.CompletedAt = DateTime.Now;
            request.CompletionNotes = dto?.Notes;
            request.Status = "في_انتظار_المصادقة";

            await _context.SaveChangesAsync();

            await _notifications.NotifyEmployeeWithTypeAsync(
                request.EmployeeId,
                "تم إعداد شهادة المرتب",
                "تم إعداد شهادة مرتبك. يرجى التحقق والمصادقة.",
                "verification",
                request.Id);

            return Ok(new { Message = "تم إعلان التنفيذ، في انتظار مصادقة صاحب الطلب" });
        }

        // ==================== 7. مصادقة صاحب الطلب ====================
        [HttpPost("verify/{id}")]
        public async Task<IActionResult> VerifyByRequester(int id, bool isVerified)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var request = await _context.SalaryCertificateRequests
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
                        "قام " + currentEmp.FullName + " بمصادقة طلب شهادة المرتب",
                        "verified",
                        request.Id);
                }

                return Ok(new { Message = "تمت المصادقة بنجاح" });
            }
            else
            {
                request.Status = "قيد_التنفيذ";
                request.CompletedAt = null;
                await _context.SaveChangesAsync();

                if (request.ClaimedByEmployeeId.HasValue)
                {
                    await _notifications.NotifyEmployeeWithTypeAsync(
                        request.ClaimedByEmployeeId.Value,
                        "تم رفض المصادقة",
                        "لم يتم مصادقة طلب شهادة المرتب من " + currentEmp.FullName,
                        "rejected",
                        request.Id);
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

            var request = await _context.SalaryCertificateRequests.FindAsync(id);
            if (request == null) return NotFound("الطلب غير موجود");

            if (request.ClaimedByEmployeeId != currentEmp.Id)
                return Forbid();

            if (request.Status != "قيد_الانتظار" && request.Status != "قيد_التنفيذ")
                return BadRequest("لا يمكن رفض الطلب في هذه الحالة");

            request.Status = "مرفوض";
            await _context.SaveChangesAsync();

            await _notifications.NotifyEmployeeWithTypeAsync(
                request.EmployeeId,
                "تم رفض طلب شهادة المرتب",
                "سبب الرفض: " + (dto?.Reason ?? "غير محدد"),
                "rejected",
                request.Id);

            return Ok(new { Message = "تم رفض الطلب" });
        }

        // ==================== 9. المهام المُكَلَّفة لي (للموظف المنفذ) ====================
        [HttpGet("my-assignments")]
        public async Task<IActionResult> GetMyAssignments()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue("UserId"));
                var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (currentEmp == null) return Unauthorized();

                var assignments = await _context.SalaryCertificateRequests
                    .AsNoTracking()
                    .Include(r => r.Employee)
                    .Where(r => r.AssignedToEmployeeId == currentEmp.Id && 
                               (r.Status == "قيد_التنفيذ" || r.Status == "في_انتظار_المصادقة"))
                    .OrderByDescending(r => r.Id)
                    .Select(r => new
                    {
                        r.Id,
                        r.Purpose,
                        r.Status,
                        r.CreatedAt,
                        r.ClaimedAt,
                        r.CompletedAt,
                        r.CompletionNotes,
                        RequesterName = r.Employee != null ? r.Employee.FullName : "غير معروف",
                        CanComplete = r.Status == "قيد_التنفيذ"
                    })
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetMyAssignments: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
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