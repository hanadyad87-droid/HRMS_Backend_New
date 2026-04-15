using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PermissionsManagementController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PermissionsManagementController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 🔹 دالة مساعدة لجلب userId من التوكن
    private int? GetUserIdFromToken()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            return userId;

        return null;
    }

    [HttpPost("set-exception")]
    [HasPermission("AssignRole")]
    public IActionResult SetUserException(Guid publicId, int permissionId, bool isAllowed)
    {
        var employee = _context.Employees
            .Include(e => e.User)
            .FirstOrDefault(e => e.PublicId == publicId);

        if (employee == null)
            return NotFound("الموظف غير موجود");

        if (employee.User == null)
            return BadRequest("الموظف لا يملك حساب");

        int userId = employee.User.Id;

        var existing = _context.UserPermissions
            .FirstOrDefault(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existing != null)
            existing.IsAllowed = isAllowed;
        else
            _context.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                IsAllowed = isAllowed
            });

        _context.SaveChanges();
        return Ok("تم الحفظ");
    }

    // ================== USER SUMMARY ==================
    [HttpGet("my-summary")]
    public IActionResult GetMyPermissionsSummary()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("لم يتم التعرف على المستخدم");

        var roles = _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.RoleName)
            .ToList();

        var rolePermissions = _context.RolePermissions
            .Where(rp => roles.Contains(rp.Role.RoleName))
            .Select(rp => rp.Permission.PermissionName)
            .ToList();

        var exceptions = _context.UserPermissions
            .Where(up => up.UserId == userId)
            .Select(up => new
            {
                up.Permission.PermissionName,
                up.IsAllowed
            })
            .ToList();

        return Ok(new
        {
            Roles = roles,
            DefaultPermissions = rolePermissions,
            Exceptions = exceptions
        });
    }

    // ================== ADMIN: GET USER SUMMARY BY PUBLIC ID ==================
    [HttpGet("user-summary/{publicId}")]
    [HasPermission("AssignRole")]
    public IActionResult GetUserPermissionsSummary(Guid publicId)
    {
        var employee = _context.Employees
            .Include(e => e.User)
            .FirstOrDefault(e => e.PublicId == publicId);

        if (employee == null)
            return NotFound("الموظف غير موجود");

        if (employee.User == null)
            return BadRequest("الموظف لا يملك حساب");

        int userId = employee.User.Id;

        var roles = _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.RoleName)
            .ToList();

        var rolePermissions = _context.RolePermissions
            .Where(rp => roles.Contains(rp.Role.RoleName))
            .Select(rp => rp.Permission.PermissionName)
            .ToList();

        var exceptions = _context.UserPermissions
            .Where(up => up.UserId == userId)
            .Select(up => new
            {
                up.Permission.PermissionName,
                up.IsAllowed
            })
            .ToList();

        return Ok(new
        {
            EmployeeName = employee.FullName, 
            Roles = roles,
            DefaultPermissions = rolePermissions,
            Exceptions = exceptions
        });
    }
}