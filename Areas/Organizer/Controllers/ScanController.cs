using Microsoft.AspNetCore.Mvc;
using EventSphere.Models.Repositories;
using System.Threading.Tasks;
using EventSphere.Models.ModelViews;

namespace EventSphere.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class ScanController : Controller
    {
        private readonly AttendanceRepository _attendanceRepo;

        public ScanController(AttendanceRepository attendanceRepo)
        {
            _attendanceRepo = attendanceRepo;
        }

        /// ✅ Zalo/camera quét QR → mở link này
        [HttpGet]
        public async Task<IActionResult> MarkAttendance(int attendanceId, int eventId, int studentId)
        {
            string payload = $"AttendanceId:{attendanceId};EventId:{eventId};StudentId:{studentId}";

            var (success, message, certificateUrl) = await _attendanceRepo.ProcessQrPayloadAsync(payload);

            var vm = new ScanResultViewModel
            {
                Success = success,
                Message = message ?? (success ? "Thành công" : "Thất bại"),
                CertificateUrl = certificateUrl
            };

            return View("ScanResult", vm);
        }
    }
}
