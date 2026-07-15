/**
 * homeBlockService.js - Ana Sayfa Ürün Blokları API Servisi
 * ------------------------------------------------
 * Ana sayfadaki ürün bloklarını (İndirimli Ürünler, Süt Ürünleri vb.)
 * yöneten API servisi.
 *
 * Her blok:
 * - Sol tarafta poster/banner
 * - Sağ tarafta ürün kartları
 * - "Tümünü Gör" butonu
 *
 * Blok Tipleri:
 * - manual: Admin elle ürün seçer
 * - category: Kategori bazlı otomatik
 * - discounted: İndirimli ürünler (SpecialPrice olanlar)
 * - newest: En son eklenen ürünler
 * - bestseller: En çok satanlar
 *
 * Public Endpoint'ler:
 * - GET /api/homeblocks - Ana sayfa için aktif bloklar
 * - GET /api/homeblocks/{slug} - Tek blok (Tümünü Gör sayfası)
 * - GET /api/homeblocks/preview - Blok tipi önizlemesi
 *
 * Admin Endpoint'leri:
 * - GET /api/admin/homeblocks - Tüm bloklar (admin listesi)
 * - POST /api/admin/homeblocks - Yeni blok oluştur
 * - PUT /api/admin/homeblocks/{id} - Blok güncelle
 * - DELETE /api/admin/homeblocks/{id} - Blok sil
 * - PUT /api/admin/homeblocks/reorder - Sıralama değiştir
 *
 * Ürün Yönetimi (Admin):
 * - POST /api/admin/homeblocks/{id}/products - Ürün ekle
 * - DELETE /api/admin/homeblocks/{id}/products/{productId} - Ürün çıkar
 * - PUT /api/admin/homeblocks/{id}/products - Ürünleri güncelle
 * - PUT /api/admin/homeblocks/{id}/products/set - Ürün listesini değiştir
 *
 * @author Senior Developer
 * @version 1.0.0
 */

import api from "./api";

// ============================================
// SABITLER VE YAPILANDIRMA
// ============================================

/**
 * Blok tipleri ve açıklamaları (Array formatında - AdminHomeBlocks.jsx için)
 */
export const BLOCK_TYPES = [
  {
    value: "manual",
    label: "Manuel Seçim",
    description: "Ürünleri elle tek tek seçin",
    icon: "✋",
  },
  {
    value: "category",
    label: "Kategori Bazlı",
    description: "Seçilen kategorideki ürünler otomatik gelir",
    icon: "📁",
  },
  {
    value: "discounted",
    label: "İndirimli Ürünler",
    description: "İndirimli fiyatı olan ürünler otomatik gelir",
    icon: "🏷️",
  },
  {
    value: "newest",
    label: "Yeni Ürünler",
    description: "En son eklenen ürünler otomatik gelir",
    icon: "🆕",
  },
  {
    value: "bestseller",
    label: "Çok Satanlar",
    description: "En çok satılan ürünler otomatik gelir",
    icon: "🔥",
  },
];

/**
 * Blok poster boyutu önerisi
 */
export const POSTER_DIMENSIONS = {
  width: 400,
  height: 500,
  text: "400x500px",
  label: "Dikey Poster",
};

/**
 * Varsayılan arka plan renkleri
 */
export const BACKGROUND_COLORS = [
  { value: "#00BCD4", label: "Turkuaz", className: "bg-cyan-500" },
  { value: "#4CAF50", label: "Yeşil", className: "bg-green-500" },
  { value: "#FF5722", label: "Turuncu", className: "bg-orange-500" },
  { value: "#E91E63", label: "Pembe", className: "bg-pink-500" },
  { value: "#9C27B0", label: "Mor", className: "bg-purple-500" },
  { value: "#2196F3", label: "Mavi", className: "bg-blue-500" },
  { value: "#607D8B", label: "Gri-Mavi", className: "bg-slate-500" },
  { value: "#795548", label: "Kahverengi", className: "bg-amber-700" },
];

// ============================================
// PUBLIC API - ANA SAYFA İÇİN
// ============================================

/**
 * Ana sayfa için aktif blokları getirir
 * Her blok poster ve ürünleriyle birlikte döner
 *
 * @returns {Promise<Array>} Aktif blok listesi
 */
export const getActiveBlocks = async () => {
  try {
    console.log("📡 [HomeBlockService] API çağrısı: GET /api/homeblocks");
    const response = await api.get("/api/homeblocks");
    console.log("🏠 Ana sayfa blokları raw response:", response);

    // API { value: [...], Count: n } veya direkt array döndürebilir
    // Ayrıca $values formatı da olabilir (JSON reference handling)
    let blocks = response?.$values || response?.value || response || [];

    // Array kontrolü
    if (!Array.isArray(blocks)) {
      console.warn("⚠️ Bloklar array değil, boş array döndürülüyor:", blocks);
      return [];
    }

    // Her bloğun products alanını normalize et
    blocks = blocks.map((block) => {
      const products =
        block?.products?.$values ||
        block?.products ||
        block?.Products?.$values ||
        block?.Products ||
        [];
      return {
        ...block,
        products: Array.isArray(products) ? products : [],
      };
    });

    console.log(
      "✅ [HomeBlockService] İşlenmiş bloklar:",
      blocks.length,
      blocks,
    );
    return blocks;
  } catch (error) {
    console.error("❌ Ana sayfa blokları alınamadı:", error);
    return [];
  }
};

/**
 * Slug'a göre tek blok getirir - Tümünü Gör sayfası için
 *
 * @param {string} slug - Blok slug'ı
 * @returns {Promise<Object|null>} Blok detayı veya null
 */
export const getBlockBySlug = async (slug) => {
  try {
    const response = await api.get(`/api/homeblocks/${slug}`);
    return response;
  } catch (error) {
    console.error(`❌ Blok bulunamadı (${slug}):`, error);
    return null;
  }
};

/**
 * Blok tipi önizlemesi - Admin için
 *
 * @param {string} blockType - Blok tipi
 * @param {number|null} categoryId - Kategori ID (opsiyonel)
 * @param {number} maxCount - Maksimum ürün sayısı
 * @returns {Promise<Array>} Ürün listesi
 */
export const previewBlockProducts = async (
  blockType,
  categoryId = null,
  maxCount = 6,
) => {
  try {
    const params = new URLSearchParams({ blockType, maxCount });
    if (categoryId) params.append("categoryId", categoryId);

    const response = await api.get(`/api/homeblocks/preview?${params}`);
    return response || [];
  } catch (error) {
    console.error("❌ Blok önizleme hatası:", error);
    return [];
  }
};

// ============================================
// ADMIN API - BLOK YÖNETİMİ
// ============================================

/**
 * Tüm blokları getirir (admin listesi)
 *
 * @returns {Promise<Array>} Blok listesi
 */
export const getAllBlocks = async () => {
  try {
    const response = await api.get("/api/admin/homeblocks");
    // API { value: [...], Count: n } formatında döndürebilir
    const blocks = response?.value || response || [];
    return Array.isArray(blocks) ? blocks : [];
  } catch (error) {
    console.error("❌ Bloklar alınamadı:", error);
    throw error;
  }
};

/**
 * ID'ye göre blok detayı getirir
 *
 * @param {number} id - Blok ID
 * @returns {Promise<Object>} Blok detayı
 */
export const getBlockById = async (id) => {
  try {
    const response = await api.get(`/api/admin/homeblocks/${id}`);
    return response;
  } catch (error) {
    console.error(`❌ Blok bulunamadı (${id}):`, error);
    throw error;
  }
};

/**
 * Yeni blok oluşturur
 *
 * @param {Object} blockData - Blok verileri
 * @returns {Promise<Object>} Oluşturulan blok
 */
export const createBlock = async (blockData) => {
  try {
    const response = await api.post("/api/admin/homeblocks", blockData);
    console.log("✅ Blok oluşturuldu:", response);
    return response;
  } catch (error) {
    console.error("❌ Blok oluşturulamadı:", error);
    throw error;
  }
};

/**
 * Mevcut bloğu günceller
 *
 * @param {number} id - Blok ID
 * @param {Object} blockData - Güncel veriler
 * @returns {Promise<Object>} Güncellenen blok
 */
export const updateBlock = async (id, blockData) => {
  try {
    const response = await api.put(`/api/admin/homeblocks/${id}`, {
      ...blockData,
      id,
    });
    console.log("✅ Blok güncellendi:", response);
    return response;
  } catch (error) {
    console.error("❌ Blok güncellenemedi:", error);
    throw error;
  }
};

/**
 * Bloğu siler
 *
 * @param {number} id - Blok ID
 * @returns {Promise<boolean>} Silme başarılı mı
 */
export const deleteBlock = async (id) => {
  try {
    await api.delete(`/api/admin/homeblocks/${id}`);
    console.log("✅ Blok silindi:", id);
    return true;
  } catch (error) {
    console.error("❌ Blok silinemedi:", error);
    throw error;
  }
};

/**
 * Blok sıralamasını günceller
 *
 * @param {Array<{id: number, displayOrder: number}>} orders - Sıralama listesi
 * @returns {Promise<boolean>} İşlem başarılı mı
 */
export const reorderBlocks = async (orders) => {
  try {
    await api.put("/api/admin/homeblocks/reorder", orders);
    console.log("✅ Blok sıralaması güncellendi");
    return true;
  } catch (error) {
    console.error("❌ Sıralama güncellenemedi:", error);
    throw error;
  }
};

// ============================================
// ADMIN API - ÜRÜN YÖNETİMİ
// ============================================

/**
 * Bloğa ürün ekler
 *
 * @param {number} blockId - Blok ID
 * @param {number} productId - Ürün ID
 * @param {number} displayOrder - Sıralama (varsayılan: 0)
 * @returns {Promise<boolean>} İşlem başarılı mı
 */
export const addProductToBlock = async (
  blockId,
  productId,
  displayOrder = 0,
) => {
  try {
    await api.post(`/api/admin/homeblocks/${blockId}/products`, {
      productId,
      displayOrder,
    });
    console.log(
      `✅ Ürün bloğa eklendi: Block#${blockId} - Product#${productId}`,
    );
    return true;
  } catch (error) {
    console.error("❌ Ürün eklenemedi:", error);
    throw error;
  }
};

/**
 * Bloğa birden fazla ürün ekler
 *
 * @param {number} blockId - Blok ID
 * @param {Array<number>} productIds - Ürün ID listesi
 * @returns {Promise<boolean>} İşlem başarılı mı
 */
export const addProductsToBlock = async (blockId, productIds) => {
  try {
    await api.post(
      `/api/admin/homeblocks/${blockId}/products/batch`,
      productIds,
    );
    console.log(`✅ ${productIds.length} ürün bloğa eklendi`);
    return true;
  } catch (error) {
    console.error("❌ Ürünler eklenemedi:", error);
    throw error;
  }
};

/**
 * Bloktan ürün çıkarır
 *
 * @param {number} blockId - Blok ID
 * @param {number} productId - Ürün ID
 * @returns {Promise<boolean>} İşlem başarılı mı
 */
export const removeProductFromBlock = async (blockId, productId) => {
  try {
    await api.delete(`/api/admin/homeblocks/${blockId}/products/${productId}`);
    console.log(
      `✅ Ürün bloktan çıkarıldı: Block#${blockId} - Product#${productId}`,
    );
    return true;
  } catch (error) {
    console.error("❌ Ürün çıkarılamadı:", error);
    throw error;
  }
};

/**
 * Bloktaki ürün listesini tamamen değiştirir
 *
 * @param {number} blockId - Blok ID
 * @param {Array<number>} productIds - Yeni ürün ID listesi
 * @returns {Promise<boolean>} İşlem başarılı mı
 */
export const setBlockProducts = async (blockId, productIds) => {
  try {
    await api.put(`/api/admin/homeblocks/${blockId}/products/set`, productIds);
    console.log(`✅ Blok ürünleri güncellendi: ${productIds.length} ürün`);
    return true;
  } catch (error) {
    console.error("❌ Ürün listesi güncellenemedi:", error);
    throw error;
  }
};

/**
 * Bloktaki ürünlerin sıralamasını günceller
 *
 * @param {number} blockId - Blok ID
 * @param {Array<{productId: number, displayOrder: number, isActive: boolean}>} products - Ürün sıralaması
 * @returns {Promise<boolean>} İşlem başarılı mı
 */
export const updateBlockProductsOrder = async (blockId, products) => {
  try {
    await api.put(`/api/admin/homeblocks/${blockId}/products`, products);
    console.log("✅ Ürün sıralaması güncellendi");
    return true;
  } catch (error) {
    console.error("❌ Sıralama güncellenemedi:", error);
    throw error;
  }
};

// ============================================
// YARDIMCI FONKSİYONLAR
// ============================================

/**
 * Slug müsait mi kontrol eder
 *
 * @param {string} slug - Kontrol edilecek slug
 * @param {number|null} excludeBlockId - Hariç tutulacak blok ID (güncelleme için)
 * @returns {Promise<boolean>} Slug müsait mi
 */
export const checkSlugAvailability = async (slug, excludeBlockId = null) => {
  try {
    const params = new URLSearchParams({ slug });
    if (excludeBlockId) params.append("excludeBlockId", excludeBlockId);

    const response = await api.get(
      `/api/admin/homeblocks/check-slug?${params}`,
    );
    return response?.isAvailable || false;
  } catch (error) {
    console.error("❌ Slug kontrolü hatası:", error);
    return false;
  }
};

/**
 * Türkçe metinden URL dostu slug oluşturur
 *
 * @param {string} text - Metin
 * @returns {string} Slug
 */
export const generateSlug = (text) => {
  if (!text) return "";

  // Türkçe karakterleri çevir
  const turkishChars = {
    ğ: "g",
    Ğ: "g",
    ü: "u",
    Ü: "u",
    ş: "s",
    Ş: "s",
    ı: "i",
    I: "i",
    İ: "i",
    ö: "o",
    Ö: "o",
    ç: "c",
    Ç: "c",
  };

  let result = text.toLowerCase();
  Object.entries(turkishChars).forEach(([char, replacement]) => {
    result = result.replace(new RegExp(char, "g"), replacement);
  });

  // Özel karakterleri temizle, boşlukları tire yap
  result = result
    .replace(/[^a-z0-9\s-]/g, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "");

  return result;
};

/**
 * İndirim yüzdesini hesaplar
 *
 * @param {number} price - Normal fiyat
 * @param {number|null} specialPrice - İndirimli fiyat
 * @returns {number|null} İndirim yüzdesi
 */
export const calculateDiscountPercent = (price, specialPrice) => {
  if (!specialPrice || specialPrice >= price || price <= 0) return null;
  return Math.round((1 - specialPrice / price) * 100);
};

/**
 * Bloktaki ürünleri getirir
 *
 * @param {number} blockId - Blok ID
 * @returns {Promise<Array>} Ürün listesi
 */
export const getBlockProducts = async (blockId) => {
  try {
    const block = await getBlockById(blockId);
    if (!block) return [];

    // Products alanını normalize et (hem PascalCase hem camelCase desteği)
    let products = block.products || block.Products || [];

    // $values formatı kontrolü (JSON reference handling)
    if (products?.$values) {
      products = products.$values;
    }

    return Array.isArray(products) ? products : [];
  } catch (error) {
    console.error(`❌ Blok ürünleri alınamadı (${blockId}):`, error);
    return [];
  }
};

/**
 * Bloğun aktif/pasif durumunu değiştirir
 *
 * @param {number} blockId - Blok ID
 * @returns {Promise<Object>} Güncellenen blok
 */
export const toggleBlock = async (blockId) => {
  try {
    const response = await api.put(`/api/admin/homeblocks/${blockId}/toggle-active`);
    const isActive = response?.isActive ?? response?.IsActive;
    console.log(
      `✅ Blok durumu güncellendi: ${blockId} → ${isActive ? "Aktif" : "Pasif"}`,
    );
    return response;
  } catch (error) {
    console.error(`❌ Blok durumu güncellenemedi (${blockId}):`, error);
    throw error;
  }
};

// Default export
export default {
  // Public
  getActiveBlocks,
  getBlockBySlug,
  previewBlockProducts,

  // Admin - Block CRUD
  getAllBlocks,
  getBlockById,
  createBlock,
  updateBlock,
  deleteBlock,
  reorderBlocks,
  toggleBlock,

  // Admin - Products
  addProductToBlock,
  addProductsToBlock,
  removeProductFromBlock,
  setBlockProducts,
  updateBlockProductsOrder,
  getBlockProducts,

  // Helpers
  checkSlugAvailability,
  generateSlug,
  calculateDiscountPercent,

  // Constants
  BLOCK_TYPES,
  POSTER_DIMENSIONS,
  BACKGROUND_COLORS,
};
