using EventSphere.Models.entities;
using EventSphere.Models.ModelViews;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace EventSphere.Models.Repositories
{
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
                Role = user.Role ??0,
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
}
