using System.Text.RegularExpressions;

namespace LGRRS.Infrastructure.Providers;

// Demo-only lookup: accepts anything that looks like a well-formed ID. To be replaced
// with an approved Lagos/LGA verification service post-prototype (spec section 22).
public partial class DemoBusinessVerificationProvider : IBusinessVerificationProvider
{
    public BusinessVerificationResult Verify(string? lagosTaxId, string? lgrrsSystemId)
    {
        if (string.IsNullOrWhiteSpace(lagosTaxId) && string.IsNullOrWhiteSpace(lgrrsSystemId))
        {
            return BusinessVerificationResult.InvalidFormat;
        }

        if (!string.IsNullOrWhiteSpace(lgrrsSystemId) && LgrrsIdPattern().IsMatch(lgrrsSystemId))
        {
            return BusinessVerificationResult.Verified;
        }

        if (!string.IsNullOrWhiteSpace(lagosTaxId) && lagosTaxId.Trim().Length >= 6)
        {
            return BusinessVerificationResult.Verified;
        }

        return BusinessVerificationResult.NotFound;
    }

    [GeneratedRegex(@"^LGRRS-\d{3,}$")]
    private static partial Regex LgrrsIdPattern();
}
