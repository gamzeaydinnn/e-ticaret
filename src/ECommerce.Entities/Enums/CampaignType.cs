namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Kampanya türlerini tanımlar.
    /// Her tür farklı bir indirim/promosyon mantığı çalıştırır.
    /// </summary>
    public enum CampaignType
    {
        /// <summary>
        /// Yüzdelik indirim (örn: %10, %20)
        /// DiscountValue alanı yüzde değerini tutar (10 = %10)
        /// </summary>
        Percentage = 0,

        /// <summary>
        /// Sabit tutar indirimi (örn: 50 TL, 100 TL)
        /// DiscountValue alanı TL cinsinden indirimi tutar
        /// </summary>
        FixedAmount = 1,

        /// <summary>
        /// X Al Y Öde kampanyası (örn: 3 Al 2 Öde)
        /// BuyQty ve PayQty alanları kullanılır
        /// En ucuz ürün(ler) bedava olur
        /// </summary>
        BuyXPayY = 2,

        /// <summary>
        /// Ücretsiz kargo kampanyası
        /// Koşullar sağlandığında kargo ücreti 0 olur
        /// </summary>
        FreeShipping = 3
    }
}
