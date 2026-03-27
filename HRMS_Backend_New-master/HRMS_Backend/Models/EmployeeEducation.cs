namespace HRMS_Backend.Models
{
    public class EmployeeEducation
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public string Name { get; set; } // اسم المؤهل

        public string Type { get; set; } // عام / خاص

        public string Institution { get; set; } // اسم المكان

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
