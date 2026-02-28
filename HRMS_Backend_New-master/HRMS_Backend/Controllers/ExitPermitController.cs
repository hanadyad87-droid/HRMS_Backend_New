using HRMS_Backend.Data;
using HRMS_Backend.DTOs.ExitPermits;
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
    public class ExitPermitController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ExitPermitController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. إنشاء طلب إذن خروج (للموظف)
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] CreateExitPermitDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

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

        // 2. جلب الطلبات المعلقة للمدير (حسب مدير القسم فقط)
        [HttpGet("pending-for-manager")]
        public async Task<IActionResult> GetPendingForManager()
        {
            var currentUserId = int.Parse(User.FindFirstValue("UserId"));

            // جلب الطلبات التي المدير الخاص بقسم الموظف هو المدير الحالي
            var requests = await _context.ExitPermitRequests
                .Include(r => r.Employee)
                .ThenInclude(e => e.AdministrativeData)
                .ThenInclude(a => a.Section)
                .Where(r => r.Status == "قيد_الانتظار" &&
                            r.Employee.AdministrativeData.Section.ManagerEmployeeId == currentUserId)
                .ToListAsync();

            return Ok(requests);
        }

        // 3. جلب طلبات الموظف الشخصية
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            var myRequests = await _context.ExitPermitRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return Ok(myRequests);
        }

        // 4. قرار المدير (موافقة أو رفض) حسب مدير القسم
        [HttpPost("manager-decision/{id}")]
        public async Task<IActionResult> ManagerDecision(int id, bool approve)
        {
            var request = await _context.ExitPermitRequests
                .Include(r => r.Employee)
                .ThenInclude(e => e.AdministrativeData)
                .ThenInclude(a => a.Section)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("الطلب غير موجود");

            var currentUserId = int.Parse(User.FindFirstValue("UserId"));

            // تحقق أن المدير الحالي هو مدير قسم الموظف
            if (request.Employee.AdministrativeData.Section.ManagerEmployeeId != currentUserId &&
                !User.IsInRole("SuperAdmin"))
            {
                return Forbid("لست مدير القسم المخول لاتخاذ قرار لهذا الموظف");
            }

            request.Status = approve ? "تمت_الموافقة" : "مرفوض";

            // إضافة تنبيه للموظف
            _context.Notifications.Add(new Notification
            {
                UserId = request.Employee.UserId,
                Title = "تحديث طلب إذن خروج",
                Message = $"تم {request.Status} على طلب إذن الخروج الخاص بك",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = $"تم {request.Status} بنجاح" });
        }

        // 5. عرض الأذونات المعتمدة بدون التحقق المعقد
        [HttpGet("approved-view")]
        public async Task<IActionResult> GetApproved()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var hasPermission = await _context.UserPermissions
                .AnyAsync(p => p.UserId == userId && p.PermissionId == 17 && p.IsAllowed);

            if (!hasPermission && !User.IsInRole("SuperAdmin"))
                return Forbid();

            var requests = await _context.ExitPermitRequests
                .Include(r => r.Employee)
                .Where(r => r.Status == "تمت_الموافقة")
                .ToListAsync();

            return Ok(requests);
        }
    }
}