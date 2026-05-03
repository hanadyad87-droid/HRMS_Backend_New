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
        private readonly FCMHttpService _fcmService;

        public NotificationService(
            ApplicationDbContext context, 
            IHubContext<NotificationHub> hub,
            FCMHttpService fcmService)
        {
            _context = context;
            _hub = hub;
            _fcmService = fcmService;
        }

        public async Task NotifyEmployeeAsync(int employeeId, string title, string message, CancellationToken cancellationToken = default)
        {
            await NotifyEmployeeWithTypeAsync(employeeId, title, message, "general", null, cancellationToken);
        }

        public async Task NotifyEmployeeWithTypeAsync(int employeeId, string title, string message, string type, int? requestId = null, CancellationToken cancellationToken = default)
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

            // إرسال إشعار عبر SignalR
            await _hub.Clients.Group(employeeId.ToString())
                .SendAsync(
                    "ReceiveNotification",
                    new
                    {
                        notification.Id,
                        title = notification.Title,
                        message = notification.Message,
                        createdAt = notification.CreatedAt,
                        isRead = notification.IsRead,
                        type,
                        requestId
                    },
                    cancellationToken);

            // إرسال إشعار عبر Firebase Cloud Messaging (FCM)
            await SendFirebasePushNotificationWithTypeAsync(employeeId, title, message, type, requestId, cancellationToken);
        }

        private async Task SendFirebasePushNotificationAsync(int employeeId, string title, string message, CancellationToken cancellationToken = default)
        {
            await SendFirebasePushNotificationWithTypeAsync(employeeId, title, message, "general", null, cancellationToken);
        }

        private async Task SendFirebasePushNotificationWithTypeAsync(int employeeId, string title, string message, string type, int? requestId, CancellationToken cancellationToken = default)
        {
            try
            {
                // جلب FCM Token للموظف
                var fcmToken = await _context.Employees
                    .AsNoTracking()
                    .Where(e => e.Id == employeeId)
                    .Select(e => e.FcmToken)
                    .FirstOrDefaultAsync(cancellationToken);

                if (string.IsNullOrEmpty(fcmToken))
                {
                    Console.WriteLine($"لا يوجد FCM Token للموظف {employeeId}");
                    return;
                }

                // إرسال الإشعار باستخدام HTTP API
                var data = new Dictionary<string, string>
                {
                    { "notificationId", DateTime.Now.Ticks.ToString() },
                    { "type", type },
                    { "requestId", requestId?.ToString() ?? "" }
                };

                await _fcmService.SendNotificationAsync(fcmToken, title, message, data, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في إرسال إشعار FCM: {ex.Message}");
            }
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
