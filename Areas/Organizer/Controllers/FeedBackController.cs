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
            int? eventId = null, int? studentId = null, int? rating = null, int? status = null, string? search = null)
        {
            var organizerId = HttpContext.Session.GetInt32("UId");
            if (organizerId == null)
                return RedirectToAction("Login", "Client", new { area = "Client" });

            // Prepare select lists
            var events = await _context.TblEvents
                .Where(e => e.OrganizerId == organizerId)
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

            ViewBag.Ratings = new SelectList(new[] { 5, 4, 3, 2, 1 }, selectedValue: rating);

            ViewBag.Statuses = new SelectList(new[]
            {
                new { Value = "", Text = "All" },
                new { Value = "0", Text = "Pending" },
                new { Value = "1", Text = "Approved" },
                new { Value = "2", Text = "Rejected" }
            }, "Value", "Text", status);

            // Get paged data
            var (data, totalCount) = await _feedbackRepo.GetPagedFeedbacksAsync(
                page, pageSize, eventId, studentId, rating, search, organizerId.Value, status);

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.EventId = eventId;
            ViewBag.StudentId = studentId;
            ViewBag.Rating = rating;
            ViewBag.Status = status;

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, int status)
        {
            var feedback = await _context.TblFeedbacks.FindAsync(id);
            if (feedback == null) return NotFound();

            feedback.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
