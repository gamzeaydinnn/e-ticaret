// src/services/categoryServiceNew.js
// JSON Server tabanlı Category CRUD servisi
import apiClient from "./apiClient";

const RESOURCE = "/categories";

// Event listener sistemi - bileşenler arası senkronizasyon için
const listeners = [];

const notify = () => {
  listeners.forEach((callback) => {
    try {
      callback();
    } catch (e) {
      console.error("[CategoryService] Listener error:", e);
    }
  });
};

// Slug oluşturma yardımcı fonksiyonu
const createSlug = (name) => {
  return name
    .toLowerCase()
    .replace(/ç/g, "c")
    .replace(/ğ/g, "g")
    .replace(/ı/g, "i")
    .replace(/ö/g, "o")
    .replace(/ş/g, "s")
    .replace(/ü/g, "u")
    .replace(/[^a-z0-9\s-]/g, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .trim();
};

const categoryServiceNew = {
  // Tüm kategorileri getir
  async getAll() {
    const res = await apiClient.get(RESOURCE);
    return res.data;
  },

  // ID'ye göre kategori getir
  async getById(id) {
    const res = await apiClient.get(`${RESOURCE}/${id}`);
    return res.data;
  },

  // Slug'a göre kategori getir
  async getBySlug(slug) {
    const res = await apiClient.get(`${RESOURCE}?slug=${slug}`);
    return res.data[0] || null;
  },

  // Yeni kategori oluştur
  async create(category) {
    const payload = {
      ...category,
      slug: category.slug || createSlug(category.name),
      isActive: category.isActive !== false,
      productCount: category.productCount || 0,
    };
    // id'yi kaldır, JSON Server otomatik verecek
    delete payload.id;
    
    const res = await apiClient.post(RESOURCE, payload);
    notify();
    return res.data;
  },

  // Kategori güncelle
  async update(id, category) {
    const payload = {
      ...category,
      slug: category.slug || createSlug(category.name),
    };
    const res = await apiClient.put(`${RESOURCE}/${id}`, payload);
    notify();
    return res.data;
  },

  // Kategori sil
  async delete(id) {
    await apiClient.delete(`${RESOURCE}/${id}`);
    notify();
    return { success: true };
  },

  // Aktif/Pasif durumunu değiştir
  async toggleActive(category) {
    const res = await apiClient.patch(`${RESOURCE}/${category.id}`, {
      isActive: !category.isActive,
    });
    notify();
    return res.data;
  },

  // Aktif kategoriler
  async getActive() {
    const res = await apiClient.get(`${RESOURCE}?isActive=true`);
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

export default categoryServiceNew;
