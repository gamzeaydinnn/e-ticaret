// src/services/productService.js
import api from "./api";
import { shouldUseMockData } from "../config/apiConfig";
import mockDataStore from "./mockDataStore";

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
    isActive: p.isActive !== false,
  };
};

export const ProductService = {
  // Public endpoints (mapped shape)
  list: async (query = "") => {
    // Mock data kullanılıyorsa mockDataStore'dan al
    if (shouldUseMockData()) {
      const items = mockDataStore.getProducts();
      return items.map(mapProduct);
    }
    const url = `/products${query}`;
    const data = await api.get(url);
    const items = Array.isArray(data) ? data : data?.data || [];
    return items.map(mapProduct);
  },
  get: async (id) => {
    if (shouldUseMockData()) {
      const product = mockDataStore.getProductById(id);
      return product ? mapProduct(product) : null;
    }
    const data = await api.get(`/products/${id}`);
    return mapProduct(data);
  },
  
  // Subscribe to product changes (for real-time updates)
  subscribe: (callback) => mockDataStore.subscribe("products", callback),

  // Admin endpoints (kept for compatibility)
  createAdmin: (formData) => api.post(`/Admin/products`, formData),
  updateAdmin: (id, formData) => api.put(`/Admin/products/${id}`, formData),
  deleteAdmin: (id) => api.delete(`/Admin/products/${id}`),
  updateStockAdmin: (id, stock) =>
    api.patch(`/Admin/products/${id}/stock`, { stock }),
};
