using System;

namespace HRMS_Backend.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; } // 🔥 ربط بالمستخدم

        public string Action { get; set; } = null!;
        // Create, Update, Delete, Login...

        public string EntityName { get; set; } = null!;
        // Employee, Task...

        public int? EntityId { get; set; } // 🔥 مهم جدًا (أي سجل بالتحديد)

        public string? Details { get; set; } // وصف إضافي

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 🔥 أفضل

        public string? IPAddress { get; set; }

        public string? OldValues { get; set; } // 🔥 قبل التعديل (JSON)
        public string? NewValues { get; set; } // 🔥 بعد التعديل (JSON)
    }
}