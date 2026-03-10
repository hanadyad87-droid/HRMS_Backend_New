using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [Route("api/company-forms")]
    [ApiController]
    [Authorize]
    public class CompanyFormController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CompanyFormController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. رفع نموذج جديد (للمخولين فقط)
        // ==========================================
        [HttpPost("upload")]
        [HasPermission("ManageForms")]
        public async Task<IActionResult> UploadForm([FromForm] CreateCompanyFormDto dto)
        {
            if (dto.Attachment == null || dto.Attachment.Length == 0)
                return BadRequest("يجب إرفاق ملف النموذج");

            var empIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (empIdClaim == null) return Unauthorized();

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "company-forms");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}_{dto.Attachment.FileName}";
            var fullPath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await dto.Attachment.CopyToAsync(stream);
            }

            var companyForm = new CompanyForm
            {
                Title = dto.Title,
                Description = dto.Description,
                FilePath = $"company-forms/{fileName}",
                UploadedByEmployeeId = int.Parse(empIdClaim),
                UploadedAt = DateTime.Now,
                IsActive = true
            };

            _context.CompanyForms.Add(companyForm);
            await _context.SaveChangesAsync();

            return Ok("تم رفع النموذج بنجاح");
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllForms([FromQuery] string? searchTerm)
        {
            var query = _context.CompanyForms
                .Include(f => f.UploadedBy)
                .Where(f => f.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(f => f.Title.Contains(searchTerm) || (f.Description != null && f.Description.Contains(searchTerm)));
            }

            var forms = await query
                .OrderBy(f => f.Id)
                .Select(f => new CompanyFormResponseDto
                {
                    Id = f.Id,
                    Title = f.Title,
                    Description = f.Description,
                    UploadedAt = f.UploadedAt,
                    UploadedBy = f.UploadedBy != null ? f.UploadedBy.FullName! : "غير معروف",
                    FileUrl = $"{Request.Scheme}://{Request.Host}/{f.FilePath}"
                })
                .ToListAsync();

            return Ok(forms);
        }

        // ==========================================
        // 3. حذف نموذج (إخفاء منطقي)
        // ==========================================
        [HttpDelete("{id}")]
        [HasPermission("ManageForms")]
        public async Task<IActionResult> DeleteForm(int id)
        {
            var form = await _context.CompanyForms.FindAsync(id);
            if (form == null) return NotFound("النموذج غير موجود");

            form.IsActive = false; // لا نحذفه فعلياً لغرض الأرشفة
            await _context.SaveChangesAsync();

            return Ok("تم حذف النموذج بنجاح");
        }
    }
}