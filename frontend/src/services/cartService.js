import api from "./api";

const base = "/api/Carts";

// userId Guid olarak gönderiliyor varsayıyoruz
export const CartService = {
  // Sepeti getir
  getCart: (userId) => api.get(`${base}/${userId}`).then(r => r.data),

  // Sepete ürün ekle
  addItem: (userId, productVariantId, quantity) =>
    api.post(`${base}/${userId}`, { productVariantId, quantity }).then(r => r.data),

  // Sepet ürününü güncelle
  updateItem: (userId, cartItemId, quantity) =>
    api.put(`${base}/${userId}/${cartItemId}`, { quantity }).then(r => r.data),

  // Sepet ürününü sil
  removeItem: (userId, cartItemId) =>
    api.delete(`${base}/${userId}/${cartItemId}`).then(r => r.data),

  // Sepeti temizle
  clearCart: (userId) =>
    api.delete(`${base}/${userId}`).then(r => r.data),
};
