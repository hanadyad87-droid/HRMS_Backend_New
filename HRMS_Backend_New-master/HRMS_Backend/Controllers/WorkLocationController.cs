using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
   
    [ApiController]
    public class WorkLocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WorkLocationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.WorkLocations.ToList());
        }

        [HttpPost]
        public IActionResult Create(WorkLocation location)
        {
            _context.WorkLocations.Add(location);
            _context.SaveChanges();
            return Ok("Work Location created");
        }
    }
}
