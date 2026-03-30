using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Enums;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= Helper: Current User =================
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst("UserId")!.Value);
        }

        private Employee GetCurrentEmployee()
        {
            var userId = GetCurrentUserId();

            return _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.AdministrativeData)
                        .ThenInclude(a => a.SubDepartment)
                            .ThenInclude(sd => sd.Department)
                .Include(u => u.Employee.AdministrativeData.Section)
                .First(u => u.Id == userId)
                .Employee!;
        }

        private string GetUserLevel(Employee emp)
        {
            if (emp.AdministrativeData.SectionId != null)
                return "Section";

            if (emp.AdministrativeData.SubDepartmentId != null)
                return "SubDepartment";

            return "Department";
        }

        private IQueryable<Employee> ApplyEmployeeFilter(Employee currentEmployee)
        {
            var level = GetUserLevel(currentEmployee);

            var query = _context.Employees
                .Include(e => e.AdministrativeData)
                    .ThenInclude(a => a.SubDepartment)
                        .ThenInclude(sd => sd.Department)
                .Include(e => e.AdministrativeData.Section)
                .AsQueryable();

            if (level == "Section")
            {
                query = query.Where(e =>
                    e.AdministrativeData.SectionId ==
                    currentEmployee.AdministrativeData.SectionId);
            }
            else if (level == "SubDepartment")
            {
                query = query.Where(e =>
                    e.AdministrativeData.SubDepartmentId ==
                    currentEmployee.AdministrativeData.SubDepartmentId);
            }
            else
            {
                query = query.Where(e =>
                    e.AdministrativeData.SubDepartment.DepartmentId ==
                    currentEmployee.AdministrativeData.SubDepartment.DepartmentId);
            }

            return query;
        }

        // ================= Employees By SubDepartment =================
        [HttpGet("employees-by-subdepartment")]
        [HasPermission("ViewReports")]
        public IActionResult GetEmployeesBySubDepartment()
        {
            var currentEmployee = GetCurrentEmployee();

            var data = ApplyEmployeeFilter(currentEmployee)
                .Where(e => e.AdministrativeData != null)
                .GroupBy(e => e.AdministrativeData.SubDepartment.Name)
                .Select(g => new EmployeesBySubDepartmentDto
                {
                    SubDepartmentName = g.Key,
                    EmployeeCount = g.Count(),
                    Employees = g.Select(e => e.FullName).ToList()
                })
                .ToList();

            return Ok(data);
        }

        // ================= Requests Report =================
        [HttpGet("requests-report")]
        [HasPermission("ViewReports")]
        public IActionResult GetRequestsReport()
        {
            var currentEmployee = GetCurrentEmployee();

            var allowedEmployeeIds = ApplyEmployeeFilter(currentEmployee)
                .Select(e => e.Id)
                .ToList();

            var today = DateTime.Now;

            // ================= Load all request types into memory =================
            var leaveRequests = _context.LeaveRequests
                .Include(r => r.Employee)
                .Where(r => allowedEmployeeIds.Contains(r.EmployeeId))
                .AsEnumerable()
                .Select(r => new
                {
                    Type = "إجازة",
                    Status = r.Status.ToString(),
                    EmployeeName = r.Employee!.FullName ?? "غير معروف"
                });

            var maintenanceRequests = _context.MaintenanceRequests
                .Include(r => r.Employee)
                .Where(r => allowedEmployeeIds.Contains(r.EmployeeId))
                .AsEnumerable()
                .Select(r => new
                {
                    Type = "صيانة جهاز",
                    Status = r.Status.ToString(),
                    EmployeeName = r.Employee!.FullName ?? "غير معروف"
                });

            var salaryRequests = _context.SalaryCertificateRequests
                .Include(r => r.Employee)
                .Where(r => allowedEmployeeIds.Contains(r.EmployeeId))
                .AsEnumerable()
                .Select(r => new
                {
                    Type = "شهادة مرتب",
                    Status = r.Status.ToString(),
                    EmployeeName = r.Employee!.FullName ?? "غير معروف"
                });

            var dataUpdateRequests = _context.DataUpdateRequests
                .Include(r => r.Employee)
                .Where(r => allowedEmployeeIds.Contains(r.EmployeeId))
                .AsEnumerable()
                .Select(r => new
                {
                    Type = "تعديل بيانات",
                    Status = r.Status.ToString(),
                    EmployeeName = r.Employee!.FullName ?? "غير معروف"
                });

            // ================= Merge all requests =================
            var allRequests = leaveRequests
                .Concat(maintenanceRequests)
                .Concat(salaryRequests)
                .Concat(dataUpdateRequests);

            // ================= Group and shape the final result =================
            var result = allRequests
                .GroupBy(x => new { x.Type, x.Status })
                .Select(g => new RequestsReportDto
                {
                    RequestType = g.Key.Type,
                    Status = g.Key.Status,
                    Count = g.Count(),
                    Employees = g.Select(x => x.EmployeeName).Distinct().ToList()
                })
                .ToList();

            return Ok(result);
        }
        // ================= Employees By Qualification =================
        [HttpGet("employees-by-qualification")]
        [HasPermission("ViewReports")]
        public IActionResult GetEmployeesByQualification()
        {
            var currentEmployee = GetCurrentEmployee();

            var allowedEmployeeIds = ApplyEmployeeFilter(currentEmployee)
                .Select(e => e.Id)
                .ToList();

            var data = _context.EmployeeEducations
                .Include(e => e.Employee)
                .Include(e => e.Qualification)
                .Where(e => allowedEmployeeIds.Contains(e.EmployeeId))
                .ToList()
                .GroupBy(e => e.Qualification.Name)
                .Select(g => new EmployeesByQualificationDto
                {
                    QualificationName = g.Key,
                    Count = g.Count(),
                    Employees = g.Select(e => e.Employee.FullName ?? "غير معروف")
                                 .Distinct()
                                 .ToList()
                })
                .ToList();

            return Ok(data);
        }
        // ================= Tasks Report =================
        [HttpGet("tasks-report")]
        [HasPermission("ViewReports")]
        public IActionResult GetTasksReport()
        {
            var currentEmployee = GetCurrentEmployee();

            var allowedEmployeeIds = ApplyEmployeeFilter(currentEmployee)
                .Select(e => e.Id)
                .ToList();

            var data = _context.TaskAssignments
                .Include(t => t.Employee)
                .Where(t => allowedEmployeeIds.Contains(t.EmployeeId))
                .ToList()
                .GroupBy(t => t.Status)
                .Select(g => new TasksReportDto
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                    Employees = g.Select(t => t.Employee.FullName ?? "غير معروف")
                                 .Distinct()
                                 .ToList()
                })
                .ToList();

            return Ok(data);
        }
        [HttpGet("tasks-by-employee")]
        [HasPermission("ViewReports")]
        public IActionResult GetTasksByEmployee()
        {
            var currentEmployee = GetCurrentEmployee();

            var allowedEmployeeIds = ApplyEmployeeFilter(currentEmployee)
                .Select(e => e.Id)
                .ToList();
            Console.WriteLine(string.Join(",", allowedEmployeeIds));
       var data = _context.TaskAssignments
    .Include(t => t.Employee)
    .Where(t => t.Employee != null)
    .ToList()
    .GroupBy(t => t.Employee.FullName)
    .Select(g => new
    {
        Employee = g.Key,
        Count = g.Count(),
    })
    .ToList();

            return Ok(data);
        }
        // ================= Delegations Report =================
        [HttpGet("delegations-report")]
        [HasPermission("ViewReports")]
        public IActionResult GetDelegationsReport()
        {
            var currentEmployee = GetCurrentEmployee();
            var allowedEmployeeIds = ApplyEmployeeFilter(currentEmployee)
                .Select(e => e.Id)
                .ToList();

            var today = DateTime.Now;

            var data = _context.ManagerDelegations
                .Include(d => d.ActingManager)
                .Include(d => d.OriginalManager)
                .Include(d => d.AssignedBy)
                .ToList()
                .Where(d =>
                    allowedEmployeeIds.Contains(d.ActingManagerId) ||
                    allowedEmployeeIds.Contains(d.OriginalManagerId))
                .Select(d => new DelegationReportDto
                {
                    ActingManager = d.ActingManager.FullName ?? "غير معروف",
                    OriginalManager = d.OriginalManager.FullName ?? "غير معروف",
                    AssignedBy = d.AssignedBy.FullName ?? "غير معروف",
                    EntityType = d.EntityType,
                    EntityName = GetEntityName(d.EntityType, d.EntityId),
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                    Status = (d.IsActive && (d.EndDate == null || d.EndDate >= today))
                        ? "نشط"
                        : "منتهي"
                })
                .ToList();

            return Ok(data);
        }

        // ================= Employees By Grade =================
        [HttpGet("employees-by-grade/{gradeId}")]
        public async Task<IActionResult> GetEmployeesByGrade(int gradeId)
        {
            var currentEmployee = GetCurrentEmployee();

            var allowedEmployeeIds = ApplyEmployeeFilter(currentEmployee)
                .Select(e => e.Id)
                .ToList();

            var data = await _context.EmployeeFinancialDatas
                .Include(e => e.Employee)
                .Include(e => e.JobGrade)
                .Where(e =>
                    e.JobGradeId == gradeId &&
                    allowedEmployeeIds.Contains(e.EmployeeId))
                .ToListAsync();

            if (!data.Any())
            {
                return Ok(new
                {
                    JobGrade = "غير موجودة",
                    Count = 0,
                    Employees = new List<object>()
                });
            }

            return Ok(new
            {
                JobGrade = data.First().JobGrade.Name,
                Count = data.Count,
                Employees = data.Select(e => new
                {
                    Id = e.Employee.Id,
                    Name = e.Employee.FullName
                })
            });
        }

        // ================= Employees On Leave =================
        [HttpGet("employees-on-leave")]
        [HasPermission("ViewReports")]
        public IActionResult GetEmployeesOnLeave(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            if (toDate < fromDate)
                return BadRequest("تاريخ النهاية لازم يكون بعد البداية");

            var currentEmployee = GetCurrentEmployee();

            var allowedEmployeeIds = ApplyEmployeeFilter(currentEmployee)
                .Select(e => e.Id)
                .ToList();

            var data = _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Where(l =>
                    allowedEmployeeIds.Contains(l.EmployeeId) &&
                    l.Status == LeaveStatus.موافقة_نهائية &&
                    l.FromDate.Date <= toDate &&
                    l.ToDate.Date >= fromDate
                )
                .Select(l => new EmployeesOnLeaveDto
                {
                    EmployeeName = l.Employee!.FullName ?? "غير معروف",
                    LeaveType = l.LeaveType != null ? l.LeaveType.اسم_الاجازة : "غير محدد",
                    FromDate = l.FromDate,
                    ToDate = l.ToDate,
                    TotalDays = l.TotalDays
                })
                .OrderBy(l => l.FromDate)
                .ToList();

            return Ok(data);
        }

        // ================= Helper =================
        private string GetEntityName(string entityType, int entityId)
        {
            return entityType switch
            {
                "Section" => _context.Sections
                    .Where(x => x.Id == entityId)
                    .Select(x => x.Name)
                    .FirstOrDefault() ?? "غير محدد",

                "SubDepartment" => _context.SubDepartments
                    .Where(x => x.Id == entityId)
                    .Select(x => x.Name)
                    .FirstOrDefault() ?? "غير محدد",

                "Department" => _context.Departments
                    .Where(x => x.Id == entityId)
                    .Select(x => x.Name)
                    .FirstOrDefault() ?? "غير محدد",

                _ => "غير معروف"
            };
        }
    }
}