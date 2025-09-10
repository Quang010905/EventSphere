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

        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _context.TblEvents
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            // Chuẩn bị danh sách Organizer cho dropdown
            ViewBag.Organizers = await _context.TblUsers
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();

            return View(ev);
        }


        [HttpPost]
        public async Task<IActionResult> EditAjax([FromForm] EditEventDto dto, IFormFile? imageFile)
        {
            var ev = await _context.TblEvents.FindAsync(dto.Id);
            if (ev == null) return Json(new { success = false, message = "Không tìm thấy sự kiện" });

            // Cập nhật các trường
            ev.Title = dto.Title;
            ev.Description = dto.Description;
            ev.Venue = dto.Venue;
            ev.Category = dto.Category;
            ev.Status = dto.Status;

            // Xử lý OrganizerId
            ev.OrganizerId = dto.OrganizerId;

            // Cập nhật ảnh nếu có
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

            await _context.SaveChangesAsync();
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
        public int? OrganizerId { get; set; }  // <--- sửa từ string sang int?
    }


}
