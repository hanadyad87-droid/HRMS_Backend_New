using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace HRMS_Backend.DTOs
{
    public class CreateMaintenanceDto
    {
        [Required]
        public string EquipmentName { get; set; }

        [Required]
        public string ProblemDescription { get; set; }

        public IFormFile? ImageFile { get; set; } // الملف اللي حيرفعه الموظف
    }
}