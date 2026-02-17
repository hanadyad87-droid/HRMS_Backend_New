using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubDepartmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubDepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/SubDepartment
        [HttpGet]
        public IActionResult GetAll()
        {
            var subs = _context.SubDepartments
                .Include(s => s.Department)
                .Include(s => s.ManagerEmployee)
                .Include(s => s.PreviousManager) // ✅ أضفنا المدير السابق
                .Select(s => new {
                    s.Id,
                    s.Name,
                    ParentDepartment = s.Department.Name,
                    ManagerName = s.ManagerEmployee != null ? s.ManagerEmployee.FullName : "لا يوجد",
                    PreviousManagerName = s.PreviousManager != null ? s.PreviousManager.FullName : "لا يوجد"
                }).ToList();
            return Ok(subs);
        }

        // POST: api/SubDepartment
        [HttpPost]
        public IActionResult Add(string name, int departmentId)
        {
            var sub = new subDepartment { Name = name, DepartmentId = departmentId };
            _context.SubDepartments.Add(sub);
            _context.SaveChanges();
            return Ok("تمت إضافة الإدارة الفرعية");
        }

        // PUT: api/SubDepartment/AssignManager
        [HttpPut("AssignManager")]
        public IActionResult AssignManager(int subDeptId, int employeeId)
        {
            var sub = _context.SubDepartments
                .Include(s => s.ManagerEmployee)
                .Include(s => s.PreviousManager) // ✅ نحتاج هذا لعرض الاسم بعد التحديث
                .FirstOrDefault(s => s.Id == subDeptId);

            if (sub == null)
                return NotFound("الإدارة الفرعية غير موجودة");

            var emp = _context.Employees
                .Include(e => e.User)
                .ThenInclude(u => u.UserRoles)
                .FirstOrDefault(e => e.Id == employeeId);

            if (emp == null)
                return NotFound("الموظف غير موجود");

            if (emp.User == null)
                return BadRequest("الموظف غير مرتبط بحساب في النظام");

            // تحقق من دور مدير إدارة فرعية
            bool isSubDeptManager = emp.User.UserRoles
                .Any(ur => ur.RoleId == 4); // 4 = SubDepartmentManager

            if (!isSubDeptManager)
                return BadRequest("لا يمكن تعيين الموظف لأنه لا يملك دور مدير إدارة فرعية");

            // حفظ المدير السابق
            sub.PreviousManagerId = sub.ManagerEmployeeId;

            // تعيين المدير الجديد
            sub.ManagerEmployeeId = employeeId;

            _context.SaveChanges();

            // ✅ إرجاع المدير الحالي والمدير السابق مباشرة للفرونتند
            return Ok(new
            {
                Message = $"تم تعيين {emp.FullName} مديراً للإدارة الفرعية {sub.Name} بنجاح",
                CurrentManager = emp.FullName,
                PreviousManager = sub.PreviousManager != null ? sub.PreviousManager.FullName : null
            });
        }
    }
}
