using HRMS_Backend.Data;
using HRMS_Backend.DTOs;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] AuditLogQuery query)
        {
            var logsQuery = _context.AuditLogs
                .Include(x => x.User)
                .AsQueryable();

            // 🔍 الفلاتر (كما هي)
            if (query.UserId.HasValue)
                logsQuery = logsQuery.Where(x => x.UserId == query.UserId);

            if (query.FromDate.HasValue)
            {
                logsQuery = logsQuery.Where(x => x.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                logsQuery = logsQuery.Where(x => x.CreatedAt <= query.ToDate.Value);
            }

            var totalCount = await logsQuery.CountAsync();

            // 1. جلب البيانات من قاعدة البيانات أولاً (بدون تحويل الوقت داخل الـ Select)
            var rawData = await logsQuery
                .OrderByDescending(x => x.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(); // نفذنا الاستعلام هنا وجبنا البيانات للذاكرة (Memory)

            // 2. تحويل الوقت في الذاكرة باستخدام C#
            TimeZoneInfo libyaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Libya Standard Time");

            var data = rawData.Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.User != null ? x.User.Username : null,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Details = x.Details,
                // التحويل هنا آمن لأننا في الذاكرة وليس في SQL
                CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(x.CreatedAt, libyaTimeZone),
                IPAddress = x.IPAddress,
                OldValues = x.OldValues,
                NewValues = x.NewValues
            }).ToList();

            return Ok(new
            {
                data,
                totalCount,
                page = query.Page,
                pageSize = query.PageSize
            });
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("by-username/{username}")]
        public async Task<IActionResult> GetByUsername(string username)
        {
            var rawLogs = await _context.AuditLogs
                .Include(x => x.User)
                .Where(x => x.User != null && x.User.Username == username)
                .ToListAsync(); // جلبناهم للذاكرة أولاً

            if (rawLogs == null || !rawLogs.Any())
                return NotFound("No logs found for this user");

            TimeZoneInfo libyaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Libya Standard Time");

            var logs = rawLogs.Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.User?.Username,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Details = x.Details,
                CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(x.CreatedAt, libyaTimeZone), // تحويل الوقت
                IPAddress = x.IPAddress,
                OldValues = x.OldValues,
                NewValues = x.NewValues
            }).ToList();

            return Ok(logs);
        }


    }
}