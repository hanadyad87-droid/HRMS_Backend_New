namespace HRMS_Backend.Services
{
    /// <summary>
    /// يحفظ الإشعار في قاعدة البيانات ويرسله فوراً عبر SignalR لمجموعة الموظف (EmployeeId).
    /// حقل <see cref="Models.Notification.UserId"/> يُستخدم في المشروع كمعرّف موظف (Employee.Id) ليتوافق مع التوكن والـ hub.
    /// </summary>
    public interface INotificationService
    {
        Task NotifyEmployeeAsync(int employeeId, string title, string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// إرسال إشعار مع نوع محدد ومعرف الطلب (للتنقل في التطبيق)
        /// </summary>
        Task NotifyEmployeeWithTypeAsync(int employeeId, string title, string message, string type, int? requestId = null, string? entityType = null, CancellationToken cancellationToken = default);

        /// <summary>يحوّل User.Id إلى Employee.Id عند وجود موظف مرتبط؛ وإلا يحفظ بالـ userId دون WebSocket.</summary>
        Task NotifyUserAsync(int userId, string title, string message, CancellationToken cancellationToken = default);
    }
}
