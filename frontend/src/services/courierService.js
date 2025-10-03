import api from "./api";

const base = "/api/couriers"; // Backend'de CourierController route'unu kontrol et

export const CourierService = {
  // Tüm kuryeleri listele
  getAll: () => api.get(base).then(res => res.data),

  // Tek bir kurye getir
  getById: (id) => api.get(`${base}/${id}`).then(res => res.data),

  // Yeni kurye ekle
  add: (courier) => api.post(base, courier).then(res => res.data),

  // Kurye güncelle
  update: (id, courier) => api.put(`${base}/${id}`, courier).then(res => res.data),

  // Kurye sil
  remove: (id) => api.delete(`${base}/${id}`).then(res => res.data),

  // Kurye siparişlerini listele
  myOrders: () => api.get("/courier/orders").then(res => res.data),

  // Sipariş durumunu güncelle
  updateStatus: (orderId, status) =>
    api.post(`/courier/orders/${orderId}/status`, { status }).then(res => res.data),
};
