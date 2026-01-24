/**
 * newsletterService.js - Newsletter (Bülten) API Servisi
 *
 * Bu servis, newsletter abonelik sisteminin frontend-backend iletişimini sağlar.
 *
 * KULLANIM ALANLARI:
 * - Ana sayfa footer'daki "Bültenimize Katılın" formu
 * - Admin paneli newsletter yönetim sayfası
 * - Toplu mail gönderim işlemleri
 *
 * PUBLIC ENDPOINT'LER (Authentication gerektirmez):
 * - POST /api/newsletter/subscribe     - Bültene abone ol
 * - GET  /api/newsletter/unsubscribe   - Token ile abonelikten çık
 * - POST /api/newsletter/unsubscribe   - Form ile abonelikten çık
 *
 * ADMIN ENDPOINT'LERİ (Admin yetkisi gerektirir):
 * - GET    /api/admin/newsletter           - Abone listesi (sayfalı)
 * - GET    /api/admin/newsletter/stats     - İstatistikler
 * - GET    /api/admin/newsletter/{id}      - Tek abone detayı
 * - DELETE /api/admin/newsletter/{id}      - Abone sil (GDPR)
 * - POST   /api/admin/newsletter/send      - Toplu mail gönder
 * - POST   /api/admin/newsletter/send-test - Test mail gönder
 *
 * @author Senior Developer
 * @version 1.0.0
 */

import api from "./api";

// ============================================
// SABİTLER VE YAPILANDIRMA
// ============================================

/**
 * Abonelik kaynakları
 * Analitik ve segmentasyon için kullanılır
 */
export const SUBSCRIPTION_SOURCES = {
  WEB_FOOTER: "web_footer",
  WEB_POPUP: "web_popup",
  MOBILE_APP: "mobile_app",
  CHECKOUT: "checkout",
  ADMIN_IMPORT: "admin_import",
};

/**
 * Sıralama seçenekleri
 * Admin panelinde abone listesi sıralaması için
 */
export const SORT_OPTIONS = [
  { value: "SubscribedAt", label: "Abonelik Tarihi" },
  { value: "Email", label: "E-posta" },
  { value: "FullName", label: "İsim" },
  { value: "Source", label: "Kaynak" },
  { value: "EmailsSentCount", label: "Gönderilen Mail Sayısı" },
];

/**
 * Varsayılan sayfalama ayarları
 */
export const DEFAULT_PAGE_SIZE = 20;
export const MAX_PAGE_SIZE = 100;

// ============================================
// PUBLIC FONKSİYONLAR (Authentication gerektirmez)
// ============================================

/**
 * Bültene abone olma
 *
 * Ana sayfa footer'daki form tarafından çağrılır.
 * Email validasyonu backend'de yapılır.
 *
 * @param {Object} data - Abonelik verileri
 * @param {string} data.email - E-posta adresi (zorunlu)
 * @param {string} [data.fullName] - Tam isim (opsiyonel)
 * @param {string} [data.source] - Abonelik kaynağı (varsayılan: web_footer)
 * @returns {Promise<Object>} Abonelik sonucu
 * @throws {Error} Ağ hatası veya validasyon hatası
 *
 * @example
 * const result = await subscribe({ email: 'test@example.com', fullName: 'Test User' });
 * if (result.success) {
 *   console.log('Abonelik başarılı:', result.message);
 * }
 */
export const subscribe = async (data) => {
  try {
    // Email'i normalize et (lowercase, trim)
    const normalizedData = {
      email: data.email?.toLowerCase().trim(),
      fullName: data.fullName?.trim() || null,
      source: data.source || SUBSCRIPTION_SOURCES.WEB_FOOTER,
    };

    // Validasyon
    if (!normalizedData.email) {
      return {
        success: false,
        message: "E-posta adresi gereklidir.",
      };
    }

    // Basit email format kontrolü (detaylı validasyon backend'de)
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(normalizedData.email)) {
      return {
        success: false,
        message: "Geçerli bir e-posta adresi giriniz.",
      };
    }

    const response = await api.post(
      "/api/newsletter/subscribe",
      normalizedData,
    );
    return response;
  } catch (error) {
    console.error("[NewsletterService] Subscribe error:", error);

    // Backend'den gelen hata mesajını döndür
    if (error.response?.data?.message) {
      return {
        success: false,
        message: error.response.data.message,
      };
    }

    // Genel hata mesajı
    return {
      success: false,
      message:
        "Abonelik işlemi sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
    };
  }
};

/**
 * Token ile abonelikten çıkma
 *
 * Mail içindeki "Abonelikten Çık" linki tarafından çağrılır.
 * GDPR uyumlu - login gerektirmez.
 *
 * @param {string} token - Benzersiz abonelik iptal token'ı
 * @returns {Promise<Object>} İptal sonucu
 *
 * @example
 * const result = await unsubscribeByToken('abc123...');
 */
export const unsubscribeByToken = async (token) => {
  try {
    if (!token) {
      return {
        success: false,
        message: "Geçersiz abonelik iptal linki.",
      };
    }

    const response = await api.get(
      `/api/newsletter/unsubscribe?token=${encodeURIComponent(token)}`,
    );
    return response;
  } catch (error) {
    console.error("[NewsletterService] Unsubscribe by token error:", error);

    if (error.response?.data?.message) {
      return {
        success: false,
        message: error.response.data.message,
      };
    }

    return {
      success: false,
      message: "Abonelik iptal işlemi sırasında bir hata oluştu.",
    };
  }
};

/**
 * Form ile abonelikten çıkma
 *
 * @param {Object} data - İptal verileri
 * @param {string} data.token - Abonelik iptal token'ı
 * @param {string} [data.reason] - İptal sebebi (opsiyonel, analitik için)
 * @returns {Promise<Object>} İptal sonucu
 */
export const unsubscribeByForm = async (data) => {
  try {
    if (!data.token) {
      return {
        success: false,
        message: "Geçersiz abonelik iptal isteği.",
      };
    }

    const response = await api.post("/api/newsletter/unsubscribe", data);
    return response;
  } catch (error) {
    console.error("[NewsletterService] Unsubscribe by form error:", error);

    if (error.response?.data?.message) {
      return {
        success: false,
        message: error.response.data.message,
      };
    }

    return {
      success: false,
      message: "Abonelik iptal işlemi sırasında bir hata oluştu.",
    };
  }
};

// ============================================
// ADMIN FONKSİYONLARI (Admin yetkisi gerektirir)
// ============================================

/**
 * Abone listesini getirir (sayfalı)
 *
 * Admin panelinde abone listesi görüntülemek için kullanılır.
 * Filtreleme ve sıralama destekler.
 *
 * @param {Object} [params] - Sorgu parametreleri
 * @param {number} [params.page=1] - Sayfa numarası
 * @param {number} [params.pageSize=20] - Sayfa başına kayıt
 * @param {string} [params.search] - Email veya isim araması
 * @param {boolean} [params.isActive] - Aktiflik filtresi
 * @param {string} [params.source] - Kaynak filtresi
 * @param {string} [params.sortBy='SubscribedAt'] - Sıralama alanı
 * @param {boolean} [params.sortDescending=true] - Azalan sıralama
 * @returns {Promise<Object>} Sayfalı abone listesi
 *
 * @example
 * const result = await getSubscribers({ page: 1, search: 'test@', isActive: true });
 * console.log(result.items, result.totalCount);
 */
export const getSubscribers = async (params = {}) => {
  try {
    const queryParams = new URLSearchParams();

    // Sayfalama
    queryParams.append("page", params.page || 1);
    queryParams.append(
      "pageSize",
      Math.min(params.pageSize || DEFAULT_PAGE_SIZE, MAX_PAGE_SIZE),
    );

    // Filtreleme
    if (params.search) {
      queryParams.append("search", params.search);
    }
    if (params.isActive !== undefined && params.isActive !== null) {
      queryParams.append("isActive", params.isActive);
    }
    if (params.source) {
      queryParams.append("source", params.source);
    }

    // Sıralama
    queryParams.append("sortBy", params.sortBy || "SubscribedAt");
    queryParams.append("sortDescending", params.sortDescending !== false);

    const response = await api.get(
      `/api/admin/newsletter?${queryParams.toString()}`,
    );
    return response;
  } catch (error) {
    console.error("[NewsletterService] Get subscribers error:", error);
    throw error;
  }
};

/**
 * Newsletter istatistiklerini getirir
 *
 * Admin dashboard için özet bilgiler.
 *
 * @returns {Promise<Object>} İstatistikler
 *
 * @example
 * const stats = await getStatistics();
 * console.log(stats.activeSubscribers, stats.newSubscribersLast7Days);
 */
export const getStatistics = async () => {
  try {
    const response = await api.get("/api/admin/newsletter/stats");
    return response;
  } catch (error) {
    console.error("[NewsletterService] Get statistics error:", error);
    throw error;
  }
};

/**
 * Tek bir aboneyi ID ile getirir
 *
 * @param {number} id - Abone ID
 * @returns {Promise<Object>} Abone detayları
 */
export const getSubscriberById = async (id) => {
  try {
    const response = await api.get(`/api/admin/newsletter/${id}`);
    return response;
  } catch (error) {
    console.error("[NewsletterService] Get subscriber by ID error:", error);
    throw error;
  }
};

/**
 * Abone kaydını siler (GDPR "Unutulma Hakkı")
 *
 * DİKKAT: Bu işlem geri alınamaz!
 *
 * @param {number} id - Silinecek abone ID
 * @returns {Promise<Object>} İşlem sonucu
 */
export const deleteSubscriber = async (id) => {
  try {
    const response = await api.delete(`/api/admin/newsletter/${id}`);
    return response;
  } catch (error) {
    console.error("[NewsletterService] Delete subscriber error:", error);
    throw error;
  }
};

/**
 * Toplu mail gönderimi
 *
 * Tüm aktif abonelere veya filtrelenmiş listeye mail gönderir.
 * Mailler kuyruğa eklenir ve arka planda asenkron işlenir.
 *
 * @param {Object} data - Mail verileri
 * @param {string} data.subject - Mail konusu
 * @param {string} data.body - Mail içeriği (HTML destekli)
 * @param {boolean} [data.isHtml=true] - HTML formatında mı
 * @param {boolean} [data.isTestMode=false] - Test modu
 * @param {string[]} [data.testEmails] - Test modunda gönderilecek adresler
 * @param {string[]} [data.sourceFilter] - Kaynak filtresi
 * @param {Date} [data.subscribedAfter] - Minimum abonelik tarihi
 * @param {Date} [data.subscribedBefore] - Maksimum abonelik tarihi
 * @returns {Promise<Object>} Gönderim sonucu
 *
 * @example
 * const result = await sendBulkEmail({
 *   subject: 'Yeni Ürünlerimiz',
 *   body: '<h1>Merhaba!</h1><p>Yeni ürünlerimize göz atın...</p>',
 *   isHtml: true
 * });
 */
export const sendBulkEmail = async (data) => {
  try {
    // Validasyon
    if (!data.subject?.trim()) {
      return {
        success: false,
        message: "Mail konusu gereklidir.",
      };
    }
    if (!data.body?.trim()) {
      return {
        success: false,
        message: "Mail içeriği gereklidir.",
      };
    }

    const payload = {
      subject: data.subject.trim(),
      body: data.body,
      isHtml: data.isHtml !== false,
      isTestMode: data.isTestMode || false,
      testEmails: data.testEmails || null,
      sourceFilter: data.sourceFilter || null,
      subscribedAfter: data.subscribedAfter || null,
      subscribedBefore: data.subscribedBefore || null,
    };

    const response = await api.post("/api/admin/newsletter/send", payload);
    return response;
  } catch (error) {
    console.error("[NewsletterService] Send bulk email error:", error);

    if (error.response?.data?.message) {
      return {
        success: false,
        message: error.response.data.message,
      };
    }

    return {
      success: false,
      message: error?.message || "Toplu mail gönderimi sırasında bir hata oluştu.",
    };
  }
};

/**
 * Test mail gönderimi
 *
 * Gerçek gönderimden önce önizleme için kullanılır.
 *
 * @param {Object} data - Mail verileri
 * @param {string} data.subject - Mail konusu
 * @param {string} data.body - Mail içeriği
 * @param {string[]} data.testEmails - Test email adresleri
 * @param {boolean} [data.isHtml=true] - HTML formatında mı
 * @returns {Promise<Object>} Gönderim sonucu
 */
export const sendTestEmail = async (data) => {
  try {
    if (!data.testEmails?.length) {
      return {
        success: false,
        message: "Test için en az bir e-posta adresi gereklidir.",
      };
    }

    const payload = {
      subject: data.subject,
      body: data.body,
      isHtml: data.isHtml !== false,
      isTestMode: true,
      testEmails: data.testEmails,
    };

    const response = await api.post("/api/admin/newsletter/send-test", payload);
    return response;
  } catch (error) {
    console.error("[NewsletterService] Send test email error:", error);

    if (error.response?.data?.message) {
      return {
        success: false,
        message: error.response.data.message,
      };
    }

    return {
      success: false,
      message: error?.message || "Test maili gönderilirken bir hata oluştu.",
    };
  }
};

// ============================================
// YARDIMCI FONKSİYONLAR
// ============================================

/**
 * Tarihi okunabilir formata çevirir
 *
 * @param {string|Date} date - Tarih
 * @returns {string} Formatlanmış tarih (örn: "24 Ocak 2026, 10:30")
 */
export const formatDate = (date) => {
  if (!date) return "-";

  const d = new Date(date);
  if (isNaN(d.getTime())) return "-";

  return d.toLocaleString("tr-TR", {
    day: "numeric",
    month: "long",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};

/**
 * Kaynak adını Türkçe'ye çevirir
 *
 * @param {string} source - Kaynak kodu
 * @returns {string} Türkçe kaynak adı
 */
export const getSourceLabel = (source) => {
  const labels = {
    web_footer: "Web Sitesi (Footer)",
    web_popup: "Web Sitesi (Popup)",
    mobile_app: "Mobil Uygulama",
    checkout: "Ödeme Sayfası",
    admin_import: "Admin İçe Aktarma",
  };
  return labels[source] || source || "Bilinmiyor";
};

/**
 * Abone durumunu badge olarak döndürür
 *
 * @param {boolean} isActive - Aktiflik durumu
 * @returns {Object} Badge bilgileri
 */
export const getStatusBadge = (isActive) => {
  return isActive
    ? { text: "Aktif", color: "success", icon: "check-circle" }
    : { text: "Pasif", color: "secondary", icon: "times-circle" };
};

// Default export - tüm fonksiyonları obje olarak export et
const newsletterService = {
  // Public
  subscribe,
  unsubscribeByToken,
  unsubscribeByForm,
  // Admin
  getSubscribers,
  getStatistics,
  getSubscriberById,
  deleteSubscriber,
  sendBulkEmail,
  sendTestEmail,
  // Helpers
  formatDate,
  getSourceLabel,
  getStatusBadge,
  // Constants
  SUBSCRIPTION_SOURCES,
  SORT_OPTIONS,
  DEFAULT_PAGE_SIZE,
  MAX_PAGE_SIZE,
};

export default newsletterService;
