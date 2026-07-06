/**
 * Müşteri iptal/iade kuralları — backend OrderCancelPolicy ile hizalı.
 * Kurye teslim alana kadar otomatik iptal; sonrasında WhatsApp.
 */

export const CANCEL_MODE = {
  AUTO: "auto",
  WHATSAPP: "whatsapp",
  NONE: "none",
};

const AUTO_CANCEL_STATUSES = new Set([
  "new",
  "pending",
  "confirmed",
  "paid",
  "preparing",
  "processing",
  "ready",
  "readyforpickup",
  "assigned",
  "preauthorized",
  "weightpending",
]);

const WHATSAPP_STATUSES = new Set([
  "pickedup",
  "intransit",
  "outfordelivery",
  "shipped",
  "delivered",
  "completed",
  "deliveryfailed",
  "deliverypaymentpending",
  "partialrefund",
]);

const TERMINAL_STATUSES = new Set(["cancelled", "refunded"]);

const INVOICE_BLOCKED_STATUSES = new Set([
  "new",
  "pending",
  "cancelled",
  "paymentfailed",
]);

const TURKEY_TZ = "Europe/Istanbul";

/** İstanbul saat diliminde YYYY-MM-DD anahtarı */
function toTurkeyDateKey(dateInput) {
  if (!dateInput) return null;
  const d = new Date(dateInput);
  if (Number.isNaN(d.getTime())) return null;
  return d.toLocaleDateString("en-CA", { timeZone: TURKEY_TZ });
}

export function normalizeStatus(status) {
  if (status === null || status === undefined) return "";
  return String(status).trim().replace(/[_-]/g, "").toLowerCase();
}

/** Backend RefundRequestStatus enum ile hizalı */
const REFUND_STATUS_BY_NUMBER = {
  0: "pending",
  1: "approved",
  2: "rejected",
  3: "refunded",
  4: "autocancelled",
  5: "refundfailed",
};

export function normalizeRefundStatus(status) {
  if (status === null || status === undefined) return "";
  if (typeof status === "number" && REFUND_STATUS_BY_NUMBER[status]) {
    return REFUND_STATUS_BY_NUMBER[status];
  }
  return normalizeStatus(status);
}

export function isSameBusinessDay(orderDate) {
  if (!orderDate) return false;
  const orderKey = toTurkeyDateKey(orderDate);
  const todayKey = toTurkeyDateKey(new Date());
  return Boolean(orderKey && todayKey && orderKey === todayKey);
}

export function getCancelMode(order) {
  const status = normalizeStatus(order?.status);
  const orderDate = order?.orderDate || order?.createdAt;

  if (TERMINAL_STATUSES.has(status)) {
    return CANCEL_MODE.NONE;
  }

  // Backend canCancel: aynı gün + PickedUp öncesi durumları içerir
  if (typeof order?.canCancel === "boolean") {
    if (order.canCancel) {
      return CANCEL_MODE.AUTO;
    }
    if (order.cancelMode === CANCEL_MODE.NONE) {
      return CANCEL_MODE.NONE;
    }
    return CANCEL_MODE.WHATSAPP;
  }

  if (order?.cancelMode === CANCEL_MODE.NONE) {
    return CANCEL_MODE.NONE;
  }

  if (AUTO_CANCEL_STATUSES.has(status)) {
    return isSameBusinessDay(orderDate) ? CANCEL_MODE.AUTO : CANCEL_MODE.WHATSAPP;
  }

  if (WHATSAPP_STATUSES.has(status)) {
    return CANCEL_MODE.WHATSAPP;
  }

  return CANCEL_MODE.WHATSAPP;
}

export function getOrderActions(order, { isAuthenticated = true } = {}) {
  const status = normalizeStatus(order?.status);
  const cancelMode = getCancelMode(order);
  const orderNumber =
    order?.orderNumber || (order?.id ? `#${order.id}` : "Sipariş");

  if (cancelMode === CANCEL_MODE.AUTO && isAuthenticated) {
    return {
      cancelMode,
      showCancel: true,
      showWhatsApp: true,
      whatsAppPrimary: false,
      cancelLabel: "Siparişi İptal Et",
      disabledReason: null,
      orderNumber,
      status,
    };
  }

  if (cancelMode === CANCEL_MODE.WHATSAPP || cancelMode === CANCEL_MODE.AUTO) {
    const orderDate = order?.orderDate || order?.createdAt;
    let disabledReason;

    if (AUTO_CANCEL_STATUSES.has(status) && !isSameBusinessDay(orderDate)) {
      disabledReason =
        "Otomatik iptal süresi doldu (yalnızca sipariş günü). İade için WhatsApp ile müşteri hizmetlerine yazın; onay sonrası para iadesi yapılır.";
    } else if (WHATSAPP_STATUSES.has(status)) {
      disabledReason =
        "Kurye teslim aldıktan sonra otomatik iptal yapılamaz. İade talebiniz müşteri hizmetleri tarafından incelenir.";
    } else {
      disabledReason =
        "Bu sipariş otomatik iptal edilemez. Müşteri hizmetleriyle iletişime geçin.";
    }

    return {
      cancelMode: CANCEL_MODE.WHATSAPP,
      showCancel: false,
      showWhatsApp: true,
      whatsAppPrimary: true,
      cancelLabel: null,
      disabledReason,
      orderNumber,
      status,
    };
  }

  return {
    cancelMode,
    showCancel: false,
    showWhatsApp: false,
    whatsAppPrimary: false,
    cancelLabel: null,
    disabledReason: null,
    orderNumber,
    status,
  };
}

export function isActiveOrder(order) {
  const status = normalizeStatus(order?.status);
  return !TERMINAL_STATUSES.has(status);
}

export function isCompletedOrder(order) {
  const status = normalizeStatus(order?.status);
  return (
    TERMINAL_STATUSES.has(status) ||
    status === "delivered" ||
    status === "completed"
  );
}

export const REFUND_STATUS_LABELS = {
  pending: "İncelemede",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  refunded: "İade Tamamlandı",
  autocancelled: "Otomatik İptal",
  refundfailed: "İade Başarısız",
};

export function getRefundStatusLabel(status) {
  return REFUND_STATUS_LABELS[normalizeRefundStatus(status)] || status;
}

const COD_METHODS = new Set([
  "cash_on_delivery",
  "kapida_odeme",
  "cod",
  "cash",
  "cashcard",
]);

export function isCashOnDelivery(order) {
  const method = normalizeStatus(
    order?.paymentMethod || order?.PaymentMethod || order?.raw?.paymentMethod || "",
  );
  return COD_METHODS.has(method);
}

export function findOrderRefundRequest(order, refundRequests = []) {
  const orderId = order?.id || order?.orderId;
  if (!orderId) return null;

  return (
    refundRequests.find(
      (r) =>
        r.orderId === orderId ||
        r.orderId === order?.orderId ||
        r.orderID === orderId,
    ) || null
  );
}

/** Müşteri sipariş ekranında gösterilecek iade durumu chip'i */
export function getOrderRefundChip(order, refundRequests = []) {
  const req = findOrderRefundRequest(order, refundRequests);
  if (!req) return null;

  const status = normalizeRefundStatus(req.status ?? req.statusText);

  if (status === "pending") {
    return {
      type: "pending",
      label: "İade talebiniz inceleniyor",
      icon: "clock",
      tone: "warning",
    };
  }

  if (status === "refundfailed") {
    return {
      type: "failed",
      label: "Para iadesi başarısız",
      detail:
        req.refundFailureReason ||
        "Ödeme iadesi tamamlanamadı. Müşteri hizmetleriyle iletişime geçin.",
      icon: "exclamation-triangle",
      tone: "danger",
    };
  }

  if (status === "refunded" || status === "autocancelled") {
    return {
      type: "success",
      label:
        status === "autocancelled"
          ? "Sipariş iptal edildi"
          : "Para iadesi tamamlandı",
      icon: "check-circle",
      tone: "success",
    };
  }

  if (status === "rejected") {
    return {
      type: "rejected",
      label: "İade talebi reddedildi",
      icon: "times-circle",
      tone: "muted",
    };
  }

  return null;
}

/**
 * PDF fatura indirilebilir mi?
 * Backend OrderInvoicePolicy ile hizalı; misafir ve giriş yapmamış kullanıcıda gizlenir.
 */
export function canDownloadInvoice(order, { isAuthenticated = true } = {}) {
  if (!order || !isAuthenticated || order.isGuestOrder) {
    return false;
  }

  if (typeof order.canDownloadInvoice === "boolean") {
    return order.canDownloadInvoice;
  }

  const status = normalizeStatus(order.status);
  if (INVOICE_BLOCKED_STATUSES.has(status)) {
    return false;
  }

  const paymentStatus = normalizeStatus(order.paymentStatus);
  return (
    order.isPaid === true ||
    paymentStatus === "paid" ||
    paymentStatus === "authorized"
  );
}
