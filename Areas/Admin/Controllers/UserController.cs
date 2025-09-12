using EventSphere.Models.entities;
using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly UserRepositoryEf _userRepo;
        private readonly EventSphereContext _context;

        public UserController(UserRepositoryEf userRepo, EventSphereContext context)
        {
            _userRepo = userRepo;
            _context = context;
        }

        // Index load page
        public async Task<IActionResult> Index(
      int page = 1, int pageSize = 10,
      int? role = null, int? status = null,
      string? search = null)
        {
            // Các role đúng theo yêu cầu
            var roles = new List<object>
    {
        new { Id = 0, Name = "Admin" },
        new { Id = 1, Name = "Student" },
        new { Id = 2, Name = "Organizer" }
    };
            ViewBag.Roles = roles;

            // Lấy dữ liệu phân trang từ repository
            var (data, totalCount) = await _userRepo.GetPagedUsersAsync(page, pageSize, role, status, search);

            // Truyền dữ liệu ra view
            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;

            return View(data);
        }

        // Partial for AJAX list
        [HttpGet]
        public async Task<IActionResult> ListPartial(
            int page = 1, int pageSize = 10,
            int? role = null, int? status = null,
            string? search = null)
        {
            var (data, totalCount) = await _userRepo.GetPagedUsersAsync(page, pageSize, role, status, search);

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;

            return PartialView("_UserTablePartial", data);
        }

        public async Task<IActionResult> Details(int id)
        {
            // Lấy user kèm details
            var user = await _context.TblUsers
                .Include(u => u.TblUserDetails)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            // Lấy event gần nhất của user có ảnh (nếu user là organizer)
            var latestEvent = await _context.TblEvents
                .Where(e => e.OrganizerId == id && !string.IsNullOrEmpty(e.Image))
                .OrderByDescending(e => e.Date)
                .FirstOrDefaultAsync();

            // Chuẩn hoá image path cho user detail:
            //  - nếu TblUserDetails.Image là null -> thử latestEvent.Image -> fallback null (view sẽ hiển thị default)
            //  - nếu giá trị bắt đầu bằng http => giữ nguyên
            //  - nếu chỉ tên file hoặc không có prefix => prefix "uploads/"
            string resolved = null;
            var detailImage = user.TblUserDetails?.FirstOrDefault()?.Image;
            if (!string.IsNullOrWhiteSpace(detailImage))
                resolved = detailImage.Trim();
            else if (latestEvent != null && !string.IsNullOrWhiteSpace(latestEvent.Image))
                resolved = latestEvent.Image.Trim();

            if (!string.IsNullOrWhiteSpace(resolved))
            {
                if (!resolved.StartsWith("http", System.StringComparison.OrdinalIgnoreCase)
                    && !resolved.StartsWith("uploads/", System.StringComparison.OrdinalIgnoreCase)
                    && !resolved.StartsWith("~/") && !resolved.StartsWith("/"))
                {
                    resolved = "uploads/" + resolved;
                }
            }
            else
            {
                resolved = null; // để view fallback
            }  // Gửi đường dẫn đã chuẩn hoá qua ViewBag để view dễ sử dụng
            ViewBag.ResolvedImage = resolved;

            return View(user);
        }

        // Update role - trả JSON cho AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(int id, int role)
        {
            var user = await _userRepo.GetUserWithDetailsAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            await _userRepo.UpdateRoleAsync(id, role);
            return Json(new { success = true, message = "Role updated." });
        }

        // Change password - trả JSON cho AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Password invalid or mismatch." });
            }

            var user = await _userRepo.GetUserWithDetailsAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            await _userRepo.UpdatePasswordAsync(id, newPassword);
            return Json(new { success = true, message = "Password updated." });
        }

        // Toggle status -> return new status so client can update UI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var newStatus = await _userRepo.ToggleStatusAsync(id);
            if (newStatus == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            return Json(new { success = true, status = newStatus, message = "Status updated" });
        }

        // Delete user cascade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ok = await _userRepo.DeleteUserCascadeAsync(id);
            return Json(new { success = ok, message = ok ? "User deleted" : "Delete failed" });
        }

        // Edit basic info
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TblUser user)
        {
            if (ModelState.IsValid)
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }
    }
}
