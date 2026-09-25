namespace LGRRS.Infrastructure.Providers;

// Spec section 10: OTP-assisted phone verification for merchant access.
public interface IOtpProvider
{
    Task<bool> RequestOtp(string phoneHash, string purpose, Guid? merchantId = null);
    Task<bool> VerifyOtp(string phoneHash, string purpose, string otp);
}
