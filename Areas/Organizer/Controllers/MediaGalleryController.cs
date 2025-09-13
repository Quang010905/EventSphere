using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventSphere.Models.entities;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventSphere.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class MediaGalleryController : Controller
    {
        private readonly EventSphereContext _context;
        public MediaGalleryController(EventSphereContext context) => _context = context;

        // Danh sách media
        public async Task<IActionResult> Index(int? eventId)
        {
            var query = _context.TblMediaGalleries
                        .Include(m => m.Event)
                        .Include(m => m.UploadedByNavigation)
                        .AsQueryable();

            if (eventId.HasValue)
                query = query.Where(m => m.EventId == eventId.Value);

            var mediaList = await query.OrderByDescending(m => m.UploadedOn).ToListAsync();
            ViewBag.EventId = eventId;
            return View(mediaList);
        }

        // GET Create để load modal
        public async Task<IActionResult> Create(int? eventId)
        {
            // Sử dụng SelectList để dropdown hiển thị đúng
            ViewBag.Events = new SelectList(
                await _context.TblEvents.ToListAsync(), "Id", "Title", eventId);

            ViewBag.Users = new SelectList(
                await _context.TblUsers.ToListAsync(), "Id", "Email");

            ViewBag.SelectedEventId = eventId;
            return PartialView("Create", new MediaGalleryDto { EventId = eventId ?? 0 });
        }


        // POST CreateAjax
        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromForm] MediaGalleryDto dto, IFormFile file)
        {
            if (dto == null || file == null) return Json(new { success = false });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var media = new TblMediaGallery
            {
                EventId = dto.EventId,
                FileType = dto.FileType,
                FileUrl = "/uploads/" + fileName,
                Caption = dto.Caption,
                UploadedBy = dto.UploadedBy,
                UploadedOn = DateTime.Now
            };

            _context.TblMediaGalleries.Add(media);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }



        // Xóa media (Ajax)
        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var media = await _context.TblMediaGalleries.FindAsync(id);
            if (media == null) return Json(new { success = false });

            _context.TblMediaGalleries.Remove(media);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Lấy view Edit
        public async Task<IActionResult> Edit(int id)
        {
            var media = await _context.TblMediaGalleries.FindAsync(id);
            if (media == null) return NotFound();

            ViewBag.Events = await _context.TblEvents.Select(e => new { e.Id, e.Title }).ToListAsync();
            ViewBag.Users = await _context.TblUsers.Select(u => new { u.Id, u.Email }).ToListAsync();

            return View(media);
        }

        // Edit full media Ajax
        [HttpPost]
        public async Task<IActionResult> EditAjax([FromForm] EditMediaDto dto, IFormFile? file)
        {
            var media = await _context.TblMediaGalleries.FindAsync(dto.Id);
            if (media == null) return Json(new { success = false, message = "Không tìm thấy media" });

            media.EventId = dto.EventId;
            media.FileType = dto.FileType;
            media.Caption = dto.Caption;
            media.UploadedBy = dto.UploadedBy;

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                media.FileUrl = "/uploads/" + fileName;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    public class MediaGalleryDto
    {
        public int EventId { get; set; }
        public int FileType { get; set; } // 1 = Image, 2 = Video
        public string FileUrl { get; set; } = "";
        public int UploadedBy { get; set; }
        public string Caption { get; set; } = "";
    }

    public class EditMediaDto
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int FileType { get; set; }
        public string Caption { get; set; } = "";
        public int UploadedBy { get; set; }
    }
}
