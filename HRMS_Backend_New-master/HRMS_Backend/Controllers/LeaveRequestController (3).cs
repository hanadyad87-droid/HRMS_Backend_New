using System.Security.Claims;
using HRMS_Backend.Attributes;
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
    [Route("api/leave-requests")]
    public class LeaveRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // CREATE LEAVE REQUEST
        [Authorize]
        [HasPermission("SubmitLeave")]
        [HttpPost("create")]
        public IActionResult Create([FromForm] CreateLeaveRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.ToDate < dto.FromDate)
                return BadRequest("تاريخ النهاية لا يمكن أن يكون قبل البداية");

            if (!int.TryParse(User.FindFirst("UserId")?.Value, out int userId))
                return Unauthorized("UserId غير موجود في التوكن");

            var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
            if (employee == null)
                return BadRequest("الموظف غير موجود");

            var adminData = _context.EmployeeAdministrativeDatas
                .Include(a => a.Department)
                .Include(a => a.SubDepartment)
                .Include(a => a.Section)
                .FirstOrDefault(a => a.EmployeeId == employee.Id);

            if (adminData == null)
                return BadRequest("لا توجد بيانات إدارية للموظف");

            // منع مدير الإدارة العامة من إرسال إجازة
            if (adminData.DepartmentId != null &&
                adminData.SubDepartmentId == null &&
                adminData.SectionId == null)
            {
                return BadRequest("مدير الإدارة العامة لا يمكنه طلب إجازة");
            }

            var leaveType = _context.LeaveTypes.Find(dto.LeaveTypeId);
            if (leaveType == null)
                return BadRequest("نوع الإجازة غير موجود");

            if (leaveType.تحتاج_نموذج && dto.Attachment == null)
                return BadRequest("هذا النوع من الإجازة يتطلب رفع نموذج");

            // حساب عدد الأيام (مع استثناء الجمعة والسبت والعطلات)
            var holidays = _context.OfficialHolidays.Select(h => h.Date.Date).ToList();
            int totalDays = 0;

            for (var date = dto.FromDate.Date; date <= dto.ToDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday) continue;
                if (holidays.Contains(date)) continue;
                totalDays++;
            }

            string? attachmentPath = null;
            if (dto.Attachment != null && dto.Attachment.Length > 0)
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "attachments");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var fileName = $"{Guid.NewGuid()}_{dto.Attachment.FileName}";
                var fullPath = Path.Combine(folderPath, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                dto.Attachment.CopyTo(stream);

                attachmentPath = $"/attachments/{fileName}";
            }

            var leave = new LeaveRequest
            {
                EmployeeId = employee.Id,
                LeaveTypeId = dto.LeaveTypeId,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                TotalDays = totalDays,
                Notes = dto.Notes,
                AttachmentPath = attachmentPath,
                Status = LeaveStatus.قيد_الانتظار
            };

            _context.LeaveRequests.Add(leave);
            _context.SaveChanges();

            // تحديد المدير التالي (دائمًا لفوق)
            int? nextManagerId = null;

            // موظف عادي → مدير قسم
            if (adminData.SectionId != null)
                nextManagerId = adminData.Section.ManagerEmployeeId;

            // مدير قسم → مدير إدارة فرعية
            else if (adminData.SubDepartmentId != null)
                nextManagerId = adminData.SubDepartment.ManagerEmployeeId;

            // مدير إدارة فرعية → مدير إدارة عامة
            else if (adminData.DepartmentId != null)
                nextManagerId = adminData.Department.ManagerEmployeeId;

            if (nextManagerId != null)
            {
                var manager = _context.Employees.Find(nextManagerId);
                if (manager != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = manager.UserId,
                        Title = "طلب إجازة جديد",
                        Message = $"طلب إجازة من {employee.FullName} لمدة {totalDays} يوم",
                        CreatedAt = DateTime.Now
                    });

                    _context.SaveChanges();
                }
            }

            return Ok("تم إرسال طلب الإجازة حسب التسلسل الإداري الصحيح");
        }



        // =========================
        // MY REQUESTS
        // =========================
        [Authorize]
        [HasPermission("SubmitLeave")]
        [HttpGet("my-requests")]
        public IActionResult MyLeaveRequests()
        {
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out int userId))
                return Unauthorized();

            var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
            if (employee == null) return NotFound("الموظف غير موجود");

            var adminData = _context.EmployeeAdministrativeDatas.FirstOrDefault(a => a.EmployeeId == employee.Id);

            var requests = _context.LeaveRequests
                .Include(l => l.LeaveType)
                .Where(l => l.EmployeeId == employee.Id)
                .Select(l => new
                {
                    l.Id,
                    LeaveTypeName = l.LeaveType.اسم_الاجازة,
                    l.FromDate,
                    l.ToDate,
                    l.TotalDays,
                    Status = l.Status.ToString(),
                    l.ManagerNote,
                    l.AttachmentPath
                }).ToList();

            return Ok(new { Balance = adminData?.LeaveBalance ?? 0, Requests = requests });
        }

        // =========================
        // MANAGER DECISION - FETCH PENDING
        // =========================
        [HttpGet("manager/pending")]
        [Authorize]
        [HasPermission("ApproveLeave")]
        public IActionResult GetPendingRequestsForManager()
        {
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out int currentUserId))
                return Unauthorized();

            var currentManager = _context.Employees
                .Include(e => e.AdministrativeData)
                    .ThenInclude(a => a.Section)
                .Include(e => e.AdministrativeData)
                    .ThenInclude(a => a.SubDepartment)
                .Include(e => e.AdministrativeData)
                    .ThenInclude(a => a.Department)
                .FirstOrDefault(e => e.UserId == currentUserId);

            if (currentManager == null) return Unauthorized();

            var requests = _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Include(l => l.Employee.AdministrativeData)
                    .ThenInclude(a => a.Section)
                .Include(l => l.Employee.AdministrativeData)
                    .ThenInclude(a => a.SubDepartment)
                .Include(l => l.Employee.AdministrativeData)
                    .ThenInclude(a => a.Department)
               .Where(l =>
    l.Status == LeaveStatus.قيد_الانتظار &&
    (
        (l.Employee.AdministrativeData.Section != null &&
         l.Employee.AdministrativeData.Section.ManagerEmployeeId == currentManager.Id)

        ||

        (l.Employee.AdministrativeData.SubDepartment != null &&
         l.Employee.AdministrativeData.SubDepartment.ManagerEmployeeId == currentManager.Id)

        ||

        (l.Employee.AdministrativeData.Department != null &&
         l.Employee.AdministrativeData.Department.ManagerEmployeeId == currentManager.Id)
    )
)

                .Select(l => new
                {
                    l.Id,
                    EmployeeName = l.Employee.FullName,
                    LeaveTypeName = l.LeaveType.اسم_الاجازة,
                    l.FromDate,
                    l.ToDate,
                    l.TotalDays,
                    NeedsAttachment = l.LeaveType.تحتاج_نموذج,
                    l.AttachmentPath,
                    Status = l.Status.ToString()
                }).ToList();

            return Ok(requests);
        }

        // =========================
        // MANAGER DECISION - APPROVE / REJECT
        // =========================
        [HttpPost("{id}/manager-decision")]
        [HasPermission("ApproveLeave")]
        public IActionResult ManagerDecision(int id, bool approve, string? note)
        {
            var leave = _context.LeaveRequests
                .Include(l => l.LeaveType)
                .Include(l => l.Employee)
                .FirstOrDefault(l => l.Id == id);

            if (leave == null) return NotFound("طلب الإجازة غير موجود");

            if (!int.TryParse(User.FindFirst("UserId")?.Value, out int currentUserId))
                return Unauthorized();

            var currentManager = _context.Employees
                .Include(e => e.AdministrativeData)
                .FirstOrDefault(e => e.UserId == currentUserId);

            if (currentManager == null) return Unauthorized();

            var empAdmin = _context.EmployeeAdministrativeDatas
                .Include(a => a.Section)
                .Include(a => a.SubDepartment)
                .Include(a => a.Department)
                .FirstOrDefault(a => a.EmployeeId == leave.EmployeeId);

            if (empAdmin == null) return BadRequest("لا توجد بيانات إدارية للموظف");

            bool isAuthorized = false;
            int? nextManagerId = null;
            bool isFinalStage = false;

            // 👨‍💼 موظف ضمن قسم
            if (empAdmin.Section != null && empAdmin.Section.ManagerEmployeeId == currentManager.Id)
            {
                nextManagerId = empAdmin.SubDepartment?.ManagerEmployeeId;
                isFinalStage = nextManagerId == null;
                isAuthorized = true;
            }
            // 👨‍💼 موظف ضمن إدارة فرعية بدون قسم
            else if (empAdmin.SubDepartment != null && empAdmin.SubDepartment.ManagerEmployeeId == currentManager.Id)
            {
                nextManagerId = empAdmin.Department?.ManagerEmployeeId;
                isFinalStage = nextManagerId == null;
                isAuthorized = true;
            }
            // 👨‍💼 موظف ضمن إدارة عامة فقط
            else if (empAdmin.Department != null && empAdmin.Department.ManagerEmployeeId == currentManager.Id)
            {
                isFinalStage = true;
                isAuthorized = true;
            }

            // ❌ لم يكن المدير موجود في التسلسل
            if (!isAuthorized)
                return Unauthorized("مش مدير الموظف");

            // =========================
            // ❌ رفض الإجازة
            // =========================
            if (!approve)
            {
                leave.Status = LeaveStatus.مرفوض;
                leave.ManagerNote = note;
                _context.SaveChanges();
                return Ok("تم رفض الطلب");
            }

            // =========================
            // ✅ موافقة الإجازة
            // =========================
            if (isFinalStage)
            {
                if (leave.LeaveType.مخصومة_من_الرصيد)
                {
                    if (empAdmin.LeaveBalance < leave.TotalDays)
                        return BadRequest("رصيد الإجازات غير كافي");

                    empAdmin.LeaveBalance -= leave.TotalDays;
                }

                leave.Status = LeaveStatus.موافقة_نهائية;
            }
            else
            {
                leave.Status = LeaveStatus.قيد_الانتظار;

                if (nextManagerId != null)
                {
                    var manager = _context.Employees.Find(nextManagerId);
                    if (manager != null)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = manager.UserId,
                            Title = "طلب إجازة جديد",
                            Message = $"طلب إجازة من {leave.Employee.FullName}",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            _context.SaveChanges();
            return Ok("تم تحديث حالة الطلب حسب التسلسل الصحيح");
        }

    }
}