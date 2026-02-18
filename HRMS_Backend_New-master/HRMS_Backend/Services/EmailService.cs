using System.Net;
using System.Net.Mail;

namespace HRMS_Backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // جلب الإعدادات من ملف appsettings.json
            var host = _config["SmtpSettings:Host"];
            var port = int.Parse(_config["SmtpSettings:Port"] ?? "587");
            var username = _config["SmtpSettings:Username"];
            var appPassword = _config["SmtpSettings:AppPassword"];

            // إعداد العميل (SMTP Client)
            using var client = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(username, appPassword),
                EnableSsl = true, // ضروري للأمان في Gmail و Outlook
            };

            // تجهيز نص الرسالة
            var mailMessage = new MailMessage
            {
                From = new MailAddress(username!, "نظام الموارد البشرية"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true, // عشان تقدر تبعت تنسيقات HTML في الإيميل
            };

            mailMessage.To.Add(toEmail);

            // الإرسال الفعلي
            await client.SendMailAsync(mailMessage);
        }
    }
}