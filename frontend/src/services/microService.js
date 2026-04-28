import { debugLog, shouldUseMockData, API_CONFIG } from "../config/apiConfig";
import api from "./api";

// Backend API endpoint'leri - AdminMicroController'a karşılık gelir
const baseAdmin = "/api/admin/micro";
const apiBaseUrl = API_CONFIG.BASE_URL; // Excel import için full URL gerekli

// ============================================================================
// Mock Data - Geliştirme ortamında backend çalışmadığında kullanılır
// ============================================================================
let mockMicroProducts = [
  {
    id: 1,
    name: "Dana Kuşbaşı",
    price: 89.9,
    category: "Et ve Et Ürünleri",
    sku: "ET001",
    barcode: "8690000000001",
  },
  {
    id: 2,
    name: "Pınar Süt 1L",
    price: 12.5,
    category: "Süt Ürünleri",
    sku: "SUT001",
    barcode: "8690000000002",
  },
  {
    id: 3,
    name: "Domates Kg",
    price: 8.75,
    category: "Meyve ve Sebze",
    sku: "SEB001",
    barcode: "8690000000003",
  },
  {
    id: 4,
    name: "Coca Cola 330ml",
    price: 5.5,
    category: "İçecekler",
    sku: "ICE001",
    barcode: "8690000000004",
  },
];

let mockMicroStocks = [
  { productId: 1, quantity: 24, warehouseCode: "DEPO1" },
  { productId: 2, quantity: 50, warehouseCode: "DEPO1" },
  { productId: 3, quantity: 12, warehouseCode: "DEPO1" },
  { productId: 4, quantity: 80, warehouseCode: "DEPO1" },
];

let mockProductSynced = false;

const clone = (data) => JSON.parse(JSON.stringify(data));
const buildMockResponse = (message, success = true) => ({
  success,
  message,
  timestamp: new Date().toISOString(),
});

const toNumberOr = (value, fallback = 0) => {
  if (typeof value === "number") {
    return Number.isFinite(value) ? value : fallback;
  }

  if (typeof value === "string") {
    const trimmed = value.trim();
    if (!trimmed) return fallback;

    // Accept values like "1.234,56", "1234,56", "1,234.56", "₺1.234,56"
    const normalized = trimmed
      .replace(/[^\d,.-]/g, "")
      .replace(/\.(?=\d{3}(\D|$))/g, "")
      .replace(/,/g, ".");

    const parsed = Number(normalized);
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
};

const normalizeProductItem = (item) => {
  const rawPrice =
    item?.price ?? item?.satisFiyati ?? item?.fiyat ?? item?.Price ?? 0;
  const rawStock =
    item?.stockQuantity ??
    item?.quantity ??
    item?.depoMiktari ??
    item?.satilabilirMiktar ??
    item?.stokMiktar ??
    item?.StockQuantity ??
    0;

  return {
    ...item,
    id: item?.id ?? item?.Id ?? item?.sku ?? item?.stokKod ?? item?.StoKod,
    sku: item?.sku ?? item?.stokKod ?? item?.StoKod ?? item?.SKU ?? "",
    name: item?.name ?? item?.stokAd ?? item?.stoIsim ?? item?.StoIsim ?? "",
    category:
      item?.category ??
      item?.categoryName ??
      item?.grupKod ??
      item?.StoAltgrupKod ??
      item?.StoAnagrupKod ??
      "-",
    price: toNumberOr(rawPrice, 0),
    stockQuantity: Math.max(0, Math.floor(toNumberOr(rawStock, 0))),
  };
};

const normalizeStockItem = (item) => {
  const rawQuantity =
    item?.quantity ??
    item?.availableQuantity ??
    item?.depoMiktari ??
    item?.satilabilirMiktar ??
    item?.stokMiktar ??
    item?.Quantity ??
    0;

  return {
    ...item,
    productId:
      item?.productId ??
      item?.id ??
      item?.sku ??
      item?.stokKod ??
      item?.StoKod ??
      "",
    sku: item?.sku ?? item?.stokKod ?? item?.StoKod ?? "",
    quantity: Math.max(0, Math.floor(toNumberOr(rawQuantity, 0))),
  };
};

const normalizeStokListesiItem = (item) => {
  const rawPrice = item?.satisFiyati ?? item?.price ?? item?.fiyat ?? 0;
  const rawQuantity =
    item?.satilabilirMiktar ?? item?.depoMiktari ?? item?.quantity ?? 0;

  return {
    ...item,
    id: item?.stokKod ?? item?.sku ?? item?.id ?? item?.productId ?? "",
    productId: item?.stokKod ?? item?.sku ?? item?.id ?? item?.productId ?? "",
    sku: item?.stokKod ?? item?.sku ?? item?.id ?? "",
    name: item?.stokAd ?? item?.name ?? "",
    category: item?.grupKod ?? item?.category ?? "-",
    barcode: item?.barkod ?? item?.barcode ?? "",
    price: toNumberOr(rawPrice, 0),
    stockQuantity: Math.max(0, Math.floor(toNumberOr(rawQuantity, 0))),
    quantity: Math.max(0, Math.floor(toNumberOr(rawQuantity, 0))),
  };
};

const fetchStokListesiPage = async (params = {}) => {
  const queryParams = new URLSearchParams();
  if (params.sayfa) queryParams.append("sayfa", params.sayfa);
  if (params.sayfaBuyuklugu)
    queryParams.append("sayfaBuyuklugu", params.sayfaBuyuklugu);
  if (params.depoNo !== undefined) queryParams.append("depoNo", params.depoNo);
  if (params.fiyatListesiNo !== undefined)
    queryParams.append("fiyatListesiNo", params.fiyatListesiNo);
  if (params.stokKod) queryParams.append("stokKod", params.stokKod);
  if (params.grupKod) queryParams.append("grupKod", params.grupKod);
  if (params.sadeceAktif !== undefined)
    queryParams.append("sadeceAktif", String(params.sadeceAktif));
  if (params.aramaMetni) queryParams.append("aramaMetni", params.aramaMetni);
  if (params.sadeceStoklu !== undefined && params.sadeceStoklu !== null) {
    queryParams.append("sadeceStoklu", String(params.sadeceStoklu));
  }

  const url = `${baseAdmin}/stok-listesi${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;
  return api.get(url);
};

const fetchAllStokListesiRows = async (params = {}) => {
  const pageSize = Number(params.sayfaBuyuklugu || params.pageSize || 100);
  const maxPages = Number(params.maxPages || 10);
  let page = 1;
  let allRows = [];
  let expectedTotal = 0;

  while (page <= maxPages) {
    const response = await fetchStokListesiPage({
      ...params,
      sayfa: page,
      sayfaBuyuklugu: pageSize,
    });

    const batchRaw = Array.isArray(response)
      ? response
      : Array.isArray(response?.data)
        ? response.data
        : [];

    const batch = batchRaw.map(normalizeStokListesiItem);
    if (!batch.length) break;

    allRows = allRows.concat(batch);

    const totalCount = Number(
      response?.toplamKayit || response?.totalCount || 0,
    );
    if (Number.isFinite(totalCount) && totalCount > 0) {
      expectedTotal = totalCount;
    }
    if (expectedTotal > 0 && allRows.length >= expectedTotal) break;

    page += 1;
  }

  const seen = new Set();
  return allRows.filter((item) => {
    const key = item?.sku || item?.stokKod || item?.id;
    if (!key) return true;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
};

// ============================================================================
// MicroService - ERP/Mikro API Entegrasyon Servisi
// ============================================================================
export const MicroService = {
  // ==================== BAĞLANTI TESTİ ====================
  /**
   * Mikro API bağlantısını test eder
   * Backend: GET /api/admin/micro/test-connection
   */
  testConnection: async () => {
    if (shouldUseMockData()) {
      debugLog("Micro testConnection - Mock");
      return Promise.resolve({
        isConnected: true,
        message: "Mock bağlantı başarılı",
        apiUrl: "https://mock-api.local",
        timestamp: new Date().toISOString(),
      });
    }
    try {
      const response = await api.get(`${baseAdmin}/test-connection`);
      return response;
    } catch (error) {
      console.error("Bağlantı testi hatası:", error);
      return {
        isConnected: false,
        message: error.message || "Bağlantı kurulamadı",
        timestamp: new Date().toISOString(),
      };
    }
  },

  // ==================== GRUP KODLARI (KATEGORİLER) ====================
  /**
   * Mikro'daki benzersiz grup kodlarını (kategorileri) getirir.
   * Grup kodu dropdown'ı için kullanılır.
   * Backend: GET /api/admin/micro/grup-kodlari
   */
  getGrupKodlari: async () => {
    if (shouldUseMockData()) {
      debugLog("Micro getGrupKodlari - Mock");
      return Promise.resolve({
        success: true,
        source: "mock",
        data: [
          "Et ve Et Ürünleri",
          "Süt Ürünleri",
          "Meyve ve Sebze",
          "İçecekler",
          "Bakkaliye",
        ],
        count: 5,
      });
    }
    try {
      const response = await api.get(`${baseAdmin}/grup-kodlari`);
      return response;
    } catch (error) {
      console.error("Grup kodları getirilemedi:", error);
      return { success: false, message: error.message, data: [] };
    }
  },

  /**
   * Mikro'daki depo listesini getirir.
   * Backend: GET /api/admin/micro/depo-listesi
   */
  getDepoListesi: async () => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        source: "mock",
        data: [
          { depoNo: 0, depoAdi: "Tum Depolar" },
          { depoNo: 1, depoAdi: "Depo 1" },
          { depoNo: 2, depoAdi: "Depo 2" },
          { depoNo: 3, depoAdi: "Depo 3" },
        ],
      });
    }

    try {
      const response = await api.get(`${baseAdmin}/depo-listesi`);
      return response;
    } catch (error) {
      console.error("Depo listesi getirilemedi:", error);
      return {
        success: false,
        message: error.message,
        data: [{ depoNo: 0, depoAdi: "Tum Depolar" }],
      };
    }
  },

  // ==================== ÜRÜN SENKRONİZASYONU ====================
  /**
   * Yerel ürünleri Mikro ERP'ye senkronize eder
   * Backend: POST /api/admin/micro/sync-products
   */
  syncProducts: async () => {
    if (shouldUseMockData()) {
      if (!mockProductSynced) {
        const nextId =
          mockMicroProducts.length > 0
            ? Math.max(...mockMicroProducts.map((p) => p.id)) + 1
            : 1;
        const newProduct = {
          id: nextId,
          name: "ERP Yeni Ürün",
          price: 49.9,
          category: "ERP Senkron",
          sku: `ERP${nextId.toString().padStart(3, "0")}`,
        };
        mockMicroProducts.push(newProduct);
        mockMicroStocks.push({
          productId: nextId,
          quantity: 30,
          warehouseCode: "DEPO1",
        });
        mockProductSynced = true;
      }
      return Promise.resolve(
        buildMockResponse("ERP ile ürünler senkronize edildi (mock)"),
      );
    }
    const response = await api.post(`${baseAdmin}/sync-products`);
    return response;
  },

  /**
   * Siparişleri Mikro ERP'ye gönderir
   * Backend: POST /api/admin/micro/export-orders
   */
  exportOrders: async (orders) => {
    if (shouldUseMockData()) {
      debugLog("Micro exportOrders - Mock", orders);
      return Promise.resolve(
        buildMockResponse("Seçilen siparişler ERP'ye aktarıldı (mock)"),
      );
    }
    const response = await api.post(`${baseAdmin}/export-orders`, orders);
    return response;
  },

  // ==================== STOK VE FİYAT SENKRONİZASYONU ====================
  /**
   * ERP'den stokları çeker ve yerel veritabanını günceller
   * Backend: POST /api/admin/micro/sync-stocks-from-erp
   */
  syncStocksFromERP: async () => {
    if (shouldUseMockData()) {
      mockMicroStocks = mockMicroStocks.map((stock, index) => {
        const delta = index % 2 === 0 ? 5 : -3;
        return {
          ...stock,
          quantity: Math.max(0, stock.quantity + delta),
        };
      });
      return Promise.resolve(
        buildMockResponse("Stoklar ERP'den çekildi (mock)"),
      );
    }
    const response = await api.post(`${baseAdmin}/sync-stocks-from-erp`);
    return response;
  },

  /**
   * ERP'den fiyatları çeker ve yerel veritabanını günceller
   * Backend: POST /api/admin/micro/sync-prices-from-erp
   */
  syncPricesFromERP: async () => {
    if (shouldUseMockData()) {
      mockMicroProducts = mockMicroProducts.map((product, index) => {
        const factor = index % 2 === 0 ? 1.02 : 0.98;
        return {
          ...product,
          price: Number((product.price * factor).toFixed(2)),
        };
      });
      return Promise.resolve(
        buildMockResponse("Fiyatlar ERP'den çekildi (mock)"),
      );
    }
    const response = await api.post(`${baseAdmin}/sync-prices-from-erp`);
    return response;
  },

  // ==================== VERİ OKUMA ====================
  /**
   * Mikro ERP'den ürünleri getirir (yerel cache veya ERP'den)
   * Backend: GET /api/admin/micro/products
   */
  getProducts: async () => {
    if (shouldUseMockData()) {
      return Promise.resolve(clone(mockMicroProducts));
    }
    try {
      const rows = await fetchAllStokListesiRows({
        sayfaBuyuklugu: 100,
        depoNo: 0,
        fiyatListesiNo: 0,
        sadeceAktif: true,
        maxPages: 10,
      });

      return rows.map((item) => ({
        ...item,
        id: item.id || item.sku,
        sku: item.sku || item.stokKod || item.id,
        name: item.name || item.stokAd || "",
        category: item.category || item.grupKod || "-",
        price: toNumberOr(item.price ?? item.satisFiyati ?? 0, 0),
        stockQuantity: Math.max(
          0,
          Math.floor(
            toNumberOr(
              item.stockQuantity ?? item.quantity ?? item.depoMiktari ?? 0,
              0,
            ),
          ),
        ),
      }));
    } catch (error) {
      console.error("Ürünler getirilemedi:", error);
      return [];
    }
  },

  /**
   * Mikro ERP'den stokları getirir (yerel cache veya ERP'den)
   * Backend: GET /api/admin/micro/stocks
   */
  getStocks: async () => {
    if (shouldUseMockData()) {
      return Promise.resolve(clone(mockMicroStocks));
    }
    try {
      const rows = await fetchAllStokListesiRows({
        sayfaBuyuklugu: 100,
        depoNo: 0,
        fiyatListesiNo: 0,
        sadeceAktif: true,
        maxPages: 10,
      });

      return rows.map((item) => ({
        productId: item.productId || item.sku || item.stokKod || item.id || "",
        sku: item.sku || item.stokKod || item.id || "",
        quantity: Math.max(
          0,
          Math.floor(
            toNumberOr(
              item.quantity ?? item.stockQuantity ?? item.depoMiktari ?? 0,
              0,
            ),
          ),
        ),
        warehouseCode: item.warehouseCode || item.depoNo || "DEPO0",
      }));
    } catch (error) {
      console.error("Stoklar getirilemedi:", error);
      return [];
    }
  },

  // ==================== MİKRO API V2 DETAYLI ENDPOINT'LER ====================
  /**
   * Mikro API StokListesiV2 endpoint'inden detaylı stok listesi çeker
   * Backend: GET /api/admin/micro/stok-listesi
   *
   * @param {Object} params - Sorgu parametreleri:
   *   - sayfa: Sayfa numarası (varsayılan: 1)
   *   - sayfaBuyuklugu: Sayfa başına kayıt (varsayılan: 50)
   *   - depoNo: Depo numarası (0 = tüm depolar)
   *   - fiyatListesiNo: Fiyat listesi numarası (1-10 arası, varsayılan: 1 = Perakende)
   *   - stokKod: Stok kodu filtresi (opsiyonel)
   *   - grupKod: Grup kodu filtresi (opsiyonel)
   *   - sadeceAktif: Sadece aktif ürünler (varsayılan: true)
   *
   * @returns {Object} - { success, data[], toplamKayit, fiyatListesiNo, ... }
   */
  getStokListesi: async (params = {}) => {
    if (shouldUseMockData()) {
      debugLog("Micro getStokListesi - Mock", params);
      return Promise.resolve({
        success: true,
        data: mockMicroProducts.map((p, idx) => ({
          stokKod: p.sku,
          stokAd: p.name,
          barkod: p.barcode,
          satisFiyati: p.price,
          depoMiktari: mockMicroStocks[idx]?.quantity || 0,
          satilabilirMiktar: mockMicroStocks[idx]?.quantity || 0,
          birim: "ADET",
          grupKod: p.category,
          tumFiyatlar: [
            {
              listeNo: 1,
              aciklama: "Perakende",
              fiyat: p.price,
              kdvDahil: true,
            },
            {
              listeNo: 2,
              aciklama: "Toptan",
              fiyat: p.price * 0.9,
              kdvDahil: true,
            },
          ],
        })),
        totalCount: mockMicroProducts.length,
        page: params.sayfa || 1,
        pageSize: params.sayfaBuyuklugu || 20,
        fiyatListesiNo: params.fiyatListesiNo || 1,
      });
    }
    try {
      const queryParams = new URLSearchParams();
      if (params.sayfa) queryParams.append("sayfa", params.sayfa);
      if (params.sayfaBuyuklugu)
        queryParams.append("sayfaBuyuklugu", params.sayfaBuyuklugu);
      // depoNo=0 tüm depolar demek, bunu da gönder
      if (params.depoNo !== undefined)
        queryParams.append("depoNo", params.depoNo);
      // Fiyat listesi numarası (1-10 arası)
      if (params.fiyatListesiNo)
        queryParams.append("fiyatListesiNo", params.fiyatListesiNo);
      if (params.stokKod) queryParams.append("stokKod", params.stokKod);
      if (params.grupKod) queryParams.append("grupKod", params.grupKod);
      if (params.sadeceAktif !== undefined)
        queryParams.append("sadeceAktif", params.sadeceAktif);
      // Arama metni filtresi
      if (params.aramaMetni)
        queryParams.append("aramaMetni", params.aramaMetni);
      // Stok durumu filtresi: true = sadece stoklu, false = sadece stoksuz, undefined = hepsi
      if (params.sadeceStoklu !== undefined && params.sadeceStoklu !== null)
        queryParams.append("sadeceStoklu", params.sadeceStoklu);

      const url = `${baseAdmin}/stok-listesi${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;
      const response = await api.get(url);
      return response;
    } catch (error) {
      console.error("Stok listesi getirilemedi:", error);
      return { success: false, message: error.message, data: [] };
    }
  },

  /**
   * Cache'teki ürünleri sayfalı getirir (hızlı - local DB).
   * Backend: GET /api/admin/micro/cache/products
   */
  getCachedProducts: async (params = {}) => {
    if (shouldUseMockData()) {
      const page = Number(params.page || 1);
      const pageSize = Number(params.pageSize || 50);
      const start = (page - 1) * pageSize;
      const end = start + pageSize;
      const data = mockMicroProducts.slice(start, end).map((p, index) => ({
        stokKod: p.sku,
        stokAd: p.name,
        barkod: p.barcode,
        grupKod: p.category,
        birim: "ADET",
        satisFiyati: p.price,
        kdvOrani: 10,
        depoMiktari: mockMicroStocks[index]?.quantity ?? 0,
        satilabilirMiktar: mockMicroStocks[index]?.quantity ?? 0,
      }));

      return Promise.resolve({
        success: true,
        data,
        pagination: {
          page,
          pageSize,
          totalCount: mockMicroProducts.length,
          totalPages: Math.max(
            1,
            Math.ceil(mockMicroProducts.length / pageSize),
          ),
          hasPreviousPage: page > 1,
          hasNextPage: end < mockMicroProducts.length,
        },
      });
    }

    try {
      const queryParams = new URLSearchParams();
      if (params.page) queryParams.append("page", params.page);
      if (params.pageSize) queryParams.append("pageSize", params.pageSize);
      if (params.stokKod) queryParams.append("stokKod", params.stokKod);
      if (params.grupKod) queryParams.append("grupKod", params.grupKod);
      if (params.search) queryParams.append("search", params.search);
      if (params.sadeceStoklu !== undefined && params.sadeceStoklu !== null) {
        queryParams.append("sadeceStoklu", String(params.sadeceStoklu));
      }
      if (params.sadeceAktif !== undefined && params.sadeceAktif !== null) {
        queryParams.append("sadeceAktif", String(params.sadeceAktif));
      }
      if (params.sortBy) queryParams.append("sortBy", params.sortBy);
      if (params.sortDesc !== undefined) {
        queryParams.append("sortDesc", String(params.sortDesc));
      }

      const url = `${baseAdmin}/cache/products${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;
      return await api.get(url);
    } catch (error) {
      console.error("Cache ürünleri getirilemedi:", error);
      return {
        success: false,
        data: [],
        pagination: {
          page: Number(params.page || 1),
          pageSize: Number(params.pageSize || 50),
          totalCount: 0,
          totalPages: 0,
          hasPreviousPage: false,
          hasNextPage: false,
        },
        message: error.message,
      };
    }
  },

  // ==================== AKTİF/PASİF YÖNETİMİ ====================

  /**
   * Tek ürünün aktif/pasif durumunu değiştirir.
   * Backend: PUT /api/admin/micro/cache/products/{stokKod}/toggle-active
   * @param {string} stokKod - Stok kodu
   * @param {boolean} aktif - Yeni aktiflik durumu
   */
  toggleProductActive: async (stokKod, aktif) => {
    if (shouldUseMockData()) {
      debugLog("Micro toggleProductActive - Mock", { stokKod, aktif });
      const product = mockMicroProducts.find((p) => p.sku === stokKod);
      if (product) {
        product.aktif = aktif;
        return Promise.resolve({
          success: true,
          message: aktif ? "Ürün aktif edildi" : "Ürün pasif edildi",
          stokKod,
          aktif,
        });
      }
      return Promise.resolve({ success: false, message: "Ürün bulunamadı" });
    }

    try {
      const url = `${baseAdmin}/cache/products/${encodeURIComponent(stokKod)}/toggle-active?aktif=${aktif}`;
      return await api.put(url);
    } catch (error) {
      console.error("Ürün aktiflik değişikliği başarısız:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Birden fazla ürünün aktif/pasif durumunu toplu değiştirir.
   * Backend: PUT /api/admin/micro/cache/products/bulk-toggle-active
   * @param {string[]} stokKodlar - Stok kodları listesi
   * @param {boolean} aktif - Yeni aktiflik durumu
   */
  bulkToggleProductActive: async (stokKodlar, aktif) => {
    if (shouldUseMockData()) {
      debugLog("Micro bulkToggleProductActive - Mock", { stokKodlar, aktif });
      let successCount = 0;
      stokKodlar.forEach((stokKod) => {
        const product = mockMicroProducts.find((p) => p.sku === stokKod);
        if (product) {
          product.aktif = aktif;
          successCount++;
        }
      });
      return Promise.resolve({
        success: true,
        message: `${successCount} ürün ${aktif ? "aktif" : "pasif"} edildi`,
        stats: {
          successCount,
          failedCount: stokKodlar.length - successCount,
          failedCodes: [],
        },
      });
    }

    try {
      const url = `${baseAdmin}/cache/products/bulk-toggle-active`;
      return await api.put(url, { stokKodlar, aktif });
    } catch (error) {
      console.error("Toplu aktiflik değişikliği başarısız:", error);
      return { success: false, message: error.message };
    }
  },

  // ==================== EXCEL IMPORT ====================

  /**
   * Excel dosyasından aktif ürün listesini import eder.
   * Backend: POST /api/admin/micro/import/active-products
   * @param {File} file - Excel veya CSV dosyası
   */
  importActiveProducts: async (file) => {
    if (shouldUseMockData()) {
      debugLog("Micro importActiveProducts - Mock", { fileName: file.name });
      return Promise.resolve({
        success: true,
        message: "Mock import başarılı: 150 ürün aktif edildi",
        stats: {
          totalRows: 150,
          successCount: 150,
          failedCount: 0,
          skippedCount: 0,
        },
        details: [],
      });
    }

    try {
      const formData = new FormData();
      formData.append("file", file);

      const response = await fetch(
        `${apiBaseUrl}/api/admin/micro/import/active-products`,
        {
          method: "POST",
          body: formData,
          headers: {
            Authorization: `Bearer ${localStorage.getItem("token")}`,
          },
        },
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || "Import başarısız");
      }

      return await response.json();
    } catch (error) {
      console.error("Excel import başarısız:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Excel import template dosyasını indirir.
   * Backend: GET /api/admin/micro/import/template
   */
  downloadImportTemplate: async () => {
    if (shouldUseMockData()) {
      // Mock CSV indir
      const csvContent = "StokKod,Aktif\nORNEK001,true\nORNEK002,false\n";
      const blob = new Blob([csvContent], { type: "text/csv" });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "mikro_aktif_urunler_template.csv";
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
      return { success: true };
    }

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/admin/micro/import/template`,
        {
          method: "GET",
          headers: {
            Authorization: `Bearer ${localStorage.getItem("token")}`,
          },
        },
      );

      if (!response.ok) {
        throw new Error("Template indirilemedi");
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "mikro_aktif_urunler_template.csv";
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
      return { success: true };
    } catch (error) {
      console.error("Template indirilirken hata:", error);
      return { success: false, message: error.message };
    }
  },

  // ==================== MİKRO'YA SYNC (ToERP) ====================

  /**
   * Ürün bilgilerini Mikro ERP'ye sync eder.
   * Backend: PUT /api/admin/micro/sync/product
   * @param {Object} data - Ürün bilgileri
   */
  syncProductToMikro: async (data) => {
    if (shouldUseMockData()) {
      debugLog("Micro syncProductToMikro - Mock", data);
      return Promise.resolve({
        success: true,
        message: `Ürün ${data.stokKod} Mikro'ya senkronize edildi (Mock)`,
        stokKod: data.stokKod,
      });
    }

    try {
      return await api.put(`${baseAdmin}/sync/product`, data);
    } catch (error) {
      console.error("Ürün sync hatası:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Ürün fiyatını Mikro ERP'ye sync eder.
   * Backend: PUT /api/admin/micro/sync/price
   * @param {Object} data - Fiyat bilgileri
   */
  syncPriceToMikro: async (data) => {
    if (shouldUseMockData()) {
      debugLog("Micro syncPriceToMikro - Mock", data);
      return Promise.resolve({
        success: true,
        message: `Fiyat ${data.stokKod} Mikro'ya senkronize edildi (Mock)`,
        stokKod: data.stokKod,
        yeniFiyat: data.yeniFiyat,
      });
    }

    try {
      return await api.put(`${baseAdmin}/sync/price`, data);
    } catch (error) {
      console.error("Fiyat sync hatası:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Ürün bilgilerini ve fiyatı birlikte Mikro ERP'ye sync eder.
   * Backend: PUT /api/admin/micro/sync/product-full
   * @param {Object} data - Ürün ve fiyat bilgileri
   */
  syncProductFullToMikro: async (data) => {
    if (shouldUseMockData()) {
      debugLog("Micro syncProductFullToMikro - Mock", data);
      return Promise.resolve({
        success: true,
        message: `Ürün ${data.stokKod} tam senkronize edildi (Mock)`,
        stokKod: data.stokKod,
      });
    }

    try {
      return await api.put(`${baseAdmin}/sync/product-full`, data);
    } catch (error) {
      console.error("Tam ürün sync hatası:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Cache senkronizasyonu başlatır.
   * Backend: POST /api/admin/micro/cache/sync
   * syncMode: full | newOnly
   */
  syncProductCache: async (params = {}) => {
    if (shouldUseMockData()) {
      return Promise.resolve(
        buildMockResponse("Mock cache senkronizasyonu tamamlandı"),
      );
    }

    try {
      const queryParams = new URLSearchParams();
      queryParams.append("fiyatListesiNo", String(params.fiyatListesiNo ?? 1));
      queryParams.append("depoNo", String(params.depoNo ?? 0));
      queryParams.append("syncMode", params.syncMode || "newOnly");

      const url = `${baseAdmin}/cache/sync?${queryParams.toString()}`;
      return await api.post(url);
    } catch (error) {
      console.error("Cache senkronizasyonu başarısız:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Mikro sync tanılama verilerini getirir.
   * Backend: GET /api/admin/micro/sync/diagnostics
   */
  getSyncDiagnostics: async (hours = 24) => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        data: {
          since: new Date(Date.now() - hours * 60 * 60 * 1000).toISOString(),
          hours,
          summary: {
            totalOperations: 120,
            successfulOperations: 112,
            failedOperations: 6,
            pendingRetries: 2,
            conflictCount: 3,
            successRate: 93.33,
            byEntityType: {},
            byDirection: { FromERP: 90, ToERP: 30 },
          },
          recentFailures: [],
          pendingRetries: [],
        },
      });
    }

    try {
      const query = new URLSearchParams({ hours: String(hours || 24) });
      return await api.get(`${baseAdmin}/sync/diagnostics?${query.toString()}`);
    } catch (error) {
      console.error("Sync diagnostics alınamadı:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Belirli bir sync log kaydını manuel retry kuyruğuna alır.
   * Backend: POST /api/admin/micro/sync/logs/{logId}/retry
   */
  retrySyncLog: async (logId) => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        message: `Log #${logId} retry kuyruğuna alındı (Mock)`,
      });
    }

    try {
      return await api.post(`${baseAdmin}/sync/logs/${logId}/retry`);
    } catch (error) {
      console.error("Retry sync log başarısız:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Conflict kaydını manuel çözer.
   * strategy: erpWins | localWins
   * Backend: POST /api/admin/micro/sync/conflicts/{logId}/resolve
   */
  resolveSyncConflict: async (logId, strategy = "erpWins") => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        message: `Conflict #${logId} çözüldü (${strategy}) (Mock)`,
      });
    }

    try {
      const query = new URLSearchParams({ strategy });
      return await api.post(
        `${baseAdmin}/sync/conflicts/${logId}/resolve?${query.toString()}`,
      );
    } catch (error) {
      console.error("Resolve conflict başarısız:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * CSV export için TÜM cache ürünlerini getirir (limit yok).
   * Backend: GET /api/admin/micro/cache/export-all
   * @param {Object} params - Filtre parametreleri
   * @param {string} [params.grupKod] - Grup kodu filtresi
   * @param {boolean} [params.sadeceStoklu] - Sadece stoklu ürünler
   * @param {boolean} [params.sadeceAktif] - Sadece aktif ürünler (null=hepsi)
   */
  exportAllCachedProducts: async (params = {}) => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        data: mockMicroProducts.map((p, index) => ({
          stokKod: p.sku,
          stokAd: p.name,
          barkod: p.barcode,
          grupKod: p.category,
          birim: "ADET",
          satisFiyati: p.price,
          kdvOrani: 10,
          depoMiktari: mockMicroStocks[index]?.quantity ?? 0,
        })),
        totalCount: mockMicroProducts.length,
      });
    }

    try {
      const queryParams = new URLSearchParams();
      if (params.grupKod) queryParams.append("grupKod", params.grupKod);
      if (params.sadeceStoklu !== undefined && params.sadeceStoklu !== null) {
        queryParams.append("sadeceStoklu", String(params.sadeceStoklu));
      }
      if (params.sadeceAktif !== undefined && params.sadeceAktif !== null) {
        queryParams.append("sadeceAktif", String(params.sadeceAktif));
      }

      const url = `${baseAdmin}/cache/export-all${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;
      return await api.get(url);
    } catch (error) {
      console.error("Tüm cache ürünleri export edilemedi:", error);
      return {
        success: false,
        data: [],
        totalCount: 0,
        message: error.message,
      };
    }
  },

  /**
   * Belirli bir stok kodunun detaylarını getirir
   * Backend: GET /api/admin/micro/stok/{stokKod}
   * @param {string} stokKod - Stok kodu
   */
  getStokDetay: async (stokKod) => {
    if (shouldUseMockData()) {
      const product = mockMicroProducts.find((p) => p.sku === stokKod);
      if (!product) {
        return { success: false, message: "Stok bulunamadı" };
      }
      const stock = mockMicroStocks.find((s) => s.productId === product.id);
      return {
        success: true,
        data: {
          stokKod: product.sku,
          stokAd: product.name,
          barkod: product.barcode,
          fiyat: product.price,
          stokMiktar: stock?.quantity || 0,
          birim: "ADET",
          grupKod: product.category,
        },
      };
    }
    try {
      const response = await api.get(
        `${baseAdmin}/stok/${encodeURIComponent(stokKod)}`,
      );
      return response;
    } catch (error) {
      console.error("Stok detayı getirilemedi:", error);
      return { success: false, message: error.message };
    }
  },

  // ==================== SYNC SAĞLIK & MONİTÖRİNG (Phase 5) ====================

  /**
   * Sync sağlık özeti — tüm kanalların durumunu döner.
   * Backend: GET /api/admin/micro/sync/health
   */
  getSyncHealth: async () => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        overallStatus: "Healthy",
        totalSyncChannels: 6,
        healthyChannels: 5,
        degradedChannels: 1,
        unhealthyChannels: 0,
        activeAlertCount: 1,
        recentErrorCount: 2,
        channels: [
          {
            syncType: "HotPoll",
            direction: "FromERP",
            status: "Healthy",
            lastSyncTime: new Date().toISOString(),
            lastSyncSuccess: true,
            consecutiveFailures: 0,
          },
          {
            syncType: "StockSync",
            direction: "FromERP",
            status: "Degraded",
            lastSyncTime: new Date(Date.now() - 3600000).toISOString(),
            lastSyncSuccess: true,
            consecutiveFailures: 1,
          },
        ],
      });
    }
    try {
      return await api.get(`${baseAdmin}/sync/health`);
    } catch (error) {
      console.error("Sync sağlık bilgisi alınamadı:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Sync metrikleri — saatlik breakdown ile trend verisi.
   * Backend: GET /api/admin/micro/sync/metrics?hours=24
   * @param {number} hours - Kaç saatlik veri (varsayılan: 24)
   */
  getSyncMetrics: async (hours = 24) => {
    if (shouldUseMockData()) {
      const now = Date.now();
      return Promise.resolve({
        success: true,
        periodHours: hours,
        totalOperations: 480,
        successfulOperations: 460,
        failedOperations: 20,
        successRate: 95.83,
        avgDurationMs: 1200,
        hourlyBreakdown: Array.from(
          { length: Math.min(hours, 24) },
          (_, i) => ({
            hour: new Date(now - (hours - 1 - i) * 3600000).toISOString(),
            totalOps: 20,
            successOps: 19,
            failedOps: 1,
            avgDurationMs: 1100 + Math.random() * 300,
          }),
        ),
      });
    }
    try {
      const query = new URLSearchParams({ hours: String(hours) });
      return await api.get(`${baseAdmin}/sync/metrics?${query.toString()}`);
    } catch (error) {
      console.error("Sync metrikleri alınamadı:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Aktif sync uyarıları.
   * Backend: GET /api/admin/micro/sync/alerts
   */
  getSyncAlerts: async () => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        alerts: [
          {
            alertType: "ConsecutiveFailure",
            severity: "Warning",
            channel: "StockSync",
            message: "3 ardışık başarısız deneme",
            detectedAt: new Date().toISOString(),
          },
        ],
      });
    }
    try {
      return await api.get(`${baseAdmin}/sync/alerts`);
    } catch (error) {
      console.error("Sync uyarıları alınamadı:", error);
      return { success: false, message: error.message };
    }
  },

  // ==================== ÜRÜN BİLGİ SYNC (Phase 4) ====================

  /**
   * Cache'ten Product tablosuna tam bilgi senkronizasyonu (ad, slug, birim, kategori, durum).
   * Backend: POST /api/admin/micro/sync/product-info
   */
  syncProductInfo: async () => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        message: "Ürün bilgileri senkronize edildi (Mock)",
        totalProcessed: 150,
        namesUpdated: 5,
        categoriesUpdated: 3,
        weightInfoUpdated: 2,
        statusUpdated: 1,
      });
    }
    try {
      return await api.post(`${baseAdmin}/sync/product-info`);
    } catch (error) {
      console.error("Ürün bilgi sync başarısız:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Kategori mapping senkronizasyonu — MikroCategoryMapping tablosundan eşleşme.
   * Backend: POST /api/admin/micro/sync/categories
   */
  syncCategories: async () => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        message: "Kategori eşleştirmesi yapıldı (Mock)",
        totalProcessed: 200,
        categoriesUpdated: 15,
      });
    }
    try {
      return await api.post(`${baseAdmin}/sync/categories`);
    } catch (error) {
      console.error("Kategori sync başarısız:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Eşleştirilmemiş GrupKod'ları getirir.
   * Backend: GET /api/admin/micro/sync/unmapped-groups
   */
  getUnmappedGroups: async () => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        unmappedGroups: [
          { grupKod: "MEYVE", productCount: 12 },
          { grupKod: "ET", productCount: 8 },
        ],
      });
    }
    try {
      return await api.get(`${baseAdmin}/sync/unmapped-groups`);
    } catch (error) {
      console.error("Eşleştirilmemiş gruplar alınamadı:", error);
      return { success: false, message: error.message };
    }
  },

  /**
   * Resim durumu raporu — kaç ürünün görseli var/yok.
   * Backend: GET /api/admin/micro/sync/image-status
   */
  getImageStatus: async () => {
    if (shouldUseMockData()) {
      return Promise.resolve({
        success: true,
        totalProducts: 500,
        productsWithImages: 420,
        productsWithoutImages: 80,
        coveragePercent: 84.0,
        missingImageProducts: [
          { productId: 101, name: "Elma", sku: "ELMA001" },
          { productId: 102, name: "Portakal", sku: "PORT001" },
        ],
      });
    }
    try {
      return await api.get(`${baseAdmin}/sync/image-status`);
    } catch (error) {
      console.error("Resim durumu raporu alınamadı:", error);
      return { success: false, message: error.message };
    }
  },
};
