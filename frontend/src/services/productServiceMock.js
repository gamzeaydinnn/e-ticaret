// src/services/productServiceMock.js
// Ürün servisi - JSON SERVER'a bağlı (GEÇİCİ - Mikro API gelene kadar)
// Endpoint: http://localhost:3005/products

import axios from "axios";

// JSON Server için ayrı axios instance
const apiProducts = axios.create({
  baseURL: "http://localhost:3005",
  headers: {
    "Content-Type": "application/json",
  },
});

// Response interceptor
apiProducts.interceptors.response.use(
  (res) => res.data,
  (error) => {
    console.error("[ProductService Mock] API Error:", error.message);
    throw error;
  }
);

// Event listener sistemi - bileşenler arası senkronizasyon için
const listeners = [];

const notify = () => {
  listeners.forEach((callback) => {
    try {
      callback();
    } catch (e) {
      console.error("[ProductServiceMock] Listener error:", e);
    }
  });
};

const productServiceMock = {
  // Tüm ürünleri getir
  async getAll(params = {}) {
    return await apiProducts.get("/products", { params });
  },

  // ID'ye göre ürün getir
  async getById(id) {
    return await apiProducts.get(`/products/${id}`);
  },

  // Yeni ürün oluştur
  async create(product) {
    const payload = {
      ...product,
      isActive: product.isActive !== false,
    };
    delete payload.id; // JSON Server otomatik ID verecek
    
    const result = await apiProducts.post("/products", payload);
    notify();
    return result;
  },

  // Ürün güncelle
  async update(id, product) {
    const result = await apiProducts.put(`/products/${id}`, product);
    notify();
    return result;
  },

  // Ürün sil
  async delete(id) {
    await apiProducts.delete(`/products/${id}`);
    notify();
    return { success: true };
  },

  // Aktif/Pasif durumunu değiştir
  async toggleActive(product) {
    const result = await apiProducts.patch(`/products/${product.id}`, {
      isActive: !product.isActive,
    });
    notify();
    return result;
  },

  // Kategoriye göre ürünler
  async getByCategory(categoryId) {
    return await apiProducts.get(`/products?categoryId=${categoryId}&isActive=true`);
  },

  // Aktif ürünler
  async getActive() {
    return await apiProducts.get("/products?isActive=true");
  },

  // Arama
  async search(query) {
    return await apiProducts.get(`/products?q=${encodeURIComponent(query)}&isActive=true`);
  },

  // Bileşen subscription sistemi
  subscribe(callback) {
    listeners.push(callback);
    return () => {
      const index = listeners.indexOf(callback);
      if (index > -1) {
        listeners.splice(index, 1);
      }
    };
  },
};

export default productServiceMock;

/*
  ======================================
  📌 MİKRO API GELDİĞİNDE YAPILACAKLAR:
  ======================================
  
  1. Bu dosyayı productServiceReal.js olarak kopyala
  
  2. apiProducts baseURL'ini değiştir:
     baseURL: "http://localhost:3005"
     ↓
     baseURL: "https://mikro-api.example.com/api/v1"
  
  3. Endpoint'leri mikro API'ye göre güncelle:
     "/products" → "/items" veya "/inventory"
  
  4. Import'u değiştir:
     import productServiceMock from "./productServiceMock"
     ↓
     import productServiceReal from "./productServiceReal"
  
  Sadece bu kadar! Hiçbir bileşen değişikliği gerekmez.
  ======================================
*/
