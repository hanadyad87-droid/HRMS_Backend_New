using HRMS_Backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CompanyForm
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.Now;

    public int UploadedByEmployeeId { get; set; }
    [ForeignKey("UploadedByEmployeeId")]
    public Employee? UploadedBy { get; set; }

    public bool IsActive { get; set; } = true;
}