using EventSphere.Models.entities;
using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AttendanceController : Controller
    {
        private readonly AttendanceRepository _attendanceRepo;
        private readonly EventSphereContext _context;

        public AttendanceController(AttendanceRepository attendanceRepo, EventSphereContext context)
        {
            _attendanceRepo = attendanceRepo;
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10,
            int? eventId = null, int? studentId = null, bool? attended = null, string? search = null)
        {
            // Prepare select lists
            var events = await _context.TblEvents
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

            // Get paged filtered data from repository
            var (data, totalCount) = await _attendanceRepo.GetPagedAttendancesAsync(
                page, pageSize, eventId, studentId, attended, search);

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.EventId = eventId;
            ViewBag.StudentId = studentId;
            ViewBag.Attended = attended;

            return View(data);
        }
    }
}
