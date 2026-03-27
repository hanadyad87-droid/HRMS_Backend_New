namespace HRMS_Backend.DTOs
{
    public class EmployeesOnLeaveDto
    {
        public string EmployeeName { get; set; } = null!;
        public string LeaveType { get; set; } = null!;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalDays { get; set; }
    }
}
