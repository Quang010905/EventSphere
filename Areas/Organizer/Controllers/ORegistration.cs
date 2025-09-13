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
            var organizerId = HttpContext.Session.GetInt32("UId");
            var list = RegistrationRepository.Instance.GetByOrganizerId(organizerId.Value);
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

                if (result == null)
                    return Json(new { success = false, message = "Không có kết quả từ repository." });

                if (result.IsWaitlisted)
                {
                    // Nếu đã đưa vào danh sách chờ thì không gửi mail
                    return Json(new { success = true, message = "Sự kiện hiện đã hết chỗ. Sinh viên đã được đưa vào danh sách chờ (waitlist). Chưa gửi email." });
                }

                if (string.IsNullOrWhiteSpace(result.StudentEmail))
                    return Json(new { success = true, message = "Đã duyệt nhưng không gửi mail vì sinh viên chưa có email." });

                var baseUrl = _configuration["AppSettings:BaseUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    throw new InvalidOperationException("BaseUrl chưa được cấu hình trong appsettings.json");

                var qrUrl = $"{baseUrl}/Organizer/Scan/MarkAttendance" +
                            $"?attendanceId={result.AttendanceId}&eventId={result.EventId}&studentId={result.StudentId}";

                byte[] qrBytes;
                using (var qrGen = new QRCodeGenerator())
                using (var qrData = qrGen.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q))
                {
                    var pngQr = new PngByteQRCode(qrData);
                    qrBytes = pngQr.GetGraphic(20);
                }

                var studentName = string.IsNullOrWhiteSpace(result.StudentName) ? result.StudentEmail : result.StudentName;
                var dateStr = result.EventDate?.ToString("yyyy-MM-dd") ?? "";
                var timeStr = result.EventTime?.ToString() ?? "";

                var subject = $"[EventSphere] QR tham dự - {result.EventName}";
                var htmlBody =
                    $"<p>Xin chào <strong>{System.Net.WebUtility.HtmlEncode(studentName)}</strong>,</p>" +
                    $"<p>Bạn đã được duyệt tham gia <strong>{System.Net.WebUtility.HtmlEncode(result.EventName)}</strong>.</p>" +
                    $"<p>Ngày: <strong>{dateStr}</strong><br/>Giờ: <strong>{timeStr}</strong></p>" +
                    $"<p>👉 Quét mã QR bên dưới để xác nhận tham dự:</p>" +
                    $"<p><img src=\"cid:qrImage\" alt=\"QR code\" /></p>" +
                    $"<p>Nếu không quét được, mở trực tiếp link: <a href='{qrUrl}'>{qrUrl}</a></p>" +
                    $"<p>Xin cảm ơn,<br/>Ban tổ chức</p>";

                await _emailSender.SendEmailWithInlineImageAsync(
                    result.StudentEmail, subject, htmlBody, qrBytes, "qrImage"
                );

                return Json(new { success = true, message = "Đã duyệt và gửi mail." });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ApproveAndSend failed for id {Id}", id);
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
