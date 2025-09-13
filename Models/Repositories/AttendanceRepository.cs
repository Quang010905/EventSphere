using EventSphere.Models.entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventSphere.Models.Repositories
{
    public class AttendanceRepository : Repository<TblAttendance>
    {
        public AttendanceRepository(EventSphereContext context) : base(context) { }

        // Lấy danh sách có phân trang
        public async Task<(IEnumerable<TblAttendance> data, int totalCount)> GetPagedAttendancesAsync(
            int pageIndex, int pageSize, int? eventId = null, int? studentId = null,
            bool? attended = null, string? keyword = null)
        {
            var query = _dbSet
                .Include(a => a.Event)
                .Include(a => a.Student)
                .AsQueryable();

            if (eventId.HasValue)
                query = query.Where(a => a.EventId == eventId);

            if (studentId.HasValue)
                query = query.Where(a => a.StudentId == studentId);

            if (attended.HasValue)
                query = query.Where(a => a.Attended == attended);

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(a =>
                    (a.Student != null && a.Student.TblUserDetails.Any(d => d.Fullname.Contains(keyword)))
                    || (a.Event != null && a.Event.Title.Contains(keyword)));

            int totalCount = await query.CountAsync();
            var data = await query
                .OrderByDescending(a => a.MarkedOn)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }

        /// ✅ Xử lý payload QR
        public async Task<(bool success, string message, string? certificateUrl)> ProcessQrPayloadAsync(string payload)
        {
            try
            {
                var parts = payload.Split(';');
                var dict = parts.Select(p => p.Split(':'))
                                .Where(p => p.Length == 2)
                                .ToDictionary(p => p[0], p => p[1]);

                if (!dict.TryGetValue("AttendanceId", out var attIdStr) ||
                    !dict.TryGetValue("EventId", out var eventIdStr) ||
                    !dict.TryGetValue("StudentId", out var stuIdStr))
                {
                    return (false, "Payload không hợp lệ.", null);
                }

                int attId = int.Parse(attIdStr);
                int eventId = int.Parse(eventIdStr);
                int stuId = int.Parse(stuIdStr);

                // Lấy entity, không track để tránh cached state
                var attendance = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == attId && a.EventId == eventId && a.StudentId == stuId);

                if (attendance == null)
                    return (false, "Không tìm thấy dữ liệu điểm danh (id/event/student không khớp).", null);

                // DEBUG: trả thêm thông tin trạng thái (tạm, chỉ dev)
                // kiểm tra kiểu dữ liệu Attended an toàn
                bool isAttended = false;
                if (attendance.Attended is bool b) isAttended = b;
                else if (attendance.Attended != null)
                {
                    // nếu Attended là nullable khác kiểu, cố parse
                    isAttended = Convert.ToBoolean(attendance.Attended);
                }

                if (isAttended)
                {
                    var existingCert = await _context.TblCertificates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.EventId == eventId && c.StudentId == stuId);

                    return (true, $"Sinh viên đã điểm danh trước đó. (attendance.Id={attendance.Id}, Attended={isAttended})", existingCert?.CertificateUrl);
                }

                // Vì ta dùng AsNoTracking để debug, đọc lại có tracking khi update:
                var attendanceForUpdate = await _dbSet
                    .FirstOrDefaultAsync(a => a.Id == attId && a.EventId == eventId && a.StudentId == stuId);

                attendanceForUpdate.Attended = true;
                attendanceForUpdate.MarkedOn = DateTime.Now;
                await _context.SaveChangesAsync();

                var cert = await _context.TblCertificates
                    .FirstOrDefaultAsync(c => c.EventId == eventId && c.StudentId == stuId);

                if (cert == null)
                {
                    cert = new TblCertificate
                    {
                        EventId = eventId,
                        StudentId = stuId,
                        CertificateUrl = null,
                        IssuedOn = DateTime.Now
                    };
                    _context.TblCertificates.Add(cert);
                    await _context.SaveChangesAsync();
                }

                return (true, "Điểm danh thành công.", cert.CertificateUrl);
            }
            catch (Exception ex)
            {
                return (false, "Lỗi xử lý: " + ex.Message, null);
            }
        }

    }
}
