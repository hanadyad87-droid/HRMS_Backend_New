namespace HRMS_Backend.DTOs
{
    public class EmployeesByQualificationDto
    {
        public string QualificationName { get; set; } = null!;

        public int Count { get; set; }

        public List<string> Employees { get; set; } = new();
    }
}