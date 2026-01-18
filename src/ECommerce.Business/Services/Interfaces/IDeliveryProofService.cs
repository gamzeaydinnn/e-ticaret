// ==========================================================================
// IDeliveryProofService.cs - Teslimat Kanıtı Servis Interface'i
// ==========================================================================
// Teslimat kanıtı (POD) yönetimi: fotoğraf, imza, OTP doğrulama
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Teslimat kanıtı servis interface'i.
    /// Fotoğraf, imza, OTP yönetimi.
    /// </summary>
    public interface IDeliveryProofService
    {
        /// <summary>
        /// Teslimat kanıtı ekler.
        /// </summary>
        Task<DeliveryProofOfDelivery> AddProofAsync(DeliveryProofOfDelivery proof);

        /// <summary>
        /// Teslimat kanıtlarını getirir.
        /// </summary>
        Task<List<DeliveryProofOfDelivery>> GetProofsByTaskIdAsync(int taskId);

        /// <summary>
        /// Kanıt detayını getirir.
        /// </summary>
        Task<DeliveryProofOfDelivery?> GetProofByIdAsync(int proofId);

        /// <summary>
        /// Kanıt siler.
        /// </summary>
        Task DeleteProofAsync(int proofId);

        /// <summary>
        /// OTP doğrular.
        /// </summary>
        Task<bool> VerifyOTPAsync(int taskId, string otpCode);

        /// <summary>
        /// OTP'yi doğrulandı olarak işaretler.
        /// </summary>
        Task MarkOTPVerifiedAsync(int taskId);

        /// <summary>
        /// Teslimat için tüm kanıtların tamamlanıp tamamlanmadığını kontrol eder.
        /// </summary>
        Task<bool> AreRequirementsMet(int taskId, decimal orderAmount);
    }
}
