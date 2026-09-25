using System.Security.Cryptography;
using LGRRS.Domain.Entities;
using LGRRS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
namespace LGRRS.Infrastructure.Providers;

// SQL-backed challenges; delivery remains console-only in Development.
public class DatabaseOtpProvider(LgrrsDbContext db, IHostEnvironment environment, IConfiguration configuration) : IOtpProvider
{
    public async Task<bool> RequestOtp(string phoneHash, string purpose, Guid? merchantId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var entry = await db.OtpChallenges.SingleOrDefaultAsync(o => o.PhoneHash == phoneHash && o.Purpose == purpose);
        if (entry != null && entry.CreatedAt > now.AddSeconds(-30)) return false;
        if (entry == null)
        {
            entry = new OtpChallenge { PhoneHash = phoneHash, Purpose = purpose };
            db.OtpChallenges.Add(entry);
        }
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        entry.MerchantId = merchantId;
        entry.Code = code;
        entry.CreatedAt = now;
        entry.ExpiresAt = now.AddMinutes(5);
        entry.ConsumedAt = null;
        entry.FailedAttempts = 0;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return false; }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { return false; }
        if (environment.IsDevelopment() || string.Equals(configuration["Demo:AllowConsoleOtp"], "true", StringComparison.OrdinalIgnoreCase))
            Console.WriteLine($"[DatabaseOtpProvider] {purpose} OTP for {phoneHash[..8]}...: {code}");
        return true;
    }
    public async Task<bool> VerifyOtp(string phoneHash, string purpose, string otp)
    {
        var entry = await db.OtpChallenges.SingleOrDefaultAsync(o => o.PhoneHash == phoneHash && o.Purpose == purpose);
        var now = DateTimeOffset.UtcNow;
        if (entry == null || entry.ConsumedAt != null || entry.ExpiresAt <= now || entry.FailedAttempts >= 5)
            return false;
        var valid = otp is { Length: 6 } && otp.All(char.IsAsciiDigit) && string.Equals(otp, entry.Code, StringComparison.Ordinal);
        if (valid) entry.ConsumedAt = now;
        else entry.FailedAttempts++;
        // Rowversion prevents concurrent verification or resend from reusing a code.
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return false; }
        return valid;
    }
}
