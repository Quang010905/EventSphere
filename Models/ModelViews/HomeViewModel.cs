using EventSphere.Models.entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventSphere.Models.ModelViews
{
    public class HomeViewModel
    {
        public IEnumerable<EventBriefDto> UpcomingEvents { get; set; } = new List<EventBriefDto>();
        public IEnumerable<TblMediaGallery> LatestMedia { get; set; } = new List<TblMediaGallery>();
        public IEnumerable<KeyValuePair<string, string>> Categories { get; set; } = new List<KeyValuePair<string, string>>();
        public IEnumerable<int> MediaYears { get; set; } = new List<int>();
        public IEnumerable<Announcement> SiteAnnouncements { get; set; } = new List<Announcement>();

        // Paging + Filter states
        public int TotalItems { get; set; } = 0;
        public int EventsPageSize { get; set; } = 6;
        public int CurrentPage { get; set; } = 1;

        // Search + filter model
        public string SearchQuery { get; set; } = string.Empty;
        public string SelectedDepartment { get; set; } = string.Empty;
        public string SelectedStatus { get; set; } = "all"; // all | upcoming | ongoing | past
        public string StartDateStr { get; set; } = string.Empty; // yyyy-MM-dd
        public string EndDateStr { get; set; } = string.Empty;

        public bool IsAuthenticated { get; set; } = false;

        public IEnumerable<KeyValuePair<string, string>> Departments
        {
            get => Categories ?? Enumerable.Empty<KeyValuePair<string, string>>();
            set => Categories = value;
        }

        public List<int> GetMediaYearsFromLatestMedia()
        {
            if (LatestMedia == null) return new List<int>();
            return LatestMedia
                .Select(m => m.UploadedOn?.Year ?? 0)
                .Where(y => y > 0)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
        }

        public class Announcement
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? ShortText { get; set; }
            public string? Link { get; set; }
            public DateTime? StartAt { get; set; }
            public DateTime? EndAt { get; set; }
        }

        public class EventBriefDto
        {
            public int Id { get; set; }
            public int EventId { get; set; }
            public string? Title { get; set; }
            public DateOnly? Date { get; set; }
            public TimeOnly? Time { get; set; }
            public string? Image { get; set; }
            public string? Venue { get; set; }
            public string? Category { get; set; }
            public string? Description { get; set; }

            public int? MaxSeats { get; set; }
            public int SeatsBooked { get; set; }
            public int SeatsAvailable { get; set; }

            public bool IsWaitlistEnabled { get; set; }
            public bool IsApproved { get; set; }
        }
    }
}
