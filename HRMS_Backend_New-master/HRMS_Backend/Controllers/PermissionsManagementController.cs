using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class PermissionsManagementController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PermissionsManagementController(ApplicationDbContext context) => _context = context;

    [HttpPost("set-exception")]

    [HasPermission("AssignRole")] // فقط السوبر أدمن يقدر يغيرها
    public IActionResult SetUserException(int userId, int permissionId, bool isAllowed)
    {
        var existing = _context.UserPermissions
            .FirstOrDefault(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existing != null)
            existing.IsAllowed = isAllowed;
        else
            _context.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = permissionId, IsAllowed = isAllowed });

        _context.SaveChanges();
        return Ok("تم حفظ التعديل بنجاح");
    }

    [HttpGet("user-summary/{userId}")]
    public IActionResult GetUserPermissionsSummary(int userId)
    {
        // دالة تجلب لك كل صلاحيات الموظف (الموروثة والخاصة) لتعرضها في الواجهة
        var roles = _context.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.Role.RoleName).ToList();
        var rolePermissions = _context.RolePermissions
            .Where(rp => roles.Contains(rp.Role.RoleName))
            .Select(rp => rp.Permission.PermissionName).ToList();

        var exceptions = _context.UserPermissions
            .Where(up => up.UserId == userId)
            .Select(up => new { up.Permission.PermissionName, up.IsAllowed }).ToList();

        return Ok(new { Roles = roles, DefaultPermissions = rolePermissions, Exceptions = exceptions });
    }
}