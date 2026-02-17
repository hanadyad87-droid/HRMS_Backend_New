using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
  
    [ApiController]
    public class MaritalStatusController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MaritalStatusController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.MaritalStatuses.ToList());
        }

        [HttpPost]
        public IActionResult Create(MaritalStatus status)
        {
            _context.MaritalStatuses.Add(status);
            _context.SaveChanges();
            return Ok("Created");
        }
    
}
}
