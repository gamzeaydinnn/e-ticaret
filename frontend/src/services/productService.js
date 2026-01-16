// src/services/productService.js
// Ürün servisi - Backend API kullanıyor
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
    isActive: p.isActive !== false,
  };
};

// Subscription callbacks
let subscribers = [];

export const ProductService = {
  // Backend API'den ürünleri çek
  list: async (query = "") => {
    try {
      const response = await api.get("/api/products");
      const items = Array.isArray(response) ? response : response?.data || [];
      return items.filter((p) => p.isActive !== false).map(mapProduct);
    } catch (err) {
      console.error("Ürünler yüklenemedi:", err);
      return [];
    }
  },

  get: async (id) => {
    try {
      const response = await api.get(`/api/products/${id}`);
      const product = response?.data || response;
      return product ? mapProduct(product) : null;
    } catch (err) {
      console.error("Ürün bulunamadı:", err);
      return null;
    }
  },

  // Kategoriye göre ürünleri çek
  getByCategory: async (categoryId) => {
    try {
      const response = await api.get(`/api/products/category/${categoryId}`);
      const items = Array.isArray(response) ? response : response?.data || [];
      return items.filter((p) => p.isActive !== false).map(mapProduct);
    } catch (err) {
      console.error("Kategori ürünleri yüklenemedi:", err);
      return [];
    }
  },

  // Subscribe to product changes
  subscribe: (callback) => {
    subscribers.push(callback);
    return () => {
      subscribers = subscribers.filter((cb) => cb !== callback);
    };
  },

  // Admin endpoints
  getAll: async () => {
    try {
      const response = await api.get("/api/products/admin/all?size=500");
      const items = Array.isArray(response) ? response : response?.data || [];
      return items.map(mapProduct);
    } catch (err) {
      console.error("Admin ürünler yüklenemedi:", err);
      // Fallback: normal endpoint dene
      try {
        const response = await api.get("/api/products?size=500");
        const items = Array.isArray(response) ? response : response?.data || [];
        return items.map(mapProduct);
      } catch (err2) {
        console.error("Ürünler yüklenemedi:", err2);
        return [];
      }
    }
  },

  createAdmin: async (formData) => {
    const response = await api.post("/api/products", formData);
    return response?.data || response;
  },

  updateAdmin: async (id, formData) => {
    const response = await api.put(`/api/products/${id}`, formData);
    return response?.data || response;
  },

  deleteAdmin: async (id) => {
    await api.delete(`/api/products/${id}`);
  },

  updateStockAdmin: async (id, stock) => {
    const response = await api.patch(`/api/products/${id}/stock`, { stock });
    return response?.data || response;
  },

  importExcel: async (file) => {
    const formData = new FormData();
    formData.append("file", file);
    const response = await api.post("/api/products/import/excel", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return response?.data || response;
  },

  downloadTemplate: async () => {
    const response = await api.get("/api/products/import/template", {
      responseType: "blob",
    });
    return response;
  },

  /**
   * Ürün resmi yükler (bilgisayardan dosya seçerek)
   * @param {File} imageFile - Yüklenecek resim dosyası
   * @returns {Promise<{success: boolean, imageUrl: string, message: string}>}
   *          Yükleme sonucu ve dosya URL'i
   */
  uploadImage: async (imageFile) => {
    const formData = new FormData();
    formData.append("image", imageFile);
    const response = await api.post("/api/products/upload/image", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return response?.data || response;
  },
};
