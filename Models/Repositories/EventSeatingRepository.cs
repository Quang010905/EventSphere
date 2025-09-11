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
            string filter = "upcoming", // "upcoming", "ongoing", "past", "all"
            string? keyword = null)
        {
            var now = DateTime.Now;

            // Lấy events kèm seating thông qua navigation property
            var events = await _context.TblEvents
                .Include(e => e.TblEventSeating)
                .AsNoTracking()
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLowerInvariant();
                events = events.Where(e =>
                       (e.Title ?? "").ToLowerInvariant().Contains(k)
                    || (e.Category ?? "").ToLowerInvariant().Contains(k)
                    || (e.Venue ?? "").ToLowerInvariant().Contains(k)
                ).ToList();
            }

            var dtoList = events.Select(ev =>
            {
                // lấy seating từ navigation (có thể null)
                var seating = ev.TblEventSeating;

                // chuyển DateOnly?/TimeOnly? -> DateTime + TimeSpan (an toàn)
                DateOnly? dateOnly = ev.Date;
                TimeOnly? timeOnly = ev.Time;
                var timeValue = timeOnly ?? TimeOnly.MinValue;
                var dateTimeStart = dateOnly.HasValue ? dateOnly.Value.ToDateTime(timeValue) : DateTime.MinValue;
                var timeSpan = timeValue.ToTimeSpan();

                bool isPast = dateTimeStart < now;
                bool isUpcoming = !isPast;
                bool isOngoing = false; // cần duration nếu muốn chính xác

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
                    IsUpcoming = isUpcoming,
                    IsOngoing = isOngoing
                };
            }).ToList();

            // Áp filter
            IEnumerable<EventWithSeatingDto> filtered = dtoList;
            filter = (filter ?? "upcoming").ToLowerInvariant();
            switch (filter)
            {
                case "upcoming":
                    filtered = dtoList.Where(d => d.IsUpcoming && !d.IsPast);
                    break;
                case "ongoing":
                    filtered = dtoList.Where(d => d.IsOngoing);
                    break;
                case "past":
                    filtered = dtoList.Where(d => d.IsPast);
                    break;
                case "all":
                    filtered = dtoList;
                    break;
                default:
                    filtered = dtoList.Where(d => d.IsUpcoming && !d.IsPast);
                    break;
            }

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
