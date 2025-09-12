using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Mvc;
using EventSphere.Service.Email;
using QRCoder;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace EventSphere.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class ORegistration : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ORegistration> _logger;

        public ORegistration(IEmailSender emailSender, ILogger<ORegistration> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
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

                string payload = $"AttendanceId:{result.AttendanceId};EventId:{result.EventId};StudentId:{result.StudentId}";

                byte[] qrBytes;
                using (var qrGen = new QRCodeGenerator())
                using (var qrData = qrGen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
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
                    $"<p>Vui lòng mang mã QR bên dưới đến sự kiện để quản lý quét điểm danh:</p>" +
                    $"<p><img src=\"cid:qrImage\" alt=\"QR code\" /></p>" +
                    $"<p>Xin cảm ơn,<br/>Ban tổ chức</p>";

                await _emailSender.SendEmailWithInlineImageAsync(result.StudentEmail, subject, htmlBody, qrBytes, "qrImage");

                return Json(new { success = true, message = "Đã duyệt và gửi mail." });
            }
            catch (InvalidOperationException ex)
            {
                // Trường hợp đã duyệt trước đó hoặc không thể approve
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
