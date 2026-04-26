using HRMS_Backend.Enums;

namespace HRMS_Backend.Models

{
    public class EmployeeAdministrativeData
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public JobStatus JobStatus { get; set; }

        // بيانات الوظيفة
        public int JobTitleId { get; set; }
        public JobTitle JobTitle { get; set; } = null!;
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
        public int SubDepartmentId { get; set; }
        public subDepartment SubDepartment { get; set; } = null!;
        public int SectionId { get; set; }
        public Section Section { get; set; } = null!;
        public DateTime StartWorkDate { get; set; }
        public int WorkLocationId { get; set; }
        public WorkLocation WorkLocation { get; set; } = null!;
        public int JobGradeId { get; set; }
        public JobGrade JobGrade { get; set; } = null!;
        public double LeaveBalance { get; set; }

        // بيانات العقود
        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public DateTime? AppointmentDate { get; set; }

        // ======= إضافة الحقول المفقودة =======
      // =====================
// انتداب (من جهة)
// =====================
public string? TransferType { get; set; }

public int? TransferFromOrganizationId { get; set; }
public Organization? TransferFromOrganization { get; set; }

public DateTime? TransferStartDate { get; set; }
public DateTime? TransferEndDate { get; set; }

// =====================
// إعارة (إلى جهة)
// =====================
public int? SecondmentToOrganizationId { get; set; }
public Organization? SecondmentToOrganization { get; set; }

public DateTime? SecondmentStartDate { get; set; }
public DateTime? SecondmentEndDate { get; set; }

        // علاقة الموظف
        public Employee Employee { get; set; } = null!;
    }


}
