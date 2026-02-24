using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_Backend.Models
{
    public class MaintenanceRequest
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        public string EquipmentName { get; set; } // اسم الجهاز (مثلاً: طابعة، لابتوب)

        [Required]
        public string ProblemDescription { get; set; } // وصف المشكلة

        public string? ImagePath { get; set; } // مسار صورة الجهاز (إضافية)

        public string Status { get; set; } = "قيد_الانتظار"; // قيد_الانتظار، تم_الإصلاح، مرفوض

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}