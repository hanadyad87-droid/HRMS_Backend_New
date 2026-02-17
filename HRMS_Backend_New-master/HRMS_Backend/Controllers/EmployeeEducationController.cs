using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        [Authorize]
        [HasPermission("AddOwnEducation")]
        [HttpPost]
        public IActionResult AddEducation(CreateEmployeeEducationDto dto)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);
            var employee = _context.Employees
                .FirstOrDefault(e => e.Id == dto.EmployeeId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            var education = new EmployeeEducation
            {
                EmployeeId = employee.Id,
                Degree = dto.Degree,
                Major = dto.Major,
                University = dto.University,
                GraduationYear = dto.GraduationYear
            };

            _context.EmployeeEducations.Add(education);
            _context.SaveChanges();

            return Ok("تم إضافة المؤهل العلمي");
        }
        [Authorize]
        [HasPermission("AddOwnEducation")]
        [HttpGet("my")]
        public IActionResult MyEducations()
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var employee = _context.Employees
                .FirstOrDefault(e => e.UserId == userId);

            if (employee == null)
                return BadRequest();

            var data = _context.EmployeeEducations
                .Where(e => e.EmployeeId == employee.Id)
                .ToList();

            return Ok(data);
        }

        [Authorize]
        [HasPermission("EditOwnEducation")]
        [HttpPut("{id}")]
       
        public IActionResult EditEducation(int id, CreateEmployeeEducationDto dto)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var employee = _context.Employees
     .FirstOrDefault(e => e.Id == dto.EmployeeId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            var education = _context.EmployeeEducations
                .FirstOrDefault(e => e.Id == id && e.EmployeeId == employee.Id);

            if (education == null)
                return NotFound("المؤهل غير موجود");

            education.Degree = dto.Degree;
            education.Major = dto.Major;
            education.University = dto.University;
            education.GraduationYear = dto.GraduationYear;

            _context.SaveChanges();

            return Ok("تم تعديل المؤهل العلمي بنجاح");
        }
    }
    }
