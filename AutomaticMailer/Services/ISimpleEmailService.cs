namespace AutomaticMailer.Services;

public interface ISimpleEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
} 