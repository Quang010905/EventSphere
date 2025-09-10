using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Areas.Client.Controllers
{
    public class ClientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
