// src/services/courierService.js
import api from "./api";

export const CourierService = {
  myOrders: () => api.get("/courier/orders"),
  updateStatus: (orderId, status) =>
    api.post(`/courier/orders/${orderId}/status`, { status }),
};
