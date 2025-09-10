using EventSphere.Models.entities;
using EventSphere.Models.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EventSphere.Repositories
{
    public class FeedbackRepository : Repository<TblFeedback>
    {
        public FeedbackRepository(EventSphereContext context) : base(context) { }

        public async Task<(IEnumerable<TblFeedback> data, int totalCount)> GetPagedFeedbacksAsync(
            int pageIndex, int pageSize,
            int? eventId = null, int? studentId = null, int? rating = null,
            string? keyword = null)
        {
            var query = _dbSet
                .Include(f => f.Event)
                .Include(f => f.Student)
                    .ThenInclude(s => s.TblUserDetails)
                .AsQueryable();

            if (eventId.HasValue)
                query = query.Where(f => f.EventId == eventId.Value);

            if (studentId.HasValue)
                query = query.Where(f => f.StudentId == studentId.Value);

            if (rating.HasValue)
                query = query.Where(f => f.Rating == rating.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(f =>
                    (f.Event != null && EF.Functions.Like(f.Event.Title, $"%{keyword}%")) ||
                    (f.Student != null && f.Student.TblUserDetails.Any(d => EF.Functions.Like(d.Fullname, $"%{keyword}%"))) ||
                    (f.Comments != null && EF.Functions.Like(f.Comments, $"%{keyword}%"))
                );
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(f => f.SubmittedOn)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }
    }
}
