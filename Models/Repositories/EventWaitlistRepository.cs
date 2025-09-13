using EventSphere.Models.entities;
using EventSphere.Models.ModelViews;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventSphere.Models.Repositories
{
    public class EventWaitlistRepository
    {
        private static EventWaitlistRepository _instance;
        private EventWaitlistRepository() { }
        public static EventWaitlistRepository Instance => _instance ??= new EventWaitlistRepository();

        /// <summary>
        /// Lấy danh sách waitlist phân trang, filter theo event và tìm kiếm tên/email.
        /// </summary>
        public PagedWaitlistResult GetPaged(
     int pageIndex,
     int pageSize,
     int organizerId,            // 👈 thêm tham số này
     int? eventId = null,
     string? keyword = null)
        {
            using var db = new EventSphereContext();

            var q = db.TblEventWaitlists
                      .Include(w => w.Event)
                      .Include(w => w.User)
                         .ThenInclude(u => u.TblUserDetails)
                      // chỉ các waitlist của sự kiện do organizer này tạo
                      .Where(w => w.Event.OrganizerId == organizerId)   // 👈 lọc quan trọng
                      .AsQueryable();

            if (eventId.HasValue && eventId.Value > 0)
                q = q.Where(w => w.EventId == eventId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLowerInvariant();
                q = q.Where(w =>
                    (w.User != null &&
                        (w.User.Email.ToLower().Contains(k) ||
                         (w.User.TblUserDetails.Any() &&
                          w.User.TblUserDetails.FirstOrDefault().Fullname.ToLower().Contains(k))))
                    || (w.Event != null && w.Event.Title.ToLower().Contains(k))
                );
            }

            var total = q.Count();

            var raw = q.OrderByDescending(w => w.WaitlistTime)
                       .Skip((pageIndex - 1) * pageSize)
                       .Take(pageSize)
                       .ToList();

            var items = raw.Select(w =>
            {
                DateTime? eventDateTime = null;
                if (w.Event?.Date != null)
                {
                    var d = w.Event.Date.Value;
                    eventDateTime = new DateTime(d.Year, d.Month, d.Day);
                }

                return new WaitlistItemView
                {
                    Id = w.Id,
                    UserId = w.UserId,
                    EventId = w.EventId,
                    WaitlistTime = w.WaitlistTime,
                    Status = w.Status,
                    EventName = w.Event?.Title ?? "",
                    EventDate = eventDateTime,
                    EventVenue = w.Event?.Venue ?? "",
                    StudentEmail = w.User?.Email ?? "",
                    StudentName = w.User?.TblUserDetails?.FirstOrDefault()?.Fullname ?? w.User?.Email ?? ""
                };
            }).ToList();

            return new PagedWaitlistResult
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy danh sách events (id,title) để fill dropdown filter.
        /// </summary>
        public List<SelectListItem> GetEventListForFilter(int organizerId)
        {
            using var db = new EventSphereContext();
            return db.TblEvents
                     .Where(e => e.OrganizerId == organizerId)
                     .OrderBy(e => e.Title)
                     .Select(e => new SelectListItem
                     {
                         Value = e.Id.ToString(),
                         Text = e.Title
                     })
                     .ToList();
        }


        /// <summary>
        /// Xác nhận waitlist: cố gắng chuyển thành attendance.
        /// Nếu thành công sẽ XÓA entry khỏi tbl_eventWaitlist.
        /// Trả về object mô tả kết quả.
        /// </summary>
        public (bool Success, string Message, int? AttendanceId, int? WaitlistId) ConfirmWaitlist(int waitlistId)
        {
            using var db = new EventSphereContext();
            using var tran = db.Database.BeginTransaction();
            try
            {
                var wait = db.TblEventWaitlists.FirstOrDefault(w => w.Id == waitlistId);
                if (wait == null)
                    return (false, "Không tìm thấy bản ghi trong danh sách chờ.", null, null);

                // Lấy event & user
                var evt = db.TblEvents.FirstOrDefault(e => e.Id == wait.EventId);
                var user = db.TblUsers
                             .Include(u => u.TblUserDetails)
                             .FirstOrDefault(u => u.Id == wait.UserId);

                if (evt == null)
                    return (false, "Event không tồn tại.", null, wait.Id);

                // Nếu đã có attendance thì xóa waitlist và return existing id
                var existingAttendance = db.TblAttendances
                                           .FirstOrDefault(a => a.EventId == wait.EventId && a.StudentId == wait.UserId);
                if (existingAttendance != null)
                {
                    int removedWaitId = wait.Id;
                    db.TblEventWaitlists.Remove(wait);
                    db.SaveChanges();
                    tran.Commit();
                    return (true, "Sinh viên đã có attendance trước đó. Bản ghi danh sách chờ đã được xóa.", existingAttendance.Id, removedWaitId);
                }

                // Kiểm tra chỗ ngồi / capacity như trước
                var seating = db.TblEventSeatings.FirstOrDefault(s => s.EventId == wait.EventId);
                if (seating != null)
                {
                    if ((seating.SeatsAvailable ?? 0) <= 0)
                        return (false,
                            "Sự kiện hiện đã hết chỗ. Vui lòng chờ khi có chỗ trống — bạn có thể thử xác nhận lại sau.",
                            null,
                            wait.Id);

                    seating.SeatsBooked = (seating.SeatsBooked ?? 0) + 1;
                    seating.SeatsAvailable = Math.Max(0, (seating.SeatsAvailable ?? 0) - 1);
                    db.TblEventSeatings.Update(seating);
                }

                // tạo attendance
                var attendance = new TblAttendance
                {
                    EventId = wait.EventId,
                    StudentId = wait.UserId,
                    Attended = false,
                    MarkedOn = null
                };
                db.TblAttendances.Add(attendance);

                // XÓA waitlist entry
                int removedId = wait.Id;
                db.TblEventWaitlists.Remove(wait);

                // --- MỚI: CẬP NHẬT TblRegistration ---
                var registration = db.TblRegistrations
                                     .FirstOrDefault(r => r.EventId == wait.EventId && r.StudentId == wait.UserId);
                if (registration != null)
                {
                    registration.Status = 1; // confirmed
                    db.TblRegistrations.Update(registration);
                }

                db.SaveChanges();
                tran.Commit();

                return (true, "Xác nhận thành công — đã tạo attendance, xóa khỏi danh sách chờ và cập nhật registration.", attendance.Id, removedId);
            }
            catch (Exception ex)
            {
                try { tran.Rollback(); } catch { }
                return (false, "Lỗi khi xác nhận: " + ex.Message, null, null);
            }
        }


        /// <summary>
        /// Xóa 1 bản ghi waitlist.
        /// </summary>
        public (bool Success, string Message) DeleteWaitlist(int waitlistId)
        {
            using var db = new EventSphereContext();
            try
            {
                var w = db.TblEventWaitlists.FirstOrDefault(x => x.Id == waitlistId);
                if (w == null) return (false, "Không tìm thấy bản ghi.");
                db.TblEventWaitlists.Remove(w);
                db.SaveChanges();
                return (true, "Đã xóa bản ghi khỏi danh sách chờ.");
            }
            catch (Exception ex)
            {
                return (false, "Lỗi khi xóa: " + ex.Message);
            }
        }
    }
}
