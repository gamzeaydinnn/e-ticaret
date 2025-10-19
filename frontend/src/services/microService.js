import api from "./api";

const base = "/api/micro";

export const MicroService = {
  syncProducts: () => api.post(`${base}/sync-products`),
  getProducts: () => api.get(`${base}/products`),
  getStocks: () => api.get(`${base}/stocks`),
  exportOrders: (orders) => api.post(`${base}/export-orders`, orders),
};
