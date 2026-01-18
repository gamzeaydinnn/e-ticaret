// ============================================================
// VARYANT SERVİSİ - Product Variant API Entegrasyonu
// ============================================================
// Bu servis, ürün varyantları (SKU bazlı stok birimleri) için
// tüm API çağrılarını yönetir. Variant = satın alınabilen birim.
// ============================================================

import api from "./api";

// ============================================================
// VARYANT CRUD İŞLEMLERİ
// ============================================================

/**
 * Bir ürünün tüm varyantlarını getirir
 * @param {number} productId - Ürün ID
 * @returns {Promise<Array>} Varyant listesi
 */
export const getVariantsByProduct = async (productId) => {
  try {
    const response = await api.get(`/api/productvariants/product/${productId}`);
    return response.data || [];
  } catch (error) {
    console.error("Varyantlar yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Tek bir varyantı ID ile getirir
 * @param {number} variantId - Varyant ID
 * @returns {Promise<Object>} Varyant detayları
 */
export const getVariantById = async (variantId) => {
  try {
    const response = await api.get(`/api/productvariants/${variantId}`);
    return response.data;
  } catch (error) {
    console.error("Varyant yüklenirken hata:", error);
    throw error;
  }
};

/**
 * SKU ile varyant getirir
 * @param {string} sku - Varyant SKU kodu
 * @returns {Promise<Object>} Varyant detayları
 */
export const getVariantBySku = async (sku) => {
  try {
    const response = await api.get(
      `/api/productvariants/sku/${encodeURIComponent(sku)}`,
    );
    return response.data;
  } catch (error) {
    console.error("SKU ile varyant aranırken hata:", error);
    throw error;
  }
};

/**
 * Yeni varyant oluşturur
 * @param {number} productId - Ürün ID
 * @param {Object} variantData - Varyant verileri
 * @returns {Promise<Object>} Oluşturulan varyant
 */
export const createVariant = async (productId, variantData) => {
  try {
    const payload = {
      productId,
      variant: {
        sku: variantData.sku,
        title: variantData.title || variantData.sku,
        price: parseFloat(variantData.price) || 0,
        stock: parseInt(variantData.stock) || 0,
        currency: variantData.currency || "TRY",
        barcode: variantData.barcode || null,
        weightGrams: variantData.weightGrams
          ? parseInt(variantData.weightGrams)
          : null,
        volumeML: variantData.volumeML ? parseInt(variantData.volumeML) : null,
        supplierCode: variantData.supplierCode || null,
        parentSku: variantData.parentSku || null,
      },
    };

    const response = await api.post("/api/productvariants", payload);
    return response.data;
  } catch (error) {
    console.error("Varyant oluşturulurken hata:", error);
    throw error;
  }
};

/**
 * Toplu varyant oluşturur
 * @param {number} productId - Ürün ID
 * @param {Array} variants - Varyant listesi
 * @returns {Promise<Object>} Oluşturma sonucu
 */
export const createVariantsBulk = async (productId, variants) => {
  try {
    const payload = {
      productId,
      variants: variants.map((v) => ({
        sku: v.sku,
        title: v.title || v.sku,
        price: parseFloat(v.price) || 0,
        stock: parseInt(v.stock) || 0,
        currency: v.currency || "TRY",
        barcode: v.barcode || null,
        weightGrams: v.weightGrams ? parseInt(v.weightGrams) : null,
        volumeML: v.volumeML ? parseInt(v.volumeML) : null,
        supplierCode: v.supplierCode || null,
        parentSku: v.parentSku || null,
      })),
    };

    const response = await api.post("/api/productvariants/bulk", payload);
    return response.data;
  } catch (error) {
    console.error("Toplu varyant oluşturulurken hata:", error);
    throw error;
  }
};

/**
 * Varyant günceller
 * @param {number} variantId - Varyant ID
 * @param {Object} updateData - Güncellenecek veriler
 * @returns {Promise<Object>} Güncellenen varyant
 */
export const updateVariant = async (variantId, updateData) => {
  try {
    const payload = {
      title: updateData.title,
      price:
        updateData.price !== undefined
          ? parseFloat(updateData.price)
          : undefined,
      stock:
        updateData.stock !== undefined ? parseInt(updateData.stock) : undefined,
      currency: updateData.currency,
      barcode: updateData.barcode,
      weightGrams:
        updateData.weightGrams !== undefined
          ? parseInt(updateData.weightGrams)
          : undefined,
      volumeML:
        updateData.volumeML !== undefined
          ? parseInt(updateData.volumeML)
          : undefined,
      supplierCode: updateData.supplierCode,
    };

    // undefined değerleri temizle
    Object.keys(payload).forEach((key) => {
      if (payload[key] === undefined) delete payload[key];
    });

    const response = await api.put(
      `/api/productvariants/${variantId}`,
      payload,
    );
    return response.data;
  } catch (error) {
    console.error("Varyant güncellenirken hata:", error);
    throw error;
  }
};

/**
 * Varyant siler (soft delete)
 * @param {number} variantId - Varyant ID
 * @returns {Promise<Object>} Silme sonucu
 */
export const deleteVariant = async (variantId) => {
  try {
    const response = await api.delete(`/api/productvariants/${variantId}`);
    return response.data;
  } catch (error) {
    console.error("Varyant silinirken hata:", error);
    throw error;
  }
};

// ============================================================
// STOK YÖNETİMİ
// ============================================================

/**
 * Varyant stok günceller
 * @param {number} variantId - Varyant ID
 * @param {number} quantity - Yeni stok miktarı
 * @returns {Promise<Object>} Güncelleme sonucu
 */
export const updateVariantStock = async (variantId, quantity) => {
  try {
    const response = await api.patch(
      `/api/productvariants/${variantId}/stock`,
      {
        quantity: parseInt(quantity),
      },
    );
    return response.data;
  } catch (error) {
    console.error("Stok güncellenirken hata:", error);
    throw error;
  }
};

/**
 * Toplu stok günceller (SKU bazlı)
 * @param {Object} skuStockMap - { sku: quantity } formatında
 * @returns {Promise<Object>} Güncelleme sonucu
 */
export const bulkUpdateStock = async (skuStockMap) => {
  try {
    const response = await api.patch(
      "/api/productvariants/stock/bulk",
      skuStockMap,
    );
    return response.data;
  } catch (error) {
    console.error("Toplu stok güncellenirken hata:", error);
    throw error;
  }
};

/**
 * Düşük stoklu varyantları getirir
 * @param {number} threshold - Stok eşiği (varsayılan: 10)
 * @returns {Promise<Array>} Düşük stoklu varyantlar
 */
export const getLowStockVariants = async (threshold = 10) => {
  try {
    const response = await api.get(
      `/api/productvariants/admin/low-stock?threshold=${threshold}`,
    );
    return response.data || [];
  } catch (error) {
    console.error("Düşük stoklu varyantlar yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Varyant istatistiklerini getirir
 * @param {number|null} feedSourceId - Opsiyonel feed filtresi
 * @returns {Promise<Object>} İstatistikler
 */
export const getVariantStatistics = async (feedSourceId = null) => {
  try {
    const url = feedSourceId
      ? `/api/productvariants/admin/statistics?feedSourceId=${feedSourceId}`
      : "/api/productvariants/admin/statistics";
    const response = await api.get(url);
    return response.data;
  } catch (error) {
    console.error("Varyant istatistikleri yüklenirken hata:", error);
    throw error;
  }
};

// ============================================================
// HELPER FUNCTIONS
// ============================================================

/**
 * Varyant verilerini frontend formatına dönüştürür
 * @param {Object} variant - Backend'den gelen varyant
 * @returns {Object} Frontend formatında varyant
 */
export const mapVariant = (variant) => {
  if (!variant) return null;

  return {
    id: variant.id,
    productId: variant.productId,
    sku: variant.sku || "",
    title: variant.title || variant.sku || "",
    price: parseFloat(variant.price) || 0,
    stock: parseInt(variant.stock) || 0,
    currency: variant.currency || "TRY",
    barcode: variant.barcode || "",
    weightGrams: variant.weightGrams || null,
    volumeML: variant.volumeML || null,
    supplierCode: variant.supplierCode || "",
    parentSku: variant.parentSku || "",
    isActive: variant.isActive !== false,
    lastSyncedAt: variant.lastSyncedAt ? new Date(variant.lastSyncedAt) : null,
    lastSeenAt: variant.lastSeenAt ? new Date(variant.lastSeenAt) : null,
    optionValues: variant.optionValues || [],
    // Fiyat formatı
    formattedPrice: formatPrice(variant.price, variant.currency),
    // Stok durumu
    stockStatus: getStockStatus(variant.stock),
    inStock: (variant.stock || 0) > 0,
  };
};

/**
 * Fiyatı para birimi ile formatlar
 * @param {number} price - Fiyat
 * @param {string} currency - Para birimi
 * @returns {string} Formatlanmış fiyat
 */
export const formatPrice = (price, currency = "TRY") => {
  const numPrice = parseFloat(price) || 0;

  const formatters = {
    TRY: new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY" }),
    USD: new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }),
    EUR: new Intl.NumberFormat("de-DE", { style: "currency", currency: "EUR" }),
  };

  const formatter = formatters[currency] || formatters.TRY;
  return formatter.format(numPrice);
};

/**
 * Stok durumunu belirler
 * @param {number} stock - Stok miktarı
 * @returns {Object} Stok durumu bilgisi
 */
export const getStockStatus = (stock) => {
  const qty = parseInt(stock) || 0;

  if (qty === 0) {
    return { status: "out", label: "Tükendi", color: "red", icon: "❌" };
  } else if (qty <= 5) {
    return {
      status: "low",
      label: "Son Birkaç Ürün",
      color: "orange",
      icon: "⚠️",
    };
  } else if (qty <= 20) {
    return { status: "medium", label: "Stokta", color: "yellow", icon: "📦" };
  } else {
    return { status: "high", label: "Bol Stok", color: "green", icon: "✅" };
  }
};

// Default export
export default {
  getVariantsByProduct,
  getVariantById,
  getVariantBySku,
  createVariant,
  createVariantsBulk,
  updateVariant,
  deleteVariant,
  updateVariantStock,
  bulkUpdateStock,
  getLowStockVariants,
  getVariantStatistics,
  mapVariant,
  formatPrice,
  getStockStatus,
};
