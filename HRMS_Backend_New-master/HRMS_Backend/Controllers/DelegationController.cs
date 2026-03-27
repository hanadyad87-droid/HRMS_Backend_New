using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using HRMS_Backend.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DelegationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DelegationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. إنشاء تكليف (ذاتي أو من مدير لإدارة تابعة)
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
            string targetName = ""; // سنخزن هنا اسم القسم أو الإدارة للإشعار

            // أ) حالة التكليف من أعلى لأسفل
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
                    targetName = targetSection.Name; // اسم القسم
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
                    targetName = targetSubDept.Name; // اسم الإدارة
                }
            }
            // ب) حالة التكليف الذاتي
            else
            {
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

            // ج) إيقاف أي تكليفات نشطة سابقة
            var activeDelegations = await _context.ManagerDelegations
                .Where(d => d.EntityId == finalEntityId && d.EntityType == finalEntityTypeStr && d.IsActive)
                .ToListAsync();

            foreach (var d in activeDelegations)
            {
                d.IsActive = false;
                d.EndDate = DateTime.Now;
            }

            // د) حفظ التكليف الجديد
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

            // هـ) إرسال الإشعار (الجزء الجديد)
            var originalManagerName = await _context.Employees
                .Where(e => e.Id == originalManagerId)
                .Select(e => e.FullName)
                .FirstOrDefaultAsync() ?? "المدير الحالي";

            var notification = new Notification
            {
                UserId = dto.ActingManagerId,
                Title = "تكليف بمهام إدارية",
                Message = $"تم تكليفك رسمياً بمهام مدير ({targetName}) بدلاً من السيد/ة ({originalManagerName})، وذلك اعتباراً من اليوم وحتى تاريخ {dto.EndDate:yyyy-MM-dd}. لديك الآن كافة الصلاحيات الإدارية المقررة لهذه الفترة.",
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

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

        // ========================================================
        // 3. إلغاء تكليف نشط
        // ========================================================
        [HttpPost("RevokeDelegation/{id}")]
        public async Task<IActionResult> Revoke(int id)
        {
            var delegation = await _context.ManagerDelegations
                .Include(d => d.OriginalManager) // جلب بيانات المدير لإضافة اسمه في الإشعار
                .FirstOrDefaultAsync(d => d.Id == id);

            if (delegation == null) return NotFound("التكليف غير موجود.");

            delegation.IsActive = false;
            delegation.EndDate = DateTime.Now;

            // --- إضافة إشعار الإلغاء ---
            var revokeNotification = new Notification
            {
                UserId = delegation.ActingManagerId, // الموظف اللي كان مكلف
                Title = "انتهاء فترة التكليف",
                Message = $"تم إنهاء تكليفك بمهام الإدارة من قبل السيد/ة ({delegation.OriginalManager?.FullName ?? "المدير"}). شكراً لجهودك خلال الفترة الماضية.",
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(revokeNotification);
            // -----------------------

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم إلغاء التكليف بنجاح وإرسال إشعار للموظف." });
        }
    }
}