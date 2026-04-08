using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeEducationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployeeEducationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 Helper
        private int? GetUserId()
        {
            var claim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(claim))
                return null;

            return int.Parse(claim);
        }

        // 🔹 جلب قائمة المؤهلات
        [HttpGet("qualifications")]
        public IActionResult GetQualifications()
        {
            var data = _context.Qualifications
                .Select(q => new
                {
                    q.Id,
                    q.Name
                })
                .ToList();

            return Ok(data);
        }

        // 🔹 إضافة مؤهل
        [Authorize]
        [HasPermission("AddEmployeeEducation")]
        [HttpPost]
        public IActionResult AddEducation([FromForm] CreateEmployeeEducationDto dto)
        {
            var employee = _context.Employees
                .FirstOrDefault(e => e.Id == dto.EmployeeId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            var qualification = _context.Qualifications
                .FirstOrDefault(q => q.Id == dto.QualificationId);

            if (qualification == null)
                return BadRequest("المؤهل غير صحيح");

            string? filePath = null;

            if (dto.File != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var extension = Path.GetExtension(dto.File.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest("مسموح فقط PDF أو صور");

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + extension;
                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    dto.File.CopyTo(stream);
                }

                filePath = "/uploads/" + fileName;
            }

            var education = new EmployeeEducation
            {
                EmployeeId = employee.Id,
                QualificationId = dto.QualificationId,
                Type = dto.Type,
                Institution = dto.Institution,
                FilePath = filePath
            };

            _context.EmployeeEducations.Add(education);
            _context.SaveChanges();

            return Ok(new
            {
                message = "تم إضافة المؤهل العلمي",
                file = filePath
            });
        }

        // 🔹 تعديل مؤهل (اختياري: تقدر تضيف تغيير ملف لو تحب)
        [Authorize]
        [HasPermission("AddEmployeeEducation")]
        [HttpPut("{id}")]
        public IActionResult EditEducation(int id, [FromForm] CreateEmployeeEducationDto dto)
        {
            var education = _context.EmployeeEducations
                .FirstOrDefault(e => e.Id == id);

            if (education == null)
                return NotFound("المؤهل غير موجود");

            education.QualificationId = dto.QualificationId;
            education.Type = dto.Type;
            education.Institution = dto.Institution;

            // 🔹 لو فيه ملف جديد
            if (dto.File != null)
            {
                // حذف القديم
                if (!string.IsNullOrEmpty(education.FilePath))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", education.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var extension = Path.GetExtension(dto.File.FileName).ToLower();
                var fileName = Guid.NewGuid().ToString() + extension;
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    dto.File.CopyTo(stream);
                }

                education.FilePath = "/uploads/" + fileName;
            }

            _context.SaveChanges();

            return Ok("تم التعديل بنجاح");
        }

        // 🔹 حذف مؤهل + حذف الملف
        [Authorize]
        [HasPermission("AddEmployeeEducation")]
        [HttpDelete("{id}")]
        public IActionResult DeleteEducation(int id)
        {
            var education = _context.EmployeeEducations
                .FirstOrDefault(e => e.Id == id);

            if (education == null)
                return NotFound("المؤهل غير موجود");

            // 🔹 حذف الملف من السيرفر
            if (!string.IsNullOrEmpty(education.FilePath))
            {
                var fullPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    education.FilePath.TrimStart('/')
                );

                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _context.EmployeeEducations.Remove(education);
            _context.SaveChanges();

            return Ok("تم حذف المؤهل العلمي بنجاح");
        }

        // 🔹 جلب كل المؤهلات (مع الملف)
        [Authorize]
        [HasPermission("AddEmployeeEducation")]
        [HttpGet("all")]
        public IActionResult GetAllEducations()
        {
            var data = _context.EmployeeEducations
                .Select(e => new
                {
                    e.Id,
                    Employee = e.Employee.FullName,
                    Qualification = e.Qualification.Name,
                    e.Type,
                    e.Institution,
                    e.CreatedAt,
                    File = e.FilePath // 👈 مهم
                })
                .ToList();

            return Ok(data);
        }
    }
}