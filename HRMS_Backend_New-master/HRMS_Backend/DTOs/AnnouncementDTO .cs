public class AnnouncementDTO
{
    public string Title { get; set; }
    public string Message { get; set; }
    public bool TargetAll { get; set; } = true;
    public int? TargetDepartmentId { get; set; }
    public bool Active { get; set; } = true;
}
