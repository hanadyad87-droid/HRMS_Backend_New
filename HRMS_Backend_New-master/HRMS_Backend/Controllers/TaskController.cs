using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Enums;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskStatusEnum = HRMS_Backend.Enums.TaskStatus;
namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TaskController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("section-employees")]
        public IActionResult GetSectionEmployees()
        {
            var managerId = int.Parse(User.Claims.First(c => c.Type == "EmployeeId").Value);

            var section = _context.Sections
                .FirstOrDefault(s => s.ManagerEmployeeId == managerId);

            if (section == null)
                return NotFound("لا يوجد قسم مرتبط بك");

            var employees = _context.EmployeeAdministrativeDatas
                .Include(a => a.Employee)
                .Where(a => a.SectionId == section.Id)
                .Select(a => new
                {
                    a.Employee.Id,
                    a.Employee.FullName
                })
                .ToList();

            return Ok(employees);
        }
        // =========================
        // إنشاء تكليف
        // =========================

        [HttpPost("assign")]
        public async Task<IActionResult> AssignTask([FromForm] CreateTaskDto dto)
        {
            // نجيب الـmanagerId من الـclaims
            var managerId = int.Parse(User.Claims.First(c => c.Type == "EmployeeId").Value);

            // نجيب القسم التابع للمدير تلقائياً
            var section = await _context.Sections
                .FirstOrDefaultAsync(s => s.ManagerEmployeeId == managerId);

            if (section == null)
                return Forbid("أنت مش مدير أي قسم");

            // التحقق من الموظف ضمن قسم المدير
            var employee = await _context.EmployeeAdministrativeDatas
                .Include(e => e.Employee)
                .FirstOrDefaultAsync(e => e.EmployeeId == dto.EmployeeId && e.SectionId == section.Id);

            if (employee == null)
                return NotFound("الموظف غير موجود ضمن قسمك");

            // التحقق من التواريخ
            if (dto.EndDate < dto.StartDate)
                return BadRequest("تاريخ النهاية يجب أن يكون بعد البداية");

            string? filePath = null;

            // التعامل مع المرفقات
            if (dto.Attachment != null)
            {
                // التأكد من حجم الملف (مثال: 5 ميجا كحد أقصى)
                if (dto.Attachment.Length > 5 * 1024 * 1024)
                    return BadRequest("حجم الملف كبير جدًا، الحد الأقصى 5 ميجا");

                // التأكد من نوع الملف (اختياري)
                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".png", ".jpg" };
                var extension = Path.GetExtension(dto.Attachment.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest("نوع الملف غير مدعوم");

                var folder = Path.Combine("wwwroot", "tasks");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}{extension}";
                var path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await dto.Attachment.CopyToAsync(stream);

                filePath = $"tasks/{fileName}";
            }

            // إنشاء التكليف
            var task = new TaskAssignment
            {
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                EmployeeId = dto.EmployeeId,
                AssignedByEmployeeId = managerId,
                SectionId = section.Id, // ناخذ القسم تلقائياً
                AttachmentPath = filePath,
                Status = TaskStatusEnum.New // ممكن تعطي الحالة الابتدائية
            };

            _context.TaskAssignments.Add(task);

            // إشعار للموظف
            var notification = new Notification
            {
                UserId = dto.EmployeeId,
                Title = "تكليف مهمة جديدة",
                Message = $"تم تكليفك بمهمة: {dto.Title}",
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return Ok("تم إرسال التكليف بنجاح");
        }
        // =========================
        // مهامي
        // =========================

        [HttpGet("my-tasks")]
        public IActionResult MyTasks()
        {
            var employeeId = int.Parse(User.Claims.First(c => c.Type == "EmployeeId").Value);
            var now = DateTime.Now;

            var tasks = _context.TaskAssignments
                .Include(t => t.AssignedBy)
                .Include(t => t.Section)
                .Where(t => t.EmployeeId == employeeId)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.StartDate,
                    t.EndDate,
                    t.Status,
                    Manager = t.AssignedBy.FullName,
                    Section = t.Section.Name,
                    t.AttachmentPath,
                    ManagerDecision = t.ManagerDecision,
                    // =================== تعديل الألوان ===================
                    Color = t.EndDate < now ? "red" :                     // انتهت المهمة
                            t.EndDate < now.AddDays(1) ? "orange" :     // قربت تنتهي خلال 24 ساعة
                            "default"                                  // باقي المهام

                })
                .ToList();

            return Ok(tasks);
        }

        // =========================
        // تغيير الحالة
        // =========================

        [HttpPut("update-status/{id}")]
        public IActionResult UpdateStatus(int id, string status)
        {
            var employeeId = int.Parse(User.Claims.First(c => c.Type == "EmployeeId").Value);

            var task = _context.TaskAssignments.FirstOrDefault(t => t.Id == id);

            if (task == null)
                return NotFound();

            if (task.EmployeeId != employeeId)
                return Forbid();

            // تحويل string إلى TaskStatus
            if (!Enum.TryParse<TaskStatusEnum>(status, out var newStatus))
                return BadRequest("الحالة غير صحيحة");

            task.Status = newStatus;

            _context.SaveChanges();

            return Ok("تم تحديث الحالة");
        }

        // =========================
        // قرار المدير
        // =========================

        [HttpPut("manager-decision/{id}")]
        public IActionResult UpdateManagerDecision(int id, ManagerDecision decision)
        {
            var managerId = int.Parse(User.Claims.First(c => c.Type == "EmployeeId").Value);
            var task = _context.TaskAssignments.FirstOrDefault(t => t.Id == id);
            if (task == null) return NotFound();
            if (task.AssignedByEmployeeId != managerId) return Forbid();

            task.ManagerDecision = decision;

            // إشعار الموظف
            string message = decision == ManagerDecision.Approved
                ? $"تم قبول مهمتك: {task.Title}"
                : $"تم رفض مهمتك: {task.Title}";

            _context.Notifications.Add(new Notification
            {
                UserId = task.EmployeeId,
                Title = "قرار المدير على مهمتك",
                Message = message,
                CreatedAt = DateTime.Now
            });

            _context.SaveChanges();

            return Ok("تم تحديث القرار وإرسال الإشعار");
        }
        [HttpGet("manager-tasks")]
        public IActionResult ManagerTasks()
        {
            var managerId = int.Parse(User.Claims.First(c => c.Type == "EmployeeId").Value);
            var now = DateTime.Now;

            var tasks = _context.TaskAssignments
                .Include(t => t.Employee)
                .Where(t => t.AssignedByEmployeeId == managerId)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.StartDate,
                    t.EndDate,
                    t.Status,
                    Employee = t.Employee.FullName,
                    Color = t.EndDate < now ? "red" :
                            t.EndDate < now.AddDays(1) ? "orange" :
                            "default"
                })
                .ToList();

            return Ok(tasks);
        }
        [HttpPost("{taskId}/comment")]
        public async Task<IActionResult> AddComment(int taskId, [FromForm] CreateTaskCommentDto dto)
        {
            var employeeId = int.Parse(User.Claims.First(c => c.Type == "EmployeeId").Value);
            var task = await _context.TaskAssignments.FindAsync(taskId);
            if (task == null) return NotFound();

            string? filePath = null;
            if (dto.Attachment != null)
            {
                var folder = Path.Combine("wwwroot", "task-comments");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Attachment.FileName)}";
                var path = Path.Combine(folder, fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await dto.Attachment.CopyToAsync(stream);

                filePath = $"task-comments/{fileName}";
            }

            // إنشاء التعليق
            var comment = new TaskComment
            {
                TaskAssignmentId = taskId,
                EmployeeId = employeeId,
                Comment = dto.Comment,
                AttachmentPath = filePath,
                CreatedAt = DateTime.Now
            };
            _context.TaskComments.Add(comment);

            // إشعار مع نص احتياطي إذا كان التعليق يحتوي على ملف فقط
            string employeeName = _context.Employees.Find(employeeId)?.FullName ?? "Unknown";
            string commentText = string.IsNullOrWhiteSpace(dto.Comment) && dto.Attachment != null ? "(تم إرسال مرفق)" : dto.Comment;

            int notifyUserId = employeeId == task.EmployeeId ? task.AssignedByEmployeeId : task.EmployeeId;

            _context.Notifications.Add(new Notification
            {
                UserId = notifyUserId,
                Title = "تعليق جديد على المهمة",
                Message = $"{employeeName}: {commentText}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok("تم إضافة التعليق بنجاح");
        }

        [HttpGet("{taskId}/comments")]
        public IActionResult GetComments(int taskId)
        {
            var comments = _context.TaskComments
                .Include(c => c.TaskAssignment)
                .Where(c => c.TaskAssignmentId == taskId)
                .Select(c => new TaskCommentDto
                {
                    Comment = c.Comment,
                    AttachmentPath = c.AttachmentPath,
                    AttachmentUrl = string.IsNullOrEmpty(c.AttachmentPath) ? null : $"{Request.Scheme}://{Request.Host}/{c.AttachmentPath}",
                    EmployeeName = _context.Employees
                        .Where(e => e.Id == c.EmployeeId)
                        .Select(e => e.FullName)
                        .FirstOrDefault() ?? "Unknown",
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            return Ok(comments);
        }
    }
}