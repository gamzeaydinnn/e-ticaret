using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace ECommerce.Core.Attributes
{
    /// <summary>
    /// HTML/JavaScript içeriğini engelleyen custom validation attribute.
    /// XSS saldırılarına karşı koruma sağlar.
    ///
    /// Kullanım:
    /// [NoHtmlContent]
    /// public string Name { get; set; }
    /// </summary>
    public class NoHtmlContentAttribute : ValidationAttribute
    {
        /// <summary>
        /// Tehlikeli karakter setleri
        /// </summary>
        private static readonly string[] DangerousPatterns = new[]
        {
            "<script",     // Script tag
            "</script>",   // Closing script tag
            "javascript:", // JavaScript protocol
            "onerror=",    // Event handler
            "onload=",     // Event handler
            "onclick=",    // Event handler
            "onfocus=",    // Event handler
            "onmouseover=",// Event handler
            "<iframe",     // Iframe tag
            "<object",     // Object tag
            "<embed",      // Embed tag
            "eval(",       // JavaScript eval
            "expression(", // CSS expression
            "vbscript:",   // VBScript protocol
            "data:text/html" // Data URI with HTML
        };

        public NoHtmlContentAttribute()
        {
            ErrorMessage = "HTML veya JavaScript içeriği giremezsiniz";
        }

        /// <summary>
        /// String değerinin HTML/JavaScript içermediğini doğrular
        /// </summary>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Null veya boş değerler geçerli (Required attribute ile kontrol edilmeli)
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var stringValue = value.ToString()!;

            try
            {
                // HTML decode ile gizlenmiş içerikleri yakala
                var decoded = HttpUtility.HtmlDecode(stringValue);

                // Küçük harfe çevir (case-insensitive kontrol)
                var lowerValue = decoded.ToLowerInvariant();
                var originalLower = stringValue.ToLowerInvariant();

                // Tehlikeli pattern'leri kontrol et
                foreach (var pattern in DangerousPatterns)
                {
                    if (lowerValue.Contains(pattern) || originalLower.Contains(pattern))
                    {
                        return new ValidationResult(
                            $"Güvenlik nedeniyle '{pattern}' içeriği kullanılamaz",
                            new[] { validationContext.MemberName ?? "Unknown" });
                    }
                }

                // Açılı parantez kontrolü (< ve > karakterleri)
                if (decoded.Contains('<') || decoded.Contains('>'))
                {
                    // Bazı özel durumlar hariç
                    // Örnek: "Ürün 2 > 1 adet" gibi matematiksel ifadeler kabul edilebilir
                    // Ancak tag benzeri yapıları yasakla
                    if (ContainsHtmlTags(decoded))
                    {
                        return new ValidationResult(
                            "HTML tag'leri kullanılamaz",
                            new[] { validationContext.MemberName ?? "Unknown" });
                    }
                }

                return ValidationResult.Success;
            }
            catch (Exception)
            {
                // HTML decode hatası oluşursa şüpheli kabul et
                return new ValidationResult(
                    "Geçersiz karakter dizisi",
                    new[] { validationContext.MemberName ?? "Unknown" });
            }
        }

        /// <summary>
        /// String'in HTML tag'i içerip içermediğini kontrol eder
        /// </summary>
        private static bool ContainsHtmlTags(string value)
        {
            // Basit HTML tag pattern'i: <anytext>
            var openBracket = value.IndexOf('<');
            if (openBracket == -1) return false;

            var closeBracket = value.IndexOf('>', openBracket);
            if (closeBracket == -1) return false;

            // < ve > arasında metin varsa tag olabilir
            var tagContent = value.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim();

            // Boş değilse ve matematiksel operatör değilse tag'dir
            // Örnek: "<div>" → tag, "2 < 3 > 1" → matematiksel ifade
            if (!string.IsNullOrWhiteSpace(tagContent))
            {
                // İçinde boşluk veya alfabe karakteri varsa muhtemelen tag
                return tagContent.Length > 0 &&
                       (tagContent.Contains(' ') ||
                        char.IsLetter(tagContent[0]) ||
                        tagContent.StartsWith('/'));
            }

            return false;
        }
    }
}
