using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
   
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var departments = _context.Departments
                .Include(d => d.ManagerEmployee)   // المدير الحالي
                .Include(d => d.PreviousManager)   // المدير السابق
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    ManagerId = d.ManagerEmployeeId,
                    ManagerName = d.ManagerEmployee != null ? d.ManagerEmployee.FullName : "لا يوجد مدير حالي",
                    PreviousManagerName = d.PreviousManager != null ? d.PreviousManager.FullName : "لا يوجد مدير سابق"
                })
                .ToList();

            return Ok(departments);
        }

        [HttpPost]
        public IActionResult AddDepartmentData(CreateDepartmentDto dto)
        {
            var department = new Department
            {
                Name = dto.Name
                // ManagerEmployeeId = null تلقائي
            };

            _context.Departments.Add(department);
            _context.SaveChanges();

            return Ok("تم إنشاء الإدارة بنجاح");
        
    }
        // ميثود تعيين مدير للإدارة
        [HttpPut("AssignManager")]
        public IActionResult AssignManager(int deptId, int employeeId)
        {
            var dept = _context.Departments
                .Include(d => d.ManagerEmployee)   // المدير الحالي
                .Include(d => d.PreviousManager)   // المدير السابق
                .FirstOrDefault(d => d.Id == deptId);

            if (dept == null)
                return NotFound("الإدارة غير موجودة");

            var emp = _context.Employees
                .Include(e => e.User)
                .ThenInclude(u => u.UserRoles)
                .FirstOrDefault(e => e.Id == employeeId);

            if (emp == null)
                return NotFound("الموظف غير موجود");

            // 🔥 تحقق من الرول
            bool isDepartmentManager = emp.User.UserRoles
                .Any(ur => ur.RoleId == 3); // 3 = DepartmentManager

            if (!isDepartmentManager)
                return BadRequest("لا يمكن تعيين الموظف لأنه لا يملك دور مدير إدارة");

            // أرشفة المدير الحالي فقط إذا موجود
            if (dept.ManagerEmployeeId != null)
            {
                dept.PreviousManagerId = dept.ManagerEmployeeId;
            }

            // تعيين المدير الجديد
            dept.ManagerEmployeeId = employeeId;

            _context.SaveChanges();

            return Ok(new
            {
                message = $"تم تعيين {emp.FullName} مديراً للإدارة بنجاح",
                currentManager = emp.FullName,
                previousManager = dept.PreviousManager != null ? dept.PreviousManager.FullName : null
            });
        }


    }
}
