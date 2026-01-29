// ==========================================================================
// dispatcherService.js - Sevkiyat Görevlisi API Servisi
// ==========================================================================
// Dispatcher paneli için tüm API çağrılarını yönetir.
// NEDEN: API çağrılarını merkezi bir yerde tutarak kod tekrarını önler
//        ve bakımı kolaylaştırır.
// ==========================================================================

import api from "./api";

// ============================================================================
// SABITLER
// ============================================================================

const DISPATCH_BASE = "/api/DispatcherOrder";

// ============================================================================
// TOKEN YÖNETİMİ
// ============================================================================

/**
 * Dispatcher token'ını header'a ekler
 * NEDEN: Her API çağrısında authentication sağlamak için
 */
const getAuthHeaders = () => {
  const token =
    localStorage.getItem("dispatcherToken") ||
    sessionStorage.getItem("dispatcherToken") ||
    localStorage.getItem("storeAttendantToken") ||
    sessionStorage.getItem("storeAttendantToken");

  return token ? { Authorization: `Bearer ${token}` } : {};
};

// ============================================================================
// SİPARİŞ İŞLEMLERİ
// ============================================================================

/**
 * Siparişleri getirir (durum filtresine göre)
 * @param {string} status - Sipariş durumu: "Ready", "Assigned", "PickedUp", "OutForDelivery"
 * @returns {Promise<Object>} Sipariş listesi ve özet bilgiler
 * NEDEN: Dispatcher panelinde duruma göre filtrelenmiş siparişleri göstermek için
 */
export const getOrders = async (status = null) => {
  try {
    const params = status ? { status } : {};
    const response = await api.get(`${DISPATCH_BASE}/orders`, {
      headers: getAuthHeaders(),
      params,
    });
    return {
      success: true,
      data: response.data,
    };
  } catch (error) {
    console.error("[DispatcherService] Siparişler alınamadı:", error);
    return {
      success: false,
      error: error.response?.data?.message || "Siparişler yüklenemedi",
    };
  }
};

/**
 * Sipariş detayını getirir
 * @param {number} orderId - Sipariş ID
 * @returns {Promise<Object>} Sipariş detayı
 * NEDEN: Sipariş kartına tıklandığında detay modal için
 */
export const getOrderDetail = async (orderId) => {
  try {
    const response = await api.get(`${DISPATCH_BASE}/orders/${orderId}`, {
      headers: getAuthHeaders(),
    });
    return {
      success: true,
      data: response.data,
    };
  } catch (error) {
    console.error("[DispatcherService] Sipariş detayı alınamadı:", error);
    return {
      success: false,
      error: error.response?.data?.message || "Sipariş detayı yüklenemedi",
    };
  }
};

/**
 * Acil siparişleri getirir (beklemede uzun süre kalanlar)
 * @returns {Promise<Object>} Acil sipariş listesi
 * NEDEN: Öncelikli siparişleri göstermek için
 */
export const getUrgentOrders = async () => {
  try {
    const response = await api.get(`${DISPATCH_BASE}/orders/urgent`, {
      headers: getAuthHeaders(),
    });
    return {
      success: true,
      data: response.data,
    };
  } catch (error) {
    console.error("[DispatcherService] Acil siparişler alınamadı:", error);
    return {
      success: false,
      error: error.response?.data?.message || "Acil siparişler yüklenemedi",
    };
  }
};

/**
 * Özet istatistikleri getirir
 * @returns {Promise<Object>} Dashboard özet bilgileri
 * NEDEN: Dashboard'daki istatistik kartları için
 */
export const getSummary = async () => {
  try {
    const response = await api.get(`${DISPATCH_BASE}/summary`, {
      headers: getAuthHeaders(),
    });
    return {
      success: true,
      data: response.data,
    };
  } catch (error) {
    console.error("[DispatcherService] Özet alınamadı:", error);
    return {
      success: false,
      error: error.response?.data?.message || "Özet bilgiler yüklenemedi",
    };
  }
};

// ============================================================================
// KURYE İŞLEMLERİ
// ============================================================================

/**
 * Müsait kuryeleri listeler
 * @returns {Promise<Object>} Kurye listesi
 * NEDEN: Kurye atama dropdown'ı için
 */
export const getCouriers = async () => {
  try {
    // api.js zaten res.data döndürüyor, tekrar .data yapmaya gerek yok
    const response = await api.get(`${DISPATCH_BASE}/couriers`, {
      headers: getAuthHeaders(),
    });
    console.log("🚴 [DispatcherService] Kurye API yanıtı:", response);
    return {
      success: true,
      data: response, // response zaten unwrap edilmiş data
    };
  } catch (error) {
    console.error("[DispatcherService] Kuryeler alınamadı:", error);
    return {
      success: false,
      error: error.response?.data?.message || "Kurye listesi yüklenemedi",
    };
  }
};

/**
 * Kurye detayını getirir
 * @param {number} courierId - Kurye ID
 * @returns {Promise<Object>} Kurye detayı ve aktif siparişleri
 * NEDEN: Kurye detay modal için
 */
export const getCourierDetail = async (courierId) => {
  try {
    const response = await api.get(`${DISPATCH_BASE}/couriers/${courierId}`, {
      headers: getAuthHeaders(),
    });
    return {
      success: true,
      data: response.data,
    };
  } catch (error) {
    console.error("[DispatcherService] Kurye detayı alınamadı:", error);
    return {
      success: false,
      error: error.response?.data?.message || "Kurye detayı yüklenemedi",
    };
  }
};

/**
 * Siparişe kurye atar
 * @param {number} orderId - Sipariş ID
 * @param {number} courierId - Atanacak kurye ID
 * @returns {Promise<Object>} İşlem sonucu
 * NEDEN: Hazır siparişlere kurye atamak için ana fonksiyon
 */
export const assignCourier = async (orderId, courierId) => {
  try {
    const response = await api.post(
      `${DISPATCH_BASE}/orders/${orderId}/assign`,
      { courierId },
      { headers: getAuthHeaders() },
    );
    return {
      success: true,
      data: response.data,
      message: "Kurye başarıyla atandı",
    };
  } catch (error) {
    console.error("[DispatcherService] Kurye atanamadı:", error);
    return {
      success: false,
      error: error.response?.data?.message || "Kurye atama başarısız",
    };
  }
};

/**
 * Siparişin kuryesini değiştirir
 * @param {number} orderId - Sipariş ID
 * @param {number} newCourierId - Yeni kurye ID
 * @param {string} reason - Değişiklik nedeni
 * @returns {Promise<Object>} İşlem sonucu
 * NEDEN: Kurye müsait değilse veya sorun varsa değişiklik yapmak için
 */
export const reassignCourier = async (orderId, newCourierId, reason = "") => {
  try {
    const response = await api.post(
      `${DISPATCH_BASE}/orders/${orderId}/reassign`,
      { newCourierId, reason },
      { headers: getAuthHeaders() },
    );
    return {
      success: true,
      data: response.data,
      message: "Kurye değişikliği başarılı",
    };
  } catch (error) {
    console.error("[DispatcherService] Kurye değiştirilemedi:", error);
    return {
      success: false,
      error: error.response?.data?.message || "Kurye değişikliği başarısız",
    };
  }
};

// ============================================================================
// NORMALIZER FONKSİYONLARI
// ============================================================================

/**
 * Sipariş verisini normalize eder
 * NEDEN: Backend'den gelen veriyi frontend'in beklediği formata dönüştürür
 */
export const normalizeOrder = (order) => {
  if (!order) return null;
  return {
    id: order.orderId || order.id,
    orderNumber: order.orderNumber,
    customerName: order.customerName,
    customerPhone: order.customerPhone,
    address: order.deliveryAddress || order.address,
    totalAmount: order.totalAmount,
    status: order.status,
    statusText: order.statusText,
    paymentMethod: order.paymentMethod,
    paymentStatus: order.paymentStatus,
    orderDate: order.orderDate,
    readyAt: order.readyAt,
    assignedAt: order.assignedAt,
    waitingTime: order.waitingTime,
    isUrgent: order.isUrgent,
    courierName: order.courierName,
    courierId: order.courierId,
    itemCount: order.itemCount,
    weightInGrams: order.weightInGrams,
  };
};

/**
 * Kurye verisini normalize eder
 * NEDEN: Backend'den gelen kurye verisini frontend formatına dönüştürür
 */
export const normalizeCourier = (courier) => {
  if (!courier) return null;
  return {
    id: courier.courierId || courier.id,
    name: courier.courierName || courier.name,
    phone: courier.phone,
    status: courier.status,
    statusText: courier.statusText,
    isOnline: courier.isOnline,
    vehicleType: courier.vehicleType,
    vehicleTypeText: courier.vehicleTypeText,
    activeOrderCount: courier.activeOrderCount,
    completedToday: courier.completedToday,
    lastSeenAt: courier.lastSeenAt,
    rating: courier.rating,
  };
};

// ============================================================================
// EXPORT
// ============================================================================

const dispatcherService = {
  getOrders,
  getOrderDetail,
  getUrgentOrders,
  getSummary,
  getCouriers,
  getCourierDetail,
  assignCourier,
  reassignCourier,
  normalizeOrder,
  normalizeCourier,
};

export default dispatcherService;
