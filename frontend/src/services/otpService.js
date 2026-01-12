// src/services/otpService.js
// SMS doğrulama servisi - Backend /api/sms/* endpoint'leri ile iletişim kurar
import api from "./api";

/**
 * SMS Doğrulama Servisi
 *
 * Backend Endpoint'leri:
 * - POST /api/sms/send-otp     : OTP gönder
 * - POST /api/sms/verify-otp   : OTP doğrula
 * - POST /api/sms/resend-otp   : Tekrar gönder
 * - GET  /api/sms/status/{phone} : Durum sorgula
 * - GET  /api/sms/can-send     : Gönderebilir mi kontrol
 *
 * Purpose Enum - Backend (C#) enum değerleriyle eşleşmelidir:
 * - 1: Registration (Kayıt)
 * - 2: PasswordReset (Şifre Sıfırlama)
 * - 3: TwoFactorAuth (2FA)
 * - 4: PhoneChange (Telefon Değişikliği)
 * - 5: OrderConfirmation (Sipariş Onayı)
 * - 6: AccountDeletion (Hesap Silme)
 */

// Purpose enum değerleri - Backend ECommerce.Entities.Enums.SmsVerificationPurpose ile eşleşir
export const SmsVerificationPurpose = {
  Registration: 1, // Backend'de 1
  PasswordReset: 2, // Backend'de 2
  TwoFactorAuth: 3, // Backend'de 3
  PhoneChange: 4, // Backend'de 4
  OrderConfirmation: 5, // Backend'de 5
  AccountDeletion: 6, // Backend'de 6
};

const smsService = {
  /**
   * OTP kodu gönderir
   * @param {string} phoneNumber - Telefon numarası (05XXXXXXXXX veya 5XXXXXXXXX)
   * @param {number} purpose - Doğrulama amacı (SmsVerificationPurpose enum)
   * @returns {Promise<Object>} { success, message, expiresInSeconds, remainingDailyCount }
   */
  async sendOtp(phoneNumber, purpose = SmsVerificationPurpose.Registration) {
    try {
      const response = await api.post("/api/sms/send-otp", {
        phoneNumber,
        purpose,
      });
      console.log("[SmsService] OTP gönderildi:", response.data);
      return {
        success: true,
        ...response.data,
      };
    } catch (error) {
      console.error("[SmsService] OTP gönderme hatası:", error);
      const data = error.response?.data || {};
      return {
        success: false,
        message: data.message || data.Message || "SMS gönderilemedi",
        errorCode: data.errorCode,
        retryAfterSeconds: data.retryAfterSeconds,
        remainingDailyCount: data.remainingDailyCount,
      };
    }
  },

  /**
   * OTP kodunu doğrular
   * @param {string} phoneNumber - Telefon numarası
   * @param {string} code - 6 haneli kod
   * @param {number} purpose - Doğrulama amacı
   * @returns {Promise<Object>} { success, message, remainingAttempts }
   */
  async verifyOtp(
    phoneNumber,
    code,
    purpose = SmsVerificationPurpose.Registration
  ) {
    try {
      const response = await api.post("/api/sms/verify-otp", {
        phoneNumber,
        code,
        purpose,
      });
      console.log("[SmsService] OTP doğrulandı:", response.data);
      return {
        success: true,
        ...response.data,
      };
    } catch (error) {
      console.error("[SmsService] OTP doğrulama hatası:", error);
      const data = error.response?.data || {};
      return {
        success: false,
        message: data.message || data.Message || "Doğrulama başarısız",
        errorCode: data.errorCode,
        remainingAttempts: data.remainingAttempts,
      };
    }
  },

  /**
   * OTP kodunu tekrar gönderir
   * @param {string} phoneNumber - Telefon numarası
   * @param {number} purpose - Doğrulama amacı
   * @returns {Promise<Object>}
   */
  async resendOtp(phoneNumber, purpose = SmsVerificationPurpose.Registration) {
    try {
      const response = await api.post("/api/sms/resend-otp", {
        phoneNumber,
        purpose,
      });
      console.log("[SmsService] OTP tekrar gönderildi:", response.data);
      return {
        success: true,
        ...response.data,
      };
    } catch (error) {
      console.error("[SmsService] OTP resend hatası:", error);
      const data = error.response?.data || {};
      return {
        success: false,
        message: data.message || data.Message || "SMS gönderilemedi",
        errorCode: data.errorCode,
        retryAfterSeconds: data.retryAfterSeconds,
      };
    }
  },

  /**
   * Belirli bir telefon için doğrulama durumunu sorgular
   * @param {string} phoneNumber - Telefon numarası
   * @param {number} purpose - Doğrulama amacı
   * @returns {Promise<Object>} Status bilgisi
   */
  async getStatus(phoneNumber, purpose = SmsVerificationPurpose.Registration) {
    try {
      const response = await api.get(
        `/api/sms/status/${encodeURIComponent(phoneNumber)}`,
        {
          params: { purpose },
        }
      );
      return {
        success: true,
        ...response.data,
      };
    } catch (error) {
      console.error("[SmsService] Status kontrolü hatası:", error);
      return {
        success: false,
        hasActiveVerification: false,
      };
    }
  },

  /**
   * OTP gönderebilir mi kontrol eder
   * @param {string} phoneNumber - Telefon numarası
   * @returns {Promise<Object>} { canSend, retryAfterSeconds, remainingDailyCount }
   */
  async canSendOtp(phoneNumber) {
    try {
      const response = await api.get("/api/sms/can-send", {
        params: { phone: phoneNumber },
      });
      return {
        success: true,
        canSend: response.data.canSend,
        reason: response.data.reason,
        retryAfterSeconds: response.data.retryAfterSeconds,
        remainingDailyCount: response.data.remainingDailyCount,
        isBlocked: response.data.isBlocked,
      };
    } catch (error) {
      console.error("[SmsService] Can send kontrolü hatası:", error);
      return {
        success: false,
        canSend: false,
      };
    }
  },

  // ======= AUTH İLE ENTEGRE METODLAR =======

  /**
   * Telefon numarası ile kayıt başlatır
   * @param {Object} userData - { email, password, firstName, lastName, phoneNumber }
   * @returns {Promise<Object>} { success, message, userId }
   */
  async registerWithPhone(userData) {
    try {
      const response = await api.post(
        "/api/auth/register-with-phone",
        userData
      );
      console.log("[SmsService] Kayıt başlatıldı:", response.data);
      return {
        success: true,
        ...response.data,
      };
    } catch (error) {
      console.error("[SmsService] Kayıt hatası:", error);
      const data = error.response?.data || {};
      return {
        success: false,
        message: data.message || data.Message || "Kayıt başarısız",
      };
    }
  },

  /**
   * Telefon doğrulama ile kayıt tamamlar
   * @param {string} phoneNumber - Telefon numarası
   * @param {string} code - 6 haneli kod
   * @param {string} email - Email adresi
   * @returns {Promise<Object>} { success, message, token, refreshToken }
   */
  async verifyPhoneRegistration(phoneNumber, code, email) {
    try {
      const response = await api.post("/api/auth/verify-phone-registration", {
        phoneNumber,
        code,
        email,
      });
      console.log("[SmsService] Kayıt doğrulandı:", response.data);
      return {
        success: true,
        ...response.data,
      };
    } catch (error) {
      console.error("[SmsService] Kayıt doğrulama hatası:", error);
      const data = error.response?.data || {};
      return {
        success: false,
        message: data.message || data.Message || "Doğrulama başarısız",
      };
    }
  },

  /**
   * Telefon ile şifre sıfırlama kodu gönderir
   * @param {string} phoneNumber - Telefon numarası
   * @returns {Promise<Object>} { success, message }
   */
  async forgotPasswordByPhone(phoneNumber) {
    try {
      const response = await api.post("/api/auth/forgot-password-by-phone", {
        phoneNumber,
      });
      console.log("[SmsService] Şifre sıfırlama kodu gönderildi");
      return {
        success: true,
        ...response.data,
      };
    } catch (error) {
      console.error("[SmsService] Şifre sıfırlama hatası:", error);
      const data = error.response?.data || {};
      return {
        success: false,
        message: data.message || data.Message || "İşlem başarısız",
      };
    }
  },

  /**
   * Telefon ile şifre sıfırlar
   * @param {string} phoneNumber - Telefon numarası
   * @param {string} code - 6 haneli kod
   * @param {string} newPassword - Yeni şifre
   * @param {string} confirmPassword - Şifre tekrar
   * @returns {Promise<Object>} { success, message }
   */
  async resetPasswordByPhone(phoneNumber, code, newPassword, confirmPassword) {
    try {
      const response = await api.post("/api/auth/reset-password-by-phone", {
        phoneNumber,
        code,
        newPassword,
        confirmPassword,
      });
      console.log("[SmsService] Şifre sıfırlandı");
      return {
        success: true,
        ...response.data,
      };
    } catch (error) {
      console.error("[SmsService] Şifre sıfırlama hatası:", error);
      const data = error.response?.data || {};
      return {
        success: false,
        message: data.message || data.Message || "Şifre sıfırlama başarısız",
      };
    }
  },
};

// Eski API uyumluluğu için (backward compatibility)
const otpService = {
  sendOtp: (phoneNumber) =>
    smsService.sendOtp(phoneNumber, SmsVerificationPurpose.Registration),
  verifyOtp: (phoneNumber, code) =>
    smsService.verifyOtp(
      phoneNumber,
      code,
      SmsVerificationPurpose.Registration
    ),
  canSendOtp: (phoneNumber) =>
    smsService.canSendOtp(phoneNumber).then((r) => r.canSend),
};

export { smsService };
export default otpService;
