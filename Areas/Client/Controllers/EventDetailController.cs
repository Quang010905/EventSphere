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
                var status = RegistrationRepository.Instance.GetRegistrationStatus(stuId.Value, id);
                ViewBag.RegistrationStatus = status;
            }
            else
            {
                ViewBag.RegistrationStatus = null;
            }
            if (stuId != null && stuId > 0)
            {

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

            return RedirectToAction("Index", "Registration", new { id = stuId });
        }

        public ActionResult CancelRegistration()
        {
            var eventId = int.Parse(Request.Form["EventId"]);
            var userId = int.Parse(Request.Form["StuId"]);
            var res = RegistrationRepository.Instance.Delete(eventId, userId);
            if (res)
            {
                TempData["SuccessMessage"] = "Cancel registration success!";
            }
            else
            {
                TempData["ErrorMessage"] = "Cancel registration fail!";
            }

            return RedirectToAction("Index", "EventDetail", new { id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendComment()
        {
            var eventId = Request.Form["EventId"];
            var stuId = Request.Form["StuId"];
            var comments = Request.Form["Comments"];
            var ratingStr = Request.Form["Rating"];
            var userId = HttpContext.Session.GetInt32("UId");

            if (userId == null || userId == 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, redirect = Url.Action("Login", "Client", new { area = "Client" }) });
                return RedirectToAction("Login", "Client", new { area = "Client" });
            }

            if (string.IsNullOrEmpty(comments) || string.IsNullOrEmpty(ratingStr) || ratingStr == "0")
            {
                var err = "Please provide both comments and a rating.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { success = false, message = err });
                TempData["ErrorMessage"] = err;
                return RedirectToAction("Index", "EventDetail", new { id = eventId });
            }

            var entity = new FeedbackView
            {
                EventId = int.Parse(eventId),
                StudentId = int.Parse(stuId),
                Comments = comments,
                Rating = int.Parse(ratingStr),
                Status = 0 // chờ duyệt
            };

            try
            {
                await CommentRepository.Instance.AddAsync(entity);
                TempData["Message"] = "Your comment has been submitted and is awaiting moderation.!";
                TempData["MessageType"] = "success";

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = TempData["Message"] });
            }
            catch (Exception ex)
            {
                TempData["Message"] = ex.Message;
                TempData["MessageType"] = "error";

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { success = false, message = ex.Message });
            }

            return RedirectToAction("Index", "EventDetail", new { id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int feedbackId)
        {
            var userId = HttpContext.Session.GetInt32("UId");
            if (userId == null || userId == 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, redirect = Url.Action("Login", "Client", new { area = "Client" }) });
                return RedirectToAction("Login", "Client", new { area = "Client" });
            }

            try
            {
                var deleted = await CommentRepository.Instance.DeleteAsync(feedbackId, userId.Value);
                if (!deleted)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return BadRequest(new { success = false, message = "No comments found." });
                    TempData["ErrorMessage"] = "No comments found..";
                    return RedirectToAction("Index", "EventDetail");
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Comment deleted successfully." });

                TempData["SuccessMessage"] = "Comment deleted successfully.";
            }
            catch (UnauthorizedAccessException uex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return StatusCode(403, new { success = false, message = uex.Message });
                TempData["ErrorMessage"] = uex.Message;
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { success = false, message = ex.Message });
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index", "EventDetail");
        }
        [HttpGet]
        public async Task<IActionResult> GetFeedbacks(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UId") ?? 0;
            var list = await CommentRepository.Instance.GetFeedbacksAsync(eventId, userId);
            // Trả về đúng IEnumerable<TblFeedback> để partial view chính xác kiểu model
            return PartialView("_FeedbackList", list);
        }

    }
}