using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_Backend.Models
{
    public class DataUpdateRequest
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        public string UpdateType { get; set; } // الاسم، الهاتف، الرقم الوطني... إلخ

        [Required]
        public string NewValue { get; set; } // القيمة الجديدة

        [Required]
        public string Reason { get; set; } // سبب التعديل

        public string Status { get; set; } = "قيد_الانتظار"; // قيد_الانتظار، مقبول، مرفوض

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}