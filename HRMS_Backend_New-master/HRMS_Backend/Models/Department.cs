namespace HRMS_Backend.Models
{
    public class Department
    {
        public int Id { get; set; }

        public string Name { get; set; }

        // المدير الحالي
        public int? ManagerEmployeeId { get; set; }
        public Employee? ManagerEmployee { get; set; }

        // المدير السابق
        public int? PreviousManagerId { get; set; }
        public Employee? PreviousManager { get; set; }
        // الإدارات الفرعية التابعة له
        public ICollection<subDepartment> SubDepartments { get; set; } = new List<subDepartment>();
        // الموظفين التابعين للإدارة
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
