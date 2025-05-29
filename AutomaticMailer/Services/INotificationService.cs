using AutomaticMailer.Models;

namespace AutomaticMailer.Services;

public interface INotificationService
{
    Task ProcessPendingNotificationsAsync();
    Task AddNotificationAsync(string toEmail, string subject, string body);
} 