using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HRMS_Backend.DTOs
{
    public class CreateLeaveRequestDto
    {
        [Required(ErrorMessage = "نوع الإجازة مطلوب")]
        public int LeaveTypeId { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        public DateTime ToDate { get; set; }

        [MaxLength(500, ErrorMessage = "الملاحظات طويلة جداً")]
        public string? Notes { get; set; }

        // هنا التعديل المهم: لاستقبال الملف المرفوع من المتصفح
        public IFormFile? Attachment { get; set; }
    }
}