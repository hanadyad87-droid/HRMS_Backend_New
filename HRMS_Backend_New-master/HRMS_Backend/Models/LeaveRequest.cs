using System.ComponentModel.DataAnnotations.Schema;
using HRMS_Backend.Enums;

namespace HRMS_Backend.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        public int LeaveTypeId { get; set; }
        [ForeignKey("LeaveTypeId")]
        public LeaveTypes? LeaveType { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public int TotalDays { get; set; }

        public string? Notes { get; set; }

        public string? سبب_الرفض { get; set; }

        // ===== APPROVAL SYSTEM =====
        /// <summary>يُحدَّد عند الإنشاء ولا يتغيّر — يحدد من يملك الموافقة في كل مرحلة.</summary>
        public LeaveApprovalFlow ApprovalFlow { get; set; } = LeaveApprovalFlow.RegularEmployee;

        public bool? PartialApproval { get; set; }
        public bool? FinalApproval { get; set; }

        public string? PartialNote { get; set; }
        public string? FinalNote { get; set; }

        public string? AttachmentPath { get; set; }
    }
}