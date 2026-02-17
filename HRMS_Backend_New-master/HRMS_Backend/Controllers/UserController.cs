using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HRMS_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------------
        // Register
        // -------------------------
        [HttpPost("register")]
        public IActionResult Register(RegisterUserDto dto)
        {
            if (_context.Users.Any(u => u.Username == dto.Username))
                return BadRequest("اسم المستخدم موجود مسبقاً");

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = HashPassword(dto.Password)
            };

            // إضافة الدور
            var role = _context.Roles.Find(dto.RoleId);
            if (role != null)
                user.UserRoles.Add(new UserRole { User = user, Role = role });

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { user.Id, user.Username });
        }

        // -------------------------
        // Login
        // -------------------------
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var hashed = HashPassword(request.Password);

            var user = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefault(u => u.Username == request.Username && u.PasswordHash == hashed);

            if (user == null)
                return Unauthorized("اسم المستخدم أو كلمة المرور خاطئة");

            // جلب الموظف المرتبط باليوزر (اختياري)
            var employee = _context.Employees.FirstOrDefault(e => e.UserId == user.Id);

            // توليد التوكن مع الصلاحيات
            var token = GenerateJwtToken(user, employee);

            return Ok(new
            {
                token,
              
                roles = user.UserRoles.Select(ur => new { ur.Role.Id, ur.Role.RoleName }),
                employeeId = employee?.Id,
                employeeName = employee?.FullName
            });
        }

        // -------------------------
        // Hash Password
        // -------------------------
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // -------------------------
        // Generate JWT with Permissions
        // -------------------------
        private string GenerateJwtToken(User user, Employee employee)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim("UserId", user.Id.ToString()),
                new Claim("EmployeeId", employee?.Id.ToString() ?? ""),
                new Claim("FullName", employee?.FullName ?? "")
            };

            // إضافة Roles و Permissions لكل دور
            foreach (var userRole in user.UserRoles)
            {
                var role = userRole.Role;
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
                claims.Add(new Claim("RoleId", role.Id.ToString()));

                // جلب صلاحيات الدور
                // var permissions = role.RolePermissions.Select(rp => rp.Permission.PermissionName).ToList();
                // foreach (var perm in permissions)
                // {
                //    claims.Add(new Claim("permission", perm));
                //}
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForJWTAuthentication1234567890"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // -------------------------
    // Login Request DTO
    // -------------------------
    public class LoginRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
