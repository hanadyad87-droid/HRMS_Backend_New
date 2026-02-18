public class AssignManagerDto
{
    public int EntityId { get; set; }
    public int EmployeeId { get; set; }
    public required string Type { get; set; } // كلمة required تحل المشكلة
}