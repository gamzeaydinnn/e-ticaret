import api from "./api";

const base = "/api/couriers"; // Backend'de CourierController route'unu kontrol et

export const CourierService = {
  // Tüm kuryeleri listele
  getAll: () => api.get(base),

  // Tek bir kurye getir
  getById: (id) => api.get(`${base}/${id}`),

  // Yeni kurye ekle
  add: (courier) => api.post(base, courier),

  // Kurye güncelle
  update: (id, courier) => api.put(`${base}/${id}`, courier),

  // Kurye sil
  remove: (id) => api.delete(`${base}/${id}`),

  // Kurye siparişlerini listele
  myOrders: () => api.get("/courier/orders"),

  // Sipariş durumunu güncelle
  updateStatus: (orderId, status) =>
    api.post(`/courier/orders/${orderId}/status`, { status }),
};
