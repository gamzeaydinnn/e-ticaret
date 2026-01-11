/**
 * categoryServiceReal.js
 * 
 * Kategori Servisi - Backend API ile iletişim
 * 
 * Bu servis, veritabanındaki kategorileri frontend'e taşır.
 * Public endpoint'ler navigasyon için, Admin endpoint'ler yönetim içindir.
 * 
 * @author E-Ticaret Projesi
 * @version 2.0.0 - Sıfırdan yeniden yazıldı
 */

import api from "./api";

// ============================================================
// YARDIMCI FONKSİYONLAR
// ============================================================

/**
 * Türkçe karakterleri ASCII'ye çevirip URL-uyumlu slug oluşturur
 * @param {string} name - Kategori adı
 * @returns {string} URL-uyumlu slug
 */
const createSlug = (name) => {
  if (!name) return "";
  
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

// ============================================================
// EVENT LISTENER SİSTEMİ
// Admin panelden kategori güncellendiğinde diğer bileşenleri bilgilendirir
// ============================================================

const listeners = [];

/**
 * Tüm listener'ları tetikler (kategori güncellendiğinde)
 */
const notify = () => {
  listeners.forEach((callback) => {
    try {
      callback();
    } catch (e) {
      // Listener hatası sessizce loglanır, uygulama çökmez
      console.error("[CategoryService] Listener hatası:", e);
    }
  });
};

// ============================================================
// KATEGORİ SERVİSİ
// ============================================================

const categoryServiceReal = {
  
  // ==================== PUBLIC ENDPOINT'LER ====================
  
  /**
   * Tüm kategorileri getirir (navigasyon menüsü için)
   * Backend: GET /api/categories
   * 
   * @returns {Promise<Array>} Kategori listesi
   */
  async getAll() {
    try {
      const response = await api.get("/categories");
      // API response'u array olmalı, değilse boş array döndür
      return Array.isArray(response) ? response : [];
    } catch (error) {
      console.error("[CategoryService] Kategoriler alınamadı:", error.message);
      return []; // Hata durumunda boş array döndür, UI çökmez
    }
  },

  /**
   * Aktif kategorileri getirir (header butonları için)
   * isActive: true veya undefined olan kategorileri filtreler
   * 
   * @returns {Promise<Array>} Aktif kategori listesi
   */
  async getActive() {
    const allCategories = await this.getAll();
    // isActive false olmayanları getir (true veya undefined kabul edilir)
    return allCategories.filter((cat) => cat.isActive !== false);
  },

  /**
   * Slug'a göre tek kategori getirir
   * Backend: GET /api/categories/{slug}
   * 
   * @param {string} slug - Kategori slug'ı
   * @returns {Promise<Object|null>} Kategori objesi veya null
   */
  async getBySlug(slug) {
    try {
      if (!slug) return null;
      return await api.get(`/categories/${encodeURIComponent(slug)}`);
    } catch (error) {
      console.error(`[CategoryService] Kategori bulunamadı: ${slug}`, error.message);
      return null;
    }
  },

  // ==================== ADMIN ENDPOINT'LER ====================

  /**
   * Admin: Tüm kategorileri getirir (aktif/pasif dahil)
   * Backend: GET /api/admin/categories
   */
  async getAllAdmin() {
    try {
      const response = await api.get("/admin/categories");
      return Array.isArray(response) ? response : [];
    } catch (error) {
      console.error("[CategoryService] Admin kategoriler alınamadı:", error.message);
      return [];
    }
  },

  /**
   * Admin: ID'ye göre kategori getirir
   */
  async getById(id) {
    return await api.get(`/admin/categories/${id}`);
  },

  /**
   * Admin: Yeni kategori oluşturur
   */
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
    notify(); // Diğer bileşenleri bilgilendir
    return result;
  },

  /**
   * Admin: Kategori günceller
   */
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

  /**
   * Admin: Kategori siler
   */
  async delete(id) {
    await api.delete(`/admin/categories/${id}`);
    notify();
    return { success: true };
  },

  /**
   * Admin: Aktif/Pasif durumunu değiştirir
   */
  async toggleActive(category) {
    const payload = {
      ...category,
      id: parseInt(category.id),
      isActive: !category.isActive,
    };
    const result = await api.put(`/admin/categories/${category.id}`, payload);
    notify();
    return result;
  },

  // ==================== SUBSCRIPTION SİSTEMİ ====================

  /**
   * Kategori değişikliklerini dinlemek için subscribe olur
   * @param {Function} callback - Değişiklik olduğunda çağrılacak fonksiyon
   * @returns {Function} Unsubscribe fonksiyonu
   */
  subscribe(callback) {
    listeners.push(callback);
    return () => {
      const index = listeners.indexOf(callback);
      if (index > -1) listeners.splice(index, 1);
    };
  },
};

export default categoryServiceReal;

