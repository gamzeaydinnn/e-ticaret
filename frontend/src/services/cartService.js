import api from "../api/client";

const base = "/api/CartItems";

export const getCart = () => api.get(base).then((r) => r.data);
export const addToCart = (productId, quantity = 1) =>
  api.post(base, { productId, quantity }).then((r) => r.data);
export const updateCartItem = (id, quantity) =>
  api.put(`${base}/${id}`, { id, quantity }).then((r) => r.data);
export const removeCartItem = (id) =>
  api.delete(`${base}/${id}`).then((r) => r.data);
