namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// CampaignTarget tablosundaki hedefin türünü belirler.
    /// TargetId'nin hangi tabloya referans verdiğini gösterir.
    /// </summary>
    public enum CampaignTargetKind
    {
        /// <summary>
        /// Hedef bir üründür (Products tablosuna referans)
        /// </summary>
        Product = 0,

        /// <summary>
        /// Hedef bir kategoridir (Categories tablosuna referans)
        /// Alt kategoriler de dahil edilir
        /// </summary>
        Category = 1
    }
}
