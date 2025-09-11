namespace EventSphere.Service.Otp
{
    public interface IOtpService
    {
        string GenerateNumericOtp(int length = 6);
        void StoreOtp(string key, string otp, TimeSpan expiry);
        bool ValidateOtp(string key, string enteredOtp);
        void RemoveOtp(string key);
        int GetRemainingAttempts(string key);
    }
}
