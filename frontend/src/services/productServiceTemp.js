// src/services/productServiceMock.js
// Ürün servisi - ŞİMDİLİK JSON Server (Mikro API gelene kadar GEÇİCİ)
// Mikro API geldiğinde sadece apiProducts import'u değişecek

import apiProducts from "./apiProducts";

// Event listener sistemi
const listeners = [];

const notify = () => {
  listeners.forEach((callback) => {
    try {
      callback();
    } catch (e) {
      console.error("[ProductService] Listener error:", e);
    }
  });
};

const productServiceMock = {
  // Tüm ürünleri getir
  async getAll(params = {}) {
    const res = await apiProducts.get("/products", { params });
    return res.data;
  },

  // ID'ye göre ürün getir
  async getById(id) {
    const res = await apiProducts.get(`/products/${id}`);
    return res.data;
  },

  // Yeni ürün oluştur
  async create(product) {
    const payload = {
      ...product,
      isActive: product.isActive !== false,
    };
    delete payload.id; // JSON Server otomatik verecek

    const res = await apiProducts.post("/products", payload);
    notify();
    return res.data;
  },

  // Ürün güncelle
  async update(id, product) {
    const res = await apiProducts.put(`/products/${id}`, product);
    notify();
    return res.data;
  },

  // Ürün sil
  async delete(id) {
    await apiProducts.delete(`/products/${id}`);
    notify();
    return { success: true };
  },

  // Aktif/Pasif durumunu değiştir
  async toggleActive(product) {
    const res = await apiProducts.patch(`/products/${product.id}`, {
      isActive: !product.isActive,
    });
    notify();
    return res.data;
  },

  // Kategoriye göre ürünler
  async getByCategory(categoryId) {
    const res = await apiProducts.get(
      `/products?categoryId=${categoryId}&isActive=true`
    );
    return res.data;
  },

  // Aktif ürünler
  async getActive() {
    const res = await apiProducts.get("/products?isActive=true");
    return res.data;
  },

  // Arama
  async search(query) {
    // JSON Server'da full-text search için q parametresi kullanılır
    const res = await apiProducts.get(
      `/products?q=${encodeURIComponent(query)}&isActive=true`
    );
    return res.data;
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

// 🚀 MİKRO API GELDİĞİNDE:
// 1. apiProducts import'unu değiştir:
//    import apiProducts from "./apiProducts";  ❌
//    import apiMikro from "./apiMikro";        ✅
//
// 2. Endpoint path'leri güncelle:
//    "/products" → "/api/v1/items" (veya mikro API'nin endpoint'i)
//
// 3. Response yapısını kontrol et ve gerekirse map et
//
// BAŞKA HİÇBİR ŞEY DEĞİŞMEYECEK! ✨

export default productServiceMock;
