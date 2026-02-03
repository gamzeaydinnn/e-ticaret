using ECommerce.Core.DTOs.Micro;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces.Mapping
{
    /// <summary>
    /// Mikro ERP verilerini e-ticaret entity'lerine dönüştüren mapper interface'i.
    /// 
    /// NEDEN: Mikro API'den gelen veriler farklı formatta
    /// (Türkçe alan adları, farklı veri yapısı). Bu interface
    /// dönüşüm kurallarını standardize eder.
    /// 
    /// SOLID: Single Responsibility - sadece mapping işlemi
    /// </summary>
    public interface IMikroStokMapper
    {
        /// <summary>
        /// Mikro stok verisini e-ticaret Product entity'sine dönüştürür.
        /// 
        /// MAPPING:
        /// - sto_kod → SKU
        /// - sto_isim → Name
        /// - sto_miktar → StockQuantity
        /// - satis_fiyatlari[0] → Price
        /// - sto_birim1_ad → WeightUnit (mapping tablosundan)
        /// </summary>
        /// <param name="mikroStok">Mikro'dan gelen stok verisi</param>
        /// <param name="categoryMapping">Kategori eşleme bilgisi (opsiyonel)</param>
        /// <returns>E-ticaret Product entity'si</returns>
        Product MapToProduct(MikroStokResponseDto mikroStok, MikroCategoryMapping? categoryMapping = null);

        /// <summary>
        /// Mevcut ürünü Mikro verileriyle günceller.
        /// 
        /// NEDEN: Yeni ürün oluşturmak yerine mevcut ürünü
        /// güncellemek gerektiğinde kullanılır (delta sync).
        /// </summary>
        void UpdateProduct(Product existingProduct, MikroStokResponseDto mikroStok);

        /// <summary>
        /// E-ticaret ürününü Mikro stok formatına dönüştürür (ters yön).
        /// 
        /// KULLANIM: E-ticaret'te oluşturulan ürünü Mikro'ya
        /// göndermek gerektiğinde (nadir senaryo).
        /// </summary>
        MikroStokKaydetRequestDto MapToMikroStok(Product product);

        /// <summary>
        /// Mikro fiyat listesinden e-ticaret fiyatını çıkarır.
        /// 
        /// NEDEN: Mikro'da birden fazla fiyat listesi olabilir
        /// (toptan, perakende, vb.). Doğru fiyatı seçmek için.
        /// </summary>
        /// <param name="fiyatlar">Mikro fiyat listesi</param>
        /// <param name="fiyatNo">Hangi fiyat listesi (varsayılan: 1 = perakende)</param>
        /// <param name="kdvDahil">KDV dahil fiyat mı?</param>
        decimal ExtractPrice(IEnumerable<MikroStokFiyatResponseDto>? fiyatlar, int fiyatNo = 1, bool kdvDahil = true);
    }

    /// <summary>
    /// E-ticaret siparişlerini Mikro formatına dönüştüren mapper interface'i.
    /// 
    /// NEDEN: Online sipariş Mikro'ya aktarılırken format
    /// dönüşümü gerekir. Bu interface dönüşüm kurallarını tanımlar.
    /// 
    /// TEK YÖNLÜ: E-ticaret → Mikro (online siparişler Mikro'ya gider)
    /// </summary>
    public interface IMikroSiparisMapper
    {
        /// <summary>
        /// E-ticaret siparişini Mikro sipariş formatına dönüştürür.
        /// 
        /// MAPPING:
        /// - OrderNumber → sip_ozel_kod (referans)
        /// - OrderDate → sip_tarih
        /// - TotalPrice → sip_tutar
        /// - OrderItems → satirlar[]
        /// </summary>
        /// <param name="order">E-ticaret siparişi</param>
        /// <param name="items">Sipariş kalemleri</param>
        /// <param name="mikroCustomerCode">Müşterinin Mikro cari kodu</param>
        MikroSiparisKaydetRequestDto ToMikroSiparis(
            Order order, 
            IEnumerable<OrderItem> items,
            string mikroCustomerCode);
    }

    /// <summary>
    /// E-ticaret müşterilerini Mikro cari formatına dönüştüren mapper interface'i.
    /// 
    /// NEDEN: Online müşterilerin Mikro'da cari hesabı olması gerekir
    /// (muhasebe, faturalama için). Bu interface dönüşümü tanımlar.
    /// 
    /// TEK YÖNLÜ: E-ticaret → Mikro
    /// </summary>
    public interface IMikroCariMapper
    {
        /// <summary>
        /// E-ticaret kullanıcısını Mikro cari formatına dönüştürür.
        /// 
        /// MAPPING:
        /// - UserId → cari_kod (format: ONL-{UserId:D6})
        /// - FirstName + LastName → cari_unvan1
        /// - Email → cari_EMail
        /// - Phone → cari_CepTel
        /// </summary>
        MikroCariKaydetRequestDto ToMikroCari(User user, IEnumerable<Address>? addresses = null);

        /// <summary>
        /// Misafir müşteriyi Mikro cari formatına dönüştürür.
        /// 
        /// NEDEN: Misafir siparişlerde de Mikro'da cari kaydı gerekir.
        /// Cari kodu: MSF-{timestamp}
        /// </summary>
        MikroCariKaydetRequestDto ToMikroCariFromGuestOrder(Order order);
    }

    /// <summary>
    /// Kategori eşleme servis interface'i.
    /// 
    /// NEDEN: Mikro kategori kodları ile e-ticaret kategori ID'leri
    /// arasındaki eşlemeyi yönetir.
    /// </summary>
    public interface IMikroCategoryMappingService
    {
        /// <summary>
        /// Mikro grup kodlarına göre e-ticaret kategorisini bulur.
        /// 
        /// Öncelik sırası:
        /// 1. AnagrupKod + AltgrupKod eşleşmesi
        /// 2. Sadece AnagrupKod eşleşmesi
        /// 3. Varsayılan kategori
        /// </summary>
        Task<MikroCategoryMapping?> GetMappingAsync(
            string anagrupKod,
            string? altgrupKod = null,
            string? markaKod = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// E-ticaret kategorisine göre Mikro kodlarını bulur (ters eşleme).
        /// </summary>
        Task<(string AnagrupKod, string? AltgrupKod)?> GetMikroKodlarAsync(
            int categoryId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Yeni eşleme ekler.
        /// </summary>
        Task<MikroCategoryMapping> AddMappingAsync(
            MikroCategoryMapping mapping,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tüm eşlemeleri listeler.
        /// </summary>
        Task<IEnumerable<MikroCategoryMapping>> GetAllMappingsAsync(
            CancellationToken cancellationToken = default);
    }
}
