using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using HRMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public EmployeeController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ==================== حفظ صورة الموظف ====================
        private string? SaveEmployeePhoto(IFormFile? photo)
        {
            if (photo == null || photo.Length == 0)
                return null;

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "employees");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            photo.CopyTo(stream);

            return $"/employees/{fileName}";
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(sha256.ComputeHash(bytes));
        }

        // ==================== إنشاء موظف مع إرسال إيميل ====================
        [HasPermission("AddEmployee")]
        [HttpPost("create-account")]
        public async Task<IActionResult> CreateEmployeeWithAccount([FromForm] CreateEmployeeAccountDto dto)
        {
            // التحقق من المدخلات الأساسية
            if (string.IsNullOrWhiteSpace(dto.Username))
                return BadRequest("اسم المستخدم مطلوب");

            if (_context.Users.Any(u => u.Username == dto.Username))
                return BadRequest("اسم المستخدم موجود مسبقاً");

            if (string.IsNullOrWhiteSpace(dto.Phone1))
                return BadRequest("رقم الهاتف الأساسي مطلوب");

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // 1. توليد كلمة مرور تلقائية من قبل النظام
                // هنا نستخدم كلمة ثابتة للتجربة أو يمكنك استخدام: "User@" + dto.Phone1.Substring(dto.Phone1.Length - 4)
                string generatedPassword = "User@" + dto.Phone1.Substring(dto.Phone1.Length - 4);

                // 2. إنشاء User
                var user = new User
                {
                    Username = dto.Username,
                    PasswordHash = HashPassword(generatedPassword) // تشفير الكلمة المولدة
                };

                // إضافة الأدوار الافتراضية
                user.UserRoles.Add(new UserRole { RoleId = 6 }); // دور موظف عادي
                if (dto.IsHR) user.UserRoles.Add(new UserRole { RoleId = 2 });
                if (dto.IsSuperAdmin) user.UserRoles.Add(new UserRole { RoleId = 1 });

                _context.Users.Add(user);
                _context.SaveChanges();

                // 3. معالجة الصورة
                string? photoPath = null;
                if (dto.Photo != null && dto.Photo.Length > 0)
                {
                    var uploadsFolder = Path.Combine("wwwroot", "employees");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}_{dto.Photo.FileName}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        dto.Photo.CopyTo(stream);
                    }
                    photoPath = $"employees/{fileName}";
                }

                // 4. توليد رقم الموظف تلقائياً
                var lastEmployee = _context.Employees
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefault();

                int nextNumber = 1;
                if (lastEmployee != null && !string.IsNullOrEmpty(lastEmployee.EmployeeNumber))
                {
                    var lastNumberPart = lastEmployee.EmployeeNumber.Replace("EMP-", "");
                    if (int.TryParse(lastNumberPart, out int lastNum))
                    {
                        nextNumber = lastNum + 1;
                    }
                }

                // 5. إنشاء سجل الموظف
                var employee = new Employee
                {
                    PublicId = Guid.NewGuid(),
                    EmployeeNumber = $"EMP-{nextNumber:D5}",
                    FullName = dto.FullName,
                    Phone1 = dto.Phone1,
                    Phone2 = dto.Phone2,
                    Email = dto.Email,
                    MotherName = dto.MotherName,
                    NationalId = dto.NationalId,
                    BirthDate = dto.BirthDate,
                    Gender = dto.Gender,
                    MaritalStatusId = dto.MaritalStatusId,
                    UserId = user.Id,
                    PhotoPath = photoPath
                };

                _context.Employees.Add(employee);
                _context.SaveChanges();

                // تثبيت التغييرات في قاعدة البيانات
                transaction.Commit();

                // 6. إرسال الإيميل (يحتوي على كلمة المرور المولدة)
                // 6. إرسال الإيميل (يحتوي على كلمة المرور المولدة)
                try
                {
                    var subject = "بيانات دخول نظام الموارد البشرية";
                    var body = $@"
        <div dir='rtl' style='font-family: Arial, sans-serif;'>
            <h2>مرحباً بك، {dto.FullName}</h2>
            <p>تم إنشاء حساب لك بنجاح. بيانات الدخول الخاصة بك هي:</p>
            <p><b>اسم المستخدم:</b> {dto.Username}</p>
            <p><b>كلمة المرور:</b> {generatedPassword}</p>
            <hr>
            <p style='color: red;'>يرجى تغيير كلمة المرور بعد أول تسجيل دخول.</p>
        </div>";

                    await _emailService.SendEmailAsync(dto.Email, subject, body);
                }
                catch (Exception emailEx)
                {
                    // الحساب تم إنشاؤه، لكن الإيميل فشل
                    return Ok(new
                    {
                        employeeId = employee.Id,
                        employeeNumber = employee.EmployeeNumber,
                        fullName = employee.FullName,
                        message = "تم إنشاء الحساب، ولكن تعذر إرسال الإيميل.",
                        error = emailEx.Message
                    });
                }

                // إذا نجح الإيميل
                return Ok(new
                {
                    employeeId = employee.Id,
                    publicId = employee.PublicId,
                    employeeNumber = employee.EmployeeNumber,
                    fullName = employee.FullName,
                    message = "تم إنشاء الحساب وإرسال البيانات للموظف بنجاح"
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                var innerError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest($"حدث خطأ أثناء الحفظ: {innerError}");
            }
        }

        // ==================== جميع الموظفين ====================
        [HasPermission("ViewEmployee")]
        [HttpGet("all")]
        public IActionResult GetAllEmployees()
        {
            var employees = _context.Employees
                .Include(e => e.User)
                .ThenInclude(u => u.UserRoles)
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.EmployeeNumber,
                    rolesIds = e.User != null
                        ? e.User.UserRoles.Select(ur => ur.RoleId).ToList()
                        : new List<int>()
                })
                .ToList();

            return Ok(employees);
        }

        // ==================== جلب المدراء ====================
        [HasPermission("ViewEmployee")]
        [HttpGet("managers")]
        public IActionResult GetManagers()
        {
            var managers = _context.Employees
                .Include(e => e.User)
                .ThenInclude(u => u.UserRoles)
                .Where(e => e.User.UserRoles.Any(ur => ur.RoleId == 3))
                .Select(e => new
                {
                    e.Id,
                    e.FullName
                })
                .ToList();

            return Ok(managers);
        }

        // ==================== My Profile ====================
        [HttpGet("my-profile")]
        [Authorize]
        public IActionResult GetMyProfile()
        {
            var username = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
            var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(employeeIdClaim))
                return Unauthorized("لم يتم التعرف على المستخدم");

            int employeeId = int.Parse(employeeIdClaim);

            var employee = _context.Employees
                .Include(e => e.User)
                .FirstOrDefault(e => e.Id == employeeId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            string? photoUrl = null;
            if (!string.IsNullOrEmpty(employee.PhotoPath))
            {
                photoUrl = $"{Request.Scheme}://{Request.Host}/{employee.PhotoPath.Replace("\\", "/")}";
            }

            var adminData = _context.EmployeeAdministrativeDatas
                .Include(a => a.Department)
                .FirstOrDefault(a => a.EmployeeId == employee.Id);

            var result = new
            {
                employee.Id,
                employee.FullName,
                employee.EmployeeNumber,
                PhotoUrl = photoUrl,
                DepartmentName = adminData?.Department?.Name
            };

            return Ok(result);
        }

        [HasPermission("AssignRole")]
        [HttpPost("add-role")]
        public IActionResult AddRoleToEmployee(int employeeId, int roleId)
        {
            var employee = _context.Employees
                .Include(e => e.User)
                .ThenInclude(u => u.UserRoles)
                .FirstOrDefault(e => e.Id == employeeId);

            if (employee == null) return NotFound("الموظف غير موجود");
            if (employee.User == null) return BadRequest("الموظف لا يملك حساب");

            if (!_context.Roles.Any(r => r.Id == roleId)) return NotFound("الدور غير موجود");

            if (employee.User.UserRoles.Any(ur => ur.RoleId == roleId))
                return BadRequest("هذا الدور مضاف مسبقاً");

            employee.User.UserRoles.Add(new UserRole { UserId = employee.User.Id, RoleId = roleId });
            _context.SaveChanges();

            return Ok(new { message = "تم إضافة الدور بنجاح" });
        }

        // ==================== تعديل Profile ====================
        [HasPermission("EditEmployee")]
        [HttpPut("update-full/{id}")]
        public IActionResult UpdateEmployeeFull(int id, [FromForm] UpdateEmployeeDto dto)
        {
            var employee = _context.Employees.Include(e => e.User).FirstOrDefault(e => e.Id == id);
            if (employee == null) return NotFound("الموظف غير موجود");

            employee.FullName = dto.FullName;
            employee.Phone1 = dto.Phone1;
            employee.Phone2 = dto.Phone2;
            employee.MotherName = dto.MotherName;
            employee.NationalId = dto.NationalId;
            employee.BirthDate = dto.BirthDate;
            employee.Gender = dto.Gender;
            employee.MaritalStatusId = dto.MaritalStatusId;

            if (dto.Photo != null && dto.Photo.Length > 0)
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "employees");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Photo.FileName)}";
                var filePath = Path.Combine(folderPath, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                dto.Photo.CopyTo(stream);
                employee.PhotoPath = $"employees/{fileName}";
            }

            _context.SaveChanges();
            return Ok("تم التحديث بنجاح");
        }

        // ==================== عرض تفاصيل الموظف ====================
        [HasPermission("ViewEmployee")]
        [HttpGet("details/{publicId}")]
        public IActionResult GetEmployeeFullDetailsByPublicId(Guid publicId)
        {
            var employee = _context.Employees
                .Include(e => e.User).ThenInclude(u => u.UserRoles)
                .Include(e => e.MaritalStatus)
                .FirstOrDefault(e => e.PublicId == publicId);

            if (employee == null) return NotFound("الموظف غير موجود");

            string? photoUrl = !string.IsNullOrEmpty(employee.PhotoPath)
                ? $"{Request.Scheme}://{Request.Host}/{employee.PhotoPath.Replace("\\", "/")}" : null;

            var adminData = _context.EmployeeAdministrativeDatas.Include(a => a.Department)
                .FirstOrDefault(a => a.EmployeeId == employee.Id);

            return Ok(new
            {
                employee.Id,
                employee.EmployeeNumber,
                employee.FullName,
                employee.Phone1,
                employee.NationalId,
                employee.BirthDate,
                employee.Gender,
                MaritalStatus = employee.MaritalStatus?.Name,
                PhotoUrl = photoUrl,
                Username = employee.User?.Username,
                Roles = employee.User?.UserRoles.Select(r => r.RoleId).ToList(),
                DepartmentName = adminData?.Department?.Name,
                employee.PublicId
            });
        }

        // ==================== تغيير كلمة المرور ====================
        [HttpPost("change-password")]
        [Authorize]
        public IActionResult ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (dto.NewPassword.Length < 6) return BadRequest("كلمة السر ضعيفة");
            if (dto.NewPassword != dto.ConfirmPassword) return BadRequest("لا يوجد تطابق");

            var username = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null || user.PasswordHash != HashPassword(dto.CurrentPassword))
                return BadRequest("بيانات المرور الحالية خاطئة");

            user.PasswordHash = HashPassword(dto.NewPassword);
            _context.SaveChanges();

            return Ok("تم تغيير كلمة المرور بنجاح");
        }

        [AllowAnonymous]
        [HttpGet("test-smtp")]
        public async Task<IActionResult> TestSmtp()
        {
            try
            {
                await _emailService.SendEmailAsync(
                    "hanadyad87@example.com", // حطي إيميلك هنا
                    "SMTP Test",
                    "<h2>إذا وصلتك هذه الرسالة فالإرسال شغال ✅</h2>"
                );

                return Ok("تم إرسال الإيميل بنجاح ✅");
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "فشل إرسال الإيميل ❌",
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message
                });
            }
        }
    }
}