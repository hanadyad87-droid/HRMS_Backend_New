using HRMS_Backend.Enums;
using HRMS_Backend.Models;
public class RequestSetting
{
    public int Id { get; set; }
    public RequestType RequestType { get; set; } // يستخدم القائمة اللي درناها فوق
    public int TargetSubDepartmentId { get; set; } // رقم الإدارة (ID)

    // إضافة العلاقة مع subDepartment
    public subDepartment TargetSubDepartment { get; set; }
}