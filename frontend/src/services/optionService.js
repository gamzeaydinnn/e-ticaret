// ============================================================
// OPTION SERVİSİ - Ürün Seçenekleri API Entegrasyonu
// ============================================================
// Bu servis, ürün seçenekleri (Renk, Beden, Hacim vb.) ve
// değerleri (Kırmızı, XL, 500ml vb.) için API çağrılarını yönetir.
// ============================================================

import api from "./api";

// ============================================================
// OPTION (SEÇENEK TÜRÜ) YÖNETİMİ
// ============================================================

/**
 * Tüm seçenek türlerini listeler
 * @returns {Promise<Array>} Seçenek türleri listesi
 */
export const getAllOptions = async () => {
  try {
    const response = await api.get("/api/product-options");
    return response.data || [];
  } catch (error) {
    console.error("Seçenek türleri yüklenirken hata:", error);
    throw error;
  }
};

/**
 * ID ile seçenek türü getirir
 * @param {number} optionId - Seçenek türü ID
 * @returns {Promise<Object>} Seçenek türü
 */
export const getOptionById = async (optionId) => {
  try {
    const response = await api.get(`/api/product-options/${optionId}`);
    return response.data;
  } catch (error) {
    console.error("Seçenek türü yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Yeni seçenek türü oluşturur (veya mevcut olanı getirir)
 * @param {string} name - Seçenek adı (örn: "Renk", "Beden")
 * @returns {Promise<Object>} Oluşturulan/mevcut seçenek
 */
export const createOption = async (name) => {
  try {
    const response = await api.post("/api/product-options", { name });
    return response.data;
  } catch (error) {
    console.error("Seçenek türü oluşturulurken hata:", error);
    throw error;
  }
};

/**
 * Seçenek türünü günceller
 * @param {number} optionId - Seçenek ID
 * @param {Object} updateData - Güncellenecek veriler
 * @returns {Promise<Object>} Güncellenen seçenek
 */
export const updateOption = async (optionId, updateData) => {
  try {
    const response = await api.put(
      `/api/product-options/${optionId}`,
      updateData,
    );
    return response.data;
  } catch (error) {
    console.error("Seçenek türü güncellenirken hata:", error);
    throw error;
  }
};

/**
 * Seçenek türünü siler
 * @param {number} optionId - Seçenek ID
 * @returns {Promise<Object>} Silme sonucu
 */
export const deleteOption = async (optionId) => {
  try {
    const response = await api.delete(`/api/product-options/${optionId}`);
    return response.data;
  } catch (error) {
    console.error("Seçenek türü silinirken hata:", error);
    throw error;
  }
};

// ============================================================
// OPTION VALUE (SEÇENEK DEĞERİ) YÖNETİMİ
// ============================================================

/**
 * Bir seçenek türünün tüm değerlerini listeler
 * @param {number} optionId - Seçenek türü ID
 * @returns {Promise<Array>} Değerler listesi
 */
export const getValuesByOptionId = async (optionId) => {
  try {
    const response = await api.get(`/api/product-options/${optionId}/values`);
    return response.data || [];
  } catch (error) {
    console.error("Seçenek değerleri yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Seçenek türüne yeni değer ekler
 * @param {number} optionId - Seçenek türü ID
 * @param {string} value - Değer (örn: "Kırmızı", "XL")
 * @returns {Promise<Object>} Oluşturulan değer
 */
export const addValueToOption = async (optionId, value) => {
  try {
    const response = await api.post(`/api/product-options/${optionId}/values`, {
      value,
    });
    return response.data;
  } catch (error) {
    console.error("Değer eklenirken hata:", error);
    throw error;
  }
};

/**
 * Seçenek türüne toplu değer ekler
 * @param {number} optionId - Seçenek türü ID
 * @param {Array<string>} values - Değerler listesi
 * @returns {Promise<Object>} İşlem sonucu
 */
export const addValuesToOption = async (optionId, values) => {
  try {
    const response = await api.post(
      `/api/product-options/${optionId}/values/bulk`,
      { values },
    );
    return response.data;
  } catch (error) {
    console.error("Toplu değer eklenirken hata:", error);
    throw error;
  }
};

/**
 * Seçenek değerini günceller
 * @param {number} valueId - Değer ID
 * @param {string} newValue - Yeni değer
 * @returns {Promise<Object>} Güncellenen değer
 */
export const updateValue = async (valueId, newValue) => {
  try {
    const response = await api.put(`/api/product-options/values/${valueId}`, {
      newValue,
    });
    return response.data;
  } catch (error) {
    console.error("Değer güncellenirken hata:", error);
    throw error;
  }
};

/**
 * Seçenek değerini siler
 * @param {number} valueId - Değer ID
 * @returns {Promise<Object>} Silme sonucu
 */
export const deleteValue = async (valueId) => {
  try {
    const response = await api.delete(`/api/product-options/values/${valueId}`);
    return response.data;
  } catch (error) {
    console.error("Değer silinirken hata:", error);
    throw error;
  }
};

// ============================================================
// ÜRÜN & KATEGORİ BAZLI SORGULAR
// ============================================================

/**
 * Bir ürün için kullanılan seçenekleri listeler
 * @param {number} productId - Ürün ID
 * @returns {Promise<Array>} Seçenekler listesi
 */
export const getOptionsForProduct = async (productId) => {
  try {
    const response = await api.get(
      `/api/product-options/by-product/${productId}`,
    );
    return response.data || [];
  } catch (error) {
    console.error("Ürün seçenekleri yüklenirken hata:", error);
    throw error;
  }
};

/**
 * Bir kategori için önerilen seçenekleri listeler
 * @param {number} categoryId - Kategori ID
 * @returns {Promise<Array>} Seçenekler listesi
 */
export const getOptionsForCategory = async (categoryId) => {
  try {
    const response = await api.get(
      `/api/product-options/by-category/${categoryId}`,
    );
    return response.data || [];
  } catch (error) {
    console.error("Kategori seçenekleri yüklenirken hata:", error);
    throw error;
  }
};

/**
 * En popüler seçenekleri listeler
 * @param {number} limit - Maksimum sonuç sayısı
 * @returns {Promise<Array>} Popüler seçenekler
 */
export const getMostUsedOptions = async (limit = 10) => {
  try {
    const response = await api.get(
      `/api/product-options/popular?limit=${limit}`,
    );
    return response.data || [];
  } catch (error) {
    console.error("Popüler seçenekler yüklenirken hata:", error);
    throw error;
  }
};

// ============================================================
// HELPER FUNCTIONS
// ============================================================

/**
 * Seçenek ve değerleri gruplar
 * @param {Array} options - Seçenek listesi
 * @returns {Object} Gruplandırılmış seçenekler
 */
export const groupOptionsByName = (options) => {
  if (!Array.isArray(options)) return {};

  return options.reduce((acc, option) => {
    acc[option.name] = {
      id: option.id,
      name: option.name,
      displayOrder: option.displayOrder || 0,
      values: option.values || [],
    };
    return acc;
  }, {});
};

/**
 * Seçenek değerlerinden seçim yapmak için dropdown options formatı oluşturur
 * @param {Array} values - Değerler listesi
 * @returns {Array} Dropdown options formatı
 */
export const formatValuesForDropdown = (values) => {
  if (!Array.isArray(values)) return [];

  return values.map((v) => ({
    value: v.id,
    label: v.value,
    data: v,
  }));
};

/**
 * Yaygın seçenek türleri
 */
export const COMMON_OPTIONS = [
  { name: "Renk", icon: "🎨" },
  { name: "Beden", icon: "📏" },
  { name: "Hacim", icon: "🧴" },
  { name: "Ağırlık", icon: "⚖️" },
  { name: "Materyal", icon: "🧵" },
  { name: "Aroma", icon: "🌸" },
  { name: "Paket", icon: "📦" },
];

// Default export
export default {
  // Option CRUD
  getAllOptions,
  getOptionById,
  createOption,
  updateOption,
  deleteOption,

  // Value CRUD
  getValuesByOptionId,
  addValueToOption,
  addValuesToOption,
  updateValue,
  deleteValue,

  // Query
  getOptionsForProduct,
  getOptionsForCategory,
  getMostUsedOptions,

  // Helpers
  groupOptionsByName,
  formatValuesForDropdown,
  COMMON_OPTIONS,
};
