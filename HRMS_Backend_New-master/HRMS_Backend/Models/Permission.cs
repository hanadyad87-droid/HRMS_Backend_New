namespace HRMS_Backend.Models
{
    public class Permission
    {
        public int Id { get; set; }
        public string PermissionName { get; set; } = null!;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
