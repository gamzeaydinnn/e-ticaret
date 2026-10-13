using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// Mikro ERP HotPoll delta sorgusu sonucu.
    /// Değişen ürün listesini ve sorgunun çalıştırıldığı andaki SQL Server saatini (watermark) taşır.
    /// Bu sayede web sunucusu ile SQL sunucusu arasındaki saat dilimi ve saat kaymaları (skew) önlenir.
    /// </summary>
    public class MikroDeltaResponse
    {
        /// <summary>
        /// Son sorgudan itibaren değişen ürünlerin listesi.
        /// </summary>
        public List<MikroUnifiedProductDto> Products { get; set; } = new();

        /// <summary>
        /// Sorgunun Mikro SQL sunucusunda yürütüldüğü andaki DB saati (GETDATE()).
        /// Bir sonraki delta polling için 'since' başlangıç noktası olarak kullanılır.
        /// </summary>
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;
    }
}
