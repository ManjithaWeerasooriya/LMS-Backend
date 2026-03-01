namespace LMS_Backend.Models.Options;

public class BootstrapAdminOptions
{
    public bool Enabled { get; set; }
    public string UserId { get; set; } = "bootstrap-admin";
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = "Bootstrap";
    public string LastName { get; set; } = "Admin";
    public string PasswordHash { get; set; } = string.Empty;
}
