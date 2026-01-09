using FluentValidation;
using ECommerce.API.DTOs.Sms;

namespace ECommerce.API.Validators.Sms
{
    /// <summary>
    /// OTP tekrar gönderme isteği validator.
    /// </summary>
    public class ResendOtpRequestValidator : AbstractValidator<ResendOtpRequestDto>
    {
        public ResendOtpRequestValidator()
        {
            // Telefon numarası validasyonu
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Telefon numarası gereklidir.")
                .Matches(@"^(0?5\d{9})$")
                .WithMessage("Geçerli bir Türkiye cep telefonu numarası giriniz.")
                .WithErrorCode("INVALID_PHONE");

            // Purpose enum validasyonu
            RuleFor(x => x.Purpose)
                .IsInEnum()
                .WithMessage("Geçerli bir doğrulama amacı seçiniz.")
                .WithErrorCode("INVALID_PURPOSE");
        }
    }
}
