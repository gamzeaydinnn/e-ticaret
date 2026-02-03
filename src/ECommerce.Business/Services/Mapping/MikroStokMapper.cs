using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Mapping
{
    /// <summary>
    /// Mikro ERP stok verilerini e-ticaret Product entity'sine dönüştürür.
    /// 
    /// NEDEN: Mikro API'den gelen stok verileri farklı formatta
    /// (Türkçe alan adları, iç içe yapılar). Bu sınıf dönüşüm
    /// kurallarını merkezi bir yerde yönetir.
    /// 
    /// MAPPING KURALLARI:
    /// - sto_kod → SKU (benzersiz ürün kodu)
    /// - sto_isim → Name
    /// - sto_kisa_ismi → Description
    /// - sto_miktar veya depo_miktar → StockQuantity
    /// - satis_fiyatlari[0].sfiyat_fiyati → Price
    /// - sto_birim1_ad → WeightUnit (birim eşlemesi ile)
    /// - sto_brut_agirlik → UnitWeightGrams
    /// </summary>
    public class MikroStokMapper : IMikroStokMapper
    {
        private readonly ILogger<MikroStokMapper> _logger;

        // Birim eşleme tablosu
        private static readonly Dictionary<string, WeightUnit> _birimMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ADET"] = WeightUnit.Piece,
            ["AD"] = WeightUnit.Piece,
            ["KG"] = WeightUnit.Kilogram,
            ["KILOGRAM"] = WeightUnit.Kilogram,
            ["KİLOGRAM"] = WeightUnit.Kilogram,
            ["GR"] = WeightUnit.Gram,
            ["GRAM"] = WeightUnit.Gram,
            ["LT"] = WeightUnit.Liter,
            ["LİTRE"] = WeightUnit.Liter,
            ["LITRE"] = WeightUnit.Liter,
            ["ML"] = WeightUnit.Milliliter,
            ["MİLİLİTRE"] = WeightUnit.Milliliter,
            ["PAKET"] = WeightUnit.Piece,
            ["KUTU"] = WeightUnit.Piece,
            ["ŞİŞE"] = WeightUnit.Piece,
            ["SISE"] = WeightUnit.Piece,
            ["DEMET"] = WeightUnit.Piece
        };

        // KDV oranları
        private static readonly decimal[] _kdvOranlari = { 0m, 1m, 10m, 20m };

        public MikroStokMapper(ILogger<MikroStokMapper> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Product MapToProduct(MikroStokResponseDto mikroStok, MikroCategoryMapping? categoryMapping = null)
        {
            if (mikroStok == null)
                throw new ArgumentNullException(nameof(mikroStok));

            if (string.IsNullOrWhiteSpace(mikroStok.StoKod))
                throw new ArgumentException("Stok kodu boş olamaz", nameof(mikroStok));

            _logger.LogDebug(
                "[MikroStokMapper] Ürün mapping başlıyor. SKU: {Sku}, İsim: {Name}",
                mikroStok.StoKod, mikroStok.StoIsim);

            var product = new Product
            {
                SKU = mikroStok.StoKod.Trim(),
                Name = mikroStok.StoIsim?.Trim() ?? mikroStok.StoKod,
                Description = BuildDescription(mikroStok),
                Slug = GenerateSlug(mikroStok.StoIsim ?? mikroStok.StoKod),
                StockQuantity = CalculateStockQuantity(mikroStok),
                Price = ExtractPrice(mikroStok.SatisFiyatlari),
                Currency = "TRY",
                IsActive = !mikroStok.StoPasifFl,
                CreatedAt = mikroStok.StoCreateDate ?? DateTime.UtcNow,
                UpdatedAt = mikroStok.StoLastupDate ?? DateTime.UtcNow
            };

            // Kategori eşlemesi
            if (categoryMapping != null)
            {
                product.CategoryId = categoryMapping.CategoryId;
                product.BrandId = categoryMapping.BrandId;
            }
            else
            {
                // Varsayılan kategori (tanımsız ürünler için)
                product.CategoryId = 1;
            }

            // Birim ve ağırlık bazlı satış
            MapWeightProperties(product, mikroStok);

            // Barkod varsa SKU'ya ekle (alternatif arama için)
            // Ana barkodu ürün açıklamasına ekle
            if (mikroStok.Barkodlar?.Any() == true)
            {
                var anaBarkod = mikroStok.Barkodlar.FirstOrDefault(b => b.BarAnaBarkod == true)
                    ?? mikroStok.Barkodlar.First();
                
                // Barkodu description'a ekle (aranabilirlik için)
                if (!string.IsNullOrEmpty(anaBarkod.BarBarkodNo))
                {
                    product.Description += $" [Barkod: {anaBarkod.BarBarkodNo}]";
                }
            }

            _logger.LogDebug(
                "[MikroStokMapper] Ürün mapping tamamlandı. SKU: {Sku}, Fiyat: {Price}, Stok: {Stock}",
                product.SKU, product.Price, product.StockQuantity);

            return product;
        }

        /// <inheritdoc />
        public void UpdateProduct(Product existingProduct, MikroStokResponseDto mikroStok)
        {
            if (existingProduct == null)
                throw new ArgumentNullException(nameof(existingProduct));

            if (mikroStok == null)
                throw new ArgumentNullException(nameof(mikroStok));

            _logger.LogDebug(
                "[MikroStokMapper] Ürün güncelleniyor. SKU: {Sku}",
                existingProduct.SKU);

            // Temel alanları güncelle
            existingProduct.Name = mikroStok.StoIsim?.Trim() ?? existingProduct.Name;
            existingProduct.StockQuantity = CalculateStockQuantity(mikroStok);
            existingProduct.Price = ExtractPrice(mikroStok.SatisFiyatlari);
            existingProduct.IsActive = !mikroStok.StoPasifFl;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            // Ağırlık özelliklerini güncelle
            MapWeightProperties(existingProduct, mikroStok);

            _logger.LogDebug(
                "[MikroStokMapper] Ürün güncellendi. SKU: {Sku}, Yeni Stok: {Stock}, Yeni Fiyat: {Price}",
                existingProduct.SKU, existingProduct.StockQuantity, existingProduct.Price);
        }

        /// <inheritdoc />
        public MikroStokKaydetRequestDto MapToMikroStok(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            _logger.LogDebug(
                "[MikroStokMapper] Ürün Mikro formatına dönüştürülüyor. SKU: {Sku}",
                product.SKU);

            var mikroStok = new MikroStokKaydetRequestDto
            {
                StoKod = product.SKU,
                StoIsim = product.Name,
                StoKisaIsmi = product.Description?.Length > 100 
                    ? product.Description.Substring(0, 100) 
                    : product.Description,
                StoBirim1Ad = MapWeightUnitToMikro(product.WeightUnit),
                StoCins = 0, // Ticari mal
                StoToptanVergi = 20, // Varsayılan KDV
                StoPerakendeVergi = 20
            };

            // Fiyat bilgisi
            if (product.Price > 0)
            {
                mikroStok.SatisFiyatlari = new List<MikroStokFiyatDto>
                {
                    new MikroStokFiyatDto
                    {
                        SfiyatNo = 1,
                        SfiyatFiyati = product.Price,
                        SfiyatDovizCinsi = 0 // TL
                    }
                };
            }

            return mikroStok;
        }

        /// <inheritdoc />
        public decimal ExtractPrice(
            IEnumerable<MikroStokFiyatResponseDto>? fiyatlar,
            int fiyatNo = 1,
            bool kdvDahil = true)
        {
            if (fiyatlar == null || !fiyatlar.Any())
            {
                _logger.LogDebug("[MikroStokMapper] Fiyat listesi boş, varsayılan 0 döndürülüyor");
                return 0m;
            }

            // İstenen fiyat numarasını bul
            var fiyat = fiyatlar.FirstOrDefault(f => f.SfiyatNo == fiyatNo);

            // Bulunamazsa ilk fiyatı al
            if (fiyat == null)
            {
                fiyat = fiyatlar.First();
                _logger.LogDebug(
                    "[MikroStokMapper] Fiyat no {FiyatNo} bulunamadı, ilk fiyat kullanılıyor",
                    fiyatNo);
            }

            var price = fiyat.SfiyatFiyati;

            // KDV dahil mi kontrol et
            if (kdvDahil && fiyat.SfiyatVergiDahil != true)
            {
                // KDV ekle (varsayılan %20)
                price *= 1.20m;
            }
            else if (!kdvDahil && fiyat.SfiyatVergiDahil == true)
            {
                // KDV çıkar
                price /= 1.20m;
            }

            return Math.Round(price, 2);
        }

        // ==================== YARDIMCI METODLAR ====================

        /// <summary>
        /// Ürün açıklamasını oluşturur.
        /// </summary>
        private string BuildDescription(MikroStokResponseDto mikroStok)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(mikroStok.StoKisaIsmi))
                parts.Add(mikroStok.StoKisaIsmi);

            // Grup bilgisi
            if (!string.IsNullOrWhiteSpace(mikroStok.StoAnagrupKod))
                parts.Add($"Kategori: {mikroStok.StoAnagrupKod}");

            // Marka
            if (!string.IsNullOrWhiteSpace(mikroStok.StoMarkaKod))
                parts.Add($"Marka: {mikroStok.StoMarkaKod}");

            return parts.Any() ? string.Join(" | ", parts) : "";
        }

        /// <summary>
        /// URL-safe slug oluşturur.
        /// </summary>
        private string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Guid.NewGuid().ToString("N")[..8];

            // Türkçe karakterleri dönüştür
            var slug = name.ToLowerInvariant()
                .Replace("ı", "i")
                .Replace("ğ", "g")
                .Replace("ü", "u")
                .Replace("ş", "s")
                .Replace("ö", "o")
                .Replace("ç", "c")
                .Replace("İ", "i")
                .Replace("Ğ", "g")
                .Replace("Ü", "u")
                .Replace("Ş", "s")
                .Replace("Ö", "o")
                .Replace("Ç", "c");

            // Alfanumerik olmayan karakterleri tire ile değiştir
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9]+", "-");

            // Baştaki ve sondaki tireleri temizle
            slug = slug.Trim('-');

            // Boşsa random ekle
            if (string.IsNullOrEmpty(slug))
                slug = Guid.NewGuid().ToString("N")[..8];

            // Uzunluk sınırı
            if (slug.Length > 100)
                slug = slug.Substring(0, 100);

            return slug;
        }

        /// <summary>
        /// Stok miktarını hesaplar.
        /// Öncelik: depo_miktar > sto_miktar
        /// </summary>
        private int CalculateStockQuantity(MikroStokResponseDto mikroStok)
        {
            // Önce belirli depo stoğunu kontrol et
            if (mikroStok.DepoMiktar.HasValue)
            {
                var available = mikroStok.DepoMiktar.Value - (mikroStok.RezerveMiktar ?? 0);
                return Math.Max(0, (int)Math.Floor(available));
            }

            // Yoksa toplam stoğu kullan
            return Math.Max(0, (int)Math.Floor(mikroStok.StoMiktar));
        }

        /// <summary>
        /// Ağırlık bazlı özellikleri eşler.
        /// </summary>
        private void MapWeightProperties(Product product, MikroStokResponseDto mikroStok)
        {
            // Birim eşlemesi
            var birimAd = mikroStok.StoBirim1Ad?.ToUpperInvariant().Trim() ?? "ADET";
            
            if (_birimMappings.TryGetValue(birimAd, out var weightUnit))
            {
                product.WeightUnit = weightUnit;
            }
            else
            {
                product.WeightUnit = WeightUnit.Piece;
                _logger.LogDebug(
                    "[MikroStokMapper] Birim eşlemesi bulunamadı: {Birim}, ADET kabul edildi",
                    birimAd);
            }

            // Ağırlık bazlı mı?
            product.IsWeightBased = product.WeightUnit == WeightUnit.Kilogram 
                || product.WeightUnit == WeightUnit.Gram
                || product.WeightUnit == WeightUnit.Liter
                || product.WeightUnit == WeightUnit.Milliliter;

            // Birim ağırlık
            if (mikroStok.StoBrutAgirlik.HasValue && mikroStok.StoBrutAgirlik > 0)
            {
                // Mikro'dan gelen ağırlık kg cinsinden olabilir, gram'a çevir
                product.UnitWeightGrams = (int)(mikroStok.StoBrutAgirlik.Value * 1000);
            }

            // Ağırlık bazlı ise birim fiyatı ayarla
            if (product.IsWeightBased)
            {
                product.PricePerUnit = product.Price;
            }
        }

        /// <summary>
        /// E-ticaret WeightUnit'i Mikro birim adına dönüştürür.
        /// </summary>
        private string MapWeightUnitToMikro(WeightUnit weightUnit)
        {
            return weightUnit switch
            {
                WeightUnit.Kilogram => "KG",
                WeightUnit.Gram => "GR",
                WeightUnit.Liter => "LT",
                WeightUnit.Milliliter => "ML",
                WeightUnit.Piece => "ADET",
                _ => "ADET"
            };
        }
    }
}
