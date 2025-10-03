import api from "./api";

const base = "/api/micro";

export const MicroService = {
  syncProducts: () => api.post(`${base}/sync-products`).then(res => res.data),
  getProducts: () => api.get(`${base}/products`).then(res => res.data),
  getStocks: () => api.get(`${base}/stocks`).then(res => res.data),
  exportOrders: (orders) => api.post(`${base}/export-orders`, orders).then(res => res.data)
};
