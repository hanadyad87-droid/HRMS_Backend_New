namespace HRMS_Backend.Models
{
    public class EmployeeEducation
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public string Degree { get; set; }        // بكالوريوس، ماجستير
        public string Major { get; set; }         // التخصص
        public string University { get; set; }   // الجامعة
        public int GraduationYear { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
