// ==========================================================================
// cartSettingsService.js - Sepet Ayarları Frontend Servisi
// ==========================================================================
// Minimum sepet tutarı ayarlarını API'den çeker ve cache'ler.
// Public ve admin endpoint'lerine erişim sağlar.
// ==========================================================================

import api from "./api";

// ═══════════════════════════════════════════════════════════════════════════════
// CLIENT-SIDE CACHE
// Gereksiz API çağrılarını önlemek için kısa süreli cache
// ═══════════════════════════════════════════════════════════════════════════════
let settingsCache = { data: null, timestamp: null };
const CACHE_TTL = 5 * 60 * 1000; // 5 dakika

const isCacheValid = () => {
  return (
    settingsCache.data !== null &&
    settingsCache.timestamp !== null &&
    Date.now() - settingsCache.timestamp < CACHE_TTL
  );
};

// ═══════════════════════════════════════════════════════════════════════════════
// PUBLIC ENDPOINT'LER
// Sepet ve checkout sayfaları için
// ═══════════════════════════════════════════════════════════════════════════════

/**
 * Aktif sepet ayarlarını getirir (cache destekli).
 * Sepet ve checkout sayfalarında minimum tutar kontrolü için kullanılır.
 * @param {boolean} forceRefresh - Cache'i atlayarak taze veri çek
 * @returns {Promise<Object>} Sepet ayarları
 */
export const getCartSettings = async (forceRefresh = false) => {
  // Cache geçerliyse ve zorunlu yenileme istenmiyorsa cache'den döndür
  if (!forceRefresh && isCacheValid()) {
    return settingsCache.data;
  }

  try {
    const response = await api.get("/api/CartSettings/settings");
    // api.js interceptor'ı zaten res.data döndürür, tekrar .data yapmaya gerek yok
    const data = response?.data || response;

    // Cache'e kaydet
    settingsCache = { data, timestamp: Date.now() };

    return data;
  } catch (error) {
    console.warn("[CartSettings] Ayarlar yüklenemedi:", error.message);
    // Hata durumunda güvenli varsayılan döndür (minimum tutar pasif)
    return {
      id: 0,
      minimumCartAmount: 0,
      isMinimumCartAmountActive: false,
      minimumCartAmountMessage:
        "Sipariş verebilmek için sepet tutarınız en az {amount} TL olmalıdır.",
      isActive: true,
      updatedAt: null,
      updatedByUserName: null,
    };
  }
};

// ═══════════════════════════════════════════════════════════════════════════════
// ADMIN ENDPOINT'LER
// Admin paneli sepet ayarları yönetimi için
// ═══════════════════════════════════════════════════════════════════════════════

/**
 * Admin paneli için sepet ayarlarını getirir (cache'siz, taze veri).
 * @returns {Promise<Object>} Sepet ayarları
 */
export const getCartSettingsAdmin = async () => {
  const response = await api.get("/api/CartSettings/admin/settings");
  return response?.data || response;
};

/**
 * Sepet ayarlarını günceller (admin yetkisi gerekli).
 * Güncelleme sonrası client cache temizlenir.
 * @param {Object} updateData - Güncellenecek alanlar
 * @param {number} [updateData.minimumCartAmount] - Minimum sepet tutarı (TL)
 * @param {boolean} [updateData.isMinimumCartAmountActive] - Aktif/pasif durumu
 * @param {string} [updateData.minimumCartAmountMessage] - Uyarı mesajı
 * @returns {Promise<Object>} Güncelleme sonucu ve güncel ayarlar
 */
export const updateCartSettings = async (updateData) => {
  const response = await api.put("/api/CartSettings/admin/settings", updateData);

  // Güncelleme sonrası cache'i temizle
  clearCartSettingsCache();

  return response?.data || response;
};

// ═══════════════════════════════════════════════════════════════════════════════
// YARDIMCI FONKSİYONLAR
// ═══════════════════════════════════════════════════════════════════════════════

/**
 * Client-side cache'i temizler.
 * Ayar güncellemesi sonrası çağrılmalı.
 */
export const clearCartSettingsCache = () => {
  settingsCache = { data: null, timestamp: null };
};

/**
 * TL para birimi formatı.
 * @param {number} amount - Formatlanacak tutar
 * @returns {string} Formatlanmış tutar (örn: "150,00 ₺")
 */
export const formatCurrency = (amount) => {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: "TRY",
    minimumFractionDigits: 2,
  }).format(amount || 0);
};

const cartSettingsService = {
  getCartSettings,
  getCartSettingsAdmin,
  updateCartSettings,
  clearCartSettingsCache,
  formatCurrency,
};

export default cartSettingsService;
