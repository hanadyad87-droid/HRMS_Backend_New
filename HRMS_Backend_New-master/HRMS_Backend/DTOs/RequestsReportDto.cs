namespace HRMS_Backend.DTOs
{
    public class RequestsReportDto
    {
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> Employees { get; set; } = new();
    }
}
