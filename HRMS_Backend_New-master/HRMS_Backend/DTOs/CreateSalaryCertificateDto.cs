using System.ComponentModel.DataAnnotations;

namespace HRMS_Backend.DTOs
{
    public class CreateSalaryCertificateDto
    {
        [Required]
        public string Purpose { get; set; }
    }
}