namespace LGRRS.Infrastructure.Providers;

public enum BusinessVerificationResult
{
    Verified,
    NotFound,
    InvalidFormat
}

// Spec section 11: IBusinessVerificationProvider — demo implementation now, swappable
// for an approved Lagos/LGA tax or business identity service later.
public interface IBusinessVerificationProvider
{
    BusinessVerificationResult Verify(string? lagosTaxId, string? lgrrsSystemId);
}
