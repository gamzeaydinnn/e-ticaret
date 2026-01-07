// src/services/productServiceNew.js
// JSON Server tabanlı Product CRUD servisi
import apiClient from "./apiClient";

const RESOURCE = "/products";

// Event listener sistemi - bileşenler arası senkronizasyon için
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

const productServiceNew = {
  // Tüm ürünleri getir
  async getAll(params = {}) {
    const res = await apiClient.get(RESOURCE, { params });
    return res.data;
  },

  // ID'ye göre ürün getir
  async getById(id) {
    const res = await apiClient.get(`${RESOURCE}/${id}`);
    return res.data;
  },

  // Yeni ürün oluştur
  async create(product) {
    const payload = {
      ...product,
      isActive: product.isActive !== false,
    };
    // id'yi kaldır, JSON Server otomatik verecek
    delete payload.id;
    
    const res = await apiClient.post(RESOURCE, payload);
    notify();
    return res.data;
  },

  // Ürün güncelle
  async update(id, product) {
    const res = await apiClient.put(`${RESOURCE}/${id}`, product);
    notify();
    return res.data;
  },

  // Ürün sil
  async delete(id) {
    await apiClient.delete(`${RESOURCE}/${id}`);
    notify();
    return { success: true };
  },

  // Aktif/Pasif durumunu değiştir
  async toggleActive(product) {
    const res = await apiClient.patch(`${RESOURCE}/${product.id}`, {
      isActive: !product.isActive,
    });
    notify();
    return res.data;
  },

  // Kategoriye göre ürünler
  async getByCategory(categoryId) {
    const res = await apiClient.get(`${RESOURCE}?categoryId=${categoryId}&isActive=true`);
    return res.data;
  },

  // Aktif ürünler
  async getActive() {
    const res = await apiClient.get(`${RESOURCE}?isActive=true`);
    return res.data;
  },

  // Arama
  async search(query) {
    // JSON Server'da full-text search için q parametresi kullanılır
    const res = await apiClient.get(`${RESOURCE}?q=${encodeURIComponent(query)}&isActive=true`);
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

export default productServiceNew;
