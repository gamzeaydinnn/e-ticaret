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
  // GÜVENLİK: Ana token httpOnly cookie'de tutulur (backend tarafından set edilir)
  // localStorage sadece yedek/geriye uyumluluk için kullanılır
  // Cookie'ler JavaScript'ten erişilemez = XSS koruması
  // ============================================================================
  saveToken: (token) => {
    // GÜVENLİK NOTU: Ana JWT token artık httpOnly cookie'de
    // localStorage sadece geriye uyumluluk ve bazı edge case'ler için
    // Yeni sistemde bu fonksiyon genellikle çağrılmaz (backend cookie set eder)
    localStorage.setItem("token", token);
    localStorage.setItem("authToken", token); // AdminGuard uyumlu
    localStorage.setItem("adminToken", token); // Eski kod uyumlu

    // Yedek olarak header'a da ekle (cookie yoksa kullanılır)
    api.defaults.headers.common["Authorization"] = `Bearer ${token}`;

    if (process.env.NODE_ENV !== "production") {
      console.log("[AuthService] ✅ Token kaydedildi (yedek localStorage)");
    }
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

    // NOT: httpOnly cookie backend tarafından /api/auth/logout çağrısında silinir
    if (process.env.NODE_ENV !== "production") {
      console.log("[AuthService] ✅ Token localStorage'dan kaldırıldı");
    }
  },

  getToken: () => {
    // GÜVENLİK NOTU: Ana token httpOnly cookie'de - JS erişemez
    // Bu fonksiyon sadece geriye uyumluluk için localStorage'a bakar
    // Yeni sistemde token cookie üzerinden otomatik gönderilir
    return (
      localStorage.getItem("token") ||
      localStorage.getItem("authToken") ||
      localStorage.getItem("adminToken")
    );
  },

  // Yardımcı: Kullanıcının giriş yapmış olup olmadığını kontrol et
  isAuthenticated: () => {
    // localStorage'ta token varsa veya cookie set edilmişse giriş yapılmıştır
    // Cookie'yi JS'ten okuyamayız, bu yüzden /api/auth/me endpoint'ini kullanın
    return !!AuthService.getToken();
  },

  // Token'ı API'ye otomatik eklemek için (yedek mekanizma)
  setupTokenInterceptor: () => {
    const token = AuthService.getToken();
    if (token) {
      api.defaults.headers.common["Authorization"] = `Bearer ${token}`;
    }
  },
};
