using HRMS_Backend.Enums;

public class CreateDataUpdateDto
{
    public DataUpdateField UpdateType { get; set; }
    public string NewValue { get; set; }
    public string Reason { get; set; }
}