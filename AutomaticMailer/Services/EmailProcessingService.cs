using Microsoft.EntityFrameworkCore;
using AutomaticMailer.Data;

namespace AutomaticMailer.Services;

public class EmailProcessingService
{
    private readonly NotificationDbContext _context;
    private readonly ISimpleEmailService _emailService;

    public EmailProcessingService(NotificationDbContext context, ISimpleEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<int> ProcessPendingEmailsAsync()
    {
        var pendingEmails = await _context.EmailSends
            .Where(e => !e.IsSent && !e.SentTN)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        var processedCount = 0;

        foreach (var email in pendingEmails)
        {
            try
            {
                await _emailService.SendEmailAsync(email.ToEmail, email.Subject, email.Body);
                
                email.IsSent = true;
                email.SentTN = true;
                email.SentAt = DateTime.UtcNow;
                email.ErrorMessage = null;
                
                processedCount++;
            }
            catch (Exception ex)
            {
                email.ErrorMessage = ex.Message;
            }
        }

        await _context.SaveChangesAsync();
        return processedCount;
    }
} 