using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace HRMS_Backend.Controllers
{
    [Authorize(Roles = "Manager,SuperAdmin")]
    [ApiController]

    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PermissionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------
        // Get all permissions
        // -------------------------------------------------------------
        [HttpGet("all")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _context.Permissions.ToListAsync();
            return Ok(permissions);
        }

        // -------------------------------------------------------------
        // Get permissions for a specific Role
        // -------------------------------------------------------------
        [HttpGet("role/{roleId}")]
        public async Task<IActionResult> GetPermissionsForRole(int roleId)
        {
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            return Ok(permissions);
        }

        // -------------------------------------------------------------
        // Update permissions for a Role (✔️ / ✖️)
        // -------------------------------------------------------------
        [HttpPost("update-role-permissions")]
        public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionRequest request)
        {
            // 1) نمسحو القديم
            var oldPermissions = _context.RolePermissions.Where(rp => rp.RoleId == request.RoleId);
            _context.RolePermissions.RemoveRange(oldPermissions);

            // 2) نضيف الجديد
            foreach (var permId in request.PermissionIds)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = request.RoleId,
                    PermissionId = permId
                });
            }

            await _context.SaveChangesAsync();
            return Ok("Permissions Updated Successfully!");
        }
    }

    // Request Model
    public class UpdateRolePermissionRequest
    {
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; }
    }
}
