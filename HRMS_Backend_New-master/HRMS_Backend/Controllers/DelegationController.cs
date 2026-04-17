using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using HRMS_Backend.Enums;
using HRMS_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DelegationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public DelegationController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // ========================================================
        // 1. إنشاء تكليف مع وراثة صلاحيات مؤقتة
        // ========================================================
        [HttpPost("CreateDelegation")]
        public async Task<IActionResult> CreateDelegation([FromForm] CreateDelegationDto dto)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (!int.TryParse(employeeIdClaim, out int currentEmployeeId))
                return Unauthorized("فشل التحقق من الهوية.");

            int finalEntityId = 0;
            string finalEntityTypeStr = "";
            int originalManagerId = currentEmployeeId;
            string targetName = "";

            // تحديد الكيان المستهدف
            if (dto.TargetEntityType.HasValue && dto.TargetEntityId.HasValue)
            {
                if (dto.TargetEntityType == EntityType.Section)
                {
                    var targetSection = await _context.Sections
                        .Include(s => s.SubDepartment)
                        .FirstOrDefaultAsync(s => s.Id == dto.TargetEntityId.Value);

                    if (targetSection == null) return NotFound("القسم المختار غير موجود.");

                    bool isAuthorized = targetSection.ManagerEmployeeId == currentEmployeeId ||
                                        (targetSection.SubDepartment != null && targetSection.SubDepartment.ManagerEmployeeId == currentEmployeeId);
                    if (!isAuthorized) return Unauthorized("لا تملك صلاحية التكليف على هذا القسم.");

                    finalEntityId = targetSection.Id;
                    finalEntityTypeStr = EntityType.Section.ToString();
                    originalManagerId = targetSection.ManagerEmployeeId ?? currentEmployeeId;
                    targetName = targetSection.Name;
                }
                else if (dto.TargetEntityType == EntityType.SubDepartment)
                {
                    var targetSubDept = await _context.SubDepartments
                        .FirstOrDefaultAsync(sd => sd.Id == dto.TargetEntityId.Value);

                    if (targetSubDept == null) return NotFound("الإدارة الفرعية غير موجودة.");
                    if (targetSubDept.ManagerEmployeeId != currentEmployeeId)
                        return Unauthorized("لا تملك صلاحية التكليف على هذه الإدارة.");

                    finalEntityId = targetSubDept.Id;
                    finalEntityTypeStr = EntityType.SubDepartment.ToString();
                    originalManagerId = targetSubDept.ManagerEmployeeId ?? currentEmployeeId;
                    targetName = targetSubDept.Name;
                }
            }
            else
            {
                // تكليف ذاتي
                var subDept = await _context.SubDepartments.FirstOrDefaultAsync(sd => sd.ManagerEmployeeId == currentEmployeeId);
                var section = await _context.Sections.FirstOrDefaultAsync(s => s.ManagerEmployeeId == currentEmployeeId);

                if (subDept != null)
                {
                    finalEntityId = subDept.Id;
                    finalEntityTypeStr = EntityType.SubDepartment.ToString();
                    targetName = subDept.Name;
                }
                else if (section != null)
                {
                    finalEntityId = section.Id;
                    finalEntityTypeStr = EntityType.Section.ToString();
                    targetName = section.Name;
                }
                else return BadRequest("أنت لست مسجلاً كمدير حالي لأي كيان لعمل تكليف ذاتي.");
            }

            // إيقاف أي تكليفات نشطة سابقة
            var activeDelegations = await _context.ManagerDelegations
                .Where(d => d.EntityId == finalEntityId && d.EntityType == finalEntityTypeStr && d.IsActive)
                .ToListAsync();

            foreach (var d in activeDelegations)
            {
                d.IsActive = false;
                d.EndDate = DateTime.Now;

                // حذف صلاحيات مؤقتة مرتبطة بهذه التكليفات القديمة
                var oldTempPerms = await _context.UserPermissions
                    .Where(up => up.DelegationId == d.Id && up.IsTemporary)
                    .ToListAsync();
                _context.UserPermissions.RemoveRange(oldTempPerms);
            }

            // حفظ التكليف الجديد
            var delegation = new ManagerDelegation
            {
                ActingManagerId = dto.ActingManagerId,
                OriginalManagerId = originalManagerId,
                AssignedById = currentEmployeeId,
                EntityType = finalEntityTypeStr,
                EntityId = finalEntityId,
                StartDate = DateTime.Now,
                EndDate = dto.EndDate,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.ManagerDelegations.Add(delegation);
            await _context.SaveChangesAsync();

            // ====================== صلاحيات مؤقتة ======================
            var roleIds = await _context.UserRoles
                .Where(ur => ur.UserId == delegation.OriginalManagerId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var originalPermissions = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            foreach (var permId in originalPermissions)
            {
                _context.UserPermissions.Add(new UserPermission
                {
                    UserId = delegation.ActingManagerId,
                    PermissionId = permId,
                    IsTemporary = true,
                    DelegationId = delegation.Id,

                });
            }

            await _context.SaveChangesAsync();

            // إرسال إشعار
            var originalManagerName = await _context.Employees
                .Where(e => e.Id == originalManagerId)
                .Select(e => e.FullName)
                .FirstOrDefaultAsync() ?? "المدير الحالي";

            await _notifications.NotifyEmployeeAsync(
                dto.ActingManagerId,
                "تكليف بمهام إدارية",
                $"تم تكليفك رسمياً بمهام مدير ({targetName}) بدلاً من السيد/ة ({originalManagerName})، وذلك اعتباراً من اليوم وحتى تاريخ {dto.EndDate:yyyy-MM-dd}. لديك الآن كافة الصلاحيات الإدارية المؤقتة.");

            return Ok(new { message = $"تم التكليف بنجاح على ({targetName}) وإرسال إشعار للموظف المكلف." });
        }

        // ========================================================
        // 2. جلب الموظفين المتاحين للتكليف
        // ========================================================
        [HttpGet("AvailableEmployees")]
        public async Task<IActionResult> GetAvailableEmployees()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            int.TryParse(employeeIdClaim, out int currentEmployeeId);

            var employees = await _context.Employees
                .Where(e => e.Id != currentEmployeeId)
                .Select(e => new { e.Id, e.FullName })
                .OrderBy(e => e.FullName)
                .ToListAsync();

            return Ok(employees);
        }
        [HttpGet("ActiveDelegations")]
        public async Task<IActionResult> GetActiveDelegations()
        {
            var activeDelegations = await _context.ManagerDelegations
     .Include(d => d.ActingManager)
     .Include(d => d.OriginalManager)
     .Where(d => d.IsActive)
     .OrderByDescending(d => d.StartDate)
     .Select(d => new
     {
         d.Id,
         ActingManagerName = d.ActingManager.FullName,
         OriginalManagerName = d.OriginalManager.FullName,
         d.EntityType,
         d.EntityId,
         EntityName = d.EntityType == EntityType.Section.ToString()
             ? _context.Sections.FirstOrDefault(s => s.Id == d.EntityId).Name
             : _context.SubDepartments.FirstOrDefault(sd => sd.Id == d.EntityId).Name,
         d.StartDate,
         d.EndDate
     })
     .ToListAsync();

            return Ok(activeDelegations);
        }
        // ========================================================
        // 3. إلغاء تكليف نشط وحذف صلاحيات مؤقتة
        // ========================================================
        [HttpPost("RevokeDelegation/{id}")]
        public async Task<IActionResult> Revoke(int id)
        {
            var delegation = await _context.ManagerDelegations
                .Include(d => d.OriginalManager)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (delegation == null) return NotFound("التكليف غير موجود.");

            delegation.IsActive = false;
            delegation.EndDate = DateTime.Now;

            // حذف صلاحيات مؤقتة
            var tempPermissions = await _context.UserPermissions
                .Where(up => up.UserId == delegation.ActingManagerId && up.DelegationId == delegation.Id && up.IsTemporary)
                .ToListAsync();

            _context.UserPermissions.RemoveRange(tempPermissions);

            await _context.SaveChangesAsync();

            await _notifications.NotifyEmployeeAsync(
                delegation.ActingManagerId,
                "انتهاء فترة التكليف",
                $"تم إنهاء تكليفك بمهام الإدارة من قبل السيد/ة ({delegation.OriginalManager?.FullName ?? "المدير"}). شكراً لجهودك خلال الفترة الماضية.");
            return Ok(new { message = "تم إلغاء التكليف بنجاح وإرسال إشعار للموظف." });
        }
    }
}