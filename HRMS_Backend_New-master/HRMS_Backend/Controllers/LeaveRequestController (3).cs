using System.Security.Claims;
using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [ApiController]
    [Route("api/leave-requests")]
    public class LeaveRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // CREATE LEAVE REQUEST
        // ==========================================
        [Authorize]
        [HasPermission("SubmitLeave")]
        [HttpPost("create")]
        public IActionResult Create([FromForm] CreateLeaveRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.ToDate < dto.FromDate)
                return BadRequest("التاريخ غير صحيح");

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
            if (employee == null)
                return BadRequest("الموظف غير موجود");

            var admin = _context.EmployeeAdministrativeDatas
                .FirstOrDefault(a => a.EmployeeId == employee.Id);

            if (admin == null)
                return BadRequest("لا توجد بيانات إدارية");

            var holidays = _context.OfficialHolidays.Select(h => h.Date.Date).ToList();

            int totalDays = 0;

            for (var d = dto.FromDate.Date; d <= dto.ToDate.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday)
                    continue;

                if (holidays.Contains(d))
                    continue;

                totalDays++;
            }

            var leaveType = _context.LeaveTypes.Find(dto.LeaveTypeId);

            if (leaveType != null && leaveType.مخصومة_من_الرصيد)
            {
                if (admin.LeaveBalance < totalDays)
                    return BadRequest("الرصيد غير كافي");
            }

            var leave = new LeaveRequest
            {
                EmployeeId = employee.Id,
                LeaveTypeId = dto.LeaveTypeId,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                TotalDays = totalDays,
                Notes = dto.Notes,
                PartialApproval = null,
                FinalApproval = null
            };

            _context.LeaveRequests.Add(leave);
            _context.SaveChanges();

            return Ok("تم إنشاء الطلب");
        }

        // ==========================================
        // MANAGER PENDING REQUESTS
        // ==========================================
        [Authorize]
        [HasPermission("ApproveLeave")]
        [HttpGet("manager/pending")]
        public IActionResult GetPending()
        {
            var empId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");

            var requests = _context.LeaveRequests
                .Include(l => l.Employee)!.ThenInclude(e => e!.AdministrativeData)
                .Include(l => l.LeaveType)
                .Where(l => l.FinalApproval == null)
                .ToList();

            return Ok(requests);
        }

        // ==========================================
        // MANAGER DECISION (FIXED)
        // ==========================================
        [Authorize]
        [HasPermission("ApproveLeave")]
        [HttpPost("{id}/manager-decision")]
        public IActionResult ManagerDecision(int id, bool approve, string? note)
        {
            var leave = _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .FirstOrDefault(l => l.Id == id);

            if (leave == null)
                return NotFound();

            var currentEmpId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");

            var admin = _context.EmployeeAdministrativeDatas
                .FirstOrDefault(a => a.EmployeeId == leave.EmployeeId);

            if (admin == null)
                return BadRequest("لا توجد بيانات الموظف");

            // =====================================================
            // 🔥 FIXED MANAGER CHECK (THIS FIX YOUR BUG)
            // =====================================================

            bool isSectionManager =
                admin.SectionId != null &&
                _context.Sections.Any(s =>
                    s.Id == admin.SectionId &&
                    s.ManagerEmployeeId == currentEmpId);

            bool isSubManager =
                admin.SubDepartmentId != null &&
                _context.SubDepartments.Any(s =>
                    s.Id == admin.SubDepartmentId &&
                    s.ManagerEmployeeId == currentEmpId);

            bool isDeptManager =
                admin.DepartmentId != null &&
                _context.Departments.Any(d =>
                    d.Id == admin.DepartmentId &&
                    d.ManagerEmployeeId == currentEmpId);

            if (!isSectionManager && !isSubManager && !isDeptManager)
            {
                return BadRequest(new
                {
                    message = "غير مسموح",
                    currentEmpId,
                    admin.DepartmentId,
                    admin.SubDepartmentId,
                    admin.SectionId,
                    isDeptManager,
                    isSubManager,
                    isSectionManager
                });
            }

            // ==========================================
            // REJECT
            // ==========================================
            if (!approve)
            {
                leave.سبب_الرفض = note;
                leave.FinalApproval = false;

                _context.SaveChanges();
                return Ok("تم الرفض");
            }

            // ==========================================
            // PARTIAL APPROVAL
            // ==========================================
            if (leave.PartialApproval == null)
            {
                leave.PartialApproval = true;
                leave.PartialNote = note;

                _context.SaveChanges();
                return Ok("تمت الموافقة الجزئية");
            }

            // ==========================================
            // FINAL APPROVAL
            // ==========================================
            if (leave.PartialApproval == true && leave.FinalApproval == null)
            {
                leave.FinalApproval = true;
                leave.FinalNote = note;

                if (leave.LeaveType!.مخصومة_من_الرصيد)
                {
                    admin.LeaveBalance -= leave.TotalDays;
                }

                _context.SaveChanges();
                return Ok("تمت الموافقة النهائية");
            }

            return BadRequest("حالة غير صحيحة");
        }
    }
}