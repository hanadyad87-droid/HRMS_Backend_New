namespace HRMS_Backend.DTOs
{
    public class EmployeesBySubDepartmentDto
    {
        public string SubDepartmentName { get; set; } = null!;
        public int EmployeeCount { get; set; }
        public List<string> Employees { get; set; } = new();
    }
}
