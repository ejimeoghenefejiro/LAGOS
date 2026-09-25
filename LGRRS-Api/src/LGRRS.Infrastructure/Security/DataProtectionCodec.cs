using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace LGRRS.Infrastructure.Security;

public class DataProtectionCodec : IDataProtectionCodec
{
    private const string Purpose = "LGRRS.PII.v1";
    private readonly IDataProtector _protector;

    public DataProtectionCodec(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Encrypt(string plaintext) => _protector.Protect(plaintext);

    public string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext);

    // One-way hash for deduplication/matching (e.g. phone lookup) without decrypting PII.
    public string Hash(string normalizedValue)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedValue));
        return Convert.ToHexString(bytes);
    }
}
