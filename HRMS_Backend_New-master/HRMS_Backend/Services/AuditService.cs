using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Http;
using System;

namespace HRMS_Backend.Services
{
    public class AuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public void Log(int? userId, string action, string entity, string details, string? ip = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entity,
                Details = details,
                IPAddress = ip,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            _context.SaveChanges();
        }
    }
}