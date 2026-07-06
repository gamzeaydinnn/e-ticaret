using System;
using System.Collections.Generic;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// ProductSeeder ile eklenen demo ürün SKU'ları — Mikro ERP kaynaklı değildir.
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

        public static bool IsLegacy(string? sku) =>
            !string.IsNullOrWhiteSpace(sku) && All.Contains(sku.Trim());
    }

}
