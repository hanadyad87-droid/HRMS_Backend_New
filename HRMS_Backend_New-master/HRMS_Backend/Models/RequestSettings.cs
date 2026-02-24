using HRMS_Backend.Enums;

public class RequestSetting
{
    public int Id { get; set; }
    public RequestType RequestType { get; set; } // يستخدم القائمة اللي درناها فوق
    public int TargetSubDepartmentId { get; set; } // رقم الإدارة (ID)
}