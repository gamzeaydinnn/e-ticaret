import api from "./api";

const AUTH_BASE = "/api/courier/auth";
const COURIER_BASE = "/api/courier";
const ORDERS_BASE = "/api/courier/orders";

const normalizeCourier = (courier) => {
  if (!courier) return null;
  const fullName = courier.fullName || courier.name || "";
  const firstName = courier.firstName || fullName.split(" ")[0] || "";
  const lastName = courier.lastName || fullName.split(" ").slice(1).join(" ");

  return {
    id: courier.courierId || courier.id,
    userId: courier.userId,
    name: fullName || `${firstName} ${lastName}`.trim(),
    email: courier.email,
    phone: courier.phone,
    vehicle: courier.vehicle,
    status: courier.status,
    location: courier.location,
    rating: courier.rating,
    activeOrders: courier.activeOrders,
    completedToday: courier.completedToday,
    isOnline: courier.status === "active" || courier.status === "online",
  };
};

const parseCoordinates = (coordinates) => {
  if (!coordinates || typeof coordinates !== "string") return {};
  const parts = coordinates.split(",").map((p) => parseFloat(p.trim()));
  if (parts.length !== 2 || Number.isNaN(parts[0]) || Number.isNaN(parts[1])) {
    return {};
  }
  return { deliveryLatitude: parts[0], deliveryLongitude: parts[1] };
};

const normalizeOrderListItem = (order) => {
  if (!order) return null;
  return {
    id: order.orderId ?? order.id,
    orderNumber: order.orderNumber,
    customerName: order.customerName,
    customerPhone: order.customerPhone,
    address: order.addressSummary || order.fullAddress || order.shippingAddress,
    totalAmount: order.totalAmount ?? order.finalPrice,
    status: order.status,
    statusText: order.statusText,
    statusColor: order.statusColor,
    paymentMethod: order.paymentMethod,
    paymentStatus: order.paymentStatus,
    priority: order.priority,
    orderTime: order.orderDate,
    assignedAt: order.assignedAt,
    estimatedDelivery: order.estimatedDelivery,
    itemCount: order.itemCount,
  };
};

const normalizeOrderDetail = (order) => {
  if (!order) return null;
  const coords = parseCoordinates(order.coordinates);

  return {
    id: order.orderId ?? order.id,
    orderId: order.orderId ?? order.id,
    orderNumber: order.orderNumber,
    status: order.status,
    statusText: order.statusText,
    customerName: order.customerName,
    customerPhone: order.customerPhone,
    customerEmail: order.customerEmail,
    deliveryAddress: order.fullAddress,
    city: order.city,
    googleMapsUrl: order.googleMapsUrl,
    orderTotal: order.totalAmount,
    paymentMethod: order.paymentMethod,
    paymentStatus: order.paymentStatus,
    paymentInfo: order.paymentInfo,
    cashOnDeliveryAmount: order.cashOnDeliveryAmount,
    orderDate: order.orderDate,
    assignedAt: order.assignedAt,
    pickedUpAt: order.pickedUpAt,
    deliveredAt: order.deliveredAt,
    estimatedDelivery: order.estimatedDelivery,
    priority: order.priority,
    notesForCourier: order.deliveryNote || order.customerNote,
    requiredProofMethods: order.requiredProofMethods,
    allowedActions: order.allowedActions,
    items: (order.items || []).map((item) => ({
      id: item.orderItemId || item.productId,
      orderItemId: item.orderItemId || item.productId,
      name: item.productName,
      quantity: item.quantity,
      price: item.unitPrice,
      totalPrice: item.totalPrice,
      weightUnit: item.unit,
      isWeightBased:
        item.unit?.toLowerCase() === "gram" ||
        item.unit?.toLowerCase() === "kilogram",
      expectedWeightGrams: item.expectedWeightGrams,
      actualWeightGrams: item.actualWeightGrams,
    })),
    ...coords,
  };
};

const mapFailureReasonToEnum = (reasonCode) => {
  const map = {
    customer_not_available: 1, // CustomerNotAvailable
    wrong_address: 2, // AddressNotFound
    access_denied: 3, // AccessDenied
    customer_rejected: 4, // RefusedByCustomer
    damaged_package: 5, // DamagedPackage
    payment_issue: 6, // PaymentIssue
    weather_conditions: 7, // WeatherConditions
    vehicle_issue: 8, // VehicleBreakdown
    other: 99, // Other
  };
  return map[reasonCode] ?? 99;
};

export const CourierService = {
  // Admin - Tüm kuryeleri listele
  getAll: () => api.get(COURIER_BASE),

  // ============================================================
  // KURYE AUTH İŞLEMLERİ
  // ============================================================

  // Kurye giriş
  login: async (emailOrPhone, password, rememberMe = false) => {
    const payload = {
      email: emailOrPhone,
      password,
      rememberMe,
    };
    const res = await api.post(`${AUTH_BASE}/login`, payload);

    if (res?.success) {
      return {
        success: true,
        token: res.accessToken,
        refreshToken: res.refreshToken,
        courier: normalizeCourier(res.courier),
        message: res.message,
      };
    }

    return {
      success: false,
      error: res?.message || "Giriş başarısız",
    };
  },

  // Token yenileme
  refreshToken: async (token, refreshToken) => {
    const res = await api.post(`${AUTH_BASE}/refresh`, {
      accessToken: token,
      refreshToken,
    });

    return {
      success: !!res?.success,
      token: res?.accessToken,
      refreshToken: res?.refreshToken,
      message: res?.message,
    };
  },

  // Çıkış (token invalidate)
  logout: () => api.post(`${AUTH_BASE}/logout`),

  // Şifre sıfırlama isteği
  requestPasswordReset: (email) =>
    api.post(`${AUTH_BASE}/password-reset-request`, { email }),

  // Şifre değiştirme
  changePassword: (currentPassword, newPassword) =>
    api.post(`${AUTH_BASE}/change-password`, { currentPassword, newPassword }),

  // Mevcut kurye bilgisi
  getMe: async () => {
    const res = await api.get(`${AUTH_BASE}/me`);
    return {
      success: !!res?.success,
      courier: normalizeCourier(res?.courier || res?.Courier),
    };
  },

  // Online durum güncelleme
  updateOnlineStatus: () =>
    Promise.resolve({
      success: false,
      error: "Online durum güncelleme endpoint'i tanımlı değil",
    }),

  // ============================================================
  // SİPARİŞ İŞLEMLERİ
  // ============================================================

  // Kurye siparişlerini listele
  getAssignedOrders: async () => {
    const res = await api.get(ORDERS_BASE);
    const orders = (res?.orders || [])
      .map(normalizeOrderListItem)
      .filter(Boolean);
    return {
      orders,
      summary: res?.summary,
      totalCount: res?.totalCount,
    };
  },

  // Sipariş detayı
  getTaskDetail: async (orderId) => {
    const res = await api.get(`${ORDERS_BASE}/${orderId}`);
    return normalizeOrderDetail(res);
  },

  // Sipariş durumunu güncelle (iş kuralları backend'de)
  updateOrderStatus: async (orderId, status, notes = "") => {
    const normalized = (status || "").toLowerCase();

    if (
      normalized === "delivered" ||
      normalized === "teslim" ||
      normalized === "completed"
    ) {
      return api.post(`${ORDERS_BASE}/${orderId}/delivered`, { note: notes });
    }

    if (
      normalized === "failed" ||
      normalized === "delivery_failed" ||
      normalized === "problem"
    ) {
      return api.post(`${ORDERS_BASE}/${orderId}/problem`, {
        reason: 99,
        description: notes || "Kurye tarafından problem bildirildi",
        attemptedToContactCustomer: true,
      });
    }

    // varsayılan: yola çıktı
    return api.post(`${ORDERS_BASE}/${orderId}/start-delivery`, {
      note: notes,
    });
  },

  // Kurye aksiyonları (detay sayfası)
  updateTaskStatus: async (orderId, newStatus) => {
    const normalized = (newStatus || "").toLowerCase();
    if (
      normalized === "outfordelivery" ||
      normalized === "out_for_delivery" ||
      normalized === "intransit" ||
      normalized === "in_transit" ||
      normalized === "pickedup" ||
      normalized === "picked_up"
    ) {
      return api.post(`${ORDERS_BASE}/${orderId}/start-delivery`, {});
    }
    if (normalized === "delivered") {
      return api.post(`${ORDERS_BASE}/${orderId}/delivered`, {});
    }
    if (normalized === "failed" || normalized === "delivery_failed") {
      return api.post(`${ORDERS_BASE}/${orderId}/problem`, {
        reason: 99,
        description: "Kurye tarafından başarısız olarak bildirildi",
        attemptedToContactCustomer: true,
      });
    }
    return api.post(`${ORDERS_BASE}/${orderId}/start-delivery`, {});
  },

  submitProofOfDelivery: async (orderId, podData) => {
    return api.post(`${ORDERS_BASE}/${orderId}/delivered`, {
      note: podData?.notes,
      photoUrl: podData?.photoBase64,
    });
  },

  submitDeliveryFailure: async (orderId, failureData) => {
    return api.post(`${ORDERS_BASE}/${orderId}/problem`, {
      reason: mapFailureReasonToEnum(failureData?.reasonCode),
      description: failureData?.additionalNotes,
      photoUrl: failureData?.photoBase64,
      attemptedToContactCustomer: !!failureData?.attemptedDelivery,
      callAttempts: failureData?.attemptedDelivery ? 1 : 0,
    });
  },

  // Sipariş ağırlık raporlarını getir
  getOrderWeightReports: (orderId) =>
    api.get(`${COURIER_BASE}/orders/${orderId}/weight-reports`),

  // ============================================================
  // KURYE CRUD İŞLEMLERİ (Admin için)
  // ============================================================

  // Yeni kurye oluştur
  createCourier: (courierData) => api.post(COURIER_BASE, courierData),

  // Kurye güncelle
  updateCourier: (courierId, courierData) =>
    api.put(`${COURIER_BASE}/${courierId}`, courierData),

  // Kurye sil
  deleteCourier: (courierId) => api.delete(`${COURIER_BASE}/${courierId}`),

  // Kurye şifresini sıfırla
  resetPassword: (courierId, newPassword) =>
    api.post(`${COURIER_BASE}/${courierId}/reset-password`, { newPassword }),

  // ============================================================
  // MVP KURYE HIZLI AKSİYONLARI
  // ============================================================

  // Tek tuşla "Yola Çık" (Assigned → OutForDelivery)
  startDelivery: (orderId) =>
    api.post(`${ORDERS_BASE}/${orderId}/start-delivery`, {}),

  // Tek tuşla "Teslim Et" (OutForDelivery → Delivered)
  markDelivered: (orderId, notes = "") =>
    api.post(`${ORDERS_BASE}/${orderId}/delivered`, { note: notes }),

  // Mevcut metotlar korunuyor
  getById: (id) => api.get(`${COURIER_BASE}/${id}`),
  add: (courier) => api.post(COURIER_BASE, courier),
  update: (id, courier) => api.put(`${COURIER_BASE}/${id}`, courier),
  remove: (id) => api.delete(`${COURIER_BASE}/${id}`),
  myOrders: () => api.get(`${ORDERS_BASE}`),
  updateStatus: (orderId, status) =>
    api.post(`${ORDERS_BASE}/${orderId}/status`, { status }),
};
