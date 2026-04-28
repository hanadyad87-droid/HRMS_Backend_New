using FcmMessage = FirebaseAdmin.Messaging.Message;
using FcmNotification = FirebaseAdmin.Messaging.Notification;
using FcmAndroidConfig = FirebaseAdmin.Messaging.AndroidConfig;
using FcmAndroidNotification = FirebaseAdmin.Messaging.AndroidNotification;
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
                        isRead = notification.IsRead
                    },
                    cancellationToken);

            // إرسال إشعار عبر Firebase Cloud Messaging (FCM)
            await SendFirebasePushNotificationAsync(employeeId, title, message, cancellationToken);
        }

        private async Task SendFirebasePushNotificationAsync(int employeeId, string title, string message, CancellationToken cancellationToken = default)
        {
            try
            {
                // جلب FCM Token للموظف
                var employee = await _context.Employees
                    .AsNoTracking()
                    .Where(e => e.Id == employeeId)
                    .Select(e => e.FcmToken)
                    .FirstOrDefaultAsync(cancellationToken);

                if (string.IsNullOrEmpty(employee))
                {
                    // الموظف ليس لديه FCM Token
                    return;
                }

                // إنشاء الإشعار
                var fcmMessage = new FcmMessage
                {
                    Token = employee,
                    Notification = new FcmNotification
                    {
                        Title = title,
                        Body = message
                    },
                    Data = new Dictionary<string, string>
                    {
                        { "notificationId", DateTime.Now.Ticks.ToString() },
                        { "type", "general" }
                    },
                    Android = new FcmAndroidConfig
                    {
                        Priority = FirebaseAdmin.Messaging.Priority.High,
                        Notification = new FcmAndroidNotification
                        {
                            ChannelId = "hrms_channel",
                            Sound = "default"
                        }
                    }
                };

                // إرسال الإشعار
                await FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ ولكن عدم إيقاف التنفيذ
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
