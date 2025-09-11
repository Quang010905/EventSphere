using EventSphere.Models.entities;
using EventSphere.Models.ModelViews;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace EventSphere.Models.Repositories
{
    // --- KEEP EXACTLY AS PROVIDED BY YOU (no changes) ---
    public class UserRepository
    {
        private static UserRepository _instance = null;
        private UserRepository() { }
        public static UserRepository Instance
        {
            get
            {
                _instance = _instance ?? new UserRepository();
                return _instance;
            }
        }
        public string HashMD5(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }
        public UserView? GetUserByEmail(string email)
        {
            var db = new EventSphereContext();
            var normalizedEmail = email?.Trim().ToLower();
            var user = db.TblUsers
                         .FirstOrDefault(u => u.Email != null && u.Email.ToLower() == normalizedEmail);

            if (user == null) return null;

            return new UserView
            {
                Id = user.Id,
                Email = user.Email,
                Password = user.Password,
                Role = user.Role ?? 0,
            };
        }




        public void Add(UserView entity)
        {
            var db = new EventSphereContext();
            try
            {
                var item = new TblUser
                {
                    Password = HashMD5(entity.Password),
                    Email = entity.Email,
                    Role = 1,
                    Status = 1,
                    CreatedAt = DateTime.Now,
                };
                db.TblUsers.Add(item);
                db.SaveChanges();
                var detail = new TblUserDetail
                {
                    UserId = item.Id,
                    Fullname = entity.UserDetail.FullName,
                    Department = entity.UserDetail.Department,
                    Phone = entity.UserDetail.Phone,
                    EnrollmentNo = entity.UserDetail.EnrollmentNo,
                    Image = entity.UserDetail.Image,
                };
                db.TblUserDetails.Add(detail);
                db.SaveChanges();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool checkEmail(string Email, int? excludeId = null)
        {
            try
            {
                using (var db = new EventSphereContext())
                {
                    string normalizedInput = NormalizeName(Email);
                    var allEmails = db.TblUsers.Where(c => !excludeId.HasValue || c.Id != excludeId.Value)
                                              .Select(c => c.Email)
                                              .ToList();
                    // 2. So sánh sau khi normalize từng cái
                    return allEmails.Any(dbName => NormalizeName(dbName) == normalizedInput);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public bool checkPhone(string Phone, int? excludeId = null)
        {
            try
            {
                using (var db = new EventSphereContext())
                {
                    string normalizedInput = NormalizeName(Phone);
                    var allPhones = db.TblUserDetails.Where(c => !excludeId.HasValue || c.Id != excludeId.Value)
                                              .Select(c => c.Phone)
                                              .ToList();
                    // 2. So sánh sau khi normalize từng cái
                    return allPhones.Any(dbName => NormalizeName(dbName) == normalizedInput);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool checkEnrollmentNo(string EnrollmentNo, int? excludeId = null)
        {
            try
            {
                using (var db = new EventSphereContext())
                {
                    string normalizedInput = NormalizeName(EnrollmentNo);
                    var allEnrollmentNos = db.TblUserDetails.Where(c => !excludeId.HasValue || c.Id != excludeId.Value)
                                              .Select(c => c.EnrollmentNo)
                                              .ToList();
                    // 2. So sánh sau khi normalize từng cái
                    return allEnrollmentNos.Any(dbName => NormalizeName(dbName) == normalizedInput);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string NormalizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Xoá toàn bộ khoảng trắng và đưa về lowercase
            return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLower();
        }
        public UserView? checkLogin(string email, string password)
        {
            try
            {
                var bs = new EventSphereContext();
                var user = bs.TblUsers.FirstOrDefault(m =>
                    m.Email == email &&
                    m.Password == password &&
                    m.Status == 1);

                if (user == null)
                {
                    return null;
                }

                var uv = new UserView
                {

                    Email = user.Email,
                    Password = user.Password,
                    Role = user.Role ?? 0
                };

                return uv;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in checkLogin: " + ex.Message);
                return null;
            }
        }
    }

    // --- RENAMED version of the second class to avoid duplicate class name ---
    // original class inherited Repository<TblUser>; keep all methods but rename class to UserRepositoryEf
    public class UserRepositoryEf : Repository<TblUser>
    {
        private readonly EventSphereContext _context;
        public const int STATUS_ACTIVE = 1;
        public const int STATUS_DISABLED = 0;

        public UserRepositoryEf(EventSphereContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<TblUser> data, int totalCount)> GetPagedUsersAsync(
            int pageIndex, int pageSize,
            int? role = null, int? status = null,
            string? keyword = null)
        {
            var query = _dbSet
                .Include(u => u.TblUserDetails)
                .AsQueryable();

            if (role.HasValue) query = query.Where(u => u.Role == role.Value);
            if (status.HasValue) query = query.Where(u => u.Status == status.Value);

            if (!string.IsNullOrEmpty(keyword))
            {
                string kw = keyword.Trim();
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(kw)) ||
                    u.TblUserDetails.Any(d =>
                        (d.Fullname != null && d.Fullname.Contains(kw)) ||
                        (d.Department != null && d.Department.Contains(kw)) ||
                        (d.Phone != null && d.Phone.Contains(kw)) ||
                        (d.EnrollmentNo != null && d.EnrollmentNo.Contains(kw))
                    ));
            }

            int totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }

        // Get user with details only (avoid multiple collection includes)
        public async Task<TblUser?> GetUserWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(u => u.TblUserDetails)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // Toggle: now returns new status (0/1) or null if not found
        public async Task<int?> ToggleStatusAsync(int userId)
        {
            var user = await _dbSet.FindAsync(userId);
            if (user == null) return null;

            int newStatus = (user.Status == STATUS_ACTIVE) ? STATUS_DISABLED : STATUS_ACTIVE;
            user.Status = newStatus;
            _context.TblUsers.Update(user);
            await _context.SaveChangesAsync();
            return newStatus;
        }

        public async Task SetStatusAsync(int id, int status)
        {
            var user = await _dbSet.FindAsync(id);
            if (user != null)
            {
                user.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateRoleAsync(int id, int role)
        {
            var user = await _dbSet.FindAsync(id);
            if (user != null)
            {
                user.Role = role;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdatePasswordAsync(int id, string newPlainPassword)
        {
            var user = await _dbSet.FindAsync(id);
            if (user != null)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPlainPassword);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _dbSet
                .Include(u => u.TblUserDetails)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user != null)
            {
                if (user.TblUserDetails != null && user.TblUserDetails.Any())
                {
                    _context.TblUserDetails.RemoveRange(user.TblUserDetails);
                }

                _dbSet.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        // SAFE cascade delete: delete related rows per table, then user
        public async Task<bool> DeleteUserCascadeAsync(int userId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var attendances = await _context.TblAttendances.Where(a => a.StudentId == userId).ToListAsync();
                if (attendances.Any()) _context.TblAttendances.RemoveRange(attendances);

                var calendarSyncs = await _context.TblCalendarSyncs.Where(c => c.UserId == userId).ToListAsync();
                if (calendarSyncs.Any()) _context.TblCalendarSyncs.RemoveRange(calendarSyncs);

                var certificates = await _context.TblCertificates.Where(c => c.StudentId == userId).ToListAsync();
                if (certificates.Any()) _context.TblCertificates.RemoveRange(certificates);

                var shareLogs = await _context.TblEventShareLogs.Where(s => s.UserId == userId).ToListAsync();
                if (shareLogs.Any()) _context.TblEventShareLogs.RemoveRange(shareLogs);

                var waitlists = await _context.TblEventWaitlists.Where(w => w.UserId == userId).ToListAsync();
                if (waitlists.Any()) _context.TblEventWaitlists.RemoveRange(waitlists);

                var feedbacks = await _context.TblFeedbacks.Where(f => f.StudentId == userId).ToListAsync();
                if (feedbacks.Any()) _context.TblFeedbacks.RemoveRange(feedbacks);

                var media = await _context.TblMediaGalleries.Where(m => m.UploadedBy == userId).ToListAsync();
                if (media.Any()) _context.TblMediaGalleries.RemoveRange(media);
                var registrations = await _context.TblRegistrations.Where(r => r.StudentId == userId).ToListAsync();
                if (registrations.Any()) _context.TblRegistrations.RemoveRange(registrations);

                // events owned by user (organizer)
                var userEventIds = await _context.TblEvents.Where(e => e.OrganizerId == userId).Select(e => e.Id).ToListAsync();

                if (userEventIds.Any())
                {
                    var seatings = await _context.TblEventSeatings
                        .Where(s => userEventIds.Contains(s.Id))
                        .ToListAsync();

                    if (seatings.Any()) _context.TblEventSeatings.RemoveRange(seatings);

                    var evCertificates = await _context.TblCertificates.Where(c => c.EventId.HasValue && userEventIds.Contains(c.EventId.Value)).ToListAsync();
                    if (evCertificates.Any()) _context.TblCertificates.RemoveRange(evCertificates);

                    var evShareLogs = await _context.TblEventShareLogs.Where(s => s.EventId.HasValue && userEventIds.Contains(s.EventId.Value)).ToListAsync();
                    if (evShareLogs.Any()) _context.TblEventShareLogs.RemoveRange(evShareLogs);

                    var evWaitlists = await _context.TblEventWaitlists.Where(w => w.EventId.HasValue && userEventIds.Contains(w.EventId.Value)).ToListAsync();
                    if (evWaitlists.Any()) _context.TblEventWaitlists.RemoveRange(evWaitlists);

                    var evMedia = await _context.TblMediaGalleries.Where(m => m.EventId.HasValue && userEventIds.Contains(m.EventId.Value)).ToListAsync();
                    if (evMedia.Any()) _context.TblMediaGalleries.RemoveRange(evMedia);

                    var evRegs = await _context.TblRegistrations.Where(r => r.EventId.HasValue && userEventIds.Contains(r.EventId.Value)).ToListAsync();
                    if (evRegs.Any()) _context.TblRegistrations.RemoveRange(evRegs);

                    var evFeedbacks = await _context.TblFeedbacks.Where(f => f.EventId.HasValue && userEventIds.Contains(f.EventId.Value)).ToListAsync();
                    if (evFeedbacks.Any()) _context.TblFeedbacks.RemoveRange(evFeedbacks);

                    var events = await _context.TblEvents.Where(e => userEventIds.Contains(e.Id)).ToListAsync();
                    if (events.Any()) _context.TblEvents.RemoveRange(events);
                }

                var userDetails = await _context.TblUserDetails.Where(d => d.UserId == userId).ToListAsync();
                if (userDetails.Any()) _context.TblUserDetails.RemoveRange(userDetails);

                var user = await _context.TblUsers.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    _context.TblUsers.Remove(user);
                }
                else
                {
                    await tx.RollbackAsync();
                    return false;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
