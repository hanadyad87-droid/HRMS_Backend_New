using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveTypeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var data = _context.LeaveTypes.ToList();
            return Ok(data);
        }

        [HttpPost]
        public IActionResult Create(LeaveTypes leaveType)
        {
            _context.LeaveTypes.Add(leaveType);
            _context.SaveChanges();
            return Ok(leaveType);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, LeaveTypes model)
        {
            var leaveType = _context.LeaveTypes.Find(id);
            if (leaveType == null) return NotFound();

            leaveType.اسم_الاجازة = model.اسم_الاجازة;
            leaveType.مخصومة_من_الرصيد = model.مخصومة_من_الرصيد;
            leaveType.تحتاج_نموذج = model.تحتاج_نموذج;
            leaveType.مفعلة = model.مفعلة;

            _context.SaveChanges();
            return Ok(leaveType);
        }

        [HttpDelete("{id}")]
        public IActionResult Disable(int id)
        {
            var leaveType = _context.LeaveTypes.Find(id);
            if (leaveType == null) return NotFound();

            leaveType.مفعلة = false;
            _context.SaveChanges();
            return Ok();
        }
    }
}
