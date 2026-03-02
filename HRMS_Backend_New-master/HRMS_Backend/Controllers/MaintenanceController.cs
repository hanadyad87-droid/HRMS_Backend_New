using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Enums;
using HRMS_Backend.Models;
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

        public MaintenanceController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

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

        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return NotFound("الموظف غير موجود");

            var myRequests = await _context.MaintenanceRequests
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
                .FirstOrDefaultAsync(s => s.RequestType == RequestType.Maintenance);

            if (setting == null)
                return BadRequest("ما فيش إعدادات لطلب الصيانة");

            var targetSubDeptId = setting.TargetSubDepartmentId;
            var userSubDeptId = user.Employee.AdministrativeData?.SubDepartmentId;

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

            if (isSuperAdmin)
            {
                return Ok(await _context.MaintenanceRequests
                    .Include(r => r.Employee)
                    .ToListAsync());
            }

            if (isInTargetDept && hasManagePermission)
            {
                return Ok(await _context.MaintenanceRequests
                    .Include(r => r.Employee)
                    .ToListAsync());
            }

            return Forbid();
        }

        [HttpPost("decision/{id}")]
        public async Task<IActionResult> Decision(int id, bool fixedStatus)
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
            var userSubDeptId = user.Employee.AdministrativeData?.SubDepartmentId;

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

            if (!isSuperAdmin && !(isInTargetDept && hasManagePermission))
                return Unauthorized("ليس لديك صلاحية اتخاذ قرار");

            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            request.Status = fixedStatus ? "تمت-الموافقة" : "مرفوض";
            await _context.SaveChangesAsync();

            return Ok($"تم تحديث حالة الطلب إلى: {request.Status}");
        }
    }
}