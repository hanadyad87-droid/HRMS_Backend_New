using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]

    [ApiController]
    public class JobGradeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public JobGradeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.JobGrades.ToList());
        }

        [HttpPost]
        public IActionResult Create(JobGrade grade)
        {
            _context.JobGrades.Add(grade);
            _context.SaveChanges();
            return Ok("Job Grade created");
        }
    }
}
