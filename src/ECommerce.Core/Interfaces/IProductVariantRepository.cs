// IProductVariantRepository: Ürün varyantları için repository arayüzü.
// SKU bazlı sorgular, toplu işlemler ve XML entegrasyonu için özel metodlar içerir.
// Bu arayüz, XML import sisteminin temelini oluşturur.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Ürün varyantları için repository arayüzü.
    /// SKU benzersizliği ve toplu işlemler için optimize edilmiş metodlar sunar.
    /// </summary>
    public interface IProductVariantRepository : IRepository<ProductVariant>
    {
        #region Tekil Sorgular

        /// <summary>
        /// SKU ile varyant getirir.
        /// XML import'ta mevcut varyant kontrolü için kritik.
        /// SKU benzersiz olduğu için tek sonuç döner.
        /// </summary>
        /// <param name="sku">Stok Tutma Birimi (benzersiz)</param>
        /// <returns>Varyant veya null</returns>
        Task<ProductVariant?> GetBySkuAsync(string sku);

        /// <summary>
        /// Barkod ile varyant getirir.
        /// Barkod okuyucu entegrasyonu için.
        /// </summary>
        /// <param name="barcode">Barkod numarası</param>
        /// <returns>Varyant veya null</returns>
        Task<ProductVariant?> GetByBarcodeAsync(string barcode);

        /// <summary>
        /// Varyantı option değerleri ile birlikte getirir.
        /// Detay sayfası için.
        /// </summary>
        /// <param name="id">Varyant ID</param>
        /// <returns>Varyant veya null (OptionValues dahil)</returns>
        Task<ProductVariant?> GetByIdWithOptionsAsync(int id);

        #endregion

        #region Liste Sorguları

        /// <summary>
        /// Bir ürüne ait tüm varyantları getirir.
        /// Ürün detay sayfasında varyant listesi için.
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <param name="includeInactive">Pasif varyantları dahil et</param>
        /// <returns>Varyant listesi</returns>
        Task<IEnumerable<ProductVariant>> GetByProductIdAsync(int productId, bool includeInactive = false);

        /// <summary>
        /// ParentSku ile gruplu varyantları getirir.
        /// Aynı ürün ailesindeki tüm varyantları bulmak için.
        /// </summary>
        /// <param name="parentSku">Ana ürün grubu SKU'su</param>
        /// <returns>Aynı gruba ait varyantlar</returns>
        Task<IEnumerable<ProductVariant>> GetByParentSkuAsync(string parentSku);

        /// <summary>
        /// Tedarikçi koduna göre varyantları getirir.
        /// Tedarikçi bazlı raporlama için.
        /// </summary>
        /// <param name="supplierCode">Tedarikçi kodu</param>
        /// <returns>Varyant listesi</returns>
        Task<IEnumerable<ProductVariant>> GetBySupplierCodeAsync(string supplierCode);

        /// <summary>
        /// Belirli bir tarihten önce görülmemiş varyantları getirir.
        /// Pasifleştirme işlemi için - feed'de görünmeyen ürünleri bulmak.
        /// </summary>
        /// <param name="date">Kesme tarihi</param>
        /// <returns>Bu tarihten önce son görülen varyantlar</returns>
        Task<IEnumerable<ProductVariant>> GetVariantsNotSeenSinceAsync(DateTime date);

        /// <summary>
        /// Stok durumuna göre varyantları getirir.
        /// Stok uyarıları ve raporlama için.
        /// </summary>
        /// <param name="maxStock">Maksimum stok eşiği (altındakiler)</param>
        /// <returns>Düşük stoklu varyantlar</returns>
        Task<IEnumerable<ProductVariant>> GetLowStockVariantsAsync(int maxStock = 5);

        #endregion

        #region Toplu İşlemler

        /// <summary>
        /// Birden fazla SKU ile varyantları getirir.
        /// XML import'ta mevcut kontrol için toplu sorgu.
        /// </summary>
        /// <param name="skus">SKU listesi</param>
        /// <returns>Bulunan varyantlar (SKU -> Variant dictionary)</returns>
        Task<Dictionary<string, ProductVariant>> GetBySkusAsync(IEnumerable<string> skus);

        /// <summary>
        /// Toplu varyant ekleme/güncelleme (UPSERT).
        /// XML import'un ana metodu - SKU'ya göre upsert yapar.
        /// Performans için batch işlem kullanır.
        /// </summary>
        /// <param name="variants">Eklenecek/güncellenecek varyantlar</param>
        /// <returns>İşlem sonucu (eklenen, güncellenen, hatalı sayıları)</returns>
        Task<BulkUpsertResult> BulkUpsertAsync(IEnumerable<ProductVariant> variants);

        /// <summary>
        /// Toplu stok güncelleme.
        /// XML'den sadece stok senkronizasyonu için optimize edilmiş.
        /// </summary>
        /// <param name="stockUpdates">SKU -> Yeni Stok değerleri</param>
        /// <returns>Güncellenen kayıt sayısı</returns>
        Task<int> BulkUpdateStockAsync(Dictionary<string, int> stockUpdates);

        /// <summary>
        /// Toplu pasifleştirme.
        /// Feed'de görünmeyen ürünleri pasifleştirmek için.
        /// </summary>
        /// <param name="variantIds">Pasifleştirilecek varyant ID'leri</param>
        /// <returns>Pasifleştirilen kayıt sayısı</returns>
        Task<int> BulkDeactivateAsync(IEnumerable<int> variantIds);

        #endregion

        #region Senkronizasyon

        /// <summary>
        /// Varyantın LastSeenAt tarihini günceller.
        /// XML import sırasında varyantı "görüldü" olarak işaretler.
        /// </summary>
        /// <param name="variantId">Varyant ID</param>
        /// <param name="seenAt">Görülme tarihi (null ise UtcNow)</param>
        Task MarkAsSeenAsync(int variantId, DateTime? seenAt = null);

        /// <summary>
        /// Birden fazla varyantı "görüldü" olarak işaretler.
        /// Toplu import sonrası.
        /// </summary>
        /// <param name="variantIds">Varyant ID'leri</param>
        /// <param name="seenAt">Görülme tarihi</param>
        Task BulkMarkAsSeenAsync(IEnumerable<int> variantIds, DateTime? seenAt = null);

        #endregion

        #region İstatistikler

        /// <summary>
        /// Ürüne ait varyant sayısını döndürür.
        /// </summary>
        Task<int> GetCountByProductIdAsync(int productId);

        /// <summary>
        /// Ürüne ait toplam stoğu hesaplar.
        /// Tüm aktif varyantların stok toplamı.
        /// </summary>
        Task<int> GetTotalStockByProductIdAsync(int productId);

        /// <summary>
        /// SKU'nun mevcut olup olmadığını kontrol eder.
        /// Yeni varyant oluşturmadan önce benzersizlik kontrolü.
        /// </summary>
        /// <param name="sku">Kontrol edilecek SKU</param>
        /// <param name="excludeVariantId">Hariç tutulacak varyant ID (güncelleme için)</param>
        Task<bool> SkuExistsAsync(string sku, int? excludeVariantId = null);

        #endregion
    }

    /// <summary>
    /// Toplu ekleme/güncelleme işlemi sonucu.
    /// </summary>
    public class BulkUpsertResult
    {
        /// <summary>
        /// Yeni eklenen kayıt sayısı
        /// </summary>
        public int InsertedCount { get; set; }

        /// <summary>
        /// Güncellenen kayıt sayısı
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Hatalı kayıt sayısı
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Değişiklik olmayan kayıt sayısı
        /// </summary>
        public int UnchangedCount { get; set; }

        /// <summary>
        /// Toplam işlenen kayıt sayısı
        /// </summary>
        public int TotalProcessed => InsertedCount + UpdatedCount + FailedCount + UnchangedCount;

        /// <summary>
        /// Hata mesajları (SKU -> Hata)
        /// </summary>
        public Dictionary<string, string> Errors { get; set; } = new();

        /// <summary>
        /// İşlem başarılı mı? (Hiç hata yok)
        /// </summary>
        public bool IsSuccess => FailedCount == 0;
    }
}
