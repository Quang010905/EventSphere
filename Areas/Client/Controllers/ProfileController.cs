using EventSphere.Models.ModelViews;
using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EventSphere.Areas.Client.Controllers
{
    [Area("Client")]
    public class ProfileController : Controller
    {
        private readonly ProfileRepository _profileRepo;

        public ProfileController(ProfileRepository profileRepo)
        {
            _profileRepo = profileRepo;
        }

        // GET: Client/Profile/Index/{id}
        public async Task<IActionResult> Index(int id)
        {
            var profile = await _profileRepo.GetProfileAsync(id);
            if (profile == null) return NotFound();

            return View("~/Areas/Client/Views/Profile/Index.cshtml", profile);
        }

        // GET: Client/Profile/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _profileRepo.GetProfileForEditAsync(id);
            if (model == null) return NotFound();

            return View("~/Areas/Client/Views/Profile/Edit.cshtml", model);
        }

        // POST: Client/Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Areas/Client/Views/Profile/Edit.cshtml", model);
            }

            var success = await _profileRepo.UpdateProfileAsync(model);
            if (!success)
            {
                return NotFound();
            }

            // Gắn thông báo vào TempData
            TempData["SuccessMessage"] = "Profile update successful!";

            // Redirect về Index sau khi update thành công
            return RedirectToAction("Index", new { id = model.UserId });
        }
    }
}
