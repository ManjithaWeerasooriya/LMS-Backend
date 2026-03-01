using Microsoft.AspNetCore.Identity;

namespace LMS_Backend.Models.Entities;

public enum UserStatus
{
    Active = 1,
    Pending = 2,
    Suspended = 3
}

public class User: IdentityUser
{
    public String? FirstName {get; set;}
    public String? LastName {get; set;}
    public UserStatus Status { get; set; } = UserStatus.Active;
    public required String Password {get; set;}
    public string? Phone {get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

}

