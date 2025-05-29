using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AutomaticMailer.Data;
using AutomaticMailer.Models;

namespace AutomaticMailer.Services;

public class NotificationService : INotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationDbContext context,
        IEmailService emailService,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ProcessPendingNotificationsAsync()
    {
        var pendingNotifications = await _context.NotificationRecords
            .Where(n => !n.IsSent && 
                       (n.NextRetryAt == null || n.NextRetryAt <= DateTime.UtcNow) &&
                       n.RetryCount < _settings.MaxRetryAttempts)
            .OrderBy(n => n.CreatedAt)
            .Take(10) // Process 10 at a time
            .ToListAsync();

        _logger.LogInformation("Processing {Count} pending notifications", pendingNotifications.Count);

        foreach (var notification in pendingNotifications)
        {
            await ProcessNotificationAsync(notification);
        }
    }

    public async Task AddNotificationAsync(string toEmail, string subject, string body)
    {
        var notification = new NotificationRecord
        {
            ToEmail = toEmail,
            Subject = subject,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };

        _context.NotificationRecords.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added new notification for {ToEmail} with subject: {Subject}", toEmail, subject);
    }

    private async Task ProcessNotificationAsync(NotificationRecord notification)
    {
        try
        {
            _logger.LogInformation("Processing notification {Id} for {ToEmail}", notification.Id, notification.ToEmail);

            var success = await _emailService.SendEmailAsync(notification.ToEmail, notification.Subject, notification.Body);

            if (success)
            {
                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;
                notification.ErrorMessage = null;
                _logger.LogInformation("Successfully sent notification {Id}", notification.Id);
            }
            else
            {
                notification.RetryCount++;
                notification.ErrorMessage = "Failed to send email";
                
                if (notification.RetryCount < _settings.MaxRetryAttempts)
                {
                    notification.NextRetryAt = DateTime.UtcNow.AddSeconds(_settings.RetryDelaySeconds * notification.RetryCount);
                    _logger.LogWarning("Failed to send notification {Id}. Retry {RetryCount}/{MaxRetries} scheduled for {NextRetry}", 
                        notification.Id, notification.RetryCount, _settings.MaxRetryAttempts, notification.NextRetryAt);
                }
                else
                {
                    _logger.LogError("Failed to send notification {Id} after {MaxRetries} attempts", 
                        notification.Id, _settings.MaxRetryAttempts);
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification {Id}", notification.Id);
            
            notification.RetryCount++;
            notification.ErrorMessage = ex.Message;
            
            if (notification.RetryCount < _settings.MaxRetryAttempts)
            {
                notification.NextRetryAt = DateTime.UtcNow.AddSeconds(_settings.RetryDelaySeconds * notification.RetryCount);
            }
            
            await _context.SaveChangesAsync();
        }
    }
} 