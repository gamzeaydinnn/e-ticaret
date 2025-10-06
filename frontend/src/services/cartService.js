import api from "./api";

const base = "/api/cartitems";

export const CartService = {
  // Sepet öğelerini getir
  getCartItems: () => api.get(base).then((r) => r.data),

  // Sepete ürün ekle
  addItem: (productId, quantity = 1) =>
    api.post(base, { productId, quantity }).then((r) => r.data),

  // Sepet ürününü güncelle
  updateItem: (id, productId, quantity) =>
    api.put(`${base}/${id}`, { productId, quantity }).then((r) => r.data),

  // Sepet ürününü sil
  removeItem: (id) => api.delete(`${base}/${id}`).then((r) => r.data),

  // LocalStorage için guest sepet yönetimi
  getGuestCart: () => {
    const cart = localStorage.getItem("guestCart");
    const parsedCart = cart ? JSON.parse(cart) : [];
    console.log("getGuestCart çağrıldı, dönen veri:", parsedCart);
    return parsedCart;
  },

  addToGuestCart: (productId, quantity = 1) => {
    console.log("addToGuestCart çağrıldı:", { productId, quantity });
    const cart = CartService.getGuestCart();
    console.log("Mevcut sepet:", cart);
    const existingItem = cart.find((item) => item.productId === productId);

    if (existingItem) {
      existingItem.quantity += quantity;
      console.log("Mevcut ürünün miktarı artırıldı:", existingItem);
    } else {
      const newItem = {
        productId,
        quantity,
        addedAt: new Date().toISOString(),
      };
      cart.push(newItem);
      console.log("Yeni ürün sepete eklendi:", newItem);
    }

    localStorage.setItem("guestCart", JSON.stringify(cart));
    console.log("Güncellenen sepet localStorage'a kaydedildi:", cart);
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
