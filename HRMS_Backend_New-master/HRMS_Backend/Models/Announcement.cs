using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_Backend.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }  // رقم تلقائي لكل إعلان

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }  // عنوان الإعلان

        [Required]
        public string Message { get; set; }  // نص الإعلان

        public DateTime CreatedAt { get; set; } = DateTime.Now;  // وقت الإنشاء أوتوماتيكي

        public bool TargetAll { get; set; } = true;  // الإعلان لكل الموظفين

        [ForeignKey("TargetDepartment")]
        public int? TargetDepartmentId { get; set; }  // الإعلان لإدارة معينة (nullable)
        public Department TargetDepartment { get; set; }

        public bool Active { get; set; } = true;  // هل الإعلان مفعل
    }
}
