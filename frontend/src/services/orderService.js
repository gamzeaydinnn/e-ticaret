import api from "../api/client";

const base = "/api/Orders";

export const createOrder = (payload) =>
  api.post(base, payload).then((r) => r.data);
export const getOrders = () => api.get(base).then((r) => r.data);
export const getOrderById = (id) =>
  api.get(`${base}/${id}`).then((r) => r.data);
