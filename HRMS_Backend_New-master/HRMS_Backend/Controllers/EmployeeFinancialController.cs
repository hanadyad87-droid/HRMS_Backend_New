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
    public class EmployeeFinancialController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployeeFinancialController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== Create or Update ====================
        [HttpPost("create-or-update")]
        public IActionResult CreateOrUpdate([FromBody] CreateEmployeeFinancialDto dto)
        {
            var employee = _context.Employees
                .Include(e => e.FinancialData)
                .FirstOrDefault(e => e.PublicId == dto.EmployeePublicId);

            if (employee == null)
                return NotFound("الموظف غير موجود");

            if (employee.FinancialData == null)
            {
                employee.FinancialData = new EmployeeFinancialData
                {
                    EmployeeId = employee.Id
                };
                _context.EmployeeFinancialDatas.Add(employee.FinancialData);
            }

            var data = employee.FinancialData;

            // ===== المصرف =====
            data.BankId = dto.BankId;
            data.BankBranchId = dto.BankBranchId;
            data.AccountNumber = dto.AccountNumber;
            data.NewAccountNumber = dto.NewAccountNumber;

            // ===== بيانات إدارية =====
            data.AdministrativeNumber = dto.AdministrativeNumber;

            // ===== الراتب =====
            data.BasicSalary = dto.BasicSalary;

            // ===== الدرجة الوظيفية =====
            data.JobGradeId = dto.JobGradeId;
            data.JobGradeDate = dto.JobGradeDate;

            // ===== العلاوة =====
            data.Allowance = dto.Allowance;
            data.AllowanceDate = dto.AllowanceDate;

            // ===== المربوط الحالي =====
            data.CurrentLinkedSalary = dto.CurrentLinkedSalary;
            data.CurrentLinkedSalaryDate = dto.CurrentLinkedSalaryDate;

            // ===== الدرجة المنتدب إليها =====
            data.DelegatedGradeId = dto.DelegatedGradeId;
            data.DelegatedGradeDate = dto.DelegatedGradeDate;

            _context.SaveChanges();

            return Ok(new
            {
                message = "تم حفظ البيانات المالية بنجاح",
                EmployeePublicId = employee.PublicId
            });
        }

        // ==================== Get By PublicId ====================
        [HttpGet("employee/{publicId}")]
        public IActionResult GetByEmployee(Guid publicId)
        {
            var data = _context.EmployeeFinancialDatas
                .Include(f => f.Employee)
                .Where(f => f.Employee.PublicId == publicId)
                .Select(f => new
                {
                    EmployeePublicId = f.Employee.PublicId,

                    // المصرف
                    f.BankId,
                    f.BankBranchId,
                    f.AccountNumber,
                    f.NewAccountNumber,

                    // إداري
                    f.AdministrativeNumber,

                    // الراتب
                    f.BasicSalary,

                    // الدرجة
                    f.JobGradeId,
                    f.JobGradeDate,

                    // العلاوة
                    f.Allowance,
                    f.AllowanceDate,

                    // المربوط
                    f.CurrentLinkedSalary,
                    f.CurrentLinkedSalaryDate,

                    // الانتداب
                    f.DelegatedGradeId,
                    f.DelegatedGradeDate
                })
                .FirstOrDefault();

            if (data == null)
                return NotFound("لا توجد بيانات مالية لهذا الموظف");

            return Ok(data);
        }

        // ==================== UPDATE BY PUBLICID ====================
        [HttpPut("employee/{publicId}")]
        public IActionResult UpdateByEmployee(Guid publicId, [FromBody] CreateEmployeeFinancialDto dto)
        {
            var data = _context.EmployeeFinancialDatas
                .Include(f => f.Employee)
                .FirstOrDefault(f => f.Employee.PublicId == publicId);

            if (data == null)
                return NotFound("لا توجد بيانات مالية لهذا الموظف");

            // ===== المصرف =====
            data.BankId = dto.BankId;
            data.BankBranchId = dto.BankBranchId;
            data.AccountNumber = dto.AccountNumber;
            data.NewAccountNumber = dto.NewAccountNumber;

            // ===== بيانات إدارية =====
            data.AdministrativeNumber = dto.AdministrativeNumber;

            // ===== الراتب =====
            data.BasicSalary = dto.BasicSalary;

            // ===== الدرجة الوظيفية =====
            data.JobGradeId = dto.JobGradeId;
            data.JobGradeDate = dto.JobGradeDate;

            // ===== العلاوة =====
            data.Allowance = dto.Allowance;
            data.AllowanceDate = dto.AllowanceDate;

            // ===== المربوط الحالي =====
            data.CurrentLinkedSalary = dto.CurrentLinkedSalary;
            data.CurrentLinkedSalaryDate = dto.CurrentLinkedSalaryDate;

            // ===== الدرجة المنتدب إليها =====
            data.DelegatedGradeId = dto.DelegatedGradeId;
            data.DelegatedGradeDate = dto.DelegatedGradeDate;

            _context.SaveChanges();

            return Ok(new
            {
                message = "تم تحديث البيانات المالية بنجاح",
                EmployeePublicId = data.Employee.PublicId
            });
        }
    }
}
