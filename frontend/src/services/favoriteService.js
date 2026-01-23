/**
 * Favori Servisi
 * Backend API ile iletişim kurar - localStorage KULLANMAZ (sadece token için)
 *
 * Mimari:
 * - Misafir kullanıcılar: X-Favorites-Token (UUID) header'ı ile backend'e istek atılır
 * - Kayıtlı kullanıcılar: JWT token ile backend'e istek atılır
 * - Token localStorage'da saklanır AMA favori verisi BACKEND'de tutulur
 */
import api from "./api";

const base = "/api/favorites";
const FAVORITES_TOKEN_KEY = "favorites_guest_token";

// ============================================================
// TOKEN YÖNETİMİ
// ============================================================

/**
 * Guest token'ı localStorage'dan alır veya yeni oluşturur
 * Token: UUID v4 formatında benzersiz kimlik
 */
const getOrCreateGuestToken = () => {
  let token = localStorage.getItem(FAVORITES_TOKEN_KEY);
  if (!token) {
    // Crypto API ile güvenli UUID oluştur
    token = crypto.randomUUID?.() || generateUUID();
    localStorage.setItem(FAVORITES_TOKEN_KEY, token);
    console.log(
      "🆕 Yeni favorites guest token oluşturuldu:",
      token.substring(0, 8) + "...",
    );
  }
  return token;
};

/**
 * Fallback UUID generator
 */
const generateUUID = () => {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
};

/**
 * Guest token'ı döner (varsa)
 */
const getGuestToken = () => {
  return localStorage.getItem(FAVORITES_TOKEN_KEY);
};

/**
 * Guest token'ı temizler (login sonrası merge işleminden sonra)
 */
const clearGuestToken = () => {
  localStorage.removeItem(FAVORITES_TOKEN_KEY);
  console.log("🗑️ Favorites guest token temizlendi");
};

export const FavoriteService = {
  // Token metodlarını dışa aktar
  getOrCreateGuestToken,
  getGuestToken,
  clearGuestToken,

  // ============================================================
  // MİSAFİR KULLANICI API'leri
  // ============================================================

  /**
   * Misafir kullanıcının favorilerini getirir
   * @returns {Promise<Array<ProductListDto>>}
   */
  getGuestFavorites: async () => {
    const token = getGuestToken();
    if (!token) {
      console.log("📭 Favorites token yok - boş liste");
      return [];
    }

    try {
      // api interceptor zaten res.data döndürüyor
      // Backend: { success: true, data: [...] } döner
      const response = await api.get(`${base}/guest`, {
        headers: { "X-Favorites-Token": token },
      });
      const data = response?.data || response || [];
      console.log(
        "⭐ Guest favoriler alındı:",
        Array.isArray(data) ? data.length : 0,
        "ürün",
      );
      return Array.isArray(data) ? data : [];
    } catch (error) {
      console.error("❌ Guest favoriler alınamadı:", error);
      return [];
    }
  },

  /**
   * Misafir kullanıcının favorisine ürün ekler/çıkarır (toggle)
   * @param {number} productId - Ürün ID
   * @returns {Promise<{success: boolean, action?: string, error?: string}>}
   */
  toggleGuestFavorite: async (productId) => {
    const token = getOrCreateGuestToken();

    try {
      const response = await api.post(`${base}/guest/${productId}`, null, {
        headers: { "X-Favorites-Token": token },
      });
      console.log("⭐ Guest favori toggle:", productId, response?.action);
      return { success: true, action: response?.action || "toggled" };
    } catch (error) {
      console.error("❌ Guest favori toggle başarısız:", error);
      return {
        success: false,
        error: error?.response?.data?.message || "İşlem başarısız",
      };
    }
  },

  /**
   * Misafir kullanıcının favorisinden ürün siler
   * @param {number} productId - Ürün ID
   */
  removeGuestFavorite: async (productId) => {
    const token = getGuestToken();
    if (!token) return { success: false, error: "Token yok" };

    try {
      await api.delete(`${base}/guest/${productId}`, {
        headers: { "X-Favorites-Token": token },
      });
      console.log("🗑️ Guest favoriden silindi:", productId);
      return { success: true };
    } catch (error) {
      console.error("❌ Guest favoriden silme başarısız:", error);
      return {
        success: false,
        error: error?.response?.data?.message || "Silinemedi",
      };
    }
  },

  // ============================================================
  // KAYITLI KULLANICI API'leri (JWT bazlı)
  // ============================================================

  /**
   * Kayıtlı kullanıcının favorilerini getirir
   */
  getFavorites: async () => {
    try {
      // Backend: { success: true, data: [...] } döner
      const response = await api.get(base);
      const data = response?.data || response || [];
      console.log(
        "⭐ Favoriler alındı:",
        Array.isArray(data) ? data.length : 0,
        "ürün",
      );
      return Array.isArray(data) ? data : [];
    } catch (error) {
      console.error("❌ Favoriler alınamadı:", error);
      return [];
    }
  },

  /**
   * Kayıtlı kullanıcının favorisine ürün ekler/çıkarır
   */
  toggleFavorite: async (productId) => {
    try {
      const response = await api.post(`${base}/${productId}`);
      console.log("⭐ Favori toggle:", productId);
      return { success: true, action: response?.action || "toggled" };
    } catch (error) {
      console.error("❌ Favori toggle başarısız:", error);
      return {
        success: false,
        error: error?.response?.data?.message || "İşlem başarısız",
      };
    }
  },

  /**
   * Kayıtlı kullanıcının favorisinden ürün siler
   */
  removeFavorite: async (productId) => {
    try {
      await api.delete(`${base}/${productId}`);
      console.log("🗑️ Favoriden silindi:", productId);
      return { success: true };
    } catch (error) {
      console.error("❌ Favoriden silme başarısız:", error);
      return {
        success: false,
        error: error?.response?.data?.message || "Silinemedi",
      };
    }
  },

  // ============================================================
  // MERGE API (Login Sonrası)
  // ============================================================

  /**
   * Misafir favorilerini kayıtlı kullanıcıya aktarır
   * Login başarılı olduktan sonra çağrılmalı
   * @returns {Promise<{mergedCount: number, message: string}>}
   */
  mergeGuestFavorites: async () => {
    const token = getGuestToken();
    if (!token) {
      console.log("📭 Favorites guest token yok - merge atlanıyor");
      return { mergedCount: 0, message: "Misafir favori yok" };
    }

    try {
      const response = await api.post(`${base}/merge`, {
        guestToken: token,
      });

      // Başarılı merge sonrası token'ı temizle
      if (response?.mergedCount > 0) {
        clearGuestToken();
      }

      console.log("🔄 Favori merge tamamlandı:", response);
      return response || { mergedCount: 0, message: "Bilinmiyor" };
    } catch (error) {
      console.error("❌ Favori merge başarısız:", error);
      return { mergedCount: 0, message: "Merge başarısız" };
    }
  },

  // ============================================================
  // FAVORİ KONTROL (Hızlı erişim için ID listesi)
  // ============================================================

  /**
   * Favori product ID'lerini döner (isFavorite kontrolü için)
   * @param {boolean} isAuthenticated - Kullanıcı giriş yapmış mı
   * @returns {Promise<number[]>}
   */
  getFavoriteIds: async (isAuthenticated) => {
    try {
      let favorites;
      if (isAuthenticated) {
        favorites = await FavoriteService.getFavorites();
      } else {
        favorites = await FavoriteService.getGuestFavorites();
      }
      // Her bir favori objesinden productId veya id al
      return favorites.map((f) => f.id || f.productId);
    } catch (error) {
      console.error("❌ Favori ID'leri alınamadı:", error);
      return [];
    }
  },
};
