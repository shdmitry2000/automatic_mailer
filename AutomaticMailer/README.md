# Automatic Email Sender

A robust .NET 8.0 console application that processes email notifications from a database queue with comprehensive testing support and configurable SMTP settings.

## 🚀 Features

- **Database-driven email queue**: Reads from `email_send` table
- **Multiple database support**: SQL Server (production) and SQLite (development/testing)
- **SMTP integration**: Send emails via any SMTP provider (Gmail, Outlook, custom)
- **Retry mechanism**: Automatic error handling and status tracking
- **Comprehensive testing**: Unit tests with mocked SMTP + integration tests
- **Configuration-based**: All settings in JSON configuration files
- **Status tracking**: `sent_t_n` field for precise email delivery tracking

## 📋 Prerequisites

- .NET 8.0 SDK or later
- SQL Server (for production) or SQLite (for development/testing)
- SMTP server access (Gmail, Outlook, or custom SMTP server)

## 🛠️ Installation

### 1. Clone or Download
```bash
git clone <repository-url>
cd AutomaticMailer
```

### 2. Restore Dependencies
```bash
dotnet restore
```

### 3. Build Application
```bash
dotnet build
```

## ⚙️ Configuration

### Database Connection

#### For SQL Server (Production):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EmailDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

#### For SQLite (Development/Testing):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=emails.db"
  }
}
```

### Email Settings

#### Gmail Configuration:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Your Application Name"
  }
}
```

> **Important**: For Gmail, you must use an App Password, not your regular password. Enable 2FA and generate an App Password in your Google Account settings.

#### Outlook Configuration:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp-mail.outlook.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@outlook.com",
    "Password": "your-password",
    "FromEmail": "your-email@outlook.com",
    "FromName": "Your Application Name"
  }
}
```

## 🏃‍♂️ Usage

### Basic Usage
1. **Configure** your `appsettings.json` with database and email settings
2. **Add email records** to the `email_send` table
3. **Run the application**:
   ```bash
   dotnet run
   ```

### Database Schema

The application automatically creates the `email_send` table with this structure:

| Column        | Type         | Description                        |
|---------------|-------------|------------------------------------|
| `Id`          | int          | Primary key (auto-increment)      |
| `ToEmail`     | varchar(255) | Recipient email address            |
| `Subject`     | varchar(500) | Email subject line                 |
| `Body`        | text         | Email content (plain text)         |
| `CreatedAt`   | datetime2    | When the record was created        |
| `SentAt`      | datetime2    | When the email was sent (nullable) |
| `IsSent`      | bit          | Whether email was sent successfully |
| `sent_t_n`    | bit          | Custom tracking flag               |
| `ErrorMessage`| varchar(1000)| Error details if sending failed    |

### Adding Emails to Queue

#### Method 1: Direct SQL Insert
```sql
INSERT INTO email_send (ToEmail, Subject, Body, CreatedAt, IsSent, sent_t_n)
VALUES 
  ('user@example.com', 'Welcome!', 'Welcome to our service!', GETUTCDATE(), 0, 0),
  ('admin@company.com', 'Alert', 'System maintenance tonight', GETUTCDATE(), 0, 0);
```

#### Method 2: Using Entity Framework (in code)
```csharp
var email = new EmailSend
{
    ToEmail = "recipient@example.com",
    Subject = "Your Subject",
    Body = "Your email content here",
    CreatedAt = DateTime.UtcNow,
    IsSent = false,
    SentTN = false
};

context.EmailSends.Add(email);
await context.SaveChangesAsync();
```

## 🧪 Testing

### Run All Tests
```bash
cd AutomaticMailer.Tests
dotnet test
```

### Test Coverage
- **Unit Tests**: Mock SMTP service, test business logic
- **Integration Tests**: Real database operations with SQLite
- **Error Handling**: Test SMTP failures and retry logic
- **Data Validation**: Test email processing order and status updates

### Using Test Data Seeder
```csharp
// Add test emails to database
await TestDataSeeder.SeedTestEmailsAsync(context);

// Display current email status
await TestDataSeeder.DisplayEmailStatusAsync(context);
```

## 📊 Monitoring

### Console Output
The application provides real-time feedback:
```
=== Email Sender ===
Database ready.
Processed 3 emails successfully.
Press any key to exit...
```

### Check Email Status
Query the database to monitor email delivery:
```sql
SELECT 
    Id, 
    ToEmail, 
    Subject, 
    IsSent, 
    sent_t_n, 
    SentAt, 
    ErrorMessage,
    CreatedAt
FROM email_send 
ORDER BY CreatedAt DESC;
```

## 🎯 Examples

### Example 1: Newsletter Campaign
```sql
INSERT INTO email_send (ToEmail, Subject, Body, CreatedAt, IsSent, sent_t_n)
VALUES 
  ('subscriber1@example.com', 'Monthly Newsletter - March 2024', 'Check out our latest updates and features...', GETUTCDATE(), 0, 0),
  ('subscriber2@example.com', 'Monthly Newsletter - March 2024', 'Check out our latest updates and features...', GETUTCDATE(), 0, 0);
```

### Example 2: System Notifications
```sql
INSERT INTO email_send (ToEmail, Subject, Body, CreatedAt, IsSent, sent_t_n)
VALUES 
  ('admin@company.com', '[ALERT] High CPU Usage', 'Server CPU usage is above 90% for the last 10 minutes.', GETUTCDATE(), 0, 0),
  ('devops@company.com', '[INFO] Deployment Complete', 'Application v2.1.0 deployed successfully to production.', GETUTCDATE(), 0, 0);
```

### Example 3: User Welcome Series
```sql
INSERT INTO email_send (ToEmail, Subject, Body, CreatedAt, IsSent, sent_t_n)
VALUES 
  ('newuser@example.com', 'Welcome to Our Platform!', 'Thank you for joining us. Here''s how to get started...', GETUTCDATE(), 0, 0),
  ('newuser@example.com', 'Complete Your Profile', 'Add more details to your profile to get personalized recommendations.', DATEADD(HOUR, 24, GETUTCDATE()), 0, 0);
```

## 🔧 Advanced Configuration

### Environment-Specific Settings

#### Development (`appsettings.Development.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=dev_emails.db"
  },
  "EmailSettings": {
    "SmtpServer": "localhost",
    "Port": 1025,
    "EnableSsl": false,
    "Username": "",
    "Password": "",
    "FromEmail": "dev@localhost",
    "FromName": "Development Mailer"
  }
}
```

#### Testing (`appsettings.Test.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=test_emails.db"
  },
  "EmailSettings": {
    "SmtpServer": "localhost",
    "Port": 25,
    "EnableSsl": false,
    "Username": "test@localhost",
    "Password": "testpassword",
    "FromEmail": "test@localhost",
    "FromName": "Test Mailer"
  }
}
```

### Custom SMTP Servers
```json
{
  "EmailSettings": {
    "SmtpServer": "mail.yourcompany.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "noreply@yourcompany.com",
    "Password": "your-smtp-password",
    "FromEmail": "noreply@yourcompany.com",
    "FromName": "Your Company Name"
  }
}
```

## 🐛 Troubleshooting

### Common Issues

#### 1. Database Connection Errors
```
Error: Cannot connect to database
```
**Solutions:**
- Verify SQL Server is running
- Check connection string format
- Ensure database exists or app has permission to create it
- For SQLite: Check file path permissions

#### 2. SMTP Authentication Errors
```
Error: SMTP authentication failed
```
**Solutions:**
- For Gmail: Use App Password instead of regular password
- Verify SMTP server settings (host, port, SSL)
- Check firewall/network restrictions
- Test SMTP credentials with email client first

#### 3. SSL/TLS Errors
```
Error: SSL handshake failed
```
**Solutions:**
- Try different ports (25, 465, 587)
- Toggle `EnableSsl` setting
- Check if SMTP server requires STARTTLS

#### 4. No Emails Being Processed
**Check:**
- Are there records in `email_send` table?
- Are emails marked as `IsSent = 0` and `sent_t_n = 0`?
- Check `ErrorMessage` field for previous failures
- Verify application has database permissions

### Logging and Debugging

The application outputs detailed information:
- Database connection status
- Number of emails found and processed
- Success/failure status for each email
- Error messages for failed sends

## 📚 API Reference

### EmailProcessingService

#### `ProcessPendingEmailsAsync()`
Processes all pending emails in the queue.

**Returns:** `Task<int>` - Number of emails successfully sent

**Behavior:**
- Queries emails where `IsSent = false` AND `sent_t_n = false`
- Processes emails in `CreatedAt` order (oldest first)
- Updates database with send status
- Sets `sent_t_n = true` only on successful send

### ISimpleEmailService

#### `SendEmailAsync(string toEmail, string subject, string body)`
Sends a single email via SMTP.

**Parameters:**
- `toEmail`: Recipient email address
- `subject`: Email subject line
- `body`: Email content (plain text)

**Returns:** `Task` - Completes when email is sent

**Throws:** `Exception` - On SMTP errors

## 🔒 Security Considerations

1. **Credential Storage**: Store SMTP passwords securely (environment variables, Azure Key Vault, etc.)
2. **SQL Injection**: The application uses Entity Framework parameterized queries
3. **Email Validation**: Validate email addresses before inserting into queue
4. **Rate Limiting**: Consider SMTP provider rate limits for high-volume sending
5. **Error Logging**: Avoid logging sensitive information in error messages

## 🚀 Production Deployment

### Recommended Setup
1. **Database**: Use SQL Server with proper backup strategy
2. **Configuration**: Use environment-specific config files
3. **Monitoring**: Set up database monitoring for queue size
4. **Scheduling**: Run as Windows Service or scheduled task
5. **Logging**: Implement structured logging (Serilog, NLog)

### Performance Considerations
- **Batch Size**: Process emails in batches for high volume
- **Connection Pooling**: Configure EF connection pooling
- **SMTP Connections**: Reuse SMTP connections when possible
- **Database Indexing**: Ensure proper indexes on query columns

## 📄 License

[Your License Here]

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## 📞 Support

For issues and questions:
- Check the troubleshooting section
- Review test examples
- Create an issue in the repository

## 📚 Documentation

### 📖 User Guides
- **[README.md](README.md)** - Complete installation and usage guide (this file)
- **[API Reference](API_REFERENCE.md)** - Detailed API documentation for all classes and methods
- **[Architecture Overview](ARCHITECTURE.md)** - System design, patterns, and technical architecture
- **[Developer Guide](DEVELOPER_GUIDE.md)** - Development setup, testing, and contribution guidelines
- **[Changelog](CHANGELOG.md)** - Release history and version changes

### 🔧 Configuration Examples
- **Production Setup**: SQL Server with Gmail SMTP
- **Development Setup**: SQLite with local SMTP server
- **Testing Setup**: In-memory database with mocked services

### 🧪 Testing Resources
- **Unit Tests**: 5 comprehensive tests with mocked dependencies
- **Integration Tests**: 4 end-to-end tests with real database operations
- **Test Data Utilities**: Automated test data creation and management

---

## 🎯 Examples 