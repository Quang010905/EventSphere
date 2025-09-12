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


        [HttpPost]
        public IActionResult Cancel(int id)
        {
            var stuId = HttpContext.Session.GetInt32("UId");
            if (id == 0)
            {
                TempData["Error"] = "Không tìm thấy thông tin đăng ký!";
                return RedirectToAction("Index", new { id = id });
            }

            RegistrationRepository.Instance.CancelRegistration(id);
            TempData["Success"] = "Bạn đã hủy đăng ký thành công!";
            return RedirectToAction("Index", new { id = stuId });
        }


    }
}
