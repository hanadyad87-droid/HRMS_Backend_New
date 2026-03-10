using System.ComponentModel.DataAnnotations;

public class CreateCompanyFormDto
{
    [Required(ErrorMessage = "عنوان النموذج مطلوب")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "الملف مطلوب")]
    public IFormFile Attachment { get; set; } = null!;
}

public class CompanyFormResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
}