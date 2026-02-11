using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.Attributes
{
    /// <summary>
    /// Gelecek tarih validasyonu - Geçmişteki tarih girilemez.
    /// Kampanya başlangıç tarihlerinin geçmişte olmasını önler.
    ///
    /// Kullanım:
    /// [FutureDate]
    /// public DateTime StartDate { get; set; }
    /// </summary>
    public class FutureDateAttribute : ValidationAttribute
    {
        /// <summary>
        /// Hata mesajı şablonu
        /// </summary>
        public FutureDateAttribute()
        {
            ErrorMessage = "Tarih geçmişte olamaz. Lütfen bugün veya gelecek bir tarih seçin.";
        }

        /// <summary>
        /// DateTime değerinin gelecekte (veya bugün) olduğunu doğrular
        /// </summary>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Null değerlere izin ver (Required attribute ile kontrol edilmeli)
            if (value == null)
            {
                return ValidationResult.Success;
            }

            // Tip kontrolü
            if (value is not DateTime dateValue)
            {
                return new ValidationResult(
                    "Geçersiz tarih formatı",
                    new[] { validationContext.MemberName ?? "Unknown" });
            }

            // UTC veya Local tarih olabilir, her ikisini de destekle
            var now = DateTime.UtcNow;
            var compareDate = dateValue.Kind == DateTimeKind.Utc
                ? dateValue
                : dateValue.ToUniversalTime();

            // Bugünün başlangıcını al (00:00:00)
            var todayStart = now.Date;
            var compareDateStart = compareDate.Date;

            // Geçmiş tarih kontrolü (bugün dahil geçerli)
            if (compareDateStart < todayStart)
            {
                return new ValidationResult(
                    $"'{validationContext.DisplayName}' geçmişte olamaz. Lütfen bugün ({todayStart:dd.MM.yyyy}) veya sonrası bir tarih seçin.",
                    new[] { validationContext.MemberName ?? "Unknown" });
            }

            return ValidationResult.Success;
        }
    }
}
