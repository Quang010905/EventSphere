using EventSphere.Models.ModelViews;
using EventSphere.Models.Repositories;
using EventSphere.Service.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EventSphere.Models.entities;

namespace EventSphere.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class EventWaitlistController : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventWaitlistController> _logger;

        public EventWaitlistController(IEmailSender emailSender, IConfiguration configuration, ILogger<EventWaitlistController> logger)
        {
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: Organizer/EventWaitlist
        public IActionResult Index(int page = 1, int pageSize = 15, int? eventId = null, string? q = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(5, pageSize);

            // Lấy organizerId từ session
            var organizerId = HttpContext.Session.GetInt32("UId");
            if (!organizerId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var repo = EventWaitlistRepository.Instance;

            // Truyền organizerId vào GetPaged
            var result = repo.GetPaged(page, pageSize, organizerId.Value, eventId, q);

            // Danh sách event của chính organizer để đổ dropdown filter
            ViewBag.Events = repo.GetEventListForFilter(organizerId.Value);
            ViewBag.Query = q ?? "";
            ViewBag.EventId = eventId;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = result.TotalCount;

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var repo = EventWaitlistRepository.Instance;
            var res = repo.ConfirmWaitlist(id);

            if (!res.Success)
                return Json(new { success = false, message = res.Message });

            // Nếu đã tạo attendance -> cố gắng gửi mail nếu có email
            if (res.AttendanceId.HasValue)
            {
                try
                {
                    using var db = new EventSphereContext();
                    var attendance = db.TblAttendances
                        .Include(a => a.Student).ThenInclude(s => s.TblUserDetails)
                        .Include(a => a.Event)
                        .FirstOrDefault(a => a.Id == res.AttendanceId.Value);

                    if (attendance != null && attendance.Student != null)
                    {
                        var studentEmail = attendance.Student.Email;
                        var studentName = attendance.Student.TblUserDetails?.FirstOrDefault()?.Fullname ?? studentEmail ?? "";
                        var eventName = attendance.Event?.Title ?? "";

                        // event date string: attendance.Event.Date may be DateOnly? -> convert
                        string eventDateStr = "";
                        try
                        {
                            if (attendance.Event?.Date != null)
                                eventDateStr = attendance.Event.Date.Value.ToDateTime(new TimeOnly(0, 0)).ToString("yyyy-MM-dd");
                        }
                        catch
                        {
                            if (attendance.Event?.Date != null)
                            {
                                var d = attendance.Event.Date.Value;
                                eventDateStr = new DateTime(d.Year, d.Month, d.Day).ToString("yyyy-MM-dd");
                            }
                        }

                        var eventTimeStr = attendance.Event?.Time != null ? attendance.Event.Time.ToString() : "";

                        if (!string.IsNullOrWhiteSpace(studentEmail))
                        {
                            var baseUrl = _configuration["AppSettings:BaseUrl"];
                            if (string.IsNullOrWhiteSpace(baseUrl))
                                throw new InvalidOperationException("BaseUrl chưa được cấu hình trong appsettings.json");

                            var qrUrl = $"{baseUrl}/Organizer/Scan/MarkAttendance" +
                                        $"?attendanceId={attendance.Id}&eventId={attendance.EventId}&studentId={attendance.StudentId}";

                            byte[] qrBytes;
                            using (var qrGen = new QRCodeGenerator())
                            using (var qrData = qrGen.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q))
                            {
                                var pngQr = new PngByteQRCode(qrData);
                                qrBytes = pngQr.GetGraphic(20);
                            }

                            var subject = $"[EventSphere] QR tham dự - {eventName}";
                            var htmlBody =
                                $"<p>Xin chào <strong>{System.Net.WebUtility.HtmlEncode(studentName)}</strong>,</p>" +
                                $"<p>Bạn đã được xác nhận tham gia <strong>{System.Net.WebUtility.HtmlEncode(eventName)}</strong>.</p>" +
                                $"<p>Ngày: <strong>{eventDateStr}</strong><br/>Giờ: <strong>{eventTimeStr}</strong></p>" +
                                $"<p>👉 Quét mã QR bên dưới để xác nhận tham dự:</p>" +
                                $"<p><img src=\"cid:qrImage\" alt=\"QR code\" /></p>" +
                                $"<p>Nếu không quét được, mở trực tiếp link: <a href='{qrUrl}'>{qrUrl}</a></p>" +
                                $"<p>Xin cảm ơn,<br/>Ban tổ chức</p>";

                            await _emailSender.SendEmailWithInlineImageAsync(studentEmail, subject, htmlBody, qrBytes, "qrImage");
                            return Json(new { success = true, message = "Xác nhận thành công và đã gửi mail." });
                        }
                        else
                        {
                            return Json(new { success = true, message = "Xác nhận thành công nhưng sinh viên không có email để gửi." });
                        }
                    }
                    else
                    {
                        return Json(new { success = true, message = "Xác nhận thành công nhưng không tìm thấy attendance để gửi mail." });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error when sending email on waitlist confirm id {Id}", id);
                    // attendance đã được tạo; nhưng gửi mail lỗi
                    return Json(new { success = true, message = "Xác nhận thành công nhưng lỗi khi gửi mail: " + ex.Message });
                }
            }

            // success nhưng không tạo attendance (ví dụ đã có attendance trước đó)
            return Json(new { success = true, message = res.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var repo = EventWaitlistRepository.Instance;
            var res = repo.DeleteWaitlist(id);
            return Json(new { success = res.Success, message = res.Message });
        }
    }
}
