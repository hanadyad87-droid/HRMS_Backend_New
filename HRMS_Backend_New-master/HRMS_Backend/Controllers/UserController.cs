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
        // 1. تسجيل مستخدم جديد
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

            var role = _context.Roles.Find(dto.RoleId);
            if (role != null)
                user.UserRoles.Add(new UserRole { User = user, Role = role });

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { user.Id, user.Username });
        }

        // -------------------------
        // 2. تسجيل الدخول (Login)
        // -------------------------
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var hashed = HashPassword(request.Password);

            // جلب المستخدم مع أدواره وصلاحياته
            var user = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefault(u => u.Username == request.Username && u.PasswordHash == hashed);

            if (user == null)
                return Unauthorized("اسم المستخدم أو كلمة المرور خاطئة");

            // جلب بيانات الموظف المرتبطة بهذا المستخدم
            var employee = _context.Employees.FirstOrDefault(e => e.UserId == user.Id);

            // توليد التوكن (هنا تتم عملية حقن صلاحيات المدير المكلف)
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
        // 3. تشفير كلمة المرور
        // -------------------------
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // ---------------------------------------------------------
        // 4. توليد التوكن مع منطق انتقال السلطات (Delegation Logic)
        // ---------------------------------------------------------
        private string GenerateJwtToken(User user, Employee employee)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserId", user.Id.ToString()),
                new Claim("EmployeeId", employee?.Id.ToString() ?? "0"),
                new Claim("FullName", employee?.FullName ?? "")
            };

            // أ. إضافة الأدوار الأصلية (المخزنة في جدول الأدوار)
            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.RoleName));
                claims.Add(new Claim("RoleId", userRole.Role.Id.ToString()));
            }

            // ب. إضافة صلاحيات "المدير المكلف" أوتوماتيكياً
            if (employee != null)
            {
                // نبحث عن أي تكليف نشط لهذا الموظف في جدول التكليفات
                var activeDelegation = _context.ManagerDelegations
                    .AsNoTracking() // لضمان قراءة أحدث حالة من قاعدة البيانات
                    .FirstOrDefault(d => d.ActingManagerId == employee.Id && d.IsActive == true);

                if (activeDelegation != null)
                {
                    // إذا وجدنا تكليفاً، نضيف Role المدير للتوكن فوراً
                    if (activeDelegation.EntityType == "Section")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "SectionManager"));
                    }
                    else if (activeDelegation.EntityType == "SubDepartment")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "SubDepartmentManager"));
                    }

                    // علامات إضافية تفيد في الفلاتر والباكيند
                    claims.Add(new Claim("IsActingManager", "true"));
                    claims.Add(new Claim("DelegatedEntityId", activeDelegation.EntityId.ToString()));
                }
            }

            // إعدادات التوكن السرية
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

    // الـ DTO الخاص بطلب الدخول
    public class LoginRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}