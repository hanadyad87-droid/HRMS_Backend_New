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
            var userId = User.FindFirstValue("UserId");
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == int.Parse(userId));

            if (employee == null) return NotFound("الموظف غير موجود");

            var request = new ExitPermitRequest
            {
                EmployeeId = employee.Id,
                // تحويل Enum إلى نص مع إرجاع المسافات بدل الشرطة السفلية
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
        // 4. عرض الطلبات المعلقة لمدير القسم (باش يوافق أو يرفض)
       
        // 5. عرض طلباتي الخاصة (للموظف العادي)
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound("الموظف غير موجود");

            // نجيبوا طلبات هذا الموظف فقط (مهما كانت حالتها)
            var myRequests = await _context.ExitPermitRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.Id) // ترتيب من الأحدث للأقدم
                .ToListAsync();

            return Ok(myRequests);
        }

        [HttpGet("pending-for-manager")]
        public async Task<IActionResult> GetPendingForManager()
        {
            var currentUserId = int.Parse(User.FindFirstValue("UserId"));
            var currentManager = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
            if (currentManager == null) return Unauthorized();

            // جلب الطلبات التي يكون فيها المدير الحالي هو المسؤول المباشر
            var requests = await _context.ExitPermitRequests
                .Include(r => r.Employee)
                .ThenInclude(e => e.AdministrativeData)
                .Where(r => r.Status == "قيد_الانتظار" &&
                    (r.Employee.AdministrativeData.Section.ManagerEmployeeId == currentManager.Id ||
                     r.Employee.AdministrativeData.SubDepartment.ManagerEmployeeId == currentManager.Id ||
                     r.Employee.AdministrativeData.Department.ManagerEmployeeId == currentManager.Id))
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPost("manager-decision/{id}")]
        public async Task<IActionResult> ManagerDecision(int id, bool approve)
        {
            var request = await _context.ExitPermitRequests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.AdministrativeData)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("الطلب غير موجود");

            var currentUserId = int.Parse(User.FindFirstValue("UserId"));
            var currentManager = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);

            // التحقق من أن الشخص الذي يضغط هو فعلاً مدير هذا الموظف (في أي مستوى)
            var admin = request.Employee.AdministrativeData;
            bool isAuthorized = admin.Section?.ManagerEmployeeId == currentManager.Id ||
                                admin.SubDepartment?.ManagerEmployeeId == currentManager.Id ||
                                admin.Department?.ManagerEmployeeId == currentManager.Id;

            if (!isAuthorized && !User.IsInRole("SuperAdmin"))
                return StatusCode(403, "لست المدير المخول باتخاذ قرار لهذا الموظف");

            request.Status = approve ? "تمت_الموافقة" : "مرفوض";

            // إضافة تنبيه للموظف بالقرار
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

        // 3. عرض الأذونات المعتمدة للإدارة المسؤولة (الموارد البشرية مثلاً)
        [HttpGet("approved-view")]
        public async Task<IActionResult> GetApproved()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.Include(e => e.AdministrativeData).FirstOrDefaultAsync(e => e.UserId == userId);

            // استخدام الـ Enum هنا
            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.ExitPermit);
            var hasPermission = User.Claims.Any(c => c.Type == "Permission" && c.Value == "ManageExitPermits");

            if (User.IsInRole("SuperAdmin") ||
               (setting != null && currentEmp.AdministrativeData?.SubDepartmentId == setting.TargetSubDepartmentId && hasPermission))
            {
                var requests = await _context.ExitPermitRequests.Include(r => r.Employee)
                                                               .Where(r => r.Status == "تمت_الموافقة")
                                                               .ToListAsync();
                return Ok(requests);
            }
            return Forbid();
        }
    }
}