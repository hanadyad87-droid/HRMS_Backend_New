using HRMS_Backend.Data;
using HRMS_Backend.DTOs; // تأكدي من وجود الـ DTO هنا
using HRMS_Backend.Models;
using HRMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncementsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public AnnouncementsController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // ========================
        // GET: جلب الإعلانات الخاصة بالموظف الحالي (تلقائياً)
        // ========================
        // ========================
        // GET: جلب الإعلانات الخاصة بالموظف الحالي (باستخدام EmployeeId من التوكن)
        // ========================
        [HttpGet("my-announcements")]
        [Authorize]
        public async Task<IActionResult> GetMyAnnouncements()
        {
            // نجيب EmployeeId من الـ JWT
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

            if (string.IsNullOrEmpty(employeeIdClaim))
                return Unauthorized("بيانات الموظف غير موجودة في التوكن");

            if (!int.TryParse(employeeIdClaim, out int employeeId))
                return Unauthorized("EmployeeId غير صالح");

            // نجيب الموظف
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            // نجيب بياناته الإدارية
            var adminData = await _context.EmployeeAdministrativeDatas
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId);

            var query = _context.Announcements
                .Where(a => a.Active);

            if (adminData != null)
            {
                query = query.Where(a =>
                    a.TargetAll ||
                    a.TargetDepartmentId == adminData.DepartmentId);
            }
            else
            {
                // لو ما عندهش إدارة → يشوف الإعلانات العامة فقط
                query = query.Where(a => a.TargetAll);
            }

            var announcements = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(announcements);
        }



        // ========================
        // GET: جلب جميع الإعلانات (للأدمن مع إمكانية الفلترة)
        // ========================
        [HttpGet]
        public async Task<IActionResult> GetAnnouncements(int? departmentId = null)
        {
            var query = _context.Announcements.AsQueryable();

            if (departmentId != null)
            {
                query = query.Where(a => a.TargetAll || a.TargetDepartmentId == departmentId);
            }

            var announcements = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(announcements);
        }

        // ========================
        // POST: إنشاء إعلان جديد
        // ========================
        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var announcement = new Announcement
            {
                Title = dto.Title,
                Message = dto.Message,
                TargetAll = dto.TargetAll,
                TargetDepartmentId = dto.TargetDepartmentId,
                Active = dto.Active,
                CreatedAt = DateTime.Now // التأكيد على وقت الإنشاء
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            await NotifyTargetEmployeesAsync(announcement);

            return Ok(announcement);
        }

        // ========================
        // PUT: تعديل إعلان موجود
        // ========================
        // ========================
        // PUT: تعديل إعلان موجود مع إمكانية تغيير الحالة
        // ========================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnnouncement(int id, [FromBody] AnnouncementDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
                return NotFound("الإعلان غير موجود");

            // تحديث البيانات من DTO
            announcement.Title = dto.Title;
            announcement.Message = dto.Message;
            announcement.TargetAll = dto.TargetAll;
            announcement.TargetDepartmentId = dto.TargetDepartmentId;
            announcement.Active = dto.Active;

            await _context.SaveChangesAsync();
            await NotifyTargetEmployeesAsync(announcement, isUpdate: true);

            return Ok(new
            {
                message = "تم تعديل الإعلان بنجاح",
                announcement
            });
        }


        // ========================
        // DELETE: تعطيل الإعلان
        // ========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
                return NotFound("الإعلان غير موجود");

            announcement.Active = false; // تعطيل بدل الحذف النهائي

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تعطيل الإعلان بنجاح" });
        }

        private async Task NotifyTargetEmployeesAsync(Announcement announcement, bool isUpdate = false)
        {
            var targetEmployeeIds = _context.EmployeeAdministrativeDatas
                .AsNoTracking()
                .Where(a => announcement.TargetAll || a.DepartmentId == announcement.TargetDepartmentId)
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToList();

            if (!targetEmployeeIds.Any())
                return;

            var title = isUpdate ? "تحديث إعلان" : "إعلان جديد";
            var action = isUpdate ? "تم تحديث إعلان" : "تم نشر إعلان";
            var message = $"{action}: {announcement.Title} [[route:/announcements?announcementId={announcement.Id}]]";

            foreach (var employeeId in targetEmployeeIds)
            {
                await _notifications.NotifyEmployeeAsync(employeeId, title, message);
            }
        }
    }
}