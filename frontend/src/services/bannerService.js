/**
 * bannerService.js - Banner/Poster Yönetimi API Servisi
 *
 * Bu servis, ana sayfa slider ve promosyon görselleri için
 * backend API ile iletişimi sağlar.
 *
 * Public Endpoint'ler:
 * - GET /api/banners/slider - Slider banner'ları
 * - GET /api/banners/promo - Promosyon banner'ları
 * - GET /api/banners - Tüm aktif banner'lar
 *
 * Admin Endpoint'leri (yetki gerektirir):
 * - GET /api/admin/banners - Tüm banner'lar (aktif/pasif dahil)
 * - POST /api/admin/banners - Yeni banner oluştur
 * - PUT /api/admin/banners/{id} - Banner güncelle
 * - DELETE /api/admin/banners/{id} - Banner sil
 * - POST /api/admin/banners/upload - Resim yükle
 * - PUT /api/admin/banners/{id}/toggle - Aktif/Pasif toggle
 * - PUT /api/admin/banners/reorder - Sıralama değiştir
 * - POST /api/admin/banners/reset-to-default - Varsayılana sıfırla
 *
 * @author Senior Developer
 * @version 2.0.0
 */

import api from "./api";

// ============================================
// SABITLER VE YAPILANDIRMA
// ============================================

/**
 * Banner tipleri için boyut önerileri
 * UI'da kullanıcıya bilgi vermek için kullanılır
 */
export const BANNER_DIMENSIONS = {
  slider: { width: 1200, height: 400, text: "1200x400px", label: "Slider" },
  promo: { width: 300, height: 200, text: "300x200px", label: "Promosyon" },
  banner: { width: 800, height: 200, text: "800x200px", label: "Banner" },
};

/**
 * Dosya yükleme için izin verilen formatlar ve boyut limiti
 */
export const UPLOAD_CONFIG = {
  maxSizeBytes: 10 * 1024 * 1024, // 10 MB
  maxSizeMB: 10,
  allowedExtensions: [".jpg", ".jpeg", ".png", ".gif", ".webp"],
  allowedMimeTypes: ["image/jpeg", "image/png", "image/gif", "image/webp"],
};

// ============================================
// YARDIMCI FONKSİYONLAR
// ============================================

/**
 * Dosya uzantısının geçerli olup olmadığını kontrol eder
 * @param {string} filename - Dosya adı
 * @returns {boolean} Geçerli ise true
 */
export const isValidFileExtension = (filename) => {
  if (!filename) return false;
  const ext = "." + filename.split(".").pop().toLowerCase();
  return UPLOAD_CONFIG.allowedExtensions.includes(ext);
};

/**
 * Dosya boyutunun limiti aşıp aşmadığını kontrol eder
 * @param {number} sizeInBytes - Dosya boyutu (byte)
 * @returns {boolean} Geçerli ise true
 */
export const isValidFileSize = (sizeInBytes) => {
  return sizeInBytes <= UPLOAD_CONFIG.maxSizeBytes;
};

/**
 * MIME tipinin geçerli olup olmadığını kontrol eder
 * @param {string} mimeType - MIME tipi
 * @returns {boolean} Geçerli ise true
 */
export const isValidMimeType = (mimeType) => {
  return UPLOAD_CONFIG.allowedMimeTypes.includes(mimeType);
};

/**
 * Dosya doğrulama - tüm kontrolleri yapar
 * @param {File} file - Kontrol edilecek dosya
 * @returns {{ valid: boolean, error?: string }} Doğrulama sonucu
 */
export const validateFile = (file) => {
  if (!file) {
    return { valid: false, error: "Dosya seçilmedi" };
  }

  if (!isValidFileSize(file.size)) {
    return {
      valid: false,
      error: `Dosya boyutu ${UPLOAD_CONFIG.maxSizeMB}MB'dan küçük olmalıdır`,
    };
  }

  if (!isValidMimeType(file.type)) {
    return {
      valid: false,
      error: `İzin verilen formatlar: ${UPLOAD_CONFIG.allowedExtensions.join(
        ", "
      )}`,
    };
  }

  if (!isValidFileExtension(file.name)) {
    return {
      valid: false,
      error: `Geçersiz dosya uzantısı. İzin verilenler: ${UPLOAD_CONFIG.allowedExtensions.join(
        ", "
      )}`,
    };
  }

  return { valid: true };
};

// ============================================
// PUBLIC API FONKSİYONLARI
// ============================================

/**
 * Slider banner'larını getirir (sadece aktif olanlar)
 * Ana sayfa Hero Slider bileşeni için kullanılır
 *
 * @returns {Promise<Array>} Slider banner listesi
 * @throws {Error} API hatası durumunda
 */
export const getSliderBanners = async () => {
  try {
    // API'den gelen response doğrudan array olarak dönüyor (api.js interceptor'da unwrap ediliyor)
    const data = await api.get("/api/banners/slider");

    // Response array değilse veya boşsa boş array döndür
    if (!Array.isArray(data)) {
      console.warn(
        "[BannerService] Slider API beklenmeyen format döndü:",
        data
      );
      return [];
    }

    return data;
  } catch (error) {
    console.error(
      "[BannerService] Slider banner'ları alınamadı:",
      error.message
    );
    throw error;
  }
};

/**
 * Promosyon banner'larını getirir (sadece aktif olanlar)
 * Ana sayfa PromoCards bileşeni için kullanılır
 *
 * @returns {Promise<Array>} Promo banner listesi
 * @throws {Error} API hatası durumunda
 */
export const getPromoBanners = async () => {
  try {
    const data = await api.get("/api/banners/promo");

    if (!Array.isArray(data)) {
      console.warn("[BannerService] Promo API beklenmeyen format döndü:", data);
      return [];
    }

    return data;
  } catch (error) {
    console.error(
      "[BannerService] Promo banner'ları alınamadı:",
      error.message
    );
    throw error;
  }
};

/**
 * Tüm aktif banner'ları getirir (public)
 *
 * @returns {Promise<Array>} Tüm aktif banner listesi
 * @throws {Error} API hatası durumunda
 */
export const getAllBanners = async () => {
  try {
    const data = await api.get("/api/banners");

    if (!Array.isArray(data)) {
      console.warn(
        "[BannerService] Banners API beklenmeyen format döndü:",
        data
      );
      return [];
    }

    return data;
  } catch (error) {
    console.error("[BannerService] Banner'lar alınamadı:", error.message);
    throw error;
  }
};

/**
 * Aktif banner'ları getirir (getAllBanners ile aynı, geriye dönük uyumluluk için)
 * @deprecated getAllBanners() kullanın
 */
export const getActiveBanners = getAllBanners;

// ============================================
// ADMIN API FONKSİYONLARI
// ============================================

/**
 * Admin için tüm banner'ları getirir (aktif/pasif dahil)
 * Yetki gerektirir: Admin veya SuperAdmin
 *
 * @returns {Promise<Array>} Tüm banner listesi
 * @throws {Error} API hatası veya yetki hatası durumunda
 */
export const getAdminBanners = async () => {
  try {
    const data = await api.get("/api/admin/banners");

    if (!Array.isArray(data)) {
      console.warn("[BannerService] Admin API beklenmeyen format döndü:", data);
      return [];
    }

    return data;
  } catch (error) {
    console.error(
      "[BannerService] Admin banner listesi alınamadı:",
      error.message
    );
    throw error;
  }
};

/**
 * ID ile tek bir banner getirir
 *
 * @param {number} id - Banner ID
 * @returns {Promise<Object>} Banner detayları
 * @throws {Error} API hatası veya 404 durumunda
 */
export const getBannerById = async (id) => {
  if (!id || isNaN(Number(id))) {
    throw new Error("Geçersiz banner ID");
  }

  try {
    const data = await api.get(`/api/admin/banners/${id}`);
    return data;
  } catch (error) {
    console.error(`[BannerService] Banner #${id} alınamadı:`, error.message);
    throw error;
  }
};

/**
 * Yeni banner oluşturur
 * Yetki gerektirir: Admin veya SuperAdmin
 *
 * @param {Object} bannerData - Banner verileri
 * @param {string} bannerData.title - Başlık (zorunlu)
 * @param {string} bannerData.imageUrl - Görsel URL (zorunlu)
 * @param {string} [bannerData.linkUrl] - Tıklanınca gidilecek URL
 * @param {string} [bannerData.bannerType] - Banner tipi (slider/promo/banner)
 * @param {string} [bannerData.position] - Konum (homepage-top/homepage-middle vb.)
 * @param {number} [bannerData.displayOrder] - Görüntüleme sırası
 * @param {boolean} [bannerData.isActive] - Aktif mi?
 * @returns {Promise<Object>} Oluşturulan banner
 * @throws {Error} Validasyon veya API hatası durumunda
 */
export const createBanner = async (bannerData) => {
  // Zorunlu alan kontrolü
  if (!bannerData.title?.trim()) {
    throw new Error("Başlık zorunludur");
  }
  if (!bannerData.imageUrl?.trim()) {
    throw new Error("Görsel URL zorunludur");
  }

  try {
    // API'ye gönderilecek veriyi hazırla (backend 'type' field'ını bekliyor)
    const payload = {
      title: bannerData.title.trim(),
      imageUrl: bannerData.imageUrl.trim(),
      linkUrl: bannerData.linkUrl?.trim() || "",
      type: bannerData.bannerType || bannerData.type || "slider",
      position:
        bannerData.position ||
        (bannerData.type === "promo" ? "homepage-middle" : "homepage-top"),
      displayOrder: Number(bannerData.displayOrder) || 0,
      isActive: bannerData.isActive !== false, // default true
      subTitle: bannerData.subTitle?.trim() || "",
      description: bannerData.description?.trim() || "",
      buttonText: bannerData.buttonText?.trim() || "",
    };

    const data = await api.post("/api/admin/banners", payload);
    return data;
  } catch (error) {
    console.error("[BannerService] Banner oluşturulamadı:", error.message);
    throw error;
  }
};

/**
 * Mevcut banner'ı günceller
 * Yetki gerektirir: Admin veya SuperAdmin
 *
 * @param {number} id - Banner ID
 * @param {Object} bannerData - Güncellenecek veriler
 * @returns {Promise<Object>} Güncellenen banner
 * @throws {Error} Validasyon veya API hatası durumunda
 */
export const updateBanner = async (id, bannerData) => {
  if (!id || isNaN(Number(id))) {
    throw new Error("Geçersiz banner ID");
  }
  if (!bannerData.title?.trim()) {
    throw new Error("Başlık zorunludur");
  }
  if (!bannerData.imageUrl?.trim()) {
    throw new Error("Görsel URL zorunludur");
  }

  try {
    // API'ye gönderilecek veriyi hazırla (backend 'type' field'ını bekliyor)
    const payload = {
      id: Number(id),
      title: bannerData.title.trim(),
      imageUrl: bannerData.imageUrl.trim(),
      linkUrl: bannerData.linkUrl?.trim() || "",
      type: bannerData.bannerType || bannerData.type || "slider",
      position:
        bannerData.position ||
        (bannerData.type === "promo" ? "homepage-middle" : "homepage-top"),
      displayOrder: Number(bannerData.displayOrder) || 0,
      isActive: bannerData.isActive !== false,
      subTitle: bannerData.subTitle?.trim() || "",
      description: bannerData.description?.trim() || "",
      buttonText: bannerData.buttonText?.trim() || "",
    };

    const data = await api.put(`/api/admin/banners/${id}`, payload);
    return data;
  } catch (error) {
    console.error(
      `[BannerService] Banner #${id} güncellenemedi:`,
      error.message
    );
    throw error;
  }
};

/**
 * Banner siler
 * Yetki gerektirir: Admin veya SuperAdmin
 *
 * @param {number} id - Silinecek banner ID
 * @returns {Promise<void>}
 * @throws {Error} API hatası durumunda
 */
export const deleteBanner = async (id) => {
  if (!id || isNaN(Number(id))) {
    throw new Error("Geçersiz banner ID");
  }

  try {
    await api.delete(`/api/admin/banners/${id}`);
  } catch (error) {
    console.error(`[BannerService] Banner #${id} silinemedi:`, error.message);
    throw error;
  }
};

/**
 * Banner görseli yükler
 * Yetki gerektirir: Admin veya SuperAdmin
 *
 * @param {File} file - Yüklenecek dosya
 * @returns {Promise<{ success: boolean, imageUrl: string }>} Yükleme sonucu
 * @throws {Error} Validasyon veya yükleme hatası durumunda
 */
export const uploadImage = async (file) => {
  // Dosya doğrulama
  const validation = validateFile(file);
  if (!validation.valid) {
    throw new Error(validation.error);
  }

  try {
    // FormData oluştur (backend 'image' key'i bekliyor)
    const formData = new FormData();
    formData.append("image", file);

    // Özel headers ile POST isteği (Content-Type otomatik ayarlanacak)
    const data = await api.post("/api/admin/banners/upload-image", formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });

    // Response formatını kontrol et
    if (data && data.imageUrl) {
      return data;
    }

    // Farklı response formatları için uyumluluk
    if (data && typeof data === "string") {
      return { success: true, imageUrl: data };
    }

    throw new Error("Beklenmeyen response formatı");
  } catch (error) {
    console.error("[BannerService] Görsel yüklenemedi:", error.message);
    throw error;
  }
};

/**
 * Banner aktif/pasif durumunu değiştirir (toggle)
 * Yetki gerektirir: Admin veya SuperAdmin
 *
 * @param {number} id - Banner ID
 * @returns {Promise<Object>} Güncellenen banner
 * @throws {Error} API hatası durumunda
 */
export const toggleBanner = async (id) => {
  if (!id || isNaN(Number(id))) {
    throw new Error("Geçersiz banner ID");
  }

  try {
    const data = await api.put(`/api/admin/banners/${id}/toggle`);
    return data;
  } catch (error) {
    console.error(
      `[BannerService] Banner #${id} toggle edilemedi:`,
      error.message
    );
    throw error;
  }
};

/**
 * Banner'ların sıralamasını günceller
 * Yetki gerektirir: Admin veya SuperAdmin
 *
 * @param {Array<{ id: number, displayOrder: number }>} orderedItems - Yeni sıralama
 * @returns {Promise<void>}
 * @throws {Error} API hatası durumunda
 */
export const reorderBanners = async (orderedItems) => {
  if (!Array.isArray(orderedItems) || orderedItems.length === 0) {
    throw new Error("Sıralama listesi boş olamaz");
  }

  try {
    await api.put("/api/admin/banners/reorder", orderedItems);
  } catch (error) {
    console.error("[BannerService] Sıralama güncellenemedi:", error.message);
    throw error;
  }
};

/**
 * Banner'ları varsayılan değerlere sıfırlar
 * Tüm banner'ları siler ve 7 default banner ekler (3 slider + 4 promo)
 * Yetki gerektirir: Admin veya SuperAdmin
 *
 * @returns {Promise<{ message: string }>} Sonuç mesajı
 * @throws {Error} API hatası durumunda
 */
export const resetToDefault = async () => {
  try {
    const data = await api.post("/api/admin/banners/reset-to-default");
    return data;
  } catch (error) {
    console.error(
      "[BannerService] Varsayılana sıfırlama başarısız:",
      error.message
    );
    throw error;
  }
};

// ============================================
// DEFAULT EXPORT
// ============================================

/**
 * Banner Service - Tüm fonksiyonları içeren nesne
 *
 * Kullanım:
 * import bannerService from './services/bannerService';
 * const sliders = await bannerService.getSliderBanners();
 *
 * veya named import:
 * import { getSliderBanners, getPromoBanners } from './services/bannerService';
 */
const bannerService = {
  // Public
  getSliderBanners,
  getPromoBanners,
  getAllBanners,
  getActiveBanners, // deprecated alias

  // Admin
  getAdminBanners,
  getBannerById,
  createBanner,
  updateBanner,
  deleteBanner,
  uploadImage,
  toggleBanner,
  reorderBanners,
  resetToDefault,

  // Yardımcılar
  validateFile,
  isValidFileExtension,
  isValidFileSize,
  isValidMimeType,

  // Sabitler
  BANNER_DIMENSIONS,
  UPLOAD_CONFIG,
};

export default bannerService;
