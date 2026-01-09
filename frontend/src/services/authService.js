// src/services/authService.js
// Kimlik doğrulama servisi - E-posta ve SMS tabanlı yetkilendirme işlemleri
import api from "./api";

export const AuthService = {
  // ==================== E-POSTA TABANLI KİMLİK DOĞRULAMA ====================
  login: (credentials) => api.post("/auth/login", credentials),
  register: (data) => api.post("/auth/register", data),
  logout: () => api.post("/auth/logout"),
  me: () => api.get("/auth/me"),
  refresh: (data) => api.post("/auth/refresh", data),
  socialLogin: (data) => api.post("/auth/social-login", data),

  // ==================== SMS TABANLI KİMLİK DOĞRULAMA ====================
  
  /**
   * Telefon numarası ile kayıt başlat
   * @param {Object} data - { phoneNumber, firstName, lastName, password }
   * @returns {Promise} - SMS gönderim sonucu
   */
  registerWithPhone: (data) => api.post("/auth/register-with-phone", data),

  /**
   * Telefon kayıt doğrulama
   * @param {Object} data - { phoneNumber, code }
   * @returns {Promise} - JWT token ve kullanıcı bilgileri
   */
  verifyPhoneRegistration: (data) => api.post("/auth/verify-phone-registration", data),

  /**
   * Telefon ile şifre sıfırlama isteği
   * @param {Object} data - { phoneNumber }
   * @returns {Promise} - SMS gönderim sonucu
   */
  forgotPasswordByPhone: (data) => api.post("/auth/forgot-password-by-phone", data),

  /**
   * Telefon ile şifre sıfırlama işlemi
   * @param {Object} data - { phoneNumber, code, newPassword }
   * @returns {Promise} - Başarı durumu
   */
  resetPasswordByPhone: (data) => api.post("/auth/reset-password-by-phone", data),

  // helper client-side
  saveToken: (token) => {
    localStorage.setItem("token", token);
    // Token'ı API header'ına da ekle
    api.defaults.headers.common["Authorization"] = `Bearer ${token}`;
  },

  removeToken: () => {
    localStorage.removeItem("token");
    // API header'ından da kaldır
    delete api.defaults.headers.common["Authorization"];
  },

  getToken: () => localStorage.getItem("token"),

  // Token'ı API'ye otomatik eklemek için
  setupTokenInterceptor: () => {
    const token = AuthService.getToken();
    if (token) {
      api.defaults.headers.common["Authorization"] = `Bearer ${token}`;
    }
  },
};
