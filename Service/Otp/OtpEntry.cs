namespace EventSphere.Service.Otp
{
    public class OtpEntry
    {
        public string OtpHash { get; set; }
        public int Attempts { get; set; }
        public int MaxAttempts { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
