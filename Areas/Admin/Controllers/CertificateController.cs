using EventSphere.Models.entities;
using EventSphere.Models.ViewModels;
using EventSphere.Repositories;
using EventSphere.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CertificateController : Controller
    {
        private readonly CertificateRepository _certRepo;
        private readonly EventSphereContext _context;
        private readonly IWebHostEnvironment _env;

        public CertificateController(CertificateRepository certRepo, EventSphereContext context, IWebHostEnvironment env)
        {
            _certRepo = certRepo;
            _context = context;
            _env = env;
        }

        // Index
        public async Task<IActionResult> Index(int? eventId, int? studentId, string? keyword, int page = 1, int pageSize = 10)
        {
            var (data, total) = await _certRepo.GetPagedCertificatesAsync(
                page, pageSize,
                eventId: eventId,
                studentId: studentId,
                issuedFrom: null,
                issuedTo: null,
                keyword: string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim()
            );

            var vm = new CertificatesIndexViewModel
            {
                Certificates = data,
                EventId = eventId,
                StudentId = studentId,
                Keyword = keyword,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };

            return View(vm);
        }

        // GET: Generate
        [HttpGet]
        public IActionResult Generate()
        {
            return View(new CertificateGenerateViewModel());
        }

        // POST: Generate (AJAX)
        // POST: Generate (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate([FromBody] CertificateGenerateViewModel model)
        {
            if (!model.EventId.HasValue || !model.StudentId.HasValue)
                return BadRequest(new { success = false, message = "Thiếu Event hoặc Student" });

            // Tìm certificate đã tồn tại
            var cert = await _context.TblCertificates
                .FirstOrDefaultAsync(c => c.EventId == model.EventId.Value && c.StudentId == model.StudentId.Value);

            if (cert == null)
            {
                return BadRequest(new { success = false, message = "Chưa có certificate trong hệ thống. Sẽ được thêm khi điểm danh." });
            }

            // Lấy dữ liệu student & event
            var student = await _context.TblUsers
                .Include(u => u.TblUserDetails)
                .FirstOrDefaultAsync(u => u.Id == model.StudentId.Value && u.Role == 1);

            var eventEntity = await _context.TblEvents
                .FirstOrDefaultAsync(e => e.Id == model.EventId.Value);

            if (student == null || eventEntity == null)
                return BadRequest(new { success = false, message = "Không tìm thấy dữ liệu" });

            var studentName = student.TblUserDetails
                                .OrderBy(d => d.Id)
                                .Select(d => d.Fullname)
                                .FirstOrDefault()
                              ?? $"User {model.StudentId.Value}";

            var eventTitle = eventEntity.Title ?? $"Event {model.EventId.Value}";
            var issuedOn = DateTime.Now;

            byte[] pdfBytes;
            try
            {
                pdfBytes = CertificateGenerator.GenerateCertificate(studentName, eventTitle, issuedOn);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Lỗi khi tạo PDF: " + ex.Message });
            }

            // Lưu file vào wwwroot/certificates
            var certDir = Path.Combine(_env.WebRootPath ?? "", "certificates");
            if (!Directory.Exists(certDir))
                Directory.CreateDirectory(certDir);

            var fileName = $"certificate_{model.StudentId.Value}_{model.EventId.Value}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var filePath = Path.Combine(certDir, fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

            // Chỉ cập nhật certificate_url
            cert.CertificateUrl = $"/certificates/{fileName}";
            cert.IssuedOn = issuedOn;

            _context.TblCertificates.Update(cert);
            await _context.SaveChangesAsync();

            return Json(new { success = true, url = cert.CertificateUrl });
        }
     
        // ---------- AJAX endpoints for dropdowns ----------
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.TblEvents
                .OrderBy(e => e.Title)
                .Select(e => new { id = e.Id, text = e.Title })
                .ToListAsync();

            return Json(events);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudents(string q = null)
        {
            var query = _context.TblUsers.Where(u => u.Role == 1);

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(u => u.TblUserDetails.Any(d => EF.Functions.Like(d.Fullname, $"%{q}%")));
            }

            var studentsProjection = await query
                .Select(u => new
                {
                    u.Id,
                    Fullname = u.TblUserDetails
                        .OrderBy(d => d.Id)
                        .Select(d => d.Fullname)
                        .FirstOrDefault()
                })
                .Take(500)
                .ToListAsync();

            var list = studentsProjection
                .Select(s => new { id = s.Id, text = s.Fullname ?? $"User {s.Id}" })
                .ToList();

            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentById(int id)
        {
            var st = await _context.TblUsers
                .Where(u => u.Id == id && u.Role == 1)
                .Select(u => new
                {
                    id = u.Id,
                    text = u.TblUserDetails
                        .OrderBy(d => d.Id)
                        .Select(d => d.Fullname)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (st == null) return Json(new { id, text = $"User {id}" });

            return Json(new { id = st.id, text = string.IsNullOrWhiteSpace(st.text) ? $"User {st.id}" : st.text });
        }
    }
}
