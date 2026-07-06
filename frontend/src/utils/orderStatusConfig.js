import { normalizeStatus } from "./orderCancelPolicy";

export const ORDER_STATUS_CONFIG = {
  new: { label: "Sipariş Alındı", shortLabel: "Alındı", color: "#ffc107", bgColor: "#fff3cd", icon: "fa-shopping-cart" },
  pending: { label: "Sipariş Alındı", shortLabel: "Alındı", color: "#ffc107", bgColor: "#fff3cd", icon: "fa-shopping-cart" },
  paid: { label: "Ödeme Alındı", shortLabel: "Ödendi", color: "#0ea5e9", bgColor: "#e0f2fe", icon: "fa-credit-card" },
  preauthorized: { label: "Provizyon Alındı", shortLabel: "Provizyon", color: "#0ea5e9", bgColor: "#e0f2fe", icon: "fa-shield-alt" },
  confirmed: { label: "Onaylandı", shortLabel: "Onay", color: "#17a2b8", bgColor: "#d1ecf1", icon: "fa-check" },
  preparing: { label: "Hazırlanıyor", shortLabel: "Hazırlanıyor", color: "#fd7e14", bgColor: "#ffe5d0", icon: "fa-utensils" },
  processing: { label: "Hazırlanıyor", shortLabel: "Hazırlanıyor", color: "#fd7e14", bgColor: "#ffe5d0", icon: "fa-cog" },
  ready: { label: "Hazır", shortLabel: "Hazır", color: "#28a745", bgColor: "#d4edda", icon: "fa-box" },
  readyforpickup: { label: "Teslime Hazır", shortLabel: "Hazır", color: "#28a745", bgColor: "#d4edda", icon: "fa-box-open" },
  assigned: { label: "Kuryeye Atandı", shortLabel: "Atandı", color: "#0d6efd", bgColor: "#cfe2ff", icon: "fa-user-check" },
  pickedup: { label: "Kurye Teslim Aldı", shortLabel: "Teslim Alındı", color: "#20c997", bgColor: "#d1f2eb", icon: "fa-hand-holding-box" },
  intransit: { label: "Yolda", shortLabel: "Yolda", color: "#6f42c1", bgColor: "#e2d9f3", icon: "fa-motorcycle" },
  outfordelivery: { label: "Teslimat Yolunda", shortLabel: "Yolda", color: "#6f42c1", bgColor: "#e2d9f3", icon: "fa-shipping-fast" },
  shipped: { label: "Kargoda", shortLabel: "Kargoda", color: "#6f42c1", bgColor: "#e2d9f3", icon: "fa-truck" },
  delivered: { label: "Teslim Edildi", shortLabel: "Teslim", color: "#28a745", bgColor: "#d4edda", icon: "fa-check-double" },
  completed: { label: "Tamamlandı", shortLabel: "Tamam", color: "#28a745", bgColor: "#d4edda", icon: "fa-check-circle" },
  cancelled: { label: "İptal Edildi", shortLabel: "İptal", color: "#dc3545", bgColor: "#f8d7da", icon: "fa-ban" },
  refunded: { label: "İade Edildi", shortLabel: "İade", color: "#6c757d", bgColor: "#e9ecef", icon: "fa-undo" },
  partialrefund: { label: "Kısmi İade", shortLabel: "Kısmi İade", color: "#17a2b8", bgColor: "#d1ecf1", icon: "fa-undo-alt" },
  deliveryfailed: { label: "Teslimat Başarısız", shortLabel: "Başarısız", color: "#dc3545", bgColor: "#f8d7da", icon: "fa-exclamation-triangle" },
  weightpending: { label: "Tartım Bekleniyor", shortLabel: "Tartım", color: "#ff9800", bgColor: "#fff3e0", icon: "fa-balance-scale" },
};

export function getStatusConfig(status) {
  const key = normalizeStatus(status);
  return (
    ORDER_STATUS_CONFIG[key] || {
      label: status || "Bilinmiyor",
      shortLabel: status || "?",
      color: "#6c757d",
      bgColor: "#e9ecef",
      icon: "fa-question-circle",
    }
  );
}
