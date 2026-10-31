using System;
using System.Linq;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// Mikro grup kodu normalleştirme yardımcıları.
    /// </summary>
    public static class MikroGrupKodHelper
    {
        /// <summary>
        /// Ana grup kodunu normalleştirir: boşlukları kırpar, tek haneli rakamı başa "0" ekleyerek
        /// iki haneye tamamlar ("2" → "02"). Ürün kartlarında bazı ana grup kodları tek haneli girilmiş.
        /// 3-4 haneli yeni düzen kodlarına ("100", "2200") dokunmaz.
        /// </summary>
        public static string NormalizeAnaKod(string? kod)
        {
            var k = (kod ?? string.Empty).Trim();
            if (k.Length == 1 && char.IsDigit(k[0]))
                return "0" + k;
            return k;
        }

        /// <summary>
        /// Alt grup kodunu kırpar. Alt kodlara 0 eklenmez: eski düzende "1" ve "01"
        /// Mikro'da farklı kayıtlardır (örn. 02/1 ALKOLLÜ İÇECEKLER ve 02/01 RAKILAR).
        /// </summary>
        public static string NormalizeAltKod(string? kod) => (kod ?? string.Empty).Trim();

        /// <summary>Yeni düzen (3-4 haneli) ana grup kodu mu? (100, 500, 1400, 2200 …)</summary>
        public static bool IsYeniDuzen(string? anaKod)
        {
            var k = (anaKod ?? string.Empty).Trim();
            return k.Length >= 3 && k.All(char.IsDigit);
        }

        /// <summary>E-ticarette satışı yasaklı olan ana gruplar (02 Alkol, 03 Tütün).</summary>
        public static bool IsForbiddenAnaGrup(string? anaKod)
        {
            var norm = NormalizeAnaKod(anaKod);
            return string.Equals(norm, "02", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(norm, "03", StringComparison.OrdinalIgnoreCase);
        }
    }
}
