using HRMS_Backend.Data;
using HRMS_Backend.DTOs.ExitPermits;
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
    public class ExitPermitController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public ExitPermitController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // 1. إنشاء طلب إذن خروج
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] CreateExitPermitDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound("الموظف غير موجود");

            // تحويل الرقم لنص مقروء
            int permitTypeValue = int.TryParse(dto.PermitType, out int parsed) ? parsed : 0;
            string permitTypeText = permitTypeValue switch
            {
                0 => "خروج عاجل",
                1 => "خروج شخصي",
                2 => "خروج طبي",
                _ => "خروج"
            };

            // تحويل الوقت من string "HH:mm" لـ TimeSpan
            var fromParts = dto.FromTime.Split(':');
            var toParts = dto.ToTime.Split(':');
            var fromTimeSpan = new TimeSpan(int.Parse(fromParts[0]), int.Parse(fromParts[1]), 0);
            var toTimeSpan = new TimeSpan(int.Parse(toParts[0]), int.Parse(toParts[1]), 0);

            // التحقق من أن وقت العودة بعد وقت الخروج
            if (toTimeSpan <= fromTimeSpan)
                return BadRequest("وقت العودة يجب أن يكون بعد وقت الخروج");

            var request = new ExitPermitRequest
            {
                EmployeeId = employee.Id,
                PermitType = permitTypeText,
                PermitDate = dto.PermitDate,
                FromTime = fromTimeSpan,
                ToTime = toTimeSpan,
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
     .Where(r => r.Status == "قيد_الانتظار" &&
                 r.Employee.AdministrativeData.Section.ManagerEmployeeId == currentManager.Id)
     .OrderByDescending(r => r.Id)
     .Select(r => new
     {
         r.Id,
         r.PermitType,
         r.PermitDate,
         r.FromTime,
         r.ToTime,
         r.Reason,
         r.Status,

         Employee = new
         {
             id = r.Employee.Id,
             fullName = r.Employee.FullName
         },

         ClaimedByName = r.Employee.FullName // لو عندك claim system لاحقاً
     })
     .ToListAsync();

            return Ok(requests);
        }

        // 3. طلبات الموظف مع Pagination
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return NotFound("الموظف غير موجود");

            // Pagination + Projection للأداء
            var query = _context.ExitPermitRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.Id);

            var totalCount = await query.CountAsync();
            var myRequests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new {
                    r.Id,
                    r.PermitType,
                    r.PermitDate,
                    r.FromTime,
                    r.ToTime,
                    r.Reason,
                    r.Status,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(new {
                Data = myRequests,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
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

          
            if (approve)
            {
                request.Status = "تمت_الموافقة";
                request.IsHrNotified = true;
            }
            else
            {
                request.Status = "مرفوض";
                request.IsHrNotified = true;
            }
            await _context.SaveChangesAsync();

            await _notifications.NotifyEmployeeAsync(
                request.Employee.Id,
                "طلب إذن خروج",
                approve ? "تمت الموافقة على طلبك" : "تم رفض طلبك");

            var hrUsers = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.Permission.PermissionName == "ManageExitPermits" && up.IsAllowed)
                .Select(up => up.UserId)
                .ToListAsync();

            foreach (var hrUserId in hrUsers)
            {
                await _notifications.NotifyUserAsync(
                    hrUserId,
                    "طلب إذن خروج - تحديث",
                    $"تم {request.Status} للموظف {request.Employee.FullName}");
            }
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
        .Where(r => r.Status == "تمت_الموافقة")
        .OrderByDescending(r => r.Id)
        .ToListAsync();

            return Ok(requests);
        }
    }
}