// Areas/Client/Controllers/HomeController.cs
using EventSphere.Models.ModelViews;
using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> Index()
        {
            var upcomingTask = _repo.GetUpcomingEventBriefsAsync();
            var mediaTask = _repo.GetLatestAsync(6);
            var catsTask = _repo.GetDistinctCategoriesAsync();
            var mediaYearsTask = _repo.GetMediaYearsAsync();
            // nếu bạn có announcements trong DB, có thể gọi _repo.GetSiteAnnouncementsAsync()

            await Task.WhenAll(upcomingTask, mediaTask, catsTask, mediaYearsTask);

            var vm = new HomeViewModel
            {
                UpcomingEvents = await upcomingTask,
                LatestMedia = await mediaTask,
                Categories = await catsTask ?? Enumerable.Empty<KeyValuePair<string, string>>(),
                MediaYears = await mediaYearsTask ?? Enumerable.Empty<int>(),
                TotalUpcomingEvents = (await upcomingTask).Count,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                SiteAnnouncements = Enumerable.Empty<HomeViewModel.Announcement>() // tạm thời rỗng nếu chưa map DB
            };

            ViewData["Title"] = "Trang chủ";
            return View(vm);
        }
    }
}
