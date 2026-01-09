using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// SMS doğrulama kayıtları için repository interface.
    /// SOLID: Interface Segregation - Sadece SMS doğrulama işlemleri
    /// </summary>
    public interface ISmsVerificationRepository : IRepository<SmsVerification>
    {
        /// <summary>
        /// Telefon numarasına göre aktif (pending) doğrulama kaydını getirir.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası (5xxxxxxxxx formatında)</param>
        /// <param name="purpose">Doğrulama amacı</param>
        /// <returns>Aktif doğrulama kaydı veya null</returns>
        Task<SmsVerification?> GetActiveByPhoneAsync(string phoneNumber, SmsVerificationPurpose purpose);

        /// <summary>
        /// Telefon numarasına ait tüm doğrulama kayıtlarını getirir.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <returns>Doğrulama kayıtları listesi</returns>
        Task<IEnumerable<SmsVerification>> GetByPhoneAsync(string phoneNumber);

        /// <summary>
        /// Kullanıcı ID'sine göre doğrulama kayıtlarını getirir.
        /// </summary>
        /// <param name="userId">Kullanıcı ID</param>
        /// <returns>Doğrulama kayıtları listesi</returns>
        Task<IEnumerable<SmsVerification>> GetByUserIdAsync(int userId);

        /// <summary>
        /// Süresi dolmuş tüm kayıtları getirir (temizlik işlemi için).
        /// </summary>
        /// <returns>Süresi dolmuş kayıtlar</returns>
        Task<IEnumerable<SmsVerification>> GetExpiredAsync();

        /// <summary>
        /// Belirli bir tarih aralığındaki doğrulama kayıtlarını getirir.
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>Tarih aralığındaki kayıtlar</returns>
        Task<IEnumerable<SmsVerification>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Telefon numarası ve kod ile doğrulama kaydını bulur.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="code">Doğrulama kodu</param>
        /// <param name="purpose">Doğrulama amacı</param>
        /// <returns>Eşleşen kayıt veya null</returns>
        Task<SmsVerification?> GetByPhoneAndCodeAsync(string phoneNumber, string code, SmsVerificationPurpose purpose);

        /// <summary>
        /// Süresi dolmuş kayıtları siler/pasifleştirir.
        /// Background job tarafından çağrılır.
        /// </summary>
        /// <returns>Temizlenen kayıt sayısı</returns>
        Task<int> CleanupExpiredAsync();

        /// <summary>
        /// Telefon numarasına ait tüm pending kayıtları iptal eder.
        /// Yeni OTP gönderilmeden önce çağrılmalı.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="purpose">Doğrulama amacı</param>
        Task CancelPendingByPhoneAsync(string phoneNumber, SmsVerificationPurpose purpose);

        /// <summary>
        /// Belirli bir süre içinde gönderilen OTP sayısını döndürür.
        /// Rate limiting için kullanılır.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="since">Başlangıç zamanı</param>
        /// <returns>Gönderilen OTP sayısı</returns>
        Task<int> GetSentCountSinceAsync(string phoneNumber, DateTime since);
    }
}
