namespace LGRRS.Api.Contracts;

public record RegisterMerchantRequest(
    string BusinessName,
    string BusinessType,
    string LgaCode,
    string? LagosTaxId,
    string? LgrrsSystemId,
    [NigerianPhone] string PhoneNumber,
    string? BusinessAddress = null);

public record MerchantSummaryResponse(
    Guid MerchantId,
    string BusinessName,
    string BusinessType,
    string LgaCode,
    string Status,
    string? LgrrsSystemId = null);

public record RequestOtpRequest([NigerianPhone] string PhoneNumber);

public record VerifyOtpRequest([NigerianPhone] string PhoneNumber, string Otp);
public record MerchantPasscodeSetupRequest([NigerianPhone] string PhoneNumber, string Otp, string Passcode);
public record MerchantPasscodeLoginRequest([NigerianPhone] string PhoneNumber, string Passcode);

public record TokenResponse(string AccessToken, string Role, string DisplayName);

public record StaffLoginRequest(string Email, string Password);

