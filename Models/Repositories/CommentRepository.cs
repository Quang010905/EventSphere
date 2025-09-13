using EventSphere.Models.entities;
using EventSphere.Models.ModelViews;
using Microsoft.EntityFrameworkCore;

namespace EventSphere.Models.Repositories
{
    public class CommentRepository
    {
        private static CommentRepository _instance = null;
        private CommentRepository() { }
        public static CommentRepository Instance
        {
            get
            {
                _instance = _instance ?? new CommentRepository();
                return _instance;
            }
        }

        public async Task AddAsync(FeedbackView entity)
        {
            using var db = new EventSphereContext();
            try
            {
                // Không cho quá 5 cmt trong X phút
                if (await IsSpamAsync(entity.StudentId, 5, 5))
                {
                    throw new Exception("You have reached the comment limit. Please wait a few minutes before trying again.");
                }

                var item = new TblFeedback
                {
                    EventId = entity.EventId,
                    StudentId = entity.StudentId,
                    Comments = entity.Comments,
                    SubmittedOn = DateTime.Now,
                    Rating = entity.Rating,
                    Status = entity.Status // 0 = pending
                };

                db.TblFeedbacks.Add(item);
                await db.SaveChangesAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> IsSpamAsync(int studentId, int limit, int minutes)
        {
            using var db = new EventSphereContext();
            var fromTime = DateTime.Now.AddMinutes(-minutes);

            var count = await db.TblFeedbacks
                .Where(f => f.StudentId == studentId && f.SubmittedOn >= fromTime)
                .CountAsync();

            return count >= limit;
        }


        public async Task<List<TblFeedback>> GetFeedbacksAsync(int eventId, int currentUserId = 0)
        {
            using var db = new EventSphereContext();

            return await db.TblFeedbacks
                .Where(f => f.EventId == eventId &&
                            (f.Status == 1 || f.StudentId == currentUserId))
                .Include(f => f.Student)
                    .ThenInclude(s => s.TblUserDetails)
                .OrderByDescending(f => f.SubmittedOn)
                .ToListAsync();
        }


        // NEW: xóa comment (chỉ chủ sở hữu mới xóa được)
        public async Task<bool> DeleteAsync(int feedbackId, int requestingStudentId)
        {
            using var db = new EventSphereContext();
            var item = await db.TblFeedbacks.FirstOrDefaultAsync(f => f.Id == feedbackId);
            if (item == null) return false;

            if (item.StudentId != requestingStudentId)
            {
                // không cho xóa nếu không phải chủ sở hữu
                throw new UnauthorizedAccessException("Bạn không có quyền xóa bình luận này.");
            }

            db.TblFeedbacks.Remove(item);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task UpdateStatusAsync(int feedbackId, int newStatus)
        {
            using var db = new EventSphereContext();
            var item = await db.TblFeedbacks.FirstOrDefaultAsync(f => f.Id == feedbackId);
            if (item == null) throw new Exception("Feedback not found.");
            item.Status = newStatus;
            await db.SaveChangesAsync();
        }
    }
}
