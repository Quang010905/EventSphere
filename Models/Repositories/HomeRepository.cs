using EventSphere.Models.entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static EventSphere.Models.ModelViews.HomeViewModel;

namespace EventSphere.Models.Repositories
{
    public class HomeRepository
    {
        private readonly EventSphereContext _context;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public HomeRepository(EventSphereContext context)
        {
            _context = context;
        }

        public async Task<(List<EventBriefDto> Items, int Total)> SearchEventsAsync(
            string q,
            string department,
            DateOnly? startDate,
            DateOnly? endDate,
            string status,
            int page,
            int pageSize)
        {
            await _semaphore.WaitAsync();
            try
            {
                IQueryable<TblEvent> baseQuery = _context.TblEvents.AsNoTracking();

                // filter by status
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                status = (status ?? "all").ToLowerInvariant();
                if (status == "upcoming")
                {
                    baseQuery = baseQuery.Where(e => e.Date.HasValue && e.Date.Value >= today);
                }
                else if (status == "ongoing")
                {
                    baseQuery = baseQuery.Where(e => e.Date.HasValue && e.Date.Value == today);
                }
                else if (status == "past")
                {
                    baseQuery = baseQuery.Where(e => e.Date.HasValue && e.Date.Value < today);
                }

                // department/category filter
                if (!string.IsNullOrWhiteSpace(department))
                {
                    var depTrim = department.Trim();
                    baseQuery = baseQuery.Where(e => !string.IsNullOrEmpty(e.Category) && e.Category.Trim() == depTrim);
                }

                // date range filter
                if (startDate.HasValue)
                {
                    baseQuery = baseQuery.Where(e => e.Date.HasValue && e.Date.Value >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    baseQuery = baseQuery.Where(e => e.Date.HasValue && e.Date.Value <= endDate.Value);
                }

                // search q in Title, Description, Venue
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var keyword = q.Trim();
                    baseQuery = baseQuery.Where(e =>
                        (!string.IsNullOrEmpty(e.Title) && EF.Functions.Like(e.Title, $"%{keyword}%")) ||
                        (!string.IsNullOrEmpty(e.Description) && EF.Functions.Like(e.Description, $"%{keyword}%")) ||
                        (!string.IsNullOrEmpty(e.Venue) && EF.Functions.Like(e.Venue, $"%{keyword}%"))
                    );
                }

                // total count
                var total = await baseQuery.CountAsync();

                // paging & order
                var skip = Math.Max(0, (page - 1) * pageSize);
                var events = await baseQuery
                    .OrderBy(e => e.Date)
                    .ThenBy(e => e.Title)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(e => new
                    {
                        e.Id,
                        e.Title,
                        e.Date,
                        e.Time,
                        e.Image,
                        e.Venue,
                        e.Category,
                        e.Description,
                        Status = e.Status
                    })
                    .ToListAsync();

                var ids = events.Select(x => x.Id).ToList();

                // registration counts
                Dictionary<int, int> regCounts = new();
                if (ids.Any())
                {
                    try
                    {
                        regCounts = await _context.Set<TblRegistration>()
                            .AsNoTracking()
                            .Where(r => r.EventId.HasValue && ids.Contains(r.EventId.Value))
                            .GroupBy(r => r.EventId!.Value)
                            .Select(g => new { EventId = g.Key, Count = g.Count() })
                            .ToDictionaryAsync(x => x.EventId, x => x.Count);
                    }
                    catch
                    {
                        regCounts = new Dictionary<int, int>();
                    }
                }

                // seatings
                List<TblEventSeating> seatings = new();
                if (ids.Any())
                {
                    try
                    {
                        seatings = await _context.Set<TblEventSeating>()
                            .AsNoTracking()
                            .Where(s => s.EventId.HasValue && ids.Contains(s.EventId.Value))
                            .ToListAsync();
                    }
                    catch
                    {
                        seatings = new List<TblEventSeating>();
                    }
                }
                var seatingMap = seatings.GroupBy(s => s.EventId).ToDictionary(g => g.Key, g => g.First());

                int? GetCapacityFromSeating(object? seatingObj)
                {
                    if (seatingObj == null) return null;
                    var t = seatingObj.GetType();
                    string[] capacityNames = { "Capacity", "MaxSeats", "MaxCapacity", "Seats", "TotalSeats", "SeatCapacity" };
                    foreach (var nm in capacityNames)
                    {
                        var pi = t.GetProperty(nm, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (pi == null) continue;
                        var val = pi.GetValue(seatingObj);
                        if (val == null) continue;
                        try { return Convert.ToInt32(val); } catch { }
                    }
                    return null;
                }

                bool GetWaitlistFlagFromSeating(object? seatingObj)
                {
                    if (seatingObj == null) return false;
                    var t = seatingObj.GetType();
                    string[] waitlistNames = { "IsWaitlistEnabled", "AllowWaitlist", "WaitlistEnabled", "EnableWaitlist", "HasWaitlist", "IsWaitlist" };
                    foreach (var nm in waitlistNames)
                    {
                        var pi = t.GetProperty(nm, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (pi == null) continue;
                        var val = pi.GetValue(seatingObj);
                        if (val == null) continue;
                        if (val is bool b) return b;
                        if (bool.TryParse(val.ToString(), out var parsedBool)) return parsedBool;
                        if (int.TryParse(val.ToString(), out var parsedInt)) return parsedInt != 0;
                    }
                    return false;
                }

                // map to DTOs
                var result = new List<EventBriefDto>(events.Count);
                foreach (var e in events)
                {
                    var booked = regCounts.TryGetValue(e.Id, out var c) ? c : 0;

                    int? maxSeats = null;
                    bool waitlist = false;
                    if (seatingMap.TryGetValue(e.Id, out var seatingEntity) && seatingEntity != null)
                    {
                        maxSeats = GetCapacityFromSeating(seatingEntity);
                        waitlist = GetWaitlistFlagFromSeating(seatingEntity);
                    }

                    int seatsAvailableVal = 0;
                    if (maxSeats.HasValue)
                    {
                        var avail = maxSeats.Value - booked;
                        seatsAvailableVal = avail < 0 ? 0 : avail;
                    }

                    TimeOnly? timeOnly = null;
                    try
                    {
                        object? rawObj = (object?)e.Time;

                        if (rawObj == null)
                        {
                            timeOnly = null;
                        }
                        else if (rawObj is TimeOnly to)
                        {
                            timeOnly = to;
                        }
                        else if (rawObj is TimeSpan ts)
                        {
                            timeOnly = TimeOnly.FromTimeSpan(ts);
                        }
                        else if (rawObj is DateTime dt)
                        {
                            timeOnly = TimeOnly.FromDateTime(dt);
                        }
                        else
                        {
                            var s = rawObj.ToString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                if (TimeOnly.TryParse(s, out var p)) timeOnly = p;
                                else if (TimeSpan.TryParse(s, out var pts)) timeOnly = TimeOnly.FromTimeSpan(pts);
                            }
                        }
                    }
                    catch
                    {
                    }

                    result.Add(new EventBriefDto
                    {
                        Id = e.Id,
                        EventId = e.Id,
                        Title = e.Title,
                        Date = e.Date,
                        Time = timeOnly,
                        Image = e.Image,
                        Venue = e.Venue,
                        Category = e.Category,
                        Description = e.Description,
                        MaxSeats = maxSeats,
                        SeatsBooked = booked,
                        SeatsAvailable = seatsAvailableVal,
                        IsWaitlistEnabled = waitlist,
                        IsApproved = (e.Status ?? 0) == 1
                    });
                }

                return (result, total);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<TblMediaGallery>> GetLatestAsync(int top)
        {
            await _semaphore.WaitAsync();
            try
            {
                return await _context.TblMediaGalleries
                    .AsNoTracking()
                    .OrderByDescending(m => m.UploadedOn)
                    .Take(top)
                    .ToListAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetDistinctCategoriesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var cats = await _context.TblEvents
                    .AsNoTracking()
                    .Where(e => !string.IsNullOrEmpty(e.Category))
                    .Select(e => e.Category!.Trim())
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return cats.Select(c => new KeyValuePair<string, string>(c, c));
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEnumerable<int>> GetMediaYearsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return await _context.TblMediaGalleries.AsNoTracking()
                    .Where(m => m.UploadedOn.HasValue)
                    .Select(m => m.UploadedOn!.Value.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
