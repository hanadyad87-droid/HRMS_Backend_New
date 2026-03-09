using HRMS_Backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace HRMS_Backend.DTOs
{
    public class CreateDelegationDto
    {
        [Required]
        public int ActingManagerId { get; set; } // الموظف اللي بياخد الصلاحية
        public DateTime? EndDate { get; set; }



        public EntityType? TargetEntityType { get; set; }

        // رقم القسم أو الإدارة اللي بنكلفو عليها
        public int? TargetEntityId { get; set; }
    }
}