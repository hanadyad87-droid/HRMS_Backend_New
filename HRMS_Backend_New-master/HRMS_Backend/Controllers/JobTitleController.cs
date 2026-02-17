using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    
    [ApiController]
    public class JobTitleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public JobTitleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/JobTitle
        [HttpGet]
        public IActionResult GetAll()
        {
            var jobTitles = _context.JobTitles.ToList();
            return Ok(jobTitles);
        }

        // POST: api/JobTitle
        [HttpPost]
        public IActionResult Create(JobTitle jobTitle)
        {
            _context.JobTitles.Add(jobTitle);
            _context.SaveChanges();
            return Ok("Job Title created successfully");
        }
    
}
}
