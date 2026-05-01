using System;
using System.ComponentModel.DataAnnotations;

namespace HRMS_Backend.DTOs.ExitPermits
{
    public class CreateExitPermitDto
    {
        [Required]
        public string PermitType { get; set; } // "0"=عاجل, "1"=شخصي, "2"=طبي

        [Required]
        public DateTime PermitDate { get; set; }

        [Required]
        public string FromTime { get; set; } // HH:mm - وقت الخروج

        [Required]
        public string ToTime { get; set; } // HH:mm - وقت العودة

        [Required]
        public string Reason { get; set; }
    }
}