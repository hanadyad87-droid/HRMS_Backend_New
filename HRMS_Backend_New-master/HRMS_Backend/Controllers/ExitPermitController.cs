using HRMS_Backend.Data;
using HRMS_Backend.DTOs.ExitPermits;
using HRMS_Backend.Enums;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExitPermitController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ExitPermitController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. إنشاء طلب إذن خروج (للموظف)
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] CreateExitPermitDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            var request = new ExitPermitRequest
            {
                EmployeeId = employee.Id,
                PermitType = dto.PermitType.ToString().Replace("_", " "),
                PermitDate = dto.PermitDate,
                PermitTime = dto.PermitTime,
                Reason = dto.Reason,
                Status = "قيد_الانتظار"
            };

            _context.ExitPermitRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم إرسال طلب إذن الخروج بنجاح" });
        }

        // 2. عرض الطلبات المعلقة لمدير الشعبة
        [HttpGet("pending-for-manager")]
        public async Task<IActionResult> GetPendingForManager()
        {
            var currentUserId = int.Parse(User.FindFirstValue("UserId"));
            var currentManager = await _context.Employees
                .Include(e => e.AdministrativeData)
                .ThenInclude(a => a.Section)
                .FirstOrDefaultAsync(e => e.UserId == currentUserId);

            if (currentManager == null)
                return NotFound("الموظف غير موجود");

            var requests = await _context.ExitPermitRequests
                .Include(r => r.Employee)
                .ThenInclude(e => e.AdministrativeData)
                .ThenInclude(a => a.Section)
                .Where(r => r.Status == "قيد_الانتظار" &&
                            r.Employee.AdministrativeData.Section.ManagerEmployeeId == currentManager.Id)
                .ToListAsync();

            return Ok(requests);
        }

        // 3. عرض طلباتي الخاصة للموظف
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            var myRequests = await _context.ExitPermitRequests
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return Ok(myRequests);
        }

        // 4. قرار المدير (موافقة أو رفض)
        [HttpPost("manager-decision/{id}")]
        public async Task<IActionResult> ManagerDecision(int id, bool approve)
        {
            var request = await _context.ExitPermitRequests
                .Include(r => r.Employee)
                .ThenInclude(e => e.AdministrativeData)
                .ThenInclude(a => a.Section)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("الطلب غير موجود");

            var currentUserId = int.Parse(User.FindFirstValue("UserId"));
            var currentManager = await _context.Employees
                .Include(e => e.AdministrativeData)
                .ThenInclude(a => a.Section)
                .FirstOrDefaultAsync(e => e.UserId == currentUserId);

            // تحقق من كونه مدير الشعبة المسؤول
            if (request.Employee.AdministrativeData?.Section?.ManagerEmployeeId != currentManager.Id)
                return Unauthorized("لست مدير الشعبة المخول لهذا الموظف");

            request.Status = approve ? "تمت_الموافقة" : "مرفوض";
            if (approve) request.IsHrNotified = true;

            await _context.SaveChangesAsync();
            return Ok(new { Message = approve ? "تمت الموافقة" : "تم الرفض" });
        }

        // 5. عرض الأذونات المعتمدة للإدارة المسؤولة
        [HttpGet("approved-view")]
        public async Task<IActionResult> GetApproved()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var currentEmp = await _context.Employees
                .Include(e => e.AdministrativeData)
                .ThenInclude(a => a.Section)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            // إعدادات المسار
            var setting = await _context.RequestSettings.FirstOrDefaultAsync(s => s.RequestType == RequestType.ExitPermit);

            // صلاحية المستخدم
            var hasPermission = await _context.UserPermissions
                .AnyAsync(p => p.UserId == userId && p.PermissionId == 17 && p.IsAllowed);

            // جلب الإدارة الفرعية المستهدفة من الإعدادات
            var targetSubDept = await _context.SubDepartments
                .FirstOrDefaultAsync(sd => sd.Id == setting.TargetSubDepartmentId);

            // التحقق إذا الموظف هو مدير هذه الإدارة الفرعية
            var isResponsible = targetSubDept != null && targetSubDept.ManagerEmployeeId == currentEmp.Id;

            if (User.IsInRole("SuperAdmin") || (isResponsible && hasPermission))
            {
                var requests = await _context.ExitPermitRequests
                    .Include(r => r.Employee)
                    .Where(r => r.Status == "تمت_الموافقة")
                    .ToListAsync();
                return Ok(requests);
            }

            return Forbid();
        }
        
    }

}