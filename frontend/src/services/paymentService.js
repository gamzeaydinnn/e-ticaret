import api from "./api";

const base = "/api/Payments";

export const PaymentService = {
  // Ödeme başlat
  processPayment: (orderId, amount) =>
    api.post(`${base}/process`, { orderId, amount }),

  // Hosted checkout (Stripe Checkout / Iyzico) başlatır
  initiate: (orderId, amount, currency = "TRY") =>
    api.post(`${base}/init`, { orderId, amount, currency }),

  // Ödeme durumunu kontrol et
  checkPaymentStatus: (paymentId) =>
    api.get(`${base}/status/${paymentId}`),
};
