using HRMS_Backend.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 جلب إشعارات الموظف الحالي
        [HttpGet]
        public IActionResult GetMyNotifications()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (employeeIdClaim == null)
                return Unauthorized("EmployeeId missing in token.");

            var employeeId = int.Parse(employeeIdClaim);

            var notifications = _context.Notifications
                .Where(n => n.UserId == employeeId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return Ok(notifications);
        }

        // 🔥 تعليم كل الإشعارات كمقروءة
        [HttpPut("read-all")]
        public IActionResult MarkAllAsRead()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (employeeIdClaim == null)
                return Unauthorized("EmployeeId missing in token.");

            var employeeId = int.Parse(employeeIdClaim);

            var notifications = _context.Notifications
                .Where(n => n.UserId == employeeId && !n.IsRead)
                .ToList();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            _context.SaveChanges();

            return Ok("All notifications marked as read.");
        }

        // 🔹 تعليم إشعار كمقروء
        [HttpPut("{id}/read")]
        public IActionResult MarkAsRead(int id)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (employeeIdClaim == null)
                return Unauthorized("EmployeeId missing in token.");

            var employeeId = int.Parse(employeeIdClaim);

            var notification = _context.Notifications
                .FirstOrDefault(n => n.Id == id && n.UserId == employeeId);

            if (notification == null)
                return NotFound("Notification not found or does not belong to you.");

            notification.IsRead = true;
            _context.SaveChanges();

            return Ok("Notification marked as read.");
        }
    }
}