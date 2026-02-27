using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Token { get; set; } = default!; // (Better: store a hash, but keeping simple)

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    [Required]
    public string UserId { get; set; } = default!;

    [Required]
    public string DeviceId { get; set; } = default!;

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = default!;
}