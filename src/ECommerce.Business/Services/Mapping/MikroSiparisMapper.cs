using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Mapping
{
    /// <summary>
    /// E-ticaret Sipariş → Mikro ERP Sipariş dönüşümü.
    /// 
    /// GÖREV: Onaylanan e-ticaret siparişlerini Mikro ERP formatına çevirir.
    /// Stok düşümü ve faturalama için Mikro'ya aktarılır.
    /// 
    /// MAPPING:
    /// Order.OrderNumber → sip_ozel_kod (çift yönlü eşleştirme için)
    /// OrderItem.VariantSku → sip_stok_kod
    /// Order.CustomerName → teslimat_adresi.adres_alici
    /// Order.TotalPrice → sip_tutar
    /// Order.VatAmount → sip_vergi
    /// Order.FinalPrice → sip_genel_toplam
    /// </summary>
    public class MikroSiparisMapper : IMikroSiparisMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MikroSiparisMapper> _logger;

        /// <summary>
        /// Mikro ayarları cache - her seferinde config okumamak için.
        /// </summary>
        private readonly MikroSiparisSettings _settings;

        public MikroSiparisMapper(
            IConfiguration configuration,
            ILogger<MikroSiparisMapper> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _settings = LoadSettings();
        }

        /// <summary>
        /// E-ticaret siparişini Mikro ERP sipariş formatına dönüştürür.
        /// </summary>
        /// <param name="order">E-ticaret sipariş entity'si</param>
        /// <param name="items">Sipariş kalemleri listesi</param>
        /// <param name="mikroCustomerCode">Mikro'daki cari kodu</param>
        /// <returns>Mikro ERP sipariş DTO</returns>
        public MikroSiparisKaydetRequestDto ToMikroSiparis(
            Order order, 
            IEnumerable<OrderItem> items,
            string mikroCustomerCode)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order), "Sipariş null olamaz");

            if (string.IsNullOrEmpty(mikroCustomerCode))
                throw new ArgumentException("Mikro cari kodu zorunlu", nameof(mikroCustomerCode));

            var itemsList = items?.ToList() ?? new List<OrderItem>();

            _logger.LogDebug("Sipariş {OrderNumber} Mikro formatına dönüştürülüyor. " +
                           "Kalem sayısı: {ItemCount}", order.OrderNumber, itemsList.Count);

            try
            {
                var dto = new MikroSiparisKaydetRequestDto
                {
                    // Evrak Bilgileri
                    SipEvraknoSeri = _settings.EvrakSeri,
                    SipEvraknoSira = null, // Mikro otomatik numara verecek
                    SipTarih = FormatDate(order.OrderDate),
                    SipTeslimTarih = order.EstimatedDeliveryDate.HasValue
                        ? FormatDate(order.EstimatedDeliveryDate.Value)
                        : FormatDate(order.OrderDate.AddDays(_settings.DefaultDeliveryDays)),

                    // Müşteri Bilgileri
                    SipMusteriKod = mikroCustomerCode,

                    // Sipariş Tipi
                    SipTip = 0, // 0 = Satış Siparişi
                    SipCins = GetMikroSiparisCins(order),

                    // Depo / Şube
                    SipDepoNo = _settings.DefaultDepoNo,
                    SipSube = _settings.DefaultSubeNo,

                    // Açıklamalar
                    SipAciklama = BuildSiparisAciklama(order),

                    // Durum
                    SipDurum = GetMikroSiparisDurum(order),

                    // Ödeme
                    SipOdemeKod = GetMikroOdemeKod(order),

                    // Döviz
                    SipDovizCinsi = GetMikroDovizCinsi(order.Currency),
                    SipDovizKuru = order.Currency == "TRY" ? null : 1.0m,

                    // Tutarlar
                    SipTutar = CalculateNetAmount(order),
                    SipVergi = order.VatAmount,
                    SipIskonto = order.DiscountAmount + order.CouponDiscountAmount + order.CampaignDiscountAmount,
                    SipGenelToplam = order.FinalPrice,
                    SipKargoTutar = order.ShippingCost,

                    // E-ticaret Referans
                    SipOzelKod = order.OrderNumber,

                    // Satırlar
                    Satirlar = MapOrderItems(itemsList, order),

                    // Teslimat Adresi
                    TeslimatAdresi = MapTeslimatAdresi(order)
                };

                _logger.LogInformation(
                    "Sipariş {OrderNumber} başarıyla Mikro formatına dönüştürüldü. " +
                    "Toplam: {Total:N2} {Currency}, Satır: {LineCount}",
                    order.OrderNumber, order.FinalPrice, order.Currency, dto.Satirlar.Count);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Sipariş {OrderNumber} Mikro formatına dönüştürülürken hata",
                    order.OrderNumber);
                throw;
            }
        }

        #region Private Mapping Methods

        /// <summary>
        /// Sipariş kalemlerini Mikro satırlarına dönüştürür.
        /// </summary>
        private List<MikroSiparisSatirDto> MapOrderItems(List<OrderItem> items, Order order)
        {
            var satirlar = new List<MikroSiparisSatirDto>();
            int satirNo = 1;

            foreach (var item in items)
            {
                var satir = MapOrderItem(item, satirNo, order);
                if (satir != null)
                {
                    satirlar.Add(satir);
                    satirNo++;
                }
            }

            // Kargo satırı ekle (eğer varsa ve ayrı kalem olarak gösterilecekse)
            if (order.ShippingCost > 0 && _settings.AddShippingAsLine)
            {
                satirlar.Add(CreateKargoSatiri(order.ShippingCost, satirNo));
            }

            return satirlar;
        }

        /// <summary>
        /// Tek bir sipariş kalemini Mikro satırına dönüştürür.
        /// </summary>
        private MikroSiparisSatirDto? MapOrderItem(OrderItem item, int satirNo, Order order)
        {
            // SKU kontrolü - SKU olmadan Mikro'ya gönderemeyiz
            var stokKod = GetStokKod(item);
            if (string.IsNullOrEmpty(stokKod))
            {
                _logger.LogWarning(
                    "OrderItem {ItemId} için SKU bulunamadı, satır atlanıyor",
                    item.Id);
                return null;
            }

            // Miktar hesaplama - ağırlık bazlı ürünler için farklı
            decimal miktar = CalculateMiktar(item);
            decimal birimFiyat = CalculateBirimFiyat(item);
            decimal satirTutar = miktar * birimFiyat;

            return new MikroSiparisSatirDto
            {
                SipSatirNo = satirNo,
                SipStokKod = stokKod,
                SipStokIsim = item.VariantTitle ?? item.Product?.Name,
                SipMiktar = miktar,
                SipTeslimMiktar = 0, // Henüz teslim edilmedi
                SipBFiyat = birimFiyat,
                SipVergiPuan = _settings.DefaultKdvOran,
                SipIskonto1 = null, // Satır bazlı iskonto varsa buraya
                SipIskonto2 = null,
                SipTutar = satirTutar,
                SipDepoNo = _settings.DefaultDepoNo,
                SipBirim = GetBirimAdi(item),
                SipAciklama = BuildSatirAciklama(item),
                SipOzelKod = item.Id.ToString() // E-ticaret kalem ID'si
            };
        }

        /// <summary>
        /// Kargo ücreti için ayrı satır oluşturur.
        /// </summary>
        private MikroSiparisSatirDto CreateKargoSatiri(decimal kargoTutar, int satirNo)
        {
            return new MikroSiparisSatirDto
            {
                SipSatirNo = satirNo,
                SipStokKod = _settings.KargoStokKod,
                SipStokIsim = "Kargo Ücreti",
                SipMiktar = 1,
                SipBFiyat = kargoTutar,
                SipVergiPuan = _settings.KargoKdvOran,
                SipTutar = kargoTutar,
                SipBirim = "ADET",
                SipAciklama = "E-ticaret kargo ücreti"
            };
        }

        /// <summary>
        /// Teslimat adresi bilgilerini map'ler.
        /// </summary>
        private MikroSiparisTeslimatAdresiDto? MapTeslimatAdresi(Order order)
        {
            // Adres bilgisi yoksa null döndür
            if (string.IsNullOrEmpty(order.ShippingAddress) && 
                string.IsNullOrEmpty(order.CustomerName))
            {
                return null;
            }

            return new MikroSiparisTeslimatAdresiDto
            {
                AdresBaslik = "Teslimat Adresi",
                AdresAlici = order.CustomerName ?? "Belirtilmemiş",
                AdresCadde = order.ShippingAddress ?? string.Empty,
                AdresIl = order.ShippingCity ?? string.Empty,
                AdresTelefon = order.CustomerPhone
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Sipariş kaleminden SKU alır.
        /// Öncelik: VariantSku > Product.SKU
        /// </summary>
        private string? GetStokKod(OrderItem item)
        {
            // Varyant SKU'su öncelikli
            if (!string.IsNullOrEmpty(item.VariantSku))
                return item.VariantSku;

            // Product SKU
            if (item.Product != null && !string.IsNullOrEmpty(item.Product.SKU))
                return item.Product.SKU;

            return null;
        }

        /// <summary>
        /// Sipariş kalemi miktarını hesaplar.
        /// Ağırlık bazlı ürünler için ActualWeight veya EstimatedWeight kullanır.
        /// </summary>
        private decimal CalculateMiktar(OrderItem item)
        {
            if (item.IsWeightBased)
            {
                // Ağırlık bazlı: Gram → KG dönüşümü
                var weight = item.ActualWeight ?? item.EstimatedWeight;
                return weight / 1000m; // Gram → KG
            }

            // Adet bazlı
            return item.Quantity;
        }

        /// <summary>
        /// Birim fiyatı hesaplar.
        /// Ağırlık bazlı ürünlerde kg fiyatı döndürür.
        /// </summary>
        private decimal CalculateBirimFiyat(OrderItem item)
        {
            if (item.IsWeightBased)
            {
                // KG fiyatı (PricePerUnit zaten kg fiyatı olmalı)
                return item.PricePerUnit;
            }

            // Adet fiyatı
            return item.UnitPrice;
        }

        /// <summary>
        /// Birim adını döndürür.
        /// </summary>
        private string GetBirimAdi(OrderItem item)
        {
            if (item.IsWeightBased)
            {
                return item.WeightUnit switch
                {
                    WeightUnit.Kilogram => "KG",
                    WeightUnit.Gram => "GR",
                    WeightUnit.Liter => "LT",
                    WeightUnit.Milliliter => "ML",
                    _ => "KG"
                };
            }

            return "ADET";
        }

        /// <summary>
        /// Sipariş durumuna göre Mikro sipariş cinsi döndürür.
        /// </summary>
        private int GetMikroSiparisCins(Order order)
        {
            // 0 = Normal, 1 = İade, 2 = Teklif
            return order.Status == OrderStatus.Refunded ? 1 : 0;
        }

        /// <summary>
        /// Sipariş durumuna göre Mikro sipariş durumu döndürür.
        /// </summary>
        private int GetMikroSiparisDurum(Order order)
        {
            return order.Status switch
            {
                OrderStatus.Pending => 0,      // Açık
                OrderStatus.Confirmed => 0,    // Açık
                OrderStatus.Processing => 0,   // Açık
                OrderStatus.Shipped => 1,      // Kısmi Teslim
                OrderStatus.Delivered => 2,    // Kapalı
                OrderStatus.Cancelled => 3,    // İptal
                _ => 0
            };
        }

        /// <summary>
        /// Ödeme durumuna göre Mikro ödeme kodu döndürür.
        /// </summary>
        private string GetMikroOdemeKod(Order order)
        {
            return order.PaymentStatus switch
            {
                PaymentStatus.Paid => "KK",      // Kredi Kartı
                PaymentStatus.Pending => "KOD",  // Kapıda Ödeme
                PaymentStatus.Failed => "KOD",   // Kapıda Ödeme
                PaymentStatus.Refunded => "IAD", // İade
                _ => "KK"
            };
        }

        /// <summary>
        /// Para birimine göre Mikro döviz cinsi döndürür.
        /// </summary>
        private int GetMikroDovizCinsi(string currency)
        {
            return currency?.ToUpperInvariant() switch
            {
                "TRY" or "TL" => 0,
                "USD" => 1,
                "EUR" => 2,
                "GBP" => 3,
                _ => 0
            };
        }

        /// <summary>
        /// Sipariş açıklaması oluşturur.
        /// </summary>
        private string BuildSiparisAciklama(Order order)
        {
            var parts = new List<string>
            {
                $"E-ticaret Sipariş: {order.OrderNumber}"
            };

            if (!string.IsNullOrEmpty(order.DeliveryNotes))
            {
                parts.Add($"Teslimat Notu: {order.DeliveryNotes}");
            }

            if (!string.IsNullOrEmpty(order.AppliedCouponCode))
            {
                parts.Add($"Kupon: {order.AppliedCouponCode}");
            }

            if (order.Priority != "normal")
            {
                parts.Add($"Öncelik: {order.Priority.ToUpperInvariant()}");
            }

            return string.Join(" | ", parts);
        }

        /// <summary>
        /// Satır açıklaması oluşturur.
        /// </summary>
        private string? BuildSatirAciklama(OrderItem item)
        {
            var parts = new List<string>();

            // Varyant bilgisi
            if (!string.IsNullOrEmpty(item.VariantTitle) && 
                item.VariantTitle != item.Product?.Name)
            {
                parts.Add($"Varyant: {item.VariantTitle}");
            }

            // Ağırlık bilgisi
            if (item.IsWeightBased)
            {
                if (item.ActualWeight.HasValue)
                {
                    parts.Add($"Gerçek: {item.ActualWeight.Value:N0}g");
                }
                else
                {
                    parts.Add($"Tahmini: {item.EstimatedWeight:N0}g");
                }
            }

            return parts.Count > 0 ? string.Join(" | ", parts) : null;
        }

        /// <summary>
        /// Net tutarı hesaplar (KDV hariç).
        /// </summary>
        private decimal CalculateNetAmount(Order order)
        {
            // FinalPrice KDV dahil, VatAmount çıkararak net tutarı buluyoruz
            return order.FinalPrice - order.VatAmount;
        }

        /// <summary>
        /// Tarih formatlar (Mikro formatı).
        /// </summary>
        private string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Mikro ayarlarını yükler.
        /// </summary>
        private MikroSiparisSettings LoadSettings()
        {
            var section = _configuration.GetSection("MikroApi:Siparis");
            
            return new MikroSiparisSettings
            {
                EvrakSeri = section["EvrakSeri"] ?? "ONL",
                DefaultDepoNo = section.GetValue<int>("DefaultDepoNo", 1),
                DefaultSubeNo = section.GetValue<int?>("DefaultSubeNo", null),
                DefaultDeliveryDays = section.GetValue<int>("DefaultDeliveryDays", 3),
                DefaultKdvOran = section.GetValue<decimal>("DefaultKdvOran", 20m),
                KargoStokKod = section["KargoStokKod"] ?? "KARGO001",
                KargoKdvOran = section.GetValue<decimal>("KargoKdvOran", 20m),
                AddShippingAsLine = section.GetValue<bool>("AddShippingAsLine", false)
            };
        }

        #endregion

        #region Settings Class

        /// <summary>
        /// Mikro sipariş ayarları.
        /// </summary>
        private class MikroSiparisSettings
        {
            public string EvrakSeri { get; set; } = "ONL";
            public int DefaultDepoNo { get; set; } = 1;
            public int? DefaultSubeNo { get; set; }
            public int DefaultDeliveryDays { get; set; } = 3;
            public decimal DefaultKdvOran { get; set; } = 20m;
            public string KargoStokKod { get; set; } = "KARGO001";
            public decimal KargoKdvOran { get; set; } = 20m;
            public bool AddShippingAsLine { get; set; } = false;
        }

        #endregion
    }
}
