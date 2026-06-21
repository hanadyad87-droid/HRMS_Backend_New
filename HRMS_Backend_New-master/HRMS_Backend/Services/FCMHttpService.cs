using System.Text;
using System.Text.Json;

namespace HRMS_Backend.Services
{
    /// <summary>
    /// خدمة إرسال إشعارات FCM باستخدام HTTP API (Legacy) - لا تحتاج ملف credentials
    /// </summary>
    public class FCMHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FCMHttpService> _logger;

        // Server Key من Firebase Console → Project Settings → Cloud Messaging → Server Key
        private string ServerKey => _configuration["Firebase:ServerKey"] 
            ?? Environment.GetEnvironmentVariable("FIREBASE_SERVER_KEY")
            ?? "AAAAZ-YourServerKeyHere:APA91b..."; // TODO: ضع Server Key هنا

        public FCMHttpService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<FCMHttpService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendNotificationAsync(
            string fcmToken, 
            string title, 
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ServerKey) ||
                ServerKey.Contains("YourServerKeyHere", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("FCM Server Key غير مُعدّ — تخطّي إرسال Push. أضف Firebase:ServerKey في appsettings أو FIREBASE_SERVER_KEY");
                return false;
            }

            try
            {
                var message = new
                {
                    to = fcmToken,
                    notification = new
                    {
                        title = title,
                        body = body,
                        sound = "default"
                    },
                    data = data ?? new Dictionary<string, string>(),
                    priority = "high"
                };

                var json = JsonSerializer.Serialize(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"key={ServerKey}");

                var response = await _httpClient.PostAsync(
                    "https://fcm.googleapis.com/fcm/send", 
                    content, 
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("FCM sent successfully");
                    return true;
                }
                else
                {
                    _logger.LogError("FCM failed: {Body}", responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending FCM");
                return false;
            }
        }
    }
}
