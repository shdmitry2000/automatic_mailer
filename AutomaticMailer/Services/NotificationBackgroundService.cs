using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AutomaticMailer.Models;

namespace AutomaticMailer.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                await notificationService.ProcessPendingNotificationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing notifications");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.CheckIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Notification Background Service stopped");
    }
} 