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

        // 🔹 جلب قائمة المؤهلات (Dropdown)
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
        public IActionResult AddEducation(CreateEmployeeEducationDto dto)
        {
            var employee = _context.Employees
                .FirstOrDefault(e => e.Id == dto.EmployeeId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            var qualification = _context.Qualifications
                .FirstOrDefault(q => q.Id == dto.QualificationId);

            if (qualification == null)
                return BadRequest("المؤهل غير صحيح");

            var education = new EmployeeEducation
            {
                EmployeeId = employee.Id,
                QualificationId = dto.QualificationId,
                Type = dto.Type,
                Institution = dto.Institution
            };

            _context.EmployeeEducations.Add(education);
            _context.SaveChanges();

            return Ok("تم إضافة المؤهل العلمي");
        }

        // 🔹 عرض مؤهلاتي
        [Authorize]
        [HttpGet("my")]
        public IActionResult MyEducations()
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var employee = _context.Employees
                .FirstOrDefault(e => e.UserId == userId);

            if (employee == null)
                return BadRequest("الموظف غير موجود");

            var data = _context.EmployeeEducations
                .Where(e => e.EmployeeId == employee.Id)
                .Select(e => new
                {
                    e.Id,
                    Qualification = e.Qualification.Name, // 👈 اسم المؤهل
                    e.Type,
                    e.Institution,
                    e.CreatedAt
                })
                .ToList();

            return Ok(data);
        }

        // 🔹 تعديل مؤهل
        [Authorize]
        [HasPermission("EditEmployeeEducation")]
        [HttpPut("{id}")]
        public IActionResult EditEducation(int id, CreateEmployeeEducationDto dto)
        {
            var education = _context.EmployeeEducations
                .FirstOrDefault(e => e.Id == id);

            if (education == null)
                return NotFound("المؤهل غير موجود");

            education.QualificationId = dto.QualificationId;
            education.Type = dto.Type;
            education.Institution = dto.Institution;

            _context.SaveChanges();

            return Ok("تم التعديل بنجاح");
        }
    }
}