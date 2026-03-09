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

        // ==========================================
        // 1. إنشاء طلب إجازة
        // ==========================================
        [Authorize]
        [HasPermission("SubmitLeave")]
        [HttpPost("create")]
        public IActionResult Create([FromForm] CreateLeaveRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.ToDate < dto.FromDate) return BadRequest("تاريخ النهاية لا يمكن أن يكون قبل البداية");

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized("UserId غير موجود في التوكن");

            var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
            if (employee == null) return BadRequest("الموظف غير موجود");

            var adminData = _context.EmployeeAdministrativeDatas
                .Include(a => a.Department).Include(a => a.SubDepartment).Include(a => a.Section)
                .FirstOrDefault(a => a.EmployeeId == employee.Id);

            if (adminData == null) return BadRequest("لا توجد بيانات إدارية للموظف");

            if (adminData.DepartmentId != null && adminData.SubDepartmentId == null && adminData.SectionId == null)
                return BadRequest("مدير الإدارة العامة لا يمكنه طلب إجازة عبر هذا النموذج");

            var isOverlapping = _context.LeaveRequests.Any(l =>
                l.EmployeeId == employee.Id && l.Status != LeaveStatus.مرفوض &&
                ((dto.FromDate >= l.FromDate && dto.FromDate <= l.ToDate) ||
                 (dto.ToDate >= l.FromDate && dto.ToDate <= l.ToDate)));

            if (isOverlapping) return BadRequest("لديك طلب إجازة آخر نشط يتداخل مع التواريخ");

            var leaveType = _context.LeaveTypes.Find(dto.LeaveTypeId);
            if (leaveType == null) return BadRequest("نوع الإجازة غير موجود");

            var holidays = _context.OfficialHolidays.Select(h => h.Date.Date).ToList();
            int totalDays = 0;
            for (var date = dto.FromDate.Date; date <= dto.ToDate.Date; date = date.AddDays(1))
            {
                if (leaveType.IsAffectedByHolidays)
                {
                    if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday) continue;
                    if (holidays.Contains(date)) continue;
                }
                totalDays++;
            }

            if (totalDays == 0) return BadRequest("الفترة المختارة لا تحتوي على أيام عمل");

            if (leaveType.مخصومة_من_الرصيد && adminData.LeaveBalance < totalDays)
                return BadRequest($"رصيدك الحالي ({adminData.LeaveBalance}) غير كافٍ");

            string? attachmentPath = null;
            if (dto.Attachment != null)
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "attachments");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                var fileName = $"{Guid.NewGuid()}_{dto.Attachment.FileName}";
                var fullPath = Path.Combine(folderPath, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create)) { dto.Attachment.CopyTo(stream); }
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
            return Ok("تم إرسال الطلب بنجاح");
        }

        // ==========================================
        // 2. عرض طلباتي الشخصية (مع الترتيب التصاعدي)
        // ==========================================
        [Authorize]
        [HasPermission("SubmitLeave")]
        [HttpGet("my-requests")]
        public IActionResult MyLeaveRequests()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
            if (employee == null) return NotFound("الموظف غير موجود");

            var adminData = _context.EmployeeAdministrativeDatas.FirstOrDefault(a => a.EmployeeId == employee.Id);

            var requests = _context.LeaveRequests
                .Include(l => l.LeaveType)
                .Where(l => l.EmployeeId == employee.Id)
                .OrderBy(l => l.Id) // الترتيب التصاعدي لطلباتي الشخصية
                .Select(l => new LeaveRequestResponseDto
                {
                    Id = l.Id,
                    EmployeeName = employee.FullName ?? "غير معروف",
                    LeaveType = l.LeaveType != null ? l.LeaveType.اسم_الاجازة : "غير محدد",
                    FromDate = l.FromDate,
                    ToDate = l.ToDate,
                    TotalDays = l.TotalDays,
                    Status = l.Status.ToString(),
                    Notes = l.Notes,
                    RejectionReason = l.سبب_الرفض,
                    ManagerNote = l.ManagerNote,
                    AttachmentPath = l.AttachmentPath
                }).ToList();

            return Ok(new { Balance = adminData?.LeaveBalance ?? 0, Requests = requests });
        }

        // ==========================================
        // 3. جلب الطلبات المعلقة للمدير/المكلف (مع الترتيب والفلترة)
        // ==========================================
        [HttpGet("manager/pending")]
        [Authorize]
        [HasPermission("ApproveLeave")]
        public IActionResult GetPendingRequestsForManager()
        {
            var empIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (empIdClaim == null || !int.TryParse(empIdClaim, out int currentEmpId))
                return Unauthorized();

            var delegations = _context.ManagerDelegations
                .Where(d => d.ActingManagerId == currentEmpId && d.IsActive)
                .ToList();

            var delSectionIds = delegations.Where(d => d.EntityType == "Section").Select(d => d.EntityId).ToList();
            var delSubDeptIds = delegations.Where(d => d.EntityType == "SubDepartment").Select(d => d.EntityId).ToList();

            var requests = _context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e!.AdministrativeData)
                .Include(l => l.LeaveType)
                .Where(l => l.Status == LeaveStatus.قيد_الانتظار &&
                            l.EmployeeId != currentEmpId && // استبعاد طلب المدير نفسه
                (
                    (l.Employee!.AdministrativeData!.Section != null && l.Employee.AdministrativeData.Section.ManagerEmployeeId == currentEmpId) ||
                    (l.Employee.AdministrativeData.SubDepartment != null && l.Employee.AdministrativeData.SubDepartment.ManagerEmployeeId == currentEmpId) ||
                    (l.Employee.AdministrativeData.Department != null && l.Employee.AdministrativeData.Department.ManagerEmployeeId == currentEmpId) ||
                    (l.Employee.AdministrativeData.SectionId != null && delSectionIds.Contains((int)l.Employee.AdministrativeData.SectionId)) ||
                    (l.Employee.AdministrativeData.SubDepartmentId != null && delSubDeptIds.Contains((int)l.Employee.AdministrativeData.SubDepartmentId))
                ))
                .OrderBy(l => l.Id) // الترتيب التصاعدي لطلبات الموظفين
                .Select(l => new LeaveRequestResponseDto
                {
                    Id = l.Id,
                    EmployeeName = l.Employee!.FullName ?? "غير معروف",
                    LeaveType = l.LeaveType != null ? l.LeaveType.اسم_الاجازة : "غير محدد",
                    FromDate = l.FromDate,
                    ToDate = l.ToDate,
                    TotalDays = l.TotalDays,
                    Status = l.Status.ToString(),
                    AttachmentPath = l.AttachmentPath,
                    Notes = l.Notes ?? ""
                }).ToList();

            return Ok(requests);
        }

        // ==========================================
        // 4. اتخاذ قرار (موافقة/رفض)
        // ==========================================
        [HttpPost("{id}/manager-decision")]
        [HasPermission("ApproveLeave")]
        public IActionResult ManagerDecision(int id, bool approve, string? note)
        {
            var leave = _context.LeaveRequests.Include(l => l.LeaveType).Include(l => l.Employee).FirstOrDefault(l => l.Id == id);
            if (leave == null) return NotFound("الطلب غير موجود");

            var empIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (empIdClaim == null || !int.TryParse(empIdClaim, out int currentEmpId)) return Unauthorized();

            var empAdmin = _context.EmployeeAdministrativeDatas.Include(a => a.Section).Include(a => a.SubDepartment).Include(a => a.Department)
                .FirstOrDefault(a => a.EmployeeId == leave.EmployeeId);
            if (empAdmin == null) return BadRequest("البيانات ناقصة");

            var delegations = _context.ManagerDelegations.Where(d => d.ActingManagerId == currentEmpId && d.IsActive).ToList();
            bool isAuth = false; bool isFinal = false;

            if (empAdmin.SectionId != null && (empAdmin.Section?.ManagerEmployeeId == currentEmpId || delegations.Any(d => d.EntityType == "Section" && d.EntityId == (int)empAdmin.SectionId)))
            {
                isFinal = (empAdmin.SubDepartmentId == null); isAuth = true;
            }
            else if (empAdmin.SubDepartmentId != null && (empAdmin.SubDepartment?.ManagerEmployeeId == currentEmpId || delegations.Any(d => d.EntityType == "SubDepartment" && d.EntityId == (int)empAdmin.SubDepartmentId)))
            {
                isFinal = (empAdmin.DepartmentId == null); isAuth = true;
            }
            else if (empAdmin.DepartmentId != null && empAdmin.Department?.ManagerEmployeeId == currentEmpId)
            {
                isFinal = true; isAuth = true;
            }

            if (!isAuth) return Unauthorized("غير مخول");

            if (!approve) { leave.Status = LeaveStatus.مرفوض; leave.سبب_الرفض = note; }
            else if (isFinal)
            {
                if (leave.LeaveType != null && leave.LeaveType.مخصومة_من_الرصيد)
                {
                    if (empAdmin.LeaveBalance < leave.TotalDays) return BadRequest("الرصيد غير كافٍ");
                    empAdmin.LeaveBalance -= leave.TotalDays;
                }
                leave.Status = LeaveStatus.موافقة_نهائية; leave.ManagerNote = note;
            }
            else { leave.Status = LeaveStatus.قيد_الانتظار; }

            _context.SaveChanges();
            return Ok("تمت المعالجة");
        }
    }
}