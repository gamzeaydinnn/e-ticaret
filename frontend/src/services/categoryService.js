// src/services/categoryService.js
// Kategori servisi - GERÇEK BACKEND API'ye bağlı
// Public: /api/categories
// Admin: /api/admin/categories

import apiBackend from "./apiBackend";

// Event listener sistemi
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

const categoryService = {
  // ============ PUBLIC ENDPOINTS ============
  
  // Tüm kategorileri getir (public - navigasyon için)
  async getAll() {
    try {
      const data = await apiBackend.get("/api/categories");
      return Array.isArray(data) ? data : [];
    } catch (error) {
      console.error("Categories fetch error:", error);
      return [];
    }
  },

  // Slug'a göre kategori getir
  async getBySlug(slug) {
    return await apiBackend.get(`/api/categories/${encodeURIComponent(slug)}`);
  },

  // Aktif kategoriler (public görünüm için)
  async getActive() {
    const all = await this.getAll();
    return all.filter((c) => c.isActive !== false);
  },

  // ============ ADMIN ENDPOINTS ============

  // Admin: Tüm kategorileri getir (aktif/pasif dahil)
  async getAllAdmin() {
    try {
      const data = await apiBackend.get("/api/admin/categories");
      return Array.isArray(data) ? data : [];
    } catch (error) {
      console.error("Admin categories fetch error:", error);
      return [];
    }
  },

  // Admin: ID'ye göre kategori getir
  async getById(id) {
    return await apiBackend.get(`/api/admin/categories/${id}`);
  },

  // Admin: Yeni kategori oluştur
  async create(category) {
    const payload = {
      name: category.name,
      description: category.description || "",
      slug: category.slug || createSlug(category.name),
      imageUrl: category.imageUrl || "",
      icon: category.icon || "",
      parentId: category.parentId || null,
      sortOrder: parseInt(category.sortOrder) || parseInt(category.displayOrder) || 0,
      isActive: category.isActive !== false,
    };
    
    const result = await apiBackend.post("/api/admin/categories", payload);
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
      icon: category.icon || "",
      parentId: category.parentId || null,
      sortOrder: parseInt(category.sortOrder) || parseInt(category.displayOrder) || 0,
      isActive: category.isActive !== false,
    };
    
    const result = await apiBackend.put(`/api/admin/categories/${id}`, payload);
    notify();
    return result;
  },

  // Admin: Kategori sil
  async delete(id) {
    await apiBackend.delete(`/api/admin/categories/${id}`);
    notify();
    return { success: true };
  },

  // Admin: Aktif/Pasif durumunu değiştir
  async toggleActive(category) {
    const payload = {
      id: category.id,
      isActive: !category.isActive,
    };
    const result = await apiBackend.patch(
      `/api/admin/categories/${category.id}/toggle`,
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

export default categoryService;
