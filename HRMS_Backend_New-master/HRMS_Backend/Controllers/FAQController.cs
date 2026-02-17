using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FAQController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FAQController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========================
        // 1. جلب كل الأسئلة (نشطة فقط)
        // ========================
        [HttpGet]
        public IActionResult GetAll()
        {
            var faqs = _context.FAQs
                .Where(f => f.IsActive)
                .OrderBy(f => f.Category)
                .ToList();
            return Ok(faqs);
        }

        // ========================
        // 2. إضافة سؤال جديد
        // ========================
        [HttpPost]
        public IActionResult Create([FromBody] FAQDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var faq = new FAQ
            {
                Question = dto.Question,
                Answer = dto.Answer,
                Category = dto.Category,
                IsActive = dto.IsActive
            };

            _context.FAQs.Add(faq);
            _context.SaveChanges();

            return Ok(new { Message = "تم إضافة السؤال بنجاح", Data = faq });
        }

        // ========================
        // 3. تعديل سؤال
        // ========================
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] FAQDTO dto)
        {
            var faq = _context.FAQs.Find(id);
            if (faq == null) return NotFound();

            faq.Question = dto.Question;
            faq.Answer = dto.Answer;
            faq.Category = dto.Category;
            faq.IsActive = dto.IsActive;

            _context.SaveChanges();
            return Ok(new { Message = "تم تعديل السؤال بنجاح", Data = faq });
        }

        // ========================
        // 4. حذف نهائي لسؤال
        // ========================
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var faq = _context.FAQs.Find(id);
            if (faq == null) return NotFound();

            _context.FAQs.Remove(faq);
            _context.SaveChanges();

            return Ok(new { Message = "تم حذف السؤال نهائيًا", Data = faq });
        }
    }
}
