import api from "./api";

const base = "/api/Payments";

export const PaymentService = {
  // Ödeme başlat
  processPayment: (orderId, amount) =>
    api.post(`${base}/process`, { orderId, amount }).then(r => r.data),

  // Ödeme durumunu kontrol et
  checkPaymentStatus: (paymentId) =>
    api.get(`${base}/status/${paymentId}`).then(r => r.data),
};
