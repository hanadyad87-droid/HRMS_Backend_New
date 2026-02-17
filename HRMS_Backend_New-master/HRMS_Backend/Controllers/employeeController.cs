using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
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

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
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

        // ==================== إنشاء موظف ====================
        [HasPermission("AddEmployee")]
        [HttpPost("create-account")]
        public IActionResult CreateEmployeeWithAccount([FromForm] CreateEmployeeAccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username))
                return BadRequest("اسم المستخدم مطلوب");

            if (_context.Users.Any(u => u.Username == dto.Username))
                return BadRequest("اسم المستخدم موجود مسبقاً");

            if (dto.Password.Length < 6)
                return BadRequest("كلمة السر يجب أن تكون 6 أحرف على الأقل");

            if (string.IsNullOrWhiteSpace(dto.Phone1))
                return BadRequest("رقم الهاتف الأساسي مطلوب");



            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // إنشاء User
                var user = new User
                {
                    Username = dto.Username,
                    PasswordHash = HashPassword(dto.Password)
                };

                user.UserRoles.Add(new UserRole { RoleId = 6 });
                if (dto.IsHR) user.UserRoles.Add(new UserRole { RoleId = 2 });
                if (dto.IsSuperAdmin) user.UserRoles.Add(new UserRole { RoleId = 1 });

                _context.Users.Add(user);
                _context.SaveChanges();

                // رفع الصورة
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

                // إنشاء Employee
                // إنشاء Employee
                var lastEmployee = _context.Employees
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefault();

                int nextNumber = 1;
                if (lastEmployee != null && !string.IsNullOrEmpty(lastEmployee.EmployeeNumber))
                {
                    var lastNumberPart = lastEmployee.EmployeeNumber.Replace("EMP-", "");
                    nextNumber = int.Parse(lastNumberPart) + 1;
                }

                var employee = new Employee
                {
                    EmployeeNumber = $"EMP-{nextNumber:D5}",  // توليد تلقائي
                    FullName = dto.FullName,
                    Phone1 = dto.Phone1,
                    Phone2 = dto.Phone2,
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

                transaction.Commit();

                return Ok("تم إنشاء الحساب والموظف بنجاح");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest($"حدث خطأ أثناء إنشاء الموظف: {ex.Message}");
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
                    // نضيف قائمة أرقام الرولات
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
                .Where(e => e.User.UserRoles.Any(ur => ur.RoleId == 3)) // Manager
                .Select(e => new
                {
                    e.Id,
                    e.FullName
                })
                .ToList();

            return Ok(managers);
        }




        // ====================  My Profile ====================
        [HttpGet("my-profile")]
        [Authorize]
        public IActionResult GetMyProfile()
        {
            // استخدم claim النوع name أو EmployeeId
            var username = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
            var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(employeeIdClaim))
                return Unauthorized("لم يتم التعرف على المستخدم");

            int employeeId = int.Parse(employeeIdClaim);

            // البحث عن الموظف مباشرة بالـ Id
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

            string? departmentName = adminData?.Department?.Name;

            var result = new
            {
                employee.Id,
                employee.FullName,
                employee.EmployeeNumber,
                PhotoUrl = photoUrl,
                DepartmentName = departmentName
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

            if (employee == null)
                return NotFound("الموظف غير موجود");

            if (employee.User == null)
                return BadRequest("الموظف لا يملك حساب مستخدم");

            // تحقق هل الرول موجودة
            var roleExists = _context.Roles.Any(r => r.Id == roleId);
            if (!roleExists)
                return NotFound("الدور غير موجود");

            // تحقق هل الرول مضافة مسبقاً
            bool alreadyAssigned = employee.User.UserRoles.Any(ur => ur.RoleId == roleId);
            if (alreadyAssigned)
                return BadRequest("هذا الدور مضاف مسبقاً للمستخدم");

            employee.User.UserRoles.Add(new UserRole
            {
                UserId = employee.User.Id,
                RoleId = roleId
            });

            _context.SaveChanges();

            return Ok(new
            {
                message = "تم إنشاء الحساب والموظف بنجاح",
                id = employee.Id,
                employeeNumber = employee.EmployeeNumber,
                fullName = employee.FullName
            });

        }


        // ==================== تعديل Profile ====================
        [HasPermission("EditEmployee")]
        [HttpPut("update-full/{id}")]
        public IActionResult UpdateEmployeeFull(int id, [FromForm] UpdateEmployeeDto dto)
        {
            var employee = _context.Employees
                .Include(e => e.User)
                .FirstOrDefault(e => e.Id == id);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            // ================= تعديل البيانات الأساسية =================
           
            employee.FullName = dto.FullName;
            employee.Phone1 = dto.Phone1;
            employee.Phone2 = dto.Phone2;
            employee.MotherName = dto.MotherName;
            employee.NationalId = dto.NationalId;
            employee.BirthDate = dto.BirthDate;
            employee.Gender = dto.Gender;
            employee.MaritalStatusId = dto.MaritalStatusId;

            // ================= تعديل الصورة =================
            if (dto.Photo != null && dto.Photo.Length > 0)
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "employees");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Photo.FileName)}";
                var filePath = Path.Combine(folderPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                dto.Photo.CopyTo(stream);

                employee.PhotoPath = $"employees/{fileName}";
            }

            _context.SaveChanges();

            return Ok("تم تحديث بيانات الموظف بالكامل بنجاح");
        }


        // ==================== عرض My Profile ====================

        [HasPermission("ViewEmployee")]
        [HttpGet("details/{publicId}")]
        public IActionResult GetEmployeeFullDetailsByPublicId(Guid publicId)
        {
            var employee = _context.Employees
                .Include(e => e.User)
                    .ThenInclude(u => u.UserRoles)
                .Include(e => e.MaritalStatus)
                .FirstOrDefault(e => e.PublicId == publicId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            string? photoUrl = null;
            if (!string.IsNullOrEmpty(employee.PhotoPath))
                photoUrl = $"{Request.Scheme}://{Request.Host}/{employee.PhotoPath.Replace("\\", "/")}";

            var adminData = _context.EmployeeAdministrativeDatas
                .Include(a => a.Department)
                .FirstOrDefault(a => a.EmployeeId == employee.Id);

            var result = new
            {
                employee.Id,
                employee.EmployeeNumber,
                employee.FullName,
                employee.Phone1,
                employee.Phone2,
                employee.MotherName,
                employee.NationalId,
                employee.BirthDate,
                employee.Gender,
                MaritalStatus = employee.MaritalStatus?.Name,
                PhotoUrl = photoUrl,
                Username = employee.User?.Username,
                Roles = employee.User?.UserRoles.Select(r => r.RoleId).ToList(),
                DepartmentName = adminData?.Department?.Name,
                employee.PublicId
            };

            return Ok(result);
        }

        //----تغيرر رمز الدخول  
        [HttpPost("change-password")]
        [Authorize]
        public IActionResult ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (dto.NewPassword.Length < 6)
                return BadRequest("كلمة السر الجديدة يجب أن تكون 6 أحرف على الأقل");

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("تأكيد كلمة السر غير متطابق");

            var username = User.Claims
                .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                ?.Value;

            if (string.IsNullOrEmpty(username))
                return Unauthorized("لم يتم التعرف على المستخدم");

            var user = _context.Users
                .FirstOrDefault(u => u.Username == username);

            if (user == null)
                return NotFound("المستخدم غير موجود");

            var currentHashed = HashPassword(dto.CurrentPassword);

            if (user.PasswordHash != currentHashed)
                return BadRequest("كلمة المرور الحالية غير صحيحة");

            user.PasswordHash = HashPassword(dto.NewPassword);

            _context.SaveChanges();

            return Ok("تم تغيير كلمة المرور بنجاح");
        }



    }
}
