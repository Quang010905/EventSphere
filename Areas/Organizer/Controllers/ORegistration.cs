using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class ORegistration : Controller
    {
        public IActionResult Index()
        {
            var item = RegistrationRepository.Instance.GetAll();
            ViewBag.listReg = item;
            return View();
        }
    }
}
