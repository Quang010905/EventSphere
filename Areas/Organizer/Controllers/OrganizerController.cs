using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class OrganizerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
