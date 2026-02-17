using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]

    [ApiController]
    public class BankController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BankController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.Banks.ToList());
        }

        [HttpPost]
        public IActionResult Create(Bank bank)
        {
            _context.Banks.Add(bank);
            _context.SaveChanges();
            return Ok("Bank created");
        }
    }
}
