namespace HRMS_Backend.Models
{
    public class UserPermission
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        // هل مسموح له (true) أم ممنوع (false)
        public bool IsAllowed { get; set; }
    }
}