// src/services/productService.js
import api from "./api";

const mapProduct = (p = {}) => ({
  id: p.id,
  name: p.name || p.title || "",
  category: p.category_name || p.category || "",
  price: p.price ?? 0,
  discountPrice: p.discount_price ?? null,
  imageUrl: p.image_url || p.image || p.imageUrl || "",
  stock: p.stock ?? p.stockQuantity ?? 0,
  description: p.description || "",
});

export const ProductService = {
  // Public endpoints (mapped shape)
  list: async () => {
    const data = await api.get(`/api/products`);
    const items = Array.isArray(data) ? data : data?.data || [];
    return items.map(mapProduct);
  },
  get: async (id) => {
    const data = await api.get(`/api/products/${id}`);
    return mapProduct(data);
  },

  // Admin endpoints (kept for compatibility)
  createAdmin: (formData) => api.post(`/api/Admin/products`, formData),
  updateAdmin: (id, formData) => api.put(`/api/Admin/products/${id}`, formData),
  deleteAdmin: (id) => api.delete(`/api/Admin/products/${id}`),
  updateStockAdmin: (id, stock) => api.patch(`/api/Admin/products/${id}/stock`, { stock }),
};
