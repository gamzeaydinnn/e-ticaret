/**
 * categoryServiceReal.js
 *
 * Kategori Servisi - Backend API ile iletişim (Mikro ERP Ana/Alt Grup Hiyerarşisi Uyumlu)
 */

import api from "./api";

export const CATEGORY_SLUG_ALIASES = {
  "sut-urunleri": "sut-ve-sut-urunleri",
  "meyve-sebze": "meyve-ve-sebze",
  "et-tavuk": "et-ve-et-urunleri",
  "et-tavuk-balik": "et-ve-et-urunleri",
  "dondurulmus-gida": "dondurma-ve-dondurulmus-gida",
  "dondurma-dondurulmus": "dondurma-ve-dondurulmus-gida",
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
  const rawSubs =
    category?.subCategories ??
    category?.SubCategories ??
    category?.children ??
    category?.Children ??
    [];

  return {
    ...category,
    id: category?.id ?? category?.Id,
    name,
    slug: rawSlug,
    imageUrl: category?.imageUrl ?? category?.ImageUrl ?? "",
    parentId: category?.parentId ?? category?.ParentId ?? null,
    sortOrder: category?.sortOrder ?? category?.SortOrder ?? 0,
    isActive: (category?.isActive ?? category?.IsActive) !== false,
    mikroAnaGrupKod:
      category?.mikroAnaGrupKod ?? category?.MikroAnaGrupKod ?? null,
    mikroAltGrupKod:
      category?.mikroAltGrupKod ?? category?.MikroAltGrupKod ?? null,
    mikroIsim: category?.mikroIsim ?? category?.MikroIsim ?? null,
    navbarGroupKey:
      category?.navbarGroupKey ?? category?.NavbarGroupKey ?? null,
    isMikroSynced: Boolean(
      category?.isMikroSynced ?? category?.IsMikroSynced ?? false,
    ),
    subCategories: Array.isArray(rawSubs)
      ? rawSubs.map((sc) => ({
          ...sc,
          id: sc?.id ?? sc?.Id,
          name: String(sc?.name ?? sc?.Name ?? "").trim(),
          slug: String(sc?.slug ?? sc?.Slug ?? "").trim(),
          imageUrl: sc?.imageUrl ?? sc?.ImageUrl ?? "",
          parentId: sc?.parentId ?? sc?.ParentId ?? null,
          sortOrder: sc?.sortOrder ?? sc?.SortOrder ?? 0,
          isActive: (sc?.isActive ?? sc?.IsActive) !== false,
          mikroAnaGrupKod: sc?.mikroAnaGrupKod ?? sc?.MikroAnaGrupKod ?? null,
          mikroAltGrupKod: sc?.mikroAltGrupKod ?? sc?.MikroAltGrupKod ?? null,
          navbarGroupKey: sc?.navbarGroupKey ?? sc?.NavbarGroupKey ?? null,
        }))
      : [],
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
    const dedupeKey =
      slugKey || `id:${normalizedItem.id ?? normalizedItem.name}`;
    const existing = seen.get(dedupeKey);

    if (!existing || shouldReplaceCategory(normalizedItem, existing)) {
      seen.set(dedupeKey, normalizedItem);
    }
  });

  return Array.from(seen.values());
};

/**
 * Türkçe karakterleri ASCII'ye çevirip URL-uyumlu slug oluşturur
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

const listeners = [];

const notify = () => {
  listeners.forEach((callback) => {
    try {
      callback();
    } catch (e) {
      console.error("[CategoryService] Listener hatası:", e);
    }
  });
};

const categoryServiceReal = {
  // ==================== PUBLIC ENDPOINT'LER ====================

  async getAll() {
    try {
      const response = await api.get("/api/categories");
      return Array.isArray(response) ? dedupeCategoriesBySlug(response) : [];
    } catch (error) {
      console.error("[CategoryService] Kategoriler alınamadı:", error.message);
      return [];
    }
  },

  async getActive() {
    const allCategories = await this.getAll();
    return allCategories.filter(
      (cat) =>
        cat.isActive !== false &&
        String(cat.slug || "").toLowerCase() !== "diger",
    );
  },

  async getBySlug(slug) {
    try {
      if (!slug) return null;
      const normalizedSlug = normalizeCategorySlug(slug);
      const res = await api.get(
        `/api/categories/${encodeURIComponent(normalizedSlug)}`,
      );
      return res ? normalizeCategoryEntity(res) : null;
    } catch (error) {
      console.error(
        `[CategoryService] Kategori bulunamadı: ${slug}`,
        error.message,
      );
      return null;
    }
  },

  /**
   * Kategori sayfası için yapışkan (sticky) alt kategori barı + scroll-spy bölümlerini getirir
   * Backend: GET /api/categories/{slug}/sections
   */
  async getSectionsBySlug(slug) {
    try {
      if (!slug) return null;
      const normalizedSlug = normalizeCategorySlug(slug);
      return await api.get(
        `/api/categories/${encodeURIComponent(normalizedSlug)}/sections`,
      );
    } catch (error) {
      console.error(
        `[CategoryService] Kategori bölümleri alınamadı: ${slug}`,
        error.message,
      );
      return null;
    }
  },

  async getCategoryTree() {
    try {
      const response = await api.get("/api/categories/tree");
      const raw = Array.isArray(response) ? response : [];
      const normalize = (cat) => {
        const kids = dedupeCategoriesBySlug(
          (
            cat.children ||
            cat.Children ||
            cat.subCategories ||
            cat.SubCategories ||
            []
          )
            .filter((k) => (k?.isActive ?? k?.IsActive) !== false)
            .map(normalize),
        );
        return {
          ...normalizeCategoryEntity(cat),
          children: kids,
          subCategories: kids,
        };
      };
      return dedupeCategoriesBySlug(
        raw
          .filter(
            (c) =>
              (c?.isActive ?? c?.IsActive) !== false &&
              String(c?.slug ?? c?.Slug ?? "").toLowerCase() !== "diger",
          )
          .map(normalize),
      );
    } catch (error) {
      console.error(
        "[CategoryService] Kategori ağacı alınamadı:",
        error.message,
      );
      return [];
    }
  },

  async getRootCategories() {
    try {
      const response = await api.get("/api/categories/root");
      return Array.isArray(response) ? dedupeCategoriesBySlug(response) : [];
    } catch (error) {
      console.error(
        "[CategoryService] Ana kategoriler alınamadı:",
        error.message,
      );
      return [];
    }
  },

  // ==================== ADMIN ENDPOINT'LER ====================

  async getAllAdmin() {
    try {
      const response = await api.get("/api/admin/categories", {
        timeout: 20000,
      });
      return Array.isArray(response)
        ? response.map(normalizeCategoryEntity)
        : [];
    } catch (error) {
      console.error(
        "[CategoryService] Admin kategoriler alınamadı:",
        error.message,
      );
      throw error;
    }
  },

  async getById(id) {
    return await api.get(`/api/admin/categories/${id}`);
  },

  async create(category) {
    const payload = {
      name: category.name,
      description: category.description || "",
      slug: category.slug || createSlug(category.name),
      imageUrl: category.imageUrl || "",
      parentId: category.parentId || null,
      sortOrder: parseInt(category.sortOrder) || 0,
      isActive: category.isActive !== false,
      navbarGroupKey: category.navbarGroupKey || null,
    };

    const result = await api.post("/api/admin/categories", payload);
    notify();
    return result;
  },

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
      navbarGroupKey:
        category.navbarGroupKey !== undefined ? category.navbarGroupKey : null,
    };

    const result = await api.put(`/api/admin/categories/${id}`, payload);
    notify();
    return result;
  },

  async delete(id) {
    await api.delete(`/api/admin/categories/${id}`);
    notify();
    return { success: true };
  },

  async toggleActive(category) {
    const nextIsActive = !category.isActive;
    try {
      const result = await api.patch(
        `/api/admin/categories/${category.id}/toggle-active`,
        { isActive: nextIsActive },
      );
      notify();
      return result;
    } catch {
      const payload = {
        ...category,
        id: parseInt(category.id),
        isActive: nextIsActive,
      };
      const result = await api.put(
        `/api/admin/categories/${category.id}`,
        payload,
      );
      notify();
      return result;
    }
  },

  async syncFromMikro() {
    const result = await api.post("/api/admin/categories/sync-mikro");
    notify();
    return result;
  },

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

  async getSubCategories(parentId) {
    try {
      const response = await api.get(
        `/api/categories/${parentId}/subcategories`,
      );
      return Array.isArray(response)
        ? response.map(normalizeCategoryEntity)
        : [];
    } catch (error) {
      console.error(
        `[CategoryService] Alt kategoriler alınamadı (parentId: ${parentId}):`,
        error.message,
      );
      return [];
    }
  },

  async getCategoryPath(categoryId) {
    try {
      const response = await api.get(
        `/api/admin/categories/${categoryId}/path`,
      );
      return Array.isArray(response) ? response : [];
    } catch {
      return [];
    }
  },

  async uploadCategoryImage(file) {
    const formData = new FormData();
    formData.append("image", file);
    const response = await api.post(
      "/api/admin/categories/upload-image",
      formData,
      {
        headers: { "Content-Type": "multipart/form-data" },
      },
    );
    return response?.imageUrl || response?.data?.imageUrl || null;
  },

  subscribe(callback) {
    listeners.push(callback);
    return () => {
      const index = listeners.indexOf(callback);
      if (index > -1) listeners.splice(index, 1);
    };
  },
};

export default categoryServiceReal;
