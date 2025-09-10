using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Areas.Admin.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
