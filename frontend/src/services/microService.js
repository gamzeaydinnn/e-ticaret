import { debugLog, shouldUseMockData } from "../config/apiConfig";
import api from "./api";

// Backend API endpoint'leri - AdminMicroController'a karşılık gelir
const baseAdmin = "/api/admin/micro";

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
      const perPage = 100;
      const maxPages = 10;
      let page = 1;
      let all = [];

      while (page <= maxPages) {
        const response = await api.get(
          `${baseAdmin}/products?source=erp&page=${page}&perPage=${perPage}`,
        );

        const batchRaw = Array.isArray(response)
          ? response
          : Array.isArray(response?.data)
            ? response.data
            : [];

        const batch = batchRaw.map(normalizeProductItem);

        if (!batch.length) break;
        all = all.concat(batch);

        const totalCount = Number(
          response?.totalCount || response?.toplamKayit || 0,
        );
        if (totalCount > 0 && all.length >= totalCount) break;
        if (batch.length < perPage) break;

        page += 1;
      }

      return all;
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
      const perPage = 100;
      const maxPages = 10;
      let page = 1;
      let all = [];

      while (page <= maxPages) {
        const response = await api.get(
          `${baseAdmin}/stocks?source=erp&page=${page}&perPage=${perPage}`,
        );

        const batchRaw = Array.isArray(response)
          ? response
          : Array.isArray(response?.data)
            ? response.data
            : [];

        const batch = batchRaw.map(normalizeStockItem);

        if (!batch.length) break;
        all = all.concat(batch);

        const totalCount = Number(
          response?.totalCount || response?.toplamKayit || 0,
        );
        if (totalCount > 0 && all.length >= totalCount) break;
        if (batch.length < perPage) break;

        page += 1;
      }

      return all;
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
          totalPages: Math.max(1, Math.ceil(mockMicroProducts.length / pageSize)),
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
};
