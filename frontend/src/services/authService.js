// src/services/authService.js
// Kimlik doğrulama servisi - E-posta ve SMS tabanlı yetkilendirme işlemleri
import api from "./api";

export const AuthService = {
  // ==================== E-POSTA TABANLI KİMLİK DOĞRULAMA ====================
  login: (credentials) => api.post("/api/auth/login", credentials),
  register: (data) => api.post("/api/auth/register", data),
  logout: () => api.post("/api/auth/logout"),
  me: () => api.get("/api/auth/me"),
  refresh: (data) => api.post("/api/auth/refresh", data),
  socialLogin: (data) => api.post("/api/auth/social-login", data),

  // ==================== SMS TABANLI KİMLİK DOĞRULAMA ====================

  /**
   * Telefon numarası ile kayıt başlat
   * @param {Object} data - { phoneNumber, firstName, lastName, password }
   * @returns {Promise} - SMS gönderim sonucu
   */
  registerWithPhone: (data) => api.post("/api/auth/register-with-phone", data),

  /**
   * Telefon kayıt doğrulama
   * @param {Object} data - { phoneNumber, code }
   * @returns {Promise} - JWT token ve kullanıcı bilgileri
   */
  verifyPhoneRegistration: (data) =>
    api.post("/api/auth/verify-phone-registration", data),

  /**
   * Telefon ile şifre sıfırlama isteği
   * @param {Object} data - { phoneNumber }
   * @returns {Promise} - SMS gönderim sonucu
   */
  forgotPasswordByPhone: (data) =>
    api.post("/api/auth/forgot-password-by-phone", data),

  /**
   * Telefon ile şifre sıfırlama işlemi
   * @param {Object} data - { phoneNumber, code, newPassword }
   * @returns {Promise} - Başarı durumu
   */
  resetPasswordByPhone: (data) =>
    api.post("/api/auth/reset-password-by-phone", data),

  // ============================================================================
  // TOKEN YÖNETİMİ
  // Tüm olası key'lere yazar - geriye dönük uyumluluk için
  // api.js interceptor'u bu key'lerden birini okur
  // ============================================================================
  saveToken: (token) => {
    // Tüm olası key'lere yaz (uyumluluk için)
    localStorage.setItem("token", token);
    localStorage.setItem("authToken", token); // AdminGuard uyumlu
    localStorage.setItem("adminToken", token); // Eski kod uyumlu

    // Token'ı API header'ına da ekle (hemen geçerli olsun)
    api.defaults.headers.common["Authorization"] = `Bearer ${token}`;

    console.log("[AuthService] ✅ Token tüm key'lere kaydedildi");
  },

  removeToken: () => {
    // Tüm key'lerden kaldır
    localStorage.removeItem("token");
    localStorage.removeItem("authToken");
    localStorage.removeItem("adminToken");
    localStorage.removeItem("user");
    localStorage.removeItem("userId");

    // API header'ından da kaldır
    delete api.defaults.headers.common["Authorization"];

    console.log("[AuthService] ✅ Token tüm key'lerden kaldırıldı");
  },

  getToken: () => {
    // Tüm olası key'leri kontrol et (api.js ile senkron)
    return (
      localStorage.getItem("token") ||
      localStorage.getItem("authToken") ||
      localStorage.getItem("adminToken")
    );
  },

  // Token'ı API'ye otomatik eklemek için
  setupTokenInterceptor: () => {
    const token = AuthService.getToken();
    if (token) {
      api.defaults.headers.common["Authorization"] = `Bearer ${token}`;
    }
  },
};
