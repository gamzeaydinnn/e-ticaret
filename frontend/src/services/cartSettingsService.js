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
const CART_SETTINGS_UPDATED_EVENT = "cart-settings-updated";

const DEFAULT_CART_SETTINGS = {
  id: 0,
  minimumCartAmount: 0,
  isMinimumCartAmountActive: false,
  minimumCartAmountMessage:
    "Sipariş verebilmek için sepet tutarınız en az {amount} TL olmalıdır.",
  guestFirstOrderShippingMessage:
    "Hesap oluştur, ilk alışverişinde kargo bedava!",
  isActive: true,
  updatedAt: null,
  updatedByUserName: null,
};

const hasCartSettingsShape = (value) =>
  Boolean(value) &&
  typeof value === "object" &&
  [
    "minimumCartAmount",
    "isMinimumCartAmountActive",
    "minimumCartAmountMessage",
    "guestFirstOrderShippingMessage",
  ].some((key) => Object.prototype.hasOwnProperty.call(value, key));

const normalizeCartSettings = (payload) => {
  const data = hasCartSettingsShape(payload?.data)
    ? payload.data
    : hasCartSettingsShape(payload?.settings)
      ? payload.settings
      : hasCartSettingsShape(payload)
        ? payload
        : null;
  if (!data || typeof data !== "object") {
    return { ...DEFAULT_CART_SETTINGS };
  }

  return {
    ...DEFAULT_CART_SETTINGS,
    ...data,
    minimumCartAmount: Number(data.minimumCartAmount ?? 0),
    isMinimumCartAmountActive: Boolean(data.isMinimumCartAmountActive),
    minimumCartAmountMessage:
      data.minimumCartAmountMessage ??
      DEFAULT_CART_SETTINGS.minimumCartAmountMessage,
    guestFirstOrderShippingMessage:
      data.guestFirstOrderShippingMessage ??
      DEFAULT_CART_SETTINGS.guestFirstOrderShippingMessage,
  };
};

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
    const data = normalizeCartSettings(response);

    // Cache'e kaydet
    settingsCache = { data, timestamp: Date.now() };

    return data;
  } catch (error) {
    console.warn("[CartSettings] Ayarlar yüklenemedi:", error.message);
    // Hata durumunda güvenli varsayılan döndür (minimum tutar pasif)
    return { ...DEFAULT_CART_SETTINGS };
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
  return normalizeCartSettings(response);
};

/**
 * Sepet ayarlarını günceller (admin yetkisi gerekli).
 * Güncelleme sonrası client cache temizlenir.
 * @param {Object} updateData - Güncellenecek alanlar
 * @param {number} [updateData.minimumCartAmount] - Minimum sepet tutarı (TL)
 * @param {boolean} [updateData.isMinimumCartAmountActive] - Aktif/pasif durumu
 * @param {string} [updateData.minimumCartAmountMessage] - Uyarı mesajı
 * @param {string} [updateData.guestFirstOrderShippingMessage] - Misafir promosyon mesajı
 * @returns {Promise<Object>} Güncelleme sonucu ve güncel ayarlar
 */
export const updateCartSettings = async (updateData) => {
  const response = await api.put("/api/CartSettings/admin/settings", updateData);
  const normalizedResponse = normalizeCartSettings(response);
  const normalized = {
    ...(settingsCache.data || DEFAULT_CART_SETTINGS),
    ...normalizedResponse,
    ...(updateData || {}),
  };

  settingsCache = { data: normalized, timestamp: Date.now() };
  if (typeof window !== "undefined") {
    window.dispatchEvent(
      new CustomEvent(CART_SETTINGS_UPDATED_EVENT, { detail: normalized }),
    );
  }

  return normalized;
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

export const CART_SETTINGS_EVENT = CART_SETTINGS_UPDATED_EVENT;

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
