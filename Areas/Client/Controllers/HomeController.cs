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
            // Lấy các dữ liệu song song
            var upcomingTask = _repo.GetUpcomingEventBriefsAsync(); // Đảm bảo repo trả về Time
            var mediaTask = _repo.GetLatestAsync(6);
            var catsTask = _repo.GetDistinctCategoriesAsync();
            var mediaYearsTask = _repo.GetMediaYearsAsync();

            await Task.WhenAll(upcomingTask, mediaTask, catsTask, mediaYearsTask);

            var upcomingEvents = await upcomingTask;

            var vm = new HomeViewModel
            {
                UpcomingEvents = upcomingEvents,
                LatestMedia = await mediaTask,
                Categories = await catsTask ?? Enumerable.Empty<KeyValuePair<string, string>>(),
                MediaYears = await mediaYearsTask ?? Enumerable.Empty<int>(),
                TotalUpcomingEvents = upcomingEvents.Count,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                SiteAnnouncements = Enumerable.Empty<HomeViewModel.Announcement>() // nếu chưa map DB
            };

            ViewData["Title"] = "Trang chủ";
            return View(vm);
        }
    }
}
