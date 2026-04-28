// src/services/bulkProductFetcher.js
// ============================================================
// TOPLU ÜRÜN ÇEKİCİ - Sequential API Fetcher with Retry Logic
// ============================================================
// Bu modül, büyük miktarda ürünü (5000+) API'den sıralı şekilde
// çeker. Tarayıcı donmasını önlemek için async/await yapısı,
// retry mekanizması, throttle ve bellek yönetimi içerir.
// Promise.all KULLANILMAZ - sıralı (sequential) istek yapılır.
// ============================================================

import api from "./api";

// ============================================================
// KONFIGÜRASYON SABİTLERİ
// ============================================================
const DEFAULT_CONFIG = {
  // Sayfa başına ürün sayısı (API'nin desteklediği maksimum değer)
  PAGE_SIZE: 50,

  // İstekler arası bekleme süresi (ms) - Rate limit önleme
  THROTTLE_DELAY_MS: 300,

  // Hata durumunda maksimum yeniden deneme sayısı
  MAX_RETRY_COUNT: 3,

  // Retry öncesi bekleme süresi (ms)
  RETRY_DELAY_MS: 3000,

  // Bellek optimizasyonu için batch kayıt eşiği
  // Bu sayıya ulaşınca veriler localStorage/IndexedDB'ye taşınabilir
  MEMORY_FLUSH_THRESHOLD: 500,

  // API timeout süresi (ms)
  REQUEST_TIMEOUT_MS: 30000,

  // API endpoint'leri (sayfalama destekleyen)
  ENDPOINTS: {
    // Admin endpoint - sayfalı ürün listesi (PagedResult formatında)
    ADMIN_PAGED: "/api/products/admin/paged",
    // Fallback: eski endpoint (array döndürür)
    ADMIN_ALL: "/api/products/admin/all",
    // Public endpoint - fallback
    PUBLIC_PAGED: "/api/products",
    // Arama endpoint'i - sayfalı
    SEARCH: "/api/products/search",
  },
};

// ============================================================
// PROGRESS CALLBACK TİPLERİ
// ============================================================
/**
 * @typedef {Object} ProgressInfo
 * @property {number} currentPage - Şu anki sayfa numarası
 * @property {number} totalPages - Toplam sayfa sayısı
 * @property {number} fetchedCount - Şu ana kadar çekilen ürün sayısı
 * @property {number} totalCount - Toplam ürün sayısı (biliniyorsa)
 * @property {number} percentage - İlerleme yüzdesi (0-100)
 * @property {string} status - Durum mesajı
 * @property {number} elapsedMs - Geçen süre (ms)
 * @property {number} estimatedRemainingMs - Tahmini kalan süre (ms)
 */

/**
 * @typedef {Object} FetchResult
 * @property {boolean} success - İşlem başarılı mı
 * @property {Array} products - Çekilen ürün listesi
 * @property {number} totalFetched - Toplam çekilen ürün sayısı
 * @property {Array} failedPages - Başarısız olan sayfa numaraları
 * @property {number} elapsedMs - Toplam geçen süre (ms)
 * @property {Object} stats - İstatistikler
 */

// ============================================================
// YARDIMCI FONKSİYONLAR
// ============================================================

/**
 * Belirtilen süre kadar bekler (non-blocking)
 * @param {number} ms - Bekleme süresi (milisaniye)
 * @returns {Promise<void>}
 */
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

/**
 * API yanıtından items array'ini ve metadata'yı çıkarır
 * PagedResult<T> yapısını destekler (Backend: items, total, skip, take)
 * @param {any} response - API yanıtı
 * @param {number} pageSize - Sayfa boyutu (totalPages hesabı için)
 * @returns {{ items: Array, totalCount: number, pageNumber: number, totalPages: number }}
 */
const extractPagedData = (response, pageSize = DEFAULT_CONFIG.PAGE_SIZE) => {
  // Varsayılan değerler
  let items = [];
  let totalCount = 0;
  let pageNumber = 1;
  let totalPages = 1;

  // Yanıt doğrudan array ise (fallback endpoint)
  if (Array.isArray(response)) {
    return {
      items: response,
      totalCount: response.length,
      pageNumber: 1,
      totalPages: 1,
    };
  }

  // PagedResult yapısı kontrolü
  if (response && typeof response === "object") {
    // Items array'i - Backend PagedResult: items property
    if (Array.isArray(response.items)) {
      items = response.items;
    } else if (Array.isArray(response.data?.items)) {
      items = response.data.items;
    } else if (Array.isArray(response.data)) {
      items = response.data;
    }

    // Metadata - Backend PagedResult: total, skip, take
    // total: toplam kayıt sayısı
    // skip: atlanan kayıt sayısı (offset)
    // take: alınan kayıt sayısı (limit)
    totalCount =
      response.total ??
      response.totalCount ??
      response.data?.total ??
      response.data?.totalCount ??
      items.length;

    // Skip'ten sayfa numarasını hesapla
    const skip = response.skip ?? response.data?.skip ?? 0;
    const take = response.take ?? response.data?.take ?? pageSize;
    pageNumber = Math.floor(skip / take) + 1;

    // Toplam sayfa sayısını hesapla
    totalPages =
      response.totalPages ??
      response.pages ??
      response.data?.totalPages ??
      Math.ceil(totalCount / DEFAULT_CONFIG.PAGE_SIZE);
  }

  return { items, totalCount, pageNumber, totalPages };
};

/**
 * Ürün verisini normalize eder (mapProduct ile uyumlu)
 * @param {Object} rawProduct - Ham ürün verisi
 * @returns {Object} Normalize edilmiş ürün
 */
const normalizeProduct = (rawProduct) => {
  if (!rawProduct || typeof rawProduct !== "object") {
    return null;
  }

  const basePrice =
    parseFloat(rawProduct.price ?? rawProduct.unitPrice ?? 0) || 0;
  const special =
    rawProduct.specialPrice ??
    rawProduct.discountPrice ??
    rawProduct.discount_price ??
    null;

  let price = basePrice;
  let originalPrice = null;
  let discountPercentage = 0;

  if (
    special !== null &&
    typeof special === "number" &&
    special > 0 &&
    basePrice > 0 &&
    special < basePrice
  ) {
    price = special;
    originalPrice = basePrice;
    discountPercentage = Math.round(100 - (special / basePrice) * 100);
  }

  const stock =
    parseInt(
      rawProduct.stock ??
        rawProduct.stockQuantity ??
        rawProduct.stock_quantity ??
        0,
    ) || 0;

  return {
    id: rawProduct.id,
    name: rawProduct.name || rawProduct.title || "",
    category: rawProduct.category_name || rawProduct.category || "",
    categoryId: rawProduct.categoryId ?? rawProduct.category_id ?? null,
    categoryName:
      rawProduct.categoryName ||
      rawProduct.category_name ||
      rawProduct.category ||
      "",
    price,
    originalPrice,
    discountPrice: special,
    specialPrice: special,
    discountPercentage,
    imageUrl:
      rawProduct.image_url || rawProduct.image || rawProduct.imageUrl || "",
    stock,
    stockQuantity: stock,
    description: rawProduct.description || "",
    sku: rawProduct.sku || rawProduct.SKU || "",
    unitWeightGrams:
      rawProduct.unitWeightGrams || rawProduct.unit_weight_grams || 0,
    isActive: rawProduct.isActive !== false,
    createdAt: rawProduct.createdAt || rawProduct.created_at || null,
    updatedAt: rawProduct.updatedAt || rawProduct.updated_at || null,
  };
};

// ============================================================
// ANA FETCH FONKSİYONU - SIRALI İSTEK DÖNGÜSÜ
// ============================================================

/**
 * Tüm ürünleri sıralı (sequential) olarak çeker.
 * Promise.all KULLANMAZ - her sayfa tek tek çekilir.
 *
 * @param {Object} options - Yapılandırma seçenekleri
 * @param {number} [options.pageSize=50] - Sayfa başına ürün sayısı
 * @param {number} [options.throttleDelayMs=300] - İstekler arası bekleme (ms)
 * @param {number} [options.maxRetryCount=3] - Maksimum retry sayısı
 * @param {number} [options.retryDelayMs=3000] - Retry öncesi bekleme (ms)
 * @param {Function} [options.onProgress] - İlerleme callback'i
 * @param {Function} [options.onError] - Hata callback'i
 * @param {Function} [options.onPageComplete] - Her sayfa tamamlandığında callback
 * @param {AbortSignal} [options.signal] - İptal sinyali (AbortController)
 * @returns {Promise<FetchResult>} Sonuç objesi
 */
export const fetchAllProductsSequential = async (options = {}) => {
  // Konfigürasyon - varsayılanlarla birleştir
  const config = {
    pageSize: options.pageSize || DEFAULT_CONFIG.PAGE_SIZE,
    throttleDelayMs:
      options.throttleDelayMs || DEFAULT_CONFIG.THROTTLE_DELAY_MS,
    maxRetryCount: options.maxRetryCount || DEFAULT_CONFIG.MAX_RETRY_COUNT,
    retryDelayMs: options.retryDelayMs || DEFAULT_CONFIG.RETRY_DELAY_MS,
    onProgress: options.onProgress || (() => {}),
    onError: options.onError || (() => {}),
    onPageComplete: options.onPageComplete || (() => {}),
    signal: options.signal || null,
  };

  // Sonuç değişkenleri
  const allProducts = []; // Ana veri deposu (Data_Store)
  const failedPages = []; // Başarısız sayfalar
  const startTime = Date.now();

  // Durum değişkenleri
  let currentPage = 1;
  let totalPages = null; // İlk istekten sonra belirlenecek
  let totalCount = 0;
  let consecutiveErrors = 0;

  // İstatistikler
  const stats = {
    totalRequests: 0,
    successfulRequests: 0,
    failedRequests: 0,
    retryCount: 0,
    avgResponseTimeMs: 0,
    responseTimes: [],
  };

  console.log("🚀 Toplu ürün çekme işlemi başlatıldı...");
  console.log(
    `📊 Konfigürasyon: pageSize=${config.pageSize}, throttle=${config.throttleDelayMs}ms`,
  );

  // ============================================================
  // SIRALI DÖNGÜ - for/while ile sequential request
  // ============================================================
  while (true) {
    // İptal kontrolü
    if (config.signal?.aborted) {
      console.warn("⚠️ İşlem kullanıcı tarafından iptal edildi.");
      break;
    }

    // Sayfa parametrelerini oluştur
    const pageParams = `?page=${currentPage}&size=${config.pageSize}`;
    const requestStartTime = Date.now();

    let pageSuccess = false;
    let retryAttempt = 0;

    // ============================================================
    // RETRY MEKANIZMASI - try-catch bloğu
    // ============================================================
    while (retryAttempt < config.maxRetryCount && !pageSuccess) {
      try {
        stats.totalRequests++;

        // API isteği (await ile bekle - sequential)
        const response = await api.get(
          `${DEFAULT_CONFIG.ENDPOINTS.ADMIN_PAGED}${pageParams}`,
          {
            timeout: DEFAULT_CONFIG.REQUEST_TIMEOUT_MS,
            signal: config.signal,
          },
        );

        // Yanıtı parse et (pageSize parametresi ile)
        const {
          items,
          totalCount: tc,
          totalPages: tp,
        } = extractPagedData(response, config.pageSize);

        // İlk sayfada toplam değerleri ayarla
        if (currentPage === 1) {
          totalCount = tc;
          totalPages = tp || Math.ceil(tc / config.pageSize);
          console.log(
            `📦 Toplam ${totalCount} ürün, ${totalPages} sayfa bulundu.`,
          );
        }

        // Ürünleri normalize et ve ana diziye ekle
        const normalizedProducts = items
          .map(normalizeProduct)
          .filter((p) => p !== null);

        allProducts.push(...normalizedProducts);

        // İstatistikleri güncelle
        const responseTime = Date.now() - requestStartTime;
        stats.responseTimes.push(responseTime);
        stats.successfulRequests++;
        stats.avgResponseTimeMs =
          stats.responseTimes.reduce((a, b) => a + b, 0) /
          stats.responseTimes.length;

        // Başarılı - döngüden çık
        pageSuccess = true;
        consecutiveErrors = 0;

        // Sayfa tamamlandı callback'i
        config.onPageComplete({
          page: currentPage,
          itemsCount: normalizedProducts.length,
          totalFetched: allProducts.length,
        });
      } catch (error) {
        retryAttempt++;
        stats.retryCount++;
        consecutiveErrors++;

        const errorMessage =
          error.response?.data?.message || error.message || "Bilinmeyen hata";

        console.warn(
          `⚠️ Sayfa ${currentPage} - Hata (Deneme ${retryAttempt}/${config.maxRetryCount}): ${errorMessage}`,
        );

        // Hata callback'i
        config.onError({
          page: currentPage,
          attempt: retryAttempt,
          error: errorMessage,
          willRetry: retryAttempt < config.maxRetryCount,
        });

        // Son deneme değilse bekle ve tekrar dene
        if (retryAttempt < config.maxRetryCount) {
          console.log(`⏳ ${config.retryDelayMs}ms bekleniyor...`);
          await sleep(config.retryDelayMs);
        }
      }
    }

    // Tüm retry'lar başarısız olduysa sayfayı hatalı işaretle
    if (!pageSuccess) {
      console.error(`❌ Sayfa ${currentPage} - Tüm denemeler başarısız!`);
      failedPages.push(currentPage);
      stats.failedRequests++;
    }

    // ============================================================
    // İLERLEME TAKİBİ (Progress Bar)
    // ============================================================
    const elapsedMs = Date.now() - startTime;
    const progressPercentage = totalPages
      ? Math.round((currentPage / totalPages) * 100)
      : 0;

    // Tahmini kalan süre hesapla
    const avgTimePerPage = elapsedMs / currentPage;
    const remainingPages = (totalPages || 1) - currentPage;
    const estimatedRemainingMs = avgTimePerPage * remainingPages;

    // Progress callback'i çağır
    config.onProgress({
      currentPage,
      totalPages: totalPages || "?",
      fetchedCount: allProducts.length,
      totalCount,
      percentage: progressPercentage,
      status: `Sayfa ${currentPage}/${totalPages || "?"} tamamlandı`,
      elapsedMs,
      estimatedRemainingMs,
    });

    // Konsola log
    console.log(
      `📄 [${progressPercentage}%] Sayfa ${currentPage}/${totalPages || "?"} ` +
        `| Çekilen: ${allProducts.length}/${totalCount} ürün ` +
        `| Süre: ${(elapsedMs / 1000).toFixed(1)}s`,
    );

    // ============================================================
    // DÖNGÜ SONLANDIRMA KONTROLÜ
    // ============================================================
    // Son sayfaya ulaştık mı?
    if (totalPages !== null && currentPage >= totalPages) {
      console.log("✅ Tüm sayfalar tamamlandı!");
      break;
    }

    // Hiç ürün gelmiyorsa döngüyü sonlandır (güvenlik)
    if (pageSuccess && allProducts.length === 0 && currentPage > 1) {
      console.warn("⚠️ Boş yanıt alındı, döngü sonlandırılıyor.");
      break;
    }

    // Art arda çok fazla hata varsa dur (sunucu sorunu)
    if (consecutiveErrors >= 5) {
      console.error("❌ Art arda 5 hata! İşlem durduruluyor.");
      break;
    }

    // ============================================================
    // THROTTLE (NEFES ALDIRMA) GECİKMESİ
    // ============================================================
    // Rate limit'i önlemek için bekle
    await sleep(config.throttleDelayMs);

    // Sonraki sayfaya geç
    currentPage++;
  }

  // ============================================================
  // SONUÇ HAZIRLA VE DÖNDÜR
  // ============================================================
  const totalElapsedMs = Date.now() - startTime;

  const result = {
    success: failedPages.length === 0,
    products: allProducts,
    totalFetched: allProducts.length,
    failedPages,
    elapsedMs: totalElapsedMs,
    stats: {
      ...stats,
      totalPages: totalPages || currentPage,
      pagesProcessed: currentPage,
      avgResponseTimeMs: Math.round(stats.avgResponseTimeMs),
    },
  };

  // Final log
  console.log("════════════════════════════════════════════════");
  console.log("📊 TOPLU ÜRÜN ÇEKME İŞLEMİ TAMAMLANDI");
  console.log("════════════════════════════════════════════════");
  console.log(`✅ Başarıyla çekilen: ${allProducts.length} ürün`);
  console.log(
    `❌ Başarısız sayfalar: ${failedPages.length > 0 ? failedPages.join(", ") : "Yok"}`,
  );
  console.log(`⏱️ Toplam süre: ${(totalElapsedMs / 1000).toFixed(2)} saniye`);
  console.log(
    `📈 Ortalama yanıt süresi: ${Math.round(stats.avgResponseTimeMs)}ms`,
  );
  console.log(`🔄 Toplam retry: ${stats.retryCount}`);
  console.log("════════════════════════════════════════════════");

  return result;
};

// ============================================================
// CSV/EXCEL EXPORT FONKSİYONU
// ============================================================

/**
 * Ürün listesini CSV formatına dönüştürür
 * @param {Array} products - Ürün listesi
 * @returns {string} CSV içeriği
 */
export const productsToCSV = (products) => {
  if (!Array.isArray(products) || products.length === 0) {
    return "";
  }

  // CSV başlık satırı
  const headers = [
    "ID",
    "Ürün Adı",
    "Kategori",
    "Fiyat",
    "İndirimli Fiyat",
    "İndirim %",
    "Stok",
    "SKU",
    "Açıklama",
    "Aktif",
    "Görsel URL",
  ];

  // CSV satırlarını oluştur
  const rows = products.map((p) => [
    p.id || "",
    `"${(p.name || "").replace(/"/g, '""')}"`, // Çift tırnak escape
    `"${(p.categoryName || "").replace(/"/g, '""')}"`,
    p.price || 0,
    p.specialPrice || "",
    p.discountPercentage || "",
    p.stock || 0,
    p.sku || "",
    `"${(p.description || "").replace(/"/g, '""').substring(0, 200)}"`,
    p.isActive ? "Evet" : "Hayır",
    p.imageUrl || "",
  ]);

  // UTF-8 BOM + başlık + satırlar
  const BOM = "\uFEFF"; // Excel'de Türkçe karakter desteği için
  const csv =
    BOM + headers.join(";") + "\n" + rows.map((r) => r.join(";")).join("\n");

  return csv;
};

/**
 * CSV içeriğini dosya olarak indirir
 * @param {string} csvContent - CSV içeriği
 * @param {string} [filename="urunler.csv"] - Dosya adı
 */
export const downloadCSV = (csvContent, filename = "urunler.csv") => {
  const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
  const link = document.createElement("a");
  const url = URL.createObjectURL(blob);

  link.setAttribute("href", url);
  link.setAttribute("download", filename);
  link.style.visibility = "hidden";

  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  URL.revokeObjectURL(url);
};

// ============================================================
// INDEXEDDB BELLEK YÖNETİMİ
// ============================================================

const DB_NAME = "BulkProductFetcherDB";
const DB_VERSION = 1;
const STORE_NAME = "products";

/**
 * IndexedDB bağlantısı açar
 * @returns {Promise<IDBDatabase>}
 */
const openDatabase = () => {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);

    request.onerror = () => reject(request.error);
    request.onsuccess = () => resolve(request.result);

    request.onupgradeneeded = (event) => {
      const db = event.target.result;
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME, { keyPath: "id" });
      }
    };
  });
};

/**
 * Ürünleri IndexedDB'ye kaydeder (bellek optimizasyonu)
 * @param {Array} products - Kaydedilecek ürünler
 * @returns {Promise<void>}
 */
export const saveProductsToIndexedDB = async (products) => {
  try {
    const db = await openDatabase();
    const transaction = db.transaction(STORE_NAME, "readwrite");
    const store = transaction.objectStore(STORE_NAME);

    // Toplu ekleme
    products.forEach((product) => {
      store.put(product);
    });

    return new Promise((resolve, reject) => {
      transaction.oncomplete = () => {
        db.close();
        resolve();
      };
      transaction.onerror = () => {
        db.close();
        reject(transaction.error);
      };
    });
  } catch (error) {
    console.error("IndexedDB kayıt hatası:", error);
    throw error;
  }
};

/**
 * IndexedDB'den tüm ürünleri okur
 * @returns {Promise<Array>}
 */
export const loadProductsFromIndexedDB = async () => {
  try {
    const db = await openDatabase();
    const transaction = db.transaction(STORE_NAME, "readonly");
    const store = transaction.objectStore(STORE_NAME);
    const request = store.getAll();

    return new Promise((resolve, reject) => {
      request.onsuccess = () => {
        db.close();
        resolve(request.result || []);
      };
      request.onerror = () => {
        db.close();
        reject(request.error);
      };
    });
  } catch (error) {
    console.error("IndexedDB okuma hatası:", error);
    return [];
  }
};

/**
 * IndexedDB'yi temizler
 * @returns {Promise<void>}
 */
export const clearIndexedDB = async () => {
  try {
    const db = await openDatabase();
    const transaction = db.transaction(STORE_NAME, "readwrite");
    const store = transaction.objectStore(STORE_NAME);
    store.clear();

    return new Promise((resolve) => {
      transaction.oncomplete = () => {
        db.close();
        resolve();
      };
    });
  } catch (error) {
    console.error("IndexedDB temizleme hatası:", error);
  }
};

// ============================================================
// GELIŞMIŞ FETCH - BELLEK YÖNETİMLİ
// ============================================================

/**
 * Tüm ürünleri çeker ve bellek optimizasyonu uygular.
 * Belirtilen eşik aşıldığında veriler IndexedDB'ye taşınır.
 *
 * @param {Object} options - fetchAllProductsSequential seçenekleri
 * @param {number} [options.memoryFlushThreshold=500] - Bellek boşaltma eşiği
 * @returns {Promise<FetchResult>}
 */
export const fetchAllProductsWithMemoryManagement = async (options = {}) => {
  const memoryThreshold =
    options.memoryFlushThreshold || DEFAULT_CONFIG.MEMORY_FLUSH_THRESHOLD;

  let inMemoryProducts = [];
  let flushedToDBCount = 0;

  // Önce IndexedDB'yi temizle
  await clearIndexedDB();

  // Ana fetch fonksiyonunu çağır, onPageComplete'i override et
  const originalOnPageComplete = options.onPageComplete || (() => {});

  const result = await fetchAllProductsSequential({
    ...options,
    onPageComplete: async (info) => {
      // Orijinal callback'i çağır
      originalOnPageComplete(info);

      // Bellek kontrolü
      if (inMemoryProducts.length >= memoryThreshold) {
        console.log(
          `💾 Bellek eşiği aşıldı (${inMemoryProducts.length}), IndexedDB'ye yazılıyor...`,
        );

        await saveProductsToIndexedDB(inMemoryProducts);
        flushedToDBCount += inMemoryProducts.length;
        inMemoryProducts = [];

        // Tarayıcıya nefes aldır
        await sleep(50);
      }
    },
  });

  // Kalan ürünleri de IndexedDB'ye yaz
  if (inMemoryProducts.length > 0) {
    await saveProductsToIndexedDB(inMemoryProducts);
    flushedToDBCount += inMemoryProducts.length;
  }

  console.log(`💾 Toplam ${flushedToDBCount} ürün IndexedDB'ye kaydedildi.`);

  return result;
};

// ============================================================
// KULLANIM ÖRNEKLERİ (JSDoc)
// ============================================================

/**
 * @example
 * // Basit kullanım
 * const result = await fetchAllProductsSequential();
 * console.log(result.products); // Tüm ürünler
 *
 * @example
 * // Progress callback ile
 * const result = await fetchAllProductsSequential({
 *   onProgress: (info) => {
 *     console.log(`%${info.percentage} tamamlandı`);
 *     // UI'da progress bar güncelle
 *     setProgress(info.percentage);
 *   },
 *   onError: (err) => {
 *     console.warn(`Sayfa ${err.page} hata: ${err.error}`);
 *   }
 * });
 *
 * @example
 * // İptal edilebilir
 * const controller = new AbortController();
 * setTimeout(() => controller.abort(), 30000); // 30sn sonra iptal
 *
 * const result = await fetchAllProductsSequential({
 *   signal: controller.signal
 * });
 *
 * @example
 * // CSV export
 * const result = await fetchAllProductsSequential();
 * const csv = productsToCSV(result.products);
 * downloadCSV(csv, "tum_urunler.csv");
 */

// Default export
export default {
  fetchAllProductsSequential,
  fetchAllProductsWithMemoryManagement,
  productsToCSV,
  downloadCSV,
  saveProductsToIndexedDB,
  loadProductsFromIndexedDB,
  clearIndexedDB,
  DEFAULT_CONFIG,
};
