using Microsoft.EntityFrameworkCore;
using Moq;
using AutomaticMailer.Data;
using AutomaticMailer.Models;
using AutomaticMailer.Services;

namespace AutomaticMailer.Tests;

public class EmailProcessingServiceTests : IDisposable
{
    private readonly NotificationDbContext _context;
    private readonly Mock<ISimpleEmailService> _mockEmailService;
    private readonly EmailProcessingService _service;

    public EmailProcessingServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new NotificationDbContext(options);
        _context.Database.EnsureCreated();

        // Setup mock email service
        _mockEmailService = new Mock<ISimpleEmailService>();
        
        // Create service under test
        _service = new EmailProcessingService(_context, _mockEmailService.Object);
    }

    [Fact]
    public async Task ProcessPendingEmailsAsync_WithNoPendingEmails_ReturnsZero()
    {
        // Act
        var result = await _service.ProcessPendingEmailsAsync();

        // Assert
        Assert.Equal(0, result);
        _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPendingEmailsAsync_WithPendingEmails_SendsEmailsAndUpdatesDatabase()
    {
        // Arrange
        var testEmails = new[]
        {
            new EmailSend
            {
                ToEmail = "test1@example.com",
                Subject = "Test Subject 1",
                Body = "Test Body 1",
                IsSent = false,
                SentTN = false
            },
            new EmailSend
            {
                ToEmail = "test2@example.com",
                Subject = "Test Subject 2",
                Body = "Test Body 2",
                IsSent = false,
                SentTN = false
            }
        };

        _context.EmailSends.AddRange(testEmails);
        await _context.SaveChangesAsync();

        _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessPendingEmailsAsync();

        // Assert
        Assert.Equal(2, result);
        
        // Verify email service was called for each email
        _mockEmailService.Verify(x => x.SendEmailAsync("test1@example.com", "Test Subject 1", "Test Body 1"), Times.Once);
        _mockEmailService.Verify(x => x.SendEmailAsync("test2@example.com", "Test Subject 2", "Test Body 2"), Times.Once);

        // Verify database was updated
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
    public async Task ProcessPendingEmailsAsync_WithEmailServiceError_UpdatesErrorMessage()
    {
        // Arrange
        var testEmail = new EmailSend
        {
            ToEmail = "error@example.com",
            Subject = "Error Test",
            Body = "This will fail",
            IsSent = false,
            SentTN = false
        };

        _context.EmailSends.Add(testEmail);
        await _context.SaveChangesAsync();

        _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ThrowsAsync(new Exception("SMTP connection failed"));

        // Act
        var result = await _service.ProcessPendingEmailsAsync();

        // Assert
        Assert.Equal(0, result); // No emails processed successfully

        // Verify database was updated with error
        var updatedEmail = await _context.EmailSends.FirstAsync();
        Assert.False(updatedEmail.IsSent);
        Assert.False(updatedEmail.SentTN);
        Assert.Null(updatedEmail.SentAt);
        Assert.Equal("SMTP connection failed", updatedEmail.ErrorMessage);
    }

    [Fact]
    public async Task ProcessPendingEmailsAsync_IgnoresAlreadySentEmails()
    {
        // Arrange
        var sentEmail = new EmailSend
        {
            ToEmail = "sent@example.com",
            Subject = "Already Sent",
            Body = "This was already sent",
            IsSent = true,
            SentTN = true,
            SentAt = DateTime.UtcNow
        };

        var pendingEmail = new EmailSend
        {
            ToEmail = "pending@example.com",
            Subject = "Still Pending",
            Body = "This needs to be sent",
            IsSent = false,
            SentTN = false
        };

        _context.EmailSends.AddRange(sentEmail, pendingEmail);
        await _context.SaveChangesAsync();

        _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessPendingEmailsAsync();

        // Assert
        Assert.Equal(1, result); // Only one email processed

        // Verify only pending email was processed
        _mockEmailService.Verify(x => x.SendEmailAsync("pending@example.com", "Still Pending", "This needs to be sent"), Times.Once);
        _mockEmailService.Verify(x => x.SendEmailAsync("sent@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPendingEmailsAsync_ProcessesEmailsInCreatedAtOrder()
    {
        // Arrange
        var emails = new[]
        {
            new EmailSend
            {
                ToEmail = "third@example.com",
                Subject = "Third",
                Body = "Created third",
                CreatedAt = DateTime.UtcNow.AddMinutes(2),
                IsSent = false,
                SentTN = false
            },
            new EmailSend
            {
                ToEmail = "first@example.com",
                Subject = "First",
                Body = "Created first",
                CreatedAt = DateTime.UtcNow,
                IsSent = false,
                SentTN = false
            },
            new EmailSend
            {
                ToEmail = "second@example.com",
                Subject = "Second",
                Body = "Created second",
                CreatedAt = DateTime.UtcNow.AddMinutes(1),
                IsSent = false,
                SentTN = false
            }
        };

        _context.EmailSends.AddRange(emails);
        await _context.SaveChangesAsync();

        var callOrder = new List<string>();
        _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .Callback<string, string, string>((email, subject, body) => callOrder.Add(email))
                        .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessPendingEmailsAsync();

        // Assert
        Assert.Equal(new[] { "first@example.com", "second@example.com", "third@example.com" }, callOrder);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
} 