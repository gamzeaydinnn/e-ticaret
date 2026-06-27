using System;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Helpers
{
    /// <summary>
    /// Ağırlık bazlı (kg) siparişlerde KART provizyon (Auth) → kesin çekim (Capt/Capture) politikası için
    /// TEK doğruluk kaynağı.
    ///
    /// NEDEN BU SINIF VAR?
    /// Aşım yüzdesi (%20), "maksimum çekilebilir tutar" hesabı ve provizyon geçerlilik süresi daha önce
    /// üç ayrı serviste (WeightBasedPaymentService, WeightAdjustmentService, PaymentCaptureService) ayrı
    /// ayrı kodlanmıştı. Bu kopyalar zamanla ayrışıp tutarsız tahsilat kararları üretebilirdi. Politika
    /// burada tek noktada toplanır; tüm servisler bunu çağırır (DRY + tek doğruluk kaynağı).
    ///
    /// İŞ KURALI (banka anlaşması — doğrulandı):
    /// - Banka, provizyon (Auth) tutarının EN FAZLA %20 ÜZERİNE kadar kesin çekime (Capt) izin verir.
    ///   Final tutar bu sınırı (Auth × 1.20) aşarsa OTOMATİK çekilemez; kalan fark için admin
    ///   müdahalesi / ek tahsilat gerekir.
    /// - Kesin çekim KURYE TESLİM ANINDA otomatik tetiklenir (CourierOrderManager.MarkDelivered).
    /// </summary>
    public static class WeightBasedCapturePolicy
    {
        /// <summary>
        /// Bankanın izin verdiği provizyon aşım üst sınırı (yüzde).
        /// Kesin çekim kuralı: Capt ≤ Auth × (1 + %20).
        /// </summary>
        public const decimal CaptureOveragePercent = 20m;

        /// <summary>
        /// Provizyon geçerlilik süresi (saat).
        /// NEDEN VARSAYILAN: Banka sözleşmesindeki kesin süre henüz teyit edilmediğinden, güvenli/
        /// muhafazakâr bir varsayılan (7 gün = 168 saat) kullanılır. Süre teyit edildiğinde yalnızca
        /// burası güncellenir; tüm servisler otomatik uyumlanır.
        /// </summary>
        public const int PreAuthValidityHours = 168;

        /// <summary>
        /// Banka kuralı gereği kesin çekime izin verilen MAKSİMUM tutarı (Auth × 1.20) döndürür.
        /// Provizyon yoksa (≤ 0) 0 döner.
        /// </summary>
        public static decimal CalculateMaxCapturableAmount(decimal authorizedAmount)
        {
            if (authorizedAmount <= 0m)
            {
                return 0m;
            }

            return Math.Round(
                authorizedAmount * (1m + (CaptureOveragePercent / 100m)),
                2,
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Final tutar, banka aşım sınırı (Auth × 1.20) içinde mi? true ise otomatik çekilebilir.
        /// </summary>
        public static bool IsWithinCaptureLimit(decimal authorizedAmount, decimal finalAmount)
            => finalAmount <= CalculateMaxCapturableAmount(authorizedAmount);

        /// <summary>
        /// Kesin çekim kararı: final tutarı banka aşım sınırına (Auth × 1.20) kırpar.
        /// - final ≤ sınır → tam final tutar çekilir, <see cref="CaptureDecision.ExceedsLimit"/> = false.
        /// - final > sınır → yalnız sınıra kadar (otomatik) çekilir, kalan fark
        ///   <see cref="CaptureDecision.ExceedsLimit"/> = true ile işaretlenir (admin/ek tahsilat).
        ///
        /// NEDEN tek nokta: Aynı "min(final, max) + aşım bayrağı" kararı birden çok serviste tekrar
        /// ediliyordu; tutarsızlık riskini ortadan kaldırmak için karar burada üretilir.
        /// </summary>
        public static CaptureDecision ClampToCaptureLimit(decimal authorizedAmount, decimal finalAmount)
        {
            var maxCapturable = CalculateMaxCapturableAmount(authorizedAmount);

            return finalAmount <= maxCapturable
                ? new CaptureDecision(finalAmount, false)
                : new CaptureDecision(maxCapturable, true);
        }

        /// <summary>
        /// Provizyon tutarı için TEK doğruluk kaynağı.
        /// NEDEN İKİ ALAN VAR: Tarihsel olarak capture katmanı <c>Order.AuthorizedAmount</c>, ağırlık
        /// katmanı <c>Order.PreAuthAmount</c> alanını kullanıyordu. 3D Secure callback ikisini eşitler;
        /// yine de alanlar ayrışırsa pozitif olan AuthorizedAmount önceliklenir, yoksa PreAuthAmount'a
        /// düşülür. Böylece tek bir alanın boş kalması yanlış "provizyon yok" kararına yol açmaz.
        /// </summary>
        public static decimal ResolveAuthorizedAmount(Order? order)
        {
            if (order is null)
            {
                return 0m;
            }

            return order.AuthorizedAmount > 0m
                ? order.AuthorizedAmount
                : order.PreAuthAmount;
        }

        /// <summary>
        /// Provizyonun geçerlilik süresi dolmuş mu?
        /// NEDEN authorizedAt yoksa false: Provizyon tarihi bilinmiyorsa güvenli tarafta kalıp çekimi
        /// erkenden engellemeyiz; gerçekten süresi dolmuşsa banka zaten Capt reddi döndürür.
        /// </summary>
        public static bool IsPreAuthExpired(DateTime? authorizedAt, DateTime? nowUtc = null)
        {
            if (!authorizedAt.HasValue)
            {
                return false;
            }

            var reference = nowUtc ?? DateTime.UtcNow;
            return authorizedAt.Value.AddHours(PreAuthValidityHours) < reference;
        }
    }

    /// <summary>
    /// Kesin çekim kararı sonucu: bankaya gönderilecek tutar ve aşım limitinin geçilip geçilmediği.
    /// </summary>
    /// <param name="CaptureAmount">Bankaya gönderilecek (limit içine kırpılmış) çekim tutarı.</param>
    /// <param name="ExceedsLimit">true ise final tutar Auth × 1.20 sınırını aşmıştır; kalan fark admin/ek tahsilat gerektirir.</param>
    public readonly record struct CaptureDecision(decimal CaptureAmount, bool ExceedsLimit);
}
