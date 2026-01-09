using System;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// SMS rate limiting için repository interface.
    /// SOLID: Single Responsibility - Sadece rate limit veri erişimi
    /// </summary>
    public interface ISmsRateLimitRepository : IRepository<SmsRateLimit>
    {
        /// <summary>
        /// Telefon numarasına göre rate limit kaydını getirir.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <returns>Rate limit kaydı veya null</returns>
        Task<SmsRateLimit?> GetByPhoneAsync(string phoneNumber);

        /// <summary>
        /// IP adresine göre rate limit kaydını getirir.
        /// </summary>
        /// <param name="ipAddress">IP adresi</param>
        /// <returns>Rate limit kaydı veya null</returns>
        Task<SmsRateLimit?> GetByIpAsync(string ipAddress);

        /// <summary>
        /// Telefon numarası ve IP için rate limit kaydını getirir veya oluşturur.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="ipAddress">IP adresi (opsiyonel)</param>
        /// <returns>Rate limit kaydı</returns>
        Task<SmsRateLimit> GetOrCreateAsync(string phoneNumber, string? ipAddress = null);

        /// <summary>
        /// Rate limit sayaçlarını artırır.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="ipAddress">IP adresi</param>
        Task IncrementCountersAsync(string phoneNumber, string? ipAddress = null);

        /// <summary>
        /// Günlük sayaçları sıfırlar (background job için).
        /// </summary>
        /// <returns>Sıfırlanan kayıt sayısı</returns>
        Task<int> ResetDailyCountersAsync();

        /// <summary>
        /// Saatlik sayaçları sıfırlar (background job için).
        /// </summary>
        /// <returns>Sıfırlanan kayıt sayısı</returns>
        Task<int> ResetHourlyCountersAsync();

        /// <summary>
        /// Telefon numarasını belirli bir süre için bloklar.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="duration">Blok süresi</param>
        /// <param name="reason">Blok nedeni</param>
        Task BlockPhoneAsync(string phoneNumber, TimeSpan duration, string reason);

        /// <summary>
        /// Telefon numarasının blokajını kaldırır.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        Task UnblockPhoneAsync(string phoneNumber);

        /// <summary>
        /// Şüpheli aktivite tespit edildiğinde çağrılır.
        /// Başarısız deneme sayısını artırır.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        Task RecordFailedAttemptAsync(string phoneNumber);
    }
}
