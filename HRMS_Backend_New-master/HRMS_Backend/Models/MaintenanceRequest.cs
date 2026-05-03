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

        // الحالات: قيد_الانتظار، قيد_التنفيذ، في_انتظار_المصادقة، تمت_العملية، مرفوض
        public string Status { get; set; } = "قيد_الانتظار";

        // من استلم الطلب (أول حد عنده صلاحية ياخذه)
        public int? ClaimedByEmployeeId { get; set; }
        [ForeignKey("ClaimedByEmployeeId")]
        public Employee? ClaimedBy { get; set; }

        // متى تم الاستلام
        public DateTime? ClaimedAt { get; set; }

        // من كلف بالتنفيذ (ممكن يكون نفس اللي استلم أو موظف تحته)
        public int? AssignedToEmployeeId { get; set; }
        [ForeignKey("AssignedToEmployeeId")]
        public Employee? AssignedTo { get; set; }

        // متى تم التنفيذ
        public DateTime? CompletedAt { get; set; }

        // ملاحظات التنفيذ
        public string? CompletionNotes { get; set; }

        // متى تمت المصادقة من صاحب الطلب
        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}