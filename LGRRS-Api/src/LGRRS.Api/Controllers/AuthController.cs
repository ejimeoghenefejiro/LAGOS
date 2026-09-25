using LGRRS.Api.Auth;
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
    public async Task<ActionResult<TokenResponse>> VerifyOtp(VerifyOtpRequest request)
    {
        var phoneHash = _codec.Hash(request.PhoneNumber);
        if (!await _otpProvider.VerifyOtp(phoneHash, Roles.Merchant, request.Otp))
        {
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

        var token = _tokenService.IssueToken(merchant.MerchantId.ToString(), Roles.Merchant, merchant.BusinessName);
        return Ok(new TokenResponse(token, Roles.Merchant, merchant.BusinessName));
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
