// ==========================================================================
// weightAdjustmentService.js - Ağırlık Fark Yönetimi API Servisi
// ==========================================================================
// Backend'deki WeightAdjustmentController ve WeightBasedPaymentController
// ile iletişim kuran servis katmanı.
//
// Endpoint'ler:
// - /api/weight-adjustment/* → Ağırlık girişi ve fark yönetimi
// - /api/weight-payment/* → Ödeme işlemleri (pre-auth, post-auth, refund)
// ==========================================================================

import api from "./api";

const WEIGHT_ADJUSTMENT_BASE = "/weight-adjustment";
const WEIGHT_PAYMENT_BASE = "/weight-payment";

/**
 * Ağırlık Fark Yönetimi Servisi
 * Kurye paneli ve admin paneli için kullanılır
 */
export const WeightAdjustmentService = {
  // =========================================================================
  // KURYE İŞLEMLERİ
  // =========================================================================

  /**
   * Tek bir ürün için ağırlık girişi yap
   * @param {number} orderId - Sipariş ID
   * @param {number} orderItemId - Sipariş kalemi ID
   * @param {object} weightData - Ağırlık bilgileri
   * @returns {Promise} API yanıtı
   */
  recordWeight: async (orderId, orderItemId, weightData) => {
    try {
      const response = await api.post(
        `${WEIGHT_ADJUSTMENT_BASE}/orders/${orderId}/items/${orderItemId}/weigh`,
        weightData,
      );
      return response.data;
    } catch (error) {
      console.error("[WeightAdjustmentService] recordWeight error:", error);
      throw error;
    }
  },

  /**
   * Toplu ağırlık girişi yap (birden fazla ürün)
   * @param {number} orderId - Sipariş ID
   * @param {Array} items - Ağırlık bilgileri dizisi
   * @returns {Promise} API yanıtı
   */
  recordBulkWeights: async (orderId, items) => {
    try {
      const response = await api.post(
        `${WEIGHT_ADJUSTMENT_BASE}/orders/${orderId}/weigh-bulk`,
        { items },
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] recordBulkWeights error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Sipariş ağırlık özetini getir
   * @param {number} orderId - Sipariş ID
   * @returns {Promise} Ağırlık özeti
   */
  getOrderWeightSummary: async (orderId) => {
    try {
      const response = await api.get(
        `${WEIGHT_ADJUSTMENT_BASE}/orders/${orderId}/summary`,
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] getOrderWeightSummary error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Sipariş fark hesaplamasını getir
   * @param {number} orderId - Sipariş ID
   * @returns {Promise} Fark hesabı detayları
   */
  calculateOrderDifference: async (orderId) => {
    try {
      const response = await api.get(
        `${WEIGHT_ADJUSTMENT_BASE}/orders/${orderId}/calculate`,
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] calculateOrderDifference error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Kuryenin bekleyen siparişlerini getir
   * @returns {Promise} Bekleyen siparişler listesi
   */
  getCourierPendingOrders: async () => {
    try {
      const response = await api.get(
        `${WEIGHT_ADJUSTMENT_BASE}/courier/pending`,
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] getCourierPendingOrders error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Nakit ödeme fark hesabı
   * @param {number} orderId - Sipariş ID
   * @param {object} settlementData - Ödeme bilgileri
   * @returns {Promise} Hesaplama sonucu
   */
  calculateCashSettlement: async (orderId, settlementData) => {
    try {
      const response = await api.post(
        `${WEIGHT_ADJUSTMENT_BASE}/orders/${orderId}/cash-settlement`,
        settlementData,
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] calculateCashSettlement error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Ağırlık değerini validate et
   * @param {object} validationData - Doğrulama parametreleri
   * @returns {Promise} Doğrulama sonucu
   */
  validateWeight: async (validationData) => {
    try {
      const response = await api.post(
        `${WEIGHT_ADJUSTMENT_BASE}/validate-weight`,
        validationData,
      );
      return response.data;
    } catch (error) {
      console.error("[WeightAdjustmentService] validateWeight error:", error);
      throw error;
    }
  },

  // =========================================================================
  // ADMIN İŞLEMLERİ
  // =========================================================================

  /**
   * Admin onayı bekleyen kayıtları getir
   * @returns {Promise} Bekleyen kayıtlar listesi
   */
  getAdminPendingList: async () => {
    try {
      const response = await api.get(`${WEIGHT_ADJUSTMENT_BASE}/admin/pending`);
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] getAdminPendingList error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Admin kararını kaydet (onay/red)
   * @param {number} adjustmentId - Fark kaydı ID
   * @param {object} decision - Karar bilgisi
   * @returns {Promise} İşlem sonucu
   */
  submitAdminDecision: async (adjustmentId, decision) => {
    try {
      const response = await api.post(
        `${WEIGHT_ADJUSTMENT_BASE}/admin/${adjustmentId}/decision`,
        decision,
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] submitAdminDecision error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Ağırlık fark istatistiklerini getir
   * @param {object} params - Filtre parametreleri
   * @returns {Promise} İstatistikler
   */
  getStatistics: async (params = {}) => {
    try {
      const response = await api.get(
        `${WEIGHT_ADJUSTMENT_BASE}/admin/statistics`,
        { params },
      );
      return response.data;
    } catch (error) {
      console.error("[WeightAdjustmentService] getStatistics error:", error);
      throw error;
    }
  },

  /**
   * Tüm fark kayıtlarını listele
   * @param {object} params - Filtre ve sayfalama parametreleri
   * @returns {Promise} Kayıt listesi
   */
  getAdjustmentList: async (params = {}) => {
    try {
      const response = await api.get(`${WEIGHT_ADJUSTMENT_BASE}/admin/list`, {
        params,
      });
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] getAdjustmentList error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Tek bir fark kaydının detayını getir
   * @param {number} adjustmentId - Fark kaydı ID
   * @returns {Promise} Kayıt detayı
   */
  getAdjustmentDetail: async (adjustmentId) => {
    try {
      const response = await api.get(
        `${WEIGHT_ADJUSTMENT_BASE}/admin/${adjustmentId}`,
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] getAdjustmentDetail error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Sipariş için admin incelemesi talep et
   * @param {number} orderId - Sipariş ID
   * @param {string} reason - Talep nedeni
   * @returns {Promise} İşlem sonucu
   */
  requestAdminReview: async (orderId, reason) => {
    try {
      const response = await api.post(
        `${WEIGHT_ADJUSTMENT_BASE}/admin/request-review/${orderId}`,
        { reason },
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] requestAdminReview error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Kurye performans raporunu getir
   * @param {number} courierId - Kurye ID
   * @param {object} params - Tarih aralığı vb.
   * @returns {Promise} Performans raporu
   */
  getCourierPerformance: async (courierId, params = {}) => {
    try {
      const response = await api.get(
        `${WEIGHT_ADJUSTMENT_BASE}/admin/courier/${courierId}/performance`,
        { params },
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightAdjustmentService] getCourierPerformance error:",
        error,
      );
      throw error;
    }
  },
};

/**
 * Ağırlık Bazlı Ödeme Servisi
 * Pre-Auth, Post-Auth, Refund işlemleri
 */
export const WeightPaymentService = {
  /**
   * Teslimatı tamamla (ödemeyi kesinleştir)
   * @param {number} orderId - Sipariş ID
   * @param {object} deliveryData - Teslimat bilgileri
   * @returns {Promise} İşlem sonucu
   */
  finalizeDelivery: async (orderId, deliveryData = {}) => {
    try {
      const response = await api.post(
        `${WEIGHT_PAYMENT_BASE}/orders/${orderId}/finalize-delivery`,
        deliveryData,
      );
      return response.data;
    } catch (error) {
      console.error("[WeightPaymentService] finalizeDelivery error:", error);
      throw error;
    }
  },

  /**
   * Nakit ödeme fark hesabı
   * @param {number} orderId - Sipariş ID
   * @returns {Promise} Fark hesabı
   */
  getCashDifference: async (orderId) => {
    try {
      const response = await api.get(
        `${WEIGHT_PAYMENT_BASE}/orders/${orderId}/cash-difference`,
      );
      return response.data;
    } catch (error) {
      console.error("[WeightPaymentService] getCashDifference error:", error);
      throw error;
    }
  },

  /**
   * Ödeme durumunu sorgula
   * @param {number} orderId - Sipariş ID
   * @returns {Promise} Ödeme durumu
   */
  getPaymentStatus: async (orderId) => {
    try {
      const response = await api.get(
        `${WEIGHT_PAYMENT_BASE}/orders/${orderId}/payment-status`,
      );
      return response.data;
    } catch (error) {
      console.error("[WeightPaymentService] getPaymentStatus error:", error);
      throw error;
    }
  },

  /**
   * Ön provizyon oluştur (kart ödemeleri için)
   * @param {number} orderId - Sipariş ID
   * @param {object} cardData - Kart bilgileri
   * @returns {Promise} Pre-auth sonucu
   */
  createPreAuth: async (orderId, cardData) => {
    try {
      const response = await api.post(
        `${WEIGHT_PAYMENT_BASE}/orders/${orderId}/pre-auth`,
        cardData,
      );
      return response.data;
    } catch (error) {
      console.error("[WeightPaymentService] createPreAuth error:", error);
      throw error;
    }
  },

  /**
   * Kesin çekim yap (post-authorization)
   * @param {number} orderId - Sipariş ID
   * @param {object} captureData - Çekim bilgileri
   * @returns {Promise} Post-auth sonucu
   */
  capturePayment: async (orderId, captureData = {}) => {
    try {
      const response = await api.post(
        `${WEIGHT_PAYMENT_BASE}/orders/${orderId}/post-auth`,
        captureData,
      );
      return response.data;
    } catch (error) {
      console.error("[WeightPaymentService] capturePayment error:", error);
      throw error;
    }
  },

  /**
   * Kısmi iade yap
   * @param {number} orderId - Sipariş ID
   * @param {object} refundData - İade bilgileri
   * @returns {Promise} İade sonucu
   */
  processPartialRefund: async (orderId, refundData) => {
    try {
      const response = await api.post(
        `${WEIGHT_PAYMENT_BASE}/orders/${orderId}/partial-refund`,
        refundData,
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightPaymentService] processPartialRefund error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Ön provizyonun hala geçerli olup olmadığını kontrol et
   * @param {number} orderId - Sipariş ID
   * @returns {Promise} Geçerlilik durumu
   */
  checkPreAuthValidity: async (orderId) => {
    try {
      const response = await api.get(
        `${WEIGHT_PAYMENT_BASE}/orders/${orderId}/pre-auth-valid`,
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightPaymentService] checkPreAuthValidity error:",
        error,
      );
      throw error;
    }
  },

  /**
   * Admin: Süresi dolan ön provizyonları temizle
   * @returns {Promise} İşlem sonucu
   */
  cleanupExpiredPreAuths: async () => {
    try {
      const response = await api.post(
        `${WEIGHT_PAYMENT_BASE}/admin/cancel-expired`,
      );
      return response.data;
    } catch (error) {
      console.error(
        "[WeightPaymentService] cleanupExpiredPreAuths error:",
        error,
      );
      throw error;
    }
  },
};

// Default export - her iki servisi de içerir
export default {
  adjustment: WeightAdjustmentService,
  payment: WeightPaymentService,
};
