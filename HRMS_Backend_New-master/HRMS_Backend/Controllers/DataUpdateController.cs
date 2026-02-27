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
    public class DataUpdateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DataUpdateController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. تقديم طلب تعديل بيانات (للموظف)
        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromForm] CreateDataUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (emp == null) return NotFound("الموظف غير موجود");

            var request = new DataUpdateRequest
            {
                EmployeeId = emp.Id,
                // تحويل Enum إلى نص
                UpdateType = dto.UpdateType.ToString().Replace("_", " "),
                NewValue = dto.NewValue,
                Reason = dto.Reason,
                Status = "قيد_الانتظار"
            };

            _context.DataUpdateRequests.Add(request);
            await _context.SaveChangesAsync();
            return Ok("تم إرسال طلب التعديل للإدارة المختصة");
        }
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound("الموظف غير موجود");

            // جلب طلبات تعديل البيانات الخاصة بهذا الموظف فقط
            var myRequests = await _context.DataUpdateRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return Ok(myRequests);
        }

        // 2. عرض الطلبات المعلقة للإدارة المسؤولة ديناميكياً
        [HttpGet("pending-for-my-dept")]
        public async Task<IActionResult> GetPending()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees
                                           .Include(e => e.AdministrativeData)
                                           .FirstOrDefaultAsync(e => e.UserId == userId);

            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.DataUpdate);

            var hasPermission = await _context.UserPermissions
                .AnyAsync(p => p.UserId == userId && p.PermissionId == 18 && p.IsAllowed);

            if (User.IsInRole("SuperAdmin") || (setting != null && hasPermission))
            {
                // الآن نجيب كل الطلبات المعلقة بدون التحقق من SubDepartmentId للموظف
                var requests = await _context.DataUpdateRequests
                    .Include(r => r.Employee)
                    .Where(r => r.Status == "قيد_الانتظار")
                    .ToListAsync();

                return Ok(requests);
            }

            return Forbid();
        }

        // 3. قرار الإدارة المختصة
        [HttpPost("decision/{id}")]
        public async Task<IActionResult> Decision(int id, bool approve)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // جلب بيانات الموظف اللي وافق (الـ HR)
            var currentEmp = await _context.Employees.Include(e => e.AdministrativeData).FirstOrDefaultAsync(e => e.UserId == userId);

            // جلب الإعدادات للتأكد من الصلاحية
            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.DataUpdate);

            if (!User.IsInRole("SuperAdmin") && (setting == null || currentEmp.AdministrativeData?.SubDepartmentId != setting.TargetSubDepartmentId))
                return Unauthorized("ليس لديك صلاحية اتخاذ قرار");

            // جلب الطلب مع بيانات الموظف (الأساسية والإدارية) صاحب الطلب
            var request = await _context.DataUpdateRequests
                .Include(r => r.Employee)
                .ThenInclude(e => e.AdministrativeData)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("الطلب غير موجود");

            if (approve)
            {
                request.Status = "مقبول";
                var emp = request.Employee;

                // التعديل التلقائي بناءً على النوع
                switch (request.UpdateType)
                {
                    case "الاسم كامل":
                        emp.FullName = request.NewValue;
                        break;
                    case "الرقم الوطني":
                        emp.NationalId = request.NewValue;
                        break;
                    case "رقم الهاتف الاول":
                        emp.Phone1 = request.NewValue;
                        break;
                    case "رقم الهاتف التاني":
                        emp.Phone2 = request.NewValue;
                        break;
                    case "الادارة":
                        // هنا NewValue لازم تكون ID الإدارة الجديدة
                        if (int.TryParse(request.NewValue, out int deptId))
                        {
                            if (emp.AdministrativeData != null)
                                emp.AdministrativeData.SubDepartmentId = deptId;
                        }
                        break;
                    case "المسمى الوظيفي":
                        // هنا NewValue لازم تكون ID المسمى الوظيفي الجديد
                        if (int.TryParse(request.NewValue, out int jobTitleId))
                        {
                            if (emp.AdministrativeData != null)
                                emp.AdministrativeData.JobTitleId = jobTitleId;
                        }
                        break;
                }
            }
            else
            {
                request.Status = "مرفوض";
            }

            await _context.SaveChangesAsync();
            return Ok(approve ? "تم قبول الطلب وتحديث البيانات بنجاح" : "تم رفض الطلب");
        }
    }
}