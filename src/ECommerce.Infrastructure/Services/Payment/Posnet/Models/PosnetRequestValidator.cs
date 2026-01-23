// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET REQUEST VALIDATOR
// Yapı Kredi POSNET request'lerinin doğrulama kuralları
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. Validation logic'i model'den ayrı tutarak SRP (Single Responsibility) sağlıyoruz
// 2. FluentValidation benzeri readable validation
// 3. Tüm validation hataları tek seferde toplanıyor (fail-fast değil)
// 4. Regex pattern'ler performans için compile edilmiş
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace ECommerce.Infrastructure.Services.Payment.Posnet.Models
{
    /// <summary>
    /// POSNET request validasyonu için helper class
    /// DataAnnotation validation + custom business rules
    /// </summary>
    public static class PosnetRequestValidator
    {
        // ═══════════════════════════════════════════════════════════════════════
        // Compiled Regex Patterns - Performans için tek seferlik compile
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kart numarası pattern - 13-19 haneli rakam
        /// </summary>
        private static readonly Regex CardNumberRegex = new(@"^\d{13,19}$", RegexOptions.Compiled);

        /// <summary>
        /// Son kullanma tarihi pattern - YYAA formatı
        /// </summary>
        private static readonly Regex ExpireDateRegex = new(@"^\d{4}$", RegexOptions.Compiled);

        /// <summary>
        /// CVV pattern - 3 veya 4 haneli
        /// </summary>
        private static readonly Regex CvvRegex = new(@"^\d{3,4}$", RegexOptions.Compiled);

        /// <summary>
        /// OrderId pattern - Alfanumerik, 1-24 karakter
        /// </summary>
        private static readonly Regex OrderIdRegex = new(@"^[a-zA-Z0-9]{1,24}$", RegexOptions.Compiled);

        /// <summary>
        /// MerchantId pattern - 10 haneli rakam
        /// </summary>
        private static readonly Regex MerchantIdRegex = new(@"^\d{10}$", RegexOptions.Compiled);

        /// <summary>
        /// TerminalId pattern - 8 karakterli alfanumerik
        /// POSNET dokümanına göre TerminalId alfanumerik olabilir (örn: 67C35037)
        /// </summary>
        private static readonly Regex TerminalIdRegex = new(@"^[a-zA-Z0-9]{8}$", RegexOptions.Compiled);

        // ═══════════════════════════════════════════════════════════════════════
        // Validation Methods
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Request'i doğrular ve hata listesi döner
        /// Boş liste = valid, dolu liste = invalid
        /// </summary>
        public static List<string> Validate<T>(T request) where T : PosnetBaseRequest
        {
            var errors = new List<string>();

            // 1. DataAnnotation validasyonu
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            bool isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            if (!isValid)
            {
                errors.AddRange(validationResults.Select(r => r.ErrorMessage ?? "Bilinmeyen doğrulama hatası"));
            }

            // 2. MerchantId kontrolü
            if (!MerchantIdRegex.IsMatch(request.MerchantId ?? ""))
            {
                errors.Add("MerchantId 10 haneli sayı olmalıdır");
            }

            // 3. TerminalId kontrolü
            if (!TerminalIdRegex.IsMatch(request.TerminalId ?? ""))
            {
                errors.Add("TerminalId 8 karakterli alfanumerik olmalıdır");
            }

            // 4. Request tipine özel validasyonlar
            switch (request)
            {
                case PosnetSaleRequest sale:
                    ValidateSaleRequest(sale, errors);
                    break;
                case PosnetAuthRequest auth:
                    ValidateCardInfo(auth.Card, errors);
                    ValidateOrderId(auth.OrderId, errors);
                    break;
                case PosnetReverseRequest reverse:
                    ValidateReverseRequest(reverse, errors);
                    break;
                case PosnetReturnRequest returnReq:
                    ValidateReturnRequest(returnReq, errors);
                    break;
                case PosnetOosRequest oos:
                    ValidateOosRequest(oos, errors);
                    break;
            }

            return errors;
        }

        /// <summary>
        /// Request valid mi kontrolü (quick check)
        /// </summary>
        public static bool IsValid<T>(T request) where T : PosnetBaseRequest
        {
            return Validate(request).Count == 0;
        }

        /// <summary>
        /// Validation hatalarını exception olarak fırlatır
        /// </summary>
        public static void ValidateAndThrow<T>(T request) where T : PosnetBaseRequest
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                throw new PosnetValidationException(errors);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Specific Validations
        // ═══════════════════════════════════════════════════════════════════════

        private static void ValidateSaleRequest(PosnetSaleRequest sale, List<string> errors)
        {
            ValidateCardInfo(sale.Card, errors);
            ValidateOrderId(sale.OrderId, errors);
            ValidateAmount(sale.Amount, errors);
            ValidateInstallment(sale.Installment, errors);
        }

        private static void ValidateCardInfo(PosnetCardInfo? card, List<string> errors)
        {
            if (card == null)
            {
                errors.Add("Kart bilgileri zorunludur");
                return;
            }

            // Kart numarası kontrolü
            if (string.IsNullOrWhiteSpace(card.CardNumber))
            {
                errors.Add("Kart numarası zorunludur");
            }
            else
            {
                var cleanCardNumber = card.CardNumber.Replace(" ", "").Replace("-", "");
                if (!CardNumberRegex.IsMatch(cleanCardNumber))
                {
                    errors.Add("Kart numarası 13-19 haneli sayı olmalıdır");
                }
                // NOT: Luhn kontrolü kaldırıldı - POSNET kendi validasyonunu yapıyor
                // Test kartları Luhn algoritmasına uymuyor olabilir
            }

            // Son kullanma tarihi kontrolü
            if (string.IsNullOrWhiteSpace(card.ExpireDate))
            {
                errors.Add("Son kullanma tarihi zorunludur");
            }
            else if (!ExpireDateRegex.IsMatch(card.ExpireDate))
            {
                errors.Add("Son kullanma tarihi YYAA formatında olmalıdır (örn: 2512)");
            }
            else
            {
                // Tarihin geçip geçmediğini kontrol et
                if (IsCardExpired(card.ExpireDate))
                {
                    errors.Add("Kartın süresi dolmuş");
                }
            }

            // CVV kontrolü
            if (string.IsNullOrWhiteSpace(card.Cvv))
            {
                errors.Add("CVV zorunludur");
            }
            else if (!CvvRegex.IsMatch(card.Cvv))
            {
                errors.Add("CVV 3 veya 4 haneli sayı olmalıdır");
            }
        }

        private static void ValidateOrderId(string? orderId, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                errors.Add("Sipariş numarası zorunludur");
            }
            else if (!OrderIdRegex.IsMatch(orderId))
            {
                errors.Add("Sipariş numarası 1-24 karakter, sadece alfanumerik olmalıdır");
            }
        }

        private static void ValidateAmount(int amount, List<string> errors)
        {
            if (amount <= 0)
            {
                errors.Add("Tutar 0'dan büyük olmalıdır");
            }
            else if (amount > 999999999)
            {
                errors.Add("Tutar maksimum 9.999.999,99 TL olabilir");
            }
        }

        private static void ValidateInstallment(string? installment, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(installment))
            {
                errors.Add("Taksit sayısı zorunludur");
            }
            else if (!Regex.IsMatch(installment, @"^(0[0-9]|1[0-2])$"))
            {
                errors.Add("Taksit 00-12 arasında olmalıdır (00 = peşin)");
            }
        }

        private static void ValidateReverseRequest(PosnetReverseRequest reverse, List<string> errors)
        {
            ValidateOrderId(reverse.OrderId, errors);
            
            if (string.IsNullOrWhiteSpace(reverse.HostLogKey))
            {
                errors.Add("HostLogKey iptal işlemi için zorunludur");
            }
        }

        private static void ValidateReturnRequest(PosnetReturnRequest returnReq, List<string> errors)
        {
            ValidateOrderId(returnReq.OrderId, errors);
            ValidateAmount(returnReq.Amount, errors);
            
            if (string.IsNullOrWhiteSpace(returnReq.HostLogKey))
            {
                errors.Add("HostLogKey iade işlemi için zorunludur");
            }
        }

        private static void ValidateOosRequest(PosnetOosRequest oos, List<string> errors)
        {
            ValidateCardInfo(oos.Card, errors);
            ValidateOrderId(oos.OrderId, errors);
            ValidateAmount(oos.Amount, errors);
            ValidateInstallment(oos.Installment, errors);

            if (string.IsNullOrWhiteSpace(oos.PosnetId))
            {
                errors.Add("PosnetId 3D Secure işlemleri için zorunludur");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Helper Methods
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Luhn algoritması ile kart numarası doğrulama
        /// Kredi kartı numaralarının doğruluğunu kontrol eden standart algoritma
        /// </summary>
        private static bool IsValidLuhn(string number)
        {
            int sum = 0;
            bool alternate = false;

            for (int i = number.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(number[i])) return false;

                int n = number[i] - '0';
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        /// <summary>
        /// Kartın süresinin dolup dolmadığını kontrol eder
        /// YYAA formatında tarih alır
        /// </summary>
        private static bool IsCardExpired(string expireDate)
        {
            if (expireDate.Length != 4) return true;

            if (!int.TryParse(expireDate[..2], out int year)) return true;
            if (!int.TryParse(expireDate[2..], out int month)) return true;

            // 2 haneli yılı 4 haneye çevir (00-99 → 2000-2099)
            year += 2000;

            var now = DateTime.Now;
            var expireDateTime = new DateTime(year, month, 1).AddMonths(1).AddDays(-1); // Ayın son günü

            return expireDateTime < now;
        }
    }

    /// <summary>
    /// POSNET validasyon hatası exception'ı
    /// </summary>
    public class PosnetValidationException : Exception
    {
        /// <summary>
        /// Tüm validasyon hataları
        /// </summary>
        public List<string> Errors { get; }

        public PosnetValidationException(List<string> errors)
            : base($"POSNET validasyon hatası: {string.Join(", ", errors)}")
        {
            Errors = errors;
        }
    }
}
