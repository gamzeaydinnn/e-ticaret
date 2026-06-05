using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Ürün alanlarının Mikro senkronizasyonunda kullanılacak global varsayılanları tutar.
    /// </summary>
    public class ProductAdminOverrideSetting : BaseEntity
    {
        public bool DefaultAdminOverrideName { get; set; }
        public bool DefaultAdminOverridePrice { get; set; }
        public bool DefaultAdminOverrideCategory { get; set; }
        public int? UpdatedByUserId { get; set; }
        public string? UpdatedByUserName { get; set; }
    }
}