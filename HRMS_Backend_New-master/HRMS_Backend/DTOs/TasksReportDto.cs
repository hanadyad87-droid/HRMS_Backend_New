namespace HRMS_Backend.DTOs
{
    public class TasksReportDto
    {
        public string Status { get; set; } = null!;

        public int Count { get; set; }

        public List<string> Employees { get; set; } = new();
    }
}