using System;

namespace HRMS_Backend.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        public string Action { get; set; } = null!;
        public string EntityName { get; set; } = null!;
        public int? EntityId { get; set; }

        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? IPAddress { get; set; }

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        // ✨ جديد
        public string? ChangedColumns { get; set; }
    }
}