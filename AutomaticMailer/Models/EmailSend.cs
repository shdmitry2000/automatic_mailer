using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomaticMailer.Models;

[Table("email_send")]
public class EmailSend
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string ToEmail { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    public string Body { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? SentAt { get; set; }
    
    public bool IsSent { get; set; } = false;
    
    [Column("sent_t_n")]
    public bool SentTN { get; set; } = false;
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
} 