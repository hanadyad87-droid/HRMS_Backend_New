namespace HRMS_Backend.DTOs
{
    public class CreateTaskCommentDto
    {
        public string Comment { get; set; }
        public IFormFile? Attachment { get; set; }
    }
}
