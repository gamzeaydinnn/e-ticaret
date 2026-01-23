namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Ürün ağırlık/miktar birimi
    /// Ağırlık bazlı satış yapılan ürünlerde kullanılır
    /// </summary>
    public enum WeightUnit
    {
        /// <summary>
        /// Adet bazlı satış (varsayılan)
        /// Şişe su, paket makarna gibi ürünler
        /// </summary>
        Piece = 0,

        /// <summary>
        /// Gram bazlı satış
        /// Küçük miktarlı ürünler (baharat, kuruyemiş vb.)
        /// </summary>
        Gram = 1,

        /// <summary>
        /// Kilogram bazlı satış
        /// Meyve, sebze, et gibi ürünler
        /// </summary>
        Kilogram = 2,

        /// <summary>
        /// Litre bazlı satış
        /// Süt, yağ gibi sıvı ürünler
        /// </summary>
        Liter = 3,

        /// <summary>
        /// Mililitre bazlı satış
        /// Küçük miktarlı sıvı ürünler
        /// </summary>
        Milliliter = 4
    }
}
