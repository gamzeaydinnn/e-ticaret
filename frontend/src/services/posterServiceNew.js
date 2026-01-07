// src/services/posterServiceNew.js
// JSON Server tabanlı Poster CRUD servisi
import apiClient from "./apiClient";

const RESOURCE = "/posters";

// Event listener sistemi - bileşenler arası senkronizasyon için
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
  // Tüm posterleri getir
  async getAll() {
    const res = await apiClient.get(RESOURCE);
    return res.data;
  },

  // ID'ye göre poster getir
  async getById(id) {
    const res = await apiClient.get(`${RESOURCE}/${id}`);
    return res.data;
  },

  // Yeni poster oluştur
  async create(poster) {
    const now = new Date().toISOString();
    const payload = {
      ...poster,
      createdAt: now,
      updatedAt: now,
      isActive: poster.isActive !== false,
    };
    // id'yi kaldır, JSON Server otomatik verecek
    delete payload.id;
    
    const res = await apiClient.post(RESOURCE, payload);
    notify(); // Tüm listener'ları bilgilendir
    return res.data;
  },

  // Poster güncelle
  async update(id, poster) {
    const payload = {
      ...poster,
      updatedAt: new Date().toISOString(),
    };
    const res = await apiClient.put(`${RESOURCE}/${id}`, payload);
    notify();
    return res.data;
  },

  // Poster sil
  async delete(id) {
    await apiClient.delete(`${RESOURCE}/${id}`);
    notify();
    return { success: true };
  },

  // Aktif/Pasif durumunu değiştir
  async toggleActive(poster) {
    const res = await apiClient.patch(`${RESOURCE}/${poster.id}`, {
      isActive: !poster.isActive,
      updatedAt: new Date().toISOString(),
    });
    notify();
    return res.data;
  },

  // Sadece slider posterleri getir (aktif olanlar)
  async getSliderPosters() {
    const res = await apiClient.get(`${RESOURCE}?type=slider&isActive=true&_sort=displayOrder,id`);
    return res.data;
  },

  // Sadece promo posterleri getir (aktif olanlar)
  async getPromoPosters() {
    const res = await apiClient.get(`${RESOURCE}?type=promo&isActive=true&_sort=displayOrder,id`);
    return res.data;
  },

  // Tip'e göre posterler (admin için - aktif/pasif hepsi)
  async getByType(type) {
    const res = await apiClient.get(`${RESOURCE}?type=${type}&_sort=displayOrder,id`);
    return res.data;
  },

  // Bileşen subscription sistemi
  subscribe(callback) {
    listeners.push(callback);
    // Unsubscribe fonksiyonu döndür
    return () => {
      const index = listeners.indexOf(callback);
      if (index > -1) {
        listeners.splice(index, 1);
      }
    };
  },
};

export default posterService;
