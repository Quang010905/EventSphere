namespace EventSphere.Service.Email
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);

        Task SendEmailWithInlineImageAsync(string toEmail, string subject, string htmlMessage, byte[] imageBytes, string imageContentId = "qrImage", CancellationToken cancellationToken = default);
    }
}
