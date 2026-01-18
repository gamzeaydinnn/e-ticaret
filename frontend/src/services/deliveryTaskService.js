/**
 * Teslimat Görevi (DeliveryTask) API Servisi
 * 
 * Bu servis, teslimat görevleri için tüm CRUD ve iş mantığı operasyonlarını yönetir.
 * Backend DeliveryTaskController ile iletişim kurar.
 * 
 * Özellikler:
 * - Teslimat görevi CRUD işlemleri
 * - Kurye atama/değiştirme
 * - Durum güncellemeleri
 * - Proof of Delivery (POD) yönetimi
 * - Raporlama ve istatistikler
 */

import api from "./api";

// ============================================================================
// SABİTLER
// ============================================================================

/**
 * Teslimat durumu enum'u (Backend ile senkronize)
 * NEDEN: Frontend ve backend arasında tutarlı durum yönetimi
 */
export const DeliveryStatus = {
  CREATED: 0,       // Oluşturuldu
  ASSIGNED: 1,      // Kuryeye atandı
  ACCEPTED: 2,      // Kurye kabul etti
  PICKED_UP: 3,     // Teslim alındı
  IN_TRANSIT: 4,    // Yolda
  DELIVERED: 5,     // Teslim edildi
  FAILED: 6,        // Başarısız
  CANCELLED: 7      // İptal edildi
};

/**
 * Teslimat durumu label'ları (Türkçe)
 */
export const DeliveryStatusLabels = {
  [DeliveryStatus.CREATED]: "Oluşturuldu",
  [DeliveryStatus.ASSIGNED]: "Atandı",
  [DeliveryStatus.ACCEPTED]: "Kabul Edildi",
  [DeliveryStatus.PICKED_UP]: "Teslim Alındı",
  [DeliveryStatus.IN_TRANSIT]: "Yolda",
  [DeliveryStatus.DELIVERED]: "Teslim Edildi",
  [DeliveryStatus.FAILED]: "Başarısız",
  [DeliveryStatus.CANCELLED]: "İptal Edildi"
};

/**
 * Durum renklerine göre Bootstrap badge class'ları
 */
export const DeliveryStatusColors = {
  [DeliveryStatus.CREATED]: "secondary",
  [DeliveryStatus.ASSIGNED]: "info",
  [DeliveryStatus.ACCEPTED]: "primary",
  [DeliveryStatus.PICKED_UP]: "warning",
  [DeliveryStatus.IN_TRANSIT]: "warning",
  [DeliveryStatus.DELIVERED]: "success",
  [DeliveryStatus.FAILED]: "danger",
  [DeliveryStatus.CANCELLED]: "dark"
};

/**
 * Teslimat önceliği enum'u
 */
export const DeliveryPriority = {
  LOW: 0,
  NORMAL: 1,
  HIGH: 2,
  URGENT: 3
};

/**
 * Öncelik label'ları
 */
export const DeliveryPriorityLabels = {
  [DeliveryPriority.LOW]: "Düşük",
  [DeliveryPriority.NORMAL]: "Normal",
  [DeliveryPriority.HIGH]: "Yüksek",
  [DeliveryPriority.URGENT]: "Acil"
};

/**
 * Öncelik renkleri
 */
export const DeliveryPriorityColors = {
  [DeliveryPriority.LOW]: "secondary",
  [DeliveryPriority.NORMAL]: "primary",
  [DeliveryPriority.HIGH]: "warning",
  [DeliveryPriority.URGENT]: "danger"
};

// ============================================================================
// API ENDPOINT'LERİ
// ============================================================================

const BASE_URL = "/api/admin/delivery-tasks";
const COURIER_BASE_URL = "/api/courier/deliveries";

// ============================================================================
// MOCK VERİLER (Backend hazır olana kadar geliştirme için)
// ============================================================================

const USE_MOCK_DATA = false; // Gerçek API kullan

const mockDeliveryTasks = [
  {
    id: 1,
    orderId: 101,
    orderNumber: "ORD-2026-0101",
    customerName: "Ahmet Yılmaz",
    customerPhone: "+90 532 123 4567",
    pickupAddress: "Merkez Depo - Kadıköy, İstanbul",
    pickupLatitude: 40.9908,
    pickupLongitude: 29.0230,
    dropoffAddress: "Atatürk Cad. No: 45/3 Maltepe, İstanbul",
    dropoffLatitude: 40.9356,
    dropoffLongitude: 29.1283,
    status: DeliveryStatus.ASSIGNED,
    priority: DeliveryPriority.NORMAL,
    codAmount: 125.50,
    notesForCourier: "Kapıda ödeme alınacak. 3. kat, zil çalışmıyor.",
    timeWindowStart: new Date(Date.now() + 2 * 60 * 60 * 1000).toISOString(),
    timeWindowEnd: new Date(Date.now() + 4 * 60 * 60 * 1000).toISOString(),
    courierId: 1,
    courierName: "Mehmet Demir",
    courierPhone: "+90 535 987 6543",
    estimatedDeliveryTime: new Date(Date.now() + 45 * 60 * 1000).toISOString(),
    createdAt: new Date(Date.now() - 30 * 60 * 1000).toISOString(),
    assignedAt: new Date(Date.now() - 15 * 60 * 1000).toISOString(),
    items: [
      { name: "Organik Domates", quantity: 2, unit: "kg" },
      { name: "Taze Ekmek", quantity: 3, unit: "adet" },
      { name: "Süt 1L", quantity: 2, unit: "adet" }
    ]
  },
  {
    id: 2,
    orderId: 102,
    orderNumber: "ORD-2026-0102",
    customerName: "Fatma Özkan",
    customerPhone: "+90 534 555 1234",
    pickupAddress: "Merkez Depo - Kadıköy, İstanbul",
    pickupLatitude: 40.9908,
    pickupLongitude: 29.0230,
    dropoffAddress: "Bağdat Cad. No: 123/7 Kadıköy, İstanbul",
    dropoffLatitude: 40.9676,
    dropoffLongitude: 29.0638,
    status: DeliveryStatus.CREATED,
    priority: DeliveryPriority.HIGH,
    codAmount: 0,
    notesForCourier: "Online ödeme yapıldı.",
    timeWindowStart: new Date(Date.now() + 1 * 60 * 60 * 1000).toISOString(),
    timeWindowEnd: new Date(Date.now() + 3 * 60 * 60 * 1000).toISOString(),
    courierId: null,
    courierName: null,
    courierPhone: null,
    estimatedDeliveryTime: null,
    createdAt: new Date(Date.now() - 10 * 60 * 1000).toISOString(),
    assignedAt: null,
    items: [
      { name: "Et 500g", quantity: 1, unit: "paket" },
      { name: "Patates", quantity: 2, unit: "kg" }
    ]
  },
  {
    id: 3,
    orderId: 103,
    orderNumber: "ORD-2026-0103",
    customerName: "Ali Kaya",
    customerPhone: "+90 533 444 5678",
    pickupAddress: "Merkez Depo - Kadıköy, İstanbul",
    pickupLatitude: 40.9908,
    pickupLongitude: 29.0230,
    dropoffAddress: "Fenerbahçe Mahallesi, Kadıköy, İstanbul",
    dropoffLatitude: 40.9723,
    dropoffLongitude: 29.0366,
    status: DeliveryStatus.IN_TRANSIT,
    priority: DeliveryPriority.URGENT,
    codAmount: 85.00,
    notesForCourier: "Acil teslimat, müşteri bekliyor!",
    timeWindowStart: new Date(Date.now() - 30 * 60 * 1000).toISOString(),
    timeWindowEnd: new Date(Date.now() + 30 * 60 * 1000).toISOString(),
    courierId: 2,
    courierName: "Ayşe Yıldız",
    courierPhone: "+90 536 222 3333",
    estimatedDeliveryTime: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
    createdAt: new Date(Date.now() - 60 * 60 * 1000).toISOString(),
    assignedAt: new Date(Date.now() - 45 * 60 * 1000).toISOString(),
    pickedUpAt: new Date(Date.now() - 20 * 60 * 1000).toISOString(),
    items: [
      { name: "Meyve Sepeti", quantity: 1, unit: "adet" }
    ]
  }
];

const mockCouriersWithScores = [
  {
    id: 1,
    name: "Mehmet Demir",
    phone: "+90 535 987 6543",
    isOnline: true,
    currentCapacity: 2,
    maxCapacity: 5,
    lastLocationLat: 40.9856,
    lastLocationLng: 29.0325,
    distanceKm: 2.3,
    rating: 4.8,
    completedToday: 12,
    assignmentScore: 92,
    vehicle: "Motosiklet",
    zones: ["Kadıköy", "Maltepe", "Ataşehir"]
  },
  {
    id: 2,
    name: "Ayşe Yıldız",
    phone: "+90 536 222 3333",
    isOnline: true,
    currentCapacity: 4,
    maxCapacity: 5,
    lastLocationLat: 40.9723,
    lastLocationLng: 29.0366,
    distanceKm: 4.1,
    rating: 4.6,
    completedToday: 8,
    assignmentScore: 78,
    vehicle: "Motosiklet",
    zones: ["Kadıköy", "Beşiktaş"]
  },
  {
    id: 3,
    name: "Emre Aksoy",
    phone: "+90 537 111 2222",
    isOnline: false,
    currentCapacity: 0,
    maxCapacity: 5,
    lastLocationLat: null,
    lastLocationLng: null,
    distanceKm: null,
    rating: 4.9,
    completedToday: 0,
    assignmentScore: 0,
    vehicle: "Bisiklet",
    zones: ["Beşiktaş", "Şişli"]
  }
];

// ============================================================================
// SERVİS FONKSİYONLARI
// ============================================================================

export const DeliveryTaskService = {
  // =========================================================================
  // ADMIN FONKSİYONLARI
  // =========================================================================

  /**
   * Tüm teslimat görevlerini listeler
   * @param {Object} filters - Filtreleme parametreleri (status, courierId, dateFrom, dateTo)
   */
  getAll: async (filters = {}) => {
    if (USE_MOCK_DATA) {
      let result = [...mockDeliveryTasks];
      
      // Durum filtresi
      if (filters.status !== undefined && filters.status !== null) {
        result = result.filter(t => t.status === filters.status);
      }
      
      // Kurye filtresi
      if (filters.courierId) {
        result = result.filter(t => t.courierId === filters.courierId);
      }
      
      return Promise.resolve(result);
    }

    const params = new URLSearchParams();
    if (filters.status !== undefined) params.append("status", filters.status);
    if (filters.courierId) params.append("courierId", filters.courierId);
    if (filters.dateFrom) params.append("dateFrom", filters.dateFrom);
    if (filters.dateTo) params.append("dateTo", filters.dateTo);

    return api.get(`${BASE_URL}?${params.toString()}`);
  },

  /**
   * Tek bir teslimat görevini getirir
   * @param {number} id - Teslimat görevi ID
   */
  getById: async (id) => {
    if (USE_MOCK_DATA) {
      const task = mockDeliveryTasks.find(t => t.id === id);
      if (!task) return Promise.reject({ message: "Teslimat görevi bulunamadı" });
      return Promise.resolve(task);
    }

    return api.get(`${BASE_URL}/${id}`);
  },

  /**
   * Siparişten teslimat görevi oluşturur
   * @param {number} orderId - Sipariş ID
   * @param {Object} options - Ek seçenekler (priority, notesForCourier, timeWindow)
   */
  createFromOrder: async (orderId, options = {}) => {
    if (USE_MOCK_DATA) {
      const newTask = {
        id: mockDeliveryTasks.length + 1,
        orderId,
        orderNumber: `ORD-2026-0${orderId}`,
        status: DeliveryStatus.CREATED,
        priority: options.priority || DeliveryPriority.NORMAL,
        notesForCourier: options.notesForCourier || "",
        createdAt: new Date().toISOString(),
        ...options
      };
      mockDeliveryTasks.push(newTask);
      return Promise.resolve(newTask);
    }

    return api.post(`${BASE_URL}/from-order/${orderId}`, options);
  },

  /**
   * Teslimat görevi günceller
   * @param {number} id - Teslimat görevi ID
   * @param {Object} data - Güncellenecek veriler
   */
  update: async (id, data) => {
    if (USE_MOCK_DATA) {
      const index = mockDeliveryTasks.findIndex(t => t.id === id);
      if (index === -1) return Promise.reject({ message: "Teslimat görevi bulunamadı" });
      mockDeliveryTasks[index] = { ...mockDeliveryTasks[index], ...data };
      return Promise.resolve(mockDeliveryTasks[index]);
    }

    return api.put(`${BASE_URL}/${id}`, data);
  },

  /**
   * Kuryeye görev atar
   * @param {number} taskId - Teslimat görevi ID
   * @param {number} courierId - Kurye ID
   */
  assignCourier: async (taskId, courierId) => {
    if (USE_MOCK_DATA) {
      const task = mockDeliveryTasks.find(t => t.id === taskId);
      const courier = mockCouriersWithScores.find(c => c.id === courierId);
      
      if (!task) return Promise.reject({ message: "Teslimat görevi bulunamadı" });
      if (!courier) return Promise.reject({ message: "Kurye bulunamadı" });
      if (!courier.isOnline) return Promise.reject({ message: "Kurye çevrimdışı" });
      
      task.courierId = courierId;
      task.courierName = courier.name;
      task.courierPhone = courier.phone;
      task.status = DeliveryStatus.ASSIGNED;
      task.assignedAt = new Date().toISOString();
      
      return Promise.resolve(task);
    }

    return api.post(`${BASE_URL}/${taskId}/assign`, { courierId });
  },

  /**
   * Kurye değiştirir (reassign)
   * @param {number} taskId - Teslimat görevi ID
   * @param {number} newCourierId - Yeni kurye ID
   * @param {string} reason - Değiştirme nedeni
   */
  reassignCourier: async (taskId, newCourierId, reason = "") => {
    if (USE_MOCK_DATA) {
      const task = mockDeliveryTasks.find(t => t.id === taskId);
      const courier = mockCouriersWithScores.find(c => c.id === newCourierId);
      
      if (!task) return Promise.reject({ message: "Teslimat görevi bulunamadı" });
      if (!courier) return Promise.reject({ message: "Kurye bulunamadı" });
      
      task.courierId = newCourierId;
      task.courierName = courier.name;
      task.courierPhone = courier.phone;
      task.status = DeliveryStatus.ASSIGNED;
      task.reassignedAt = new Date().toISOString();
      task.reassignReason = reason;
      
      return Promise.resolve(task);
    }

    return api.post(`${BASE_URL}/${taskId}/reassign`, { newCourierId, reason });
  },

  /**
   * Teslimat görevini iptal eder
   * @param {number} taskId - Teslimat görevi ID
   * @param {string} reason - İptal nedeni
   */
  cancel: async (taskId, reason = "") => {
    if (USE_MOCK_DATA) {
      const task = mockDeliveryTasks.find(t => t.id === taskId);
      if (!task) return Promise.reject({ message: "Teslimat görevi bulunamadı" });
      
      task.status = DeliveryStatus.CANCELLED;
      task.cancelledAt = new Date().toISOString();
      task.cancelReason = reason;
      
      return Promise.resolve(task);
    }

    return api.post(`${BASE_URL}/${taskId}/cancel`, { reason });
  },

  /**
   * Atama için uygun kuryeleri listeler (skor hesaplaması ile)
   * @param {number} taskId - Teslimat görevi ID (mesafe hesabı için)
   */
  getAvailableCouriers: async (taskId) => {
    if (USE_MOCK_DATA) {
      // Online olan ve kapasitesi dolu olmayan kuryeler
      const available = mockCouriersWithScores
        .filter(c => c.isOnline && c.currentCapacity < c.maxCapacity)
        .sort((a, b) => b.assignmentScore - a.assignmentScore);
      
      return Promise.resolve(available);
    }

    return api.get(`${BASE_URL}/${taskId}/available-couriers`);
  },

  /**
   * Teslimat istatistiklerini getirir
   */
  getStatistics: async (dateFrom, dateTo) => {
    if (USE_MOCK_DATA) {
      return Promise.resolve({
        totalTasks: mockDeliveryTasks.length,
        pending: mockDeliveryTasks.filter(t => t.status === DeliveryStatus.CREATED).length,
        assigned: mockDeliveryTasks.filter(t => t.status === DeliveryStatus.ASSIGNED).length,
        inProgress: mockDeliveryTasks.filter(t => 
          [DeliveryStatus.ACCEPTED, DeliveryStatus.PICKED_UP, DeliveryStatus.IN_TRANSIT].includes(t.status)
        ).length,
        delivered: mockDeliveryTasks.filter(t => t.status === DeliveryStatus.DELIVERED).length,
        failed: mockDeliveryTasks.filter(t => t.status === DeliveryStatus.FAILED).length,
        cancelled: mockDeliveryTasks.filter(t => t.status === DeliveryStatus.CANCELLED).length,
        averageDeliveryTime: 35, // dakika
        onTimeRate: 92.5 // yüzde
      });
    }

    const params = new URLSearchParams();
    if (dateFrom) params.append("dateFrom", dateFrom);
    if (dateTo) params.append("dateTo", dateTo);

    return api.get(`${BASE_URL}/statistics?${params.toString()}`);
  },

  /**
   * Teslimat timeline'ını getirir (event geçmişi)
   * @param {number} taskId - Teslimat görevi ID
   */
  getTimeline: async (taskId) => {
    if (USE_MOCK_DATA) {
      const task = mockDeliveryTasks.find(t => t.id === taskId);
      if (!task) return Promise.resolve([]);

      const timeline = [
        {
          id: 1,
          eventType: "CREATED",
          description: "Teslimat görevi oluşturuldu",
          actorType: "SYSTEM",
          actorName: "Sistem",
          timestamp: task.createdAt
        }
      ];

      if (task.assignedAt) {
        timeline.push({
          id: 2,
          eventType: "ASSIGNED",
          description: `${task.courierName} kuryesine atandı`,
          actorType: "ADMIN",
          actorName: "Admin",
          timestamp: task.assignedAt
        });
      }

      if (task.pickedUpAt) {
        timeline.push({
          id: 3,
          eventType: "PICKED_UP",
          description: "Kurye siparişi teslim aldı",
          actorType: "COURIER",
          actorName: task.courierName,
          timestamp: task.pickedUpAt
        });
      }

      return Promise.resolve(timeline);
    }

    return api.get(`${BASE_URL}/${taskId}/timeline`);
  },

  /**
   * POD (Proof of Delivery) bilgilerini getirir
   * @param {number} taskId - Teslimat görevi ID
   */
  getPOD: async (taskId) => {
    if (USE_MOCK_DATA) {
      // Teslim edilmiş görevler için mock POD
      const task = mockDeliveryTasks.find(t => t.id === taskId);
      if (!task || task.status !== DeliveryStatus.DELIVERED) {
        return Promise.resolve(null);
      }

      return Promise.resolve({
        id: 1,
        deliveryTaskId: taskId,
        method: "PHOTO",
        photoUrl: "/uploads/pod/sample-pod.jpg",
        otpCode: null,
        signatureUrl: null,
        capturedAt: task.deliveredAt,
        capturedByName: task.courierName,
        recipientName: task.customerName,
        notes: "Kapıda teslim edildi"
      });
    }

    return api.get(`${BASE_URL}/${taskId}/pod`);
  },

  // =========================================================================
  // KURYE FONKSİYONLARI
  // =========================================================================

  /**
   * Kuryenin atanmış görevlerini listeler
   */
  getCourierTasks: async () => {
    if (USE_MOCK_DATA) {
      // Mock olarak kurye 1'in görevleri
      return Promise.resolve(mockDeliveryTasks.filter(t => t.courierId === 1));
    }

    return api.get(COURIER_BASE_URL);
  },

  /**
   * Kurye görevi kabul eder
   * @param {number} taskId - Teslimat görevi ID
   */
  acceptTask: async (taskId) => {
    if (USE_MOCK_DATA) {
      const task = mockDeliveryTasks.find(t => t.id === taskId);
      if (!task) return Promise.reject({ message: "Görev bulunamadı" });
      
      task.status = DeliveryStatus.ACCEPTED;
      task.acceptedAt = new Date().toISOString();
      
      return Promise.resolve(task);
    }

    return api.post(`${COURIER_BASE_URL}/${taskId}/accept`);
  },

  /**
   * Kurye siparişi teslim aldı
   * @param {number} taskId - Teslimat görevi ID
   */
  pickUp: async (taskId) => {
    if (USE_MOCK_DATA) {
      const task = mockDeliveryTasks.find(t => t.id === taskId);
      if (!task) return Promise.reject({ message: "Görev bulunamadı" });
      
      task.status = DeliveryStatus.PICKED_UP;
      task.pickedUpAt = new Date().toISOString();
      
      return Promise.resolve(task);
    }

    return api.post(`${COURIER_BASE_URL}/${taskId}/pickup`);
  },

  /**
   * Kurye yolda
   * @param {number} taskId - Teslimat görevi ID
   */
  startTransit: async (taskId) => {
    if (USE_MOCK_DATA) {
      const task = mockDeliveryTasks.find(t => t.id === taskId);
      if (!task) return Promise.reject({ message: "Görev bulunamadı" });
      
      task.status = DeliveryStatus.IN_TRANSIT;
      task.transitStartedAt = new Date().toISOString();
      
      return Promise.resolve(task);
    }

    return api.post(`${COURIER_BASE_URL}/${taskId}/start-transit`);
  },

  /**
   * Teslimat tamamlandı
   * @param {number} taskId - Teslimat görevi ID
   * @param {Object} pod - Proof of Delivery verileri
   */
  complete: async (taskId, pod = {}) => {
    if (USE_MOCK_DATA) {
      const task = mockDeliveryTasks.find(t => t.id === taskId);
      if (!task) return Promise.reject({ message: "Görev bulunamadı" });
      
      task.status = DeliveryStatus.DELIVERED;
      task.deliveredAt = new Date().toISOString();
      task.pod = pod;
      
      return Promise.resolve(task);
    }

    return api.post(`${COURIER_BASE_URL}/${taskId}/complete`, pod);
  },

  /**
   * Teslimat başarısız
   * @param {number} taskId - Teslimat görevi ID
   * @param {Object} failure - Başarısızlık detayları
   */
  fail: async (taskId, failure) => {
    if (USE_MOCK_DATA) {
      const task = mockDeliveryTasks.find(t => t.id === taskId);
      if (!task) return Promise.reject({ message: "Görev bulunamadı" });
      
      task.status = DeliveryStatus.FAILED;
      task.failedAt = new Date().toISOString();
      task.failureReason = failure.reasonCode;
      task.failureNote = failure.note;
      
      return Promise.resolve(task);
    }

    return api.post(`${COURIER_BASE_URL}/${taskId}/fail`, failure);
  }
};

// Default export
export default DeliveryTaskService;
