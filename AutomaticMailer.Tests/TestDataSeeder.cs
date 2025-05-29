using Microsoft.EntityFrameworkCore;
using AutomaticMailer.Data;
using AutomaticMailer.Models;

namespace AutomaticMailer.Tests;

public static class TestDataSeeder
{
    public static async Task SeedTestEmailsAsync(NotificationDbContext context)
    {
        // Clear existing data
        context.EmailSends.RemoveRange(context.EmailSends);
        await context.SaveChangesAsync();

        // Add test emails
        var testEmails = new[]
        {
            new EmailSend
            {
                ToEmail = "user1@test.com",
                Subject = "Welcome to our service!",
                Body = "Hello! Thank you for joining our service. We're excited to have you on board!",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                IsSent = false,
                SentTN = false
            },
            new EmailSend
            {
                ToEmail = "user2@test.com",
                Subject = "Monthly Newsletter",
                Body = "Here's what's new this month: New features, updates, and exciting announcements!",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                IsSent = false,
                SentTN = false
            },
            new EmailSend
            {
                ToEmail = "admin@test.com",
                Subject = "System Maintenance Notice",
                Body = "Scheduled maintenance will occur tonight from 2 AM to 4 AM EST. Please plan accordingly.",
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                IsSent = false,
                SentTN = false
            },
            new EmailSend
            {
                ToEmail = "already.sent@test.com",
                Subject = "Previous Email",
                Body = "This email was already sent successfully.",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                IsSent = true,
                SentTN = true,
                SentAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        context.EmailSends.AddRange(testEmails);
        await context.SaveChangesAsync();

        Console.WriteLine($"Seeded {testEmails.Length} test emails to the database.");
        Console.WriteLine($"Pending emails: {testEmails.Count(e => !e.IsSent)}");
        Console.WriteLine($"Already sent: {testEmails.Count(e => e.IsSent)}");
    }

    public static async Task DisplayEmailStatusAsync(NotificationDbContext context)
    {
        var allEmails = await context.EmailSends.OrderBy(e => e.CreatedAt).ToListAsync();
        
        Console.WriteLine("\n=== Email Status Report ===");
        foreach (var email in allEmails)
        {
            var status = email.IsSent ? "✓ SENT" : "⏳ PENDING";
            var sentInfo = email.SentAt.HasValue ? $" (sent: {email.SentAt:HH:mm:ss})" : "";
            var errorInfo = !string.IsNullOrEmpty(email.ErrorMessage) ? $" [ERROR: {email.ErrorMessage}]" : "";
            
            Console.WriteLine($"{status} | {email.ToEmail} | {email.Subject}{sentInfo}{errorInfo}");
        }
        Console.WriteLine("========================\n");
    }
} 