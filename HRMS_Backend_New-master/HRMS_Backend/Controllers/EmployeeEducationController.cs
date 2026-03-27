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

        // 🔹 إضافة مؤهل علمي
        [Authorize]
        [HasPermission("AddEmployeeEducation")]
        [HttpPost]
        public IActionResult AddEducation(CreateEmployeeEducationDto dto)
        {
            var employee = _context.Employees
                .FirstOrDefault(e => e.Id == dto.EmployeeId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            var education = new EmployeeEducation
            {
                EmployeeId = employee.Id,
                Name = dto.Name,
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
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var employee = _context.Employees
                .FirstOrDefault(e => e.UserId == userId);

            if (employee == null)
                return BadRequest("الموظف غير موجود");

            var data = _context.EmployeeEducations
                .Where(e => e.EmployeeId == employee.Id)
                .ToList();

            return Ok(data);
        }

        // 🔹 تعديل مؤهل علمي
        [Authorize]
        [HasPermission("EditEmployeeEducation")]
        [HttpPut("{id}")]
        public IActionResult EditEducation(int id, CreateEmployeeEducationDto dto)
        {
            var education = _context.EmployeeEducations
                .FirstOrDefault(e => e.Id == id);

            if (education == null)
                return NotFound("المؤهل غير موجود");

            education.Name = dto.Name;
            education.Type = dto.Type;
            education.Institution = dto.Institution;

            _context.SaveChanges();

            return Ok("تم تعديل المؤهل العلمي بنجاح");
        }
    }
}