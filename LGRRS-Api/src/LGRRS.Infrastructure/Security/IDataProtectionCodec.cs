namespace LGRRS.Infrastructure.Security;

// Wraps ASP.NET Core Data Protection so PII (phone, bank name/account) is encrypted
// at rest per spec section 17, while staying swappable behind an interface for later
// key-management changes (e.g. moving to Azure Key Vault).
public interface IDataProtectionCodec
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string Hash(string normalizedValue);
}
