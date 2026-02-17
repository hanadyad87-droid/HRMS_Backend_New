namespace HRMS_Backend.Models
{
    public class Section
    {
        public int Id { get; set; }

        public string Name { get; set; }

        // العلاقة مع الإدارة الفرعية
        public int SubDepartmentId { get; set; }
        public subDepartment SubDepartment { get; set; }

        // المدير الحالي
        public int? ManagerEmployeeId { get; set; }
        public Employee? ManagerEmployee { get; set; }

        // المدير السابق
        public int? PreviousManagerId { get; set; }
        public Employee? PreviousManager { get; set; }


    }
}
