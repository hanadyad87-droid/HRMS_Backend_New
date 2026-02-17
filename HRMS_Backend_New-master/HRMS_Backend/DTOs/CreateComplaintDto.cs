using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using HRMS_Backend.Enums;

namespace HRMS_Backend.DTOs
{
    public class CreateComplaintDto
    {
        [Required(ErrorMessage = "مطلوب كتابة محتوى الشكوى")]
        public string Content { get; set; }
        public bool IsAnonymous { get; set; }

        public int? DepartmentId { get; set; }  // لو الشكوى لإدارة معينة
        public bool IsForAllDepartments { get; set; }  // لو الشكوى لجميع الإدارات

        public IFormFile? File { get; set; } // نفس الاسم المستخدم في الـ controller
    }

    public class ManagerDecisionDto
    {
        [Required]
        public ComplaintStatus Status { get; set; } // اختيار الحالة الجديدة

        public string? Notes { get; set; } // ملاحظات المدير
    }
}
