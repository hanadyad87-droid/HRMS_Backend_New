using HRMS_Backend.Models.HRMS_Backend.Models;

namespace HRMS_Backend.Models
{
    public class EmployeeEducation
    {
        public int Id { get; set; }

        // 🔗 ربط مع الموظف
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        // 🔗 ربط مع المؤهل
        public int QualificationId { get; set; }
        public Qualification Qualification { get; set; } = null!;

        public string Type { get; set; } = null!; // داخلي / خارجي
    }
}
