using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventSphere.Models.entities;
using EventSphere.Models.ViewModels;
using EventSphere.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EventShareLogController : Controller
    {
        private readonly EventShareLogRepository _repo;

        public EventShareLogController(EventShareLogRepository repo)
        {
            _repo = repo;
        }

        // GET: Admin/EventShareLog
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, int? eventId = null, string? platform = null, string? from = null, string? to = null, string? keyword = null)
        {
            DateTime? dtFrom = null;
            DateTime? dtTo = null;
            if (DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tmpFrom))
                dtFrom = tmpFrom;
            if (DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tmpTo))
                dtTo = tmpTo.AddDays(1).AddSeconds(-1); // include whole day

            var (items, total) = await _repo.QueryPagedAsync(page, pageSize, eventId, platform, dtFrom, dtTo, keyword);

            // Events dropdown
            var events = await _repo.GetEventsAsync();
            var vm = new EventShareLogIndexViewModel
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize,
                EventId = eventId,
                Platform = platform,
                From = from,
                To = to,
                Keyword = keyword,
                Events = events
            };

            ViewBag.Platforms = await _repo.GetDistinctPlatformsAsync();
            return View(vm);
        }

        // Export CSV using same filters
        public async Task<IActionResult> ExportCsv(int? eventId = null, string? platform = null, string? from = null, string? to = null, string? keyword = null)
        {
            DateTime? dtFrom = null;
            DateTime? dtTo = null;
            if (DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tmpFrom))
                dtFrom = tmpFrom;
            if (DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tmpTo))
                dtTo = tmpTo.AddDays(1).AddSeconds(-1);

            var (items, total) = await _repo.QueryPagedAsync(1, int.MaxValue, eventId, platform, dtFrom, dtTo, keyword);

            var sb = new StringBuilder();
            sb.AppendLine("Id,EventId,EventTitle,UserId,UserName,Platform,ShareTimestamp,Message");
            foreach (var r in items)
            {
                var userName = r.User?.TblUserDetails?.FirstOrDefault()?.Fullname?.Replace("\"", "\"\"") ?? $"User {r.UserId}";
                var evTitle = r.Event?.Title?.Replace("\"", "\"\"") ?? $"Event {r.EventId}";
                var msg = (r.ShareMessage ?? "").Replace("\"", "\"\"");
                sb.AppendLine($"{r.Id},{r.EventId},\"{evTitle}\",{r.UserId},\"{userName}\",\"{r.Platform}\",{r.ShareTimestamp:yyyy-MM-dd HH:mm:ss},\"{msg}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"event_share_logs_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }
    }
}
