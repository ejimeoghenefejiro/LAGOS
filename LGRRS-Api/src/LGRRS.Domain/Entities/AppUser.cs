using LGRRS.Domain.Enums;

namespace LGRRS.Domain.Entities;

// Staff account (LGA Admin, Claim Processor, Auditor). Merchants authenticate via
// phone/OTP against Merchant directly rather than a staff account, per spec section 10.
public class AppUser
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AppRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
