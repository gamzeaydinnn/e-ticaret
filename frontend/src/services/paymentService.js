// ═══════════════════════════════════════════════════════════════════════════════════════════════
// PAYMENT SERVICE
// Tüm ödeme işlemleri için API servisleri
// Desteklenen provider'lar: Stripe, Iyzico, PayPal, POSNET (Yapı Kredi)
// ═══════════════════════════════════════════════════════════════════════════════════════════════

import api from "./api";

const base = "/api/payments";

export const PaymentService = {
  // ═══════════════════════════════════════════════════════════════════════════
  // TEMEL ÖDEME İŞLEMLERİ
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Ödeme başlatır (legacy)
   * @param {number} orderId - Sipariş ID
   * @param {number} amount - Tutar
   */
  processPayment: (orderId, amount) =>
    api.post(`${base}/process`, { orderId, amount }),

  /**
   * Hosted checkout (Stripe/Iyzico) başlatır
   * @param {number} orderId - Sipariş ID
   * @param {number} amount - Tutar
   * @param {string} paymentMethod - Ödeme yöntemi
   * @param {string} currency - Para birimi
   */
  initiate: (orderId, amount, paymentMethod, currency = "TRY") =>
    api.post(`${base}/initiate`, { orderId, amount, paymentMethod, currency }),

  /**
   * Ödeme durumunu kontrol eder
   * @param {string} paymentId - Ödeme ID
   */
  checkPaymentStatus: (paymentId) => api.get(`${base}/status/${paymentId}`),

  // ═══════════════════════════════════════════════════════════════════════════
  // POSNET / YAPI KREDİ ÖDEME İŞLEMLERİ
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * POSNET 3D Secure ödeme başlatır
   * Kullanıcı banka sayfasına yönlendirilir
   * @param {Object} paymentData - Ödeme verileri
   * @param {number} paymentData.orderId - Sipariş ID
   * @param {number} paymentData.amount - Tutar (TL)
   * @param {string} paymentData.cardNumber - Kart numarası (16 hane)
   * @param {string} paymentData.expireDate - Son kullanma (MMYY veya MM/YY)
   * @param {string} paymentData.cvv - CVV (3-4 hane)
   * @param {string} paymentData.cardHolderName - Kart sahibi adı
   * @param {number} paymentData.installmentCount - Taksit sayısı (0=peşin)
   * @param {boolean} paymentData.use3DSecure - 3D Secure kullan
   * @returns {Promise<{success: boolean, redirectUrl?: string, error?: string}>}
   */
  initiatePosnet3DSecure: async (paymentData) => {
    try {
      // ExpireDate formatı: YYMM veya MMYY - Ayrıştır
      let expireMonth = paymentData.expireMonth;
      let expireYear = paymentData.expireYear;
      
      // Eğer expireDate geldi ise (YYMM formatında) parse et
      if (paymentData.expireDate && !expireMonth && !expireYear) {
        const exp = paymentData.expireDate.replace(/[\s\-\/]/g, "");
        if (exp.length === 4) {
          // YYMM formatı (backend bekliyor)
          expireYear = exp.substring(0, 2);
          expireMonth = exp.substring(2, 4);
        }
      }
      
      // Backend DTO sadece bu alanları bekliyor - ExtraField hata çıkarır!
      const response = await api.post(`${base}/posnet/3dsecure/initiate`, {
        orderId: paymentData.orderId,
        amount: paymentData.amount,
        cardNumber: paymentData.cardNumber?.replace(/\s/g, ""),
        expireMonth: expireMonth,
        expireYear: expireYear,
        cvv: paymentData.cvv,
        cardHolderName: paymentData.cardHolderName,
        installmentCount: paymentData.installmentCount || 0,
      });
      return response;
    } catch (error) {
      console.error("POSNET 3D Secure başlatma hatası:", error);
      throw error;
    }
  },

  /**
   * POSNET direkt satış (3D Secure olmadan)
   * ⚠️ PCI DSS uyumluluğu gerektirir - Dikkatli kullanın
   * @param {Object} paymentData - Ödeme verileri
   */
  processPosnetDirectSale: async (paymentData) => {
    try {
      const response = await api.post(`${base}/posnet/sale`, {
        orderId: paymentData.orderId,
        amount: paymentData.amount,
        paymentMethod: "posnet",
        currency: paymentData.currency || "TRY",
        cardNumber: paymentData.cardNumber?.replace(/\s/g, ""),
        expireDate: paymentData.expireDate?.replace("/", ""),
        cvv: paymentData.cvv,
        cardHolderName: paymentData.cardHolderName,
        installmentCount: paymentData.installmentCount || 0,
        use3DSecure: false,
      });
      return response;
    } catch (error) {
      console.error("POSNET direkt satış hatası:", error);
      throw error;
    }
  },

  /**
   * POSNET World Puan sorgulama
   * @param {string} cardNumber - Kart numarası
   * @param {string} expireDate - Son kullanma tarihi
   * @param {string} cvv - CVV
   * @returns {Promise<{success: boolean, availablePoints?: number, pointsAsTL?: number}>}
   */
  queryWorldPoints: async (cardNumber, expireDate, cvv) => {
    try {
      const response = await api.post(`${base}/posnet/points/query`, {
        cardNumber: cardNumber?.replace(/\s/g, ""),
        expireDate: expireDate?.replace("/", ""),
        cvv,
      });
      return response;
    } catch (error) {
      console.error("World puan sorgulama hatası:", error);
      return { success: false, availablePoints: 0, pointsAsTL: 0 };
    }
  },

  /**
   * Taksit seçeneklerini getirir (kart BIN'ine göre)
   * @param {string} cardBin - Kartın ilk 6 hanesi
   * @param {number} amount - Tutar
   * @returns {Promise<Array<{count: number, monthlyAmount: number, totalAmount: number}>>}
   */
  getInstallmentOptions: async (cardBin, amount) => {
    try {
      // Varsayılan taksit seçenekleri (gerçek API'den alınabilir)
      const installments = [
        {
          count: 0,
          label: "Tek Çekim",
          monthlyAmount: amount,
          totalAmount: amount,
        },
        {
          count: 2,
          label: "2 Taksit",
          monthlyAmount: amount / 2,
          totalAmount: amount,
        },
        {
          count: 3,
          label: "3 Taksit",
          monthlyAmount: amount / 3,
          totalAmount: amount,
        },
        {
          count: 6,
          label: "6 Taksit",
          monthlyAmount: amount / 6,
          totalAmount: amount * 1.02,
        },
        {
          count: 9,
          label: "9 Taksit",
          monthlyAmount: (amount / 9) * 1.03,
          totalAmount: amount * 1.03,
        },
        {
          count: 12,
          label: "12 Taksit",
          monthlyAmount: (amount / 12) * 1.05,
          totalAmount: amount * 1.05,
        },
      ];

      // API'den taksit oranları alınabilir
      // const response = await api.get(`${base}/installments?bin=${cardBin}&amount=${amount}`);
      // return response.installments;

      return installments;
    } catch (error) {
      console.error("Taksit seçenekleri alınamadı:", error);
      return [];
    }
  },

  // ═══════════════════════════════════════════════════════════════════════════
  // İADE VE İPTAL İŞLEMLERİ
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Ödeme iadesi yapar
   * @param {string} paymentId - Ödeme ID
   * @param {number} amount - İade tutarı
   * @param {string} reason - İade sebebi
   */
  refundPayment: async (paymentId, amount, reason) => {
    try {
      const response = await api.post(`${base}/refund`, {
        paymentId,
        amount,
        reason,
      });
      return response;
    } catch (error) {
      console.error("İade hatası:", error);
      throw error;
    }
  },

  /**
   * Ödeme iptali yapar (gün içi)
   * @param {number} paymentId - Ödeme ID
   * @param {string} reason - İptal sebebi
   */
  cancelPayment: async (paymentId, reason) => {
    try {
      const response = await api.post(`${base}/cancel`, {
        paymentId,
        reason,
      });
      return response;
    } catch (error) {
      console.error("İptal hatası:", error);
      throw error;
    }
  },
};

export default PaymentService;
