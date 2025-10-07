import api from "./api";

const base = "/api/cartitems";

export const CartService = {
  // Sepet öğelerini getir
  getCartItems: () => api.get(base).then((r) => r.data),

  // Sepete ürün ekle
  addItem: (productId, quantity = 1) =>
    api.post(base, { productId, quantity }).then((r) => r.data),

  // Sepet ürününü güncelle
  updateItem: async (id, productId, quantity) => {
    try {
      return await api
        .put(`${base}/${id}`, { productId, quantity })
        .then((r) => r.data);
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
      return await api.delete(`${base}/${id}`).then((r) => r.data);
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
};
