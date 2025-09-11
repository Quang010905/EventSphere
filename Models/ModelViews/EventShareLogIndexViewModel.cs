using System;
using System.Collections.Generic;
using EventSphere.Models.entities;

namespace EventSphere.Models.ViewModels
{
    public class EventShareLogIndexViewModel
    {
        public IEnumerable<TblEventShareLog> Items { get; set; } = new List<TblEventShareLog>();
        public int Total { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public int? EventId { get; set; }
        public string? Platform { get; set; }
        public string? From { get; set; }   // yyyy-MM-dd
        public string? To { get; set; }     // yyyy-MM-dd
        public string? Keyword { get; set; }

        // for event dropdown
        public List<(int Id, string Title)> Events { get; set; } = new();
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    }
}
