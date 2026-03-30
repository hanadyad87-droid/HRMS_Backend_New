namespace HRMS_Backend.Models
{
    public class EmployeeEducation
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public int QualificationId { get; set; }
        public Qualification Qualification { get; set; } = null!;

        public EducationType Type { get; set; }

        public string Institution { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}