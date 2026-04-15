namespace HRMS_Backend.DTOs
{
    public class LeaveRequestResponseDto
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string LeaveType { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalDays { get; set; }

        public bool? PartialApproval { get; set; }
        public bool? FinalApproval { get; set; }

        public string? PartialNote { get; set; }
        public string? FinalNote { get; set; }

        public string? RejectionReason { get; set; }

        public string? Notes { get; set; }
        public string? AttachmentPath { get; set; }
    }
}