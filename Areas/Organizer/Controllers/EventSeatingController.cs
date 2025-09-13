
using EventSphere.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class EventSeatingController : Controller
    {
        private readonly EventSeatingRepository _repo;

        public EventSeatingController(EventSeatingRepository repo)
        {
            _repo = repo;
        }

        // view: list events as buttons
        // filter: upcoming | ongoing | past | all
        public async Task<IActionResult> Index(string filter = "upcoming", string? search = null)
        {
            // 🔑 Lấy OrganizerId từ session
            var organizerId = HttpContext.Session.GetInt32("UId");
            if (organizerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ✅ Truyền organizerId vào repo
            var events = await _repo.GetEventsAsync(organizerId.Value, filter, search);

            ViewBag.Filter = filter ?? "upcoming";
            ViewBag.Search = search;
            return View(events);
        }


        // view seating for one event
        public async Task<IActionResult> Seating(int id)
        {
            var dto = await _repo.GetSeatingByEventIdAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }

        // POST add seat (increments TotalSeats by 1)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSeat(int eventId)
        {
            if (eventId <= 0) return Json(new { success = false, message = "Invalid event id" });

            var ok = await _repo.AddSeatAsync(eventId);
            if (!ok) return Json(new { success = false, message = "Could not add seat" });

            return Json(new { success = true });
        }
    }
}
