namespace HRMS_Backend.Models
{
    public class Employee
    {
        public int Id { get; set; }

        // بيانات الموظف الأساسية
        public string EmployeeNumber { get; set; } = null!;
        public Guid PublicId { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = null!;
        public string Phone1 { get; set; } = null!;
        public string? Phone2 { get; set; }
        public string MotherName { get; set; } = null!;
        public string NationalId { get; set; } = null!;
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; } = null!;
       

        public int MaritalStatusId { get; set; }
        public MaritalStatus MaritalStatus { get; set; } = null!;

        // تم إزالة الحقول التالية لأنها غير موجودة في DTO:
        // JobTitleId, JobTitle, EmploymentStatusId, EmploymentStatus, DepartmentId, Department
        // WorkLocationId, WorkLocation, JobGradeId, JobGrade

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // 🔹 صورة الموظف
        public string? PhotoPath { get; set; }

        public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
        public ICollection<EmployeeEducation> Educations { get; set; } = new List<EmployeeEducation>();
        public EmployeeAdministrativeData AdministrativeData { get; set; } = null!;
        public EmployeeFinancialData? FinancialData { get; set; }

    }
}
