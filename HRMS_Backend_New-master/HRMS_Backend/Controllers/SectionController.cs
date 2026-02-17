using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SectionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Section
        [HttpGet]
        public IActionResult GetAll()
        {
            var sections = _context.Sections
                .Include(s => s.SubDepartment)
                .Include(s => s.ManagerEmployee)
                .Include(s => s.PreviousManager) // ✅ أضفنا المدير السابق
                .Select(s => new {
                    s.Id,
                    s.Name,
                    ParentSubDept = s.SubDepartment.Name,
                    ManagerName = s.ManagerEmployee != null ? s.ManagerEmployee.FullName : "لا يوجد",
                    PreviousManagerName = s.PreviousManager != null ? s.PreviousManager.FullName : "لا يوجد"
                }).ToList();

            return Ok(sections);
        }

        // POST: api/Section
        [HttpPost]
        public IActionResult Add(string name, int subDeptId)
        {
            var sec = new Section { Name = name, SubDepartmentId = subDeptId };
            _context.Sections.Add(sec);
            _context.SaveChanges();
            return Ok("تمت إضافة القسم بنجاح");
        }

        // PUT: api/Section/AssignManager
        [HttpPut("AssignManager")]
        public IActionResult AssignManager(int sectionId, int employeeId)
        {
            var sec = _context.Sections
                .Include(s => s.ManagerEmployee)
                .Include(s => s.PreviousManager) // ✅ نحتاج هذا لعرض الاسم بعد التحديث
                .FirstOrDefault(s => s.Id == sectionId);

            if (sec == null)
                return NotFound("القسم غير موجود");

            var emp = _context.Employees
                .Include(e => e.User)
                .ThenInclude(u => u.UserRoles)
                .FirstOrDefault(e => e.Id == employeeId);

            if (emp == null)
                return NotFound("الموظف غير موجود");

            if (emp.User == null)
                return BadRequest("الموظف غير مرتبط بمستخدم في النظام");

            // تحقق من دور مدير قسم
            bool isSectionManager = emp.User.UserRoles
                .Any(ur => ur.RoleId == 5); // 5 = SectionManager

            if (!isSectionManager)
                return BadRequest("لا يمكن تعيين الموظف لأنه لا يملك دور مدير قسم");

            // حفظ المدير السابق
            sec.PreviousManagerId = sec.ManagerEmployeeId;

            // تعيين المدير الجديد
            sec.ManagerEmployeeId = employeeId;

            _context.SaveChanges();

            // ✅ إرجاع المدير الحالي والمدير السابق
            return Ok(new
            {
                message = $"تم تعيين {emp.FullName} مديراً لقسم {sec.Name}",
                currentManager = emp.FullName,
                previousManager = sec.PreviousManager != null ? sec.PreviousManager.FullName : null
            });
        }
    }
}
