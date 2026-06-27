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

const parseRequiredCategoryId = (value) => {
  const categoryId = Number.parseInt(String(value ?? ""), 10);
  if (!Number.isInteger(categoryId) || categoryId <= 0) {
    throw new Error("Geçerli bir kategori seçilmelidir");
  }

  return categoryId;
};

const normalizeNullableBoolean = (value) => {
  if (value === true) return true;
  if (value === false) return false;
  return null;
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
    pricePerUnit: parseFloat(p.pricePerUnit ?? p.PricePerUnit ?? 0) || 0,
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
    barcode: p.barcode || p.barkod || "",
    sku: p.sku || p.SKU || "",
    unit: p.unit || p.Unit || "",
    isWeightBased: p.isWeightBased === true || p.IsWeightBased === true,
    weightUnit: p.weightUnit ?? p.WeightUnit ?? null,
    unitWeightGrams: p.unitWeightGrams || p.unit_weight_grams || 0,
    isActive: p.isActive !== false,
    createdAt: p.createdAt || p.created_at || null,
    updatedAt: p.updatedAt || p.updated_at || null,
    adminOverrideName: normalizeNullableBoolean(p.adminOverrideName),
    adminOverridePrice: normalizeNullableBoolean(p.adminOverridePrice),
    adminOverrideCategory: normalizeNullableBoolean(p.adminOverrideCategory),
    effectiveAdminOverrideName: p.effectiveAdminOverrideName === true,
    effectiveAdminOverridePrice: p.effectiveAdminOverridePrice === true,
    effectiveAdminOverrideCategory: p.effectiveAdminOverrideCategory === true,
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

const extractPagedData = (response, pageSize = 25) => {
  if (Array.isArray(response)) {
    return {
      items: response,
      total: response.length,
      page: 1,
      pageSize,
      totalPages: 1,
    };
  }

  const payload =
    response?.data && typeof response.data === "object"
      ? response.data
      : response;

  const items = Array.isArray(payload?.items)
    ? payload.items
    : Array.isArray(payload)
      ? payload
      : [];
  const total = payload?.total ?? payload?.totalCount ?? items.length;
  const take = payload?.take ?? payload?.pageSize ?? pageSize;
  const skip = payload?.skip ?? 0;
  const page = payload?.page ?? Math.floor(skip / Math.max(take, 1)) + 1;
  const totalPages =
    payload?.totalPages ?? Math.max(1, Math.ceil(total / Math.max(take, 1)));

  return {
    items,
    total,
    page,
    pageSize: take,
    totalPages,
  };
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

export const getProductDetailPath = (product = {}) => {
  if (product?.id && product.id > 0) {
    return `/product/${product.slug || product.id}`;
  }

  if (product?.slug) {
    return `/product/${product.slug}`;
  }

  if (product?.sku) {
    return `/product/sku/${encodeURIComponent(product.sku)}`;
  }

  return `/product/${product?.id || ""}`;
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

      // Ana sayfada sadece aktif, stokta olan ve satilabilir fiyatli urunleri göster.
      return items
        .filter((p) => p.isActive !== false)
        .filter((p) => {
          const stock = parseInt(p.stock ?? p.stockQuantity ?? p.stock_quantity ?? 0, 10) || 0;
          return stock > 0;
        })
        .filter((p) => {
          const basePrice = parseFloat(p.price ?? p.Price ?? 0) || 0;
          const rawSpecialPrice = p.specialPrice ?? p.SpecialPrice ?? p.discountedPrice ?? p.discountPrice;
          const specialPrice = rawSpecialPrice === undefined || rawSpecialPrice === null
            ? null
            : parseFloat(rawSpecialPrice) || 0;
          const displayPrice = specialPrice !== null && specialPrice > 0 && specialPrice < basePrice
            ? specialPrice
            : basePrice;
          return displayPrice > 0;
        })
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
   * Slug ile ürün detayı getirir.
   */
  getBySlug: async (slug) => {
    try {
      if (!slug) return null;
      const response = await api.get(
        `/api/products/slug/${encodeURIComponent(slug)}`,
      );
      const product = response?.data || response;
      return product ? mapProduct(product) : null;
    } catch (err) {
      console.error(`❌ Ürün bulunamadı (slug: ${slug}):`, err);
      return null;
    }
  },

  /**
   * SKU (Mikro stok kodu) ile ürün detayı getirir.
   * Mikro ERP'den gelen Id=0 ürünler için kullanılır.
   */
  getBySku: async (sku) => {
    try {
      if (!sku) return null;
      const response = await api.get(
        `/api/products/sku/${encodeURIComponent(sku)}`,
      );
      const product = response?.data || response;
      return product ? mapProduct(product) : null;
    } catch (err) {
      console.error(`❌ Ürün bulunamadı (SKU: ${sku}):`, err);
      return null;
    }
  },

  /**
   * Kategoriye göre ürünleri getirir
   * @param {number} categoryId - Kategori ID
   * @returns {Promise<Array>} Ürün listesi
   */
  getByCategory: async (categoryId, pageOrOptions = null, sizeArg = null) => {
    try {
      if (!categoryId) {
        console.warn("ProductService.getByCategory: categoryId gerekli");
        return [];
      }

      const hasPaging =
        typeof pageOrOptions === "number" ||
        (pageOrOptions && typeof pageOrOptions === "object");

      if (!hasPaging) {
        const response = await api.get(
          `/api/products?categoryId=${categoryId}`,
        );
        const items = extractItems(response);

        return items
          .filter((p) => p.isActive !== false)
          .map(mapProduct)
          .filter((p) => p !== null);
      }

      const options =
        typeof pageOrOptions === "object"
          ? pageOrOptions
          : { page: pageOrOptions, size: sizeArg };

      const page = Math.max(1, parseInt(options?.page, 10) || 1);
      const size = Math.max(1, parseInt(options?.size, 10) || 25);
      const sort = options?.sort || "name";
      const direction = options?.direction || "asc";

      const params = new URLSearchParams({
        page: String(page),
        size: String(size),
        sort,
        direction,
      });

      if (typeof options?.inStock === "boolean") {
        params.set("inStock", String(options.inStock));
      }

      const response = await api.get(
        `/api/products/category/${categoryId}/paged?${params.toString()}`,
      );
      const paged = extractPagedData(response, size);

      return {
        items: paged.items
          .filter((p) => p.isActive !== false)
          .map(mapProduct)
          .filter((p) => p !== null),
        total: paged.total,
        page: paged.page,
        pageSize: paged.pageSize,
        totalPages: paged.totalPages,
      };
    } catch (err) {
      console.error(
        `❌ Kategori ürünleri yüklenemedi (ID: ${categoryId}):`,
        err,
      );
      return pageOrOptions
        ? { items: [], total: 0, page: 1, pageSize: 25, totalPages: 1 }
        : [];
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
      if (!query || !query.trim()) {
        return [];
      }

      const response = await api.get(
        `/api/products/search?query=${encodeURIComponent(
          query,
        )}&page=${page}&size=${size}`,
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
      const pageSize = 200;
      let currentPage = 1;
      let totalPages = 1;
      const collectedItems = [];

      do {
        const response = await api.get(
          `/api/products/admin/all?page=${currentPage}&size=${pageSize}`,
        );
        const paged = extractPagedData(response, pageSize);

        collectedItems.push(...(paged.items || []));
        totalPages = Math.max(1, Number(paged.totalPages) || 1);
        currentPage += 1;

        if ((paged.items || []).length === 0) {
          break;
        }
      } while (currentPage <= totalPages);

      return collectedItems.map(mapProduct).filter((p) => p !== null);
    } catch (err) {
      console.error("⚠️ Admin endpoint başarısız, fallback deneniyor:", err);

      // Fallback: normal endpoint
      try {
        const response = await api.get("/api/products?size=1000");
        const items = extractItems(response);
        return items.map(mapProduct).filter((p) => p !== null);
      } catch (err2) {
        console.error("❌ Ürünler yüklenemedi:", err2);
        return [];
      }
    }
  },

  getAdminPage: async ({
    page = 1,
    size = 24,
    sku = "",
    name = "",
    status = "all",
    stockStatus = "all",
  } = {}) => {
    try {
      const params = new URLSearchParams({
        page: String(page),
        size: String(size),
      });

      if (sku.trim()) {
        params.set("sku", sku.trim());
      }

      if (name.trim()) {
        params.set("name", name.trim());
      }

      if (status && status !== "all") {
        params.set("status", status);
      }

      if (stockStatus && stockStatus !== "all") {
        params.set("stockStatus", stockStatus);
      }

      const response = await api.get(`/api/products/admin/all?${params.toString()}`);
      const paged = extractPagedData(response, size);

      return {
        ...paged,
        items: paged.items.map(mapProduct).filter((product) => product !== null),
      };
    } catch (err) {
      console.error("❌ Admin ürün sayfası yüklenemedi:", err);
      return {
        items: [],
        total: 0,
        page,
        pageSize: size,
        totalPages: 1,
      };
    }
  },

  /**
   * Yeni ürün oluşturur
   * @param {object} formData - Ürün verileri
   * @returns {Promise<object>} Oluşturulan ürün
   */
  createAdmin: async (formData) => {
    try {
      const categoryId = parseRequiredCategoryId(formData.categoryId);

      // Form verilerini API formatına dönüştür
      const payload = {
        name: formData.name?.trim() || "",
        description: formData.description?.trim() || "",
        price: parseFloat(formData.price) || 0,
        stockQuantity: parseInt(formData.stockQuantity || formData.stock) || 0,
        categoryId,
        imageUrl: formData.imageUrl?.trim() || null,
        specialPrice: formData.specialPrice
          ? parseFloat(formData.specialPrice)
          : null,
        isActive: formData.isActive !== false,
        adminOverrideName: normalizeNullableBoolean(formData.adminOverrideName),
        adminOverridePrice: normalizeNullableBoolean(formData.adminOverridePrice),
        adminOverrideCategory: normalizeNullableBoolean(formData.adminOverrideCategory),
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
   * ID > 0 ise id bazlı, değilse SKU bazlı güncelleme yapar (Mikro ERP ürünleri).
   * @param {number} id - Ürün ID (0 ise SKU kullanılır)
   * @param {object} formData - Güncellenecek veriler (sku alanı içermeli)
   * @returns {Promise<object>} Güncellenen ürün
   */
  updateAdmin: async (id, formData) => {
    try {
      const categoryId = parseRequiredCategoryId(formData.categoryId);

      // Form verilerini API formatına dönüştür
      const payload = {
        name: formData.name?.trim() || "",
        description: formData.description?.trim() || "",
        price: parseFloat(formData.price) || 0,
        stockQuantity: parseInt(formData.stockQuantity || formData.stock) || 0,
        categoryId,
        imageUrl: formData.imageUrl?.trim() || null,
        specialPrice: formData.specialPrice
          ? parseFloat(formData.specialPrice)
          : null,
        isActive: formData.isActive !== false,
        adminOverrideName: normalizeNullableBoolean(formData.adminOverrideName),
        adminOverridePrice: normalizeNullableBoolean(formData.adminOverridePrice),
        adminOverrideCategory: normalizeNullableBoolean(formData.adminOverrideCategory),
      };

      let response;
      // Sayısal id kontrolü — 0 ve altı Mikro ürünü demek
      const numericId = typeof id === "number" ? id : parseInt(id) || 0;
      const hasSku = formData.sku && formData.sku.trim() !== "";

      if (numericId > 0) {
        // Yerel DB'de kayıtlı ürün — id bazlı güncelleme
        response = await api.put(`/api/products/${numericId}`, payload);
      } else if (hasSku) {
        // Mikro ERP ürünü (id=0) — SKU bazlı upsert
        response = await api.put(
          `/api/products/by-sku/${encodeURIComponent(formData.sku.trim())}`,
          payload,
        );
      } else {
        throw new Error("Ürün ID veya SKU gerekli");
      }

      const result = response?.data || response;

      // Subscriber'lara bildir (ana sayfa güncellemesi için)
      notifySubscribers("update", { id: numericId, ...result });

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
   * Mükerrer ürün gruplarını getirir.
   * Backend: GET /api/products/admin/duplicates
   */
  getDuplicateGroups: async () => {
    try {
      const response = await api.get("/api/products/admin/duplicates");
      const payload = response?.data || response;
      const groups = Array.isArray(payload?.groups) ? payload.groups : [];

      return {
        totalGroups: Number(payload?.totalGroups || groups.length || 0),
        groups: groups.map((group) => ({
          ...group,
          products: Array.isArray(group?.products)
            ? group.products
                .map((product) => mapProduct(product))
                .filter((product) => product !== null)
            : [],
        })),
      };
    } catch (err) {
      console.error("❌ Mükerrer ürün grupları alınamadı:", err);
      return { totalGroups: 0, groups: [] };
    }
  },

  getAdminOverrideSettings: async () => {
    const response = await api.get("/api/products/admin/override-settings");
    const payload = response?.data || response;

    return {
      id: payload?.id || 0,
      defaultAdminOverrideName: payload?.defaultAdminOverrideName === true,
      defaultAdminOverridePrice: payload?.defaultAdminOverridePrice === true,
      defaultAdminOverrideCategory: payload?.defaultAdminOverrideCategory === true,
      updatedAt: payload?.updatedAt || null,
      updatedByUserName: payload?.updatedByUserName || null,
    };
  },

  updateAdminOverrideSettings: async (settings) => {
    const payload = {
      defaultAdminOverrideName:
        settings?.defaultAdminOverrideName === true,
      defaultAdminOverridePrice:
        settings?.defaultAdminOverridePrice === true,
      defaultAdminOverrideCategory:
        settings?.defaultAdminOverrideCategory === true,
    };

    const response = await api.put("/api/products/admin/override-settings", payload);
    const result = response?.data?.data || response?.data || response;

    return {
      id: result?.id || 0,
      defaultAdminOverrideName: result?.defaultAdminOverrideName === true,
      defaultAdminOverridePrice: result?.defaultAdminOverridePrice === true,
      defaultAdminOverrideCategory: result?.defaultAdminOverrideCategory === true,
      updatedAt: result?.updatedAt || null,
      updatedByUserName: result?.updatedByUserName || null,
    };
  },

  /**
   * Ürünü silmeden pasife alır.
   * Backend: PATCH /api/products/{id}/deactivate
   */
  deactivateAdmin: async (id) => {
    try {
      if (!id) {
        throw new Error("Ürün ID gerekli");
      }

      const response = await api.patch(`/api/products/${id}/deactivate`);
      const result = response?.data || response;
      notifySubscribers("update", { id, isActive: false, ...result });
      return result;
    } catch (err) {
      console.error(`❌ Ürün pasife alma hatası (ID: ${id}):`, err);
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
  importExcel: async (file, imageFiles = []) => {
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
          "Sadece Excel (.xlsx, .xls) veya CSV dosyaları kabul edilir",
        );
      }

      // Dosya boyutu kontrolü (200MB — görseller dahil)
      const maxSize = 200 * 1024 * 1024;
      if (file.size > maxSize) {
        throw new Error("Dosya boyutu maksimum 200MB olabilir");
      }

      const formData = new FormData();
      formData.append("file", file);

      // Görsel dosyalarını ekle (tümü "images" key'i altında, backend IFormFileCollection alır)
      if (imageFiles && imageFiles.length > 0) {
        for (const img of imageFiles) {
          formData.append("images", img);
        }
      }

      const response = await api.post("/api/products/import/excel", formData, {
        headers: { "Content-Type": "multipart/form-data" },
        timeout: 300000, // 5 dakika (görseller dahil büyük yüklemeler için)
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
          "Sadece resim dosyaları (jpg, png, gif, webp) yüklenebilir",
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
