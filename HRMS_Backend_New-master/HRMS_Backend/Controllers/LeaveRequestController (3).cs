using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Enums;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [ApiController]
    [Route("api/leave-requests")]
    public class LeaveRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================
        // CREATE LEAVE REQUEST
        // ============================
        [Authorize]
        [HasPermission("SubmitLeave")]
        [HttpPost("create")]
        public IActionResult Create([FromForm] CreateLeaveRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.ToDate < dto.FromDate)
                return BadRequest("التاريخ غير صحيح");

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
            if (employee == null)
                return BadRequest("الموظف غير موجود");

            var admin = _context.EmployeeAdministrativeDatas
                .FirstOrDefault(a => a.EmployeeId == employee.Id);

            if (admin == null)
                return BadRequest("لا توجد بيانات إدارية");

            var holidays = _context.OfficialHolidays.Select(h => h.Date.Date).ToList();

            int totalDays = 0;

            for (var d = dto.FromDate.Date; d <= dto.ToDate.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday)
                    continue;

                if (holidays.Contains(d))
                    continue;

                totalDays++;
            }

            var leaveType = _context.LeaveTypes.Find(dto.LeaveTypeId);

            if (leaveType != null && leaveType.مخصومة_من_الرصيد)
            {
                if (admin.LeaveBalance < totalDays)
                    return BadRequest("الرصيد غير كافي");
            }

            var flow = ResolveApprovalFlow(employee.Id, admin);

            var leave = new LeaveRequest
            {
                EmployeeId = employee.Id,
                LeaveTypeId = dto.LeaveTypeId,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                TotalDays = totalDays,
                Notes = dto.Notes,
                AttachmentPath = null,
                ApprovalFlow = flow,
                PartialApproval = null,
                FinalApproval = null
            };

            _context.LeaveRequests.Add(leave);
            _context.SaveChanges();

            // =========================
            // 🔥 إشعار أول مدير
            // =========================
            var section = GetManagedSection(employee.Id);
            int? managerId = section?.ManagerEmployeeId;

            if (managerId == null)
            {
                var sub = GetSubDepartmentFromAdmin(admin);
                managerId = sub?.ManagerEmployeeId;
            }

            if (managerId == null)
            {
                var dept = GetDepartmentFromAdmin(admin);
                managerId = dept?.ManagerEmployeeId;
            }

            if (managerId != null)
            {
                CreateNotificationForEmployee(
                    managerId.Value,
                    "طلب إجازة جديد",
                    $"طلب إجازة جديد من {employee.FullName}"
                );
            }

            return Ok(new
            {
                message = "تم إنشاء الطلب"
            });
        }

        private static string FlowHint(LeaveApprovalFlow flow) => flow switch
        {
            LeaveApprovalFlow.RegularEmployee =>
                "المرحلة 1: مدير القسم → المرحلة 2 (النهائية): مدير الإدارة الفرعية.",
            LeaveApprovalFlow.SectionManager =>
                "المرحلة 1: مدير الإدارة الفرعية → المرحلة 2 (النهائية): مدير الإدارة العامة.",
            LeaveApprovalFlow.SubDepartmentManager =>
                "مرحلة واحدة (نهائية): مدير الإدارة العامة فقط.",
            _ => ""
        };

        private Section? GetManagedSection(int employeeId)
        {
            return _context.Sections.AsNoTracking()
                .FirstOrDefault(s => s.ManagerEmployeeId == employeeId);
        }

        private subDepartment? GetManagedSubDepartment(int employeeId)
        {
            return _context.SubDepartments.AsNoTracking()
                .FirstOrDefault(s => s.ManagerEmployeeId == employeeId);
        }

        private Department? GetManagedDepartment(int employeeId)
        {
            return _context.Departments.AsNoTracking()
                .FirstOrDefault(d => d.ManagerEmployeeId == employeeId);
        }

        private subDepartment? GetSubDepartmentForSection(Section? section)
        {
            if (section == null)
                return null;

            return _context.SubDepartments.AsNoTracking()
                .FirstOrDefault(s => s.Id == section.SubDepartmentId);
        }

        private Department? GetDepartmentForSubDepartment(subDepartment? sub)
        {
            if (sub == null)
                return null;

            return _context.Departments.AsNoTracking()
                .FirstOrDefault(d => d.Id == sub.DepartmentId);
        }

        private Section? GetSectionFromAdmin(EmployeeAdministrativeData admin)
        {
            return _context.Sections.AsNoTracking()
                .FirstOrDefault(s => s.Id == admin.SectionId);
        }

        private subDepartment? GetSubDepartmentFromAdmin(EmployeeAdministrativeData admin)
        {
            return _context.SubDepartments.AsNoTracking()
                .FirstOrDefault(s => s.Id == admin.SubDepartmentId);
        }

        private Department? GetDepartmentFromAdmin(EmployeeAdministrativeData admin)
        {
            return _context.Departments.AsNoTracking()
                .FirstOrDefault(d => d.Id == admin.DepartmentId);
        }

        private string? ValidateApprovalChainExists(LeaveApprovalFlow flow, EmployeeAdministrativeData admin)
        {
            var section = GetManagedSection(admin.EmployeeId) ?? GetSectionFromAdmin(admin);
            var sub = GetManagedSubDepartment(admin.EmployeeId)
                ?? GetSubDepartmentForSection(section)
                ?? GetSubDepartmentFromAdmin(admin);
            var dept = GetManagedDepartment(admin.EmployeeId)
                ?? GetDepartmentForSubDepartment(sub)
                ?? GetDepartmentFromAdmin(admin);

            return flow switch
            {
                LeaveApprovalFlow.RegularEmployee when section?.ManagerEmployeeId == null =>
                    "لم يُعيَّن مدير للقسم في الهيكل — عيّن مدير القسم (AssignManager type=section).",
                LeaveApprovalFlow.RegularEmployee when sub?.ManagerEmployeeId == null =>
                    "لم يُعيَّن مدير للإدارة الفرعية — عيّن مدير الإدارة الفرعية لإكمال الموافقة النهائية.",
                LeaveApprovalFlow.SectionManager when sub?.ManagerEmployeeId == null =>
                    "لم يُعيَّن مدير للإدارة الفرعية — مطلوب لمرحلة موافقة مدير القسم على إجازته.",
                LeaveApprovalFlow.SectionManager when dept?.ManagerEmployeeId == null =>
                    "لم يُعيَّن مدير للإدارة العامة — مطلوب للموافقة النهائية بعد مدير الإدارة الفرعية.",
                LeaveApprovalFlow.SubDepartmentManager when dept?.ManagerEmployeeId == null =>
                    "لم يُعيَّن مدير للإدارة العامة — مطلوب لمسار مدير الإدارة الفرعية (موافقة نهائية واحدة).",
                _ => null
            };
        }

        /// <summary>
        /// يمنع حالات «الموافق النهائي = مقدّم الطلب» حيث لا يوجد مستوى أعلى في الهيكل.
        /// </summary>
        private static string? ValidateNoSelfApprovalAtFinalStep(
            LeaveApprovalFlow flow,
            int employeeId,
            subDepartment? sub,
            Section? section,
            Department? dept)
        {
            if (flow == LeaveApprovalFlow.SubDepartmentManager && dept?.ManagerEmployeeId == employeeId)
                return "أنت مسجّل كمدير إدارة فرعية ومدير الإدارة العامة لنفس الوحدة — لا يمكن إكمال الموافقة آلياً. تواصل مع الموارد البشرية.";

            if (flow == LeaveApprovalFlow.SectionManager && dept?.ManagerEmployeeId == employeeId)
                return "أنت مدير قسم ومدير الإدارة العامة لنفس السلسلة — الموافقة النهائية لا يمكن أن تكون منك. عيّن مدير إدارة عامة مختلف أو تواصل مع الموارد البشرية.";

            _ = sub;
            _ = section;
            return null;
        }

        /// <summary>
        /// وصف عربي موحّد: من تنتظر الطلب الآن (للواجهات والتشخيص).
        /// </summary>
        private string DescribeCurrentWaitingStep(LeaveRequest leave, EmployeeAdministrativeData admin)
        {
            if (leave.FinalApproval == false)
                return "مرفوض";
            if (leave.FinalApproval == true)
                return "مكتمل (مقبول نهائياً)";

            return EffectiveRouting(leave, admin) switch
            {
                LeaveApprovalFlow.RegularEmployee when leave.PartialApproval != true =>
                    "بانتظار موافقة مدير القسم",
                LeaveApprovalFlow.RegularEmployee =>
                    "بانتظار الموافقة النهائية من مدير الإدارة الفرعية",
                LeaveApprovalFlow.SectionManager when leave.PartialApproval != true =>
                    "بانتظار موافقة مدير الإدارة الفرعية",
                LeaveApprovalFlow.SectionManager =>
                    "بانتظار الموافقة النهائية من مدير الإدارة العامة",
                LeaveApprovalFlow.SubDepartmentManager =>
                    "بانتظار الموافقة النهائية من مدير الإدارة العامة",
                _ => "قيد المعالجة"
            };
        }

        /// <summary>
        /// أولوية: مدير إدارة فرعية أولاً (حتى لا يُطلب من مدير الفرعية موافقة نفسه كخطوة أولى عند التداخل)،
        /// ثم مدير قسم، ثم موظف عادي.
        /// </summary>
        private LeaveApprovalFlow ResolveApprovalFlow(int employeeId, EmployeeAdministrativeData admin)
        {
            if (GetManagedSubDepartment(employeeId) != null)
                return LeaveApprovalFlow.SubDepartmentManager;

            if (GetManagedSection(employeeId) != null)
                return LeaveApprovalFlow.SectionManager;

            return LeaveApprovalFlow.RegularEmployee;
        }

        // ==========================================
        // MY LEAVE REQUESTS (موظف — تطبيق الموبايل)
        // ==========================================
        [Authorize]
        [HttpGet("my-requests")]
        public IActionResult GetMyRequests()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
            if (employee == null)
                return NotFound("الموظف غير موجود");

            var admin = _context.EmployeeAdministrativeDatas
                .FirstOrDefault(a => a.EmployeeId == employee.Id);

            var balance = admin?.LeaveBalance ?? 0;

            var list = _context.LeaveRequests
                .AsNoTracking()
                .Include(l => l.LeaveType)
                .Where(l => l.EmployeeId == employee.Id)
                .OrderByDescending(l => l.Id)
                .ToList();

            var requests = list.Select(l => new
            {
                id = l.Id,
                leaveType = l.LeaveType?.اسم_الاجازة ?? "إجازة",
                fromDate = l.FromDate,
                toDate = l.ToDate,
                status = MapLeaveStatusForMobile(l, admin),
                waitingFor = DescribeCurrentWaitingStep(l, admin),
                effectiveFlow = EffectiveRouting(l, admin).ToString()
            }).ToList();

            return Ok(new { requests, balance });
        }

        private static LeaveApprovalFlow EffectiveFlow(LeaveRequest leave)
        {
            if (Enum.IsDefined(typeof(LeaveApprovalFlow), leave.ApprovalFlow))
                return leave.ApprovalFlow;
            // صفوف قديمة أو قيمة غير معروفة
            return LeaveApprovalFlow.RegularEmployee;
        }

        /// <summary>
        /// يصحّح المسار الفعلي من بيانات الطلب + هيكل الموظف: مثلاً إذا كان المسار مخزّناً RegularEmployee
        /// لكن مقدّم الطلب هو مدير القسم، لا يُطلب منه أن يوافق على نفسه كمدير قسم — يُعامل كمسار SectionManager.
        /// أولوية مدير الإدارة الفرعية قبل مدير القسم عند التداخل.
        /// </summary>
        private LeaveApprovalFlow EffectiveRouting(LeaveRequest leave, EmployeeAdministrativeData admin)
        {
            var flow = EffectiveFlow(leave);

            if (flow == LeaveApprovalFlow.RegularEmployee && GetManagedSubDepartment(leave.EmployeeId) != null)
                return LeaveApprovalFlow.SubDepartmentManager;

            if (flow == LeaveApprovalFlow.RegularEmployee && GetManagedSection(leave.EmployeeId) != null)
                return LeaveApprovalFlow.SectionManager;

            return flow;
        }

        private string MapLeaveStatusForMobile(LeaveRequest l, EmployeeAdministrativeData? admin)
        {
            if (l.FinalApproval == false)
                return "مرفوض";
            if (l.FinalApproval == true)
                return "مقبولة نهائياً";
            if (admin == null)
                return "قيد الانتظار";

            switch (EffectiveRouting(l, admin))
            {
                case LeaveApprovalFlow.RegularEmployee:
                    if (l.PartialApproval == true)
                        return "قيد انتظار موافقة مدير الإدارة الفرعية";
                    return "قيد انتظار موافقة مدير القسم";
                case LeaveApprovalFlow.SectionManager:
                    if (l.PartialApproval == true)
                        return "قيد انتظار موافقة مدير الإدارة العامة";
                    return "قيد انتظار موافقة مدير الإدارة الفرعية";
                case LeaveApprovalFlow.SubDepartmentManager:
                    return "قيد انتظار موافقة مدير الإدارة العامة";
                default:
                    if (l.PartialApproval == true)
                        return "قيد انتظار الموافقة النهائية";
                    return "قيد الانتظار";
            }
        }

        // ==========================================
        // MANAGER PENDING REQUESTS
        // ==========================================
        [Authorize]
        [HasPermission("ApproveLeave")]
        [HttpGet("manager/pending")]
        public IActionResult GetPending([FromQuery] bool verbose = false)
        {
            var currentEmpId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");

            var all = _context.LeaveRequests
                .Include(l => l.Employee)!.ThenInclude(e => e!.AdministrativeData)
                .Include(l => l.LeaveType)
                .Where(l => l.FinalApproval == null)
                .ToList();

            var filtered = all.Where(l => IsPendingForCurrentUser(currentEmpId, l)).ToList();

            if (!verbose)
                return Ok(filtered);

            var result = filtered.Select(l =>
            {
                return new LeaveRequestResponseDto
                {
                    Id = l.Id,
                    EmployeeName = l.Employee?.FullName ?? "غير معروف",
                    LeaveType = l.LeaveType?.اسم_الاجازة ?? "غير محدد",

                    FromDate = l.FromDate,
                    ToDate = l.ToDate,
                    TotalDays = l.TotalDays,

                    PartialApproval = l.PartialApproval,
                    FinalApproval = l.FinalApproval,

                    PartialNote = l.PartialNote,
                    FinalNote = l.FinalNote,

                    RejectionReason = l.سبب_الرفض,
                    Notes = l.Notes,

                    AttachmentPath = l.AttachmentPath
                };
            }).ToList();

            return Ok(result);
        }

        private bool IsPendingForCurrentUser(int currentEmpId, LeaveRequest leave)
        {
            var admin = leave.Employee?.AdministrativeData
                ?? _context.EmployeeAdministrativeDatas
                    .AsNoTracking()
                    .FirstOrDefault(a => a.EmployeeId == leave.EmployeeId);

            if (admin == null)
                return false;

            return CanActOnCurrentStep(currentEmpId, leave, admin);
        }

        /// <summary>من يملك حق الموافقة أو الرفض في المرحلة الحالية.</summary>
        private bool CanActOnCurrentStep(int currentEmpId, LeaveRequest leave,
            EmployeeAdministrativeData admin)
        {
            var section = GetManagedSection(leave.EmployeeId) ?? GetSectionFromAdmin(admin);
            var sub = GetManagedSubDepartment(leave.EmployeeId)
                ?? GetSubDepartmentForSection(section)
                ?? GetSubDepartmentFromAdmin(admin);
            var dept = GetManagedDepartment(leave.EmployeeId)
                ?? GetDepartmentForSubDepartment(sub)
                ?? GetDepartmentFromAdmin(admin);

            switch (EffectiveRouting(leave, admin))
            {
                case LeaveApprovalFlow.RegularEmployee:
                    if (leave.PartialApproval != true)
                        return section?.ManagerEmployeeId == currentEmpId;
                    return sub?.ManagerEmployeeId == currentEmpId;

                case LeaveApprovalFlow.SectionManager:
                    if (leave.PartialApproval != true)
                        return sub?.ManagerEmployeeId == currentEmpId;
                    if (leave.EmployeeId == dept?.ManagerEmployeeId)
                        return false;
                    return dept?.ManagerEmployeeId == currentEmpId;

                case LeaveApprovalFlow.SubDepartmentManager:
                    if (leave.EmployeeId == dept?.ManagerEmployeeId)
                        return false;
                    return leave.PartialApproval == null
                           && leave.FinalApproval == null
                           && dept?.ManagerEmployeeId == currentEmpId;

                default:
                    return false;
            }
        }

        [Authorize]
        [HasPermission("ApproveLeave")]
        [HttpPost("{id}/manager-decision")]
        public IActionResult ManagerDecision(int id, bool approve, string? note)
        {
            var leave = _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .FirstOrDefault(l => l.Id == id);

            if (leave == null)
                return NotFound();

            var currentEmpId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");

            var admin = _context.EmployeeAdministrativeDatas
                .FirstOrDefault(a => a.EmployeeId == leave.EmployeeId);

            if (admin == null)
                return BadRequest("لا توجد بيانات الموظف");

            if (!CanActOnCurrentStep(currentEmpId, leave, admin))
                return BadRequest("غير مسموح");

            // =========================
            // ❌ رفض الطلب
            // =========================
            if (!approve)
            {
                leave.سبب_الرفض = note;
                leave.FinalApproval = false;

                CreateNotificationForEmployee(
                    leave.EmployeeId,
                    "تم رفض طلب الإجازة",
                    note ?? "تم رفض الطلب"
                );

                _context.SaveChanges();
                return Ok("تم الرفض");
            }

            // =========================
            // ✅ موافقة حسب المرحلة
            // =========================
            switch (EffectiveRouting(leave, admin))
            {
                case LeaveApprovalFlow.RegularEmployee:
                case LeaveApprovalFlow.SectionManager:

                    if (leave.PartialApproval != true)
                    {
                        leave.PartialApproval = true;
                        leave.PartialNote = note;

                        // 🔔 إشعار الموظف بالموافقة الأولى
                        CreateNotificationForEmployee(
                            leave.EmployeeId,
                            "تمت الموافقة على المرحلة الأولى",
                            "تمت الموافقة على طلب الإجازة في المرحلة الأولى"
                        );

                        // 🔔 إشعار المدير التالي
                        NotifyNextManager(leave, admin);
                    }
                    else
                    {
                        leave.FinalApproval = true;
                        leave.FinalNote = note;
                        DeductLeaveBalanceIfNeeded(leave, admin);

                        CreateNotificationForEmployee(
                            leave.EmployeeId,
                            "تمت الموافقة النهائية",
                            "تمت الموافقة على طلب الإجازة بالكامل"
                        );
                    }

                    break;

                case LeaveApprovalFlow.SubDepartmentManager:
                    leave.FinalApproval = true;
                    leave.FinalNote = note;
                    DeductLeaveBalanceIfNeeded(leave, admin);

                    CreateNotificationForEmployee(
                        leave.EmployeeId,
                        "تمت الموافقة النهائية",
                        "تمت الموافقة على طلب الإجازة بالكامل"
                    );

                    break;
            }

            _context.SaveChanges();
            return Ok("تم التحديث");
        }

        private void DeductLeaveBalanceIfNeeded(LeaveRequest leave, EmployeeAdministrativeData requesterAdmin)
        {
            if (leave.FinalApproval != true)
                return;

            if (leave.LeaveType != null && leave.LeaveType.مخصومة_من_الرصيد)
                requesterAdmin.LeaveBalance -= leave.TotalDays;
        }
        private void CreateNotificationForEmployee(int employeeId, string title, string message)
        {
            var notification = new Notification
            {
                UserId = employeeId,
                Title = title,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
        }
        private void CreateNotificationForManager(LeaveRequest leave, string title, string message)
        {
            var admin = _context.EmployeeAdministrativeDatas
                .FirstOrDefault(a => a.EmployeeId == leave.EmployeeId);

            if (admin == null) return;

            int? managerId = null;

            var section = GetManagedSection(leave.EmployeeId);
            if (section?.ManagerEmployeeId != null)
                managerId = section.ManagerEmployeeId;

            if (managerId == null)
            {
                var sub = GetSubDepartmentFromAdmin(admin);
                if (sub?.ManagerEmployeeId != null)
                    managerId = sub.ManagerEmployeeId;
            }

            if (managerId == null)
            {
                var dept = GetDepartmentFromAdmin(admin);
                managerId = dept?.ManagerEmployeeId;
            }

            if (managerId == null) return;

            var notification = new Notification
            {
                UserId = managerId.Value,
                Title = title,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
        }
        private void NotifyNextManager(LeaveRequest leave, EmployeeAdministrativeData admin)
        {
            int? nextManagerId = null;

            var section = GetManagedSection(leave.EmployeeId);
            if (section?.ManagerEmployeeId != null)
                nextManagerId = section.ManagerEmployeeId;

            if (nextManagerId == null)
            {
                var sub = GetSubDepartmentFromAdmin(admin);
                nextManagerId = sub?.ManagerEmployeeId;
            }

            if (nextManagerId == null)
            {
                var dept = GetDepartmentFromAdmin(admin);
                nextManagerId = dept?.ManagerEmployeeId;
            }

            if (nextManagerId != null)
            {
                CreateNotificationForEmployee(
                    nextManagerId.Value,
                    "طلب إجازة جديد يحتاج مراجعة",
                    "وصلتك إجازة تحتاج موافقة"
                );
            }
        }
    }
}
