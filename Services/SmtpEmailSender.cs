using MailKit.Net.Smtp;
using MimeKit;

namespace LMS_Backend.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    public SmtpEmailSender(IConfiguration config) => _config = config;

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);

        var fromAddress = _config["Email:From"];
        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException("Email:From configuration is missing.");
        }

        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(fromAddress));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;

        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_config["Email:SmtpHost"], int.Parse(_config["Email:SmtpPort"]!), MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
        await smtp.SendAsync(msg);
        await smtp.DisconnectAsync(true);
    }
}
