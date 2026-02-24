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
            var currentEmp = await _context.Employees.Include(e => e.AdministrativeData).FirstOrDefaultAsync(e => e.UserId == userId);

            // البحث باستخدام الـ Enum
            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.SalaryCertificate);
            var hasPermission = User.Claims.Any(c => c.Type == "Permission" && c.Value == "ManageSalaryCertificates");

            if (User.IsInRole("SuperAdmin") ||
               (setting != null && currentEmp.AdministrativeData?.SubDepartmentId == setting.TargetSubDepartmentId && hasPermission))
            {
                var requests = await _context.SalaryCertificateRequests.Include(r => r.Employee)
                                                                      .Where(r => r.Status == "قيد_الانتظار")
                                                                      .ToListAsync();
                return Ok(requests);
            }
            return Forbid();
        }

        [HttpPost("decision/{id}")]
        public async Task<IActionResult> Decision(int id, bool isReady)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.Include(e => e.AdministrativeData).FirstOrDefaultAsync(e => e.UserId == userId);
            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.SalaryCertificate);

            if (!User.IsInRole("SuperAdmin") && (setting == null || currentEmp.AdministrativeData?.SubDepartmentId != setting.TargetSubDepartmentId))
                return Unauthorized("ليس لديك صلاحية اتخاذ قرار");

            var request = await _context.SalaryCertificateRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = isReady ? "جاهزة" : "مرفوض";
            await _context.SaveChangesAsync();
            return Ok($"تم تحديث حالة الطلب إلى: {request.Status}");
        }
    }
}