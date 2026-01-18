// XmlMappingConfigDto: XML alanlarını entity alanlarına eşleştirme konfigürasyonu.
// Farklı tedarikçi XML formatlarını desteklemek için esnek yapı.
// JSON olarak saklanır ve deserialize edilir.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ECommerce.Core.DTOs.XmlImport
{
    /// <summary>
    /// XML alan eşleştirme konfigürasyonunu tanımlar.
    /// Her tedarikçinin farklı XML formatı için ayrı konfigürasyon oluşturulabilir.
    /// </summary>
    public class XmlMappingConfigDto
    {
        #region XML Yapı Tanımları

        /// <summary>
        /// Kök element adı
        /// Örn: "Products", "Items", "Catalog"
        /// Null ise ilk element kullanılır
        /// </summary>
        [StringLength(100, ErrorMessage = "Root element adı en fazla 100 karakter olabilir")]
        public string? RootElement { get; set; }

        /// <summary>
        /// Tekil ürün element adı
        /// Örn: "Product", "Item", "Row"
        /// </summary>
        [Required(ErrorMessage = "Item element adı zorunludur")]
        [StringLength(100, ErrorMessage = "Item element adı en fazla 100 karakter olabilir")]
        public string ItemElement { get; set; } = "Product";

        /// <summary>
        /// XML namespace (varsa)
        /// </summary>
        [StringLength(500, ErrorMessage = "Namespace en fazla 500 karakter olabilir")]
        public string? Namespace { get; set; }

        /// <summary>
        /// Encoding (varsayılan: UTF-8)
        /// </summary>
        public string Encoding { get; set; } = "UTF-8";

        #endregion

        #region Zorunlu Alan Eşleştirmeleri

        /// <summary>
        /// SKU alanı eşleştirmesi
        /// XML'deki SKU tag/attribute adı
        /// Örn: "ProductCode", "SKU", "Sku", "@sku"
        /// </summary>
        [Required(ErrorMessage = "SKU mapping zorunludur")]
        public string SkuMapping { get; set; } = "SKU";

        /// <summary>
        /// Başlık alanı eşleştirmesi
        /// </summary>
        public string TitleMapping { get; set; } = "ProductName";

        /// <summary>
        /// Fiyat alanı eşleştirmesi
        /// </summary>
        public string PriceMapping { get; set; } = "Price";

        /// <summary>
        /// Stok alanı eşleştirmesi
        /// </summary>
        public string StockMapping { get; set; } = "Stock";

        #endregion

        #region Opsiyonel Alan Eşleştirmeleri

        /// <summary>
        /// Barkod alanı eşleştirmesi
        /// </summary>
        public string? BarcodeMapping { get; set; }

        /// <summary>
        /// Para birimi alanı eşleştirmesi
        /// </summary>
        public string? CurrencyMapping { get; set; }

        /// <summary>
        /// Tedarikçi kodu alanı eşleştirmesi
        /// </summary>
        public string? SupplierCodeMapping { get; set; }

        /// <summary>
        /// Ana ürün SKU alanı eşleştirmesi
        /// </summary>
        public string? ParentSkuMapping { get; set; }

        /// <summary>
        /// Ağırlık alanı eşleştirmesi
        /// </summary>
        public string? WeightMapping { get; set; }

        /// <summary>
        /// Hacim alanı eşleştirmesi
        /// </summary>
        public string? VolumeMapping { get; set; }

        #endregion

        #region Kategori ve Marka

        /// <summary>
        /// Kategori yolu alanı eşleştirmesi
        /// Örn: "CategoryPath" -> "İçecekler > Gazlı"
        /// </summary>
        public string? CategoryPathMapping { get; set; }

        /// <summary>
        /// Kategori ayırıcı karakter
        /// Örn: ">", "/", "|"
        /// </summary>
        public string CategoryPathSeparator { get; set; } = ">";

        /// <summary>
        /// Marka alanı eşleştirmesi
        /// </summary>
        public string? BrandMapping { get; set; }

        #endregion

        #region Görsel ve Açıklama

        /// <summary>
        /// Ana görsel URL alanı eşleştirmesi
        /// </summary>
        public string? ImageUrlMapping { get; set; }

        /// <summary>
        /// Ek görseller alanı eşleştirmesi
        /// Virgülle ayrılmış URL listesi veya alt elementler
        /// </summary>
        public string? AdditionalImagesMapping { get; set; }

        /// <summary>
        /// Açıklama alanı eşleştirmesi
        /// </summary>
        public string? DescriptionMapping { get; set; }

        #endregion

        #region Option/Seçenek Eşleştirmeleri

        /// <summary>
        /// Seçenek eşleştirmeleri
        /// Key: Sistemdeki seçenek adı (Hacim, Renk vb.)
        /// Value: XML'deki tag/attribute adı
        /// Örn: { "Hacim": "Volume", "Renk": "Color" }
        /// </summary>
        public Dictionary<string, string>? OptionMappings { get; set; }

        #endregion

        #region Veri Dönüştürme

        /// <summary>
        /// Fiyat çarpanı
        /// XML fiyatı * bu değer = sistem fiyatı
        /// KDV ekleme veya döviz çevirimi için
        /// </summary>
        public decimal PriceMultiplier { get; set; } = 1.0m;

        /// <summary>
        /// Fiyata eklenecek sabit tutar
        /// </summary>
        public decimal PriceAddition { get; set; } = 0;

        /// <summary>
        /// Ağırlık birimi
        /// "g" (gram), "kg" (kilogram), "lb" (pound)
        /// </summary>
        public string WeightUnit { get; set; } = "g";

        /// <summary>
        /// Hacim birimi
        /// "ml" (mililitre), "l" (litre), "cl" (santilitre)
        /// </summary>
        public string VolumeUnit { get; set; } = "ml";

        /// <summary>
        /// Ondalık ayırıcı
        /// "." veya "," - XML'deki format
        /// </summary>
        public string DecimalSeparator { get; set; } = ".";

        #endregion

        #region Özel Eşleştirmeler

        /// <summary>
        /// Ek alan eşleştirmeleri
        /// Standart alanlara uymayan XML tagları için
        /// Key: Hedef alan adı, Value: XML tag/attribute
        /// </summary>
        public Dictionary<string, string>? CustomMappings { get; set; }

        /// <summary>
        /// Değer dönüştürme kuralları
        /// Örn: { "Stock": { "InStock": "999", "OutOfStock": "0" } }
        /// </summary>
        public Dictionary<string, Dictionary<string, string>>? ValueTransformations { get; set; }

        #endregion

        #region Filtreler

        /// <summary>
        /// Sadece belirli koşulu sağlayan kayıtları al
        /// XPath ifadesi
        /// Örn: "[Stock > 0]", "[contains(Category, 'İçecek')]"
        /// </summary>
        public string? FilterExpression { get; set; }

        /// <summary>
        /// Atlanacak SKU pattern'leri
        /// Regex pattern listesi
        /// </summary>
        public List<string>? SkipSkuPatterns { get; set; }

        #endregion

        #region Varsayılan Konfigürasyonlar

        /// <summary>
        /// Varsayılan/standart XML konfigürasyonu
        /// </summary>
        public static XmlMappingConfigDto Default => new()
        {
            ItemElement = "Product",
            SkuMapping = "SKU",
            TitleMapping = "Name",
            PriceMapping = "Price",
            StockMapping = "Stock",
            BarcodeMapping = "Barcode",
            CategoryPathMapping = "Category",
            BrandMapping = "Brand",
            ImageUrlMapping = "ImageUrl"
        };

        /// <summary>
        /// Tipik B2B XML formatı
        /// </summary>
        public static XmlMappingConfigDto B2BFormat => new()
        {
            RootElement = "Catalog",
            ItemElement = "Item",
            SkuMapping = "ProductCode",
            TitleMapping = "ProductName",
            PriceMapping = "SalesPrice",
            StockMapping = "Quantity",
            BarcodeMapping = "EAN",
            SupplierCodeMapping = "SupplierRef",
            ParentSkuMapping = "GroupCode",
            CategoryPathMapping = "CategoryPath",
            CategoryPathSeparator = ">",
            BrandMapping = "Manufacturer"
        };

        /// <summary>
        /// Market/Süpermarket XML formatı
        /// </summary>
        public static XmlMappingConfigDto MarketFormat => new()
        {
            RootElement = "Products",
            ItemElement = "Product",
            SkuMapping = "SKU",
            TitleMapping = "Title",
            PriceMapping = "Price",
            StockMapping = "Stock",
            BarcodeMapping = "Barcode",
            WeightMapping = "Weight",
            VolumeMapping = "Volume",
            WeightUnit = "g",
            VolumeUnit = "ml",
            OptionMappings = new Dictionary<string, string>
            {
                { "Hacim", "Volume" },
                { "Ağırlık", "Weight" },
                { "Paket", "PackageType" }
            }
        };

        #endregion

        #region Validation

        /// <summary>
        /// Konfigürasyonun geçerliliğini kontrol eder.
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ItemElement))
            {
                errors.Add("Item element zorunludur");
            }

            if (string.IsNullOrWhiteSpace(SkuMapping))
            {
                errors.Add("SKU mapping zorunludur");
            }

            if (PriceMultiplier <= 0)
            {
                errors.Add("Fiyat çarpanı 0'dan büyük olmalıdır");
            }

            return errors.Count == 0;
        }

        #endregion
    }
}
