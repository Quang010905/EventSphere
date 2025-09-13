using EventSphere.Models.ModelViews;
using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EventSphere.Areas.Client.Controllers
{
    [Area("Client")]
    public class HomeController : Controller
    {
        private readonly HomeRepository _repo;

        public HomeController(HomeRepository repo)
        {
            _repo = repo;
        }

        // Index với filter + paging
        public async Task<IActionResult> Index(
            string q = "",
            string department = "",
            string status = "all",
            string start = "",
            string end = "",
            int page = 1,
            int pageSize = 6)
        {
            DateOnly? startDate = null;
            DateOnly? endDate = null;
            if (!string.IsNullOrWhiteSpace(start))
            {
                if (DateTime.TryParse(start, out var dtStart))
                    startDate = DateOnly.FromDateTime(dtStart);
            }
            if (!string.IsNullOrWhiteSpace(end))
            {
                if (DateTime.TryParse(end, out var dtEnd))
                    endDate = DateOnly.FromDateTime(dtEnd);
            }
            var catsTask = _repo.GetDistinctCategoriesAsync();
            var mediaTask = _repo.GetLatestAsync(6);
            var yearsTask = _repo.GetMediaYearsAsync();

            var searchTask = _repo.SearchEventsAsync(q, department, startDate, endDate, status, Math.Max(1, page), pageSize);

            await Task.WhenAll(catsTask, mediaTask, yearsTask, searchTask);

            var (items, total) = await searchTask;

            var vm = new HomeViewModel
            {
                UpcomingEvents = items,
                LatestMedia = await mediaTask,
                Categories = await catsTask ?? Enumerable.Empty<System.Collections.Generic.KeyValuePair<string, string>>(),
                MediaYears = await yearsTask ?? Enumerable.Empty<int>(),

                TotalItems = total,
                EventsPageSize = pageSize,
                CurrentPage = Math.Max(1, page),

                SearchQuery = q ?? string.Empty,
                SelectedDepartment = department ?? string.Empty,
                SelectedStatus = status ?? "all",
                StartDateStr = string.IsNullOrWhiteSpace(startDate?.ToString()) ? start : start,
                EndDateStr = string.IsNullOrWhiteSpace(endDate?.ToString()) ? end : end,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                SiteAnnouncements = Enumerable.Empty<HomeViewModel.Announcement>()
            };

            ViewData["Title"] = "Home"; 
            return View(vm);
        }
        public IActionResult About()
        {
            ViewData["Title"] = "Introducing EventSphere";
            var aboutInfo = new
            {
                Name = "EventSphere",
                Description = "Online event information system for university students, faculty and staff.",
                Purpose = "Provide event information, manage registration, and improve user experience.",
                ContactEmail = "support@eventsphere.edu.vn"
            };

            return View(aboutInfo); 
        }
    }
}