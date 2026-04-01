namespace HRMS_Backend.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public string? UserName { get; set; } // 🔥 مهم للعرض

        public string Action { get; set; } = null!;
        public string EntityName { get; set; } = null!;
        public int? EntityId { get; set; }

        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? IPAddress { get; set; }

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
    }
}
