using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventSphere.Models.entities;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventSphere.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class EventsController : Controller
    {
        private readonly EventSphereContext _context;

        public EventsController(EventSphereContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            int? organizerId = HttpContext.Session.GetInt32("UId");
            if (organizerId == null)
            {
                // chưa login hoặc session hết hạn
                return RedirectToAction("Login", "CLient", new { area = "Client" });
            }
            var events = await _context.TblEvents
                .Include(e => e.Organizer).Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
            return View(events);
        }

 
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var ev = await _context.TblEvents.FindAsync(id);
            if (ev == null) return Json(new { success = false });

            ev.Status = 1; 
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        [HttpPost]
        public async Task<IActionResult> Disable(int id)
        {
            var ev = await _context.TblEvents.FindAsync(id);
            if (ev == null) return Json(new { success = false });

            ev.Status = 2; 
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.TblEvents
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            return View(ev);
        }


        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _context.TblEvents
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();


            if (ev.Status == 1 || ev.Status == 2)
            {
                TempData["ErrorMessage"] = "Event approved or disabled, cannot be edited";
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
            if (ev == null) return Json(new { success = false, message = "No events found" });

            if (ev.Status == 1 || ev.Status == 2)
                return Json(new { success = false, message = "Event approved or disabled, cannot be edited!" });

            // Normalize & validate title
            var title = dto.Title?.Trim();
            if (string.IsNullOrEmpty(title))
                return Json(new { success = false, message = "Title cannot be empty!" });

            var titleLower = title.ToLower();
            var exists = await _context.TblEvents
                .AnyAsync(e => e.Id != dto.Id && e.Title != null && e.Title.ToLower() == titleLower);
            if (exists)
                return Json(new { success = false, message = "Title already exists, please choose another title." });

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
                    return Json(new { success = false, message = "Date and time must be greater than current!" });
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
                return Json(new { success = false, message = "Save failed due to duplicate title (DB constraint). Please try another title.." });
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
                return Json(new { success = false, message = "Title cannot be empty!" });

            // Kiểm tra trùng (không phân biệt hoa thường)
            var titleLower = title.ToLower();
            var exists = await _context.TblEvents
                .AnyAsync(e => e.Title != null && e.Title.ToLower() == titleLower);
            if (exists)
                return Json(new { success = false, message = "Title already exists, please choose another title." });

            // Validate ngày giờ phải ở tương lai (nếu có)
            if (dto.Date.HasValue && dto.Time.HasValue)
            {
                var eventDateTime = dto.Date.Value.ToDateTime(dto.Time.Value);
                if (eventDateTime <= DateTime.Now)
                    return Json(new { success = false, message = "Date and time must be greater than current!" });
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
                return Json(new { success = false, message = "Save failed due to duplicate title (DB constraint). Please try another title." });
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
