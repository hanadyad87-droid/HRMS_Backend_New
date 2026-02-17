using HRMS_Backend.Models;

public class Role
{

    public int Id { get; set; }
    public string RoleName { get; set; } = null!;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}