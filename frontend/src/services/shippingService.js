// ==========================================================================
// shippingService.js - Kargo Ücreti API Servisi
// ==========================================================================
// Kargo ayarları için API iletişimini yönetir.
// Public endpoint'ler müşteri sepeti için, admin endpoint'leri yönetim için.
// Cache mekanizması ile gereksiz API çağrılarını önler.
// ==========================================================================

import api from "./api";

// ============================================
// SABİTLER VE YAPILANDIRMA
// ============================================

/**
 * Araç tipleri için görsel ve metin bilgileri
 * UI'da kullanıcıya göstermek için kullanılır
 */
export const VEHICLE_TYPES = {
  motorcycle: {
    key: "motorcycle",
    icon: "fa-motorcycle",
    label: "Motosiklet",
    description: "Hızlı teslimat, küçük paketler için ideal",
    color: "#ff6b35", // Turuncu
    bgColor: "#fff5f0",
  },
  car: {
    key: "car",
    icon: "fa-car",
    label: "Araç",
    description: "Büyük paketler ve ağır ürünler için uygun",
    color: "#2196f3", // Mavi
    bgColor: "#e3f2fd",
  },
};

/**
 * Cache yapılandırması
 * Kargo fiyatları sık değişmeyeceği için cache kullanıyoruz
 */
const CACHE_CONFIG = {
  ttlMs: 5 * 60 * 1000, // 5 dakika cache süresi
};

// ============================================
// CACHE MEKANİZMASI
// ============================================

/**
 * Basit in-memory cache
 * Sayfa yenilenene kadar geçerli
 */
let settingsCache = {
  data: null,
  timestamp: null,
};

/**
 * Cache'in geçerli olup olmadığını kontrol eder
 */
const isCacheValid = () => {
  if (!settingsCache.data || !settingsCache.timestamp) return false;
  const elapsed = Date.now() - settingsCache.timestamp;
  return elapsed < CACHE_CONFIG.ttlMs;
};

/**
 * Cache'i temizler
 * Admin güncelleme sonrası çağrılmalı
 */
export const clearShippingCache = () => {
  settingsCache = { data: null, timestamp: null };
  console.log("[ShippingService] 🗑️ Cache temizlendi");
};

// ============================================
// PUBLIC API FONKSİYONLARI (Herkes Erişebilir)
// ============================================

/**
 * Aktif kargo seçeneklerini getirir
 * Müşteri sepet sayfası için kullanılır
 *
 * @param {boolean} forceRefresh - Cache'i bypass etmek için true
 * @returns {Promise<Array>} Kargo seçenekleri listesi
 *
 * @example
 * const settings = await getActiveSettings();
 * // [{ id: 1, vehicleType: "motorcycle", price: 40, ... }, ...]
 */
export const getActiveSettings = async (forceRefresh = false) => {
  try {
    // Cache kontrolü
    if (!forceRefresh && isCacheValid()) {
      console.log("[ShippingService] 📦 Cache'den döndürülüyor");
      return settingsCache.data;
    }

    console.log("[ShippingService] 🌐 API'den kargo ayarları çekiliyor...");
    const response = await api.get("/api/shipping/settings");

    // Response unwrap (api.js interceptor'ı zaten data döndürüyor)
    const data = Array.isArray(response)
      ? response
      : response?.data || response || [];

    // Cache'e kaydet
    settingsCache = {
      data: data,
      timestamp: Date.now(),
    };

    console.log(
      "[ShippingService] ✅ Kargo ayarları yüklendi:",
      data.length,
      "seçenek",
    );
    return data;
  } catch (error) {
    console.error("[ShippingService] ❌ Kargo ayarları yüklenemedi:", error);

    // Hata durumunda varsayılan değerler döndür (graceful degradation)
    return getDefaultSettings();
  }
};

/**
 * Belirli bir araç tipinin fiyatını getirir
 *
 * @param {string} vehicleType - "motorcycle" veya "car"
 * @returns {Promise<number|null>} Kargo ücreti veya null
 */
export const getPriceByVehicleType = async (vehicleType) => {
  if (!vehicleType) {
    console.warn("[ShippingService] ⚠️ vehicleType parametresi boş");
    return null;
  }

  try {
    // Önce cache'den kontrol et
    if (isCacheValid() && settingsCache.data) {
      const setting = settingsCache.data.find(
        (s) => s.vehicleType?.toLowerCase() === vehicleType.toLowerCase(),
      );
      if (setting) {
        return setting.price;
      }
    }

    // Cache'de yoksa API'den çek
    const response = await api.get(`/api/shipping/price/${vehicleType}`);
    return response?.price ?? null;
  } catch (error) {
    console.error(
      "[ShippingService] ❌ Kargo fiyatı alınamadı:",
      vehicleType,
      error,
    );

    // Hata durumunda varsayılan fiyat döndür
    return getDefaultPriceByType(vehicleType);
  }
};

/**
 * Araç tipine göre detaylı kargo bilgisi getirir
 *
 * @param {string} vehicleType - "motorcycle" veya "car"
 * @returns {Promise<Object|null>} Kargo ayarı detayı
 */
export const getSettingByVehicleType = async (vehicleType) => {
  try {
    // Önce tüm ayarları çek (cache'li)
    const settings = await getActiveSettings();
    return (
      settings.find(
        (s) => s.vehicleType?.toLowerCase() === vehicleType.toLowerCase(),
      ) || null
    );
  } catch (error) {
    console.error(
      "[ShippingService] ❌ Kargo ayarı alınamadı:",
      vehicleType,
      error,
    );
    return null;
  }
};

// ============================================
// ADMIN API FONKSİYONLARI (Yetkilendirme Gerekli)
// ============================================

/**
 * Tüm kargo ayarlarını getirir (aktif/pasif dahil)
 * Admin paneli için kullanılır
 *
 * @returns {Promise<Array>} Tüm kargo ayarları
 */
export const getAllSettingsAdmin = async () => {
  try {
    console.log("[ShippingService] 🔧 [ADMIN] Tüm kargo ayarları çekiliyor...");
    const response = await api.get("/api/shipping/admin/settings");
    const data = Array.isArray(response)
      ? response
      : response?.data || response || [];
    console.log(
      "[ShippingService] ✅ [ADMIN] Kargo ayarları yüklendi:",
      data.length,
    );
    return data;
  } catch (error) {
    console.error(
      "[ShippingService] ❌ [ADMIN] Kargo ayarları yüklenemedi:",
      error,
    );
    throw error;
  }
};

/**
 * Kargo ayarını günceller
 *
 * @param {number} id - Güncellenecek ayar ID'si
 * @param {Object} updateData - Güncellenecek veriler
 * @param {number} [updateData.price] - Yeni fiyat
 * @param {string} [updateData.displayName] - Görüntüleme adı
 * @param {string} [updateData.estimatedDeliveryTime] - Tahmini süre
 * @param {string} [updateData.description] - Açıklama
 * @param {number} [updateData.sortOrder] - Sıralama
 * @param {boolean} [updateData.isActive] - Aktif durumu
 * @returns {Promise<Object>} Güncellenmiş ayar
 */
export const updateSetting = async (id, updateData) => {
  if (!id) {
    throw new Error("Güncelleme için ID gerekli");
  }

  try {
    console.log(
      "[ShippingService] 🔧 [ADMIN] Kargo ayarı güncelleniyor:",
      id,
      updateData,
    );
    const response = await api.put(
      `/api/shipping/admin/settings/${id}`,
      updateData,
    );

    // Cache'i temizle (güncel veri için)
    clearShippingCache();

    console.log("[ShippingService] ✅ [ADMIN] Kargo ayarı güncellendi:", id);
    return response?.data || response;
  } catch (error) {
    console.error(
      "[ShippingService] ❌ [ADMIN] Kargo ayarı güncellenemedi:",
      id,
      error,
    );
    throw error;
  }
};

/**
 * Kargo ayarının aktif/pasif durumunu değiştirir
 *
 * @param {number} id - Ayar ID'si
 * @param {boolean} isActive - Yeni durum
 * @returns {Promise<Object>} API response
 */
export const toggleActive = async (id, isActive) => {
  try {
    console.log(
      "[ShippingService] 🔧 [ADMIN] Aktiflik değiştiriliyor:",
      id,
      isActive,
    );
    const response = await api.patch(
      `/api/shipping/admin/settings/${id}/toggle`,
      { isActive },
    );

    // Cache'i temizle
    clearShippingCache();

    return response;
  } catch (error) {
    console.error(
      "[ShippingService] ❌ [ADMIN] Aktiflik değiştirilemedi:",
      id,
      error,
    );
    throw error;
  }
};

// ============================================
// YARDIMCI FONKSİYONLAR
// ============================================

/**
 * API erişilemez olduğunda varsayılan ayarları döndürür
 * Graceful degradation için
 */
const getDefaultSettings = () => {
  console.warn("[ShippingService] ⚠️ Varsayılan kargo ayarları kullanılıyor");
  return [
    {
      id: 1,
      vehicleType: "motorcycle",
      displayName: "Motosiklet ile Teslimat",
      price: 40,
      estimatedDeliveryTime: "30-45 dakika",
      description: "Hızlı teslimat, küçük ve orta boy paketler için ideal",
      sortOrder: 1,
      isActive: true,
    },
    {
      id: 2,
      vehicleType: "car",
      displayName: "Araç ile Teslimat",
      price: 60,
      estimatedDeliveryTime: "1-2 saat",
      description: "Büyük paketler ve ağır ürünler için uygun",
      sortOrder: 2,
      isActive: true,
    },
  ];
};

/**
 * Araç tipine göre varsayılan fiyat döndürür
 */
const getDefaultPriceByType = (vehicleType) => {
  const defaults = {
    motorcycle: 40,
    car: 60,
  };
  return defaults[vehicleType?.toLowerCase()] || 40;
};

/**
 * Kargo ücretini formatlar (TL)
 *
 * @param {number} price - Fiyat
 * @returns {string} Formatlanmış fiyat (örn: "40,00 ₺")
 */
export const formatShippingPrice = (price) => {
  if (typeof price !== "number" || isNaN(price)) return "0,00 ₺";
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: "TRY",
    minimumFractionDigits: 2,
  }).format(price);
};

/**
 * Araç tipi için görsel bilgileri döndürür
 *
 * @param {string} vehicleType - "motorcycle" veya "car"
 * @returns {Object} Görsel bilgiler (icon, label, color vb.)
 */
export const getVehicleTypeInfo = (vehicleType) => {
  return VEHICLE_TYPES[vehicleType?.toLowerCase()] || VEHICLE_TYPES.motorcycle;
};

// ============================================
// DEFAULT EXPORT
// ============================================

const shippingService = {
  // Public
  getActiveSettings,
  getPriceByVehicleType,
  getSettingByVehicleType,

  // Admin
  getAllSettingsAdmin,
  updateSetting,
  toggleActive,

  // Helpers
  clearShippingCache,
  formatShippingPrice,
  getVehicleTypeInfo,

  // Constants
  VEHICLE_TYPES,
};

export default shippingService;
