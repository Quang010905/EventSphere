using System;
using System.Collections.Generic;

namespace EventSphere.Models.ModelViews
{
    // Models/ModelViews/WaitlistViewModels.cs
    public class WaitlistItemView
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? EventId { get; set; }
        public DateTime? WaitlistTime { get; set; }
        public int? Status { get; set; }

        public string EventName { get; set; } = "";

        public DateTime? EventDate { get; set; }

        public string EventVenue { get; set; } = "";

        public string StudentEmail { get; set; } = "";
        public string StudentName { get; set; } = "";
    }
        public class PagedWaitlistResult
    {
        public List<WaitlistItemView> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }
}
