namespace HRMS_Backend.Models
{
    public class subDepartment
    {
        public int Id { get; set; }

        public string Name { get; set; }

        // العلاقة مع الإدارة الرئيسية
        public int DepartmentId { get; set; }
        public Department Department { get; set; }

        // المدير الحالي
        public int? ManagerEmployeeId { get; set; }
        public Employee? ManagerEmployee { get; set; }

        // المدير السابق
        public int? PreviousManagerId { get; set; }
        public Employee? PreviousManager { get; set; }
        public ICollection<Section> Sections { get; set; }

    }
}
