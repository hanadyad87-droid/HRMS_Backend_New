using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_Backend.Models
{
    public class LeaveTypes
    {
        public int Id { get; set; }

        [Required]
        [Column("اسم_الاجازة")]
        public string اسم_الاجازة { get; set; } = string.Empty; // سنوية، مرضية، عارضة، حج...

        [Column("مخصومة_من_الرصيد")]
        public bool مخصومة_من_الرصيد { get; set; }

        [Column("تحتاج_نموذج")]
        public bool تحتاج_نموذج { get; set; }

        // --- الإضافات الجديدة لضبط المنطق الليبي ---

        [Column("تتأثر_بالعطلات_الرسمية")]
        public bool IsAffectedByHolidays { get; set; } = true;

        [Column("مفعلة")]
        public bool مفعلة { get; set; } = true;
    }
}