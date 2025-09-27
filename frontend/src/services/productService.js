import api from "../api/client";

const base = "/api/Product";

export const getAllProducts = () => api.get(base).then((r) => r.data);
export const getProductById = (id) =>
  api.get(`${base}/${id}`).then((r) => r.data);
export const searchProducts = (q) =>
  api.get(`${base}/search`, { params: { q } }).then((r) => r.data);
