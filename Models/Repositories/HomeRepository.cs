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

        // Semaphore để đảm bảo chỉ có 1 thao tác EF chạy trên context này tại 1 thời điểm
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public HomeRepository(EventSphereContext context)
        {
            _context = context;
        }

        // ví dụ method (lưu ý: luôn lock trước khi dùng _context)
        public async Task<List<EventBriefDto>> GetUpcomingEventBriefsAsync(int top = 6)
        {
            await _semaphore.WaitAsync();
            try
            {
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                var events = await _context.TblEvents
                    .AsNoTracking()
                    .Where(e => e.Date.HasValue && e.Status == 1 && e.Date.Value >= today)
                    .OrderBy(e => e.Date)
                    .Take(top)
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
                if (!ids.Any()) return new List<EventBriefDto>();

                // registrations counts
                Dictionary<int, int> regCounts;
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

                // seatings
                List<TblEventSeating> seatings;
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

                    int? seatsAvailable = null;
                    if (maxSeats.HasValue)
                    {
                        var avail = maxSeats.Value - booked;
                        seatsAvailable = avail < 0 ? 0 : avail;
                    }

                    result.Add(new EventBriefDto
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Date = e.Date,
                        Time = e.Time,
                        Image = e.Image,
                        Venue = e.Venue,
                        Category = e.Category,
                        Description = e.Description,
                        MaxSeats = maxSeats,
                        SeatsBooked = booked,
                        SeatsAvailable = seatsAvailable ?? 0,
                        IsWaitlistEnabled = waitlist,
                        IsApproved = (e.Status ?? 0) == 1,
                        EventId = e.Id
                    });
                }

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // Fix GetLatestAsync with same semaphore
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

        // Fix other methods similarly
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
                return await _context.TblMediaGalleries
                    .AsNoTracking()
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
