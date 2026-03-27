namespace HRMS_Backend.DTOs
{
    public class CreateEmployeeEducationDto
    {
        public int EmployeeId { get; set; }

        public string Name { get; set; }

        public string Type { get; set; } // عام / خاص

        public string Institution { get; set; }
    }
}
