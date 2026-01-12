import { debugLog, shouldUseMockData } from "../config/apiConfig";
import api from "./api";

const baseAdmin = "/admin/micro"; // admin-only actions

let mockMicroProducts = [
  { id: 1, name: "Dana Kuşbaşı", price: 89.9, category: "Et ve Et Ürünleri" },
  { id: 2, name: "Pınar Süt 1L", price: 12.5, category: "Süt Ürünleri" },
  { id: 3, name: "Domates Kg", price: 8.75, category: "Meyve ve Sebze" },
  { id: 4, name: "Coca Cola 330ml", price: 5.5, category: "İçecekler" },
];

let mockMicroStocks = [
  { productId: 1, quantity: 24 },
  { productId: 2, quantity: 50 },
  { productId: 3, quantity: 12 },
  { productId: 4, quantity: 80 },
];

let mockProductSynced = false;

const clone = (data) => JSON.parse(JSON.stringify(data));
const buildMockResponse = (message) => ({
  success: true,
  message,
  timestamp: new Date().toISOString(),
});

export const MicroService = {
  // Admin actions
  syncProducts: () => {
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
        };
        mockMicroProducts.push(newProduct);
        mockMicroStocks.push({ productId: nextId, quantity: 30 });
        mockProductSynced = true;
      }
      return Promise.resolve(buildMockResponse("ERP ile ürünler senkronize edildi (mock)"));
    }
    return api.post(`${baseAdmin}/sync-products`);
  },
  exportOrders: (orders) => {
    if (shouldUseMockData()) {
      debugLog("Micro exportOrders - Mock", orders);
      return Promise.resolve(buildMockResponse("Seçilen siparişler ERP'ye aktarıldı (mock)"));
    }
    return api.post(`${baseAdmin}/export-orders`, orders);
  },
  syncStocksFromERP: () => {
    if (shouldUseMockData()) {
      mockMicroStocks = mockMicroStocks.map((stock, index) => {
        const delta = index % 2 === 0 ? 5 : -3;
        return {
          ...stock,
          quantity: Math.max(0, stock.quantity + delta),
        };
      });
      return Promise.resolve(buildMockResponse("Stoklar ERP'den çekildi (mock)"));
    }
    return api.post(`${baseAdmin}/sync-stocks-from-erp`);
  },
  syncPricesFromERP: () => {
    if (shouldUseMockData()) {
      mockMicroProducts = mockMicroProducts.map((product, index) => {
        const factor = index % 2 === 0 ? 1.02 : 0.98;
        return {
          ...product,
          price: Number((product.price * factor).toFixed(2)),
        };
      });
      return Promise.resolve(buildMockResponse("Fiyatlar ERP'den çekildi (mock)"));
    }
    return api.post(`${baseAdmin}/sync-prices-from-erp`);
  },

  // Read endpoints (admin scope da sağlayabilir, burada admin'i tercih ediyoruz)
  getProducts: () => {
    if (shouldUseMockData()) {
      return Promise.resolve(clone(mockMicroProducts));
    }
    return api.get(`${baseAdmin}/products`);
  },
  getStocks: () => {
    if (shouldUseMockData()) {
      return Promise.resolve(clone(mockMicroStocks));
    }
    return api.get(`${baseAdmin}/stocks`);
  },
};
