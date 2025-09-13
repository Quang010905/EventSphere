using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EventSphere.Models.entities;
using EventSphere.Models.ModelViews;
using Microsoft.AspNetCore.Hosting;

namespace EventSphere.Models.Repositories
{
    /// <summary>
    /// Repository xử lý logic liên quan tới profile (lấy, chuẩn bị edit model, cập nhật, upload/xóa ảnh).
    /// Sử dụng generic IRepository<T> có sẵn trong project.
    /// </summary>
    public class ProfileRepository
    {
        private readonly IRepository<TblUser> _userRepo;
        private readonly IRepository<TblUserDetail> _detailRepo;
        private readonly EventSphereContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileRepository(
            IRepository<TblUser> userRepo,
            IRepository<TblUserDetail> detailRepo,
            EventSphereContext context,
            IWebHostEnvironment env)
        {
            _userRepo = userRepo;
            _detailRepo = detailRepo;
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Lấy Profile (User + UserDetail) để hiển thị.
        /// </summary>
        public async Task<ProfileViewModel?> GetProfileAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return null;

            var detail = (await _detailRepo.FindAsync(d => d.UserId == userId)).FirstOrDefault();
            return new ProfileViewModel { User = user, Detail = detail };
        }

        /// <summary>
        /// Lấy model dùng cho màn Edit (ProfileEditModel).
        /// </summary>
        public async Task<ProfileEditModel?> GetProfileForEditAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return null;

            var detail = (await _detailRepo.FindAsync(d => d.UserId == userId)).FirstOrDefault();

            return new ProfileEditModel
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                Status = user.Status,
                Fullname = detail?.Fullname,
                Department = detail?.Department,
                EnrollmentNo = detail?.EnrollmentNo,
                Phone = detail?.Phone,
                ExistingImage = detail?.Image
            };
        }

        public async Task<bool> UpdateProfileAsync(ProfileEditModel model)
        {
            var user = await _userRepo.GetByIdAsync(model.UserId);
            if (user == null) return false;

            var detail = (await _detailRepo.FindAsync(d => d.UserId == model.UserId)).FirstOrDefault();

            // Chỉ update khi có giá trị (không ép null vào DB)
            if (!string.IsNullOrWhiteSpace(model.Email))
                user.Email = model.Email;

            if (model.Role.HasValue)
                user.Role = model.Role.Value;

            if (model.Status.HasValue)
                user.Status = model.Status.Value;

            _userRepo.Update(user);

            // Xử lý ảnh
            string? savedRelativePath = null;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "profiles");
                if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

                var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(model.ImageFile.FileName)}";
                var savePath = Path.Combine(uploadsRoot, fileName);

                using (var fs = new FileStream(savePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fs);
                }

                savedRelativePath = $"/uploads/profiles/{fileName}";

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrWhiteSpace(model.ExistingImage))
                {
                    try
                    {
                        var oldPhysical = Path.Combine(_env.WebRootPath ?? "wwwroot",
                            model.ExistingImage.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(oldPhysical)) File.Delete(oldPhysical);
                    }
                    catch { }
                }
            }

            // Update detail
            if (detail != null)
            {
                if (!string.IsNullOrWhiteSpace(model.Fullname))
                    detail.Fullname = model.Fullname;
                if (!string.IsNullOrWhiteSpace(model.Department))
                    detail.Department = model.Department;
                if (!string.IsNullOrWhiteSpace(model.EnrollmentNo))
                    detail.EnrollmentNo = model.EnrollmentNo;
                if (!string.IsNullOrWhiteSpace(model.Phone))
                    detail.Phone = model.Phone;
                if (savedRelativePath != null)
                    detail.Image = savedRelativePath;

                _detailRepo.Update(detail);
            }
            else
            {
                var newDetail = new TblUserDetail
                {
                    UserId = model.UserId,
                    Fullname = model.Fullname,
                    Department = model.Department,
                    EnrollmentNo = model.EnrollmentNo,
                    Phone = model.Phone,
                    Image = savedRelativePath
                };
                await _detailRepo.AddAsync(newDetail);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Xóa ảnh profile (cập nhật DB và xóa file vật lý). Trả về true nếu đã xóa.
        /// </summary>
        public async Task<bool> DeleteImageAsync(int userId)
        {
            var detail = (await _detailRepo.FindAsync(d => d.UserId == userId)).FirstOrDefault();
            if (detail == null || string.IsNullOrWhiteSpace(detail.Image)) return false;

            try
            {
                var physical = Path.Combine(_env.WebRootPath ?? "wwwroot", detail.Image.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(physical)) File.Delete(physical);
            }
            catch
            {
                // ignore
            }

            detail.Image = null;
            _detailRepo.Update(detail);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}