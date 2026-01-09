using ECommerce.API.Services.Otp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.Controllers;

/// <summary>
/// OTP (SMS Doğrulama) Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class OtpController : ControllerBase
{
    private readonly IOtpService _otpService;
    private readonly ILogger<OtpController> _logger;

    public OtpController(IOtpService otpService, ILogger<OtpController> logger)
    {
        _otpService = otpService;
        _logger = logger;
    }

    /// <summary>
    /// OTP kodu gönderir
    /// </summary>
    /// <param name="request">Telefon numarası</param>
    /// <returns>Gönderim sonucu</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(OtpSendResponse), 200)]
    [ProducesResponseType(typeof(OtpSendResponse), 429)] // Too Many Requests
    [ProducesResponseType(typeof(OtpSendResponse), 400)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new OtpSendResponse
            {
                Success = false,
                Message = "Geçersiz telefon numarası."
            });
        }

        _logger.LogInformation("[OtpController] OTP gönderim isteği: {Phone}", request.PhoneNumber);

        var result = await _otpService.SendOtpAsync(request.PhoneNumber);

        if (!result.Success && result.RetryAfterSeconds.HasValue)
        {
            Response.Headers.Append("Retry-After", result.RetryAfterSeconds.Value.ToString());
            return StatusCode(429, new OtpSendResponse
            {
                Success = false,
                Message = result.Message,
                RetryAfterSeconds = result.RetryAfterSeconds
            });
        }

        if (!result.Success)
        {
            return BadRequest(new OtpSendResponse
            {
                Success = false,
                Message = result.Message
            });
        }

        return Ok(new OtpSendResponse
        {
            Success = true,
            Message = result.Message,
            ExpiresInSeconds = result.ExpiresInSeconds
        });
    }

    /// <summary>
    /// OTP kodunu doğrular
    /// </summary>
    /// <param name="request">Telefon ve kod</param>
    /// <returns>Doğrulama sonucu</returns>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(OtpVerifyResponse), 200)]
    [ProducesResponseType(typeof(OtpVerifyResponse), 400)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new OtpVerifyResponse
            {
                Success = false,
                Message = "Geçersiz istek."
            });
        }

        _logger.LogInformation("[OtpController] OTP doğrulama isteği: {Phone}", request.PhoneNumber);

        var result = await _otpService.VerifyOtpAsync(request.PhoneNumber, request.Code);

        if (!result.Success)
        {
            return BadRequest(new OtpVerifyResponse
            {
                Success = false,
                Message = result.Message,
                RemainingAttempts = result.RemainingAttempts
            });
        }

        return Ok(new OtpVerifyResponse
        {
            Success = true,
            Message = result.Message,
            Token = result.Token
        });
    }

    /// <summary>
    /// OTP gönderebilir mi kontrolü (rate limit)
    /// </summary>
    /// <param name="phoneNumber">Telefon numarası</param>
    /// <returns>Gönderebilir mi?</returns>
    [HttpGet("can-send")]
    [ProducesResponseType(typeof(CanSendResponse), 200)]
    public async Task<IActionResult> CanSend([FromQuery] string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BadRequest(new CanSendResponse { CanSend = false });
        }

        var canSend = await _otpService.CanSendOtpAsync(phoneNumber);
        return Ok(new CanSendResponse { CanSend = canSend });
    }
}

#region Request/Response Models

/// <summary>
/// OTP gönderim isteği
/// </summary>
public class SendOtpRequest
{
    /// <summary>
    /// Telefon numarası (05XXXXXXXXX veya 5XXXXXXXXX)
    /// </summary>
    [Required(ErrorMessage = "Telefon numarası gereklidir.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [RegularExpression(@"^(0?5\d{9})$", ErrorMessage = "Geçerli bir Türkiye cep telefonu numarası giriniz.")]
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// OTP doğrulama isteği
/// </summary>
public class VerifyOtpRequest
{
    /// <summary>
    /// Telefon numarası
    /// </summary>
    [Required(ErrorMessage = "Telefon numarası gereklidir.")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// SMS ile gelen 6 haneli kod
    /// </summary>
    [Required(ErrorMessage = "Doğrulama kodu gereklidir.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Kod 6 haneli olmalıdır.")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// OTP gönderim yanıtı
/// </summary>
public class OtpSendResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? RetryAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
}

/// <summary>
/// OTP doğrulama yanıtı
/// </summary>
public class OtpVerifyResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public int RemainingAttempts { get; set; }
}

/// <summary>
/// OTP gönderebilir mi yanıtı
/// </summary>
public class CanSendResponse
{
    public bool CanSend { get; set; }
}

#endregion
