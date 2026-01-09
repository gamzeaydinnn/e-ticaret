using FluentValidation;
using ECommerce.API.DTOs.Sms;

namespace ECommerce.API.Validators.Sms
{
    /// <summary>
    /// OTP doğrulama isteği validator.
    /// Telefon numarası, kod formatı ve doğrulama amacını kontrol eder.
    /// </summary>
    public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequestDto>
    {
        public VerifyOtpRequestValidator()
        {
            // Telefon numarası validasyonu
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Telefon numarası gereklidir.")
                .Matches(@"^(0?5\d{9})$")
                .WithMessage("Geçerli bir Türkiye cep telefonu numarası giriniz.")
                .WithErrorCode("INVALID_PHONE");

            // OTP kodu validasyonu
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Doğrulama kodu gereklidir.")
                .Length(6)
                .WithMessage("Kod 6 haneli olmalıdır.")
                .Matches(@"^\d{6}$")
                .WithMessage("Kod sadece rakamlardan oluşmalıdır.")
                .WithErrorCode("INVALID_CODE_FORMAT");

            // Purpose enum validasyonu
            RuleFor(x => x.Purpose)
                .IsInEnum()
                .WithMessage("Geçerli bir doğrulama amacı seçiniz.")
                .WithErrorCode("INVALID_PURPOSE");
        }
    }
}
