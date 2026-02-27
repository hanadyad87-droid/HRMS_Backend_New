namespace HRMS_Backend.DTOs
{
    public class AssignRoleAndEntityDto
    {
        public int EmployeeId { get; set; }
        public int RoleId { get; set; }      // 3 = مدير إدارة
                                             // 4 = مدير إدارة فرعية
                                             // 5 = مدير قسم

        public string Type { get; set; }     // department | subdepartment | section
        public int EntityId { get; set; }    // Id الإدارة أو الفرعية أو القسم
    }
}
