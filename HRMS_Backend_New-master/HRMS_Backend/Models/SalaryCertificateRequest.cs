using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_Backend.Models
{
    public class SalaryCertificateRequest
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        public string Purpose { get; set; } // الغرض من الشهادة (مثلاً: تقديمها للمصرف)

        public string Status { get; set; } = "قيد_الانتظار"; // قيد_الانتظار، جاهزة، مرفوض

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}