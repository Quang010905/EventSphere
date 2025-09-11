namespace EventSphere.Models.ModelViews
{
    public class OtpSettings
    {
        public int Length { get; set; } = 6;
        public int ExpiryMinutes { get; set; } = 5;
        public int MaxAttempts {  get; set; } = 3;
    }
}
