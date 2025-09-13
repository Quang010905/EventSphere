using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventSphere.Models.entities;
using System.Threading.Tasks;
using System.Linq;

namespace EventSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EventsAdminController : Controller
    {
        private readonly EventSphereContext _context;

        public EventsAdminController(EventSphereContext context)
        {
            _context = context;
        }

        // Danh sách sự kiện chưa duyệt
        public async Task<IActionResult> Index()
        {
            var events = await _context.TblEvents
                .Include(e => e.Organizer)
                .Where(e => e.Status == 0 || e.Status == null) // chỉ lấy sự kiện chưa duyệt
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            return View(events);
        }

        // Duyệt sự kiện
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var ev = await _context.TblEvents.FindAsync(id);
            if (ev == null) return Json(new { success = false });

            ev.Status = 1; // Đã duyệt
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Chi tiết sự kiện
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.TblEvents
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            return View(ev);
        }
    }
}
