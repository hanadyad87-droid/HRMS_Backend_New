using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
   
    [ApiController]
    public class BankBranchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BankBranchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.BankBranches.ToList());
        }

        [HttpPost]
        public IActionResult Create(BankBranch branch)
        {
            _context.BankBranches.Add(branch);
            _context.SaveChanges();
            return Ok("Bank Branch created");
        }
    }
}
