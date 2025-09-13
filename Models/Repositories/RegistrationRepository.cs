using EventSphere.Models.entities;
using EventSphere.Models.ModelViews;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EventSphere.Models.Repositories
{
    public class RegistrationRepository
    {
        private static RegistrationRepository _instance;
        private RegistrationRepository() { }

        public static RegistrationRepository Instance
        {
            get
            {
                _instance ??= new RegistrationRepository();
                return _instance;
            }
        }

        // Lấy toàn bộ đăng ký
        public List<RegistrationView> GetAll()
        {
            using var db = new EventSphereContext();
            return db.TblRegistrations
                     .Include(x => x.Event)
                     .Include(x => x.Student)
                        .ThenInclude(s => s.TblUserDetails)
                     .Select(x => new RegistrationView
                     {
                         Id = x.Id,
                         EventId = x.EventId ?? 0,
                         StudentId = x.StudentId ?? 0,
                         Status = x.Status ?? 0,
                         Venue = x.Event.Venue,
                         EventImage = x.Event.Image,
                         EventName = x.Event.Title,
                         EventDate = x.Event.Date,
                         EventTime = x.Event.Time ?? default,
                         RegisterOn = x.RegisteredOn ?? DateTime.Now,
                         StudentEmail = x.Student.Email,
                         StudentName = x.Student.TblUserDetails.FirstOrDefault().Fullname ?? x.Student.Email
                     }).ToList();
        }

        public void Add(RegistrationView entity)
        {
            using var db = new EventSphereContext();
            var item = new TblRegistration
            {
                StudentId = entity.StudentId,
                EventId = entity.EventId,
                RegisteredOn = DateTime.Now,
                Status = 0
            };
            db.TblRegistrations.Add(item);
            db.SaveChanges();
        }

        public bool Delete(int eventId, int userId)
        {
            using var db = new EventSphereContext();
            var item = db.TblRegistrations
                         .FirstOrDefault(r => r.EventId == eventId && r.StudentId == userId);

            if (item != null)
            {
                db.TblRegistrations.Remove(item);
                return db.SaveChanges() > 0;
            }
            return false;
        }

        public int? GetRegistrationStatus(int studentId, int eventId)
        {
            using var db = new EventSphereContext();
            var registration = db.TblRegistrations
                                 .FirstOrDefault(r => r.StudentId == studentId && r.EventId == eventId);
            return registration?.Status;
        }

        public bool CheckRegistered(int stuId, int eventId)
        {
            using var db = new EventSphereContext();
            return db.TblRegistrations.Any(r => r.StudentId == stuId && r.EventId == eventId);
        }

        public List<RegistrationView> GetRegistrationByStuId(int id)
        {
            using var db = new EventSphereContext();
            return db.TblRegistrations
                     .Where(x => x.StudentId == id)
                     .Include(x => x.Event)
                     .Include(x => x.Student)
                        .ThenInclude(s => s.TblUserDetails)
                     .Select(x => new RegistrationView
                     {
                         EventId = x.EventId ?? 0,
                         Status = x.Status ?? 0,
                         Venue = x.Event.Venue,
                         EventImage = x.Event.Image,
                         EventName = x.Event.Title,
                         EventDate = x.Event.Date,
                         EventTime = x.Event.Time ?? default,
                         RegisterOn = x.RegisteredOn ?? DateTime.Now,
                         StudentEmail = x.Student.Email,
                         StudentName = x.Student.TblUserDetails.FirstOrDefault().Fullname ?? x.Student.Email
                     }).ToList();
        }
        public string NormalizeSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string lower = input.ToLowerInvariant();
            string normalized = lower.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new();
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return new string(sb.ToString().Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        public RegistrationProcessResult ApproveAndCreateAttendance(int registrationId)
        {
            using var db = new EventSphereContext();
            using var tran = db.Database.BeginTransaction();
            try
            {
                // Cập nhật atomic: chỉ set status = 1 nếu đang pending (NULL hoặc 0)
                int updated = db.Database.ExecuteSqlInterpolated(
                    $"UPDATE dbo.tbl_registration SET _status = 1 WHERE _id = {registrationId} AND (_status IS NULL OR _status = 0)");

                if (updated == 0)
                {
                    // Đã được xử lý trước đó (hoặc không tồn tại) -> trả về thông tin để caller biết
                    var already = db.TblRegistrations
                                   .Include(r => r.Event)
                                   .Include(r => r.Student)
                                   .FirstOrDefault(r => r.Id == registrationId);

                    return new RegistrationProcessResult
                    {
                        RegistrationId = registrationId,
                        EventId = already?.EventId ?? 0,
                        EventName = already?.Event?.Title ?? "",
                        EventDate = already?.Event?.Date,
                        EventTime = already?.Event?.Time,
                        StudentId = already?.StudentId ?? 0,
                        StudentEmail = already?.Student?.Email ?? "",
                        StudentName = already?.Student?.TblUserDetails?.FirstOrDefault()?.Fullname ?? already?.Student?.Email ?? "",
                        AlreadyProcessed = true,
                        Message = "Registration đã được xử lý trước đó hoặc không thể duyệt."
                    };
                }

                // Lấy lại registration sau khi cập nhật status
                var reg = db.TblRegistrations
                            .Include(r => r.Event)
                            .Include(r => r.Student)
                            .FirstOrDefault(r => r.Id == registrationId);

                if (reg == null)
                    throw new InvalidOperationException("Registration not found after update.");

                var evt = reg.Event;
                var student = reg.Student;

                // --- 1) Kiểm tra TblEventSeatings (nếu có) ---
                var seating = db.TblEventSeatings.FirstOrDefault(s => s.EventId == reg.EventId);
                if (seating != null && (seating.SeatsAvailable ?? 0) <= 0)
                {
                    // Hết chỗ -> đưa vào waitlist
                    var wait = new TblEventWaitlist
                    {
                        UserId = reg.StudentId,
                        EventId = reg.EventId,
                        WaitlistTime = DateTime.Now,
                        Status = 0
                    };
                    db.TblEventWaitlists.Add(wait);

                    // Cập nhật registration status sang "waitlisted" (ví dụ 3)
                    reg.Status = 3;
                    db.TblRegistrations.Update(reg);

                    db.SaveChanges();
                    tran.Commit();

                    return new RegistrationProcessResult
                    {
                        RegistrationId = reg.Id,
                        WaitlistId = wait.Id,
                        EventId = reg.EventId ?? 0,
                        EventName = evt?.Title ?? "",
                        EventDate = evt?.Date,
                        EventTime = evt?.Time,
                        StudentId = reg.StudentId ?? 0,
                        StudentEmail = student?.Email ?? "",
                        StudentName = student?.TblUserDetails?.FirstOrDefault()?.Fullname ?? student?.Email ?? "",
                        IsWaitlisted = true,
                        Message = "Sự kiện đã đầy. Đã chuyển vào danh sách chờ."
                    };
                }

                // --- 2) Nếu có seating và còn chỗ: cập nhật seating trước khi tạo attendance ---
                TblAttendance attendance = db.TblAttendances
                    .FirstOrDefault(a => a.EventId == reg.EventId && a.StudentId == reg.StudentId);

                if (seating != null)
                {
                    // Nếu chưa có attendance thì tăng booked
                    if (attendance == null)
                    {
                        seating.SeatsBooked = (seating.SeatsBooked ?? 0) + 1;
                        seating.SeatsAvailable = Math.Max(0, (seating.SeatsAvailable ?? 0) - 1);
                        db.TblEventSeatings.Update(seating);
                    }
                }
                else
                {
                    // --- 3) Nếu không có seating: fallback kiểm tra capacity qua property (reflection) hoặc đếm attendance hiện tại ---
                    int? capacity = null;
                    try
                    {
                        var eventType = evt?.GetType();
                        string[] candidateNames = new[] { "Capacity", "SeatCapacity", "MaxSeats", "TotalSeats", "SeatingCapacity", "Seats" };
                        foreach (var n in candidateNames)
                        {
                            var prop = eventType?.GetProperty(n);
                            if (prop != null)
                            {
                                var v = prop.GetValue(evt);
                                if (v != null && int.TryParse(v.ToString(), out var c) && c > 0)
                                {
                                    capacity = c;
                                    break;
                                }
                            }
                        }
                    }
                    catch { /* ignore */ }

                    if (capacity.HasValue)
                    {
                        var reservedCount = db.TblAttendances.Count(a => a.EventId == evt.Id);
                        if (reservedCount >= capacity.Value)
                        {
                            // chuyển vào waitlist
                            var wait = new TblEventWaitlist
                            {
                                UserId = reg.StudentId,
                                EventId = reg.EventId,
                                WaitlistTime = DateTime.Now,
                                Status = 0
                            };
                            db.TblEventWaitlists.Add(wait);

                            reg.Status = 3;
                            db.TblRegistrations.Update(reg);

                            db.SaveChanges();
                            tran.Commit();

                            return new RegistrationProcessResult
                            {
                                RegistrationId = reg.Id,
                                WaitlistId = wait.Id,
                                EventId = reg.EventId ?? 0,
                                EventName = evt?.Title ?? "",
                                EventDate = evt?.Date,
                                EventTime = evt?.Time,
                                StudentId = reg.StudentId ?? 0,
                                StudentEmail = student?.Email ?? "",
                                StudentName = student?.TblUserDetails?.FirstOrDefault()?.Fullname ?? student?.Email ?? "",
                                IsWaitlisted = true,
                                Message = "Sự kiện đã đầy (theo capacity). Đã chuyển vào danh sách chờ."
                            };
                        }
                    }
                    // nếu không có capacity hoặc còn chỗ -> tiếp tục tạo attendance
                }

                // --- Tạo attendance nếu chưa có ---
                if (attendance == null)
                {
                    attendance = new TblAttendance
                    {
                        EventId = reg.EventId,
                        StudentId = reg.StudentId,
                        Attended = false,
                        MarkedOn = null // chưa điểm danh
                                        // nếu entity của bạn có CreatedOn/CreatedDate, có thể set thêm ở đây
                    };
                    db.TblAttendances.Add(attendance);
                }

                // Đảm bảo registration marked accepted = 1 (mặc dù đã update ở đầu, nhưng vẫn an toàn)
                reg.Status = 1;
                db.TblRegistrations.Update(reg);

                db.SaveChanges();
                tran.Commit();

                return new RegistrationProcessResult
                {
                    RegistrationId = reg.Id,
                    AttendanceId = attendance.Id,
                    EventId = reg.EventId ?? 0,
                    EventName = evt?.Title ?? "",
                    EventDate = evt?.Date,
                    EventTime = evt?.Time,
                    StudentId = reg.StudentId ?? 0,
                    StudentEmail = student?.Email ?? "",
                    StudentName = student?.TblUserDetails?.FirstOrDefault()?.Fullname ?? student?.Email ?? "",
                    IsWaitlisted = false,
                    Message = "Đã duyệt và tạo attendance."
                };
            }
            catch
            {
                try { tran.Rollback(); } catch { }
                throw;
            }
        }


        public void DenyRegistration(int registrationId)
        {
            using var db = new EventSphereContext();
            using var tran = db.Database.BeginTransaction();
            var reg = db.TblRegistrations.FirstOrDefault(r => r.Id == registrationId);
            if (reg == null) throw new InvalidOperationException("Registration not found.");

            int currentStatus = reg.Status ?? 0;
            if (currentStatus == 2)
                throw new InvalidOperationException("Registration already denied.");

            reg.Status = 2;
            db.TblRegistrations.Update(reg);

            var seating = db.TblEventSeatings.FirstOrDefault(s => s.EventId == reg.EventId);
            if (seating != null && currentStatus == 1)
            {
                seating.SeatsBooked = Math.Max(0, (seating.SeatsBooked ?? 0) - 1);
                seating.SeatsAvailable = (seating.SeatsAvailable ?? 0) + 1;
                db.TblEventSeatings.Update(seating);
            }

            var attendance = db.TblAttendances.FirstOrDefault(a => a.EventId == reg.EventId && a.StudentId == reg.StudentId);
            if (attendance != null) db.TblAttendances.Remove(attendance);

            db.SaveChanges();
            tran.Commit();
        }
    }
}
