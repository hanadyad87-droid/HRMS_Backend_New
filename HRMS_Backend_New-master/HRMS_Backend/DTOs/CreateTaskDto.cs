using Microsoft.AspNetCore.Http;

namespace HRMS_Backend.DTOs
{
    public class CreateTaskDto
    {
        public int EmployeeId { get; set; }

        

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public IFormFile? Attachment { get; set; }
    }
}