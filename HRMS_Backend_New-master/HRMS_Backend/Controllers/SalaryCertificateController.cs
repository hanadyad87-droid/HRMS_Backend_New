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

        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound("الموظف غير موجود");

            var myRequests = await _context.SalaryCertificateRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return Ok(myRequests);
        }

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
            var userSubDeptId = user.Employee.AdministrativeData?.SubDepartmentId;

            var isInTargetDept = userSubDeptId == targetSubDeptId;

            // صلاحية وحدة شاملة
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

            // SuperAdmin
            if (user.UserRoles.Any(r => r.Role.RoleName == "SuperAdmin"))
            {
                return Ok(await _context.SalaryCertificateRequests
                    .Include(r => r.Employee)
                    .Where(r => r.Status == "قيد_الانتظار")
                    .ToListAsync());
            }

            // نفس الإدارة + عنده الصلاحية
            if (isInTargetDept && hasPermission)
            {
                return Ok(await _context.SalaryCertificateRequests
                    .Include(r => r.Employee)
                    .Where(r => r.Status == "قيد_الانتظار")
                    .ToListAsync());
            }

            return Forbid();
        }

        [HttpPost("decision/{id}")]
        public async Task<IActionResult> Decision(int id, bool isReady)
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
            var userSubDeptId = user.Employee.AdministrativeData?.SubDepartmentId;

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

            if (!isSuperAdmin && !(isInTargetDept && hasPermission))
                return Unauthorized("ليس لديك صلاحية اتخاذ قرار");

            var request = await _context.SalaryCertificateRequests
                .Include(r => r.Employee)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("الطلب غير موجود");

            request.Status = isReady ? "جاهزة" : "مرفوض";

            await _context.SaveChangesAsync();

            await _notifications.NotifyEmployeeAsync(
                request.Employee.Id,
                "تحديث طلب شهادة مرتب",
                $"تم تحديث حالة طلبك إلى: {request.Status}");
            return Ok($"تم تحديث حالة الطلب إلى: {request.Status}");
        }
    }
}