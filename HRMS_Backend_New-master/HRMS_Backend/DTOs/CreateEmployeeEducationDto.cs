namespace HRMS_Backend.DTOs
{
    public class CreateEmployeeEducationDto
    {
        public int EmployeeId { get; set; }
        public string Degree { get; set; }
        public string Major { get; set; }
        public string University { get; set; }
        public int GraduationYear { get; set; }
    
}
}
