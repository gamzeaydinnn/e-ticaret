using System;
using System.Collections.Generic;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// ProductSeeder / demo verisi ile eklenen ürünler — Mikro ERP kaynaklı değildir.
    /// Admin panelinden sonradan eklenen manuel ürünler bu listeye dahil değildir.
    /// </summary>
    public static class LegacySeedProductSkus
    {
        public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
        {
            "ET-001",
            "ET-002",
            "ET-003",
            "SUT-001",
            "SUT-002",
            "SEB-001",
            "SEB-002",
            "ICE-001",
            "ICE-002",
            "ICE-003",
            "ATI-001",
            "TEM-001",
            "BAK-001",
        };

        public static readonly HashSet<string> LegacySlugs = new(StringComparer.OrdinalIgnoreCase)
        {
            "dana-kusbasi",
            "kuzu-incik",
            "sucuk-250gr",
            "pinar-sut-1l",
            "sek-kasar-peyniri-200gr",
            "domates-kg",
            "salatalik-kg",
            "bulgur-1-kg",
            "coca-cola-330ml",
            "lipton-ice-tea-330ml",
            "nescafe-200gr",
            "tahil-cipsi-150gr",
            "cif-krem-temizleyici",
        };

        public static readonly HashSet<string> LegacyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Dana Kuşbaşı",
            "Kuzu İncik",
            "Sucuk 250gr",
            "Pınar Süt 1L",
            "Şek Kaşar Peyniri 200gr",
            "Domates Kg",
            "Salatalık Kg",
            "Bulgur 1 Kg",
            "Coca Cola 330ml",
            "Lipton Ice Tea 330ml",
            "Nescafe 200gr",
            "Tahıl Cipsi 150gr",
            "Cif Krem Temizleyici",
        };

        public static bool IsLegacy(string? sku) =>
            !string.IsNullOrWhiteSpace(sku) && All.Contains(sku.Trim());

        public static bool IsLegacyProduct(string? sku, string? slug, string? name) =>
            IsLegacy(sku)
            || (!string.IsNullOrWhiteSpace(slug) && LegacySlugs.Contains(slug.Trim()))
            || (!string.IsNullOrWhiteSpace(name) && LegacyNames.Contains(name.Trim()));
    }
}
