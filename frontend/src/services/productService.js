// src/services/productService.js
import api from "./api";

export const ProductService = {
  // Genel kullanıcı tarafı
  list: (query = "") => api.get(`/api/Products${query}`),
  get: (id) => api.get(`/api/Products/${id}`),

  // Admin endpoints
  createAdmin: (formData) => api.post("/api/Admin/products", formData),
  updateAdmin: (id, formData) => api.put(`/api/Admin/products/${id}`, formData),
  deleteAdmin: (id) => api.delete(`/api/Admin/products/${id}`),
  updateStockAdmin: (id, stock) =>
    api.patch(`/api/Admin/products/${id}/stock`, { stock }),
};
