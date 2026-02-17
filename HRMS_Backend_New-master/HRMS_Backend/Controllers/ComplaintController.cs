using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Enums;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [ApiController]
    [Route("api/complaints")]
    public class ComplaintController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ComplaintController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // CREATE COMPLAINT
        // =========================
        [Authorize(Roles = "Employee,DepartmentManager,SubDepartmentManager,SectionManager")]
        [HttpPost("create")]
        public IActionResult CreateComplaint([FromForm] CreateComplaintDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int employeeId = int.Parse(User.FindFirst("EmployeeId")?.Value);

            if (dto.DepartmentId == null && !dto.IsForAllDepartments)
                return BadRequest("يجب اختيار إدارة أو جميع الإدارات");

            if (dto.DepartmentId != null && dto.IsForAllDepartments)
                return BadRequest("لا يمكن اختيار الاثنين معاً");

            var complaint = new Complaint
            {
                EmployeeId = employeeId,
                DepartmentId = dto.DepartmentId,
                IsForAllDepartments = dto.IsForAllDepartments,
                Content = dto.Content,
                Status = ComplaintStatus.تحت_المراجعة,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Complaints.Add(complaint);
            _context.SaveChanges();

            // =======================
            // إرسال الإشعارات
            // =======================

            if (dto.IsForAllDepartments)
            {
                var managers = _context.Departments
                    .Where(d => d.ManagerEmployeeId != null)
                    .Select(d => d.ManagerEmployeeId.Value)
                    .ToList();

                foreach (var managerId in managers)
                {
                    var manager = _context.Employees.Find(managerId);
                    if (manager != null)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = manager.UserId,
                            Title = "شكوى عامة جديدة",
                            Message = "تم إرسال شكوى موجهة لجميع الإدارات",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }
            else
            {
                var department = _context.Departments.Find(dto.DepartmentId);

                if (department?.ManagerEmployeeId != null)
                {
                    var manager = _context.Employees.Find(department.ManagerEmployeeId);

                    if (manager != null)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = manager.UserId,
                            Title = "شكوى جديدة",
                            Message = "تم إرسال شكوى لإدارتك",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            _context.SaveChanges();

            return Ok(new { message = "تم إرسال الشكوى بنجاح" });
        }

        // =========================
        // GET MY COMPLAINTS
        // =========================
        [Authorize(Roles = "Employee")]
        [HttpGet("my")]
        public IActionResult MyComplaints()
        {
            int employeeId = int.Parse(User.FindFirst("EmployeeId")?.Value);

            var complaints = _context.Complaints
                .Include(c => c.Department)
                .Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return Ok(complaints);
        }

        // =========================
        // GET COMPLAINTS FOR MANAGER
        // =========================
        [Authorize(Roles = "DepartmentManager,SubDepartmentManager,SectionManager")]
        [HttpGet("all")]
        public IActionResult GetAllComplaintsForManager()
        {
            int managerId = int.Parse(User.FindFirst("EmployeeId")?.Value);

            var complaints = _context.Complaints
                .Include(c => c.Employee)
                    .ThenInclude(e => e.AdministrativeData)
                .Include(c => c.Department)
                .Where(c =>

                    // إدارة معينة → كل التسلسل يشوف
                    (!c.IsForAllDepartments &&
                        (
                            c.Employee.AdministrativeData.Section.ManagerEmployeeId == managerId ||
                            c.Employee.AdministrativeData.SubDepartment.ManagerEmployeeId == managerId ||
                            c.Employee.AdministrativeData.Department.ManagerEmployeeId == managerId
                        )
                    )

                    ||

                    // جميع الإدارات → فقط مدراء الإدارات
                    (c.IsForAllDepartments &&
                        _context.Departments
                            .Any(d => d.ManagerEmployeeId == managerId)
                    )
                )
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return Ok(complaints);
        }

        // =========================
        // MANAGER DECISION
        // =========================
        [Authorize(Roles = "DepartmentManager,SubDepartmentManager,SectionManager")]
        [HttpPost("{id}/manager-decision")]
        public IActionResult ManagerDecision(int id, [FromBody] ManagerDecisionDto dto)
        {
            int managerId = int.Parse(User.FindFirst("EmployeeId")?.Value);

            var complaint = _context.Complaints
                .Include(c => c.Department)
                .FirstOrDefault(c => c.Id == id);

            if (complaint == null)
                return NotFound("الشكوى غير موجودة");

            // ===============================
            // حالة إدارة معينة
            // ===============================
            if (!complaint.IsForAllDepartments)
            {
                // فقط مدير الإدارة يغير الحالة
                if (complaint.Department?.ManagerEmployeeId != managerId)
                    return Forbid("ليس لديك صلاحية تغيير حالة هذه الشكوى");
            }

            // ===============================
            // حالة جميع الإدارات
            // ===============================
            if (complaint.IsForAllDepartments)
            {
                // أول مدير يحجزها
                if (complaint.HandledByManagerId == null)
                {
                    complaint.HandledByManagerId = managerId;
                }
                else if (complaint.HandledByManagerId != managerId)
                {
                    return BadRequest("الشكوى قيد المعالجة من إدارة أخرى");
                }
            }

            complaint.Status = dto.Status;
            complaint.Notes = dto.Notes;
            complaint.UpdatedAt = DateTime.Now;

            // إشعار للموظف
            var employeeUserId = _context.Employees
                .Where(e => e.Id == complaint.EmployeeId)
                .Select(e => e.UserId)
                .FirstOrDefault();

            _context.Notifications.Add(new Notification
            {
                UserId = employeeUserId,
                Title = "تم تحديث شكواك",
                Message = $"حالة الشكوى: {complaint.Status}",
                CreatedAt = DateTime.Now
            });

            _context.SaveChanges();

            return Ok(new { message = "تم تحديث حالة الشكوى بنجاح" });
        }

        // =========================
        // DELETE COMPLAINT
        // =========================
        [Authorize(Roles = "Employee")]
        [HttpDelete("{id}")]
        public IActionResult DeleteComplaint(int id)
        {
            int employeeId = int.Parse(User.FindFirst("EmployeeId")?.Value);

            var complaint = _context.Complaints.Find(id);

            if (complaint == null)
                return NotFound("الشكوى غير موجودة");

            if (complaint.EmployeeId != employeeId)
                return Forbid("لا يمكنك حذف شكوى لا تخصك");

            if (complaint.Status != ComplaintStatus.تحت_المراجعة)
                return BadRequest("لا يمكن حذف الشكوى بعد تغيير حالتها");

            _context.Complaints.Remove(complaint);
            _context.SaveChanges();

            return Ok(new { message = "تم حذف الشكوى بنجاح" });
        }
    }
}
