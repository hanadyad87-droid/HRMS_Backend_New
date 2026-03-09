using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_Backend.Models
{
    public class ManagerDelegation
    {
        [Key]
        public int Id { get; set; }

        // الموظف المكلف (الذي سيأخذ الصلاحيات)
        public int ActingManagerId { get; set; }
        [ForeignKey("ActingManagerId")]
        public virtual Employee ActingManager { get; set; } = null!;

        // المدير الأصلي (صاحب الصلاحية)
        public int OriginalManagerId { get; set; }
        [ForeignKey("OriginalManagerId")]
        public virtual Employee OriginalManager { get; set; } = null!;

        // الشخص الذي قام بالعملية (المدير نفسه أو مدير الإدارة)
        public int AssignedById { get; set; }
        [ForeignKey("AssignedById")]
        public virtual Employee AssignedBy { get; set; } = null!;

        // نوع الكيان (Section, subDepartment, Department)
        public string EntityType { get; set; } = null!;
        public int EntityId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}