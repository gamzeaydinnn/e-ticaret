import api from "./api";

const base = "/api/cartitems";

export const CartService = {
  // Sepet öğelerini getir
  getCartItems: () => api.get(base),

  // Sepete ürün ekle
  addItem: (productId, quantity = 1) => api.post(base, { productId, quantity }),

  // Sepet ürününü güncelle
  updateItem: async (id, productId, quantity) => {
    try {
      return await api.put(`${base}/${id}`, { productId, quantity });
    } catch (error) {
      console.log("Backend bağlantısı yok, localStorage kullanılıyor");
      // Backend hatası durumunda localStorage'e güncelle
      CartService.updateGuestCartItem(productId, quantity);
      return { success: true, fallback: true };
    }
  },

  // Sepet ürününü sil
  removeItem: async (id, productId) => {
    try {
      return await api.delete(`${base}/${id}`);
    } catch (error) {
      console.log("Backend bağlantısı yok, localStorage kullanılıyor");
      // Backend hatası durumunda localStorage'den sil
      CartService.removeFromGuestCart(productId);
      return { success: true, fallback: true };
    }
  },

  // LocalStorage için guest sepet yönetimi
  getGuestCart: () => {
    const cart = localStorage.getItem("guestCart");
    return cart ? JSON.parse(cart) : [];
  },

  addToGuestCart: (productId, quantity = 1) => {
    const cart = CartService.getGuestCart();
    const existingItem = cart.find((item) => item.productId === productId);

    if (existingItem) {
      existingItem.quantity += quantity;
    } else {
      cart.push({
        productId,
        quantity,
        addedAt: new Date().toISOString(),
      });
    }

    localStorage.setItem("guestCart", JSON.stringify(cart));
    return cart;
  },

  updateGuestCartItem: (productId, quantity) => {
    const cart = CartService.getGuestCart();
    const itemIndex = cart.findIndex((item) => item.productId === productId);

    if (itemIndex !== -1) {
      if (quantity <= 0) {
        cart.splice(itemIndex, 1);
      } else {
        cart[itemIndex].quantity = quantity;
      }
      localStorage.setItem("guestCart", JSON.stringify(cart));
    }

    return cart;
  },

  removeFromGuestCart: (productId) => {
    const cart = CartService.getGuestCart();
    const filteredCart = cart.filter((item) => item.productId !== productId);
    localStorage.setItem("guestCart", JSON.stringify(filteredCart));
    return filteredCart;
  },

  clearGuestCart: () => {
    localStorage.removeItem("guestCart");
  },

  getGuestCartCount: () => {
    const cart = CartService.getGuestCart();
    return cart.reduce((total, item) => total + item.quantity, 0);
  },

  // Shipping method persistence for guest flow (frontend only)
  getShippingMethod: () => {
    return localStorage.getItem("shippingMethod") || "motorcycle"; // default motokurye
  },

  setShippingMethod: (method) => {
    localStorage.setItem("shippingMethod", method);
  },

  previewPrice: (payload) => api.post(`${base}/price-preview`, payload),

  // ========================================
  // KUPON İŞLEMLERİ
  // ========================================
  
  /**
   * Kupon kodunu kontrol et (basit doğrulama)
   * @param {string} code - Kupon kodu
   * @returns {Promise<{isValid: boolean, message: string, coupon?: object}>}
   */
  checkCoupon: async (code) => {
    try {
      const response = await api.get(
        `/api/coupon/check/${encodeURIComponent(code)}`
      );
      return response;
    } catch (error) {
      const errorData = error?.raw?.response?.data || error?.response?.data;
      if (errorData) {
        return errorData;
      }
      throw error;
    }
  },

  /**
   * Kupon kodunu sepet detaylarıyla doğrula ve indirim hesapla
   * @param {string} couponCode - Kupon kodu
   * @param {Array} cartItems - Sepet ürünleri [{productId, quantity, unitPrice}]
   * @param {number} subtotal - Ara toplam
   * @returns {Promise<CouponValidationResult>}
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
          item.categoryId || item?.product?.categoryId || item?.product?.category?.id;

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
      const response = await api.post("/api/coupon/validate", payload);
      return response;
    } catch (error) {
      const errorData = error?.raw?.response?.data || error?.response?.data;
      if (errorData) {
        return errorData;
      }
      throw error;
    }
  },

  /**
   * Aktif kuponları getir
   * @returns {Promise<Array>}
   */
  getActiveCoupons: async () => {
    try {
      const response = await api.get("/api/coupon/active");
      return response;
    } catch (error) {
      console.error("Aktif kuponlar alınamadı:", error);
      return [];
    }
  },

  // Uygulanan kupon bilgisini localStorage'da sakla
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
  }
};
