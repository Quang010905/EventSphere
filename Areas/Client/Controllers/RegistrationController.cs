using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Areas.Client.Controllers
{
    [Area("Client")]
    public class RegistrationController : Controller
    {
        public IActionResult Index(int id, int? status)
        {
            var items = RegistrationRepository.Instance.GetRegistrationByStuId(id);

            if (status.HasValue)
            {
                items = items.Where(x => x.Status == status.Value).ToList();
            }

            ViewBag.itemReg = items;
            ViewBag.StatusFilter = status;
            ViewBag.StudentId = id;

            return View();
        }


    }
}
