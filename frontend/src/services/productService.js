// src/services/productService.js
import api from "./api";

export const ProductService = {
  // Genel kullanıcı tarafı
  list: (query = "") => api.get(`/api/Products${query}`).then(r => r.data),
  get: (id) => api.get(`/api/Products/${id}`).then(r => r.data),

  // Admin endpoints
  createAdmin: (formData) => api.post("/api/Admin/products", formData).then(r => r.data),
  updateAdmin: (id, formData) => api.put(`/api/Admin/products/${id}`, formData).then(r => r.data),
  deleteAdmin: (id) => api.delete(`/api/Admin/products/${id}`).then(r => r.data),
  updateStockAdmin: (id, stock) => api.patch(`/api/Admin/products/${id}/stock`, { stock }).then(r => r.data),
};
