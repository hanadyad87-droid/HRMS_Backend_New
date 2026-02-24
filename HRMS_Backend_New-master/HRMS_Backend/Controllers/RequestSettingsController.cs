using HRMS_Backend.Data;
using HRMS_Backend.Enums;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")] // الوصول للأدمن فقط
    public class RequestSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RequestSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. جلب كل الإدارات الفرعية (عشان الدروب داون في الفرونت إند)
        [HttpGet("sub-departments")]
        public async Task<IActionResult> GetSubDepartments()
        {
            var depts = await _context.SubDepartments
                .Select(d => new
                {
                    Id = d.Id,
                    Name = d.Name
                })
                .ToListAsync();
            return Ok(depts);
        }

        // 2. جلب التوجيهات الحالية (عشان الأدمن يشوف الإعدادات الحالية)
        [HttpGet("all-routings")]
        public async Task<IActionResult> GetAllRoutings()
        {
            var settings = await _context.RequestSettings.ToListAsync();
            return Ok(settings);
        }

        // 3. تحديد التوجيه (باستخدام الـ Enum لضمان ظهور القائمة في Swagger)
        [HttpPost("set-routing")]
        public async Task<IActionResult> SetRouting(RequestType type, int deptId)
        {
            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == type);

            if (setting == null)
            {
                _context.RequestSettings.Add(new RequestSetting { RequestType = type, TargetSubDepartmentId = deptId });
            }
            else
            {
                setting.TargetSubDepartmentId = deptId;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = $"تم توجيه طلبات {type} إلى الإدارة رقم {deptId} بنجاح" });
        }
    }
}