using HRMS_Backend.Enums;

namespace HRMS_Backend.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        // الموظف
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        // نوع الإجازة
        public int LeaveTypeId { get; set; }
        public LeaveTypes? LeaveType { get; set; }

        // من - إلى
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // عدد الأيام
        public int TotalDays { get; set; }

        // ملاحظات
        public string? Notes { get; set; }

        // حالة الطلب
        public LeaveStatus Status { get; set; }

        public string? سبب_الرفض { get; set; }
        public string? ManagerNote { get; set; }
        public string? AttachmentPath { get; set; }
    }
}
