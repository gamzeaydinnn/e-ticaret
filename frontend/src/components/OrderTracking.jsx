// ==========================================================================
// OrderTracking.jsx - Müşteri Sipariş Takip Ekranı (Geliştirilmiş)
// ==========================================================================
// SignalR entegrasyonu ile real-time sipariş takibi.
// Stepper UI ile adım adım sipariş durumu gösterimi.
// ==========================================================================

import { useEffect, useState, useCallback } from "react";
import { OrderService } from "../services/orderService";
import signalRService, { ConnectionState } from "../services/signalRService";

// ==========================================================================
// DURUM TANIMLARI VE RENKLER
// ==========================================================================

/**
 * Sipariş durumları ve özellikleri
 * NEDEN: Backend ile tutarlı durum yönetimi için merkezi tanımlama
 */
const ORDER_STATUSES = {
  // Sipariş oluşturma aşaması
  pending: {
    step: 0,
    label: "Siparişiniz Alındı",
    shortLabel: "Alındı",
    description: "Siparişiniz başarıyla oluşturuldu ve onay bekliyor",
    icon: "fa-shopping-cart",
    color: "#ffc107",
    bgColor: "#fff3cd",
  },
  new: {
    step: 0,
    label: "Siparişiniz Alındı",
    shortLabel: "Alındı",
    description: "Siparişiniz başarıyla oluşturuldu",
    icon: "fa-shopping-cart",
    color: "#ffc107",
    bgColor: "#fff3cd",
  },
  // Onay aşaması
  confirmed: {
    step: 1,
    label: "Sipariş Onaylandı",
    shortLabel: "Onaylandı",
    description: "Siparişiniz mağaza tarafından onaylandı",
    icon: "fa-check-circle",
    color: "#17a2b8",
    bgColor: "#d1ecf1",
  },
  // Hazırlık aşaması
  preparing: {
    step: 2,
    label: "Hazırlanıyor",
    shortLabel: "Hazırlanıyor",
    description: "Siparişiniz hazırlanıyor ve paketleniyor",
    icon: "fa-box",
    color: "#fd7e14",
    bgColor: "#ffe5d0",
  },
  processing: {
    step: 2,
    label: "İşleniyor",
    shortLabel: "İşleniyor",
    description: "Siparişiniz işleme alındı",
    icon: "fa-cog",
    color: "#fd7e14",
    bgColor: "#ffe5d0",
  },
  // Hazır / Kurye ataması aşaması
  ready: {
    step: 2,
    label: "Sipariş Hazırlandı",
    shortLabel: "Hazır",
    description: "Siparişiniz hazırlandı, kurye ataması bekleniyor",
    icon: "fa-box",
    color: "#fd7e14",
    bgColor: "#ffe5d0",
  },
  assigned: {
    step: 3,
    label: "Kuryeniz Atandı",
    shortLabel: "Kurye Atandı",
    description: "Kurye siparişinizi teslim almak üzere yola çıktı",
    icon: "fa-motorcycle",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  pickedup: {
    step: 3,
    label: "Kurye Siparişi Aldı",
    shortLabel: "Kurye'de",
    description: "Siparişiniz kuryede, teslimata hazırlanıyor",
    icon: "fa-motorcycle",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  picked_up: {
    step: 3,
    label: "Kurye Siparişi Aldı",
    shortLabel: "Kurye'de",
    description: "Siparişiniz kuryede, teslimata hazırlanıyor",
    icon: "fa-motorcycle",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  // Kargo aşaması
  shipped: {
    step: 3,
    label: "Siparişiniz Yola Çıktı",
    shortLabel: "Yola Çıktı",
    description: "Siparişiniz teslimat için yola çıktı",
    icon: "fa-truck",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  out_for_delivery: {
    step: 3,
    label: "Siparişiniz Yola Çıktı",
    shortLabel: "Yola Çıktı",
    description: "Siparişiniz teslimat için yola çıktı",
    icon: "fa-shipping-fast",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  outfordelivery: {
    step: 3,
    label: "Siparişiniz Yola Çıktı",
    shortLabel: "Yola Çıktı",
    description: "Siparişiniz teslimat için yola çıktı",
    icon: "fa-shipping-fast",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  // Teslim aşaması
  delivered: {
    step: 4,
    label: "Teslim Edildi ✅",
    shortLabel: "Teslim Edildi",
    description: "Siparişiniz başarıyla teslim edildi",
    icon: "fa-check-double",
    color: "#28a745",
    bgColor: "#d4edda",
  },
  // İptal/Problem durumları
  cancelled: {
    step: -1,
    label: "İptal Edildi",
    shortLabel: "İptal",
    description: "Siparişiniz iptal edildi",
    icon: "fa-times-circle",
    color: "#dc3545",
    bgColor: "#f8d7da",
  },
  delivery_failed: {
    step: -1,
    label: "Teslimat Başarısız",
    shortLabel: "Başarısız",
    description:
      "Teslimat gerçekleştirilemedi. Lütfen bizimle iletişime geçin.",
    icon: "fa-exclamation-triangle",
    color: "#dc3545",
    bgColor: "#f8d7da",
  },
  delivery_payment_pending: {
    step: 4, // Teslim edildi ama ödeme bekliyor
    label: "Ödeme Bekleniyor",
    shortLabel: "Ödeme Bekliyor",
    description:
      "Siparişiniz teslim edildi ancak ödeme işlemi beklemede. Kısa sürede tamamlanacak.",
    icon: "fa-credit-card",
    color: "#fd7e14",
    bgColor: "#fff3cd",
  },
  refunded: {
    step: -1,
    label: "İade Edildi",
    shortLabel: "İade",
    description: "Siparişiniz iade edildi",
    icon: "fa-undo",
    color: "#6c757d",
    bgColor: "#e9ecef",
  },
};

/**
 * Stepper adımları
 */
const STEPPER_STEPS = [
  { key: "pending", label: "Sipariş Alındı", icon: "fa-shopping-cart" },
  { key: "confirmed", label: "Onaylandı", icon: "fa-check-circle" },
  { key: "preparing", label: "Hazırlanıyor", icon: "fa-box" },
  { key: "shipped", label: "Yola Çıktı", icon: "fa-truck" },
  { key: "delivered", label: "Teslim Edildi", icon: "fa-check-double" },
];

// ==========================================================================
// HELPER FONKSİYONLAR
// ==========================================================================

const getStatusInfo = (status) => {
  const normalizedStatus = (status || "pending")
    .toLowerCase()
    .replace(/ /g, "_");
  return ORDER_STATUSES[normalizedStatus] || ORDER_STATUSES.pending;
};

const getStepperProgress = (status) => {
  const info = getStatusInfo(status);
  return info.step >= 0 ? ((info.step + 1) / STEPPER_STEPS.length) * 100 : 0;
};

// ==========================================================================
// ANA COMPONENT
// ==========================================================================

const OrderTracking = () => {
  // State
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [trackingCode, setTrackingCode] = useState("");
  const [connectionStatus, setConnectionStatus] = useState(
    ConnectionState.DISCONNECTED,
  );
  const [notification, setNotification] = useState(null);

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================
  const loadOrders = useCallback(async () => {
    try {
      const userId = localStorage.getItem("userId");
      const userOrders = userId
        ? await OrderService.list(userId)
        : await OrderService.list();
      setOrders(userOrders || []);
    } catch (error) {
      console.error("Siparişler yüklenemedi:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  // =========================================================================
  // SIGNALR BAĞLANTISI
  // =========================================================================
  // SES BİLDİRİMİ VE BROWSER NOTIFICATION
  // =========================================================================
  const playNotificationSound = useCallback(() => {
    try {
      const audio = new Audio(
        "/sounds/mixkit-happy-bells-notification-937.wav",
      );
      audio.volume = 0.6;
      audio.play().catch(() => {});
    } catch (e) {
      console.warn("[OrderTracking] Ses çalınamadı:", e);
    }
  }, []);

  const showBrowserNotification = useCallback(
    (title, body, icon = "fa-bell") => {
      // Ses çal
      playNotificationSound();

      // Browser notification
      if ("Notification" in window && Notification.permission === "granted") {
        new Notification(title, {
          body,
          icon: "/logo192.png",
          tag: "order-tracking",
          requireInteraction: false,
        });
      } else if (
        "Notification" in window &&
        Notification.permission !== "denied"
      ) {
        Notification.requestPermission();
      }
    },
    [playNotificationSound],
  );

  // Browser notification izni iste
  useEffect(() => {
    if ("Notification" in window && Notification.permission === "default") {
      Notification.requestPermission();
    }
  }, []);

  // =========================================================================
  // SIGNALR BAĞLANTISI
  // =========================================================================
  useEffect(() => {
    loadOrders();

    // SignalR bağlantısı kur
    const connectSignalR = async () => {
      try {
        const connected = await signalRService.connectCustomer();
        if (connected) {
          setConnectionStatus(ConnectionState.CONNECTED);
          console.log("[OrderTracking] SignalR bağlantısı kuruldu");
        }
      } catch (error) {
        console.error("[OrderTracking] SignalR bağlantı hatası:", error);
        setConnectionStatus(ConnectionState.FAILED);
      }
    };

    connectSignalR();

    // Sipariş durum değişikliği dinle
    const unsubscribeStatus = signalRService.onOrderStatusChanged((data) => {
      console.log("[OrderTracking] Sipariş durumu değişti:", data);

      // Bildirimi göster
      const statusInfo = getStatusInfo(data.newStatus || data.status);

      // Browser notification ve ses
      showBrowserNotification(
        `📦 Sipariş #${data.orderNumber || data.orderId}`,
        statusInfo.label + " - " + (statusInfo.description || ""),
        statusInfo.icon,
      );

      setNotification({
        type: "info",
        title: `Sipariş #${data.orderId || data.orderNumber}`,
        message: statusInfo.label,
        icon: statusInfo.icon,
        color: statusInfo.color,
      });

      // Sipariş listesini güncelle
      setOrders((prev) =>
        prev.map((order) =>
          order.id === data.orderId || order.orderNumber === data.orderNumber
            ? { ...order, status: data.newStatus || data.status }
            : order,
        ),
      );

      // Seçili sipariş güncellemesi
      setSelectedOrder((prev) =>
        prev &&
        (prev.id === data.orderId || prev.orderNumber === data.orderNumber)
          ? { ...prev, status: data.newStatus || data.status }
          : prev,
      );

      // Bildirimi 5 saniye sonra kaldır
      setTimeout(() => setNotification(null), 5000);
    });

    // Teslimat durum değişikliği dinle
    const unsubscribeDelivery = signalRService.onDeliveryStatusChanged(
      (data) => {
        console.log("[OrderTracking] Teslimat durumu değişti:", data);

        // Sipariş listesini güncelle (orderId eşleşirse)
        if (data.orderId) {
          loadOrders(); // Verileri yenile
        }
      },
    );

    // Bağlantı durumu değişikliği dinle
    const deliveryHub = signalRService.deliveryHub;
    const unsubscribeState = deliveryHub.onStateChange((newState) => {
      setConnectionStatus(newState);
    });

    // Cleanup
    return () => {
      unsubscribeStatus();
      unsubscribeDelivery();
      unsubscribeState();
    };
  }, [loadOrders, showBrowserNotification]);

  // =========================================================================
  // SİPARİŞ TAKİP
  // =========================================================================
  const trackOrderByCode = async () => {
    if (!trackingCode.trim()) return;

    // Önce local listede ara
    const order = orders.find(
      (o) => o.orderNumber === trackingCode || String(o.id) === String(trackingCode),
    );

    if (order) {
      setSelectedOrder(order);
      // SignalR ile bu siparişin grubuna katıl
      await signalRService.connectCustomer(order.id);
      return;
    }

    // Sunucudan getir
    try {
      const fetched = await OrderService.getById(trackingCode);
      if (fetched) {
        setSelectedOrder(fetched);
        await signalRService.connectCustomer(fetched.id);
      } else {
        setNotification({
          type: "error",
          title: "Sipariş Bulunamadı",
          message: "Lütfen takip kodunu kontrol edin.",
          icon: "fa-exclamation-circle",
          color: "#dc3545",
        });
        setTimeout(() => setNotification(null), 4000);
      }
    } catch (err) {
      setNotification({
        type: "error",
        title: "Hata",
        message: "Sipariş bulunamadı veya sunucuya erişilemiyor.",
        icon: "fa-exclamation-circle",
        color: "#dc3545",
      });
      setTimeout(() => setNotification(null), 4000);
    }
  };

  // =========================================================================
  // RENDER HELPERS
  // =========================================================================
  const renderConnectionBadge = () => {
    const statusConfig = {
      [ConnectionState.CONNECTED]: {
        color: "success",
        icon: "fa-wifi",
        text: "Canlı Takip Aktif",
      },
      [ConnectionState.CONNECTING]: {
        color: "warning",
        icon: "fa-spinner fa-spin",
        text: "Bağlanıyor...",
      },
      [ConnectionState.RECONNECTING]: {
        color: "warning",
        icon: "fa-sync fa-spin",
        text: "Yeniden Bağlanıyor...",
      },
      [ConnectionState.DISCONNECTED]: {
        color: "secondary",
        icon: "fa-wifi",
        text: "Çevrimdışı",
      },
      [ConnectionState.FAILED]: {
        color: "danger",
        icon: "fa-exclamation-triangle",
        text: "Bağlantı Hatası",
      },
    };
    const config =
      statusConfig[connectionStatus] ||
      statusConfig[ConnectionState.DISCONNECTED];

    return (
      <span
        className={`badge bg-${config.color} ms-2`}
        style={{ fontSize: "10px" }}
      >
        <i className={`fas ${config.icon} me-1`}></i>
        {config.text}
      </span>
    );
  };

  // =========================================================================
  // LOADING STATE
  // =========================================================================
  if (loading) {
    return (
      <div className="text-center py-5">
        <div
          className="spinner-border mb-3"
          role="status"
          style={{ color: "#ff8f00", width: "3rem", height: "3rem" }}
        >
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
        <p className="text-muted fw-bold">Siparişleriniz yükleniyor...</p>
      </div>
    );
  }

  // =========================================================================
  // MAIN RENDER
  // =========================================================================
  return (
    <div
      style={{
        minHeight: "100vh",
        background:
          "linear-gradient(135deg, #fff3e0 0%, #ffe0b2 50%, #ffcc80 100%)",
        paddingTop: "2rem",
        paddingBottom: "2rem",
      }}
    >
      <div className="container">
        {/* Real-time Bildirim */}
        {notification && (
          <div
            className="alert d-flex align-items-center shadow-lg mb-4"
            style={{
              backgroundColor: notification.color + "15",
              borderLeft: `4px solid ${notification.color}`,
              borderRadius: "12px",
              animation: "slideIn 0.3s ease",
            }}
          >
            <i
              className={`fas ${notification.icon} me-3`}
              style={{ fontSize: "24px", color: notification.color }}
            ></i>
            <div>
              <strong>{notification.title}</strong>
              <p className="mb-0 small text-muted">{notification.message}</p>
            </div>
            <button
              className="btn-close ms-auto"
              onClick={() => setNotification(null)}
            ></button>
          </div>
        )}

        {/* Sipariş No ile Arama kaldırıldı */}

        {/* Seçilen Sipariş Detayı */}
        {selectedOrder && (
          <OrderDetailCard
            order={selectedOrder}
            onClose={() => setSelectedOrder(null)}
          />
        )}

        {/* Tüm Siparişler */}
        <div
          className="card shadow-lg border-0"
          style={{ borderRadius: "20px" }}
        >
          <div
            className="card-header text-white border-0"
            style={{
              background: "linear-gradient(45deg, #6f42c1, #e83e8c)",
              borderTopLeftRadius: "20px",
              borderTopRightRadius: "20px",
              padding: "1.5rem",
            }}
          >
            <h4 className="mb-0 fw-bold">
              <i className="fas fa-list me-2"></i>Tüm Siparişlerim
              <span className="badge bg-white text-primary ms-2">
                {orders.length}
              </span>
            </h4>
          </div>
          <div className="card-body" style={{ padding: "2rem" }}>
            {orders.length === 0 ? (
              <EmptyOrdersState />
            ) : (
              <div className="row">
                {orders.map((order) => (
                  <div key={order.id} className="col-md-6 mb-4">
                    <OrderCard
                      order={order}
                      onClick={() => setSelectedOrder(order)}
                    />
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* CSS Animations */}
      <style>{`
        @keyframes slideIn {
          from {
            opacity: 0;
            transform: translateY(-20px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
        @keyframes pulse {
          0%, 100% { transform: scale(1); }
          50% { transform: scale(1.05); }
        }
      `}</style>
    </div>
  );
};

// ==========================================================================
// ALT COMPONENTLER
// ==========================================================================

/**
 * Sipariş Kartı
 */
const OrderCard = ({ order, onClick }) => {
  const statusInfo = getStatusInfo(order.status);
  const isCancelled = statusInfo.step === -1;

  return (
    <div
      className="card shadow-sm border-0 h-100"
      style={{
        borderRadius: "15px",
        cursor: "pointer",
        transition: "transform 0.2s, box-shadow 0.2s",
      }}
      onClick={onClick}
      onMouseOver={(e) => {
        e.currentTarget.style.transform = "translateY(-4px)";
        e.currentTarget.style.boxShadow = "0 8px 25px rgba(0,0,0,0.15)";
      }}
      onMouseOut={(e) => {
        e.currentTarget.style.transform = "translateY(0)";
        e.currentTarget.style.boxShadow = "";
      }}
    >
      <div className="card-body" style={{ padding: "1.5rem" }}>
        {/* Header */}
        <div className="d-flex justify-content-between align-items-start mb-3">
          <h6 className="fw-bold mb-0">Sipariş #{order.orderNumber}</h6>
          <span
            className="badge px-3 py-2"
            style={{
              backgroundColor: statusInfo.bgColor,
              color: statusInfo.color,
              borderRadius: "20px",
            }}
          >
            <i className={`fas ${statusInfo.icon} me-1`}></i>
            {statusInfo.shortLabel}
          </span>
        </div>

        {/* Mini Stepper (iptal/problem durumlarında gösterme) */}
        {!isCancelled && <MiniStepper status={order.status} />}

        {/* İptal/Problem Banner */}
        {isCancelled && (
          <div
            className="alert mb-3 py-2"
            style={{
              backgroundColor: statusInfo.bgColor,
              borderRadius: "10px",
              border: `1px solid ${statusInfo.color}`,
            }}
          >
            <small
              className="d-flex align-items-center"
              style={{ color: statusInfo.color }}
            >
              <i className={`fas ${statusInfo.icon} me-2`}></i>
              {statusInfo.description}
            </small>
          </div>
        )}

        {/* Bilgiler */}
        <p className="text-muted mb-2">
          <i className="fas fa-calendar me-2"></i>
          {new Date(order.orderDate).toLocaleDateString("tr-TR")}
        </p>


        <p className="fw-bold mb-3" style={{ color: "#ff6f00" }}>
          <i className="fas fa-tag me-2"></i>₺
          {Number(order.totalAmount || order.totalPrice || 0).toFixed(2)}
        </p>

        <button
          className="btn btn-outline-warning btn-sm fw-bold w-100"
          style={{ borderRadius: "15px" }}
        >
          <i className="fas fa-eye me-2"></i>
          Detayları Görüntüle
        </button>
      </div>
    </div>
  );
};

/**
 * Mini Stepper (Sipariş kartı için)
 */
const MiniStepper = ({ status }) => {
  const statusInfo = getStatusInfo(status);
  const currentStep = statusInfo.step;

  return (
    <div className="d-flex justify-content-between mb-3" style={{ gap: "4px" }}>
      {STEPPER_STEPS.map((step, index) => (
        <div
          key={step.key}
          className="flex-grow-1"
          style={{
            height: "6px",
            borderRadius: "3px",
            backgroundColor: index <= currentStep ? "#28a745" : "#e9ecef",
            transition: "background-color 0.3s",
          }}
        />
      ))}
    </div>
  );
};

/**
 * Sipariş Detay Kartı (Modal gibi)
 */
const OrderDetailCard = ({ order, onClose }) => {
  const statusInfo = getStatusInfo(order.status);
  const isCancelled = statusInfo.step === -1;

  return (
    <div
      className="card shadow-lg border-0 mb-4"
      style={{ borderRadius: "20px" }}
    >
      <div
        className="card-header text-white border-0 d-flex justify-content-between align-items-center"
        style={{
          background: `linear-gradient(45deg, ${statusInfo.color}, ${statusInfo.color}dd)`,
          borderTopLeftRadius: "20px",
          borderTopRightRadius: "20px",
          padding: "1.5rem",
        }}
      >
        <h5 className="mb-0 fw-bold">
          <i className="fas fa-package me-2"></i>
          Sipariş #{order.orderNumber}
        </h5>
        <button
          className="btn btn-light btn-sm rounded-circle"
          onClick={onClose}
          style={{ width: "32px", height: "32px" }}
        >
          <i className="fas fa-times"></i>
        </button>
      </div>
      <div className="card-body" style={{ padding: "2rem" }}>
        {/* İptal/Problem Banner */}
        {isCancelled && (
          <div
            className="alert d-flex align-items-center mb-4"
            style={{
              backgroundColor: statusInfo.bgColor,
              borderRadius: "12px",
              border: `2px solid ${statusInfo.color}`,
            }}
          >
            <i
              className={`fas ${statusInfo.icon} me-3`}
              style={{ fontSize: "24px", color: statusInfo.color }}
            ></i>
            <div>
              <strong style={{ color: statusInfo.color }}>
                {statusInfo.label}
              </strong>
              <p className="mb-0 small text-muted">{statusInfo.description}</p>
            </div>
          </div>
        )}

        {/* Stepper Timeline */}
        {!isCancelled && <OrderStepper status={order.status} />}

        {/* Bilgiler */}
        <div className="row mt-4">
          <div className="col-md-6">
            <h6 className="fw-bold mb-3" style={{ color: "#ff6f00" }}>
              <i className="fas fa-info-circle me-2"></i>Sipariş Bilgileri
            </h6>
            <p className="mb-2">
              <strong>Sipariş No:</strong> {order.orderNumber}
            </p>
            <p className="mb-2">
              <strong>Toplam Tutar:</strong>{" "}
              <span className="fw-bold" style={{ color: "#ff6f00" }}>
                ₺{Number(order.totalAmount || order.totalPrice || 0).toFixed(2)}
              </span>
            </p>
            <p className="mb-2">
              <strong>Sipariş Tarihi:</strong>{" "}
              {new Date(order.orderDate).toLocaleDateString("tr-TR", {
                day: "numeric",
                month: "long",
                year: "numeric",
                hour: "2-digit",
                minute: "2-digit",
              })}
            </p>
          </div>
          <div className="col-md-6">
            <h6 className="fw-bold mb-3" style={{ color: "#ff6f00" }}>
              <i className="fas fa-truck me-2"></i>Teslimat Bilgileri
            </h6>
            <p className="mb-2">
              <strong>Adres:</strong> {order.deliveryAddress || "Belirtilmedi"}
            </p>
            {order.shippingCompany && (
              <p className="mb-2">
                <strong>Kargo Firması:</strong> {order.shippingCompany}
              </p>
            )}
            {order.estimatedDeliveryDate && (
              <p className="mb-2">
                <strong>Tahmini Teslimat:</strong>{" "}
                {new Date(order.estimatedDeliveryDate).toLocaleDateString(
                  "tr-TR",
                )}
              </p>
            )}
          </div>
        </div>

        {/* Ürünler */}
        {order.items && order.items.length > 0 && (
          <div className="mt-4">
            <h6 className="fw-bold mb-3" style={{ color: "#ff6f00" }}>
              <i className="fas fa-shopping-basket me-2"></i>Sipariş Ürünleri
            </h6>
            <div className="row">
              {order.items.map((item, index) => (
                <div key={item.id || index} className="col-md-6 mb-3">
                  <div
                    className="card border-0 shadow-sm"
                    style={{ borderRadius: "12px" }}
                  >
                    <div className="card-body p-3">
                      <div className="d-flex align-items-center">
                        <div
                          className="me-3 d-flex align-items-center justify-content-center"
                          style={{
                            width: "50px",
                            height: "50px",
                            backgroundColor: "#fff8f0",
                            borderRadius: "10px",
                          }}
                        >
                          <i
                            className="fas fa-box"
                            style={{ color: "#ff6f00" }}
                          ></i>
                        </div>
                        <div className="flex-grow-1">
                          <h6 className="mb-1 fw-bold">
                            {item.name || item.productName}
                          </h6>
                          <p className="mb-0 text-muted small">
                            {item.quantity} adet × ₺
                            {Number(item.unitPrice || item.price || 0).toFixed(
                              2,
                            )}
                          </p>
                        </div>
                        <span className="fw-bold" style={{ color: "#ff6f00" }}>
                          ₺
                          {Number(
                            (item.quantity || 1) *
                              (item.unitPrice || item.price || 0),
                          ).toFixed(2)}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

/**
 * Sipariş Stepper (Büyük timeline)
 */
const OrderStepper = ({ status }) => {
  const statusInfo = getStatusInfo(status);
  const currentStep = statusInfo.step;

  return (
    <div className="stepper-container">
      {/* Progress Bar */}
      <div
        className="position-relative mb-4"
        style={{
          height: "6px",
          backgroundColor: "#e9ecef",
          borderRadius: "3px",
        }}
      >
        <div
          className="position-absolute top-0 start-0 h-100"
          style={{
            width: `${getStepperProgress(status)}%`,
            backgroundColor: "#28a745",
            borderRadius: "3px",
            transition: "width 0.5s ease",
          }}
        />
      </div>

      {/* Steps */}
      <div className="d-flex justify-content-between">
        {STEPPER_STEPS.map((step, index) => {
          const isCompleted = index < currentStep;
          const isActive = index === currentStep;

          return (
            <div
              key={step.key}
              className="text-center"
              style={{ flex: 1, maxWidth: "100px" }}
            >
              {/* Step Circle */}
              <div
                className={`mx-auto mb-2 d-flex align-items-center justify-content-center rounded-circle ${
                  isActive ? "shadow-lg" : ""
                }`}
                style={{
                  width: isActive ? "56px" : "44px",
                  height: isActive ? "56px" : "44px",
                  backgroundColor: isCompleted
                    ? "#28a745"
                    : isActive
                      ? "#ff6f00"
                      : "#e9ecef",
                  color: isCompleted || isActive ? "white" : "#6c757d",
                  transition: "all 0.3s ease",
                  animation: isActive ? "pulse 2s infinite" : "none",
                }}
              >
                {isCompleted ? (
                  <i className="fas fa-check"></i>
                ) : (
                  <i
                    className={`fas ${step.icon}`}
                    style={{ fontSize: isActive ? "18px" : "14px" }}
                  ></i>
                )}
              </div>

              {/* Step Label */}
              <small
                className={`d-block ${
                  isCompleted
                    ? "text-success fw-bold"
                    : isActive
                      ? "fw-bold"
                      : "text-muted"
                }`}
                style={{
                  fontSize: isActive ? "13px" : "11px",
                  color: isActive ? "#ff6f00" : undefined,
                }}
              >
                {step.label}
              </small>
            </div>
          );
        })}
      </div>

      {/* Mevcut Durum Açıklaması */}
      <div
        className="text-center mt-4 p-3"
        style={{
          backgroundColor: statusInfo.bgColor,
          borderRadius: "12px",
          border: `2px solid ${statusInfo.color}`,
        }}
      >
        <i
          className={`fas ${statusInfo.icon} me-2`}
          style={{ color: statusInfo.color }}
        ></i>
        <strong style={{ color: statusInfo.color }}>{statusInfo.label}</strong>
        <p className="mb-0 mt-1 small text-muted">{statusInfo.description}</p>
      </div>
    </div>
  );
};

/**
 * Boş Sipariş Durumu
 */
const EmptyOrdersState = () => (
  <div className="text-center py-5">
    <div
      className="p-4 rounded-circle mx-auto mb-4 shadow-lg"
      style={{
        backgroundColor: "#fff8f0",
        width: "120px",
        height: "120px",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      <i
        className="fas fa-shopping-bag text-warning"
        style={{ fontSize: "3rem" }}
      ></i>
    </div>
    <h4 className="text-warning fw-bold mb-3">Henüz Siparişiniz Yok</h4>
    <p className="text-muted fs-5">
      İlk siparişinizi vermek için alışverişe başlayın!
    </p>
  </div>
);

export default OrderTracking;
