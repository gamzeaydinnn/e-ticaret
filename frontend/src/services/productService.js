// src/services/productService.js
// ============================================================
// ÜRÜN SERVİSİ - Backend API Entegrasyonu
// ============================================================
// Bu servis, ürün yönetimi için tüm API çağrılarını yönetir.
// Mock API yerine gerçek backend API kullanılır.
// Türkçe karakter desteği ve Excel import/export özellikleri içerir.
// ============================================================

import api from "./api";

// ============================================================
// KONFIGÜRASYON
// ============================================================
// API Base URL - Environment variable veya varsayılan değer kullanılır
// Bu sayede mock/real API geçişi otomatik olur
const API_CONFIG = {
  // Backend çalışmıyorsa fallback davranış
  useFallback: false,
  // Timeout süresi (ms)
  timeout: 30000,
  // Retry sayısı
  retryCount: 2,
};

// Backend API base URL'ini al
const getBackendBaseURL = () => {
  // Environment variable varsa kullan
  if (process.env.REACT_APP_API_URL) {
    return process.env.REACT_APP_API_URL;
  }

  // Localhost development için
  if (
    window.location.hostname === "localhost" ||
    window.location.hostname === "127.0.0.1"
  ) {
    return "http://localhost:5153";
  }

  // Production ortamı - same origin
  return window.location.origin;
};

// ============================================================
// PRODUCT MAPPER
// ============================================================
// Backend'den gelen veriyi frontend formatına dönüştürür.
// Farklı alan isimlerini normalize eder (snake_case -> camelCase)
// Türkçe karakterler UTF-8 olarak korunur.
const mapProduct = (p = {}) => {
  // Null/undefined kontrolü
  if (!p || typeof p !== "object") {
    console.warn("mapProduct: Geçersiz ürün verisi:", p);
    return null;
  }

  // Fiyat hesaplaması - basePrice ve specialPrice ayrımı
  const basePrice = parseFloat(p.price ?? p.unitPrice ?? 0) || 0;
  const special = p.specialPrice ?? p.discountPrice ?? p.discount_price ?? null;

  let price = basePrice;
  let originalPrice = null;
  let discountPercentage = 0;

  // İndirimli fiyat varsa hesapla
  if (
    special !== null &&
    typeof special === "number" &&
    special > 0 &&
    basePrice > 0 &&
    special < basePrice
  ) {
    price = special;
    originalPrice = basePrice;
    // İndirim yüzdesi hesapla (yuvarlama)
    discountPercentage = Math.round(100 - (special / basePrice) * 100);
  }

  // Stok bilgisi (farklı alan isimlerini destekle)
  const stock =
    parseInt(p.stock ?? p.stockQuantity ?? p.stock_quantity ?? 0) || 0;

  return {
    id: p.id,
    name: p.name || p.title || "",
    category: p.category_name || p.category || "",
    categoryId: p.categoryId ?? p.category_id ?? null,
    categoryName: p.categoryName || p.category_name || p.category || "",
    price,
    originalPrice,
    discountPrice: special,
    specialPrice: special,
    discountPercentage,
    // Backend'den gelen imageUrl'i API base URL ile birleştir (eğer relative path ise)
    imageUrl: (() => {
      const img = p.image_url || p.image || p.imageUrl || "";
      if (!img) return "";

      // Eğer URL http/https ile başlıyorsa, zaten tam URL
      if (img.startsWith("http://") || img.startsWith("https://")) {
        return img;
      }

      // Relative path ise backend base URL'i ile birleştir
      const baseURL = getBackendBaseURL();
      return img.startsWith("/") ? baseURL + img : baseURL + "/" + img;
    })(),
    stock,
    stockQuantity: stock,
    description: p.description || "",
    sku: p.sku || p.SKU || "",
    unitWeightGrams: p.unitWeightGrams || p.unit_weight_grams || 0,
    isActive: p.isActive !== false,
    createdAt: p.createdAt || p.created_at || null,
    updatedAt: p.updatedAt || p.updated_at || null,
  };
};

// ============================================================
// SUBSCRIPTION PATTERN
// ============================================================
// Ürün değişikliklerini dinleyen callback'ler
// Ana sayfa otomatik güncelleme için kullanılır
let subscribers = [];

// ============================================================
// HELPER FUNCTIONS
// ============================================================

/**
 * API yanıtından items array'ini çıkarır
 * Sayfalama yapısını destekler (PagedResult<T>)
 */
const extractItems = (response) => {
  // Doğrudan array ise
  if (Array.isArray(response)) {
    return response;
  }
  // PagedResult yapısı (items, totalCount, pageNumber, pageSize)
  if (response?.items && Array.isArray(response.items)) {
    return response.items;
  }
  // data property içinde
  if (response?.data) {
    if (Array.isArray(response.data)) {
      return response.data;
    }
    if (response.data.items && Array.isArray(response.data.items)) {
      return response.data.items;
    }
  }
  // Boş array döndür
  return [];
};

/**
 * Subscriber'lara değişiklik bildirimi gönderir
 * @param {string} action - CRUD işlem tipi (create, update, delete)
 * @param {object} data - İşlem verisi
 */
const notifySubscribers = (action, data) => {
  subscribers.forEach((callback) => {
    try {
      callback({ action, data, timestamp: Date.now() });
    } catch (err) {
      console.error("Subscriber notification error:", err);
    }
  });
};

// ============================================================
// PRODUCT SERVICE
// ============================================================
export const ProductService = {
  // -----------------------------------------------------------
  // PUBLIC ENDPOINTS (Herkes erişebilir)
  // -----------------------------------------------------------

  /**
   * Aktif ürünleri listeler (ana sayfa için)
   * @param {string} query - Arama sorgusu (opsiyonel)
   * @returns {Promise<Array>} Ürün listesi
   */
  list: async (query = "") => {
    try {
      const endpoint = query
        ? `/api/products/search?query=${encodeURIComponent(query)}`
        : "/api/products";

      const response = await api.get(endpoint);
      const items = extractItems(response);

      // Sadece aktif ürünleri filtrele ve map'le
      return items
        .filter((p) => p.isActive !== false)
        .map(mapProduct)
        .filter((p) => p !== null); // null değerleri temizle
    } catch (err) {
      console.error("❌ Ürünler yüklenemedi:", err);
      // Fallback: boş array döndür (UI crash önleme)
      return [];
    }
  },

  /**
   * Tek bir ürünü ID ile getirir
   * @param {number} id - Ürün ID
   * @returns {Promise<object|null>} Ürün objesi veya null
   */
  get: async (id) => {
    try {
      if (!id) {
        console.warn("ProductService.get: ID gerekli");
        return null;
      }

      const response = await api.get(`/api/products/${id}`);
      const product = response?.data || response;
      return product ? mapProduct(product) : null;
    } catch (err) {
      console.error(`❌ Ürün bulunamadı (ID: ${id}):`, err);
      return null;
    }
  },

  /**
   * Kategoriye göre ürünleri getirir
   * @param {number} categoryId - Kategori ID
   * @returns {Promise<Array>} Ürün listesi
   */
  getByCategory: async (categoryId) => {
    try {
      if (!categoryId) {
        console.warn("ProductService.getByCategory: categoryId gerekli");
        return [];
      }

      const response = await api.get(`/api/products?categoryId=${categoryId}`);
      const items = extractItems(response);

      return items
        .filter((p) => p.isActive !== false)
        .map(mapProduct)
        .filter((p) => p !== null);
    } catch (err) {
      console.error(
        `❌ Kategori ürünleri yüklenemedi (ID: ${categoryId}):`,
        err
      );
      return [];
    }
  },

  /**
   * Ürün araması yapar
   * @param {string} query - Arama terimi
   * @param {number} page - Sayfa numarası
   * @param {number} size - Sayfa başına ürün sayısı
   * @returns {Promise<Array>} Arama sonuçları
   */
  search: async (query, page = 1, size = 20) => {
    try {
      if (!query || query.trim().length < 2) {
        return [];
      }

      const response = await api.get(
        `/api/products/search?query=${encodeURIComponent(
          query
        )}&page=${page}&size=${size}`
      );
      const items = extractItems(response);

      return items.map(mapProduct).filter((p) => p !== null);
    } catch (err) {
      console.error(`❌ Ürün araması başarısız (query: ${query}):`, err);
      return [];
    }
  },

  // -----------------------------------------------------------
  // SUBSCRIPTION PATTERN (Ana sayfa auto-refresh için)
  // -----------------------------------------------------------

  /**
   * Ürün değişikliklerine subscribe ol
   * @param {function} callback - Değişiklik callback'i
   * @returns {function} Unsubscribe fonksiyonu
   */
  subscribe: (callback) => {
    if (typeof callback !== "function") {
      console.warn("ProductService.subscribe: callback fonksiyon olmalı");
      return () => {};
    }

    subscribers.push(callback);

    // Unsubscribe fonksiyonunu döndür
    return () => {
      subscribers = subscribers.filter((cb) => cb !== callback);
    };
  },

  /**
   * Tüm subscriber'lara güncelleme bildirimi gönder
   * CRUD işlemlerinden sonra çağrılmalı
   */
  notifyChange: (action = "update", data = null) => {
    notifySubscribers(action, data);
  },

  // -----------------------------------------------------------
  // ADMIN ENDPOINTS (Yetkilendirme gerekli)
  // -----------------------------------------------------------

  /**
   * Tüm ürünleri getirir (admin panel için)
   * Aktif/pasif tüm ürünleri içerir
   * @returns {Promise<Array>} Tüm ürünler
   */
  getAll: async () => {
    try {
      // Admin endpoint'i dene
      const response = await api.get("/api/products/admin/all?size=500");
      const items = extractItems(response);
      return items.map(mapProduct).filter((p) => p !== null);
    } catch (err) {
      console.error("⚠️ Admin endpoint başarısız, fallback deneniyor:", err);

      // Fallback: normal endpoint
      try {
        const response = await api.get("/api/products?size=500");
        const items = extractItems(response);
        return items.map(mapProduct).filter((p) => p !== null);
      } catch (err2) {
        console.error("❌ Ürünler yüklenemedi:", err2);
        return [];
      }
    }
  },

  /**
   * Yeni ürün oluşturur
   * @param {object} formData - Ürün verileri
   * @returns {Promise<object>} Oluşturulan ürün
   */
  createAdmin: async (formData) => {
    try {
      // Form verilerini API formatına dönüştür
      const payload = {
        name: formData.name?.trim() || "",
        description: formData.description?.trim() || "",
        price: parseFloat(formData.price) || 0,
        stockQuantity: parseInt(formData.stockQuantity || formData.stock) || 0,
        categoryId: parseInt(formData.categoryId) || 1,
        imageUrl: formData.imageUrl?.trim() || null,
        specialPrice: formData.specialPrice
          ? parseFloat(formData.specialPrice)
          : null,
        isActive: formData.isActive !== false,
      };

      const response = await api.post("/api/products", payload);
      const result = response?.data || response;

      // Subscriber'lara bildir (ana sayfa güncellemesi için)
      notifySubscribers("create", result);

      return result;
    } catch (err) {
      console.error("❌ Ürün oluşturma hatası:", err);
      throw err;
    }
  },

  /**
   * Mevcut ürünü günceller
   * @param {number} id - Ürün ID
   * @param {object} formData - Güncellenecek veriler
   * @returns {Promise<object>} Güncellenen ürün
   */
  updateAdmin: async (id, formData) => {
    try {
      if (!id) {
        throw new Error("Ürün ID gerekli");
      }

      // Form verilerini API formatına dönüştür
      const payload = {
        name: formData.name?.trim() || "",
        description: formData.description?.trim() || "",
        price: parseFloat(formData.price) || 0,
        stockQuantity: parseInt(formData.stockQuantity || formData.stock) || 0,
        categoryId: parseInt(formData.categoryId) || 1,
        imageUrl: formData.imageUrl?.trim() || null,
        specialPrice: formData.specialPrice
          ? parseFloat(formData.specialPrice)
          : null,
        isActive: formData.isActive !== false,
      };

      const response = await api.put(`/api/products/${id}`, payload);
      const result = response?.data || response;

      // Subscriber'lara bildir (ana sayfa güncellemesi için)
      notifySubscribers("update", { id, ...result });

      return result;
    } catch (err) {
      console.error(`❌ Ürün güncelleme hatası (ID: ${id}):`, err);
      throw err;
    }
  },

  /**
   * Ürünü siler
   * @param {number} id - Silinecek ürün ID
   * @returns {Promise<void>}
   */
  deleteAdmin: async (id) => {
    try {
      if (!id) {
        throw new Error("Ürün ID gerekli");
      }

      await api.delete(`/api/products/${id}`);

      // Subscriber'lara bildir
      notifySubscribers("delete", { id });
    } catch (err) {
      console.error(`❌ Ürün silme hatası (ID: ${id}):`, err);
      throw err;
    }
  },

  /**
   * Ürün stoğunu günceller
   * @param {number} id - Ürün ID
   * @param {number} stock - Yeni stok miktarı
   * @returns {Promise<object>}
   */
  updateStockAdmin: async (id, stock) => {
    try {
      if (!id) {
        throw new Error("Ürün ID gerekli");
      }

      const response = await api.patch(`/api/products/${id}/stock`, {
        stock: parseInt(stock) || 0,
      });

      // Subscriber'lara bildir
      notifySubscribers("update", { id, stock });

      return response?.data || response;
    } catch (err) {
      console.error(`❌ Stok güncelleme hatası (ID: ${id}):`, err);
      throw err;
    }
  },

  // -----------------------------------------------------------
  // EXCEL IMPORT/EXPORT ENDPOINTS
  // -----------------------------------------------------------

  /**
   * Excel dosyasından toplu ürün yükler
   * Desteklenen formatlar: .xlsx, .xls, .csv
   * @param {File} file - Yüklenecek Excel dosyası
   * @returns {Promise<object>} Import sonucu (successCount, errorCount, errors)
   */
  importExcel: async (file) => {
    try {
      if (!file) {
        throw new Error("Dosya seçilmedi");
      }

      // Dosya uzantısı kontrolü
      const validExtensions = [".xlsx", ".xls", ".csv"];
      const fileName = file.name.toLowerCase();
      const isValid = validExtensions.some((ext) => fileName.endsWith(ext));

      if (!isValid) {
        throw new Error(
          "Sadece Excel (.xlsx, .xls) veya CSV dosyaları kabul edilir"
        );
      }

      // Dosya boyutu kontrolü (50MB)
      const maxSize = 50 * 1024 * 1024;
      if (file.size > maxSize) {
        throw new Error("Dosya boyutu maksimum 50MB olabilir");
      }

      const formData = new FormData();
      formData.append("file", file);

      const response = await api.post("/api/products/import/excel", formData, {
        headers: { "Content-Type": "multipart/form-data" },
        timeout: 120000, // 2 dakika (büyük dosyalar için)
      });

      const result = response?.data || response;

      // Import başarılıysa subscriber'lara bildir
      if (result?.successCount > 0) {
        notifySubscribers("import", result);
      }

      return result;
    } catch (err) {
      console.error("❌ Excel import hatası:", err);
      throw err;
    }
  },

  /**
   * Boş Excel şablonu indirir
   * Şablon Türkçe örnek veriler ve açıklamalar içerir
   * @returns {Promise<Blob>} Excel dosyası blob'u
   */
  downloadTemplate: async () => {
    try {
      const response = await api.get("/api/products/import/template", {
        responseType: "blob",
      });
      return response;
    } catch (err) {
      console.error("❌ Şablon indirme hatası:", err);
      throw err;
    }
  },

  /**
   * Mevcut tüm ürünleri Excel dosyası olarak dışa aktarır
   * UTF-8 encoding ile Türkçe karakterler korunur
   * @returns {Promise<Blob>} Excel dosyası blob'u
   */
  exportExcel: async () => {
    try {
      const response = await api.get("/api/products/export/excel", {
        responseType: "blob",
        timeout: 60000, // 1 dakika
      });
      return response;
    } catch (err) {
      console.error("❌ Excel export hatası:", err);
      throw err;
    }
  },

  // -----------------------------------------------------------
  // IMAGE UPLOAD ENDPOINT
  // -----------------------------------------------------------

  /**
   * Ürün resmi yükler (bilgisayardan dosya seçerek)
   * Desteklenen formatlar: jpg, jpeg, png, gif, webp
   * Maksimum boyut: 10MB
   * @param {File} imageFile - Yüklenecek resim dosyası
   * @returns {Promise<{success: boolean, imageUrl: string, message: string}>}
   */
  uploadImage: async (imageFile) => {
    try {
      if (!imageFile) {
        throw new Error("Resim dosyası seçilmedi");
      }

      // Dosya türü kontrolü (frontend güvenlik katmanı)
      const allowedTypes = [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
      ];
      if (!allowedTypes.includes(imageFile.type)) {
        throw new Error(
          "Sadece resim dosyaları (jpg, png, gif, webp) yüklenebilir"
        );
      }

      // Dosya boyutu kontrolü (10MB)
      const maxSize = 10 * 1024 * 1024;
      if (imageFile.size > maxSize) {
        throw new Error("Dosya boyutu maksimum 10MB olabilir");
      }

      const formData = new FormData();
      formData.append("image", imageFile);

      const response = await api.post("/api/products/upload/image", formData, {
        headers: { "Content-Type": "multipart/form-data" },
        timeout: 30000, // 30 saniye
      });

      return response?.data || response;
    } catch (err) {
      console.error("❌ Resim yükleme hatası:", err);
      throw err;
    }
  },
};

// Default export
export default ProductService;
