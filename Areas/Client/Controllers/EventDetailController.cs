using EventSphere.Models.entities;
using EventSphere.Models.ModelViews;
using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Areas.Client.Controllers
{
    [Area("Client")]
    public class EventDetailController : Controller
    {
        public IActionResult Index(int id)
        {
            var item = EventRepository.Instance.FindById(id);
            ViewBag.itemEvent = item;
            var stuId = HttpContext.Session.GetInt32("UId");

            if (stuId != null && stuId > 0)
            {
                // Kiểm tra xem student đã đăng ký event chưa
                bool alreadyRegistered = RegistrationRepository.Instance
                    .CheckRegistered(stuId.Value, id);

                ViewBag.AlreadyRegistered = alreadyRegistered;
            }
            else
            {
                ViewBag.AlreadyRegistered = false;
            }

            return View();
        }


        public ActionResult RegisterEvent()
        {
            var db = new EventSphereContext();
            var stuId = HttpContext.Session.GetInt32("UId");
            var eventId = Request.Form["EventId"];
            if (stuId == null || stuId == 0)
            {
                return RedirectToAction("Login", "Client", new { area = "Client" });
            }
            var item = new RegistrationView
            {
                EventId = int.Parse(eventId),
                StudentId = stuId.Value,
                Status = 0,
                RegisterOn = DateTime.Now
            };
            RegistrationRepository.Instance.Add(item);

            return RedirectToAction("Index", "Registration", new {id = stuId});
        }
    }
}
