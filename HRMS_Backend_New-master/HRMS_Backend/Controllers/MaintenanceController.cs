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
            var currentEmp = await _context.Employees
                                           .Include(e => e.AdministrativeData)
                                           .FirstOrDefaultAsync(e => e.UserId == userId);

            // البحث باستخدام الـ Enum
            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.Maintenance);

            // التأكد من الصلاحية
            var hasPermission = await _context.UserPermissions
                .AnyAsync(p => p.UserId == userId && p.PermissionId == 15 && p.IsAllowed);

            if (User.IsInRole("SuperAdmin") || (setting != null && hasPermission))
            {
                // جلب كل طلبات الصيانة المعلقة دون النظر إلى SubDepartmentId
                var requests = await _context.MaintenanceRequests
                    .Include(r => r.Employee)
                    .Where(r => r.Status == "قيد_الانتظار")
                    .ToListAsync();

                return Ok(requests);
            }

            return Forbid();
        }

        [HttpPost("decision/{id}")]
        public async Task<IActionResult> Decision(int id, bool fixedStatus)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.Include(e => e.AdministrativeData)
                                                     .FirstOrDefaultAsync(e => e.UserId == userId);
            var setting = await _context.RequestSettings
                                        .FirstOrDefaultAsync(s => s.RequestType == RequestType.Maintenance);

            if (!User.IsInRole("SuperAdmin") && (setting == null || currentEmp.AdministrativeData?.SubDepartmentId != setting.TargetSubDepartmentId))
                return Unauthorized("ليس لديك صلاحية اتخاذ قرار");

            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = fixedStatus ? "تم_الإصلاح" : "مرفوض";
            await _context.SaveChangesAsync();
            return Ok($"تم تحديث حالة الطلب إلى: {request.Status}");
        }
    }
}