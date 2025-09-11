using EventSphere.Models.entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventSphere.Models.Repositories
{
    public class HomeRepository
    {
        private readonly EventSphereContext _context;

        public HomeRepository(EventSphereContext context)
        {
            _context = context;
        }

        public async Task<List<TblEvent>> GetUpcomingEventsAsync(int top = 6)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            return await _context.TblEvents
                .Where(e => e.Date.HasValue
                            && e.Status == 1
                            && e.Date.Value >= today)
                .OrderBy(e => e.Date)
                .Take(top)
                .ToListAsync();
        }

        public async Task<List<TblMediaGallery>> GetLatestMediaAsync(int top = 6)
        {
            return await _context.TblMediaGalleries
                .OrderByDescending(m => m.UploadedOn)
                .Take(top)
                .ToListAsync();
        }
    }
}
