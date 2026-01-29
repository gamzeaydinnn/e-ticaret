import api from "./api";

const base = "/api/orders";

/**
 * Tekrar sipariş oluşturulmasını engellemek için benzersiz client order ID üretir
 * UUID v4 formatında GUID üretir, desteklenmiyorsa fallback kullanır
 */
const generateClientOrderId = () => {
  try {
    if (
      typeof crypto !== "undefined" &&
      typeof crypto.randomUUID === "function"
    ) {
      return crypto.randomUUID();
    }
  } catch {
    // no-op, fallback below
  }
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
};

/**
 * Backend'den gelen sipariş verisini normalize eder
 * Farklı response formatlarını standart bir yapıya dönüştürür
 * @param {Object} order - Backend'den gelen ham sipariş verisi
 * @returns {Object} Normalize edilmiş sipariş objesi
 */
const normalizeOrder = (order = {}) => {
  const itemsSource = Array.isArray(order.orderItems)
    ? order.orderItems
    : Array.isArray(order.items)
      ? order.items
      : [];

  const normalizedItems = itemsSource.map((item, index) => ({
    id: item.id ?? item.productId ?? index,
    productId: item.productId ?? item.id ?? index,
    name: item.productName || item.name || "Ürün",
    quantity: item.quantity ?? 0,
    unitPrice: item.unitPrice ?? item.price ?? 0,
    price: item.unitPrice ?? item.price ?? 0,
  }));

  const fallbackOrderNo = order.id ? `ORD-${order.id}` : "ORD-0000";

  return {
    id: order.id ?? order.orderId ?? null,
    orderNumber: order.orderNumber || fallbackOrderNo,
    // Yeni backend alanı: trackingNumber. Eski client kodları için trackingCode de bırakılıyor.
    trackingNumber:
      order.trackingNumber ||
      order.trackingCode ||
      order.orderNumber ||
      fallbackOrderNo,
    trackingCode:
      order.trackingNumber ||
      order.trackingCode ||
      order.orderNumber ||
      fallbackOrderNo,
    status: (order.status || "").toLowerCase(),
    orderDate: order.orderDate || new Date().toISOString(),
    totalAmount: order.totalAmount ?? order.totalPrice ?? 0,
    totalPrice: order.totalPrice ?? order.totalAmount ?? 0,
    finalPrice: order.finalPrice ?? order.totalPrice ?? order.totalAmount ?? 0,
    discountAmount: order.discountAmount ?? 0,
    couponDiscountAmount: order.couponDiscountAmount ?? 0,
    campaignDiscountAmount: order.campaignDiscountAmount ?? 0,
    couponCode:
      order.couponCode ||
      order.appliedCouponCode ||
      order.raw?.couponCode ||
      null,
    isGuestOrder: Boolean(order.isGuestOrder),
    customerName: order.customerName || "",
    customerPhone: order.customerPhone || "",
    deliveryAddress:
      order.deliveryAddress ||
      order.shippingAddress ||
      order.address ||
      order.fullAddress ||
      order.addressSummary ||
      "Adres belirtilmedi",
    shippingCompany: order.shippingCompany || order.shippingMethod || "",
    estimatedDeliveryDate: order.estimatedDeliveryDate || null,
    shippingMethod: order.shippingMethod || "",
    shippingCost: order.shippingCost ?? 0,
    items: normalizedItems,
    orderItems: normalizedItems,
    raw: order,
  };
};

const normalizeList = (payload) =>
  Array.isArray(payload) ? payload.map(normalizeOrder) : [];

export const OrderService = {
  create: async (payload) => {
    const finalPayload = {
      ...payload,
      clientOrderId: payload?.clientOrderId || generateClientOrderId(),
    };

    try {
      const data = await api.post(base, finalPayload);
      return normalizeOrder(data);
    } catch (error) {
      if (error.status === 409 && error.raw?.response?.data) {
        const existing = error.raw.response.data;
        // Beklenen şema: doğrudan sipariş DTO'su veya { order: dto }
        const orderPayload = existing.order || existing;
        return normalizeOrder(orderPayload);
      }
      throw error;
    }
  },
  list: (userId) =>
    api
      .get(base, { params: userId ? { userId } : undefined })
      .then(normalizeList),
  getById: (id) => api.get(`${base}/${id}`).then(normalizeOrder),
  updateStatus: (id, status) => api.patch(`${base}/${id}/status`, { status }),
  cancel: (id) => api.post(`${base}/${id}/cancel`),
  checkout: async (payload) => {
    // GUID format validation: must be valid UUID v4 format
    // Valid format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
    const isValidGuid = (str) => {
      if (!str || typeof str !== "string") return false;
      const guidRegex =
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
      return guidRegex.test(str);
    };

    const finalPayload = {
      ...payload,
      couponCode: payload?.couponCode ? payload.couponCode.trim() : null,
      // Only include clientOrderId if it's a valid GUID, otherwise set to null
      clientOrderId: isValidGuid(payload?.clientOrderId)
        ? payload.clientOrderId
        : null,
    };

    try {
      return await api.post(`${base}/checkout`, finalPayload);
    } catch (error) {
      if (error.status === 409 && error.raw?.response?.data) {
        // Idempotent sipariş: mevcut order yanıtını döndür
        return error.raw.response.data;
      }
      throw error;
    }
  },
  downloadInvoice: async (id) => {
    const blob = await api.get(`${base}/${id}/invoice`, {
      responseType: "blob",
      transformResponse: (value) => value,
    });

    const url = window.URL.createObjectURL(new Blob([blob]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", `invoice-${id}.pdf`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  // ============================================================================
  // MİSAFİR SİPARİŞ SORGULAMA
  // Giriş yapmamış kullanıcılar için email + sipariş numarası ile arama
  // Backend'de /api/orders/guest-lookup endpoint'ine istek atar
  // ============================================================================
  findGuestOrder: async (email, orderNumber) => {
    if (!email || !orderNumber) {
      throw new Error("E-posta ve sipariş numarası zorunludur");
    }

    try {
      console.log("[OrderService] Misafir siparişi aranıyor:", {
        email,
        orderNumber,
      });

      const response = await api.get(`${base}/guest-lookup`, {
        params: {
          email: email.trim(),
          orderNumber: orderNumber.trim(),
        },
      });

      // Response kontrolü ve normalizasyon
      if (!response) {
        return null;
      }

      // API'den sipariş geldi, normalize et
      const order = response?.order || response?.data || response;
      return normalizeOrder(order);
    } catch (error) {
      console.error("[OrderService] Misafir sipariş arama hatası:", error);

      // 404 hatası: Sipariş bulunamadı
      if (error?.status === 404) {
        return null;
      }

      throw error;
    }
  },

  // ============================================================================
  // SİPARİŞ TAKİP NUMARASI İLE SORGULAMA
  // Takip numarası ile sipariş durumu sorgulama (public endpoint)
  // ============================================================================
  trackOrder: async (trackingNumber) => {
    if (!trackingNumber) {
      throw new Error("Takip numarası zorunludur");
    }

    try {
      const response = await api.get(`${base}/track/${trackingNumber.trim()}`);
      return normalizeOrder(response);
    } catch (error) {
      console.error("[OrderService] Sipariş takip hatası:", error);
      throw error;
    }
  },
};
