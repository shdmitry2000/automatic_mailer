using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using AutomaticMailer.Models;

namespace AutomaticMailer.Services;

public class SimpleEmailService : ISimpleEmailService
{
    private readonly EmailSettings _settings;

    public SimpleEmailService(EmailSettings settings)
    {
        _settings = settings;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpServer, _settings.Port, _settings.EnableSsl);
        
        if (!string.IsNullOrEmpty(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
} 