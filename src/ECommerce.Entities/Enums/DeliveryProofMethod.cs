// ==========================================================================
// DeliveryProofMethod.cs - Teslimat Kanıtı Yöntemi
// ==========================================================================
// Bu enum, teslimat tamamlandığında kullanılacak kanıt yöntemini tanımlar.
// Her teslimat için en az bir POD (Proof of Delivery) zorunludur.
// ==========================================================================

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Teslimat kanıtı (POD - Proof of Delivery) yöntemleri.
    /// Teslimat tamamlandığında bu yöntemlerden biri ile kanıt alınmalıdır.
    /// </summary>
    public enum DeliveryProofMethod
    {
        /// <summary>
        /// Fotoğraf ile kanıt.
        /// Kurye teslim anında fotoğraf çeker (kapı önü, müşteri ile vb.)
        /// En yaygın kullanılan yöntem.
        /// </summary>
        Photo = 0,

        /// <summary>
        /// OTP (One-Time Password) ile kanıt.
        /// Müşteriye SMS ile gönderilen kodu kurye girer.
        /// Müşteri kimliğini doğrulamak için güvenli yöntem.
        /// </summary>
        Otp = 1,

        /// <summary>
        /// Dijital imza ile kanıt.
        /// Müşteri kurye cihazında imza atar.
        /// Kurumsal teslimatlar için tercih edilir.
        /// </summary>
        Signature = 2,

        /// <summary>
        /// Pin kodu ile kanıt.
        /// Müşterinin bildiği sabit pin kodunu kurye girer.
        /// Hızlı doğrulama için kullanılır.
        /// </summary>
        PinCode = 3,

        /// <summary>
        /// QR kod taraması ile kanıt.
        /// Müşteri uygulamasındaki QR kodu kurye tarar.
        /// Temassız teslimat için idealdir.
        /// </summary>
        QrCode = 4
    }
}
