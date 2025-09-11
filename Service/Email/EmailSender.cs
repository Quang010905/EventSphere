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
        public EmailSender(IOptions<MailSettings> mailOptions)
        {
            _mailSettings = mailOptions.Value;
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
    }
}
