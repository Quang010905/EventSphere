using EventSphere.Models.ModelViews;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
namespace EventSphere.Service.Email
{
    public class EmailSender:IEmailSender
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<EmailSender> _logger;
        public EmailSender(IOptions<MailSettings> mailOptions, ILogger<EmailSender> logger)
        {
            _mailSettings = mailOptions.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_mailSettings.SenderName, _mailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            // 1. Kết nối SMTP
            await client.ConnectAsync(_mailSettings.SmtpServer, _mailSettings.SmtpPort, SecureSocketOptions.StartTls);

            // 2. Xác thực nếu cần
            if (!string.IsNullOrEmpty(_mailSettings.Username))
            {
                await client.AuthenticateAsync(_mailSettings.Username, _mailSettings.Password);
            }

            // 3. Gửi mail
            await client.SendAsync(message);

            // 4. Ngắt kết nối
            await client.DisconnectAsync(true);
        }

        public async Task SendEmailWithInlineImageAsync(string toEmail, string subject, string htmlMessage, byte[] imageBytes, string imageContentId = "qrImage", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) throw new ArgumentException("Recipient email is null or empty.", nameof(toEmail));
            if (string.IsNullOrWhiteSpace(_mailSettings?.SenderEmail)) throw new InvalidOperationException("MailSettings.SenderEmail chưa cấu hình.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_mailSettings.SenderName ?? string.Empty, _mailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject ?? string.Empty;

            var builder = new BodyBuilder();
            builder.HtmlBody = htmlMessage ?? string.Empty;

            if (imageBytes == null) imageBytes = Array.Empty<byte>();

            // Add inline resource
            var linked = builder.LinkedResources.Add($"{imageContentId}.png", imageBytes);
            linked.ContentId = imageContentId;
            linked.ContentType.MediaType = "image";
            linked.ContentType.MediaSubtype = "png";
            linked.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                var socketOption = _mailSettings.SmtpPort == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(_mailSettings.SmtpServer, _mailSettings.SmtpPort, socketOption, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_mailSettings.Username))
                {
                    await client.AuthenticateAsync(_mailSettings.Username, _mailSettings.Password, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);
                _logger?.LogInformation("Email (inline image) sent to {To}", toEmail);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send email (inline image) to {To}", toEmail);
                throw;
            }
            finally
            {
                try
                {
                    if (client.IsConnected)
                        await client.DisconnectAsync(true, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error disconnecting SMTP client");
                }
            }
        }
    }
}
