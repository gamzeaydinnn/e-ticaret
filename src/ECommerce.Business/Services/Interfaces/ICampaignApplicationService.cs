using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Kampanya Uygulama Motoru - Ürünlere otomatik indirim uygulama servisi.
    /// Kampanya oluşturulduğunda veya güncellendiğinde ürünlerin SpecialPrice'ını hesaplar.
    /// 
    /// Desteklenen kampanya türleri:
    /// - Percentage: Yüzdelik indirim (SpecialPrice = Price * (1 - DiscountValue/100))
    /// - FixedAmount: Sabit tutar indirim (SpecialPrice = Price - DiscountValue)
    /// - BuyXPayY: X al Y öde (SpecialPrice hesaplanmaz, sepette uygulanır)
    /// - FreeShipping: Ücretsiz kargo (SpecialPrice hesaplanmaz)
    /// 
    /// Birden fazla kampanya aynı ürüne uygunsa, EN YÜKSEK İNDİRİM uygulanır.
    /// </summary>
    public interface ICampaignApplicationService
    {
        /// <summary>
        /// Belirli bir kampanyanın indirimlerini hedef ürünlere uygular.
        /// Kampanya oluşturma veya güncelleme sonrası çağrılır.
        /// </summary>
        /// <param name="campaignId">Kampanya ID</param>
        /// <returns>Güncellenen ürün sayısı</returns>
        Task<int> ApplyCampaignToProductsAsync(int campaignId);

        /// <summary>
        /// Kampanya silindiğinde veya pasif yapıldığında ürünlerin SpecialPrice'ını yeniden hesaplar.
        /// Diğer aktif kampanyalar varsa en iyi indirimi uygular, yoksa null yapar.
        /// </summary>
        /// <param name="campaignId">Silinen/pasif yapılan kampanya ID</param>
        /// <returns>Güncellenen ürün sayısı</returns>
        Task<int> RemoveCampaignFromProductsAsync(int campaignId);

        /// <summary>
        /// Tüm aktif kampanyaları yeniden hesaplar ve ürünlere uygular.
        /// Toplu güncelleme veya bakım işlemleri için kullanılır.
        /// </summary>
        /// <returns>Güncellenen ürün sayısı</returns>
        Task<int> RecalculateAllCampaignsAsync();

        /// <summary>
        /// Belirli bir ürün için en iyi kampanya indirimini hesaplar ve uygular.
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <returns>Uygulanan kampanya (varsa)</returns>
        Task<Campaign?> ApplyBestCampaignToProductAsync(int productId);

        /// <summary>
        /// Belirli bir kategorideki tüm ürünlerin kampanya indirimlerini yeniden hesaplar.
        /// Kategori bazlı kampanya değişikliklerinde kullanılır.
        /// </summary>
        /// <param name="categoryId">Kategori ID</param>
        /// <returns>Güncellenen ürün sayısı</returns>
        Task<int> RecalculateCategoryProductsAsync(int categoryId);

        /// <summary>
        /// Bir ürün için geçerli kampanyalardan en iyi indirimi hesaplar (uygulamaz).
        /// Ürün kartında "İndirimli fiyat" göstermek için kullanılabilir.
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <param name="categoryId">Kategori ID</param>
        /// <param name="price">Ürün fiyatı</param>
        /// <returns>İndirimli fiyat ve uygulanan kampanya bilgisi</returns>
        Task<(decimal? SpecialPrice, Campaign? AppliedCampaign)> CalculateBestPriceAsync(int productId, int categoryId, decimal price);
    }
}
