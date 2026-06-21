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
    public class MaintenanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly INotificationService _notifications;

        public MaintenanceController(ApplicationDbContext context, IWebHostEnvironment environment, INotificationService notifications)
        {
            _context = context;
            _environment = environment;
            _notifications = notifications;
        }

        // ==================== 1. تقديم طلب (للموظف) ====================
        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromForm] CreateMaintenanceDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (emp == null) return NotFound("الموظف غير موجود");

            string? imagePath = null;
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/maintenance");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(dto.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.ImageFile.CopyToAsync(stream);

                imagePath = "/uploads/maintenance/" + fileName;
            }

            var request = new MaintenanceRequest
            {
                EmployeeId = emp.Id,
                EquipmentName = dto.EquipmentName,
                ProblemDescription = dto.ProblemDescription,
                ImagePath = imagePath,
                Status = "قيد_الانتظار"
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "تم إرسال طلب الصيانة بنجاح", Photo = imagePath });
        }

        // ==================== 2. طلباتي (للموظف) ====================
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return NotFound("الموظف غير موجود");

            var myRequests = await _context.MaintenanceRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.EquipmentName,
                    r.ProblemDescription,
                    r.ImagePath,
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
                .FirstOrDefaultAsync(s => s.RequestType == RequestType.Maintenance);

            if (setting == null)
                return BadRequest("ما فيش إعدادات لطلب الصيانة");

            var targetSubDeptId = setting.TargetSubDepartmentId;

            // تحديد SubDepartment للمستخدم (مدير أو موظف عادي)
            var userSubDeptId = await GetUserSubDepartmentIdAsync(user.Employee.Id);

            var isInTargetDept = userSubDeptId == targetSubDeptId;

            var hasManagePermission =
                user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Any(rp => rp.Permission.PermissionName == "ManageMaintenance")
                ||
                await _context.UserPermissions
                    .Include(up => up.Permission)
                    .AnyAsync(up => up.UserId == userId &&
                                    up.Permission.PermissionName == "ManageMaintenance" &&
                                    up.IsAllowed);

            var isSuperAdmin = user.UserRoles.Any(r => r.Role.RoleName == "SuperAdmin");

            // ✅ لو مش مسؤول/مش عنده صلاحية: نسمح له فقط برؤية الطلبات المُكلَّف بها (وليس كل الطلبات)
            var targetSubDept = await _context.SubDepartments.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == targetSubDeptId);

            var isManagerOfTarget =
                await _context.SubDepartments.AsNoTracking()
                    .AnyAsync(s => s.Id == targetSubDeptId && s.ManagerEmployeeId == user.Employee.Id)
                || await _context.Sections.AsNoTracking()
                    .AnyAsync(s => s.SubDepartmentId == targetSubDeptId && s.ManagerEmployeeId == user.Employee.Id)
                || (targetSubDept != null && await _context.Departments.AsNoTracking()
                    .AnyAsync(d => d.Id == targetSubDept.DepartmentId && d.ManagerEmployeeId == user.Employee.Id));

            // ملاحظة: مجرد كون الموظف "داخل القسم" لا يعطيه حق الإدارة.
            _ = isInTargetDept;

            var canManage = isSuperAdmin || hasManagePermission || isManagerOfTarget;

            // جلب كل الطلبات المعلقة (قيد_الانتظار) أو اللي تحت التنفيذ
            var baseQuery = _context.MaintenanceRequests
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
                baseQuery = baseQuery.Where(r =>
                    r.AssignedToEmployeeId == user.Employee.Id &&
                    (r.Status == "قيد_التنفيذ" || r.Status == "في_انتظار_المصادقة"));
            }

            var requests = await baseQuery
                .OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.EquipmentName,
                    r.ProblemDescription,
                    r.ImagePath,
                    r.Status,
                    r.CreatedAt,
                    r.ClaimedAt,
                    r.CompletedAt,
                    r.ClaimedByEmployeeId,
                    r.AssignedToEmployeeId,
                    employeeName = r.Employee.FullName,
                    ClaimedByName = r.ClaimedBy != null ? r.ClaimedBy.FullName : null,
                    AssignedToName = r.AssignedTo != null ? r.AssignedTo.FullName : null,
                    IsClaimed = r.ClaimedByEmployeeId != null,
                    IsClaimedByMe = r.ClaimedByEmployeeId == user.Employee.Id,
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

            // التحقق من الصلاحية
            var setting = await _context.RequestSettings
                .FirstOrDefaultAsync(s => s.RequestType == RequestType.Maintenance);
            if (setting == null) return BadRequest("ما فيش إعدادات");

            var targetSubDeptId = setting.TargetSubDepartmentId;

            // تحديد SubDepartment للمستخدم (مدير أو موظف عادي)
            var userSubDeptId = await GetUserSubDepartmentIdAsync(user.Employee.Id);
            var isInTargetDept = userSubDeptId == targetSubDeptId;

            var hasManagePermission =
                user.UserRoles.SelectMany(ur => ur.Role.RolePermissions)
                    .Any(rp => rp.Permission.PermissionName == "ManageMaintenance")
                || await _context.UserPermissions
                    .AnyAsync(up => up.UserId == userId &&
                                   up.Permission.PermissionName == "ManageMaintenance" &&
                                   up.IsAllowed);

            var isSuperAdmin = user.UserRoles.Any(r => r.Role.RoleName == "SuperAdmin");

            // يكفي ان المدير يكون في القسم المستهدف OR عنده صلاحية الإدارة
            if (!isSuperAdmin && !isInTargetDept && !hasManagePermission)
                return Forbid();

            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null) return NotFound("الطلب غير موجود");

            // التحقق إن الطلب لسه ما اشتغل عليه حد
            if (request.ClaimedByEmployeeId != null)
                return BadRequest("الطلب مستلم بالفعل من " + request.ClaimedBy?.FullName);

            if (request.Status != "قيد_الانتظار")
                return BadRequest("الطلب ليس في حالة الانتظار");

            // استلام الطلب
            request.ClaimedByEmployeeId = user.Employee.Id;
            request.ClaimedAt = DateTime.Now;
            request.Status = "قيد_التنفيذ";

            await _context.SaveChangesAsync();

            // إشعار لصاحب الطلب
            await _notifications.NotifyEmployeeWithTypeAsync(
                request.EmployeeId,
                "تحديث طلب الصيانة",
                "تم استلام طلبك من " + user.Employee.FullName + " وهو قيد التنفيذ الآن",
                "claimed",
                request.Id, "maintenance");

            return Ok(new { Message = "تم استلام الطلب بنجاح" });
        }

        // ==================== 5. تكليف موظف بالتنفيذ (من اللي استلم الطلب) ====================
        [HttpPost("assign/{id}")]
        public async Task<IActionResult> AssignToEmployee(int id, int employeeId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue("UserId"));
                var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (currentEmp == null) return Unauthorized();

                Console.WriteLine($"Assign: UserId={userId}, CurrentEmp={currentEmp.Id}, RequestId={id}, TargetEmployee={employeeId}");

                var request = await _context.MaintenanceRequests.FindAsync(id);
                if (request == null) return NotFound("الطلب غير موجود");

                Console.WriteLine($"Request found: ClaimedBy={request.ClaimedByEmployeeId}, Status={request.Status}");

                // فقط اللي استلم الطلب يقدر يكلف حد
                if (request.ClaimedByEmployeeId != currentEmp.Id)
                    return StatusCode(403, new { message = "فقط من استلم الطلب يمكنه توزيع المهمة" });

                // تحديد SubDepartment للمُكَلِّف (من جداول الإدارة أو AdministrativeData)
                var claimantDeptId = await GetUserSubDepartmentIdAsync(currentEmp.Id);

                // التحقق من الموظف المُكَلَّف
                var assignedEmp = await _context.Employees
                    .Include(e => e.AdministrativeData)
                    .FirstOrDefaultAsync(e => e.Id == employeeId);
                if (assignedEmp == null) return NotFound("الموظف غير موجود");

                // تحديد SubDepartment للموظف المُكَلَّف
                var assignedDeptId = await GetUserSubDepartmentIdAsync(assignedEmp.Id);

                // التحقق: المُكَلَّف لازم يكون في نفس القسم
                if (assignedDeptId != claimantDeptId)
                    return BadRequest("يمكنك فقط تكليف موظفين في نفس إدارتك/قسمك");

                Console.WriteLine($"Assigning to: {assignedEmp.FullName} (Dept: {assignedDeptId})");

                request.AssignedToEmployeeId = employeeId;
                await _context.SaveChangesAsync();

                // إشعار للموظف المُكَلَّف
                try
                {
                    await _notifications.NotifyEmployeeWithTypeAsync(
                        employeeId,
                        "تكليف جديد",
                        "تم تكليفك بصيانة جهاز: " + request.EquipmentName,
                        "assignment",
                        request.Id, "maintenance");
                }
                catch (Exception notifyEx)
                {
                    Console.WriteLine($"Notification error (non-critical): {notifyEx.Message}");
                }

                return Ok(new { Message = "تم تكليف " + assignedEmp.FullName + " بالتنفيذ" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in AssignToEmployee: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        // ==================== 6. إعلان تمام التنفيذ (من المنفذ) ====================
        [HttpPost("complete/{id}")]
        public async Task<IActionResult> MarkAsCompleted(int id, [FromBody] CompleteRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var request = await _context.MaintenanceRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("الطلب غير موجود");

            // فقط اللي استلم الطلب أو المُكَلَّف يقدر يعلن التنفيذ
            if (request.ClaimedByEmployeeId != currentEmp.Id &&
                request.AssignedToEmployeeId != currentEmp.Id)
                return StatusCode(403, new { message = "ليس لديك صلاحية" });

            if (request.Status != "قيد_التنفيذ")
                return BadRequest("الطلب ليس قيد التنفيذ");

            request.CompletedAt = DateTime.Now;
            request.CompletionNotes = dto?.Notes;
            request.Status = "في_انتظار_المصادقة";

            await _context.SaveChangesAsync();

            // إشعار لصاحب الطلب الأصلي
            await _notifications.NotifyEmployeeWithTypeAsync(
                request.EmployeeId,
                "تم تنفيذ طلب الصيانة",
                "تم صيانة جهاز " + request.EquipmentName + ". يرجى التحقق والمصادقة.",
                "verification",
                request.Id, "maintenance");

            return Ok(new { Message = "تم إعلان التنفيذ، في انتظار مصادقة صاحب الطلب" });
        }

        // ==================== 7. مصادقة صاحب الطلب (التحقق النهائي) ====================
        [HttpPost("verify/{id}")]
        public async Task<IActionResult> VerifyByRequester(int id, bool isVerified)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var request = await _context.MaintenanceRequests
                .Include(r => r.ClaimedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("الطلب غير موجود");

            // فقط صاحب الطلب يقدر يمصادق
            if (request.EmployeeId != currentEmp.Id)
                return StatusCode(403, new { message = "فقط صاحب الطلب يمكنه المصادقة" });

            if (request.Status != "في_انتظار_المصادقة")
                return BadRequest("الطلب ليس في انتظار المصادقة");

            if (isVerified)
            {
                request.VerifiedAt = DateTime.Now;
                request.Status = "تمت_العملية";
                await _context.SaveChangesAsync();

                // إشعار للمنفذ
                if (request.ClaimedByEmployeeId.HasValue)
                {
                    await _notifications.NotifyEmployeeWithTypeAsync(
                        request.ClaimedByEmployeeId.Value,
                        "تمت المصادقة",
                        "قام " + currentEmp.FullName + " بمصادقة طلب الصيانة الخاص به",
                        "verified",
                        request.Id, "maintenance");
                }

                return Ok(new { Message = "تمت المصادقة بنجاح" });
            }
            else
            {
                // لو ما صدق - يرجع للتنفيذ
                request.Status = "قيد_التنفيذ";
                request.CompletedAt = null;
                await _context.SaveChangesAsync();

                // إشعار للمنفذ
                if (request.ClaimedByEmployeeId.HasValue)
                {
                    await _notifications.NotifyEmployeeWithTypeAsync(
                        request.ClaimedByEmployeeId.Value,
                        "تم رفض المصادقة",
                        "لم يتم مصادقة طلب الصيانة من " + currentEmp.FullName + ". يرجى مراجعة الطلب.",
                        "rejected",
                        request.Id, "maintenance");
                }

                return Ok(new { Message = "تم إرجاع الطلب للتنفيذ" });
            }
        }

        // ==================== 8. رفض الطلب (من اللي استلم) ====================
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectRequest(int id, [FromBody] RejectRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (currentEmp == null) return Unauthorized();

            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null) return NotFound("الطلب غير موجود");

            // فقط اللي استلم الطلب يرفض
            if (request.ClaimedByEmployeeId != currentEmp.Id)
                return Forbid();

            if (request.Status != "قيد_الانتظار" && request.Status != "قيد_التنفيذ")
                return BadRequest("لا يمكن رفض الطلب في هذه الحالة");

            request.Status = "مرفوض";
            await _context.SaveChangesAsync();

            // إشعار لصاحب الطلب
            await _notifications.NotifyEmployeeWithTypeAsync(
                request.EmployeeId,
                "تم رفض طلب الصيانة",
                "سبب الرفض: " + (dto?.Reason ?? "غير محدد"),
                "rejected",
                request.Id, "maintenance");

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

                // Debug: اطبع الـ EmployeeId
                Console.WriteLine($"Current EmployeeId: {currentEmp.Id}");

                var assignments = await _context.MaintenanceRequests
                    .AsNoTracking()
                    .Include(r => r.Employee)
                    .Where(r => r.AssignedToEmployeeId == currentEmp.Id && 
                               (r.Status == "قيد_التنفيذ" || r.Status == "في_انتظار_المصادقة"))
                    .OrderByDescending(r => r.Id)
                    .Select(r => new
                    {
                        r.Id,
                        r.EquipmentName,
                        r.ProblemDescription,
                        r.ImagePath,
                        r.Status,
                        r.CreatedAt,
                        r.ClaimedAt,
                        r.CompletedAt,
                        r.CompletionNotes,
                        RequesterName = r.Employee != null ? r.Employee.FullName : "غير معروف",
                        CanComplete = r.Status == "قيد_التنفيذ"
                    })
                    .ToListAsync();

                Console.WriteLine($"Found {assignments.Count} assignments");
                return Ok(assignments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetMyAssignments: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
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