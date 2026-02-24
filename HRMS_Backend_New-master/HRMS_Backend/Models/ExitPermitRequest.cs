using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_Backend.Models // تأكدي إن الـ namespace نفس اللي عندك
{
    public class ExitPermitRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        [MaxLength(50)]
        public string PermitType { get; set; } // نوع الإذن: عاجل، شخصي، طبي

        [Required]
        public DateTime PermitDate { get; set; } // تاريخ الإذن

        [Required]
        public TimeSpan PermitTime { get; set; } // ساعة الإذن (ساعة الخروج)

        [Required]
        public string Reason { get; set; } // سبب الإذن

        // حالة الطلب: قيد_الانتظار، موافقة_المدير، مرفوض
        [MaxLength(50)]
        public string Status { get; set; } = "قيد_الانتظار";

        // هذي باش نعرفوا هل الـ HR شافوه أو لا بعد موافقة المدير
        public bool IsHrNotified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}