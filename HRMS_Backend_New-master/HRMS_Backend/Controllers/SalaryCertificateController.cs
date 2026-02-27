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
    public class SalaryCertificateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SalaryCertificateController(ApplicationDbContext context) => _context = context;

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

            // جلب طلبات شهادة المرتب الخاصة بهذا الموظف فقط
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
            var currentEmp = await _context.Employees
                                           .Include(e => e.AdministrativeData)
                                           .FirstOrDefaultAsync(e => e.UserId == userId);

            if (currentEmp == null) return NotFound("الموظف غير موجود");

            var setting = await _context.RequestSettings
                                        .FirstOrDefaultAsync(s => s.RequestType == RequestType.SalaryCertificate);

            var hasPermission = User.Claims.Any(c => c.Type == "Permission" && c.Value == "ManageSalaryCertificates");

            // 1. السوبر أدمن يشوف كل شيء
            if (User.IsInRole("SuperAdmin"))
            {
                return Ok(await _context.SalaryCertificateRequests
                    .Include(r => r.Employee)
                    .ThenInclude(e => e.AdministrativeData) // مهم للعرض
                    .Where(r => r.Status == "قيد_الانتظار")
                    .ToListAsync());
            }

            // 2. الموظف المخول (مثل نهى في المالية)
            // الشرط هنا: لازم تكون إدارتها هي نفس الإدارة المستهدفة في الإعدادات + عندها الصلاحية
            if (setting != null && hasPermission && currentEmp.AdministrativeData?.SubDepartmentId == setting.TargetSubDepartmentId)
            {
                // هنا نجلب كل الطلبات "قيد الانتظار" لشهادات المرتب من كل الإدارات
                var allPendingRequests = await _context.SalaryCertificateRequests
                    .Include(r => r.Employee)
                    .ThenInclude(e => e.AdministrativeData)
                    .Where(r => r.Status == "قيد_الانتظار")
                    .ToListAsync();

                return Ok(allPendingRequests);
            }

            return StatusCode(403, "ليس لديك صلاحية الوصول لطلبات هذه الإدارة");
        }
        [HttpPost("decision/{id}")]
        public async Task<IActionResult> Decision(int id, bool isReady)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.Include(e => e.AdministrativeData).FirstOrDefaultAsync(e => e.UserId == userId);
            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.SalaryCertificate);

            // إضافة تشيك الصلاحية هنا أيضاً للأمان
            var hasPermission = User.Claims.Any(c => c.Type == "Permission" && c.Value == "ManageSalaryCertificates");

            if (!User.IsInRole("SuperAdmin") && (!hasPermission || currentEmp.AdministrativeData?.SubDepartmentId != setting?.TargetSubDepartmentId))
                return StatusCode(403, "ليس لديك صلاحية اتخاذ قرار على هذا الطلب");

            var request = await _context.SalaryCertificateRequests.FindAsync(id);
            if (request == null) return NotFound("الطلب غير موجود");

            request.Status = isReady ? "جاهزة" : "مرفوض";

            // فكرة من كود الإجازات: إضافة تنبيه للموظف أن طلبه جهز
            _context.Notifications.Add(new Notification
            {
                UserId = request.Employee.UserId, // تأكدي من عمل Include للموظف قبل هذا السطر
                Title = "تحديث طلب شهادة مرتب",
                Message = $"طلبك أصبح حالته: {request.Status}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok($"تم تحديث حالة الطلب إلى: {request.Status}");
        }
    }
}