using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LMS_Backend.Models.Entities;

public enum UserStatus
{
    Active = 1,
    Suspended = 3
}

public class User: IdentityUser
{
    public String? FirstName {get; set;}
    public String? LastName {get; set;}
    public UserStatus Status { get; set; } = UserStatus.Active;
    public string? Phone {get; set;}
    [MaxLength(1000)]
    public string? ProfileImageUrl { get; set; }
    [MaxLength(300)]
    public string? ProfileImageBlobName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

}
