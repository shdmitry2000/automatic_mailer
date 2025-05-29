using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AutomaticMailer.Data;
using AutomaticMailer.Models;
using AutomaticMailer.Services;

namespace AutomaticMailer;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Email Sender ===");
        
        // Load configuration
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var emailSettings = config.GetSection("EmailSettings").Get<EmailSettings>()!;
        var connectionString = config.GetConnectionString("DefaultConnection")!;

        // Setup database
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var context = new NotificationDbContext(options);
        
        // Create database if it doesn't exist
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("Database ready.");

        // Create services
        var emailService = new SimpleEmailService(emailSettings);
        var processingService = new EmailProcessingService(context, emailService);

        // Process emails
        var processedCount = await processingService.ProcessPendingEmailsAsync();
        Console.WriteLine($"Processed {processedCount} emails successfully.");

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
