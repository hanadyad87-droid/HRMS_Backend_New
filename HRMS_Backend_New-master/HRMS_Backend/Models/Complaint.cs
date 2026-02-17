using HRMS_Backend.Enums;
using HRMS_Backend.Models;

public class Complaint
{
    public int Id { get; set; }

    public int? EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public int? DepartmentId { get; set; } // لو شكوى لإدارة معينة
    public Department? Department { get; set; }

    public bool IsForAllDepartments { get; set; } // لو لجميع الإدارات
    public bool IsAnonymous { get; set; } // <--- اضفت السطر ده

    public string Content { get; set; }
    public string? AttachmentPath { get; set; }

    public ComplaintStatus Status { get; set; }
    public string? Notes { get; set; }

    public int? HandledByManagerId { get; set; } // من حجز الشكوى (في حالة جميع الإدارات)

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
