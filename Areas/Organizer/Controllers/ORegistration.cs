using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;
using EventSphere.Service.Email;
using QRCoder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace EventSphere.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class ORegistration : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ORegistration> _logger;
        private readonly IConfiguration _configuration;

        public ORegistration(IEmailSender emailSender, ILogger<ORegistration> logger, IConfiguration configuration)
        {
            _emailSender = emailSender;
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var list = RegistrationRepository.Instance.GetAll();
            ViewBag.listReg = list;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAndSend(int id)
        {
            try
            {
                var result = RegistrationRepository.Instance.ApproveAndCreateAttendance(id);

                if (string.IsNullOrWhiteSpace(result.StudentEmail))
                {
                    return Json(new { success = true, message = "Đã duyệt nhưng không gửi mail vì sinh viên chưa có email." });
                }

                // 🔑 Lấy BaseUrl từ appsettings.json hoặc fallback
                string baseUrl = _configuration["AppSettings:BaseUrl"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    baseUrl = $"{Request.Scheme}://{Request.Host}";
                }

                // ✅ URL QR để check-in
                string qrUrl = $"{baseUrl}/Organizer/Scan/MarkAttendance" +
                               $"?attendanceId={result.AttendanceId}&eventId={result.EventId}&studentId={result.StudentId}";

                // Tạo QR code từ URL
                byte[] qrBytes;
                using (var qrGen = new QRCodeGenerator())
                using (var qrData = qrGen.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q))
                {
                    var pngQr = new PngByteQRCode(qrData);
                    qrBytes = pngQr.GetGraphic(20);
                }

                string studentName = string.IsNullOrWhiteSpace(result.StudentName) ? result.StudentEmail : result.StudentName;
                string dateStr = result.EventDate?.ToString("yyyy-MM-dd") ?? "";
                string timeStr = result.EventTime?.ToString() ?? "";

                string subject = $"[EventSphere] QR tham dự - {result.EventName}";
                string htmlBody =
                    $"<p>Xin chào <strong>{System.Net.WebUtility.HtmlEncode(studentName)}</strong>,</p>" +
                    $"<p>Bạn đã được duyệt tham gia <strong>{System.Net.WebUtility.HtmlEncode(result.EventName)}</strong>.</p>" +
                    $"<p>Ngày: <strong>{dateStr}</strong><br/>Giờ: <strong>{timeStr}</strong></p>" +
                    $"<p>👉 Vui lòng quét mã QR bên dưới để xác nhận tham dự:</p>" +
                    $"<p><img src=\"cid:qrImage\" alt=\"QR code\" /></p>" +
                    $"<p>Nếu không quét được, bạn có thể mở trực tiếp link: <a href='{qrUrl}'>{qrUrl}</a></p>" +
                    $"<p>Xin cảm ơn,<br/>Ban tổ chức</p>";

                await _emailSender.SendEmailWithInlineImageAsync(
                    result.StudentEmail, subject, htmlBody, qrBytes, "qrImage"
                );

                return Json(new { success = true, message = "Đã duyệt và gửi mail." });
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "Approve failed for id {Id}", id);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ApproveAndSend failed for id {Id}", id);
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Deny(int id)
        {
            try
            {
                RegistrationRepository.Instance.DenyRegistration(id);
                return Json(new { success = true, message = "Đã từ chối đăng ký." });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
