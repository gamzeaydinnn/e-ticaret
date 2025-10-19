import api from "./api";

const basePublic = "/api/micro"; // readonly endpoints
const baseAdmin = "/api/admin/micro"; // admin-only actions

export const MicroService = {
  // Admin actions
  syncProducts: () => api.post(`${baseAdmin}/sync-products`),
  exportOrders: (orders) => api.post(`${baseAdmin}/export-orders`, orders),
  syncStocksFromERP: () => api.post(`${baseAdmin}/sync-stocks-from-erp`),
  syncPricesFromERP: () => api.post(`${baseAdmin}/sync-prices-from-erp`),

  // Read endpoints (admin scope da sağlayabilir, burada admin'i tercih ediyoruz)
  getProducts: () => api.get(`${baseAdmin}/products`),
  getStocks: () => api.get(`${baseAdmin}/stocks`),
};
