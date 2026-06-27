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

export const CATEGORY_SLUG_ALIASES = {
  diger: "ev-ve-mutfak",
  "sut-urunleri": "sut-ve-sut-urunleri",
  "meyve-sebze": "meyve-ve-sebze",
  "et-tavuk": "et-ve-et-urunleri",
  "et-tavuk-balik": "et-ve-et-urunleri",
};

export const normalizeCategorySlug = (slug) => {
  if (!slug || typeof slug !== "string") return "";
  const normalized = slug.trim().toLowerCase();
  return CATEGORY_SLUG_ALIASES[normalized] || normalized;
};

export const matchesCategorySlug = (candidateSlug, requestedSlug) => {
  const normalizedCandidate = normalizeCategorySlug(candidateSlug);
  const normalizedRequested = normalizeCategorySlug(requestedSlug);
  return (
    normalizedCandidate !== "" && normalizedCandidate === normalizedRequested
  );
};

const normalizeCategoryEntity = (category = {}) => {
  const name = String(category?.name ?? category?.Name ?? "").trim();
  const rawSlug = String(category?.slug ?? category?.Slug ?? "").trim();

  return {
    ...category,
    name,
    slug: rawSlug,
  };
};

const shouldReplaceCategory = (current, existing) => {
  const currentRawSlug = String(current?.slug ?? "").trim().toLowerCase();
  const existingRawSlug = String(existing?.slug ?? "").trim().toLowerCase();
  const currentNormalizedSlug = normalizeCategorySlug(currentRawSlug);
  const existingNormalizedSlug = normalizeCategorySlug(existingRawSlug);

  const currentIsCanonical = currentRawSlug === currentNormalizedSlug;
  const existingIsCanonical = existingRawSlug === existingNormalizedSlug;

  return currentIsCanonical && !existingIsCanonical;
};

const dedupeCategoriesBySlug = (categories = []) => {
  const seen = new Map();

  categories.forEach((item) => {
    const normalizedItem = normalizeCategoryEntity(item);
    const slugKey = normalizeCategorySlug(normalizedItem.slug);
    const dedupeKey = slugKey || `id:${normalizedItem.id ?? normalizedItem.name}`;
    const existing = seen.get(dedupeKey);

    if (!existing || shouldReplaceCategory(normalizedItem, existing)) {
      seen.set(dedupeKey, normalizedItem);
    }
  });

  return Array.from(seen.values());
};

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
      const response = await api.get("/api/categories");
      // API response'u array olmalı, değilse boş array döndür
      return Array.isArray(response)
        ? dedupeCategoriesBySlug(response)
        : [];
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
      const normalizedSlug = normalizeCategorySlug(slug);
      return await api.get(
        `/api/categories/${encodeURIComponent(normalizedSlug)}`,
      );
    } catch (error) {
      console.error(
        `[CategoryService] Kategori bulunamadı: ${slug}`,
        error.message,
      );
      return null;
    }
  },

  // ✨ YENİ: Hiyerarşik kategori ağacı
  /**
   * Hiyerarşik kategori ağacını getirir (tree yapısı)
   * Backend: GET /api/categories/tree
   *
   * @returns {Promise<Array>} Kategori ağacı
   */
  async getCategoryTree() {
    try {
      const response = await api.get("/api/categories/tree");
      const raw = Array.isArray(response) ? response : [];
      // Backend "Children" alanı döndürüyor, CategoryTile "subCategories" bekliyor
      // Her kategoriyi normalize et
      const normalize = (cat) => ({
        ...normalizeCategoryEntity(cat),
        subCategories: dedupeCategoriesBySlug(
          (cat.children || cat.Children || cat.subCategories || []).map(normalize),
        ),
      });
      return dedupeCategoriesBySlug(raw.map(normalize));
    } catch (error) {
      console.error(
        "[CategoryService] Kategori ağacı alınamadı:",
        error.message,
      );
      return [];
    }
  },

  // ✨ YENİ: Ana kategoriler (root)
  /**
   * Sadece ana kategorileri getirir (ParentId == null)
   * Backend: GET /api/categories/root
   *
   * @returns {Promise<Array>} Ana kategori listesi
   */
  async getRootCategories() {
    try {
      const response = await api.get("/api/categories/root");
      return Array.isArray(response)
        ? dedupeCategoriesBySlug(response)
        : [];
    } catch (error) {
      console.error(
        "[CategoryService] Ana kategoriler alınamadı:",
        error.message,
      );
      return [];
    }
  },

  // ==================== ADMIN ENDPOINT'LER ====================

  /**
   * Admin: Tüm kategorileri getirir (aktif/pasif dahil)
   * Backend: GET /api/admin/categories
   */
  async getAllAdmin() {
    try {
      const response = await api.get("/api/admin/categories");
      return Array.isArray(response) ? response : [];
    } catch (error) {
      console.error(
        "[CategoryService] Admin kategoriler alınamadı:",
        error.message,
      );
      return [];
    }
  },

  /**
   * Admin: ID'ye göre kategori getirir
   */
  async getById(id) {
    return await api.get(`/api/admin/categories/${id}`);
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

    const result = await api.post("/api/admin/categories", payload);
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

    const result = await api.put(`/api/admin/categories/${id}`, payload);
    notify();
    return result;
  },

  /**
   * Admin: Kategori siler
   */
  async delete(id) {
    await api.delete(`/api/admin/categories/${id}`);
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
    const result = await api.put(
      `/api/admin/categories/${category.id}`,
      payload,
    );
    notify();
    return result;
  },

  // ✨ YENİ ADMIN ENDPOINT'LER

  /**
   * Admin: Hiyerarşik kategori ağacı (pasifler dahil)
   * Backend: GET /api/admin/categories/tree
   */
  async getAdminCategoryTree() {
    try {
      const response = await api.get("/api/admin/categories/tree");
      return Array.isArray(response) ? response : [];
    } catch (error) {
      console.error(
        "[CategoryService] Admin kategori ağacı alınamadı:",
        error.message,
      );
      return [];
    }
  },

  /**
   * Admin: Ana kategoriler (pasifler dahil)
   * Backend: GET /api/admin/categories/root
   */
  async getAdminRootCategories() {
    try {
      const response = await api.get("/api/admin/categories/root");
      return Array.isArray(response) ? response : [];
    } catch (error) {
      console.error(
        "[CategoryService] Admin ana kategoriler alınamadı:",
        error.message,
      );
      return [];
    }
  },

  /**
   * Admin: Belirli kategorinin alt kategorileri
   * Backend: GET /api/admin/categories/{id}/subcategories
   */
  async getSubCategories(parentId) {
    try {
      const response = await api.get(
        `/api/admin/categories/${parentId}/subcategories`,
      );
      return Array.isArray(response) ? response : [];
    } catch (error) {
      console.error(
        `[CategoryService] Alt kategoriler alınamadı (parentId: ${parentId}):`,
        error.message,
      );
      return [];
    }
  },

  /**
   * Admin: Kategori yolu (breadcrumb)
   * Backend: GET /api/admin/categories/{id}/path
   */
  async getCategoryPath(categoryId) {
    try {
      const response = await api.get(
        `/api/admin/categories/${categoryId}/path`,
      );
      return Array.isArray(response) ? response : [];
    } catch (error) {
      console.error(
        `[CategoryService] Kategori yolu alınamadı (id: ${categoryId}):`,
        error.message,
      );
      return [];
    }
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
