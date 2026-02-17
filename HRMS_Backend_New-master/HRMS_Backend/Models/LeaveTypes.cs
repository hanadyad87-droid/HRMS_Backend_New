using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_Backend.Models
{
    public class LeaveTypes
    {
        public int Id { get; set; }

        [Column("اسم_الاجازة")]
        public string اسم_الاجازة { get; set; }   // سنوية، مرضية، حج...

        [Column("مخصومة_من_الرصيد")]
        public bool مخصومة_من_الرصيد { get; set; }

        [Column("تحتاج_نموذج")]
        public bool تحتاج_نموذج { get; set; }

        [Column("مفعلة")]
        public bool مفعلة { get; set; } = true; // تفعيل / إيقاف
    }
}
