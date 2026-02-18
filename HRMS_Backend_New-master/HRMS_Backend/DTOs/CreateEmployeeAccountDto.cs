using HRMS_Backend.Models;
using Microsoft.AspNetCore.Http; // لضمان عمل IFormFile
using System.ComponentModel.DataAnnotations;

namespace HRMS_Backend.DTOs
{
    public class CreateEmployeeAccountDto
    {
        // بيانات الحساب
        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        public string Username { get; set; } = string.Empty;

        // كلمة المرور اختيارية هنا لأن النظام سيولدها تلقائياً
        public string? Password { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف الأساسي مطلوب")]
        public string Phone1 { get; set; } = string.Empty;

        public string? Phone2 { get; set; }

        // --- هذا الحقل كان ناقصاً ويسبب الخطأ في الـ Controller ---
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string Email { get; set; } = string.Empty;
        // -------------------------------------------------------

        [Required(ErrorMessage = "اسم الأم مطلوب")]
        public string MotherName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الرقم الوطني مطلوب")]
        public string NationalId { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "الجنس مطلوب")]
        public string Gender { get; set; } = string.Empty;

        public int MaritalStatusId { get; set; }

        public IFormFile? Photo { get; set; }

        public bool IsHR { get; set; }
        public bool IsSuperAdmin { get; set; }
    }
}