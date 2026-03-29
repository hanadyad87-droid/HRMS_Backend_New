using HRMS_Backend.Enums;


namespace HRMS_Backend.Models
{
    public class TaskAssignment
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // ===== الموظف المنفذ =====
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;  // Navigation property

        // ===== الموظف الذي أسند المهمة =====
        public int AssignedByEmployeeId { get; set; }
        public Employee AssignedBy { get; set; } = null!;  // Navigation property

        // ===== القسم =====
        public int SectionId { get; set; }
        public Section Section { get; set; } = null!;

        // ===== المرفقات والحالة =====
        public string? AttachmentPath { get; set; }
        public HRMS_Backend.Enums.TaskStatus Status { get; set; } = HRMS_Backend.Enums.TaskStatus.New;
        public string? EmployeeComment { get; set; }
        public string? EmployeeAttachment { get; set; }
        public ManagerDecision ManagerDecision { get; set; } = ManagerDecision.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}