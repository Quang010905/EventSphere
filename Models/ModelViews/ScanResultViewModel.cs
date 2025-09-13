namespace EventSphere.Models.ModelViews
{
    public class ScanResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? CertificateUrl { get; set; }
    }
}
