using Microsoft.Extensions.Options;
using SPEAK.Abstraction.IServices;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SPEAK.Domain.Models.Helpers;
using MimeKit.Text;

namespace SPEAK.Service.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _emailSettings = options.Value;
        }

        public async Task SendAsync(EmailMessage emailMessage)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.Username ?? "SPEAK", _emailSettings.From ?? "speak.help.team@gmail.com"));
            message.To.Add(MailboxAddress.Parse(emailMessage.To ?? ""));
            message.Subject = emailMessage.Subject ?? "";
            message.Body = new TextPart(emailMessage.IsHtml ? TextFormat.Html : TextFormat.Plain)
            {
                Text = emailMessage.Content ?? ""
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailSettings.Host ?? "smtp.gmail.com", _emailSettings.Port, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(_emailSettings.From ?? "", _emailSettings.Password ?? "");
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}