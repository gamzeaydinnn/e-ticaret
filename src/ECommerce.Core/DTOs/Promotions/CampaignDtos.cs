using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ECommerce.Core.Attributes;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.DTOs.Promotions
{
    /// <summary>
    /// Kampanya listesi için minimal DTO
    /// </summary>
    public class CampaignListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        // Yeni alanlar
        public CampaignType Type { get; set; }
        public CampaignTargetType TargetType { get; set; }
        public decimal DiscountValue { get; set; }
        public string TypeDisplayName => GetTypeDisplayName();
        public string TargetTypeDisplayName => GetTargetTypeDisplayName();
        
        private string GetTypeDisplayName() => Type switch
        {
            CampaignType.Percentage => "Yüzde İndirim",
            CampaignType.FixedAmount => "Sabit Tutar İndirim",
            CampaignType.BuyXPayY => "X Al Y Öde",
            CampaignType.FreeShipping => "Ücretsiz Kargo",
            _ => "Bilinmiyor"
        };
        
        private string GetTargetTypeDisplayName() => TargetType switch
        {
            CampaignTargetType.All => "Tüm Ürünler",
            CampaignTargetType.Category => "Kategori Bazlı",
            CampaignTargetType.Product => "Ürün Bazlı",
            _ => "Bilinmiyor"
        };
    }

    /// <summary>
    /// Kampanya detayı için genişletilmiş DTO
    /// </summary>
    public class CampaignDetailDto : CampaignListDto
    {
        // Eski alanlar (geriye dönük uyumluluk)
        public List<string> RulesSummaries { get; set; } = new();
        public List<string> RewardsSummaries { get; set; } = new();
        public string? ConditionJson { get; set; }
        public string RewardType { get; set; } = "Percent";
        public decimal RewardValue { get; set; }
        
        // Yeni alanlar
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinCartTotal { get; set; }
        public int? MinQuantity { get; set; }
        public int? BuyQty { get; set; }
        public int? PayQty { get; set; }
        public int Priority { get; set; }
        public bool IsStackable { get; set; }
        
        /// <summary>
        /// Kampanya hedefleri (ürün veya kategori ID'leri)
        /// </summary>
        public List<CampaignTargetDto> Targets { get; set; } = new();
    }

    /// <summary>
    /// Kampanya hedefi DTO
    /// </summary>
    public class CampaignTargetDto
    {
        public int TargetId { get; set; }
        public CampaignTargetKind TargetKind { get; set; }
        public string? TargetName { get; set; } // Ürün veya kategori adı (opsiyonel, sadece gösterim için)
    }

    /// <summary>
    /// Kampanya kaydetme/güncelleme için DTO
    /// Hem eski hem yeni yapıyı destekler
    /// </summary>
    [DateRange(MinDays = 1, MaxDays = 365)]
    public class CampaignSaveDto
    {
        [Required(ErrorMessage = "Kampanya adı zorunludur")]
        [StringLength(200, ErrorMessage = "Kampanya adı en fazla 200 karakter olabilir")]
        [NoHtmlContent(ErrorMessage = "Kampanya adında HTML/JavaScript içeriği kullanılamaz")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
        [NoHtmlContent(ErrorMessage = "Açıklamada HTML/JavaScript içeriği kullanılamaz")]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi zorunludur")]
        [FutureDate]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi zorunludur")]
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; }
        
        // Eski alanlar (geriye dönük uyumluluk)
        public string? ConditionJson { get; set; }
        [Required]
        public string RewardType { get; set; } = "Percent";
        public decimal RewardValue { get; set; }
        
        // Yeni alanlar - Kampanya Türü ve Hedefi
        public CampaignType Type { get; set; } = CampaignType.Percentage;
        public CampaignTargetType TargetType { get; set; } = CampaignTargetType.All;
        
        /// <summary>
        /// İndirim değeri:
        /// - Percentage için: yüzde (örn: 10 = %10)
        /// - FixedAmount için: TL tutarı (örn: 50 = 50 TL)
        /// </summary>
        [Range(0, 100000, ErrorMessage = "İndirim değeri 0-100000 arasında olmalıdır")]
        public decimal DiscountValue { get; set; }
        
        /// <summary>
        /// Maksimum indirim tutarı (opsiyonel, yüzdelik indirimler için)
        /// </summary>
        [Range(0, 100000, ErrorMessage = "Maksimum indirim tutarı 0-100000 arasında olmalıdır")]
        public decimal? MaxDiscountAmount { get; set; }
        
        /// <summary>
        /// Minimum sepet tutarı (opsiyonel)
        /// </summary>
        [Range(0, 1000000, ErrorMessage = "Minimum sepet tutarı 0-1000000 arasında olmalıdır")]
        public decimal? MinCartTotal { get; set; }
        
        /// <summary>
        /// Minimum ürün adedi (opsiyonel)
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Minimum ürün adedi 1-1000 arasında olmalıdır")]
        public int? MinQuantity { get; set; }
        
        /// <summary>
        /// X Al Y Öde - Alınması gereken adet (örn: 3 Al 2 Öde için BuyQty=3)
        /// </summary>
        [Range(2, 100, ErrorMessage = "Alınması gereken adet 2-100 arasında olmalıdır")]
        public int? BuyQty { get; set; }
        
        /// <summary>
        /// X Al Y Öde - Ödenecek adet (örn: 3 Al 2 Öde için PayQty=2)
        /// </summary>
        [Range(1, 99, ErrorMessage = "Ödenecek adet 1-99 arasında olmalıdır")]
        public int? PayQty { get; set; }
        
        /// <summary>
        /// Kampanya önceliği (düşük değer = yüksek öncelik)
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Öncelik 1-1000 arasında olmalıdır")]
        public int Priority { get; set; } = 100;
        
        /// <summary>
        /// Diğer kampanyalarla birlikte çalışabilir mi?
        /// </summary>
        public bool IsStackable { get; set; } = true;
        
        /// <summary>
        /// Kampanya hedefleri (ürün veya kategori ID'leri)
        /// TargetType = All ise boş bırakılabilir
        /// </summary>
        public List<int>? TargetIds { get; set; }
    }

    /// <summary>
    /// Sepet satırına uygulanan kampanya bilgisi
    /// PricingEngine tarafından kullanılır
    /// </summary>
    public class AppliedCampaignDto
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public CampaignType Type { get; set; }
        public decimal DiscountAmount { get; set; }
        public string DisplayText { get; set; } = string.Empty;
        
        /// <summary>
        /// Hangi satırlara uygulandı (OrderItemId veya ProductId listesi)
        /// </summary>
        public List<int> AppliedToItemIds { get; set; } = new();
    }

    /// <summary>
    /// Kampanya hesaplama sonucu
    /// </summary>
    public class CampaignCalculationResult
    {
        /// <summary>
        /// Uygulanan kampanyalar
        /// </summary>
        public List<AppliedCampaignDto> AppliedCampaigns { get; set; } = new();
        
        /// <summary>
        /// Toplam kampanya indirimi
        /// </summary>
        public decimal TotalCampaignDiscount { get; set; }
        
        /// <summary>
        /// Ücretsiz kargo mu?
        /// </summary>
        public bool IsFreeShipping { get; set; }
        
        /// <summary>
        /// Satır bazlı indirimler (ProductId -> İndirim tutarı)
        /// </summary>
        public Dictionary<int, decimal> LineDiscounts { get; set; } = new();
    }

    /// <summary>
    /// Sepet satırı için kampanya hesaplama girişi
    /// </summary>
    public class CartItemForCampaign
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}

