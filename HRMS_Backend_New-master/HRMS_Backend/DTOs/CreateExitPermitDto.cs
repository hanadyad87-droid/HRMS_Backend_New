using HRMS_Backend.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace HRMS_Backend.DTOs.ExitPermits
{
    public class CreateExitPermitDto
    {
        [Required]
        public ExitPermitType PermitType { get; set; }

        [Required]
        public DateTime PermitDate { get; set; }

        [Required]
        public TimeSpan PermitTime { get; set; }

        [Required]
        public string Reason { get; set; }
    }
}