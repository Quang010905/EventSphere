using EventSphere.Models.Repositories;
using EventSphere.Models.ModelViews;
using Microsoft.AspNetCore.Mvc;

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
            var vm = new HomeViewModel
            {
                UpcomingEvents = await _repo.GetUpcomingEventsAsync(),
                LatestMedia = await _repo.GetLatestMediaAsync()
            };

            ViewData["Title"] = "Trang chủ";
            return View(vm);
        }
    }
}
