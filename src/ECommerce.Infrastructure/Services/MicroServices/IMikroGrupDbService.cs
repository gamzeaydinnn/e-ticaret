using ECommerce.Core.DTOs.Micro;

namespace ECommerce.Infrastructure.Services.MicroServices
{
    /// <summary>
    /// Mikro stok grup tablolarını (STOK_ANA_GRUPLARI / STOK_ALT_GRUPLARI) okuyan ve
    /// yalnızca Admin yeni kategori/alt kategori eklediğinde Mikro'ya INSERT yapan servis sözleşmesi.
    /// </summary>
    public interface IMikroGrupDbService
    {
        /// <summary>Mikro SQL bağlantı string'i yapılandırılmış mı?</summary>
        bool IsConfigured { get; }

        /// <summary>STOK_ANA_GRUPLARI satırlarını okur (iptal edilmişler hariç).</summary>
        Task<MikroGrupReadResult<MikroAnaGrupDto>> GetAnaGruplariAsync(CancellationToken cancellationToken = default);

        /// <summary>STOK_ALT_GRUPLARI satırlarını okur (iptal edilmişler hariç).</summary>
        Task<MikroGrupReadResult<MikroAltGrupDto>> GetAltGruplariAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Web'e açık ürünlerin (sto_webe_gonderilecek_fl = 1, sto_iptal = 0) ana + alt grup bazında sayısı.
        /// Ana grup kodları normalleştirilir (tek haneli "2" → "02"). Yalnızca admin ekranı içindir.
        /// </summary>
        Task<MikroGrupReadResult<MikroGrupUrunSayisiDto>> GetWebUrunSayilariAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// YALNIZCA Admin panelinden yeni bir ANA KATEGORİ eklendiğinde çağrılır.
        /// STOK_ANA_GRUPLARI tablosuna yeni ana grup (örn: 2300) INSERT eder.
        /// </summary>
        Task<(bool Success, string? Kod, string? Error)> CreateAnaGrupInMikroAsync(
            string isim,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// YALNIZCA Admin panelinden yeni bir ALT KATEGORİ eklendiğinde çağrılır.
        /// STOK_ALT_GRUPLARI tablosuna belirtilen ana grup altında yeni alt grup (örn: 1411) INSERT eder.
        /// </summary>
        Task<(bool Success, string? Kod, string? Error)> CreateAltGrupInMikroAsync(
            string anaGrupKod,
            string isim,
            CancellationToken cancellationToken = default);
    }
}
