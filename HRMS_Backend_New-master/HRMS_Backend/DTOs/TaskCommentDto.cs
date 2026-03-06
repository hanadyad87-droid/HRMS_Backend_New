namespace HRMS_Backend.DTOs
{
    public class TaskCommentDto
    {
        public string Comment { get; set; }
        public string? AttachmentPath { get; set; }
        public string EmployeeName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
