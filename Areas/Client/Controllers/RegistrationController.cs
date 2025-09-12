using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Areas.Client.Controllers
{
    [Area("Client")]
    public class RegistrationController : Controller
    {
        public IActionResult Index(int id)
        {
            var item = RegistrationRepository.Instance.GetRegistrationByStuId(id);
            ViewBag.itemReg = item;
            return View();
        }
    }
}
