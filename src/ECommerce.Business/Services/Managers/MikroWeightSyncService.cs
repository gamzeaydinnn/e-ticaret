// ==========================================================================
// MikroWeightSyncService.cs - Mikro ERP Tartı Senkronizasyon Servisi
// ==========================================================================
// Mikro ERP'den sipariş teslim miktarlarını (tartı sonuçları) çekip
// OrderItem entity'lerini güncelleyen köprü servis.
//
// AKIŞ:
// 1. Sipariş ve OrderItem'lar DB'den yüklenir
// 2. IMicroService.GetOrderDeliveryWeightsAsync ile Mikro'dan tartı verileri çekilir
// 3. SKU eşleştirmesiyle OrderItem.ActualWeight güncellenir
// 4. Fark hesaplanır ve DB'ye yazılır
//
// NEDEN: Mağaza personeli ürünleri tartıp Mikro'ya girdiğinde,
// gerçek ağırlık bilgileri bu servis aracılığıyla e-ticaret sistemine aktarılır.
// Böylece provizyon capture sırasında doğru tutar çekilir.
// ==========================================================================

using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Mikro ERP'den sipariş teslim miktarlarını çekip OrderItem'lara senkronize eder.
    /// </summary>
    public class MikroWeightSyncService : IMikroWeightSyncService
    {
        private readonly ECommerceDbContext _context;
        private readonly IMicroService _microService;
        private readonly ILogger<MikroWeightSyncService> _logger;

        public MikroWeightSyncService(
            ECommerceDbContext context,
            IMicroService microService,
            ILogger<MikroWeightSyncService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _microService = microService ?? throw new ArgumentNullException(nameof(microService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<bool> SyncDeliveryWeightsForOrderAsync(int orderId, CancellationToken ct = default)
        {
            try
            {
                // 1. Sipariş ve kalemlerini yükle (Product dahil - SKU eşleştirme için)
                var order = await _context.Orders
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId, ct);

                if (order == null)
                {
                    _logger.LogWarning("[MikroWeightSync] Sipariş bulunamadı. OrderId={OrderId}", orderId);
                    return false;
                }

                // 2. Ağırlık bazlı ürün yoksa sync'e gerek yok
                var weightBasedItems = order.OrderItems?
                    .Where(oi => oi.IsWeightBased)
                    .ToList();

                if (weightBasedItems == null || weightBasedItems.Count == 0)
                {
                    _logger.LogDebug("[MikroWeightSync] Siparişte ağırlık bazlı ürün yok. OrderId={OrderId}", orderId);
                    return false;
                }

                // 3. Mikro'dan teslim miktarlarını çek
                var mikroResult = await _microService.GetOrderDeliveryWeightsAsync(
                    order.OrderNumber ?? string.Empty, ct);

                if (mikroResult == null || !mikroResult.Success || mikroResult.Items.Count == 0)
                {
                    _logger.LogWarning(
                        "[MikroWeightSync] Mikro'dan ağırlık verisi alınamadı. OrderId={OrderId}, Error={Error}",
                        orderId, mikroResult?.ErrorMessage ?? "Boş yanıt");
                    return false;
                }

                _logger.LogInformation(
                    "[MikroWeightSync] Mikro'dan {ItemCount} satır teslim verisi alındı. OrderId={OrderId}",
                    mikroResult.Items.Count, orderId);

                // 4. SKU eşleştirmesi ile güncelleme
                int updatedCount = 0;
                decimal totalWeightDifference = 0m;
                decimal totalPriceDifference = 0m;

                foreach (var orderItem in weightBasedItems)
                {
                    // SKU eşleştirme: VariantSku > Product.SKU
                    var sku = orderItem.VariantSku ?? orderItem.Product?.SKU;
                    if (string.IsNullOrEmpty(sku))
                    {
                        _logger.LogDebug(
                            "[MikroWeightSync] OrderItem {ItemId} için SKU bulunamadı, atlanıyor",
                            orderItem.Id);
                        continue;
                    }

                    // Mikro verisinde eşleşen satırı bul (case-insensitive)
                    var mikroItem = mikroResult.Items.FirstOrDefault(
                        m => string.Equals(m.StokKod?.Trim(), sku.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (mikroItem == null)
                    {
                        _logger.LogDebug(
                            "[MikroWeightSync] SKU={SKU} Mikro verisinde bulunamadı, atlanıyor", sku);
                        continue;
                    }

                    // Mikro'dan gelen teslim miktarı 0 ise henüz tartılmamış demektir
                    if (mikroItem.TeslimMiktar <= 0)
                    {
                        _logger.LogDebug(
                            "[MikroWeightSync] SKU={SKU} için teslim miktarı 0, henüz tartılmamış", sku);
                        continue;
                    }

                    // 5. KG → Gram dönüşümü ve güncelleme
                    // NEDEN: Mikro'da birim KG olarak gelir, OrderItem gram cinsinden saklar
                    decimal actualWeightGrams = ConvertToGrams(mikroItem.TeslimMiktar, orderItem.WeightUnit);

                    // Daha önce bu veriye sahipse ve değer değişmediyse atla
                    if (orderItem.IsWeighed && orderItem.ActualWeight.HasValue &&
                        Math.Abs(orderItem.ActualWeight.Value - actualWeightGrams) < 0.01m)
                    {
                        continue;
                    }

                    // OrderItem güncelle
                    orderItem.ActualWeight = actualWeightGrams;
                    orderItem.WeightDifference = actualWeightGrams - orderItem.EstimatedWeight;
                    orderItem.IsWeighed = true;
                    orderItem.WeighedAt = DateTime.UtcNow;

                    // Gerçek fiyat hesapla: ActualWeight (gram) → KG → * PricePerUnit
                    // NEDEN: PricePerUnit KG fiyatıdır, gram'ı KG'a çevirip çarpıyoruz
                    if (orderItem.PricePerUnit > 0)
                    {
                        decimal actualWeightKg = actualWeightGrams / 1000m;
                        orderItem.ActualPrice = Math.Round(actualWeightKg * orderItem.PricePerUnit, 2);
                        orderItem.PriceDifference = orderItem.ActualPrice.Value - orderItem.EstimatedPrice;
                    }

                    // Toplam farkları biriktir
                    totalWeightDifference += orderItem.WeightDifference ?? 0m;
                    totalPriceDifference += orderItem.PriceDifference ?? 0m;

                    updatedCount++;

                    _logger.LogInformation(
                        "[MikroWeightSync] OrderItem güncellendi. ItemId={ItemId}, SKU={SKU}, " +
                        "Tahmini={EstimatedGr}g, Gerçek={ActualGr}g, Fark={DiffGr}g, " +
                        "FiyatFarkı={PriceDiff:N2} TL",
                        orderItem.Id, sku,
                        orderItem.EstimatedWeight, actualWeightGrams,
                        orderItem.WeightDifference, orderItem.PriceDifference);
                }

                // 6. Sipariş seviyesinde toplam farkları güncelle
                if (updatedCount > 0)
                {
                    order.TotalWeightDifference = totalWeightDifference;
                    order.TotalPriceDifference = totalPriceDifference;
                    order.HasWeightBasedItems = true;

                    await _context.SaveChangesAsync(ct);

                    _logger.LogInformation(
                        "[MikroWeightSync] Senkronizasyon tamamlandı. OrderId={OrderId}, " +
                        "Güncellenen={UpdatedCount}/{TotalCount}, " +
                        "ToplamAğırlıkFarkı={WeightDiff}g, ToplamFiyatFarkı={PriceDiff:N2} TL",
                        orderId, updatedCount, weightBasedItems.Count,
                        totalWeightDifference, totalPriceDifference);
                }
                else
                {
                    _logger.LogInformation(
                        "[MikroWeightSync] Güncellenecek veri bulunamadı. OrderId={OrderId}", orderId);
                }

                return updatedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[MikroWeightSync] Ağırlık senkronizasyonu hatası. OrderId={OrderId}", orderId);
                return false;
            }
        }

        #region Yardımcı Metotlar

        /// <summary>
        /// Mikro'dan gelen miktarı gram'a dönüştürür.
        /// NEDEN: Mikro birim bazlı (KG, GR, LT, ML) gönderir,
        /// OrderItem her zaman gram cinsinden saklar.
        /// </summary>
        private static decimal ConvertToGrams(decimal miktar, WeightUnit unit)
        {
            return unit switch
            {
                WeightUnit.Kilogram => miktar * 1000m,     // 1.1 KG → 1100 gram
                WeightUnit.Gram => miktar,                  // 500 GR → 500 gram
                WeightUnit.Liter => miktar * 1000m,         // 1 LT ≈ 1000 gram (su bazlı)
                WeightUnit.Milliliter => miktar,            // 500 ML ≈ 500 gram
                _ => miktar * 1000m                         // Varsayılan: KG kabul et
            };
        }

        #endregion
    }
}
