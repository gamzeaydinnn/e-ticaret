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
        /// DateTime değerinin gelecekte (veya bugün) olduğunu doğrular.
        /// Timezone sorunu yaşamamak için sadece tarih kısmı (yıl/ay/gün) karşılaştırılır.
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

            // TIMEZONE SORUNU DÜZELTMESİ:
            // Frontend'den gelen tarih yerel saatte olabilir (örn: TR UTC+3).
            // ToUniversalTime() yapıldığında tarih 1 gün geriye kayabilir.
            // Bu nedenle sadece tarih kısmını (yıl/ay/gün) karşılaştırıyoruz,
            // timezone dönüşümü yapmadan.

            // Sunucu yerel zamanında bugünün tarihi
            var todayLocal = DateTime.Now.Date;

            // Gelen değerin sadece tarih kısmı (saat bilgisi önemsiz)
            var compareDateLocal = dateValue.Date;

            // Geçmiş tarih kontrolü (bugün dahil geçerli)
            if (compareDateLocal < todayLocal)
            {
                return new ValidationResult(
                    $"'{validationContext.DisplayName}' geçmişte olamaz. Lütfen bugün ({todayLocal:dd.MM.yyyy}) veya sonrası bir tarih seçin.",
                    new[] { validationContext.MemberName ?? "Unknown" });
            }

            return ValidationResult.Success;
        }
    }
}
