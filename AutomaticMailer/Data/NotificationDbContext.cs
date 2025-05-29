using Microsoft.EntityFrameworkCore;
using AutomaticMailer.Models;

namespace AutomaticMailer.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<NotificationRecord> NotificationRecords { get; set; }
    public DbSet<EmailSend> EmailSends { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NotificationRecord>(entity =>
        {
            entity.HasIndex(e => e.IsSent);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.NextRetryAt);
        });

        modelBuilder.Entity<EmailSend>(entity =>
        {
            entity.HasIndex(e => e.IsSent);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
} 