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
    id: courier.courierId ?? courier.id,
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

const normalizeCourierList = (payload) => {
  const list = Array.isArray(payload)
    ? payload
    : payload?.couriers || payload?.data?.couriers || payload?.data || [];

  return list.map(normalizeCourier).filter(Boolean);
};

/** Kurye panelinde gösterilmeyecek dahili/sistem not satırları */
const INTERNAL_NOTE_LINE_PATTERNS = [
  /^\[PREAUTH_EXPIRY_WARNING_SENT\]/i,
  /^\[Teslim Alma\]/i,
  /^\[Yola Çıkış\]/i,
  /^\[Teslim\]/i,
  /^\[Teslim Alan\]/i,
  /^\[PROBLEM/i,
  /^\[Fotoğraf\]/i,
  /^\[Kurye Notu\]/i,
  /^\[SİSTEM\]/i,
];

/**
 * DeliveryNotes alanından yalnızca müşterinin yazdığı notu ayıklar.
 * Sistem, kurye ve ödeme işaretlerini gizler.
 */
export const extractCustomerFacingNote = (rawNotes) => {
  if (!rawNotes || typeof rawNotes !== "string") return "";

  const customerParts = [];

  for (const line of rawNotes.split("\n")) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    if (INTERNAL_NOTE_LINE_PATTERNS.some((pattern) => pattern.test(trimmed))) {
      continue;
    }

    const cleaned = trimmed
      .replace(/\[PREAUTH_EXPIRY_WARNING_SENT\]\s*\S*/gi, "")
      .trim();
    if (!cleaned) continue;

    for (const segment of cleaned.split("|").map((part) => part.trim()).filter(Boolean)) {
      if (INTERNAL_NOTE_LINE_PATTERNS.some((pattern) => pattern.test(segment))) {
        continue;
      }

      const noteMatch = segment.match(/^Not:\s*(.+)$/i);
      if (noteMatch) {
        customerParts.push(noteMatch[1].trim());
        continue;
      }

      // Teslimat slotu gibi meta bilgileri müşteri notu olarak gösterme
      if (/^Slot:/i.test(segment)) continue;

      customerParts.push(segment);
    }
  }

  return [...new Set(customerParts)].join(" · ");
};

const parseCoordinates = (coordinates) => {
  if (!coordinates || typeof coordinates !== "string") return {};
  const parts = coordinates.split(",").map((p) => parseFloat(p.trim()));
  if (parts.length !== 2 || Number.isNaN(parts[0]) || Number.isNaN(parts[1])) {
    return {};
  }
  return { deliveryLatitude: parts[0], deliveryLongitude: parts[1] };
};

const pick = (obj, ...keys) => {
  for (const key of keys) {
    if (obj?.[key] !== undefined && obj?.[key] !== null) {
      return obj[key];
    }
  }
  return undefined;
};

const normalizeOrderItems = (items) => {
  const list = Array.isArray(items) ? items : [];
  return list
    .map((item) => {
      const quantity = Number(pick(item, "quantity", "Quantity") ?? 0);
      const unitPrice = Number(pick(item, "unitPrice", "UnitPrice", "price", "Price") ?? 0);
      const totalPrice = Number(
        pick(item, "totalPrice", "TotalPrice") ?? unitPrice * quantity,
      );
      const unit = pick(item, "unit", "Unit", "weightUnit", "WeightUnit") || "adet";

      return {
        id:
          pick(item, "orderItemId", "OrderItemId", "productId", "ProductId", "id", "Id") ??
          undefined,
        orderItemId:
          pick(item, "orderItemId", "OrderItemId", "productId", "ProductId") ?? undefined,
        name: pick(item, "productName", "ProductName", "name", "Name") || "Ürün",
        quantity,
        price: unitPrice,
        totalPrice,
        weightUnit: unit,
        isWeightBased:
          pick(item, "isWeightBased", "IsWeightBased") ??
          (String(unit).toLowerCase() === "gram" ||
            String(unit).toLowerCase() === "kilogram"),
        expectedWeightGrams: pick(
          item,
          "expectedWeightGrams",
          "ExpectedWeightGrams",
        ),
        actualWeightGrams: pick(item, "actualWeightGrams", "ActualWeightGrams"),
        weightDifferenceGrams: pick(
          item,
          "weightDifferenceGrams",
          "WeightDifferenceGrams",
        ),
        weightDifferenceAmount: pick(
          item,
          "weightDifferenceAmount",
          "WeightDifferenceAmount",
        ),
        hasWeightDifference:
          pick(item, "hasWeightDifference", "HasWeightDifference") ?? false,
      };
    })
    .filter((item) => item.name);
};

/** Backend renk kodlarını Bootstrap badge sınıfına çevirir */
export const mapCourierStatusColor = (colorOrStatus, status) => {
  const raw = String(colorOrStatus || status || "").toLowerCase();
  const colorMap = {
    yellow: "warning",
    blue: "primary",
    green: "success",
    red: "danger",
    orange: "warning",
    info: "info",
    gray: "secondary",
    grey: "secondary",
    preparing: "warning",
    ready: "info",
    assigned: "warning",
    picked_up: "info",
    pickedup: "info",
    out_for_delivery: "primary",
    outfordelivery: "primary",
    in_transit: "primary",
    delivered: "success",
    delivery_failed: "danger",
    deliveryfailed: "danger",
    deliverypaymentpending: "warning",
    delivery_payment_pending: "warning",
  };
  return colorMap[raw] || "secondary";
};

/** Türkiye telefon numarasını ekranda gösterim için normalize eder (0XXXXXXXXXX) */
export const formatPhoneDisplay = (phone) => {
  if (!phone) return "";
  let digits = String(phone).replace(/\D/g, "");
  if (digits.startsWith("90") && digits.length >= 12) {
    digits = digits.slice(2);
  }
  if (digits.length === 11 && digits.startsWith("0")) {
    return digits;
  }
  if (digits.length === 10 && digits.startsWith("5")) {
    return `0${digits}`;
  }
  if (digits.length === 9 && digits.startsWith("5")) {
    return `0${digits}`;
  }
  return digits || String(phone).trim();
};

/** Okunabilir format: 545 275 94 25 — satır kırılmasın diye nbsp kullanır */
export const formatPhoneReadable = (phone) => {
  let digits = formatPhoneDisplay(phone).replace(/\D/g, "");
  if (digits.startsWith("0")) {
    digits = digits.slice(1);
  }
  if (digits.length === 10 && digits.startsWith("5")) {
    const nbsp = "\u00A0";
    return `${digits.slice(0, 3)}${nbsp}${digits.slice(3, 6)}${nbsp}${digits.slice(6, 8)}${nbsp}${digits.slice(8)}`;
  }
  return formatPhoneDisplay(phone);
};

/** Mobil arama için tel: URI (+90 formatı) */
export const getPhoneTelUri = (phone) => {
  const display = formatPhoneDisplay(phone);
  if (!display) return "";
  const digits = display.replace(/\D/g, "");
  const national = digits.startsWith("0") ? digits.slice(1) : digits;
  if (national.length >= 10) {
    return `tel:+90${national}`;
  }
  return `tel:${display}`;
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
    finalAmount: order.finalAmount ?? order.totalAmount ?? order.finalPrice,
    totalPriceDifference: order.totalPriceDifference ?? 0,
    authorizedAmount: order.authorizedAmount ?? 0,
    weightAdjustmentStatus: order.weightAdjustmentStatus,
    hasWeightDifference: order.hasWeightDifference ?? false,
    hasWeightBasedItems: order.hasWeightBasedItems ?? false,
    allItemsWeighed: order.allItemsWeighed ?? false,
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
  const payload = order.data ?? order.order ?? order;
  const coords = parseCoordinates(
    pick(payload, "coordinates", "Coordinates"),
  );
  const rawItems =
    pick(payload, "items", "Items", "orderItems", "OrderItems") || [];

  return {
    id: pick(payload, "orderId", "OrderId", "id", "Id"),
    orderId: pick(payload, "orderId", "OrderId", "id", "Id"),
    orderNumber: pick(payload, "orderNumber", "OrderNumber"),
    status: pick(payload, "status", "Status"),
    statusText: pick(payload, "statusText", "StatusText"),
    customerName: pick(payload, "customerName", "CustomerName"),
    customerPhone: pick(payload, "customerPhone", "CustomerPhone"),
    customerEmail: pick(payload, "customerEmail", "CustomerEmail"),
    deliveryAddress: pick(payload, "fullAddress", "FullAddress", "deliveryAddress", "DeliveryAddress"),
    city: pick(payload, "city", "City"),
    googleMapsUrl: pick(payload, "googleMapsUrl", "GoogleMapsUrl"),
    orderTotal: pick(payload, "totalAmount", "TotalAmount"),
    finalAmount:
      pick(payload, "finalAmount", "FinalAmount") ??
      pick(payload, "totalAmount", "TotalAmount"),
    shippingCost: pick(payload, "shippingCost", "ShippingCost") ?? 0,
    totalPriceDifference:
      pick(payload, "totalPriceDifference", "TotalPriceDifference") ?? 0,
    authorizedAmount: pick(payload, "authorizedAmount", "AuthorizedAmount") ?? 0,
    weightAdjustmentStatus: pick(
      payload,
      "weightAdjustmentStatus",
      "WeightAdjustmentStatus",
    ),
    hasWeightDifference:
      pick(payload, "hasWeightDifference", "HasWeightDifference") ?? false,
    hasWeightBasedItems:
      pick(payload, "hasWeightBasedItems", "HasWeightBasedItems") ?? false,
    allItemsWeighed: pick(payload, "allItemsWeighed", "AllItemsWeighed") ?? false,
    paymentMethod: pick(payload, "paymentMethod", "PaymentMethod"),
    paymentStatus: pick(payload, "paymentStatus", "PaymentStatus"),
    paymentInfo: pick(payload, "paymentInfo", "PaymentInfo"),
    cashOnDeliveryAmount: pick(
      payload,
      "cashOnDeliveryAmount",
      "CashOnDeliveryAmount",
    ),
    orderDate: pick(payload, "orderDate", "OrderDate"),
    createdAt:
      pick(payload, "createdAt", "CreatedAt") ||
      pick(payload, "orderDate", "OrderDate"),
    assignedAt: pick(payload, "assignedAt", "AssignedAt"),
    pickedUpAt: pick(payload, "pickedUpAt", "PickedUpAt"),
    deliveredAt: pick(payload, "deliveredAt", "DeliveredAt"),
    estimatedDelivery: pick(payload, "estimatedDelivery", "EstimatedDelivery"),
    priority: pick(payload, "priority", "Priority"),
    notesForCourier: extractCustomerFacingNote(
      pick(payload, "deliveryNote", "DeliveryNote", "customerNote", "CustomerNote", "deliveryNotes", "DeliveryNotes"),
    ),
    requiredProofMethods: pick(
      payload,
      "requiredProofMethods",
      "RequiredProofMethods",
    ),
    allowedActions: pick(payload, "allowedActions", "AllowedActions"),
    items: normalizeOrderItems(rawItems),
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
  getAll: async () => {
    const res = await api.get(COURIER_BASE);
    return normalizeCourierList(res);
  },

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
  // Opsiyonel filtreler: { status, fromDate, toDate }
  // NEDEN obje parametresi: Backend CourierOrderFilterDto status/fromDate/toDate/page/pageSize
  //   destekliyor; tarih filtresi sipariş tarihine (OrderDate) göre çalışır. Filtre
  //   gönderilmezse backend yalnızca aktif siparişleri döner (mevcut davranış korunur).
  getAssignedOrders: async ({ status, fromDate, toDate } = {}) => {
    const params = [];
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    if (fromDate) params.push(`fromDate=${encodeURIComponent(fromDate)}`);
    if (toDate) params.push(`toDate=${encodeURIComponent(toDate)}`);
    const qs = params.length ? `?${params.join("&")}` : "";

    const res = await api.get(`${ORDERS_BASE}${qs}`);
    const orders = (res?.orders || res?.data?.orders || res?.Orders || [])
      .map(normalizeOrderListItem)
      .filter(Boolean);
    return {
      orders,
      summary: res?.summary || res?.data?.summary || res?.Summary,
      totalCount: res?.totalCount ?? res?.data?.totalCount ?? res?.TotalCount,
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

    // Teslim al (Assigned → PickedUp)
    if (normalized === "picked_up" || normalized === "pickedup") {
      return api.post(`${ORDERS_BASE}/${orderId}/pickup`, {
        note: notes,
      });
    }

    // Yola çık (PickedUp → OutForDelivery)
    return api.post(`${ORDERS_BASE}/${orderId}/start-delivery`, {
      note: notes,
    });
  },

  // Kurye aksiyonları (detay sayfası)
  updateTaskStatus: async (orderId, newStatus) => {
    const normalized = (newStatus || "").toLowerCase();

    // Teslim al (Assigned → PickedUp)
    if (normalized === "pickedup" || normalized === "picked_up") {
      return api.post(`${ORDERS_BASE}/${orderId}/pickup`, {});
    }

    // Yola çık (PickedUp → OutForDelivery)
    if (
      normalized === "outfordelivery" ||
      normalized === "out_for_delivery" ||
      normalized === "intransit" ||
      normalized === "in_transit"
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
