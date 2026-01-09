using FluentValidation;
using ECommerce.API.DTOs.Sms;

namespace ECommerce.API.Validators.Sms
{
    /// <summary>
    /// OTP gönderme isteği validator.
    /// Telefon numarası formatını ve doğrulama amacını kontrol eder.
    /// </summary>
    public class SendOtpRequestValidator : AbstractValidator<SendOtpRequestDto>
    {
        public SendOtpRequestValidator()
        {
            // Telefon numarası validasyonu
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Telefon numarası gereklidir.")
                .Matches(@"^(0?5\d{9})$")
                .WithMessage("Geçerli bir Türkiye cep telefonu numarası giriniz. (05XXXXXXXXX veya 5XXXXXXXXX)")
                .WithErrorCode("INVALID_PHONE");

            // Purpose enum validasyonu
            RuleFor(x => x.Purpose)
                .IsInEnum()
                .WithMessage("Geçerli bir doğrulama amacı seçiniz.")
                .WithErrorCode("INVALID_PURPOSE");
        }
    }
}
