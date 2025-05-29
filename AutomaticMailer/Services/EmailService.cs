using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using AutomaticMailer.Models;

namespace AutomaticMailer.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body,
                TextBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, 
                _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            
            if (!string.IsNullOrEmpty(_emailSettings.Username))
            {
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {ToEmail} with subject: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} with subject: {Subject}", toEmail, subject);
            return false;
        }
    }
} 