// ============================================================
// XML IMPORT SERVİSİ - Tedarikçi XML Feed Entegrasyonu
// ============================================================
// Bu servis, XML feed'lerden ürün/varyant import işlemlerini yönetir.
// URL veya dosya üzerinden import, önizleme ve mapping destekler.
// ============================================================

import api from "./api";

// ============================================================
// FEED SOURCE YÖNETİMİ
// ============================================================

/**
 * Tüm feed kaynaklarını listeler
 * @returns {Promise<Array>} Feed kaynakları listesi
 */
export const getAllFeedSources = async () => {
  try {
    const response = await api.get("/api/xml-feeds");
    return response.data || [];
  } catch (error) {
    console.error("Feed kaynakları yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Aktif feed kaynaklarını listeler
 * @returns {Promise<Array>} Aktif feed kaynakları
 */
export const getActiveFeedSources = async () => {
  try {
    const response = await api.get("/api/xml-feeds/active");
    return response.data || [];
  } catch (error) {
    console.error("Aktif feed kaynakları yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Feed kaynağı detayını getirir
 * @param {number} feedId - Feed ID
 * @returns {Promise<Object>} Feed detayları
 */
export const getFeedSourceById = async (feedId) => {
  try {
    const response = await api.get(`/api/xml-feeds/${feedId}`);
    return response.data;
  } catch (error) {
    console.error("Feed kaynağı yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Yeni feed kaynağı oluşturur
 * @param {Object} feedData - Feed bilgileri
 * @returns {Promise<Object>} Oluşturulan feed
 */
export const createFeedSource = async (feedData) => {
  try {
    const payload = {
      name: feedData.name,
      url: feedData.url,
      supplierName: feedData.supplierName || null,
      mappingConfig: feedData.mappingConfig
        ? JSON.stringify(feedData.mappingConfig)
        : null,
      isActive: feedData.isActive !== false,
      autoSyncEnabled: feedData.autoSyncEnabled || false,
      syncIntervalMinutes: feedData.syncIntervalMinutes || 60,
    };

    const response = await api.post("/api/xml-feeds", payload);
    return response.data;
  } catch (error) {
    console.error("Feed kaynağı oluşturulurken hata:", error);
    throw error;
  }
};

/**
 * Feed kaynağını günceller
 * @param {number} feedId - Feed ID
 * @param {Object} updateData - Güncellenecek veriler
 * @returns {Promise<Object>} Güncellenen feed
 */
export const updateFeedSource = async (feedId, updateData) => {
  try {
    const payload = {
      name: updateData.name,
      url: updateData.url,
      supplierName: updateData.supplierName,
      mappingConfig: updateData.mappingConfig
        ? JSON.stringify(updateData.mappingConfig)
        : null,
      isActive: updateData.isActive,
      autoSyncEnabled: updateData.autoSyncEnabled,
      syncIntervalMinutes: updateData.syncIntervalMinutes,
    };

    const response = await api.put(`/api/xml-feeds/${feedId}`, payload);
    return response.data;
  } catch (error) {
    console.error("Feed kaynağı güncellenirken hata:", error);
    throw error;
  }
};

/**
 * Feed kaynağını siler
 * @param {number} feedId - Feed ID
 * @returns {Promise<Object>} Silme sonucu
 */
export const deleteFeedSource = async (feedId) => {
  try {
    const response = await api.delete(`/api/xml-feeds/${feedId}`);
    return response.data;
  } catch (error) {
    console.error("Feed kaynağı silinirken hata:", error);
    throw error;
  }
};

/**
 * Feed durumunu getirir
 * @param {number} feedId - Feed ID
 * @returns {Promise<Object>} Feed durumu
 */
export const getFeedStatus = async (feedId) => {
  try {
    const response = await api.get(`/api/xml-feeds/${feedId}/status`);
    return response.data;
  } catch (error) {
    console.error("Feed durumu yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Feed aktif/pasif durumunu değiştirir
 * @param {number} feedId - Feed ID
 * @param {boolean} isActive - Aktif durumu
 * @returns {Promise<Object>} İşlem sonucu
 */
export const setFeedActiveStatus = async (feedId, isActive) => {
  try {
    const response = await api.patch(`/api/xml-feeds/${feedId}/active`, {
      isActive,
    });
    return response.data;
  } catch (error) {
    console.error("Feed durumu değiştirilirken hata:", error);
    throw error;
  }
};

// ============================================================
// MAPPING KONFIGÜRASYONU
// ============================================================

/**
 * Feed mapping konfigürasyonunu getirir
 * @param {number} feedId - Feed ID
 * @returns {Promise<Object>} Mapping konfigürasyonu
 */
export const getFeedMappingConfig = async (feedId) => {
  try {
    const response = await api.get(`/api/xml-feeds/${feedId}/mapping`);
    return response.data;
  } catch (error) {
    console.error("Mapping konfigürasyonu yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Mapping konfigürasyonunu günceller
 * @param {number} feedId - Feed ID
 * @param {Object} config - Mapping konfigürasyonu
 * @returns {Promise<Object>} İşlem sonucu
 */
export const updateFeedMappingConfig = async (feedId, config) => {
  try {
    const response = await api.put(`/api/xml-feeds/${feedId}/mapping`, config);
    return response.data;
  } catch (error) {
    console.error("Mapping konfigürasyonu güncellenirken hata:", error);
    throw error;
  }
};

/**
 * Otomatik mapping önerisi alır
 * @param {number} feedId - Feed ID
 * @returns {Promise<Object>} Önerilen mapping
 */
export const suggestMappingConfig = async (feedId) => {
  try {
    const response = await api.get(`/api/xml-feeds/${feedId}/suggest-mapping`);
    return response.data;
  } catch (error) {
    console.error("Mapping önerisi alınırken hata:", error);
    throw error;
  }
};

// ============================================================
// IMPORT İŞLEMLERİ
// ============================================================

/**
 * Feed'i manuel senkronize eder
 * @param {number} feedId - Feed ID
 * @returns {Promise<Object>} Import sonucu
 */
export const syncFeed = async (feedId) => {
  try {
    const response = await api.post(`/api/xml-feeds/${feedId}/sync`);
    return response.data;
  } catch (error) {
    console.error("Feed senkronizasyonu sırasında hata:", error);
    throw error;
  }
};

/**
 * URL'den import yapar
 * @param {string} url - XML URL
 * @param {Object} mapping - Mapping konfigürasyonu
 * @returns {Promise<Object>} Import sonucu
 */
export const importFromUrl = async (url, mapping) => {
  try {
    const response = await api.post("/api/xml-feeds/import/url", {
      url,
      mapping,
    });
    return response.data;
  } catch (error) {
    console.error("URL import sırasında hata:", error);
    throw error;
  }
};

/**
 * Dosyadan import yapar
 * @param {File} file - XML dosyası
 * @param {Object} mapping - Mapping konfigürasyonu
 * @param {number|null} feedSourceId - Opsiyonel feed ID
 * @returns {Promise<Object>} Import sonucu
 */
export const importFromFile = async (file, mapping, feedSourceId = null) => {
  try {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("mappingJson", JSON.stringify(mapping));
    if (feedSourceId) {
      formData.append("feedSourceId", feedSourceId.toString());
    }

    const response = await api.post("/api/xml-feeds/import/file", formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });
    return response.data;
  } catch (error) {
    console.error("Dosya import sırasında hata:", error);
    throw error;
  }
};

// ============================================================
// ÖNİZLEME VE DOĞRULAMA
// ============================================================

/**
 * Feed önizlemesi yapar
 * @param {number} feedId - Feed ID
 * @param {number} sampleSize - Örnek sayısı
 * @returns {Promise<Object>} Önizleme sonucu
 */
export const previewFeed = async (feedId, sampleSize = 5) => {
  try {
    const response = await api.get(
      `/api/xml-feeds/${feedId}/preview?sampleSize=${sampleSize}`,
    );
    return response.data;
  } catch (error) {
    console.error("Feed önizlemesi sırasında hata:", error);
    throw error;
  }
};

/**
 * Import önizlemesi yapar
 * @param {number} feedId - Feed ID
 * @param {number} sampleSize - Örnek sayısı
 * @returns {Promise<Object>} Import önizleme sonucu
 */
export const previewImport = async (feedId, sampleSize = 10) => {
  try {
    const response = await api.get(
      `/api/xml-feeds/${feedId}/import-preview?sampleSize=${sampleSize}`,
    );
    return response.data;
  } catch (error) {
    console.error("Import önizlemesi sırasında hata:", error);
    throw error;
  }
};

/**
 * Feed URL'sini doğrular
 * @param {string} url - Doğrulanacak URL
 * @returns {Promise<Object>} Doğrulama sonucu
 */
export const validateFeedUrl = async (url) => {
  try {
    const response = await api.post("/api/xml-feeds/validate-url", { url });
    return response.data;
  } catch (error) {
    console.error("URL doğrulama sırasında hata:", error);
    throw error;
  }
};

// ============================================================
// IMPORT PROGRESS & HISTORY
// ============================================================

/**
 * Aktif import işlemlerini listeler
 * @returns {Promise<Array>} Aktif import listesi
 */
export const getActiveImports = async () => {
  try {
    const response = await api.get("/api/xml-feeds/imports/active");
    return response.data || [];
  } catch (error) {
    console.error("Aktif importlar yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Import durumunu getirir
 * @param {string} importId - Import ID
 * @returns {Promise<Object>} Import durumu
 */
export const getImportProgress = async (importId) => {
  try {
    const response = await api.get(`/api/xml-feeds/imports/${importId}`);
    return response.data;
  } catch (error) {
    console.error("Import durumu yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Import işlemini iptal eder
 * @param {string} importId - Import ID
 * @returns {Promise<Object>} İptal sonucu
 */
export const cancelImport = async (importId) => {
  try {
    const response = await api.post(
      `/api/xml-feeds/imports/${importId}/cancel`,
    );
    return response.data;
  } catch (error) {
    console.error("Import iptali sırasında hata:", error);
    throw error;
  }
};

/**
 * Import geçmişini listeler
 * @param {number|null} feedSourceId - Opsiyonel feed filtresi
 * @param {number} page - Sayfa numarası
 * @param {number} pageSize - Sayfa boyutu
 * @returns {Promise<Array>} Import geçmişi
 */
export const getImportHistory = async (
  feedSourceId = null,
  page = 1,
  pageSize = 20,
) => {
  try {
    let url = `/api/xml-feeds/imports/history?page=${page}&pageSize=${pageSize}`;
    if (feedSourceId) {
      url += `&feedSourceId=${feedSourceId}`;
    }
    const response = await api.get(url);
    return response.data || [];
  } catch (error) {
    console.error("Import geçmişi yüklenirken hata:", error);
    throw error;
  }
};

// ============================================================
// TEMİZLİK İŞLEMLERİ
// ============================================================

/**
 * Eski ürünleri temizler (deaktif eder)
 * @param {number} feedId - Feed ID
 * @param {number} hoursThreshold - Saat eşiği
 * @returns {Promise<Object>} Temizlik sonucu
 */
export const cleanupStaleProducts = async (feedId, hoursThreshold = 48) => {
  try {
    const response = await api.post(
      `/api/xml-feeds/${feedId}/cleanup?hoursThreshold=${hoursThreshold}`,
    );
    return response.data;
  } catch (error) {
    console.error("Temizlik işlemi sırasında hata:", error);
    throw error;
  }
};

// ============================================================
// VARSAYILAN MAPPING ŞABLONLARI
// ============================================================

/**
 * Varsayılan mapping şablonu döndürür
 * @returns {Object} Varsayılan mapping
 */
export const getDefaultMappingTemplate = () => ({
  rootElement: "Products",
  itemElement: "Product",
  namespace: null,
  encoding: "UTF-8",
  skuMapping: "SKU",
  titleMapping: "ProductName",
  priceMapping: "Price",
  stockMapping: "StockQuantity",
  barcodeMapping: "Barcode",
  parentSkuMapping: "GroupCode",
  categoryPathMapping: "Category",
  brandMapping: "Brand",
  imageUrlMapping: "ImageURL",
  descriptionMapping: "Description",
  weightMapping: "Weight",
  volumeMapping: "Volume",
  optionMappings: {},
});

/**
 * Yaygın tedarikçi mapping şablonları
 */
export const SUPPLIER_TEMPLATES = {
  generic: {
    name: "Genel XML",
    mapping: getDefaultMappingTemplate(),
  },
  trendyol: {
    name: "Trendyol",
    mapping: {
      ...getDefaultMappingTemplate(),
      rootElement: "products",
      itemElement: "product",
      skuMapping: "stockCode",
      titleMapping: "title",
      priceMapping: "salePrice",
      stockMapping: "quantity",
    },
  },
  hepsiburada: {
    name: "Hepsiburada",
    mapping: {
      ...getDefaultMappingTemplate(),
      rootElement: "Items",
      itemElement: "Item",
      skuMapping: "MerchantSku",
      titleMapping: "ProductName",
      priceMapping: "Price",
      stockMapping: "Stock",
    },
  },
  n11: {
    name: "N11",
    mapping: {
      ...getDefaultMappingTemplate(),
      rootElement: "products",
      itemElement: "product",
      skuMapping: "sellerCode",
      titleMapping: "title",
      priceMapping: "displayPrice",
      stockMapping: "stockQuantity",
    },
  },
};

// Default export
export default {
  // Feed Source
  getAllFeedSources,
  getActiveFeedSources,
  getFeedSourceById,
  createFeedSource,
  updateFeedSource,
  deleteFeedSource,
  getFeedStatus,
  setFeedActiveStatus,

  // Mapping
  getFeedMappingConfig,
  updateFeedMappingConfig,
  suggestMappingConfig,

  // Import
  syncFeed,
  importFromUrl,
  importFromFile,

  // Preview & Validation
  previewFeed,
  previewImport,
  validateFeedUrl,

  // Progress & History
  getActiveImports,
  getImportProgress,
  cancelImport,
  getImportHistory,

  // Cleanup
  cleanupStaleProducts,

  // Templates
  getDefaultMappingTemplate,
  SUPPLIER_TEMPLATES,
};
