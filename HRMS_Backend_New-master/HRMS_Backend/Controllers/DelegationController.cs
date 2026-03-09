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

            // أ) حالة التكليف من أعلى لأسفل (حددنا النوع والـ ID)
            if (dto.TargetEntityType.HasValue && dto.TargetEntityId.HasValue)
            {
                if (dto.TargetEntityType == EntityType.Section)
                {
                    var targetSection = await _context.Sections
                        .Include(s => s.SubDepartment)
                        .FirstOrDefaultAsync(s => s.Id == dto.TargetEntityId.Value);

                    if (targetSection == null) return NotFound("القسم المختار غير موجود.");

                    // التحقق: هل أنت مدير القسم أو مدير الإدارة التابع لها القسم؟
                    bool isAuthorized = targetSection.ManagerEmployeeId == currentEmployeeId ||
                                       (targetSection.SubDepartment != null && targetSection.SubDepartment.ManagerEmployeeId == currentEmployeeId);

                    if (!isAuthorized) return Unauthorized("لا تملك صلاحية التكليف على هذا القسم.");

                    finalEntityId = targetSection.Id;
                    finalEntityTypeStr = EntityType.Section.ToString();
                    originalManagerId = targetSection.ManagerEmployeeId ?? currentEmployeeId;
                }
                else if (dto.TargetEntityType == EntityType.SubDepartment)
                {
                    var targetSubDept = await _context.SubDepartments
                        .FirstOrDefaultAsync(sd => sd.Id == dto.TargetEntityId.Value);

                    if (targetSubDept == null) return NotFound("الإدارة الفرعية غير موجودة.");

                    // التحقق: فقط مدير الإدارة نفسه (أو مدير عام لو عندكم)
                    if (targetSubDept.ManagerEmployeeId != currentEmployeeId)
                        return Unauthorized("لا تملك صلاحية التكليف على هذه الإدارة.");

                    finalEntityId = targetSubDept.Id;
                    finalEntityTypeStr = EntityType.SubDepartment.ToString();
                    originalManagerId = targetSubDept.ManagerEmployeeId ?? currentEmployeeId;
                }
            }
            // ب) حالة التكليف الذاتي (السيستم يكتشف مكانك بروحه)
            else
            {
                var subDept = await _context.SubDepartments.FirstOrDefaultAsync(sd => sd.ManagerEmployeeId == currentEmployeeId);
                var section = await _context.Sections.FirstOrDefaultAsync(s => s.ManagerEmployeeId == currentEmployeeId);

                if (subDept != null)
                {
                    finalEntityId = subDept.Id;
                    finalEntityTypeStr = EntityType.SubDepartment.ToString();
                }
                else if (section != null)
                {
                    finalEntityId = section.Id;
                    finalEntityTypeStr = EntityType.Section.ToString();
                }
                else return BadRequest("أنت لست مسجلاً كمدير حالي لأي كيان لعمل تكليف ذاتي.");
            }

            // ج) إيقاف أي تكليفات نشطة سابقة لنفس القسم/الإدارة
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
            await _context.SaveChangesAsync();

            return Ok(new { message = $"تم التكليف بنجاح على كيان من نوع ({finalEntityTypeStr})." });
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
            var delegation = await _context.ManagerDelegations.FindAsync(id);
            if (delegation == null) return NotFound("التكليف غير موجود.");

            delegation.IsActive = false;
            delegation.EndDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم إلغاء التكليف بنجاح." });
        }
    }
}