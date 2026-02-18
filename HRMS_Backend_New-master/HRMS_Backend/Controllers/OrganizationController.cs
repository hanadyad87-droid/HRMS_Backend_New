using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrganizationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 1. GET ALL (مطابق تماماً للي بعثتيه) ---

        [HttpGet("Departments")]
        public IActionResult GetDepartments()
        {
            return Ok(_context.Departments
                .Include(d => d.ManagerEmployee).Include(d => d.PreviousManager)
                .Select(d => new {
                    d.Id,
                    d.Name,
                    ManagerId = d.ManagerEmployeeId,
                    ManagerName = d.ManagerEmployee != null ? d.ManagerEmployee.FullName : "لا يوجد مدير حالي",
                    PreviousManagerName = d.PreviousManager != null ? d.PreviousManager.FullName : "لا يوجد مدير سابق"
                }).ToList());
        }

        [HttpGet("SubDepartments")]
        public IActionResult GetSubDepartments()
        {
            return Ok(_context.SubDepartments
                .Include(s => s.Department).Include(s => s.ManagerEmployee).Include(s => s.PreviousManager)
                .Select(s => new {
                    s.Id,
                    s.Name,
                    ParentDepartment = s.Department != null ? s.Department.Name : "لا يوجد",
                    ManagerName = s.ManagerEmployee != null ? s.ManagerEmployee.FullName : "لا يوجد",
                    PreviousManagerName = s.PreviousManager != null ? s.PreviousManager.FullName : "لا يوجد"
                }).ToList());
        }

        [HttpGet("Sections")]
        public IActionResult GetSections()
        {
            return Ok(_context.Sections
                .Include(s => s.SubDepartment).Include(s => s.ManagerEmployee).Include(s => s.PreviousManager)
                .Select(s => new {
                    s.Id,
                    s.Name,
                    ParentSubDept = s.SubDepartment != null ? s.SubDepartment.Name : "لا يوجد",
                    ManagerName = s.ManagerEmployee != null ? s.ManagerEmployee.FullName : "لا يوجد",
                    PreviousManagerName = s.PreviousManager != null ? s.PreviousManager.FullName : "لا يوجد"
                }).ToList());
        }

        // --- 2. POST (إضافة بيانات جديدة) ---

        [HttpPost("AddDepartment")]
        public IActionResult AddDept(CreateDepartmentDto dto)
        {
            _context.Departments.Add(new Department { Name = dto.Name });
            _context.SaveChanges();
            return Ok("تم إنشاء الإدارة بنجاح");
        }

        [HttpPost("AddSubDepartment")]
        public IActionResult AddSub(string name, int departmentId)
        {
            _context.SubDepartments.Add(new subDepartment { Name = name, DepartmentId = departmentId });
            _context.SaveChanges();
            return Ok("تمت إضافة الإدارة الفرعية بنجاح");
        }

        [HttpPost("AddSection")]
        public IActionResult AddSection(string name, int subDeptId)
        {
            _context.Sections.Add(new Section { Name = name, SubDepartmentId = subDeptId });
            _context.SaveChanges();
            return Ok("تمت إضافة القسم بنجاح");
        }

        // --- 3. PUT (Assign Manager) الموحدة ---

        [HttpPut("AssignManager")]
        public IActionResult AssignManager([FromBody] AssignManagerDto dto)
        {
            var emp = _context.Employees
                .Include(e => e.User).ThenInclude(u => u!.UserRoles)
                .FirstOrDefault(e => e.Id == dto.EmployeeId);

            if (emp == null || emp.User == null) return NotFound("الموظف غير موجود");

            string entityName = "";
            string? prevManagerName = null;

            if (dto.Type.ToLower() == "department")
            {
                var dept = _context.Departments.Include(d => d.PreviousManager).FirstOrDefault(d => d.Id == dto.EntityId);
                if (dept == null) return NotFound("الإدارة غير موجودة");
                if (!emp.User.UserRoles.Any(ur => ur.RoleId == 3)) return BadRequest("لا يملك دور مدير إدارة");
                dept.PreviousManagerId = dept.ManagerEmployeeId;
                dept.ManagerEmployeeId = dto.EmployeeId;
                entityName = dept.Name;
                prevManagerName = dept.PreviousManager?.FullName;
            }
            else if (dto.Type.ToLower() == "subdepartment")
            {
                var sub = _context.SubDepartments.Include(s => s.PreviousManager).FirstOrDefault(s => s.Id == dto.EntityId);
                if (sub == null) return NotFound("الإدارة الفرعية غير موجودة");
                if (!emp.User.UserRoles.Any(ur => ur.RoleId == 4)) return BadRequest("لا يملك دور مدير إدارة فرعية");
                sub.PreviousManagerId = sub.ManagerEmployeeId;
                sub.ManagerEmployeeId = dto.EmployeeId;
                entityName = sub.Name;
                prevManagerName = sub.PreviousManager?.FullName;
            }
            else if (dto.Type.ToLower() == "section")
            {
                var sec = _context.Sections.Include(s => s.PreviousManager).FirstOrDefault(s => s.Id == dto.EntityId);
                if (sec == null) return NotFound("القسم غير موجود");
                if (!emp.User.UserRoles.Any(ur => ur.RoleId == 5)) return BadRequest("لا يملك دور مدير قسم");
                sec.PreviousManagerId = sec.ManagerEmployeeId;
                sec.ManagerEmployeeId = dto.EmployeeId;
                entityName = sec.Name;
                prevManagerName = sec.PreviousManager?.FullName;
            }
            else return BadRequest("النوع غير صحيح");

            _context.SaveChanges();
            return Ok(new
            {
                Message = $"تم تعيين {emp.FullName} مديراً لـ {entityName}",
                currentManager = emp.FullName,
                previousManager = prevManagerName
            });
        }
    }
}