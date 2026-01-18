/**
 * CourierLocationService.js - Kurye Konum Servisi
 *
 * Bu servis kuryelerin konum güncellemelerini ve sorgularını yönetir.
 *
 * Özellikler:
 * - Aktif kuryelerin konumlarını getir
 * - Kurye konum güncelleme (kurye panelinden)
 * - Teslimat bölgelerini getir
 * - Konum geçmişi sorgulama
 */

import api from "./api";

const base = "/courier-location";

export const CourierLocationService = {
  /**
   * Tüm aktif kuryelerin anlık konumlarını getir
   * Admin panelinde haritada göstermek için kullanılır
   */
  getActiveCourierLocations: async () => {
    try {
      const response = await api.get(`${base}/active`);
      return response || [];
    } catch (error) {
      console.error(
        "[CourierLocationService] getActiveCourierLocations hatası:",
        error,
      );
      throw error;
    }
  },

  /**
   * Belirli bir kuryenin konum bilgisini getir
   * @param {number} courierId - Kurye ID
   */
  getCourierLocation: async (courierId) => {
    try {
      const response = await api.get(`${base}/${courierId}`);
      return response;
    } catch (error) {
      console.error(
        `[CourierLocationService] getCourierLocation(${courierId}) hatası:`,
        error,
      );
      throw error;
    }
  },

  /**
   * Kurye konumunu güncelle
   * Kurye panelinden Geolocation API ile gönderilir
   * @param {Object} locationData
   * @param {number} locationData.latitude - Enlem
   * @param {number} locationData.longitude - Boylam
   * @param {number} [locationData.accuracyMeters] - GPS doğruluğu
   * @param {number} [locationData.speedKmh] - Hız (km/s)
   * @param {number} [locationData.heading] - Yön (derece)
   */
  updateLocation: async (locationData) => {
    try {
      const response = await api.post(`${base}/update`, locationData);
      return response;
    } catch (error) {
      console.error("[CourierLocationService] updateLocation hatası:", error);
      throw error;
    }
  },

  /**
   * Kurye konum geçmişini getir
   * @param {number} courierId - Kurye ID
   * @param {Object} [params] - Sorgu parametreleri
   * @param {string} [params.startDate] - Başlangıç tarihi
   * @param {string} [params.endDate] - Bitiş tarihi
   * @param {number} [params.limit] - Kayıt sayısı limiti
   */
  getLocationHistory: async (courierId, params = {}) => {
    try {
      const response = await api.get(`${base}/${courierId}/history`, {
        params,
      });
      return response || [];
    } catch (error) {
      console.error(
        `[CourierLocationService] getLocationHistory(${courierId}) hatası:`,
        error,
      );
      throw error;
    }
  },

  /**
   * Yakındaki kuryeleri bul
   * @param {number} latitude - Merkez enlem
   * @param {number} longitude - Merkez boylam
   * @param {number} radiusKm - Yarıçap (km)
   */
  findNearbyCouriers: async (latitude, longitude, radiusKm = 5) => {
    try {
      const response = await api.get(`${base}/nearby`, {
        params: { latitude, longitude, radiusKm },
      });
      return response || [];
    } catch (error) {
      console.error(
        "[CourierLocationService] findNearbyCouriers hatası:",
        error,
      );
      throw error;
    }
  },

  /**
   * Teslimat bölgelerini getir
   * Haritada polygon olarak göstermek için
   */
  getDeliveryZones: async () => {
    try {
      const response = await api.get("/delivery-zones");
      return response || [];
    } catch (error) {
      console.error("[CourierLocationService] getDeliveryZones hatası:", error);
      return [];
    }
  },

  /**
   * Kurye ile teslimat noktası arasındaki mesafeyi hesapla
   * @param {number} courierLat - Kurye enlemi
   * @param {number} courierLng - Kurye boylamı
   * @param {number} deliveryLat - Teslimat enlemi
   * @param {number} deliveryLng - Teslimat boylamı
   * @returns {number} Mesafe (km)
   */
  calculateDistance: (courierLat, courierLng, deliveryLat, deliveryLng) => {
    const R = 6371; // Dünya yarıçapı (km)
    const dLat = ((deliveryLat - courierLat) * Math.PI) / 180;
    const dLon = ((deliveryLng - courierLng) * Math.PI) / 180;
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos((courierLat * Math.PI) / 180) *
        Math.cos((deliveryLat * Math.PI) / 180) *
        Math.sin(dLon / 2) *
        Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
  },
};

export default CourierLocationService;
