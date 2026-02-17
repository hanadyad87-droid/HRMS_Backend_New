using HRMS_Backend.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    // علاقة many-to-many مع Roles
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // علاقة واحد-لكثير مع Employee
    public Employee? Employee { get; set; }
}
