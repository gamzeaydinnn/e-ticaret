namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Kampanyanın hedeflediği kapsam türünü belirler.
    /// Kampanya tüm sepete mi, belirli kategorilere mi yoksa belirli ürünlere mi uygulanacak.
    /// </summary>
    public enum CampaignTargetType
    {
        /// <summary>
        /// Kampanya tüm ürünlere/sepete uygulanır
        /// Hedef listesi gerekmez
        /// </summary>
        All = 0,

        /// <summary>
        /// Kampanya sadece belirli kategorilerdeki ürünlere uygulanır
        /// CampaignTarget tablosunda CategoryId'ler tutulur
        /// </summary>
        Category = 1,

        /// <summary>
        /// Kampanya sadece belirli ürünlere uygulanır
        /// CampaignTarget tablosunda ProductId'ler tutulur
        /// </summary>
        Product = 2
    }
}
