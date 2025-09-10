using EventSphere.Models.entities;
using Microsoft.EntityFrameworkCore;

namespace EventSphere.Models.Repositories
{
    public class AttendanceRepository : Repository<TblAttendance>
    {
        public AttendanceRepository(EventSphereContext context) : base(context) { }

        // Lấy danh sách có phân trang, tìm kiếm, lọc
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
    }
}
