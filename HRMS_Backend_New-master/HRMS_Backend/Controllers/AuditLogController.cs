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
                .Include(x => x.User) // 🔥 باش نجيب الاسم
                .AsQueryable();

            // 🔍 Filters
            if (query.UserId.HasValue)
                logsQuery = logsQuery.Where(x => x.UserId == query.UserId);

            if (!string.IsNullOrEmpty(query.Action))
                logsQuery = logsQuery.Where(x => x.Action == query.Action);

            if (!string.IsNullOrEmpty(query.EntityName))
                logsQuery = logsQuery.Where(x => x.EntityName.Contains(query.EntityName));

            if (query.FromDate.HasValue)
                logsQuery = logsQuery.Where(x => x.CreatedAt >= query.FromDate);

            if (query.ToDate.HasValue)
                logsQuery = logsQuery.Where(x => x.CreatedAt <= query.ToDate);

            var totalCount = await logsQuery.CountAsync();

            var data = await logsQuery
                .OrderByDescending(x => x.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new AuditLogDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    UserName = x.User != null ? x.User.Username : null,

                    Action = x.Action,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,

                    Details = x.Details,
                    CreatedAt = x.CreatedAt,
                    IPAddress = x.IPAddress,

                    OldValues = x.OldValues,
                    NewValues = x.NewValues
                })
                .ToListAsync();

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
            var logs = await _context.AuditLogs
                .Include(x => x.User)
                .Where(x => x.User != null && x.User.Username == username)
                .Select(x => new AuditLogDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    UserName = x.User.Username,

                    Action = x.Action,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,

                    Details = x.Details,
                    CreatedAt = x.CreatedAt,
                    IPAddress = x.IPAddress,

                    OldValues = x.OldValues,
                    NewValues = x.NewValues
                })
                .ToListAsync();

            if (logs == null || !logs.Any())
                return NotFound("No logs found for this user");

            return Ok(logs);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var log = await _context.AuditLogs.FindAsync(id);

            if (log == null)
                return NotFound();

            _context.AuditLogs.Remove(log);
            await _context.SaveChangesAsync();

            return Ok("Deleted");
        }
    }
}