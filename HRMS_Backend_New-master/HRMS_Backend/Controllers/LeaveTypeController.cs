using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveTypeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            // نرجع الإجازات المفعلة فقط أو كلها حسب رغبتك
            var data = _context.LeaveTypes.OrderBy(x => x.Id).ToList();
            return Ok(data);
        }

        [HttpPost]
        public IActionResult Create(LeaveTypes leaveType)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.LeaveTypes.Add(leaveType);
            _context.SaveChanges();
            return Ok(leaveType);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, LeaveTypes model)
        {
            var leaveType = _context.LeaveTypes.Find(id);
            if (leaveType == null) return NotFound();

            leaveType.اسم_الاجازة = model.اسم_الاجازة;
            leaveType.مخصومة_من_الرصيد = model.مخصومة_من_الرصيد;
            leaveType.تحتاج_نموذج = model.تحتاج_نموذج;
            leaveType.مفعلة = model.مفعلة;

            _context.SaveChanges();
            return Ok(leaveType);
        }

        [HttpDelete("{id}")]
        public IActionResult Disable(int id)
        {
            var leaveType = _context.LeaveTypes.Find(id);
            if (leaveType == null) return NotFound();

            // بدل الحذف النهائي، نقوم بإلغاء التفعيل للحفاظ على سجلات الإجازات القديمة
            leaveType.مفعلة = false;
            _context.SaveChanges();
            return Ok(new { message = "تم إيقاف نوع الإجازة بنجاح" });
        }

        // ==========================================================
        // إضافة العطلات الرسمية الليبية الثابتة (Seed Data)
        // ==========================================================
        [HttpPost("seed-libyan-holidays")]
        public IActionResult SeedHolidays(int year)
        {
            if (year < 2024) return BadRequest("يرجى إدخال سنة صالحة");

            var existingHolidays = _context.OfficialHolidays.Where(h => h.Date.Year == year).ToList();

            // قائمة بالعطلات الثابتة في ليبيا
            var libyanHolidays = new List<OfficialHoliday>
            {
                new OfficialHoliday { Name = "عيد الثورة", Date = new DateTime(year, 2, 17) },
                new OfficialHoliday { Name = "عيد العمال", Date = new DateTime(year, 5, 1) },
                new OfficialHoliday { Name = "يوم الشهيد", Date = new DateTime(year, 9, 16) },
                new OfficialHoliday { Name = "عيد التحرير", Date = new DateTime(year, 10, 23) },
                new OfficialHoliday { Name = "عيد الاستقلال", Date = new DateTime(year, 12, 24) }
            };

            foreach (var holiday in libyanHolidays)
            {
                if (!existingHolidays.Any(h => h.Date.Date == holiday.Date.Date))
                {
                    _context.OfficialHolidays.Add(holiday);
                }
            }

            _context.SaveChanges();
            return Ok($"تمت إضافة العطلات الرسمية الليبية الثابتة لسنة {year} بنجاح. (ملاحظة: العطلات الدينية تضاف يدوياً)");
        }
    }
}