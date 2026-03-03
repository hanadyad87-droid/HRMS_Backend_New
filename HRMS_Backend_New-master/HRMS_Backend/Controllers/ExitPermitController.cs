using HRMS_Backend.Data;
using HRMS_Backend.DTOs.ExitPermits;
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
    public class ExitPermitController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ExitPermitController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. إنشاء طلب إذن خروج
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] CreateExitPermitDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound("الموظف غير موجود");

            var request = new ExitPermitRequest
            {
                EmployeeId = employee.Id,
                PermitType = dto.PermitType.ToString().Replace("_", " "),
                PermitDate = dto.PermitDate,
                PermitTime = dto.PermitTime,
                Reason = dto.Reason,
                Status = "قيد_الانتظار"
            };

            _context.ExitPermitRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم إرسال طلب إذن الخروج بنجاح" });
        }

        // 2. جلب الطلبات المعلقة للمدير
        [HttpGet("pending-for-manager")]
        public async Task<IActionResult> GetPendingForManager()
        {
            var currentUserId = int.Parse(User.FindFirstValue("UserId"));
            var currentManager = await _context.Employees
                .Include(e => e.AdministrativeData)
                    .ThenInclude(a => a.Section)
                .FirstOrDefaultAsync(e => e.UserId == currentUserId);

            if (currentManager == null) return Unauthorized();

            var requests = await _context.ExitPermitRequests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.AdministrativeData)
                        .ThenInclude(a => a.Section)
                .Where(r => r.Status == "قيد_الانتظار" &&
                            r.Employee.AdministrativeData.Section.ManagerEmployeeId == currentManager.Id)
                .ToListAsync();

            return Ok(requests);
        }

        // 3. طلبات الموظف
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return NotFound("الموظف غير موجود");

            var myRequests = await _context.ExitPermitRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return Ok(myRequests);
        }

        // 4. قرار المدير
        [HttpPost("manager-decision/{id}")]
        public async Task<IActionResult> ManagerDecision(int id, bool approve)
        {
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out int currentUserId))
                return Unauthorized();

            var currentManager = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == currentUserId);

            if (currentManager == null)
                return Unauthorized("المدير غير موجود");

            var request = await _context.ExitPermitRequests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.AdministrativeData)
                        .ThenInclude(a => a.Section)
                .Include(r => r.Employee)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("الطلب غير موجود");

            var empAdmin = request.Employee.AdministrativeData;

            if (empAdmin == null || empAdmin.Section == null)
                return BadRequest("القسم غير مربوط بالموظف");

            // ✅ فقط مدير قسم الموظف يقرر
            if (empAdmin.Section.ManagerEmployeeId != currentManager.Id)
                return Forbid();

            request.Status = approve ? "تمت_الموافقة" : "مرفوض";

            // إشعار الموظف
            _context.Notifications.Add(new Notification
            {
                UserId = request.Employee.UserId,
                Title = "تحديث إذن خروج",
                Message = $"تم {request.Status} على طلب إذن الخروج الخاص بك",
                CreatedAt = DateTime.Now
            });

            // إشعار HR كمعلومة فقط
            var hrUsers = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.Permission.PermissionName == "ManageExitPermits" && up.IsAllowed)
                .Select(up => up.UserId)
                .ToListAsync();

            foreach (var hrUserId in hrUsers)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = hrUserId,
                    Title = "معلومة - إذن خروج",
                    Message = $"طلب إذن خروج للموظف {request.Employee.FullName} أصبح: {request.Status}",
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Ok($"تم {request.Status} بنجاح");
        }
        [HttpGet("hr-view")]
        public async Task<IActionResult> GetForHr()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // لازم يكون عنده صلاحية HR
            var hasPermission = await _context.UserPermissions
                .Include(up => up.Permission)
                .AnyAsync(up =>
                    up.UserId == userId &&
                    up.Permission.PermissionName == "ManageExitPermits" &&
                    up.IsAllowed);

            if (!hasPermission && !User.IsInRole("SuperAdmin"))
                return Forbid();

            // HR يشوفوا الطلبات اللي المدير وافق عليها فقط
            var requests = await _context.ExitPermitRequests
                .Include(r => r.Employee)
                .Where(r =>
                    r.Status == "تمت_الموافقة" &&
                    r.IsHrNotified == true
                )
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return Ok(requests);
        }
    }
}