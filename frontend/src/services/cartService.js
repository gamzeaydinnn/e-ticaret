/**
 * Sepet Servisi
 * Backend API ile iletişim kurar - localStorage KULLANMAZ (sadece token için)
 *
 * Mimari:
 * - Misafir kullanıcılar: CartToken (UUID) ile tanımlanır - SessionStorage'da saklanır
 *   (Her tarayıcı penceresi/tab farklı session = farklı sepet)
 * - Kayıtlı kullanıcılar: JWT token ile tanımlanır
 * - Token sessionStorage'da saklanır AMA sepet verisi BACKEND'de tutulur
 *
 * NOT: SessionStorage kullanılması sayesinde:
 * - Farklı tarayıcılarda farklı misafir sepetleri
 * - Aynı tarayıcıda farklı tab'larda aynı sepet (aynı origin)
 * - Tarayıcı kapatıldığında sepet korunmaz (güvenlik için iyi)
 */
import api from "./api";

const base = "/api/cartitems";
const CART_TOKEN_KEY = "cart_guest_token";
const GUEST_SESSION_KEY = "guest_session_id";

// ============================================================
// TOKEN YÖNETİMİ
// ============================================================

/**
 * Benzersiz misafir session ID oluşturur veya mevcut olanı döndürür
 * SessionStorage kullanıldığı için her tarayıcı penceresi farklı session'a sahip olur
 */
const getOrCreateGuestSessionId = () => {
  // Önce sessionStorage kontrol et (mevcut session)
  let sessionId = sessionStorage.getItem(GUEST_SESSION_KEY);
  if (!sessionId) {
    // Yeni session ID oluştur
    sessionId = crypto.randomUUID?.() || generateUUID();
    sessionStorage.setItem(GUEST_SESSION_KEY, sessionId);
    console.log(
      "🆔 Yeni guest session oluşturuldu:",
      sessionId.substring(0, 8) + "...",
    );
  }
  return sessionId;
};

/**
 * Guest token'ı alır veya yeni oluşturur
 * Session ID ile birleştirilerek her tarayıcıda benzersiz token oluşur
 * Token: UUID v4 formatında benzersiz kimlik
 */
const getOrCreateGuestToken = () => {
  // Session ID'yi al
  const sessionId = getOrCreateGuestSessionId();

  // SessionStorage'dan token kontrol et (session bazlı)
  let token = sessionStorage.getItem(CART_TOKEN_KEY);

  // localStorage'dan da kontrol et (backward compatibility + tab arası paylaşım)
  if (!token) {
    token = localStorage.getItem(CART_TOKEN_KEY + "_" + sessionId);
  }

  if (!token) {
    // Crypto API ile güvenli UUID oluştur
    token = crypto.randomUUID?.() || generateUUID();
    // Her iki storage'a da kaydet
    sessionStorage.setItem(CART_TOKEN_KEY, token);
    localStorage.setItem(CART_TOKEN_KEY + "_" + sessionId, token);
    console.log(
      "🆕 Yeni guest token oluşturuldu:",
      token.substring(0, 8) + "...",
      "session:",
      sessionId.substring(0, 8) + "...",
    );
  }
  return token;
};

/**
 * Fallback UUID generator (crypto.randomUUID desteklenmiyorsa)
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
 * Önce sessionStorage, sonra localStorage kontrol eder
 */
const getGuestToken = () => {
  // Önce sessionStorage kontrol et
  let token = sessionStorage.getItem(CART_TOKEN_KEY);
  if (token) return token;

  // Session ID ile localStorage'dan kontrol et
  const sessionId = sessionStorage.getItem(GUEST_SESSION_KEY);
  if (sessionId) {
    token = localStorage.getItem(CART_TOKEN_KEY + "_" + sessionId);
  }

  return token || localStorage.getItem(CART_TOKEN_KEY);
};

/**
 * Guest session ID'yi döner
 */
const getGuestSessionId = () => {
  return sessionStorage.getItem(GUEST_SESSION_KEY);
};

/**
 * Guest token'ı temizler (login sonrası veya logout)
 */
const clearGuestToken = () => {
  const sessionId = sessionStorage.getItem(GUEST_SESSION_KEY);

  // SessionStorage'dan temizle
  sessionStorage.removeItem(CART_TOKEN_KEY);

  // localStorage'dan da temizle
  if (sessionId) {
    localStorage.removeItem(CART_TOKEN_KEY + "_" + sessionId);
  }
  localStorage.removeItem(CART_TOKEN_KEY);

  console.log("🗑️ Guest token temizlendi");
};

export const CartService = {
  // Token metodlarını dışa aktar
  getOrCreateGuestToken,
  getGuestToken,
  getGuestSessionId,
  getOrCreateGuestSessionId,
  clearGuestToken,

  // ============================================================
  // MİSAFİR KULLANICI API'leri (CartToken bazlı)
  // ============================================================

  /**
   * Misafir kullanıcının sepetini getirir
   * @returns {Promise<CartSummaryDto>}
   */
  getGuestCart: async () => {
    const token = getGuestToken();
    if (!token) {
      console.log("📭 Guest token yok - boş sepet");
      return { items: [], total: 0 };
    }

    try {
      const response = await api.get(`${base}/guest`, {
        headers: { "X-Cart-Token": token },
      });
      console.log(
        "🛒 Guest sepet alındı:",
        response?.items?.length || 0,
        "ürün",
      );
      return response;
    } catch (error) {
      console.error("❌ Guest sepet alınamadı:", error);
      return { items: [], total: 0 };
    }
  },

  /**
   * Misafir kullanıcının sepetine ürün ekler
   * @param {number} productId - Ürün ID
   * @param {number} quantity - Miktar
   * @param {number|null} variantId - Varyant ID (opsiyonel)
   * @returns {Promise<{success: boolean, data?: CartItemDto, error?: string}>}
   */
  addToGuestCart: async (productId, quantity = 1, variantId = null) => {
    const token = getOrCreateGuestToken();

    try {
      const response = await api.post(
        `${base}/guest`,
        {
          cartToken: token,
          productId,
          quantity,
          variantId,
        },
        {
          headers: { "X-Cart-Token": token },
        },
      );
      console.log("✅ Guest sepete eklendi:", productId, "x", quantity);
      return { success: true, data: response };
    } catch (error) {
      console.error("❌ Guest sepete ekleme başarısız:", error);
      const errorMessage =
        error?.response?.data?.message || error?.message || "Sepete eklenemedi";
      return { success: false, error: errorMessage };
    }
  },

  /**
   * Misafir kullanıcının sepet öğesini günceller
   * @param {number} productId - Ürün ID
   * @param {number} quantity - Yeni miktar (0 = sil)
   * @param {number|null} variantId - Varyant ID (opsiyonel)
   */
  updateGuestCartItem: async (productId, quantity, variantId = null) => {
    const token = getGuestToken();
    if (!token) return { success: false, error: "Token yok" };

    try {
      await api.put(
        `${base}/guest`,
        {
          cartToken: token,
          productId,
          quantity,
          variantId,
        },
        {
          headers: { "X-Cart-Token": token },
        },
      );
      console.log("✏️ Guest sepet güncellendi:", productId, "=>", quantity);
      return { success: true };
    } catch (error) {
      console.error("❌ Guest sepet güncellenemedi:", error);
      return {
        success: false,
        error: error?.response?.data?.message || "Güncellenemedi",
      };
    }
  },

  /**
   * Misafir kullanıcının sepetinden ürün siler
   * @param {number} productId - Ürün ID
   * @param {number|null} variantId - Varyant ID (opsiyonel)
   */
  removeFromGuestCart: async (productId, variantId = null) => {
    const token = getGuestToken();
    if (!token) return { success: false, error: "Token yok" };

    try {
      let url = `${base}/guest/${productId}`;
      if (variantId) {
        url += `?variantId=${variantId}`;
      }
      await api.delete(url, {
        headers: { "X-Cart-Token": token },
      });
      console.log("🗑️ Guest sepetten silindi:", productId);
      return { success: true };
    } catch (error) {
      console.error("❌ Guest sepetten silme başarısız:", error);
      return {
        success: false,
        error: error?.response?.data?.message || "Silinemedi",
      };
    }
  },

  /**
   * Misafir kullanıcının sepetini temizler
   */
  clearGuestCart: async () => {
    const token = getGuestToken();
    if (!token) return { success: true }; // Zaten boş

    try {
      await api.delete(`${base}/guest/clear`, {
        headers: { "X-Cart-Token": token },
      });
      console.log("🧹 Guest sepet temizlendi");
      return { success: true };
    } catch (error) {
      console.error("❌ Guest sepet temizlenemedi:", error);
      return { success: false };
    }
  },

  /**
   * Misafir sepet ürün sayısını döner
   */
  getGuestCartCount: async () => {
    const cart = await CartService.getGuestCart();
    return (
      cart?.items?.reduce((total, item) => total + (item.quantity || 0), 0) || 0
    );
  },

  // ============================================================
  // KAYITLI KULLANICI API'leri (JWT bazlı)
  // ============================================================

  /**
   * Kayıtlı kullanıcının sepetini getirir
   */
  getCartItems: () => api.get(base),

  /**
   * Kayıtlı kullanıcının sepetine ürün ekler
   */
  addItem: (productId, quantity = 1, variantId = null) =>
    api.post(base, { productId, quantity, variantId }),

  /**
   * Kayıtlı kullanıcının sepet öğesini günceller
   */
  updateItem: async (id, productId, quantity) => {
    try {
      return await api.put(`${base}/${id}`, { productId, quantity });
    } catch (error) {
      console.error("Backend sepet güncellenemedi:", error);
      throw error;
    }
  },

  /**
   * Kayıtlı kullanıcının sepetinden öğe siler
   */
  removeItem: async (id) => {
    try {
      return await api.delete(`${base}/${id}`);
    } catch (error) {
      console.error("Backend sepetten silme başarısız:", error);
      throw error;
    }
  },

  // ============================================================
  // MERGE API (Login Sonrası)
  // ============================================================

  /**
   * Misafir sepetini kayıtlı kullanıcıya aktarır
   * Login başarılı olduktan sonra çağrılmalı
   * @returns {Promise<{mergedCount: number, message: string}>}
   */
  mergeGuestCart: async () => {
    const token = getGuestToken();
    if (!token) {
      console.log("📭 Guest token yok - merge atlanıyor");
      return { mergedCount: 0, message: "Misafir sepet yok" };
    }

    try {
      const response = await api.post(
        `${base}/merge`,
        {
          cartToken: token,
        },
        {
          headers: { "X-Cart-Token": token },
        },
      );

      // Başarılı merge sonrası token'ı temizle
      if (response?.mergedCount > 0) {
        clearGuestToken();
      }

      console.log("🔄 Sepet merge tamamlandı:", response);
      return response;
    } catch (error) {
      console.error("❌ Sepet merge başarısız:", error);
      return { mergedCount: 0, message: "Merge başarısız" };
    }
  },

  // ============================================================
  // FİYAT HESAPLAMA
  // ============================================================

  /**
   * Sepet fiyat önizlemesi
   */
  previewPrice: (payload) => api.post(`${base}/price-preview`, payload),

  // ============================================================
  // KUPON İŞLEMLERİ
  // ============================================================

  /**
   * Kupon kodunu kontrol et (basit doğrulama)
   */
  checkCoupon: async (code) => {
    try {
      const response = await api.get(
        `/api/coupon/check/${encodeURIComponent(code)}`,
      );
      return response;
    } catch (error) {
      const errorData = error?.raw?.response?.data || error?.response?.data;
      if (errorData) return errorData;
      throw error;
    }
  },

  /**
   * Kupon kodunu sepet detaylarıyla doğrula ve indirim hesapla
   */
  validateCoupon: async (couponCode, cartItems, subtotal, shippingCost = 0) => {
    try {
      const normalizedItems = Array.isArray(cartItems) ? cartItems : [];
      const productIds = [];
      const categoryIds = [];
      const productQuantities = {};

      normalizedItems.forEach((item) => {
        const productId = item.productId || item.id;
        const categoryId =
          item.categoryId ||
          item?.product?.categoryId ||
          item?.product?.category?.id;

        if (productId != null) {
          productIds.push(productId);
          productQuantities[productId] =
            (productQuantities[productId] || 0) + (item.quantity || 0);
        }
        if (categoryId != null) {
          categoryIds.push(categoryId);
        }
      });

      const payload = {
        couponCode,
        cartTotal: Number.isFinite(subtotal) ? subtotal : 0,
        shippingCost: Number.isFinite(shippingCost) ? shippingCost : 0,
        productIds: [...new Set(productIds)],
        categoryIds: [...new Set(categoryIds)],
        productQuantities,
      };

      return await api.post("/api/coupon/validate", payload);
    } catch (error) {
      const errorData = error?.raw?.response?.data || error?.response?.data;
      if (errorData) return errorData;
      throw error;
    }
  },

  /**
   * Aktif kuponları getir
   */
  getActiveCoupons: async () => {
    try {
      return await api.get("/api/coupon/active");
    } catch (error) {
      console.error("Aktif kuponlar alınamadı:", error);
      return [];
    }
  },

  // ============================================================
  // KUPON STATE (localStorage - sadece UI state için)
  // ============================================================

  getAppliedCoupon: () => {
    const coupon = localStorage.getItem("appliedCoupon");
    return coupon ? JSON.parse(coupon) : null;
  },

  setAppliedCoupon: (couponData) => {
    if (couponData) {
      localStorage.setItem("appliedCoupon", JSON.stringify(couponData));
    } else {
      localStorage.removeItem("appliedCoupon");
    }
  },

  clearAppliedCoupon: () => {
    localStorage.removeItem("appliedCoupon");
  },

  // ============================================================
  // KARGO YÖNTEMİ (localStorage - UI preference)
  // ============================================================

  getShippingMethod: () => {
    return localStorage.getItem("shippingMethod") || "motorcycle";
  },

  setShippingMethod: (method) => {
    localStorage.setItem("shippingMethod", method);
  },
};

export default CartService;
