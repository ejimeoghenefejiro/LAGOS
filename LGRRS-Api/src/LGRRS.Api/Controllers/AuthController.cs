using LGRRS.Api.Auth;
using LGRRS.Domain.Entities;
using LGRRS.Api.Contracts;
using LGRRS.Domain.Enums;
using LGRRS.Infrastructure.Persistence;
using LGRRS.Infrastructure.Providers;
using LGRRS.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LGRRS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly LgrrsDbContext _db;
    private readonly IDataProtectionCodec _codec;
    private readonly IOtpProvider _otpProvider;
    private readonly ITokenService _tokenService;

    public AuthController(LgrrsDbContext db, IDataProtectionCodec codec, IOtpProvider otpProvider, ITokenService tokenService)
    {
        _db = db;
        _codec = codec;
        _otpProvider = otpProvider;
        _tokenService = tokenService;
    }

    [HttpPost("merchant/request-otp")]
    public async Task<IActionResult> RequestOtp(RequestOtpRequest request)
    {
        var phoneHash = _codec.Hash(request.PhoneNumber);
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.PhoneHash == phoneHash);
        if (merchant is null)
        {
            return NotFound("No merchant is registered with this phone number.");
        }

        if (!await _otpProvider.RequestOtp(phoneHash, Roles.Merchant, merchant.MerchantId))
            return StatusCode(429, "Please wait 30 seconds before requesting another code.");
        return Accepted();
    }

    [HttpPost("merchant/verify-otp")]
    public async Task<ActionResult<TokenResponse>> VerifyOtp(MerchantPasscodeSetupRequest request)
    {
        if (!ValidPasscode(request.Passcode)) return BadRequest("Choose a six-digit passcode.");
        var phoneHash = _codec.Hash(request.PhoneNumber);
        var merchantId = await _db.Merchants.Where(m => m.PhoneHash == phoneHash).Select(m => (Guid?)m.MerchantId).FirstOrDefaultAsync();
        if (merchantId is null) return Unauthorized("Invalid phone number or OTP.");
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await PosLock.Acquire(_db, merchantId.Value);
        if (!await _otpProvider.VerifyOtp(phoneHash, Roles.Merchant, request.Otp))
        {
            await transaction.CommitAsync(); // Persist failed OTP attempt counters.
            return Unauthorized("Invalid or expired OTP.");
        }

        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.PhoneHash == phoneHash);
        if (merchant is null)
        {
            return NotFound();
        }

        if (merchant.Status == MerchantStatus.Suspended)
        {
            return Forbid();
        }

        var eventType = merchant.PasscodeHash is null ? "MerchantPasscodeCreated" : "MerchantPasscodeReset";
        merchant.PasscodeHash = BCrypt.Net.BCrypt.HashPassword(request.Passcode, workFactor: 12);
        merchant.PasscodeFailedAttempts = 0;
        merchant.PasscodeLockedUntil = null;
        _db.AuditEvents.Add(new AuditEvent { EventType = eventType, EntityType = "Merchant", EntityId = merchant.MerchantId,
            ActorId = merchant.MerchantId.ToString(), ActorName = merchant.BusinessName, ActorRole = Roles.Merchant });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        var token = _tokenService.IssueToken(merchant.MerchantId.ToString(), Roles.Merchant, merchant.BusinessName);
        return Ok(new TokenResponse(token, Roles.Merchant, merchant.BusinessName));
    }

    private static bool ValidPasscode(string? passcode) => passcode is { Length: 6 } && passcode.All(char.IsAsciiDigit);

    [HttpPost("merchant/login")]
    public async Task<ActionResult<TokenResponse>> MerchantLogin(MerchantPasscodeLoginRequest request)
    {
        if (!ValidPasscode(request.Passcode)) return Unauthorized("Invalid phone number or passcode.");
        var phoneHash = _codec.Hash(request.PhoneNumber);
        var merchantId = await _db.Merchants.Where(m => m.PhoneHash == phoneHash).Select(m => (Guid?)m.MerchantId).FirstOrDefaultAsync();
        if (merchantId is null) return Unauthorized("Invalid phone number or passcode.");
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await PosLock.Acquire(_db, merchantId.Value);
        var merchant = await _db.Merchants.SingleAsync(m => m.MerchantId == merchantId);
        var now = DateTimeOffset.UtcNow;
        if (merchant.PasscodeLockedUntil > now) return StatusCode(429, "Too many incorrect attempts. Try again in 15 minutes or reset your passcode using OTP.");
        if (merchant.Status == MerchantStatus.Suspended) return Forbid();
        if (merchant.PasscodeHash is null) return Unauthorized("Verify your phone and set up your passcode first.");
        if (merchant.PasscodeLockedUntil != null) merchant.PasscodeFailedAttempts = 0;
        if (!BCrypt.Net.BCrypt.Verify(request.Passcode, merchant.PasscodeHash))
        {
            merchant.PasscodeFailedAttempts++;
            merchant.PasscodeLockedUntil = merchant.PasscodeFailedAttempts >= 5 ? now.AddMinutes(15) : null;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Unauthorized("Invalid phone number or passcode.");
        }
        merchant.PasscodeFailedAttempts = 0;
        merchant.PasscodeLockedUntil = null;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new TokenResponse(_tokenService.IssueToken(merchant.MerchantId.ToString(), Roles.Merchant, merchant.BusinessName), Roles.Merchant, merchant.BusinessName));
    }

    [HttpPost("consumer/request-otp")]
    public async Task<IActionResult> RequestConsumerOtp(RequestOtpRequest request)
    {
        var phoneHash = _codec.Hash(request.PhoneNumber);
        if (!await _otpProvider.RequestOtp(phoneHash, Roles.Consumer))
            return StatusCode(429, "Please wait 30 seconds before requesting another code.");
        return Accepted();
    }

    [HttpPost("consumer/verify-otp")]
    public async Task<ActionResult<TokenResponse>> VerifyConsumerOtp(VerifyOtpRequest request)
    {
        var phoneHash = _codec.Hash(request.PhoneNumber);
        if (!await _otpProvider.VerifyOtp(phoneHash, Roles.Consumer, request.Otp))
        {
            return Unauthorized("Invalid or expired OTP.");
        }

        var maskedPhone = PhoneMasker.Mask(request.PhoneNumber);
        var token = _tokenService.IssueToken(phoneHash, Roles.Consumer, maskedPhone);
        return Ok(new TokenResponse(token, Roles.Consumer, maskedPhone));
    }

    [HttpPost("staff/login")]
    public async Task<ActionResult<TokenResponse>> StaffLogin(StaffLoginRequest request)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid credentials.");
        }

        var token = _tokenService.IssueToken(user.UserId.ToString(), user.Role.ToString(), user.DisplayName);
        return Ok(new TokenResponse(token, user.Role.ToString(), user.DisplayName));
    }
}
