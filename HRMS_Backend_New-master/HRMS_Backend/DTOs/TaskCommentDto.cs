namespace HRMS_Backend.DTOs
{
    public class TaskCommentDto
    {
        public string Comment { get; set; }
        public string? AttachmentPath { get; set; }   // ❌ غير ضروري هنا
        public string? AttachmentUrl { get; set; }    // ❌ غير ضروري هنا
        public string EmployeeName { get; set; }      // ❌ غير ضروري هنا
        public DateTime CreatedAt { get; set; }
    }
}
