using EventSphere.Models.ModelViews;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace EventSphere.Service.Otp
{
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private readonly OtpSettings _otpSettings;

        public OtpService(IMemoryCache cache, IOptions<OtpSettings> otpOptions)
        {
            _cache = cache;
            _otpSettings = otpOptions.Value;
        }

        // Lấy thời gian Việt Nam (nếu cần)
        private DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        }

        public void StoreOtp(string key, string otp, TimeSpan expiry)
        {
            var entry = new OtpEntry
            {
                OtpHash = Hash(otp),
                Attempts = 0,
                MaxAttempts = _otpSettings.MaxAttempts,
            };

            // lưu cache với thời gian hết hạn giống ExpiresAt
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            _cache.Set(key, entry, cacheOptions);
        }

        public bool ValidateOtp(string key, string enteredOtp)
        {
            // Lấy entry từ cache
            if (!_cache.TryGetValue<OtpEntry>(key, out var entry))
                return false; // cache không tồn tại → hết hạn hoặc chưa tạo

            // Kiểm tra số lần thử
            if (entry.Attempts >= entry.MaxAttempts)
            {
                _cache.Remove(key);
                return false;
            }

            // Kiểm tra OTP đúng
            if (entry.OtpHash == Hash(enteredOtp))
            {
                _cache.Remove(key); // OTP đúng → xóa khỏi cache
                return true;
            }
            else
            {
                // OTP sai → tăng Attempts, giữ thời gian còn lại
                entry.Attempts++;
                // Lấy thời gian còn lại để refresh cache
                var now = DateTime.UtcNow;
                var ttl = entry.ExpiresAt - now;
                if (ttl > TimeSpan.Zero)
                {
                    _cache.Set(key, entry, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = ttl
                    });
                }
                else
                {
                    // Hết hạn → xóa
                    _cache.Remove(key);
                }

                return false;
            }
        }



        public string GenerateNumericOtp(int length = 6)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            uint random = BitConverter.ToUInt32(bytes, 0);
            var max = (uint)Math.Pow(10, length);
            var val = random % max;
            return val.ToString($"D{length}");
        }

        private string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        public void RemoveOtp(string key)
        {
            _cache.Remove(key);
        }

        public int GetRemainingAttempts(string key)
        {
            if (!_cache.TryGetValue<OtpEntry>(key, out var entry))
                return 0;

            return Math.Max(0, entry.MaxAttempts - entry.Attempts);
        }

    }
}
