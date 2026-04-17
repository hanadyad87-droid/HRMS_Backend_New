using HRMS_Backend.Data;
using HRMS_Backend.Hubs;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public async Task NotifyEmployeeAsync(int employeeId, string title, string message, CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                UserId = employeeId,
                Title = title,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            await _hub.Clients.Group(employeeId.ToString())
                .SendAsync(
                    "ReceiveNotification",
                    new
                    {
                        notification.Id,
                        title = notification.Title,
                        message = notification.Message,
                        createdAt = notification.CreatedAt,
                        isRead = notification.IsRead
                    },
                    cancellationToken);
        }

        public async Task NotifyUserAsync(int userId, string title, string message, CancellationToken cancellationToken = default)
        {
            var employeeId = await _context.Employees
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (employeeId.HasValue)
            {
                await NotifyEmployeeAsync(employeeId.Value, title, message, cancellationToken);
                return;
            }

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
