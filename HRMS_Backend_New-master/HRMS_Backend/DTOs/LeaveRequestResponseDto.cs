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

        public string Status { get; set; }   // نص مش رقم
        public string? RejectionReason { get; set; }
    }
}
