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

        // ==================== حفظ FCM Token للموظف ====================
        [HttpPost("save-fcm-token")]
        public async Task<IActionResult> SaveFcmToken([FromBody] SaveFcmTokenDto dto)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Unauthorized("فشل التحقق من هوية الموظف");
            }

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return NotFound("الموظف غير موجود");
            }

            employee.FcmToken = dto.Token;
            await _context.SaveChangesAsync();

            return Ok("تم حفظ التوكن بنجاح");
        }

        // ==================== إنشاء موظف مع إرسال إيميل ====================
        [HasPermission("AddEmployee")]
        [HttpPost("create-account")]
        public async Task<IActionResult> CreateEmployeeWithAccount([FromForm] CreateEmployeeAccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username))
                return BadRequest("اسم المستخدم مطلوب");

            if (_context.Users.Any(u => u.Username == dto.Username))
                return BadRequest("اسم المستخدم موجود مسبقاً");

            if (string.IsNullOrWhiteSpace(dto.Phone1))
                return BadRequest("رقم الهاتف الأساسي مطلوب");

            if (dto.Phone1.Length < 4)
                return BadRequest("رقم الهاتف يجب أن يكون 4 أرقام على الأقل");

            try
            {
                string generatedPassword = "User@" + dto.Phone1.Substring(dto.Phone1.Length - 4);

                var user = new User
                {
                    Username = dto.Username,
                    PasswordHash = HashPassword(generatedPassword)
                };

                user.UserRoles.Add(new UserRole { RoleId = 6 });
                if (dto.IsHR) user.UserRoles.Add(new UserRole { RoleId = 2 });
                if (dto.IsSuperAdmin) user.UserRoles.Add(new UserRole { RoleId = 1 });

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

                var lastEmployee = _context.Employees
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefault();

                int nextNumber = 1;
                if (lastEmployee != null && !string.IsNullOrEmpty(lastEmployee.EmployeeNumber))
                {
                    var lastNumberPart = lastEmployee.EmployeeNumber.Replace("EMP-", "");
                    if (int.TryParse(lastNumberPart, out int lastNum))
                        nextNumber = lastNum + 1;
                }

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
                    BirthDate = dto.BirthDate == default ? DateTime.UtcNow : dto.BirthDate,
                    Gender = dto.Gender,
                    MaritalStatusId = dto.MaritalStatusId > 0 ? dto.MaritalStatusId : 1,
                    User = user,
                    PhotoPath = photoPath
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

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
                    return Ok(new
                    {
                        employeeId = employee.Id,
                        publicId = employee.PublicId,
                        employeeNumber = employee.EmployeeNumber,
                        fullName = employee.FullName,
                        message = "تم إنشاء الحساب، ولكن تعذر إرسال الإيميل.",
                        error = emailEx.Message
                    });
                }

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
                var innerError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest($"حدث خطأ أثناء الحفظ: {innerError}");
            }
        }

        // ==================== جميع الموظفين ====================
        [HasPermission("ViewEmployee")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllEmployees([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var employees = await _context.Employees
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
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await _context.Employees.CountAsync();

            return Ok(new
            {
                employees,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }

        // ==================== جلب الموظفين حسب قسم المستخدم الحالي ====================
        [HttpGet("by-my-department")]
        [Authorize]
        public async Task<IActionResult> GetEmployeesByMyDepartment()
        {
            var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
            if (!int.TryParse(employeeIdClaim, out int currentEmployeeId))
                return Unauthorized("فشل التحقق من الهوية");

            var currentEmp = await _context.Employees
                .Include(e => e.AdministrativeData)
                .FirstOrDefaultAsync(e => e.Id == currentEmployeeId);

            if (currentEmp == null) return Unauthorized("الموظف غير موجود");

            // تحديد SubDepartment للمستخدم الحالي
            int? userSubDeptId = null;

            // 1. التحقق إذا كان مدير إدارة فرعية
            var managedSubDept = await _context.SubDepartments
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ManagerEmployeeId == currentEmployeeId);
            if (managedSubDept != null)
            {
                userSubDeptId = managedSubDept.Id;
            }
            else
            {
                // 2. التحقق إذا كان مدير قسم
                var managedSection = await _context.Sections
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ManagerEmployeeId == currentEmployeeId);
                if (managedSection != null)
                {
                    userSubDeptId = managedSection.SubDepartmentId;
                }
                else
                {
                    // 3. التحقق إذا كان مدير إدارة عامة (يرجع أول إدارة فرعية تحتها)
                    var managedDept = await _context.Departments
                        .AsNoTracking()
                        .Include(d => d.SubDepartments)
                        .FirstOrDefaultAsync(d => d.ManagerEmployeeId == currentEmployeeId);
                    if (managedDept != null && managedDept.SubDepartments.Any())
                    {
                        userSubDeptId = managedDept.SubDepartments.First().Id;
                    }
                    else
                    {
                        // 4. موظف عادي - من AdministrativeData
                        userSubDeptId = currentEmp.AdministrativeData?.SubDepartmentId;
                    }
                }
            }

            if (userSubDeptId == null)
                return Ok(new { employees = new List<object>() });

            // جلب الموظفين في نفس القسم
            var employees = await _context.EmployeeAdministrativeDatas
                .Include(a => a.Employee)
                .Where(a => a.SubDepartmentId == userSubDeptId && a.EmployeeId != currentEmployeeId)
                .Select(a => new
                {
                    a.Employee.Id,
                    a.Employee.FullName,
                    a.Employee.EmployeeNumber
                })
                .OrderBy(e => e.FullName)
                .ToListAsync();

            return Ok(new { employees });
        }

        // ==================== My Profile ====================
        [HttpGet("my-profile")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            var username = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
            var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(employeeIdClaim))
                return Unauthorized("لم يتم التعرف على المستخدم");

            int employeeId = int.Parse(employeeIdClaim);

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            string? photoUrl = null;
            if (!string.IsNullOrEmpty(employee.PhotoPath))
            {
                photoUrl = $"{Request.Scheme}://{Request.Host}/{employee.PhotoPath.Replace("\\", "/")}";
            }

            var adminData = await _context.EmployeeAdministrativeDatas
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id);

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

        // ==================== Full Profile (بياناتي) ====================
        [HttpGet("full-profile")]
        [Authorize]
        public async Task<IActionResult> GetFullProfile()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Unauthorized("فشل التحقق من هوية الموظف");
            }

            try
            {
                // 1. البيانات الشخصية
                var employee = await _context.Employees
                    .AsNoTracking()
                    .Include(e => e.MaritalStatus)
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == employeeId);

                if (employee == null)
                    return NotFound("الموظف غير موجود");

                // 2. البيانات الوظيفية والإدارية
                var adminData = await _context.EmployeeAdministrativeDatas
                    .AsNoTracking()
                    .Include(a => a.JobTitle)
                    .Include(a => a.Department)
                    .Include(a => a.SubDepartment)
                    .Include(a => a.Section)
                    .Include(a => a.WorkLocation)
                    .Include(a => a.JobGrade)
                    .FirstOrDefaultAsync(a => a.EmployeeId == employeeId);

                // 3. المؤهلات العلمية
                var educations = await _context.EmployeeEducations
                    .AsNoTracking()
                    .Include(e => e.Qualification)
                    .Where(e => e.EmployeeId == employeeId)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                // 4. تجهيز روابط الصور
                string? photoUrl = null;
                if (!string.IsNullOrEmpty(employee.PhotoPath))
                {
                    photoUrl = $"{Request.Scheme}://{Request.Host}/{employee.PhotoPath.Replace("\\", "/")}";
                }

                // 5. بناء الـ Response المنظم
                var result = new
                {
                    personalInfo = new
                    {
                        employee.Id,
                        employee.PublicId,
                        employee.EmployeeNumber,
                        employee.FullName,
                        employee.Email,
                        employee.Phone1,
                        employee.Phone2,
                        employee.MotherName,
                        employee.NationalId,
                        BirthDate = employee.BirthDate.ToString("yyyy-MM-dd"),
                        employee.Gender,
                        MaritalStatus = employee.MaritalStatus?.Name,
                        PhotoUrl = photoUrl
                    },
                    administrativeInfo = new
                    {
                        JobTitle = adminData?.JobTitle?.Name,
                        Department = adminData?.Department?.Name,
                        SubDepartment = adminData?.SubDepartment?.Name,
                        Section = adminData?.Section?.Name,
                        WorkLocation = adminData?.WorkLocation?.Name,
                        JobGrade = adminData?.JobGrade?.Name,
                        JobStatus = adminData?.JobStatus.ToString(),
                        adminData?.LeaveBalance,
                        StartWorkDate = adminData?.StartWorkDate.ToString("yyyy-MM-dd"),
                        ContractStartDate = adminData?.ContractStartDate?.ToString("yyyy-MM-dd"),
                        ContractEndDate = adminData?.ContractEndDate?.ToString("yyyy-MM-dd"),
                        AppointmentDate = adminData?.AppointmentDate?.ToString("yyyy-MM-dd"),
                        // بيانات الانتداب والإعارة
                        adminData?.TransferType,
                        TransferFromOrganization = adminData?.TransferFromOrganization?.Name,
                        TransferStartDate = adminData?.TransferStartDate?.ToString("yyyy-MM-dd"),
                        TransferEndDate = adminData?.TransferEndDate?.ToString("yyyy-MM-dd"),
                        SecondmentToOrganization = adminData?.SecondmentToOrganization?.Name,
                        SecondmentStartDate = adminData?.SecondmentStartDate?.ToString("yyyy-MM-dd"),
                        SecondmentEndDate = adminData?.SecondmentEndDate?.ToString("yyyy-MM-dd")
                    },
                    education = educations.Select(e => new
                    {
                        e.Id,
                        QualificationName = e.Qualification?.Name,
                        e.Qualification?.Level,
                        e.Type,
                        e.Institution,
                        CreatedAt = e.CreatedAt.ToString("yyyy-MM-dd"),
                        FileUrl = !string.IsNullOrEmpty(e.FilePath) 
                            ? $"{Request.Scheme}://{Request.Host}/{e.FilePath.Replace("\\", "/")}" 
                            : null
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "حدث خطأ في الخادم", details = ex.Message });
            }
        }

        // ==================== تعديل Profile + User Roles ====================
        // ==================== تعديل Profile + User Roles ====================
        [HasPermission("EditEmployee")]
        [HttpPut("update-full/{publicId}")]
        public IActionResult UpdateEmployeeFull(Guid publicId, [FromForm] UpdateEmployeeDto dto)
        {
            var employee = _context.Employees
                .Include(e => e.User)
                .ThenInclude(u => u.UserRoles)
                .FirstOrDefault(e => e.PublicId == publicId);
            if (employee == null)
                return NotFound("الموظف غير موجود");

            // ===== تحديث البيانات الأساسية =====
            employee.FullName = dto.FullName;
            employee.Phone1 = dto.Phone1;
            employee.Phone2 = dto.Phone2;
            employee.Email = dto.Email;
            employee.MotherName = dto.MotherName;
            employee.NationalId = dto.NationalId;
            employee.BirthDate = dto.BirthDate ?? employee.BirthDate;
            employee.Gender = dto.Gender;
            employee.MaritalStatusId = dto.MaritalStatusId ?? employee.MaritalStatusId;

            // ===== تحديث صورة الموظف =====
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

            // ===== تحديث بيانات الحساب =====
            if (employee.User != null)
            {
                // تحديث اسم المستخدم
                if (!string.IsNullOrWhiteSpace(dto.Username))
                {
                    if (_context.Users.Any(u => u.Username == dto.Username && u.Id != employee.UserId))
                        return BadRequest("اسم المستخدم موجود مسبقاً");

                    employee.User.Username = dto.Username;
                }

                // تعديل أدوار HR و SuperAdmin
                var hrRole = employee.User.UserRoles.FirstOrDefault(r => r.RoleId == 2);
                if (dto.IsHR && hrRole == null)
                    employee.User.UserRoles.Add(new UserRole { UserId = employee.User.Id, RoleId = 2 });
                else if (!dto.IsHR && hrRole != null)
                    employee.User.UserRoles.Remove(hrRole);

                var adminRole = employee.User.UserRoles.FirstOrDefault(r => r.RoleId == 1);
                if (dto.IsSuperAdmin && adminRole == null)
                    employee.User.UserRoles.Add(new UserRole { UserId = employee.User.Id, RoleId = 1 });
                else if (!dto.IsSuperAdmin && adminRole != null)
                    employee.User.UserRoles.Remove(adminRole);
            }

            _context.SaveChanges();

            return Ok(new
            {
                message = "تم التحديث بنجاح",
                employeeId = employee.Id,
                fullName = employee.FullName,
                email = employee.Email,
                motherName = employee.MotherName,
                username = employee.User?.Username,
                roles = employee.User?.UserRoles.Select(r => r.RoleId).ToList()
            });
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

            // تحقق من أدوار المستخدم
            bool isHR = employee.User?.UserRoles.Any(r => r.RoleId == 2) ?? false;
            bool isSuperAdmin = employee.User?.UserRoles.Any(r => r.RoleId == 1) ?? false;

            return Ok(new
            {
                employee.Id,
                employee.EmployeeNumber,
                employee.FullName,
                employee.Phone1,
                employee.Phone2,
                employee.NationalId,
                employee.BirthDate,
                employee.Gender,
                MaritalStatusId = employee.MaritalStatusId,
                MaritalStatus = employee.MaritalStatus?.Name,
                PhotoUrl = photoUrl,
                Username = employee.User?.Username,
                Email = employee.Email,
                MotherName = employee.MotherName,
                IsHR = isHR,
                IsSuperAdmin = isSuperAdmin,
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

        [HasPermission("AssignRole")]
        [HttpPost("assign-role-and-entity")]
        public IActionResult AssignRoleAndEntity([FromBody] AssignRoleAndEntityDto dto)
        {
            try
            {
                var employee = _context.Employees
                    .Include(e => e.User)
                    .ThenInclude(u => u.UserRoles)
                    .FirstOrDefault(e => e.Id == dto.EmployeeId);

                if (employee == null || employee.User == null)
                    return NotFound("الموظف غير موجود");

                if (!employee.User.UserRoles.Any(r => r.RoleId == dto.RoleId))
                {
                    employee.User.UserRoles.Add(new UserRole
                    {
                        UserId = employee.User.Id,
                        RoleId = dto.RoleId
                    });
                }

                if (dto.Type.ToLower() == "department")
                {
                    var dept = _context.Departments
                        .Include(d => d.PreviousManager)
                        .FirstOrDefault(d => d.Id == dto.EntityId);

                    if (dept == null)
                        return NotFound("الإدارة غير موجودة");

                    dept.PreviousManagerId = dept.ManagerEmployeeId;
                    dept.ManagerEmployeeId = dto.EmployeeId;
                }
                else if (dto.Type.ToLower() == "subdepartment")
                {
                    var sub = _context.SubDepartments
                        .Include(s => s.PreviousManager)
                        .FirstOrDefault(s => s.Id == dto.EntityId);

                    if (sub == null)
                        return NotFound("الإدارة الفرعية غير موجودة");

                    sub.PreviousManagerId = sub.ManagerEmployeeId;
                    sub.ManagerEmployeeId = dto.EmployeeId;
                }
                else if (dto.Type.ToLower() == "section")
                {
                    var sec = _context.Sections
                        .Include(s => s.PreviousManager)
                        .FirstOrDefault(s => s.Id == dto.EntityId);

                    if (sec == null)
                        return NotFound("القسم غير موجود");

                    sec.PreviousManagerId = sec.ManagerEmployeeId;
                    sec.ManagerEmployeeId = dto.EmployeeId;
                }
                else
                {
                    return BadRequest("نوع الكيان غير صحيح");
                }

                _context.SaveChanges();

                return Ok("تم التعيين بنجاح");
            }
            catch (Exception ex)
            {
                return BadRequest("حدث خطأ: " + ex.Message);
            }
        }



    }
}