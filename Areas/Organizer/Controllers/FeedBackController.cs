using EventSphere.Models.entities;
using EventSphere.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventSphere.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class FeedbackController : Controller
    {
        private readonly FeedbackRepository _feedbackRepo;
        private readonly EventSphereContext _context;

        public FeedbackController(FeedbackRepository feedbackRepo, EventSphereContext context)
        {
            _feedbackRepo = feedbackRepo;
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10,
            int? eventId = null, int? studentId = null, int? rating = null, string? search = null)
        {
            var orgernizerId = HttpContext.Session.GetInt32("UId");
            if (orgernizerId == null)
            {
                // chưa login hoặc session hết hạn
                return RedirectToAction("Login", "CLient", new { area = "Client" });
            }
            // Prepare select lists
            var events = await _context.TblEvents.Where(e=> e.OrganizerId == orgernizerId)
                .OrderBy(e => e.Title)
                .Select(e => new { e.Id, e.Title })
                .ToListAsync();
            ViewBag.Events = new SelectList(events, "Id", "Title", eventId);

            var users = await _context.TblUsers
                .Include(u => u.TblUserDetails)
                .ToListAsync();
            var studentsList = users
                .Select(u => new
                {
                    u.Id,
                    Fullname = u.TblUserDetails?.FirstOrDefault()?.Fullname ?? $"User {u.Id}"
                })
                .OrderBy(x => x.Fullname)
                .ToList();
            ViewBag.Students = new SelectList(studentsList, "Id", "Fullname", studentId);

            // Ratings select (you can adjust if rating range differs)
            ViewBag.Ratings = new SelectList(new[] { 5, 4, 3, 2, 1 }, selectedValue: rating);

            // Get paged data
            var (data, totalCount) = await _feedbackRepo.GetPagedFeedbacksAsync(page, pageSize, eventId, studentId, rating, search, orgernizerId.Value);

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.EventId = eventId;
            ViewBag.StudentId = studentId;
            ViewBag.Rating = rating;

            return View(data);
        }
    }
}
