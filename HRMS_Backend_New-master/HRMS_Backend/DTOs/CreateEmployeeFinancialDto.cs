namespace HRMS_Backend.DTOs
{
    public class CreateEmployeeFinancialDto
    {
        public Guid EmployeePublicId { get; set; }

        public int BankId { get; set; }
        public int BankBranchId { get; set; }

        public string AccountNumber { get; set; } = string.Empty;
        public string? NewAccountNumber { get; set; }

        public string AdministrativeNumber { get; set; } = string.Empty;

        public decimal BasicSalary { get; set; }

        public int JobGradeId { get; set; }
        public DateTime JobGradeDate { get; set; }

        public decimal Allowance { get; set; }
        public DateTime AllowanceDate { get; set; }

        public decimal CurrentLinkedSalary { get; set; }
        public DateTime CurrentLinkedSalaryDate { get; set; }

        public int DelegatedGradeId { get; set; }
        public DateTime DelegatedGradeDate { get; set; }
    }
}
