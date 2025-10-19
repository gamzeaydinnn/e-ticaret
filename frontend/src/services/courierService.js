import api from "./api";
import { API_CONFIG } from "../config/apiConfig";

const base = "/api/couriers";
const { USE_MOCK_DATA } = API_CONFIG;

// Mock kurye verileri
const mockCouriers = [
  {
    id: 1,
    name: "Ahmet Yılmaz",
    email: "ahmet@courier.com",
    phone: "0532 123 4567",
    status: "active",
    activeOrders: 3,
    completedToday: 12,
    rating: 4.8,
    vehicle: "Motosiklet",
    location: "Kadıköy, İstanbul",
  },
  {
    id: 2,
    name: "Mehmet Demir",
    email: "mehmet@courier.com",
    phone: "0535 987 6543",
    status: "busy",
    activeOrders: 5,
    completedToday: 8,
    rating: 4.6,
    vehicle: "Bisiklet",
    location: "Beşiktaş, İstanbul",
  },
];

const mockOrders = [
  {
    id: 1,
    customerName: "Ayşe Kaya",
    customerPhone: "0534 555 1234",
    address: "Atatürk Cad. No: 45/3 Kadıköy, İstanbul",
    items: ["Domates 1kg", "Ekmek 2 adet", "Süt 1lt"],
    totalAmount: 45.5,
    status: "preparing",
    orderTime: new Date(Date.now() - 1000 * 60 * 30).toISOString(),
    assignedAt: new Date(Date.now() - 1000 * 60 * 20).toISOString(),
    estimatedDelivery: new Date(Date.now() + 1000 * 60 * 40).toISOString(),
    priority: "normal",
  },
  {
    id: 2,
    customerName: "Fatma Özkan",
    customerPhone: "0532 444 5678",
    address: "Bağdat Cad. No: 123/7 Maltepe, İstanbul",
    items: ["Et 500g", "Patates 2kg", "Soğan 1kg"],
    totalAmount: 125.75,
    status: "ready",
    orderTime: new Date(Date.now() - 1000 * 60 * 45).toISOString(),
    assignedAt: new Date(Date.now() - 1000 * 60 * 35).toISOString(),
    estimatedDelivery: new Date(Date.now() + 1000 * 60 * 25).toISOString(),
    priority: "urgent",
  },
];

export const CourierService = {
<<<<<<< HEAD
  // Admin - Tüm kuryeleri listele
  getAll: () => {
    if (USE_MOCK_DATA) {
      return Promise.resolve(mockCouriers);
    }
    return api
      .get(base)
      .then((res) => res.data)
      .catch(() => mockCouriers);
  },

  // Kurye giriş
  login: (email, password) => {
    if (USE_MOCK_DATA) {
      const courier = mockCouriers.find((c) => c.email === email);
      if (courier && password === "123456") {
        return Promise.resolve({
          success: true,
          courier: { ...courier, token: `mock-token-${courier.id}` },
        });
      }
      return Promise.reject({ message: "Geçersiz giriş bilgileri" });
    }
    return api
      .post(`${base}/login`, { email, password })
      .then((res) => res.data);
  },

  // Kurye siparişlerini listele
  getAssignedOrders: (courierId) => {
    if (USE_MOCK_DATA) {
      return Promise.resolve(mockOrders);
    }
    return api
      .get(`${base}/${courierId}/orders`)
      .then((res) => res.data)
      .catch(() => mockOrders);
  },
=======
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
>>>>>>> sare-branch

  // Sipariş durumunu güncelle
  updateOrderStatus: (orderId, status, notes = "") => {
    if (USE_MOCK_DATA) {
      const order = mockOrders.find((o) => o.id === orderId);
      if (order) {
        order.status = status;
        order.lastUpdate = new Date().toISOString();
        if (notes) order.notes = notes;
      }
      return Promise.resolve({ success: true, order });
    }
    return api
      .patch(`${base}/orders/${orderId}/status`, { status, notes })
      .then((res) => res.data);
  },

  // Admin için kurye performans raporu
  getCourierPerformance: (courierId, period = "today") => {
    if (USE_MOCK_DATA) {
      const courier = mockCouriers.find((c) => c.id === courierId);
      return Promise.resolve({
        courier,
        deliveries: { total: 12, onTime: 10, delayed: 2, cancelled: 0 },
        rating: 4.7,
        timeline: [
          { time: "09:00", action: "Vardiya başladı", status: "active" },
          {
            time: "09:15",
            action: "Sipariş #1001 teslim alındı",
            status: "picked_up",
          },
          {
            time: "09:45",
            action: "Sipariş #1001 teslim edildi",
            status: "delivered",
          },
        ],
      });
    }
    return api
      .get(`${base}/${courierId}/performance`, { params: { period } })
      .then((res) => res.data);
  },

  // Mevcut metotlar korunuyor
  getById: (id) => api.get(`${base}/${id}`).then((res) => res.data),
  add: (courier) => api.post(base, courier).then((res) => res.data),
  update: (id, courier) =>
    api.put(`${base}/${id}`, courier).then((res) => res.data),
  remove: (id) => api.delete(`${base}/${id}`).then((res) => res.data),
  myOrders: () => api.get("/courier/orders").then((res) => res.data),
  updateStatus: (orderId, status) =>
<<<<<<< HEAD
    api
      .post(`/courier/orders/${orderId}/status`, { status })
      .then((res) => res.data),
=======
    api.post(`/courier/orders/${orderId}/status`, { status }),
>>>>>>> sare-branch
};
