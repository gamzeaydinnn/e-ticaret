using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.Attributes
{
    /// <summary>
    /// Tarih aralığı validasyonu - StartDate ve EndDate arasındaki süreyi kontrol eder.
    /// Kampanya süresinin çok kısa veya çok uzun olmasını önler.
    ///
    /// Kullanım:
    /// [DateRange(MinDays = 1, MaxDays = 365)]
    /// public class CampaignSaveDto
    /// {
    ///     public DateTime StartDate { get; set; }
    ///     public DateTime EndDate { get; set; }
    /// }
    /// </summary>
    public class DateRangeAttribute : ValidationAttribute
    {
        /// <summary>
        /// Minimum kampanya süresi (gün)
        /// </summary>
        public int MinDays { get; set; } = 1;

        /// <summary>
        /// Maksimum kampanya süresi (gün)
        /// </summary>
        public int MaxDays { get; set; } = 365;

        /// <summary>
        /// StartDate property adı (varsayılan: "StartDate")
        /// </summary>
        public string StartDateProperty { get; set; } = "StartDate";

        /// <summary>
        /// EndDate property adı (varsayılan: "EndDate")
        /// </summary>
        public string EndDateProperty { get; set; } = "EndDate";

        public DateRangeAttribute()
        {
            ErrorMessage = "Kampanya süresi {0}-{1} gün arasında olmalıdır";
        }

        /// <summary>
        /// Tarih aralığının belirlenen limitler içinde olduğunu doğrular
        /// </summary>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // DTO instance'ını al
            var dto = validationContext.ObjectInstance;
            if (dto == null)
            {
                return ValidationResult.Success;
            }

            // StartDate ve EndDate property'lerini al
            var startDateProperty = dto.GetType().GetProperty(StartDateProperty);
            var endDateProperty = dto.GetType().GetProperty(EndDateProperty);

            if (startDateProperty == null || endDateProperty == null)
            {
                return new ValidationResult(
                    $"'{StartDateProperty}' veya '{EndDateProperty}' property'si bulunamadı",
                    new[] { validationContext.MemberName ?? "Unknown" });
            }

            // Değerleri al
            var startDateValue = startDateProperty.GetValue(dto);
            var endDateValue = endDateProperty.GetValue(dto);

            // Null kontrolü
            if (startDateValue == null || endDateValue == null)
            {
                return ValidationResult.Success; // Required attribute ile kontrol edilmeli
            }

            // Tip kontrolü
            if (startDateValue is not DateTime startDate || endDateValue is not DateTime endDate)
            {
                return new ValidationResult(
                    "Başlangıç ve bitiş tarihleri geçerli DateTime değerleri olmalıdır",
                    new[] { validationContext.MemberName ?? "Unknown" });
            }

            // Mantık kontrolü: EndDate > StartDate
            if (endDate <= startDate)
            {
                return new ValidationResult(
                    "Bitiş tarihi başlangıç tarihinden sonra olmalıdır",
                    new[] { EndDateProperty });
            }

            // Süre hesaplama
            var duration = (endDate - startDate).TotalDays;

            // Minimum süre kontrolü
            if (duration < MinDays)
            {
                return new ValidationResult(
                    $"Kampanya süresi en az {MinDays} gün olmalıdır. Şu anki süre: {Math.Ceiling(duration)} gün",
                    new[] { EndDateProperty });
            }

            // Maksimum süre kontrolü
            if (duration > MaxDays)
            {
                return new ValidationResult(
                    $"Kampanya süresi en fazla {MaxDays} gün olabilir. Şu anki süre: {Math.Ceiling(duration)} gün",
                    new[] { EndDateProperty });
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// Format metodu - error message'da {0}, {1} placeholder'ları için
        /// </summary>
        public override string FormatErrorMessage(string name)
        {
            return string.Format(ErrorMessageString, MinDays, MaxDays);
        }
    }
}
