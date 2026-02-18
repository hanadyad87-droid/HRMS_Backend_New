using HRMS_Backend.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_Backend.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        // الموظف
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        // نوع الإجازة
        public int LeaveTypeId { get; set; }
        [ForeignKey("LeaveTypeId")]
        public LeaveTypes? LeaveType { get; set; }

        // من - إلى
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // عدد الأيام الفعلي (بعد استثناء الجمعة والسبت والعطلات في ليبيا)
        public int TotalDays { get; set; }

        // ملاحظات الموظف
        public string? Notes { get; set; }

        // حالة الطلب
        public LeaveStatus Status { get; set; }

        // الإجراءات الإدارية
        public string? سبب_الرفض { get; set; }
        public string? ManagerNote { get; set; }

        // مسار ملف المرفق (النموذج)
        public string? AttachmentPath { get; set; }

   
    }
}