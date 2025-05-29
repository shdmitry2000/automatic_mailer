namespace AutomaticMailer.Models;

public class NotificationSettings
{
    public int CheckIntervalMinutes { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int RetryDelaySeconds { get; set; }
} 