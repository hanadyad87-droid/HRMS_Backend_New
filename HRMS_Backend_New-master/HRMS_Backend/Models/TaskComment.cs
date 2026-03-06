namespace HRMS_Backend.Models
{
    public class TaskComment
    {
        public int Id { get; set; }
        public int TaskAssignmentId { get; set; }
        public TaskAssignment TaskAssignment { get; set; }

        public int EmployeeId { get; set; } // الشخص اللي كتب التعليق
        public string Comment { get; set; }
        public string? AttachmentPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
