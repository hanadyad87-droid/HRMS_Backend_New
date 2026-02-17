using HRMS_Backend.Enums;

namespace HRMS_Backend.DTOs
{
    public class CreateEmployeeAdministrativeDto
    {
        public Guid EmployeePublicId { get; set; }

        public JobStatus JobStatus { get; set; }

        // روابط بـ Lookup Tables
        public int JobTitleId { get; set; }        // مرتبط بجدول JobTitles
        public int DepartmentId { get; set; }      // مرتبط بجدول Departments
        public int SubDepartmentId { get; set; }   // مرتبط بجدول SubDepartments
        public int SectionId { get; set; }         // مرتبط بجدول Sections
        public DateTime StartWorkDate { get; set; }
        public int WorkLocationId { get; set; }    // مرتبط بجدول WorkLocations
        public int JobGradeId { get; set; }        // مرتبط بجدول JobGrades
        public int LeaveBalance { get; set; }

        // متعاقد
        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }

        // تعيين
        public DateTime? AppointmentDate { get; set; }

        // منتدب
        public string? TransferType { get; set; }
        public int? TransferFromEntityId { get; set; } // لو مرتبط بجهة
        public DateTime? TransferStartDate { get; set; }
        public DateTime? TransferEndDate { get; set; }

        // إعارة
        public int? SecondmentToEntityId { get; set; } // لو مرتبط بجهة
        public DateTime? SecondmentStartDate { get; set; }
        public DateTime? SecondmentEndDate { get; set; }

    }
}
