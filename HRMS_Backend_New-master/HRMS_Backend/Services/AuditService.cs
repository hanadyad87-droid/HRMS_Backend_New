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

        public void Log(int? userId, string action, string entityName, string details)
        {
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                Details = details,
                IPAddress = ip,
                CreatedAt = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            _context.SaveChanges();
        }
    }
}