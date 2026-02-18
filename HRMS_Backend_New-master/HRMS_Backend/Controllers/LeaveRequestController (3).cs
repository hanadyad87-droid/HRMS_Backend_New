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

        // إنشاء طلب إجازة (Create Leave Request)

        // ==========================================

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



            // منع مدير الإدارة العامة من طلب إجازة لنفسه (تصحيح مقارنة null)

            if (adminData.DepartmentId != null && adminData.SubDepartmentId == null && adminData.SectionId == null)

                return BadRequest("مدير الإدارة العامة لا يمكنه طلب إجازة عبر هذا النموذج");



            // 1. فحص تداخل التواريخ

            var isOverlapping = _context.LeaveRequests.Any(l =>

                l.EmployeeId == employee.Id &&

                l.Status != LeaveStatus.مرفوض &&

                ((dto.FromDate >= l.FromDate && dto.FromDate <= l.ToDate) ||

                 (dto.ToDate >= l.FromDate && dto.ToDate <= l.ToDate)));



            if (isOverlapping)

                return BadRequest("لديك طلب إجازة آخر نشط يتداخل مع هذه التواريخ");



            var leaveType = _context.LeaveTypes.Find(dto.LeaveTypeId);

            if (leaveType == null)

                return BadRequest("نوع الإجازة غير موجود");



            if (leaveType.تحتاج_نموذج && dto.Attachment == null)

                return BadRequest("هذا النوع من الإجازة يتطلب رفع نموذج/تقرير");



            // 2. حساب عدد الأيام الفعلي (المنطق الليبي)

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



            if (totalDays == 0)

                return BadRequest("الفترة المختارة لا تحتوي على أيام عمل فعالة");





            // 4. فحص الرصيد المبدئي

            if (leaveType.مخصومة_من_الرصيد && adminData.LeaveBalance < totalDays)

                return BadRequest($"رصيدك الحالي ({adminData.LeaveBalance}) غير كافٍ لطلب {totalDays} يوم");



            // رفع الملفات

            string? attachmentPath = null;

            if (dto.Attachment != null && dto.Attachment.Length > 0)

            {

                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "attachments");

                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);



                var fileName = $"{Guid.NewGuid()}_{dto.Attachment.FileName}";

                var fullPath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))

                {

                    dto.Attachment.CopyTo(stream);

                }

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

                Status = LeaveStatus.قيد_الانتظار,



            };



            _context.LeaveRequests.Add(leave);

            _context.SaveChanges();



            // تنبيه المدير

            int? nextManagerId = null;

            if (adminData.Section != null) nextManagerId = adminData.Section.ManagerEmployeeId;

            else if (adminData.SubDepartment != null) nextManagerId = adminData.SubDepartment.ManagerEmployeeId;

            else if (adminData.Department != null) nextManagerId = adminData.Department.ManagerEmployeeId;



            if (nextManagerId.HasValue)

            {

                var manager = _context.Employees.Find(nextManagerId);

                if (manager != null)

                {

                    _context.Notifications.Add(new Notification

                    {

                        UserId = manager.UserId,

                        Title = "طلب إجازة جديد",

                        Message = $"قدم {employee.FullName} طلب {leaveType.اسم_الاجازة} لمدة {totalDays} يوم",

                        CreatedAt = DateTime.Now

                    });

                    _context.SaveChanges();

                }

            }



            return Ok("تم إرسال الطلب بنجاح وهو قيد المراجعة الإدارية");

        }



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



        [HttpGet("manager/pending")]

        [Authorize]

        [HasPermission("ApproveLeave")]

        public IActionResult GetPendingRequestsForManager()

        {

            if (!int.TryParse(User.FindFirst("UserId")?.Value, out int currentUserId))

                return Unauthorized();



            var currentManager = _context.Employees.FirstOrDefault(e => e.UserId == currentUserId);

            if (currentManager == null) return Unauthorized();



            var requests = _context.LeaveRequests

                .Include(l => l.Employee)

                .Include(l => l.LeaveType)

                .Include(l => l.Employee.AdministrativeData)

                .Where(l => l.Status == LeaveStatus.قيد_الانتظار &&

                (

                    (l.Employee.AdministrativeData.Section != null && l.Employee.AdministrativeData.Section.ManagerEmployeeId == currentManager.Id) ||

                    (l.Employee.AdministrativeData.SubDepartment != null && l.Employee.AdministrativeData.SubDepartment.ManagerEmployeeId == currentManager.Id) ||

                    (l.Employee.AdministrativeData.Department != null && l.Employee.AdministrativeData.Department.ManagerEmployeeId == currentManager.Id)

                ))

                .Select(l => new LeaveRequestResponseDto

                {

                    Id = l.Id,

                    EmployeeName = l.Employee.FullName ?? "غير معروف",

                    LeaveType = l.LeaveType != null ? l.LeaveType.اسم_الاجازة : "غير محدد",

                    FromDate = l.FromDate,

                    ToDate = l.ToDate,

                    TotalDays = l.TotalDays,

                    Status = l.Status.ToString(),

                    AttachmentPath = l.AttachmentPath,

                    Notes = l.Notes

                }).ToList();



            return Ok(requests);

        }



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



            var currentManager = _context.Employees.FirstOrDefault(e => e.UserId == currentUserId);

            if (currentManager == null) return Unauthorized();



            var empAdmin = _context.EmployeeAdministrativeDatas

                .Include(a => a.Section)

                .Include(a => a.SubDepartment)

                .Include(a => a.Department)

                .FirstOrDefault(a => a.EmployeeId == leave.EmployeeId);



            if (empAdmin == null) return BadRequest("بيانات الموظف الإدارية ناقصة");



            bool isAuthorized = false;

            int? nextManagerId = null;

            bool isFinalStage = false;



            if (empAdmin.Section != null && empAdmin.Section.ManagerEmployeeId == currentManager.Id)

            {

                nextManagerId = empAdmin.SubDepartment?.ManagerEmployeeId;

                isFinalStage = !nextManagerId.HasValue;

                isAuthorized = true;

            }

            else if (empAdmin.SubDepartment != null && empAdmin.SubDepartment.ManagerEmployeeId == currentManager.Id)

            {

                nextManagerId = empAdmin.Department?.ManagerEmployeeId;

                isFinalStage = !nextManagerId.HasValue;

                isAuthorized = true;

            }

            else if (empAdmin.Department != null && empAdmin.Department.ManagerEmployeeId == currentManager.Id)

            {

                isFinalStage = true;

                isAuthorized = true;

            }



            if (!isAuthorized) return Unauthorized("لست المدير المخول حالياً");



            if (!approve)

            {

                leave.Status = LeaveStatus.مرفوض;

                leave.سبب_الرفض = note;

                _context.SaveChanges();

                return Ok("تم رفض الطلب");

            }



            if (isFinalStage)

            {

                if (leave.LeaveType != null && leave.LeaveType.مخصومة_من_الرصيد)

                {

                    if (empAdmin.LeaveBalance < leave.TotalDays)

                        return BadRequest("رصيد الموظف غير كافٍ");



                    empAdmin.LeaveBalance -= leave.TotalDays;

                }

                leave.Status = LeaveStatus.موافقة_نهائية;

                leave.ManagerNote = note;

            }

            else

            {

                leave.Status = LeaveStatus.قيد_الانتظار; // تبقى قيد الانتظار للمدير التالي

            }



            _context.SaveChanges();

            return Ok("تمت المعالجة بنجاح");

        }

    }

}

