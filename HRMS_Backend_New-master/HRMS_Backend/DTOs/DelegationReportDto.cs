namespace HRMS_Backend.DTOs
{
    public class DelegationReportDto
    {
        public string ActingManager { get; set; } = string.Empty;
        public string OriginalManager { get; set; } = string.Empty;
        public string AssignedBy { get; set; } = string.Empty;

        public string EntityType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
