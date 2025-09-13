using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventSphere.Models.entities;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EventsController : Controller
    {
        private readonly EventSphereContext _context;

        public EventsController(EventSphereContext context)
        {
            _context = context;
        }

        // Danh sách sự kiện
        public async Task<IActionResult> Index()
        {
            var events = await _context.TblEvents
                .Include(e => e.Organizer)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
            return View(events);
        }

        // Duyệt sự kiện
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var ev = await _context.TblEvents.FindAsync(id);
            if (ev == null) return Json(new { success = false });

            ev.Status = 1; // Đã duyệt
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Vô hiệu sự kiện
        [HttpPost]
        public async Task<IActionResult> Disable(int id)
        {
            var ev = await _context.TblEvents.FindAsync(id);
            if (ev == null) return Json(new { success = false });

            ev.Status = 2; // Đã vô hiệu
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Chi tiết sự kiện
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.TblEvents
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            return View(ev);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _context.TblEvents
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            // Chỉ cho sửa nếu status = 0
            if (ev.Status == 1 || ev.Status == 2)
            {
                TempData["ErrorMessage"] = "Sự kiện đã duyệt hoặc bị vô hiệu, không thể chỉnh sửa!";
                return RedirectToAction("Index");
            }

            // Chuẩn bị danh sách Organizer cho dropdown
            ViewBag.Organizers = await _context.TblUsers
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();

            return View(ev);
        }

        // POST: EditAjax
        [HttpPost]
        public async Task<IActionResult> EditAjax([FromForm] EditEventDto dto, IFormFile? imageFile)
        {
            var ev = await _context.TblEvents.FindAsync(dto.Id);
            if (ev == null) return Json(new { success = false, message = "Không tìm thấy sự kiện" });

            if (ev.Status == 1 || ev.Status == 2)
                return Json(new { success = false, message = "Sự kiện đã duyệt hoặc bị vô hiệu, không thể chỉnh sửa!" });

            // Normalize & validate title
            var title = dto.Title?.Trim();
            if (string.IsNullOrEmpty(title))
                return Json(new { success = false, message = "Tiêu đề không được bỏ trống!" });

            var titleLower = title.ToLower();
            var exists = await _context.TblEvents
                .AnyAsync(e => e.Id != dto.Id && e.Title != null && e.Title.ToLower() == titleLower);
            if (exists)
                return Json(new { success = false, message = "Tiêu đề đã tồn tại, vui lòng chọn tiêu đề khác." });

            // Cập nhật dữ liệu
            ev.Title = title;
            ev.Description = dto.Description;
            ev.Venue = dto.Venue;
            ev.Category = dto.Category;
            ev.OrganizerId = dto.OrganizerId;

            var dateStr = Request.Form["Date"];
            var timeStr = Request.Form["Time"];

            if (!string.IsNullOrEmpty(dateStr) && DateOnly.TryParse(dateStr, out var parsedDate))
                ev.Date = parsedDate;

            if (!string.IsNullOrEmpty(timeStr) && TimeOnly.TryParse(timeStr, out var parsedTime))
                ev.Time = parsedTime;

            // Validate ngày giờ
            if (ev.Date.HasValue && ev.Time.HasValue)
            {
                var eventDateTime = ev.Date.Value.ToDateTime(ev.Time.Value);
                if (eventDateTime <= DateTime.Now)
                    return Json(new { success = false, message = "Ngày giờ phải lớn hơn hiện tại!" });
            }

            // Xử lý ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                ev.Image = fileName;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Json(new { success = false, message = "Lưu thất bại do trùng tiêu đề (ràng buộc DB). Vui lòng thử tiêu đề khác." });
            }

            return Json(new { success = true });
        }


        // GET: Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Organizers = await _context.TblUsers
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();

            return View();
        }

        // POST: CreateAjax
        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromForm] EditEventDto dto, IFormFile? imageFile)
        {
            // Normalize title
            var title = dto.Title?.Trim();
            if (string.IsNullOrEmpty(title))
                return Json(new { success = false, message = "Tiêu đề không được bỏ trống!" });

            // Kiểm tra trùng (không phân biệt hoa thường)
            var titleLower = title.ToLower();
            var exists = await _context.TblEvents
                .AnyAsync(e => e.Title != null && e.Title.ToLower() == titleLower);
            if (exists)
                return Json(new { success = false, message = "Tiêu đề đã tồn tại, vui lòng chọn tiêu đề khác." });

            // Validate ngày giờ phải ở tương lai (nếu có)
            if (dto.Date.HasValue && dto.Time.HasValue)
            {
                var eventDateTime = dto.Date.Value.ToDateTime(dto.Time.Value);
                if (eventDateTime <= DateTime.Now)
                    return Json(new { success = false, message = "Ngày giờ phải lớn hơn hiện tại!" });
            }

            var ev = new TblEvent
            {
                Title = title,
                Description = dto.Description,
                Venue = dto.Venue,
                Category = dto.Category,
                OrganizerId = dto.OrganizerId,
                Date = dto.Date,
                Time = dto.Time,
                Status = 0 // mặc định Chưa duyệt
            };

            // Xử lý ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                ev.Image = fileName;
            }

            _context.TblEvents.Add(ev);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Nếu có ràng buộc DB unique (race condition), bắt và trả lỗi thân thiện
                return Json(new { success = false, message = "Lưu thất bại do trùng tiêu đề (ràng buộc DB). Vui lòng thử tiêu đề khác." });
            }

            return Json(new { success = true });
        }

    }

    // DTO dùng gửi dữ liệu
    public class EditEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Venue { get; set; } = "";
        public string Category { get; set; } = "";
        public int Status { get; set; }
        public int? OrganizerId { get; set; }
        public DateOnly? Date { get; set; }
        public TimeOnly? Time { get; set; }
    }
}
