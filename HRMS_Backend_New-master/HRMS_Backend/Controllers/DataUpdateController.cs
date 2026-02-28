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
            var userIdClaim = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = int.Parse(userIdClaim);
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (emp == null) return NotFound("الموظف غير موجود");

            var request = new DataUpdateRequest
            {
                EmployeeId = emp.Id,
                // تخزين القيمة مع استبدال الشرطة السفلية بمسافة لشكل أجمل في العرض
                UpdateType = dto.UpdateType.ToString().Replace("_", " "),
                NewValue = dto.NewValue,
                Reason = dto.Reason,
                Status = "قيد_الانتظار",
                CreatedAt = DateTime.Now
            };

            _context.DataUpdateRequests.Add(request);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "تم إرسال طلب التعديل للإدارة المختصة بنجاح" });
        }

        // 2. عرض طلباتي الخاصة (للموظف)
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound("الموظف غير موجود");

            var myRequests = await _context.DataUpdateRequests
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

        // 4. قرار الإدارة المختصة (الموافقة وتحديث البيانات تلقائياً)
        [HttpPost("decision/{id}")]
        public async Task<IActionResult> Decision(int id, bool approve)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees.Include(e => e.AdministrativeData).FirstOrDefaultAsync(e => e.UserId == userId);
            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.DataUpdate);

            if (!User.IsInRole("SuperAdmin") && (setting == null || currentEmp?.AdministrativeData?.SubDepartmentId != setting.TargetSubDepartmentId))
                return Unauthorized("ليس لديك صلاحية اتخاذ قرار");

            var request = await _context.DataUpdateRequests
                .Include(r => r.Employee)
                .ThenInclude(e => e.AdministrativeData)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("الطلب غير موجود");

            if (approve)
            {
                request.Status = "مقبول";
                var emp = request.Employee;

                // تحويل النص المخزن إلى Enum للمقارنة الصحيحة
                // نعيد المسافة إلى شرطة سفلية ليطابق الـ Enum التعريف الأصلي
                var enumKey = request.UpdateType.Replace(" ", "_");

                if (Enum.TryParse(enumKey, out DataUpdateField field))
                {
                    switch (field)
                    {
                        case DataUpdateField.الاسم_الكامل:
                            emp.FullName = request.NewValue;
                            break;
                        case DataUpdateField.الرقم_الوطني:
                            emp.NationalId = request.NewValue;
                            break;
                        case DataUpdateField.رقم_الهاتف_الأول:
                            emp.Phone1 = request.NewValue;
                            break;
                        case DataUpdateField.رقم_الهاتف_الثاني:
                            emp.Phone2 = request.NewValue;
                            break;
                        case DataUpdateField.الإدارة:
                            if (int.TryParse(request.NewValue, out int deptId) && emp.AdministrativeData != null)
                                emp.AdministrativeData.SubDepartmentId = deptId;
                            break;
                        case DataUpdateField.المسمى_الوظيفي:
                            if (int.TryParse(request.NewValue, out int jobTitleId) && emp.AdministrativeData != null)
                                emp.AdministrativeData.JobTitleId = jobTitleId;
                            break;
                    }
                }
            }
            else
            {
                request.Status = "مرفوض";
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = approve ? "تم قبول الطلب وتحديث البيانات تلقائياً" : "تم رفض الطلب" });
        }
    }
}