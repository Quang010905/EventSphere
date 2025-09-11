using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSphere.Models.entities;
using Microsoft.EntityFrameworkCore;

namespace EventSphere.Repositories
{
    public class EventShareLogRepository
    {
        private readonly EventSphereContext _context;

        public EventShareLogRepository(EventSphereContext context)
        {
            _context = context;
        }

        // Add a new share log
        public async Task<TblEventShareLog> AddAsync(int userId, int eventId, string platform, string? message = null)
        {
            var entity = new TblEventShareLog
            {
                UserId = userId,
                EventId = eventId,
                Platform = platform,
                ShareMessage = message,
                ShareTimestamp = DateTime.Now
            };

            _context.TblEventShareLogs.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // Query with filters + paging
        public async Task<(IEnumerable<TblEventShareLog> items, int total)> QueryPagedAsync(
            int page = 1, int pageSize = 20,
            int? eventId = null, string? platform = null,
            DateTime? from = null, DateTime? to = null,
            string? keyword = null)
        {
            var q = _context.TblEventShareLogs
                .Include(x => x.Event)
                .Include(x => x.User)
                    .ThenInclude(u => u.TblUserDetails)
                .AsQueryable();

            if (eventId.HasValue)
                q = q.Where(x => x.EventId == eventId.Value);

            if (!string.IsNullOrWhiteSpace(platform))
                q = q.Where(x => x.Platform == platform);

            if (from.HasValue)
                q = q.Where(x => x.ShareTimestamp >= from.Value);

            if (to.HasValue)
                q = q.Where(x => x.ShareTimestamp <= to.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                q = q.Where(x =>
                    (x.Platform != null && EF.Functions.Like(x.Platform, $"%{k}%")) ||
                    (x.ShareMessage != null && EF.Functions.Like(x.ShareMessage, $"%{k}%")) ||
                    (x.Event != null && EF.Functions.Like(x.Event.Title, $"%{k}%")) ||
                    (x.User != null && x.User.TblUserDetails.Any(d => EF.Functions.Like(d.Fullname, $"%{k}%")))
                );
            }

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(x => x.ShareTimestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        // distinct platforms
        public async Task<List<string>> GetDistinctPlatformsAsync()
        {
            return await _context.TblEventShareLogs
                .Select(x => x.Platform)
                .Where(p => p != null)
                .Distinct()
                .ToListAsync();
        }

        // Updated method to fix CS8143: An expression tree may not contain a tuple literal.
        public async Task<List<(int Id, string Title)>> GetEventsAsync()
        {
            return await _context.TblEvents
                .OrderBy(e => e.Title)
                .Select(e => new { e.Id, e.Title }) // Anonymous type used here
                .AsNoTracking()
                .ToListAsync()
                .ContinueWith(task => task.Result.Select(x => (x.Id, x.Title ?? $"Event {x.Id}")).ToList());
        }
    }
}
