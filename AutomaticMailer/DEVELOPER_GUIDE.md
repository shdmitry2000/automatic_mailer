# Developer Guide

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 / VS Code / JetBrains Rider
- SQL Server (optional, SQLite used for development)
- Git

### Initial Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd AutomaticMailer
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   cd AutomaticMailer.Tests
   dotnet test
   ```

## 🔧 Development Environment

### Recommended Configuration

#### For Local Development (`appsettings.Development.json`)
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

#### SMTP Testing Tools
- **MailHog**: Local SMTP server for testing
  ```bash
  # Install via Go
  go install github.com/mailhog/MailHog@latest
  
  # Run on port 1025
  MailHog
  ```
- **Papercut**: Windows SMTP server
- **smtp4dev**: Cross-platform SMTP server

### Database Setup

#### SQLite (Recommended for Development)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=emails.db"
  }
}
```

#### SQL Server (Optional)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EmailSender_Dev;Trusted_Connection=true;"
  }
}
```

## 🧪 Testing Guidelines

### Running Tests

#### All Tests
```bash
cd AutomaticMailer.Tests
dotnet test
```

#### Specific Test Class
```bash
dotnet test --filter "EmailProcessingServiceTests"
```

#### With Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Writing Tests

#### Unit Test Example
```csharp
[Fact]
public async Task ProcessPendingEmailsAsync_WithValidEmails_SendsSuccessfully()
{
    // Arrange
    var mockEmailService = new Mock<ISimpleEmailService>();
    mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                   .Returns(Task.CompletedTask);
    
    var service = new EmailProcessingService(_context, mockEmailService.Object);
    
    // Add test data
    _context.EmailSends.Add(new EmailSend
    {
        ToEmail = "test@example.com",
        Subject = "Test",
        Body = "Test body",
        IsSent = false,
        SentTN = false
    });
    await _context.SaveChangesAsync();
    
    // Act
    var result = await service.ProcessPendingEmailsAsync();
    
    // Assert
    Assert.Equal(1, result);
    mockEmailService.Verify(x => x.SendEmailAsync("test@example.com", "Test", "Test body"), Times.Once);
}
```

#### Integration Test Example
```csharp
[Fact]
public async Task DatabaseOperations_FullWorkflow_WorksCorrectly()
{
    // Arrange
    var email = new EmailSend
    {
        ToEmail = "integration@test.com",
        Subject = "Integration Test",
        Body = "Testing complete workflow"
    };
    
    // Act - Create
    _context.EmailSends.Add(email);
    await _context.SaveChangesAsync();
    
    // Act - Process
    var mockEmailService = new MockEmailService(shouldSucceed: true);
    var service = new EmailProcessingService(_context, mockEmailService);
    var result = await service.ProcessPendingEmailsAsync();
    
    // Assert
    Assert.Equal(1, result);
    var updatedEmail = await _context.EmailSends.FirstAsync();
    Assert.True(updatedEmail.IsSent);
    Assert.True(updatedEmail.SentTN);
}
```

### Test Data Management

#### Using TestDataSeeder
```csharp
// In test setup
await TestDataSeeder.SeedTestEmailsAsync(context);

// Verify data
await TestDataSeeder.DisplayEmailStatusAsync(context);
```

#### Custom Test Data
```csharp
private async Task<EmailSend> CreateTestEmailAsync(string toEmail = "test@example.com")
{
    var email = new EmailSend
    {
        ToEmail = toEmail,
        Subject = "Test Subject",
        Body = "Test Body",
        CreatedAt = DateTime.UtcNow,
        IsSent = false,
        SentTN = false
    };
    
    _context.EmailSends.Add(email);
    await _context.SaveChangesAsync();
    return email;
}
```

## 🔄 Development Workflow

### Feature Development

1. **Create feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Implement feature**
   - Write failing tests first (TDD)
   - Implement feature
   - Ensure all tests pass

3. **Test thoroughly**
   ```bash
   dotnet test
   dotnet run  # Manual testing
   ```

4. **Commit and push**
   ```bash
   git add .
   git commit -m "feat: add your feature description"
   git push origin feature/your-feature-name
   ```

### Code Review Checklist

- [ ] All tests pass
- [ ] New features have tests
- [ ] Code follows existing patterns
- [ ] Configuration is properly handled
- [ ] Error handling is appropriate
- [ ] Documentation is updated

## 🏗️ Adding New Features

### Adding a New Email Service

1. **Create interface implementation**
   ```csharp
   public class SendGridEmailService : ISimpleEmailService
   {
       private readonly SendGridSettings _settings;
       
       public SendGridEmailService(SendGridSettings settings)
       {
           _settings = settings;
       }
       
       public async Task SendEmailAsync(string toEmail, string subject, string body)
       {
           // Implementation
       }
   }
   ```

2. **Add configuration**
   ```json
   {
     "SendGridSettings": {
       "ApiKey": "your-api-key",
       "FromEmail": "noreply@yourcompany.com"
     }
   }
   ```

3. **Register service**
   ```csharp
   // In Program.cs
   var sendGridSettings = config.GetSection("SendGridSettings").Get<SendGridSettings>();
   var emailService = new SendGridEmailService(sendGridSettings);
   ```

4. **Add tests**
   ```csharp
   public class SendGridEmailServiceTests
   {
       [Fact]
       public async Task SendEmailAsync_WithValidData_SendsSuccessfully()
       {
           // Test implementation
       }
   }
   ```

### Adding Database Fields

1. **Update model**
   ```csharp
   public class EmailSend
   {
       // Existing properties...
       
       [MaxLength(100)]
       public string? Priority { get; set; }
       
       public DateTime? ScheduledAt { get; set; }
   }
   ```

2. **Update DbContext if needed**
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       base.OnModelCreating(modelBuilder);
       
       modelBuilder.Entity<EmailSend>(entity =>
       {
           entity.HasIndex(e => e.Priority);
           entity.HasIndex(e => e.ScheduledAt);
       });
   }
   ```

3. **Create migration**
   ```bash
   dotnet ef migrations add AddPriorityAndScheduling
   dotnet ef database update
   ```

## 🐛 Debugging

### Common Debug Scenarios

#### Email Not Sending
1. Check SMTP configuration
2. Verify database records exist
3. Check error messages in database
4. Test SMTP connection manually

#### Database Connection Issues
1. Verify connection string
2. Check database server status
3. Validate permissions
4. Test with SQL Server Management Studio

#### Test Failures
1. Check test isolation (each test should be independent)
2. Verify mock setups
3. Check test data initialization
4. Ensure proper cleanup

### Debugging Tools

#### Enable EF Logging
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.LogTo(Console.WriteLine);
}
```

#### SMTP Debugging
```csharp
public async Task SendEmailAsync(string toEmail, string subject, string body)
{
    try
    {
        Console.WriteLine($"Connecting to {_settings.SmtpServer}:{_settings.Port}");
        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpServer, _settings.Port, _settings.EnableSsl);
        Console.WriteLine("Connected successfully");
        
        // Rest of implementation...
    }
    catch (Exception ex)
    {
        Console.WriteLine($"SMTP Error: {ex.Message}");
        throw;
    }
}
```

## 📊 Performance Testing

### Load Testing Setup

```csharp
[Fact]
public async Task ProcessPendingEmailsAsync_WithLargeVolume_PerformsWell()
{
    // Arrange - Create 1000 test emails
    var emails = Enumerable.Range(1, 1000)
        .Select(i => new EmailSend
        {
            ToEmail = $"user{i}@test.com",
            Subject = $"Test Email {i}",
            Body = "Test body content",
            IsSent = false,
            SentTN = false
        });
    
    _context.EmailSends.AddRange(emails);
    await _context.SaveChangesAsync();
    
    var mockEmailService = new Mock<ISimpleEmailService>();
    mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                   .Returns(Task.CompletedTask);
    
    var service = new EmailProcessingService(_context, mockEmailService.Object);
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    var result = await service.ProcessPendingEmailsAsync();
    stopwatch.Stop();
    
    // Assert
    Assert.Equal(1000, result);
    Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Processing should complete within 5 seconds");
}
```

### Memory Profiling
- Use dotMemory or PerfView
- Monitor DbContext disposal
- Check for memory leaks in long-running scenarios

## 📝 Code Style Guidelines

### Naming Conventions
- **Classes**: PascalCase (`EmailProcessingService`)
- **Methods**: PascalCase (`ProcessPendingEmailsAsync`)
- **Properties**: PascalCase (`ToEmail`)
- **Fields**: camelCase with underscore (`_emailService`)
- **Constants**: PascalCase (`MaxRetryAttempts`)

### File Organization
```
Services/
├── Interfaces/
│   ├── IEmailService.cs
│   └── ISimpleEmailService.cs
├── Implementations/
│   ├── SimpleEmailService.cs
│   └── EmailProcessingService.cs
└── Models/
    ├── EmailServiceResult.cs
    └── EmailSendRequest.cs
```

### Documentation Standards
```csharp
/// <summary>
/// Processes all pending emails in the queue and sends them via SMTP.
/// </summary>
/// <returns>The number of emails successfully sent.</returns>
/// <exception cref="InvalidOperationException">Thrown when database is unavailable.</exception>
public async Task<int> ProcessPendingEmailsAsync()
{
    // Implementation
}
```

## 🚀 Deployment

### Local Deployment
```bash
# Build release version
dotnet build -c Release

# Run with production settings
dotnet run -c Release
```

### Environment Variables
```bash
# Windows
set EmailSettings__Username=your-email@company.com
set EmailSettings__Password=your-password

# Linux/Mac
export EmailSettings__Username="your-email@company.com"
export EmailSettings__Password="your-password"
```

### Docker (Future)
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0
COPY bin/Release/net8.0/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "AutomaticMailer.dll"]
```

## 📚 Resources

### Documentation
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [MailKit Documentation](https://github.com/jstedfast/MailKit)
- [xUnit Testing Framework](https://xunit.net/)
- [Moq Mocking Framework](https://github.com/moq/moq4)

### Tools
- **Database Tools**: SQL Server Management Studio, Azure Data Studio
- **SMTP Testing**: MailHog, Papercut, smtp4dev
- **Code Analysis**: SonarQube, CodeQL
- **Performance**: dotTrace, PerfView

### Best Practices
- Follow SOLID principles
- Write tests before implementation (TDD)
- Use dependency injection
- Handle errors gracefully
- Log appropriately (not too much, not too little)
- Keep methods small and focused 