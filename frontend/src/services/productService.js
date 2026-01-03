// src/services/productService.js
import api from "./api";

const mapProduct = (p = {}) => {
  const basePrice = p.price ?? p.unitPrice ?? 0;
  const special = p.specialPrice ?? p.discountPrice ?? p.discount_price ?? null;

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

  const stock = p.stock ?? p.stockQuantity ?? 0;

  return {
    id: p.id,
    name: p.name || p.title || "",
    category: p.category_name || p.category || "",
    categoryId: p.categoryId ?? p.category_id ?? null,
    categoryName: p.categoryName || p.category_name || p.category || "",
    price,
    originalPrice,
    discountPrice: special,
    discountPercentage,
    imageUrl: p.image_url || p.image || p.imageUrl || "",
    stock,
    stockQuantity: stock,
    description: p.description || "",
  };
};

export const ProductService = {
  // Public endpoints (mapped shape)
  list: async (query = "") => {
    const url = `/api/products${query}`;
    const data = await api.get(url);
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
  updateStockAdmin: (id, stock) =>
    api.patch(`/api/Admin/products/${id}/stock`, { stock }),
};
