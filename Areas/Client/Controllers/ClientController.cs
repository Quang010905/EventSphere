using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Areas.Client.Controllers
{
    public class ClientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Register()
        {
            return View();
        }
        public IActionResult saveItem()
        {
            return View();
        }
    }
}
