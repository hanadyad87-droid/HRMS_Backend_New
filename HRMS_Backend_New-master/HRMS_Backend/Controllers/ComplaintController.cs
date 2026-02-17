using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Enums;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HRMS_Backend.Controllers
{
    [ApiController]
    [Route("api/complaints")]
    public class ComplaintController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ComplaintController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // =========================
        // تشفير وفك تشفير النص (IV ثابت) - أبسط طريقة
        // =========================
        private string EncryptContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_config["AESKey"]); // المفتاح من appsettings
            aes.IV = new byte[16]; // IV ثابت
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var bytes = Encoding.UTF8.GetBytes(content);
            var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return Convert.ToBase64String(encrypted);
        }

        private string DecryptContent(string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return encrypted;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_config["AESKey"]);
            aes.IV = new byte[16];
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var bytes = Convert.FromBase64String(encrypted);
            var decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(decrypted);
        }

        // =========================
        // CREATE COMPLAINT
        // =========================
        [AllowAnonymous]
        [HttpPost("create")]
        public IActionResult CreateComplaint([FromForm] CreateComplaintDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int? employeeId = null;

            if (!dto.IsAnonymous)
            {
                var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
                if (string.IsNullOrEmpty(employeeIdClaim))
                    return Unauthorized("يجب تسجيل الدخول أو اختيار شكوى مجهولة");

                employeeId = int.Parse(employeeIdClaim);
            }

            if (dto.DepartmentId == null && !dto.IsForAllDepartments)
                return BadRequest("يجب اختيار إدارة أو جميع الإدارات");

            if (dto.DepartmentId != null && dto.IsForAllDepartments)
                return BadRequest("لا يمكن اختيار الاثنين معاً");

            string? attachmentPath = null;
            if (dto.File != null)
            {
                var allowedExtensions = new[] { ".pdf", ".png", ".jpeg", ".jpg" };
                var ext = Path.GetExtension(dto.File.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                    return BadRequest("نوع الملف غير مسموح");

                var allowedMimeTypes = new[] { "application/pdf", "image/png", "image/jpeg" };
                if (!allowedMimeTypes.Contains(dto.File.ContentType.ToLower()))
                    return BadRequest("نوع الملف غير مسموح");

                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "attachments");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(folder, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                dto.File.CopyTo(stream);

                attachmentPath = $"/attachments/{fileName}";
            }

            var complaint = new Complaint
            {
                EmployeeId = employeeId,
                DepartmentId = dto.DepartmentId,
                IsForAllDepartments = dto.IsForAllDepartments,
                Content = EncryptContent(dto.Content),
                AttachmentPath = attachmentPath,
                Status = ComplaintStatus.تحت_المراجعة,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Complaints.Add(complaint);
            _context.SaveChanges();

            // إشعارات للمديرين
            if (dto.IsForAllDepartments)
            {
                var managers = _context.Departments
                    .Where(d => d.ManagerEmployeeId != null)
                    .Select(d => d.ManagerEmployeeId!.Value)
                    .ToList();

                foreach (var managerId in managers)
                {
                    var manager = _context.Employees.Find(managerId);
                    if (manager != null)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = manager.UserId,
                            Title = "شكوى عامة جديدة",
                            Message = "تم إرسال شكوى موجهة لجميع الإدارات",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }
            else if (dto.DepartmentId != null)
            {
                var department = _context.Departments.Find(dto.DepartmentId);
                if (department?.ManagerEmployeeId != null)
                {
                    var manager = _context.Employees.Find(department.ManagerEmployeeId);
                    if (manager != null)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = manager.UserId,
                            Title = "شكوى جديدة",
                            Message = "تم إرسال شكوى لإدارتك",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            _context.SaveChanges();

            return Ok(new { message = "تم إرسال الشكوى بنجاح" });
        }

        // =========================
        // GET MY COMPLAINTS
        // =========================
        [Authorize(Roles = "Employee")]
        [HttpGet("my")]
        public IActionResult MyComplaints()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim))
                return Unauthorized();

            int employeeId = int.Parse(employeeIdClaim);

            var complaints = _context.Complaints
                .Include(c => c.Department)
                .Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.CreatedAt)
                .AsEnumerable()
                .Select(c => new
                {
                    c.Id,
                    c.DepartmentId,
                    DepartmentName = c.Department?.Name,
                    Content = DecryptContent(c.Content),
                    c.AttachmentPath,
                    c.Status,
                    c.Notes,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToList();

            return Ok(complaints);
        }

        // =========================
        // GET ALL COMPLAINTS FOR MANAGER
        // =========================
        [HttpGet("all")]
        [Authorize(Roles = "DepartmentManager,Employee,HR,SuperAdmin")]
        public async Task<IActionResult> GetAllComplaints()
        {
            // جلب البيانات أولاً من قاعدة البيانات
            var complaints = await _context.Complaints
                .Include(c => c.Employee)
                .Include(c => c.Department)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // بعد ما البيانات في الذاكرة، نفك التشفير ونجهز الـ DTO
            var result = complaints.Select(c => new
            {
                c.Id,
                Content = DecryptContent(c.Content),

                Status = c.Status,
                Notes = c.Notes,
                c.AttachmentPath,
                c.CreatedAt,
                c.UpdatedAt,
                DepartmentId = c.DepartmentId,
                DepartmentName = c.IsForAllDepartments
                                    ? "كل الأقسام"
                                    : (c.Department != null ? c.Department.Name : "-"),
                EmployeeName = c.Employee != null && !c.IsAnonymous ? c.Employee.FullName : "من مجهول"
            }).ToList();

            return Ok(result);
        }

        // دالة فك التشفير
        private static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";
            try
            {
                var bytes = Convert.FromBase64String(cipherText);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return cipherText;
            }
        }




        // =========================
        // MANAGER DECISION
        // =========================
        [Authorize(Roles = "DepartmentManager,SubDepartmentManager,SectionManager")]
        [HttpPost("{id}/manager-decision")]
        public IActionResult ManagerDecision(int id, [FromBody] ManagerDecisionDto dto)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim))
                return Unauthorized();

            int managerId = int.Parse(employeeIdClaim);

            var complaint = _context.Complaints.Include(c => c.Department)
                                              .FirstOrDefault(c => c.Id == id);
            if (complaint == null) return NotFound("الشكوى غير موجودة");

            // صلاحية المدير
            if (!complaint.IsForAllDepartments && complaint.Department?.ManagerEmployeeId != managerId)
                return Forbid("ليس لديك صلاحية تغيير حالة هذه الشكوى");

            if (complaint.IsForAllDepartments && complaint.HandledByManagerId != null && complaint.HandledByManagerId != managerId)
                return BadRequest("الشكوى قيد المعالجة من إدارة أخرى");

            if (complaint.IsForAllDepartments && complaint.HandledByManagerId == null)
                complaint.HandledByManagerId = managerId;

            complaint.Status = dto.Status;
            complaint.Notes = dto.Notes;
            complaint.UpdatedAt = DateTime.Now;

            // إشعار للموظف
            var employeeUserId = _context.Employees
                .Where(e => e.Id == complaint.EmployeeId)
                .Select(e => e.UserId)
                .FirstOrDefault();

            // إشعار للموظف فقط إذا كان هناك موظف معروف
            // إشعار للموظف فقط إذا كان هناك موظف معروف
            if (complaint.EmployeeId != null)
            {
                var userIdToNotify = _context.Employees
                    .Where(e => e.Id == complaint.EmployeeId)
                    .Select(e => e.UserId)
                    .FirstOrDefault();

                if (userIdToNotify != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = userIdToNotify,
                        Title = "تم تحديث شكواك",
                        Message = $"حالة الشكوى: {complaint.Status}",
                        CreatedAt = DateTime.Now
                    });
                }
            }



            _context.SaveChanges();

            return Ok(new { message = "تم تحديث حالة الشكوى بنجاح" });
        }

        // =========================
        // DELETE COMPLAINT
        // =========================
        [Authorize(Roles = "Employee")]
        [HttpDelete("{id}")]
        public IActionResult DeleteComplaint(int id)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim))
                return Unauthorized();

            int employeeId = int.Parse(employeeIdClaim);

            var complaint = _context.Complaints.Find(id);
            if (complaint == null) return NotFound("الشكوى غير موجودة");
            if (complaint.EmployeeId != employeeId) return Forbid("لا يمكنك حذف شكوى لا تخصك");
            if (complaint.Status != ComplaintStatus.تحت_المراجعة)
                return BadRequest("لا يمكن حذف الشكوى بعد تغيير حالتها");

            _context.Complaints.Remove(complaint);
            _context.SaveChanges();

            return Ok(new { message = "تم حذف الشكوى بنجاح" });
        }
    }
}
