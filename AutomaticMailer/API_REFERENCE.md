# API Reference

## Core Services

### EmailProcessingService

Primary service for processing email queue.

#### Constructor
```csharp
public EmailProcessingService(NotificationDbContext context, ISimpleEmailService emailService)
```

**Parameters:**
- `context`: Database context for email operations
- `emailService`: Service for sending emails via SMTP

#### Methods

##### ProcessPendingEmailsAsync()
```csharp
public async Task<int> ProcessPendingEmailsAsync()
```

Processes all pending emails in the queue.

**Returns:** `Task<int>` - Number of emails successfully sent

**Behavior:**
- Queries emails where `IsSent = false` AND `sent_t_n = false`
- Processes emails in `CreatedAt` order (oldest first)
- Updates database with send status
- Sets `sent_t_n = true` only on successful send

**Example:**
```csharp
var service = new EmailProcessingService(context, emailService);
var processedCount = await service.ProcessPendingEmailsAsync();
Console.WriteLine($"Processed {processedCount} emails");
```

---

### ISimpleEmailService

Interface for email sending implementations.

#### Methods

##### SendEmailAsync()
```csharp
Task SendEmailAsync(string toEmail, string subject, string body)
```

Sends a single email via SMTP.

**Parameters:**
- `toEmail`: Recipient email address
- `subject`: Email subject line
- `body`: Email content (plain text)

**Returns:** `Task` - Completes when email is sent

**Throws:** `Exception` - On SMTP errors

**Example:**
```csharp
await emailService.SendEmailAsync("user@example.com", "Welcome!", "Welcome to our service!");
```

---

### SimpleEmailService

Default SMTP implementation of `ISimpleEmailService`.

#### Constructor
```csharp
public SimpleEmailService(EmailSettings settings)
```

**Parameters:**
- `settings`: SMTP configuration settings

#### Configuration
Uses `EmailSettings` configuration object:
```csharp
public class EmailSettings
{
    public string SmtpServer { get; set; }
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
}
```

---

## Data Models

### EmailSend

Main entity for email queue.

```csharp
[Table("email_send")]
public class EmailSend
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string ToEmail { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; }
    
    [Required]
    public string Body { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? SentAt { get; set; }
    
    public bool IsSent { get; set; } = false;
    
    [Column("sent_t_n")]
    public bool SentTN { get; set; } = false;
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
}
```

#### Properties

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `Id` | `int` | Primary key (auto-increment) | Auto |
| `ToEmail` | `string` | Recipient email address (required, max 255) | - |
| `Subject` | `string` | Email subject (required, max 500) | - |
| `Body` | `string` | Email content (required) | - |
| `CreatedAt` | `DateTime` | When record was created | `DateTime.UtcNow` |
| `SentAt` | `DateTime?` | When email was sent (nullable) | `null` |
| `IsSent` | `bool` | Whether email was sent successfully | `false` |
| `SentTN` | `bool` | Custom tracking flag | `false` |
| `ErrorMessage` | `string?` | Error details if sending failed | `null` |

#### Database Indexes
- Index on `IsSent` for query performance
- Index on `CreatedAt` for ordering

---

### EmailSettings

Configuration model for SMTP settings.

```csharp
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
```

#### Configuration Example
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Your Application"
  }
}
```

---

## Data Access

### NotificationDbContext

Entity Framework DbContext for database operations.

#### Constructor
```csharp
public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
```

#### DbSets
```csharp
public DbSet<EmailSend> EmailSends { get; set; }
public DbSet<NotificationRecord> NotificationRecords { get; set; }  // Legacy
```

#### Database Providers
- **SQL Server**: `UseSqlServer(connectionString)`
- **SQLite**: `UseSqlite(connectionString)`
- **In-Memory**: `UseInMemoryDatabase(databaseName)` (testing only)

#### Example Usage
```csharp
var options = new DbContextOptionsBuilder<NotificationDbContext>()
    .UseSqlServer(connectionString)
    .Options;

using var context = new NotificationDbContext(options);
await context.Database.EnsureCreatedAsync();

var pendingEmails = await context.EmailSends
    .Where(e => !e.IsSent && !e.SentTN)
    .OrderBy(e => e.CreatedAt)
    .ToListAsync();
```

---

## Testing Utilities

### TestDataSeeder

Utility class for creating test data.

#### Methods

##### SeedTestEmailsAsync()
```csharp
public static async Task SeedTestEmailsAsync(NotificationDbContext context)
```

Creates sample email records for testing.

**Behavior:**
- Clears existing email records
- Creates 4 test emails (3 pending, 1 sent)
- Saves to database

##### DisplayEmailStatusAsync()
```csharp
public static async Task DisplayEmailStatusAsync(NotificationDbContext context)
```

Displays current email status to console.

**Output Example:**
```
=== Email Status Report ===
✓ SENT | sent@example.com | Welcome Email (sent: 14:30:25)
⏳ PENDING | user1@test.com | Newsletter
⏳ PENDING | user2@test.com | Notification [ERROR: SMTP failed]
========================
```

#### Usage Example
```csharp
// In test setup
await TestDataSeeder.SeedTestEmailsAsync(context);

// Verify current status
await TestDataSeeder.DisplayEmailStatusAsync(context);
```

---

### MockEmailService

Test implementation of `ISimpleEmailService` for unit testing.

#### Constructor
```csharp
public MockEmailService(bool shouldSucceed = true)
```

**Parameters:**
- `shouldSucceed`: Whether email sending should succeed or throw exception

#### Properties
```csharp
public List<EmailData> SentEmails { get; }  // List of sent emails for verification
```

#### Methods
```csharp
public Task SendEmailAsync(string toEmail, string subject, string body)
```

Mock implementation that:
- Records sent emails if `shouldSucceed = true`
- Throws exception if `shouldSucceed = false`

#### Usage Example
```csharp
var mockEmailService = new MockEmailService(shouldSucceed: true);
var service = new EmailProcessingService(context, mockEmailService);

await service.ProcessPendingEmailsAsync();

// Verify emails were sent
Assert.Equal(2, mockEmailService.SentEmails.Count);
Assert.Contains(mockEmailService.SentEmails, e => e.ToEmail == "test@example.com");
```

---

## Configuration API

### Connection Strings

#### SQL Server
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EmailDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

#### SQLite
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=emails.db"
  }
}
```

### Environment-Specific Configuration

Files are loaded in order (later files override earlier ones):
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables
4. Command line arguments

#### Environment Variables
```bash
# Override email settings
EmailSettings__Username="new-email@company.com"
EmailSettings__Password="new-password"

# Override connection string
ConnectionStrings__DefaultConnection="Server=prod;Database=EmailDB;..."
```

---

## Error Handling

### Common Exceptions

#### SMTP Errors
```csharp
try
{
    await emailService.SendEmailAsync(email, subject, body);
}
catch (SmtpException ex)
{
    // Handle SMTP-specific errors
    Console.WriteLine($"SMTP Error: {ex.Message}");
}
catch (AuthenticationException ex)
{
    // Handle authentication errors
    Console.WriteLine($"Auth Error: {ex.Message}");
}
```

#### Database Errors
```csharp
try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
    // Handle database update errors
    Console.WriteLine($"Database Error: {ex.Message}");
}
```

### Error Storage
Failed email attempts store error information in the database:
```csharp
email.ErrorMessage = ex.Message;  // Store error for troubleshooting
email.IsSent = false;             // Keep as unsent for retry
email.SentTN = false;             // Don't mark as processed
```

---

## Performance Considerations

### Batch Processing
The service processes all pending emails in a single batch:
```csharp
var pendingEmails = await context.EmailSends
    .Where(e => !e.IsSent && !e.SentTN)
    .ToListAsync();  // Single database query

// Process all emails
foreach (var email in pendingEmails)
{
    // Send email...
}

await context.SaveChangesAsync();  // Single database update
```

### Database Indexes
Queries are optimized with indexes:
- `IsSent` index for filtering pending emails
- `CreatedAt` index for ordering by creation time

### Memory Management
- DbContext is disposed after processing
- SMTP client is disposed after each email
- Minimal object allocation during processing

---

## Extension Points

### Custom Email Services
Implement `ISimpleEmailService` for different providers:
```csharp
public class CustomEmailService : ISimpleEmailService
{
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        // Custom implementation (SendGrid, Amazon SES, etc.)
    }
}
```

### Custom Data Models
Extend `EmailSend` for additional fields:
```csharp
public class EmailSend
{
    // Existing properties...
    
    public string? Priority { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? Category { get; set; }
}
```

### Custom Processing Logic
Override `EmailProcessingService` for custom behavior:
```csharp
public class CustomEmailProcessingService : EmailProcessingService
{
    public override async Task<int> ProcessPendingEmailsAsync()
    {
        // Custom processing logic
        return await base.ProcessPendingEmailsAsync();
    }
}
``` 