// src/services/posterService.js
// Poster/Banner servisi - GERÇEK BACKEND API'ye bağlı
// Public: /api/banners
// Admin: /api/admin/banners

import apiBackend from "./apiBackend";

// Event listener sistemi
const listeners = [];

const notify = () => {
  listeners.forEach((callback) => {
    try {
      callback();
    } catch (e) {
      console.error("[PosterService] Listener error:", e);
    }
  });
};

const posterService = {
  // ============ PUBLIC ENDPOINTS ============

  // Aktif slider posterlerini getir
  async getSliderPosters() {
    try {
      const data = await apiBackend.get("/banners");
      console.log("[PosterService] All banners from API:", data);
      // Backend'den gelen posterler tıplarına göre filtrele (case-insensitive)
      const sliders = Array.isArray(data) ? data.filter(p => {
        const type = (p.type || '').toLowerCase();
        return type === 'slider';
      }) : [];
      console.log("[PosterService] Filtered sliders:", sliders);
      return sliders.length > 0 ? sliders : [];
    } catch (error) {
      console.error("Slider posters fetch error:", error);
      return [];
    }
  },

  // Aktif promo posterlerini getir
  async getPromoPosters() {
    try {
      const data = await apiBackend.get("/banners");
      // Backend'den gelen posterler tıplarına göre filtrele (case-insensitive)
      const promos = Array.isArray(data) ? data.filter(p => {
        const type = (p.type || '').toLowerCase();
        return type === 'promo';
      }) : [];
      console.log("[PosterService] Filtered promos:", promos);
      return promos.length > 0 ? promos : [];
    } catch (error) {
      console.error("Promo posters fetch error:", error);
      return [];
    }
  },

  // ============ ADMIN ENDPOINTS ============

  // Admin: Tüm posterleri getir
  async getAll() {
    try {
      const data = await apiBackend.get("/admin/banners");
      return Array.isArray(data) ? data : [];
    } catch (error) {
      console.error("All posters fetch error:", error);
      return [];
    }
  },

  // Admin: ID'ye göre poster getir
  async getById(id) {
    return await apiBackend.get(`/admin/banners/${id}`);
  },

  // Admin: Tip'e göre posterler
  async getByType(type) {
    try {
      const data = await apiBackend.get(`/admin/banners?type=${type}`);
      return Array.isArray(data) ? data : [];
    } catch (error) {
      console.error(`Posters by type (${type}) fetch error:`, error);
      return [];
    }
  },

  // Admin: Yeni poster oluştur
  async create(poster) {
    const payload = {
      title: poster.title,
      imageUrl: poster.imageUrl,
      linkUrl: poster.linkUrl || "",
      type: poster.type || "slider",
      displayOrder: parseInt(poster.displayOrder) || 0,
      isActive: poster.isActive !== false,
    };

    const result = await apiBackend.post("/admin/banners", payload);
    notify();
    return result;
  },

  // Admin: Poster güncelle
  async update(id, poster) {
    const payload = {
      id: parseInt(id),
      title: poster.title,
      imageUrl: poster.imageUrl,
      linkUrl: poster.linkUrl || "",
      type: poster.type || "slider",
      displayOrder: parseInt(poster.displayOrder) || 0,
      isActive: poster.isActive !== false,
    };

    const result = await apiBackend.put(`/admin/banners/${id}`, payload);
    notify();
    return result;
  },

  // Admin: Poster sil
  async delete(id) {
    await apiBackend.delete(`/admin/banners/${id}`);
    notify();
    return { success: true };
  },

  // Admin: Aktif/Pasif durumunu değiştir
  async toggleActive(poster) {
    const payload = {
      id: poster.id,
      isActive: !poster.isActive,
    };
    const result = await apiBackend.patch(
      `/admin/banners/${poster.id}/toggle`,
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

export default posterService;
