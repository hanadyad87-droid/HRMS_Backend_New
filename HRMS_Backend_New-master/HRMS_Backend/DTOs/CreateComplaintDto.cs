using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using HRMS_Backend.Enums;

namespace HRMS_Backend.DTOs
{
    public class CreateComplaintDto
    {
        [Required(ErrorMessage = "مطلوب كتابة محتوى الشكوى")]
        public string Content { get; set; }

        public int? DepartmentId { get; set; }
        public bool IsForAllDepartments { get; set; }
        public IFormFile? File { get; set; } // اختياري
    }

    public class ManagerDecisionDto
    {
        [Required]
        public ComplaintStatus Status { get; set; } // اختيار الحالة الجديدة

        public string? Notes { get; set; } // ملاحظات المدير
    }
}
