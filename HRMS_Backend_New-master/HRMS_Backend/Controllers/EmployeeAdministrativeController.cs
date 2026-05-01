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
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeAdministrativeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployeeAdministrativeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // ADD
        // =========================
        // =========================
        // ADD (Using PublicId)
        // =========================
        [HttpPost]
        [HasPermission("AddEmployee")]
        public async Task<IActionResult> Add(CreateEmployeeAdministrativeDto dto)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.PublicId == dto.EmployeePublicId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            var existing = await _context.EmployeeAdministrativeDatas
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id);

            if (existing != null)
                return BadRequest("الموظف لديه بيانات إدارية موجودة بالفعل");

            var validationError = ValidateByJobStatus(dto);
            if (validationError != null)
                return BadRequest(validationError);

            var data = new EmployeeAdministrativeData
            {
                EmployeeId = employee.Id, // مهم جداً 👈 نخزن الـ Id الداخلي
                JobStatus = dto.JobStatus,

                JobTitleId = dto.JobTitleId,
                DepartmentId = dto.DepartmentId,
                SubDepartmentId = dto.SubDepartmentId,
                SectionId = dto.SectionId,
                StartWorkDate = dto.StartWorkDate,
                WorkLocationId = dto.WorkLocationId,
                JobGradeId = dto.JobGradeId,
                LeaveBalance = dto.LeaveBalance,

                ContractStartDate = dto.ContractStartDate,
                ContractEndDate = dto.ContractEndDate,
                AppointmentDate = dto.AppointmentDate,

                TransferType = dto.TransferType,
                TransferFromOrganizationId = dto.TransferFromOrganizationId,
                TransferStartDate = dto.TransferStartDate,
                TransferEndDate = dto.TransferEndDate,

                SecondmentToOrganizationId = dto.SecondmentToOrganizationId,
                SecondmentStartDate = dto.SecondmentStartDate,
                SecondmentEndDate = dto.SecondmentEndDate
            };

            _context.EmployeeAdministrativeDatas.Add(data);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم إنشاء البيانات الإدارية بنجاح",
                EmployeePublicId = employee.PublicId,
                employeeNumber = employee.EmployeeNumber,
                fullName = employee.FullName
            });
        }


        // =========================
        // GET MY DATA
        // =========================
        [HttpGet("my-data")]
        [Authorize]
        [HasPermission("ViewEmployee")]
        public async Task<IActionResult> GetMyAdministrativeData()
        {
            var username = User.Identity!.Name;

            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.AdministrativeData)
                    .ThenInclude(a => a.TransferFromOrganization)
                .Include(e => e.AdministrativeData)
                    .ThenInclude(a => a.SecondmentToOrganization)
                .FirstOrDefaultAsync(e => e.User.Username == username);

            if (employee == null || employee.AdministrativeData == null)
                return NotFound("الموظف غير موجود أو لا توجد بيانات إدارية");

            var a = employee.AdministrativeData;

            await _context.Entry(a).Reference(x => x.JobTitle).LoadAsync();
            await _context.Entry(a).Reference(x => x.Department).LoadAsync();
            await _context.Entry(a).Reference(x => x.SubDepartment).LoadAsync();
            await _context.Entry(a).Reference(x => x.Section).LoadAsync();
            await _context.Entry(a).Reference(x => x.WorkLocation).LoadAsync();
            await _context.Entry(a).Reference(x => x.JobGrade).LoadAsync();

            return Ok(new
            {
                a.Id,
                EmployeePublicId = employee.PublicId,
                a.JobStatus,
                JobTitle = a.JobTitle?.Name,
                Department = a.Department?.Name,
                SubDepartment = a.SubDepartment?.Name,
                Section = a.Section?.Name,
                a.StartWorkDate,
                WorkLocation = a.WorkLocation?.Name,
                JobGrade = a.JobGrade?.Name,
                a.LeaveBalance,

                a.ContractStartDate,
                a.ContractEndDate,
                a.AppointmentDate,

                a.TransferType,
                TransferFromEntity = a.TransferFromOrganization?.Name,
                a.TransferStartDate,
                a.TransferEndDate,

                SecondmentToEntity = a.SecondmentToOrganization?.Name,
                a.SecondmentStartDate,
                a.SecondmentEndDate
            });
        }

        // =========================
        // GET ALL
        // =========================
        [HttpGet("get-all")]
        [HasPermission("ViewEmployee")]
        public async Task<IActionResult> GetAllAdministrativeData()
        {
            var data = await _context.EmployeeAdministrativeDatas
                .Include(a => a.Employee)
                .Include(a => a.JobTitle)
                .Include(a => a.Department)
                .Include(a => a.SubDepartment)
                .Include(a => a.Section)
                .Include(a => a.WorkLocation)
                .Include(a => a.JobGrade)
                .Include(a => a.TransferFromOrganization)
                .Include(a => a.SecondmentToOrganization)
                .Select(a => new
                {
                    a.Id,
                    EmployeePublicId = a.Employee.PublicId,
                    FullName = a.Employee.FullName,
                    a.JobStatus,
                    JobTitle = a.JobTitle.Name,
                    Department = a.Department.Name,
                    SubDepartment = a.SubDepartment.Name,
                    Section = a.Section.Name,
                    a.StartWorkDate,
                    WorkLocation = a.WorkLocation.Name,
                    JobGrade = a.JobGrade.Name,
                    a.LeaveBalance,
                    a.ContractStartDate,
                    a.ContractEndDate,
                    a.AppointmentDate,
                    TransferFromEntity = a.TransferFromOrganization != null ? a.TransferFromOrganization.Name : null,
                    a.TransferStartDate,
                    a.TransferEndDate,
                    SecondmentToEntity = a.SecondmentToOrganization != null ? a.SecondmentToOrganization.Name : null,
                    a.SecondmentStartDate,
                    a.SecondmentEndDate
                })
                .ToListAsync();

            return Ok(data);
        }

        // =========================
        // GET BY PUBLIC ID
        // =========================
        [HttpGet("by-publicid/{publicId}")]
        [HasPermission("ViewEmployee")]
        public async Task<IActionResult> GetByEmployeePublicId(Guid publicId)
        {
            var a = await _context.EmployeeAdministrativeDatas
                .Include(x => x.Employee)
                .Include(x => x.JobTitle)
                .Include(x => x.Department)
                .Include(x => x.SubDepartment)
                .Include(x => x.Section)
                .Include(x => x.WorkLocation)
                .Include(x => x.JobGrade)
                .Include(x => x.TransferFromOrganization)
                .Include(x => x.SecondmentToOrganization)
                .FirstOrDefaultAsync(x => x.Employee.PublicId == publicId);

            if (a == null)
                return NotFound("لا توجد بيانات إدارية لهذا الموظف");

            return Ok(new
            {
                a.Id,
                EmployeePublicId = a.Employee.PublicId,
                FullName = a.Employee.FullName,
                a.JobStatus,

                // 🔥 IDs بدل Names (هذا المهم)
                JobTitleId = a.JobTitleId,
                DepartmentId = a.DepartmentId,
                SubDepartmentId = a.SubDepartmentId,
                SectionId = a.SectionId,
                WorkLocationId = a.WorkLocationId,
                JobGradeId = a.JobGradeId,

                // اختياري لو تبي تعرض الاسم في UI
                JobTitleName = a.JobTitle?.Name,
                DepartmentName = a.Department?.Name,
                SubDepartmentName = a.SubDepartment?.Name,
                SectionName = a.Section?.Name,
                WorkLocationName = a.WorkLocation?.Name,
                JobGradeName = a.JobGrade?.Name,

                a.StartWorkDate,
                a.LeaveBalance,
                a.ContractStartDate,
                a.ContractEndDate,
                a.AppointmentDate,
                a.TransferType,

                TransferFromEntityId = a.TransferFromOrganizationId,
                TransferStartDate = a.TransferStartDate,
                TransferEndDate = a.TransferEndDate,

                SecondmentToEntityId = a.SecondmentToOrganizationId,
                SecondmentStartDate = a.SecondmentStartDate,
                SecondmentEndDate = a.SecondmentEndDate
            });
        }

        // =========================
        // UPDATE
        // =========================
        [HttpPut("{publicId}")]
        [HasPermission("EditEmployee")]
        public async Task<IActionResult> Update(Guid publicId, CreateEmployeeAdministrativeDto dto)
        {
            var a = await _context.EmployeeAdministrativeDatas
                .FirstOrDefaultAsync(x => x.Employee.PublicId == publicId);
            if (a == null)
                return NotFound("البيانات الإدارية غير موجودة");

            var validationError = ValidateByJobStatus(dto);
            if (validationError != null)
                return BadRequest(validationError);

            a.JobStatus = dto.JobStatus;
            a.JobTitleId = dto.JobTitleId;
            a.DepartmentId = dto.DepartmentId;
            a.SubDepartmentId = dto.SubDepartmentId;
            a.SectionId = dto.SectionId;
            a.StartWorkDate = dto.StartWorkDate;
            a.WorkLocationId = dto.WorkLocationId;
            a.JobGradeId = dto.JobGradeId;
            a.LeaveBalance = dto.LeaveBalance;
            a.ContractStartDate = dto.ContractStartDate;
            a.ContractEndDate = dto.ContractEndDate;
            a.AppointmentDate = dto.AppointmentDate;
            a.TransferType = dto.TransferType;
            a.TransferFromOrganizationId = dto.TransferFromOrganizationId;
            a.TransferStartDate = dto.TransferStartDate;
            a.TransferEndDate = dto.TransferEndDate;
            a.SecondmentToOrganizationId = dto.SecondmentToOrganizationId;
            a.SecondmentStartDate = dto.SecondmentStartDate;
            a.SecondmentEndDate = dto.SecondmentEndDate;

            await _context.SaveChangesAsync();
            return Ok("تم تحديث البيانات الإدارية بنجاح");
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete("{publicId}")]
        [HasPermission("DeleteEmployee")]
        public async Task<IActionResult> Delete(Guid publicId)
        {
            var a = await _context.EmployeeAdministrativeDatas
                .FirstOrDefaultAsync(x => x.Employee.PublicId == publicId);
            if (a == null)
                return NotFound("البيانات الإدارية غير موجودة");

            _context.EmployeeAdministrativeDatas.Remove(a);
            await _context.SaveChangesAsync();

            return Ok("تم حذف البيانات الإدارية");
        }

        // =========================
        // VALIDATION
        // =========================
        private string? ValidateByJobStatus(CreateEmployeeAdministrativeDto dto)
        {
            switch (dto.JobStatus)
            {
                case JobStatus.Contract:
                    if (dto.ContractStartDate == null || dto.ContractEndDate == null)
                        return "يجب إدخال تاريخ بداية ونهاية العقد";
                    break;
                case JobStatus.Appointment:
                    if (dto.AppointmentDate == null)
                        return "يجب إدخال تاريخ التعيين";
                    break;
                case JobStatus.Transfer:
                    if (dto.TransferStartDate == null || dto.TransferEndDate == null || dto.TransferFromOrganizationId == null)
                        return "بيانات الانتداب غير مكتملة";
                    break;
                case JobStatus.Secondment:
                    if (dto.SecondmentStartDate == null || dto.SecondmentEndDate == null || dto.SecondmentToOrganizationId == null)
                        return "بيانات الإعارة غير مكتملة";
                    break;
            }
            return null;
        }

        // =========================
        // GET ALL WITH ADMIN INFO
        // =========================
        [HttpGet("all-with-admin")]
        public async Task<IActionResult> GetAllWithAdmin()
        {
            var employees = await _context.Employees.ToListAsync();
            var adminData = await _context.EmployeeAdministrativeDatas
                                           .Include(a => a.Department)
                                           .Include(a => a.JobTitle)
                                           .Include(a => a.WorkLocation)
                                           .ToListAsync();

            var result = employees.Select(emp => {
                var admin = adminData.FirstOrDefault(a => a.EmployeeId == emp.Id);

                string employmentStatus = "";
                if (admin != null)
                {
                    switch (admin.JobStatus)
                    {
                        case JobStatus.Permanent:
                            employmentStatus = "ثابت";
                            break;
                        case JobStatus.Contract:
                            employmentStatus = "متعاقد";
                            break;
                        case JobStatus.Appointment:
                            employmentStatus = "تعيين";
                            break;
                        case JobStatus.Transfer:
                            employmentStatus = "منتدب";
                            break;
                        case JobStatus.Secondment:
                            employmentStatus = "إعارة";
                            break;
                        default:
                            employmentStatus = "";
                            break;
                    }
                }

                return new
                {
                    emp.Id,
                    EmployeePublicId = emp.PublicId,
                    emp.FullName,
                    Department = admin?.Department?.Name ?? "",
                    JobTitle = admin?.JobTitle?.Name ?? "",
                    WorkLocation = admin?.WorkLocation?.Name ?? "",
                    EmploymentStatus = employmentStatus
                };
            });

            return Ok(result);
        }
    }
}
