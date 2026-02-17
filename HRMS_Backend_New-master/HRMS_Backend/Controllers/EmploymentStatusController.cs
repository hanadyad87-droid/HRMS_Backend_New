using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
  
    [ApiController]
    public class EmploymentStatusController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmploymentStatusController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/EmploymentStatus
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.EmploymentStatuses.ToList());
        }

        // POST: api/EmploymentStatus
        [HttpPost]
        public IActionResult Create(EmploymentStatus status)
        {
            _context.EmploymentStatuses.Add(status);
            _context.SaveChanges();
            return Ok("Employment Status created");
        }
    }
}
