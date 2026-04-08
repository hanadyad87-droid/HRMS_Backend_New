namespace HRMS_Backend.DTOs
{
    public class OrganizationEntityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;            // اسم الإدارة أو القسم
        public string Type { get; set; } = string.Empty;            // department / subdepartment / section
        public DateTime CreatedAt { get; set; }                     // تاريخ الإضافة
        public string? Parent { get; set; }                         // الإدارة الأم أو القسم الأم (nullable)
        public string ManagerName { get; set; } = string.Empty;     // اسم المدير الحالي
        public string PreviousManagerName { get; set; } = string.Empty; // اسم المدير السابق
    }
}
