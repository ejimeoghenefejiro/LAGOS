namespace LGRRS.Api.Contracts;

public record RegisterMerchantRequest(
    string BusinessName,
    string BusinessType,
    string LgaCode,
    string? LagosTaxId,
    string? LgrrsSystemId,
    string PhoneNumber,
    string? BusinessAddress = null);

public record MerchantSummaryResponse(
    Guid MerchantId,
    string BusinessName,
    string BusinessType,
    string LgaCode,
    string Status,
    string? LgrrsSystemId = null);

public record RequestOtpRequest(string PhoneNumber);

public record VerifyOtpRequest(string PhoneNumber, string Otp);

public record TokenResponse(string AccessToken, string Role, string DisplayName);

public record StaffLoginRequest(string Email, string Password);
