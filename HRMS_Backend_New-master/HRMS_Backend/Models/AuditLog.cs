using System;

namespace HRMS_Backend.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public string Action { get; set; } // Login, Logout, Create, Update, Delete

        public string EntityName { get; set; } // Employee, Task, etc

        public string Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string IPAddress { get; set; }
    }
}