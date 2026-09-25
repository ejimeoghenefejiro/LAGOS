namespace LGRRS.Infrastructure.Security;

// Produces the merchant-facing masked phone format from spec section 5.2, e.g. "0808 *** 7788".
public static class PhoneMasker
{
    public static string Mask(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 8)
        {
            return "***";
        }

        var prefix = digits[..4];
        var suffix = digits[^4..];
        return $"{prefix} *** {suffix}";
    }
}
