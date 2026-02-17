using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using HRMS_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Attributes
{
    public class HasPermissionAttribute : TypeFilterAttribute
    {
        public HasPermissionAttribute(string permission) : base(typeof(PermissionFilter))
        {
            Arguments = new object[] { permission };
        }
    }

    public class PermissionFilter : IAuthorizationFilter
    {
        private readonly string _permission;
        private readonly ApplicationDbContext _db;

        public PermissionFilter(string permission, ApplicationDbContext db)
        {
            _permission = permission;
            _db = db;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 1. جلب UserId من الـ Token
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                context.Result = new ForbidResult();
                return;
            }
            int userId = int.Parse(userIdClaim);

            // 2. فحص الاستثناءات الشخصية (جدول UserPermissions)
            var customPerm = _db.UserPermissions
                .Include(up => up.Permission)
                .FirstOrDefault(up => up.UserId == userId && up.Permission.PermissionName == _permission);

            if (customPerm != null)
            {
                if (customPerm.IsAllowed) return; // مسموح له بالاستثناء -> ادخل
                else { context.Result = new ForbidResult(); return; } // ممنوع بالاستثناء -> اطلع
            }

            // 3. إذا لم يوجد استثناء، نفحص الدور (Role) كالمعتاد
            var userRoleNames = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            var hasRolePermission = _db.RolePermissions
                .Any(rp => userRoleNames.Contains(rp.Role.RoleName) && rp.Permission.PermissionName == _permission);

            if (!hasRolePermission)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}