using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AutomaticMailer.Data;
using AutomaticMailer.Models;
using AutomaticMailer.Services;

namespace AutomaticMailer.Tests;

public class EmailSendIntegrationTests : IDisposable
{
    private readonly NotificationDbContext _context;
    private readonly EmailSettings _emailSettings;
    private readonly string _dbPath;

    public EmailSendIntegrationTests()
    {
        // Create a unique SQLite database for each test
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _context = new NotificationDbContext(options);
        _context.Database.EnsureCreated();

        // Create test email settings
        _emailSettings = new EmailSettings
        {
            SmtpServer = "localhost",
            Port = 25,
            EnableSsl = false,
            Username = "test@localhost",
            Password = "testpassword",
            FromEmail = "test@localhost",
            FromName = "Test Mailer"
        };
    }

    [Fact]
    public async Task DatabaseOperations_CreateReadUpdateDelete_WorksCorrectly()
    {
        // Create
        var emailSend = new EmailSend
        {
            ToEmail = "integration@test.com",
            Subject = "Integration Test",
            Body = "Testing database operations",
            CreatedAt = DateTime.UtcNow,
            IsSent = false,
            SentTN = false
        };

        _context.EmailSends.Add(emailSend);
        await _context.SaveChangesAsync();

        // Read
        var retrievedEmail = await _context.EmailSends
            .FirstOrDefaultAsync(e => e.ToEmail == "integration@test.com");

        Assert.NotNull(retrievedEmail);
        Assert.Equal("Integration Test", retrievedEmail.Subject);
        Assert.False(retrievedEmail.IsSent);
        Assert.False(retrievedEmail.SentTN);

        // Update
        retrievedEmail.IsSent = true;
        retrievedEmail.SentTN = true;
        retrievedEmail.SentAt = DateTime.UtcNow;
        retrievedEmail.ErrorMessage = null;

        await _context.SaveChangesAsync();

        // Verify update
        var updatedEmail = await _context.EmailSends
            .FirstAsync(e => e.ToEmail == "integration@test.com");

        Assert.True(updatedEmail.IsSent);
        Assert.True(updatedEmail.SentTN);
        Assert.NotNull(updatedEmail.SentAt);

        // Delete
        _context.EmailSends.Remove(updatedEmail);
        await _context.SaveChangesAsync();

        // Verify deletion
        var deletedEmail = await _context.EmailSends
            .FirstOrDefaultAsync(e => e.ToEmail == "integration@test.com");

        Assert.Null(deletedEmail);
    }

    [Fact]
    public async Task EmailProcessingService_WithMockEmailService_ProcessesCorrectly()
    {
        // Arrange - Add test data
        var testEmails = new[]
        {
            new EmailSend
            {
                ToEmail = "user1@test.com",
                Subject = "Welcome Email",
                Body = "Welcome to our service!",
                IsSent = false,
                SentTN = false
            },
            new EmailSend
            {
                ToEmail = "user2@test.com",
                Subject = "Newsletter",
                Body = "Monthly newsletter content",
                IsSent = false,
                SentTN = false
            }
        };

        _context.EmailSends.AddRange(testEmails);
        await _context.SaveChangesAsync();

        // Create mock email service that always succeeds
        var mockEmailService = new MockEmailService(shouldSucceed: true);
        var processingService = new EmailProcessingService(_context, mockEmailService);

        // Act
        var result = await processingService.ProcessPendingEmailsAsync();

        // Assert
        Assert.Equal(2, result);
        Assert.Equal(2, mockEmailService.SentEmails.Count);

        // Verify sent emails
        Assert.Contains(mockEmailService.SentEmails, e => e.ToEmail == "user1@test.com");
        Assert.Contains(mockEmailService.SentEmails, e => e.ToEmail == "user2@test.com");

        // Verify database updates
        var updatedEmails = await _context.EmailSends.ToListAsync();
        Assert.All(updatedEmails, email =>
        {
            Assert.True(email.IsSent);
            Assert.True(email.SentTN);
            Assert.NotNull(email.SentAt);
            Assert.Null(email.ErrorMessage);
        });
    }

    [Fact]
    public async Task EmailProcessingService_WithFailingEmailService_HandlesErrorsCorrectly()
    {
        // Arrange
        var testEmail = new EmailSend
        {
            ToEmail = "fail@test.com",
            Subject = "This will fail",
            Body = "This email sending will fail",
            IsSent = false,
            SentTN = false
        };

        _context.EmailSends.Add(testEmail);
        await _context.SaveChangesAsync();

        // Create mock email service that always fails
        var mockEmailService = new MockEmailService(shouldSucceed: false);
        var processingService = new EmailProcessingService(_context, mockEmailService);

        // Act
        var result = await processingService.ProcessPendingEmailsAsync();

        // Assert
        Assert.Equal(0, result); // No emails processed successfully

        // Verify database was updated with error
        var updatedEmail = await _context.EmailSends.FirstAsync();
        Assert.False(updatedEmail.IsSent);
        Assert.False(updatedEmail.SentTN);
        Assert.Null(updatedEmail.SentAt);
        Assert.Equal("Mock email service failure", updatedEmail.ErrorMessage);
    }

    [Fact]
    public async Task EmailSendTable_SupportsMultipleRecords_WithCorrectIndexing()
    {
        // Arrange - Create multiple emails with different statuses
        var emails = new[]
        {
            new EmailSend { ToEmail = "sent1@test.com", Subject = "Sent 1", Body = "Body 1", IsSent = true, SentTN = true, SentAt = DateTime.UtcNow },
            new EmailSend { ToEmail = "sent2@test.com", Subject = "Sent 2", Body = "Body 2", IsSent = true, SentTN = true, SentAt = DateTime.UtcNow },
            new EmailSend { ToEmail = "pending1@test.com", Subject = "Pending 1", Body = "Body 3", IsSent = false, SentTN = false },
            new EmailSend { ToEmail = "pending2@test.com", Subject = "Pending 2", Body = "Body 4", IsSent = false, SentTN = false },
            new EmailSend { ToEmail = "error@test.com", Subject = "Error", Body = "Body 5", IsSent = false, SentTN = false, ErrorMessage = "Previous error" }
        };

        _context.EmailSends.AddRange(emails);
        await _context.SaveChangesAsync();

        // Act & Assert - Test different queries that would use indexes
        var sentEmails = await _context.EmailSends
            .Where(e => e.IsSent == true)
            .ToListAsync();
        Assert.Equal(2, sentEmails.Count);

        var pendingEmails = await _context.EmailSends
            .Where(e => !e.IsSent && !e.SentTN)
            .ToListAsync();
        Assert.Equal(3, pendingEmails.Count);

        var recentEmails = await _context.EmailSends
            .Where(e => e.CreatedAt > DateTime.UtcNow.AddHours(-1))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
        Assert.Equal(5, recentEmails.Count);
    }

    public void Dispose()
    {
        _context.Dispose();
        
        // Clean up the test database file
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}

// Mock email service for testing
public class MockEmailService : ISimpleEmailService
{
    private readonly bool _shouldSucceed;
    public List<EmailData> SentEmails { get; } = new();

    public MockEmailService(bool shouldSucceed = true)
    {
        _shouldSucceed = shouldSucceed;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body)
    {
        if (!_shouldSucceed)
        {
            throw new Exception("Mock email service failure");
        }

        SentEmails.Add(new EmailData(toEmail, subject, body));
        return Task.CompletedTask;
    }

    public record EmailData(string ToEmail, string Subject, string Body);
} 