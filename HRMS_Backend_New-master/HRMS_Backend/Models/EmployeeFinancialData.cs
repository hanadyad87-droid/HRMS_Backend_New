namespace HRMS_Backend.Models
{
    public class EmployeeFinancialData
    {
        public int Id { get; set; }

        // ================= الربط بالموظف =================
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        // ================= المصرف =================
        public int BankId { get; set; }
        public Bank Bank { get; set; } = null!;

        public int BankBranchId { get; set; }
        public BankBranch BankBranch { get; set; } = null!;

        public string AccountNumber { get; set; } = string.Empty;
        public string? NewAccountNumber { get; set; }

        // ================= بيانات إدارية =================
        public string AdministrativeNumber { get; set; } = string.Empty;

        // ================= الراتب =================
        public decimal BasicSalary { get; set; }

        // ================= الدرجة الوظيفية =================
        public int JobGradeId { get; set; }
        public JobGrade JobGrade { get; set; } = null!;
        public DateTime JobGradeDate { get; set; }

        // ================= العلاوة =================
        public decimal Allowance { get; set; }
        public DateTime AllowanceDate { get; set; }

        // ================= المربوط الحالي =================
        public decimal CurrentLinkedSalary { get; set; }
        public DateTime CurrentLinkedSalaryDate { get; set; }

        // ================= الدرجة المنتدب إليها =================
        public int DelegatedGradeId { get; set; }
        public JobGrade DelegatedGrade { get; set; } = null!;
        public DateTime DelegatedGradeDate { get; set; }
    }
}
