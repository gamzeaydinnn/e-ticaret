// src/services/categoryServiceReal.js
// Kategori servisi - GERÇEK BACKEND API'ye bağlı
// Public: /api/categories
// Admin: /api/admin/categories

import api from "./api";

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

const categoryServiceReal = {
  // ============ PUBLIC ENDPOINTS ============

  // Tüm kategorileri getir (public - navigasyon için)
  async getAll() {
    console.log("[CategoryService] Calling /categories...");
    const data = await api.get("/categories");
    console.log("[CategoryService] Received data:", data);
    return Array.isArray(data) ? data : [];
  },

  // Slug'a göre kategori getir
  async getBySlug(slug) {
    return await api.get(`/categories/${encodeURIComponent(slug)}`);
  },

  // Aktif kategoriler (public görünüm için)
  async getActive() {
    console.log("[CategoryService] getActive() called");
    const all = await this.getAll();
    console.log("[CategoryService] getActive() all categories:", all);
    // isActive field'ı olmayan kategorileri de dahil et (varsayılan aktif)
    const filtered = all.filter((c) => c.isActive !== false);
    console.log("[CategoryService] getActive() filtered:", filtered);
    return filtered;
  },

  // ============ ADMIN ENDPOINTS ============

  // Admin: Tüm kategorileri getir (aktif/pasif dahil)
  async getAllAdmin() {
    const data = await api.get("/admin/categories");
    return Array.isArray(data) ? data : [];
  },

  // Admin: ID'ye göre kategori getir
  async getById(id) {
    return await api.get(`/admin/categories/${id}`);
  },

  // Admin: Yeni kategori oluştur
  async create(category) {
    const payload = {
      name: category.name,
      description: category.description || "",
      slug: category.slug || createSlug(category.name),
      imageUrl: category.imageUrl || "",
      parentId: category.parentId || null,
      sortOrder: parseInt(category.sortOrder) || 0,
      isActive: category.isActive !== false,
    };

    const result = await api.post("/admin/categories", payload);
    notify();
    return result;
  },

  // Admin: Kategori güncelle
  async update(id, category) {
    const payload = {
      id: parseInt(id),
      name: category.name,
      description: category.description || "",
      slug: category.slug || createSlug(category.name),
      imageUrl: category.imageUrl || "",
      parentId: category.parentId || null,
      sortOrder: parseInt(category.sortOrder) || 0,
      isActive: category.isActive !== false,
    };

    const result = await api.put(`/admin/categories/${id}`, payload);
    notify();
    return result;
  },

  // Admin: Kategori sil (soft delete)
  async delete(id) {
    await api.delete(`/admin/categories/${id}`);
    notify();
    return { success: true };
  },

  // Admin: Aktif/Pasif durumunu değiştir
  async toggleActive(category) {
    const payload = {
      ...category,
      id: parseInt(category.id),
      isActive: !category.isActive,
    };
    const result = await api.put(
      `/admin/categories/${category.id}`,
      payload
    );
    notify();
    return result;
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

export default categoryServiceReal;
