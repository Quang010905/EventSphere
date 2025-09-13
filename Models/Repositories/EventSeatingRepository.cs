using EventSphere.Models.entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventSphere.Repositories
{
    public class EventSeatingRepository
    {
        private readonly EventSphereContext _context;

        public EventSeatingRepository(EventSphereContext context)
        {
            _context = context;
        }

        // Lấy danh sách event + seating info theo filter + search
        public async Task<IEnumerable<EventWithSeatingDto>> GetEventsAsync(
        int organizerId,                      // 👈 thêm tham số
        string filter = "upcoming",
        string? keyword = null)
        {
            var now = DateTime.Now;

            var query = _context.TblEvents
                .Include(e => e.TblEventSeating)
                .Where(e => e.OrganizerId == organizerId)   // 👈 lọc theo organizer
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLowerInvariant();
                query = query.Where(e =>
                    (e.Title ?? "").ToLower().Contains(k) ||
                    (e.Category ?? "").ToLower().Contains(k) ||
                    (e.Venue ?? "").ToLower().Contains(k));
            }

            var events = await query.ToListAsync();

            var dtoList = events.Select(ev =>
            {
                var seating = ev.TblEventSeating;
                DateTime start = ev.Date?.ToDateTime(ev.Time ?? TimeOnly.MinValue) ?? DateTime.MinValue;
                bool isPast = start < now;

                return new EventWithSeatingDto
                {
                    EventId = ev.Id,
                    Title = ev.Title ?? "",
                    Date = start,
                    Time = (ev.Time ?? TimeOnly.MinValue).ToTimeSpan(),
                    TotalSeats = seating?.TotalSeats ?? 0,
                    SeatsBooked = seating?.SeatsBooked ?? 0,
                    SeatsAvailable = seating?.SeatsAvailable ?? ((seating?.TotalSeats ?? 0) - (seating?.SeatsBooked ?? 0)),
                    WaitlistEnabled = seating?.WaitlistEnabled ?? false,
                    IsPast = isPast,
                    IsUpcoming = !isPast,
                    IsOngoing = false
                };
            });

            IEnumerable<EventWithSeatingDto> filtered = filter?.ToLower() switch
            {
                "upcoming" => dtoList.Where(d => d.IsUpcoming && !d.IsPast),
                "ongoing" => dtoList.Where(d => d.IsOngoing),
                "past" => dtoList.Where(d => d.IsPast),
                "all" => dtoList,
                _ => dtoList.Where(d => d.IsUpcoming && !d.IsPast)
            };

            return filtered.OrderBy(d => d.Date).ThenBy(d => d.Time);
        }


        // Lấy thông tin seating của 1 event (dùng navigation)
        public async Task<EventWithSeatingDto?> GetSeatingByEventIdAsync(int eventId)
        {
            var ev = await _context.TblEvents
                .Include(e => e.TblEventSeating)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null) return null;

            var seating = ev.TblEventSeating;

            DateOnly? dateOnly = ev.Date;
            TimeOnly? timeOnly = ev.Time;
            var timeValue = timeOnly ?? TimeOnly.MinValue;
            var dateTimeStart = dateOnly.HasValue ? dateOnly.Value.ToDateTime(timeValue) : DateTime.MinValue;
            var timeSpan = timeValue.ToTimeSpan();

            var now = DateTime.Now;
            bool isPast = dateTimeStart < now;

            return new EventWithSeatingDto
            {
                EventId = ev.Id,
                Title = ev.Title ?? "",
                Date = dateTimeStart,
                Time = timeSpan,
                TotalSeats = seating?.TotalSeats ?? 0,
                SeatsBooked = seating?.SeatsBooked ?? 0,
                SeatsAvailable = seating?.SeatsAvailable ?? ((seating?.TotalSeats ?? 0) - (seating?.SeatsBooked ?? 0)),
                WaitlistEnabled = seating?.WaitlistEnabled ?? false,
                IsPast = isPast,
                IsUpcoming = !isPast,
                IsOngoing = false
            };
        }

        // Tăng 1 chỗ (TotalSeats += 1), cập nhật SeatsAvailable
        public async Task<bool> AddSeatAsync(int eventId)
        {
            // load event + seating (navigation)
            var ev = await _context.TblEvents
                .Include(e => e.TblEventSeating)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null) return false;

            var seating = ev.TblEventSeating;

            if (seating != null)
            {
                // có row seating rồi -> update
                seating.TotalSeats = (seating.TotalSeats ?? 0) + 1;
                seating.SeatsAvailable = (seating.TotalSeats ?? 0) - (seating.SeatsBooked ?? 0);
                if (seating.SeatsAvailable < 0) seating.SeatsAvailable = 0;

                _context.TblEventSeatings.Update(seating);
                await _context.SaveChangesAsync();
                return true;
            }

            // seating == null -> cần insert row mới.
            var seatingType = typeof(TblEventSeating);
            var eventIdProp = seatingType.GetProperty("EventId");

            if (eventIdProp != null)
            {
                // class có EventId property -> create normally
                var newSeating = new TblEventSeating
                {
                    EventId = eventId,
                    TotalSeats = 1,
                    SeatsBooked = 0,
                    SeatsAvailable = 1,
                    WaitlistEnabled = false
                };

                _context.TblEventSeatings.Add(newSeating);
                await _context.SaveChangesAsync();
                return true;
            }
            else
            {
                // Không có EventId property: chèn raw SQL (trường hợp hiếm)
                var sql = @"
                SET IDENTITY_INSERT dbo.tbl_eventSeating ON;
                INSERT INTO dbo.tbl_eventSeating (_id, _event_id, _total_seats, _seats_booked, _seats_available, _waitlist_enabled)
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5);
                SET IDENTITY_INSERT dbo.tbl_eventSeating OFF;
                ";
                await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    parameters: new object[] { eventId, eventId, 1, 0, 1, 0 });

                // detach ev để khi cần reload EF sẽ lấy row mới
                _context.Entry(ev).State = EntityState.Detached;
                return true;
            }
        }
    }

    // DTO dùng giữa repo/controller/view
    public class EventWithSeatingDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = "";
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public int TotalSeats { get; set; }
        public int SeatsBooked { get; set; }
        public int SeatsAvailable { get; set; }
        public bool WaitlistEnabled { get; set; }
        public bool IsPast { get; set; }
        public bool IsUpcoming { get; set; }
        public bool IsOngoing { get; set; }
    }
}
