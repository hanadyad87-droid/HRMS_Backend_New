using HRMS_Backend.Attributes;
using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using HRMS_Backend.Enums;
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

        //================= Employees By SubDepartment =================
        [HttpGet("employees-by-subdepartment")]
        [HasPermission("ViewReports")]
        public IActionResult GetEmployeesBySubDepartment()
        {
            var data = _context.Employees
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

        //================= Requests Report =================
        [HttpGet("requests-report")]
        [HasPermission("ViewReports")]
        public IActionResult GetRequestsReport()
        {
            var leaveRequests = _context.LeaveRequests
                .Include(r => r.Employee)
                .Select(r => new
                {
                    Type = "إجازة",
                    Status = r.Status.ToString(),
                    EmployeeName = r.Employee!.FullName ?? "غير معروف"
                })
                .ToList();

            var maintenanceRequests = _context.MaintenanceRequests
                .Include(r => r.Employee)
                .Select(r => new
                {
                    Type = "صيانة جهاز",
                    Status = r.Status.ToString(),
                    EmployeeName = r.Employee!.FullName ?? "غير معروف"
                })
                .ToList();

            var salaryRequests = _context.SalaryCertificateRequests
                .Include(r => r.Employee)
                .Select(r => new
                {
                    Type = "شهادة مرتب",
                    Status = r.Status.ToString(),
                    EmployeeName = r.Employee!.FullName ?? "غير معروف"
                })
                .ToList();

            var dataUpdateRequests = _context.DataUpdateRequests
                .Include(r => r.Employee)
                .Select(r => new
                {
                    Type = "تعديل بيانات",
                    Status = r.Status.ToString(),
                    EmployeeName = r.Employee!.FullName ?? "غير معروف"
                })
                .ToList();

            var allRequests = leaveRequests
                .Concat(maintenanceRequests)
                .Concat(salaryRequests)
                .Concat(dataUpdateRequests);

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

        //================= Delegations Report =================
        [HttpGet("delegations-report")]
        [HasPermission("ViewReports")]
        public IActionResult GetDelegationsReport()
        {
            var today = DateTime.Now;

            var data = _context.ManagerDelegations
                .Include(d => d.ActingManager)
                .Include(d => d.OriginalManager)
                .Include(d => d.AssignedBy)
                .ToList() // 🔥 مهم جداً
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

        //================= Employees By Grade =================
        [HttpGet("employees-by-grade/{gradeId}")]
        public async Task<IActionResult> GetEmployeesByGrade(int gradeId)
        {
            var data = await _context.EmployeeFinancialDatas
                .Include(e => e.Employee)
                .Include(e => e.JobGrade)
                .Where(e => e.JobGradeId == gradeId)
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

        //================= Employees On Leave =================
        [HttpGet("employees-on-leave")]
        [HasPermission("ViewReports")]
        public IActionResult GetEmployeesOnLeave(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            if (toDate < fromDate)
                return BadRequest("تاريخ النهاية لازم يكون بعد البداية");

            var from = fromDate.Date;
            var to = toDate.Date;

            var data = _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Where(l =>
                    l.Status == LeaveStatus.موافقة_نهائية &&
                    l.FromDate.Date <= to &&
                    l.ToDate.Date >= from
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

        //================= Helper =================
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