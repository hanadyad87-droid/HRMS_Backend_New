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
        public IActionResult Add(CreateEmployeeAdministrativeDto dto)
        {
            var employee = _context.Employees
                .FirstOrDefault(e => e.PublicId == dto.EmployeePublicId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            var existing = _context.EmployeeAdministrativeDatas
                .FirstOrDefault(a => a.EmployeeId == employee.Id);

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
                TransferFromEntityId = dto.TransferFromEntityId,
                TransferStartDate = dto.TransferStartDate,
                TransferEndDate = dto.TransferEndDate,

                SecondmentToEntityId = dto.SecondmentToEntityId,
                SecondmentStartDate = dto.SecondmentStartDate,
                SecondmentEndDate = dto.SecondmentEndDate
            };

            _context.EmployeeAdministrativeDatas.Add(data);
            _context.SaveChanges();

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
        public IActionResult GetMyAdministrativeData()
        {
            var username = User.Identity!.Name;

            var employee = _context.Employees
                .Include(e => e.User)
                .Include(e => e.AdministrativeData)
                    .ThenInclude(a => a.TransferFromEntity)
                .Include(e => e.AdministrativeData)
                    .ThenInclude(a => a.SecondmentToEntity)
                .FirstOrDefault(e => e.User.Username == username);

            if (employee == null || employee.AdministrativeData == null)
                return NotFound("الموظف غير موجود أو لا توجد بيانات إدارية");

            var a = employee.AdministrativeData;

            _context.Entry(a).Reference(x => x.JobTitle).Load();
            _context.Entry(a).Reference(x => x.Department).Load();
            _context.Entry(a).Reference(x => x.SubDepartment).Load();
            _context.Entry(a).Reference(x => x.Section).Load();
            _context.Entry(a).Reference(x => x.WorkLocation).Load();
            _context.Entry(a).Reference(x => x.JobGrade).Load();

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
                TransferFromEntity = a.TransferFromEntity?.FullName,
                a.TransferStartDate,
                a.TransferEndDate,

                SecondmentToEntity = a.SecondmentToEntity?.FullName,
                a.SecondmentStartDate,
                a.SecondmentEndDate
            });
        }

        // =========================
        // GET ALL
        // =========================
        [HttpGet("get-all")]
        [HasPermission("ViewEmployee")]
        public IActionResult GetAllAdministrativeData()
        {
            var data = _context.EmployeeAdministrativeDatas
                .Include(a => a.Employee)
                .Include(a => a.JobTitle)
                .Include(a => a.Department)
                .Include(a => a.SubDepartment)
                .Include(a => a.Section)
                .Include(a => a.WorkLocation)
                .Include(a => a.JobGrade)
                .Include(a => a.TransferFromEntity)
                .Include(a => a.SecondmentToEntity)
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
                    TransferFromEntity = a.TransferFromEntity != null ? a.TransferFromEntity.FullName : null,
                    a.TransferStartDate,
                    a.TransferEndDate,
                    SecondmentToEntity = a.SecondmentToEntity != null ? a.SecondmentToEntity.FullName : null,
                    a.SecondmentStartDate,
                    a.SecondmentEndDate
                })
                .ToList();

            return Ok(data);
        }

        // =========================
        // GET BY PUBLIC ID
        // =========================
        [HttpGet("by-publicid/{publicId}")]
        [HasPermission("ViewEmployee")]
        public IActionResult GetByEmployeePublicId(Guid publicId)
        {
            var a = _context.EmployeeAdministrativeDatas
                .Include(x => x.Employee)
                .Include(x => x.JobTitle)
                .Include(x => x.Department)
                .Include(x => x.SubDepartment)
                .Include(x => x.Section)
                .Include(x => x.WorkLocation)
                .Include(x => x.JobGrade)
                .Include(x => x.TransferFromEntity)
                .Include(x => x.SecondmentToEntity)
                .FirstOrDefault(x => x.Employee.PublicId == publicId);

            if (a == null)
                return NotFound("لا توجد بيانات إدارية لهذا الموظف");

            return Ok(new
            {
                a.Id,
                EmployeePublicId = a.Employee.PublicId,
                FullName = a.Employee.FullName,
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
                TransferFromEntity = a.TransferFromEntity?.FullName,
                a.TransferStartDate,
                a.TransferEndDate,
                SecondmentToEntity = a.SecondmentToEntity?.FullName,
                a.SecondmentStartDate,
                a.SecondmentEndDate
            });
        }

        // =========================
        // UPDATE
        // =========================
        [HttpPut("{publicId}")]
        [HasPermission("EditEmployee")]
        public IActionResult Update(Guid publicId, CreateEmployeeAdministrativeDto dto)
        {
            var a = _context.EmployeeAdministrativeDatas
                .FirstOrDefault(x => x.Employee.PublicId == publicId);
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
            a.TransferFromEntityId = dto.TransferFromEntityId;
            a.TransferStartDate = dto.TransferStartDate;
            a.TransferEndDate = dto.TransferEndDate;
            a.SecondmentToEntityId = dto.SecondmentToEntityId;
            a.SecondmentStartDate = dto.SecondmentStartDate;
            a.SecondmentEndDate = dto.SecondmentEndDate;

            _context.SaveChanges();
            return Ok("تم تحديث البيانات الإدارية بنجاح");
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete("{publicId}")]
        [HasPermission("DeleteEmployee")]
        public IActionResult Delete(Guid publicId)
        {
            var a = _context.EmployeeAdministrativeDatas
                .FirstOrDefault(x => x.Employee.PublicId == publicId);
            if (a == null)
                return NotFound("البيانات الإدارية غير موجودة");

            _context.EmployeeAdministrativeDatas.Remove(a);
            _context.SaveChanges();

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
                    if (dto.TransferStartDate == null || dto.TransferEndDate == null || dto.TransferFromEntityId == null)
                        return "بيانات الانتداب غير مكتملة";
                    break;
                case JobStatus.Secondment:
                    if (dto.SecondmentStartDate == null || dto.SecondmentEndDate == null || dto.SecondmentToEntityId == null)
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
