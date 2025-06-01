using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Locale { get; set; }

    [PersonalData]
    public string? ProfilePictureUrl { get; set; }

    public string? TwoFactorSecret { get; set; }

    public string? Preferred2FAMethod { get; set; } // "Email" or "Authenticator"



}