using HRMS_Backend.Models;

namespace HRMS_Backend.DTOs
{
    public class CreateEmployeeEducationDto
    {
        public int EmployeeId { get; set; }

        public int QualificationId { get; set; } // 👈 مهم

        public EducationType Type { get; set; }

        public string Institution { get; set; } = null!;
    }
}