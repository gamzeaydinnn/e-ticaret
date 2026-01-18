/**
 * ============================================================================
 * E-TİCARET - BİLDİRİM ÇANI KOMPONENTİ
 * ============================================================================
 * 
 * Gerçek zamanlı bildirimleri gösteren header komponenti.
 * SignalR ile yeni sipariş, teslimat güncellemeleri vb. alır.
 * 
 * Özellikler:
 * - Okunmamış bildirim sayısı badge
 * - Dropdown bildirim listesi
 * - Bildirim türlerine göre ikon ve renk
 * - Tümünü okundu işaretle
 * - Tümünü görüntüle sayfasına yönlendirme
 * 
 * @author E-Ticaret Ekibi
 * @version 1.0.0
 * ============================================================================
 */

import React, { useState, useEffect, useCallback, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { signalRService, SignalREvents, ConnectionState } from "../services/signalRService";

// ============================================================================
// BİLDİRİM TÜRLERİ
// ============================================================================

/**
 * Bildirim türü enum
 */
export const NotificationType = {
  NEW_ORDER: "new_order",
  ORDER_STATUS: "order_status",
  DELIVERY_ASSIGNED: "delivery_assigned",
  DELIVERY_STATUS: "delivery_status",
  COURIER_ONLINE: "courier_online",
  COURIER_OFFLINE: "courier_offline",
  PAYMENT_RECEIVED: "payment_received",
  LOW_STOCK: "low_stock",
  SYSTEM: "system"
};

/**
 * Bildirim türüne göre ikon ve renk
 */
const NotificationConfig = {
  [NotificationType.NEW_ORDER]: {
    icon: "fas fa-shopping-cart",
    color: "primary",
    bgColor: "rgba(13, 110, 253, 0.1)"
  },
  [NotificationType.ORDER_STATUS]: {
    icon: "fas fa-info-circle",
    color: "info",
    bgColor: "rgba(13, 202, 240, 0.1)"
  },
  [NotificationType.DELIVERY_ASSIGNED]: {
    icon: "fas fa-truck",
    color: "success",
    bgColor: "rgba(25, 135, 84, 0.1)"
  },
  [NotificationType.DELIVERY_STATUS]: {
    icon: "fas fa-shipping-fast",
    color: "warning",
    bgColor: "rgba(255, 193, 7, 0.1)"
  },
  [NotificationType.COURIER_ONLINE]: {
    icon: "fas fa-motorcycle",
    color: "success",
    bgColor: "rgba(25, 135, 84, 0.1)"
  },
  [NotificationType.COURIER_OFFLINE]: {
    icon: "fas fa-user-slash",
    color: "secondary",
    bgColor: "rgba(108, 117, 125, 0.1)"
  },
  [NotificationType.PAYMENT_RECEIVED]: {
    icon: "fas fa-credit-card",
    color: "success",
    bgColor: "rgba(25, 135, 84, 0.1)"
  },
  [NotificationType.LOW_STOCK]: {
    icon: "fas fa-exclamation-triangle",
    color: "danger",
    bgColor: "rgba(220, 53, 69, 0.1)"
  },
  [NotificationType.SYSTEM]: {
    icon: "fas fa-cog",
    color: "secondary",
    bgColor: "rgba(108, 117, 125, 0.1)"
  }
};

// ============================================================================
// ANA KOMPONENT
// ============================================================================

export default function NotificationBell() {
  // =========================================================================
  // STATE YÖNETİMİ
  // =========================================================================
  
  const [notifications, setNotifications] = useState([]);
  const [isOpen, setIsOpen] = useState(false);
  const [isConnected, setIsConnected] = useState(false);
  const dropdownRef = useRef(null);
  const navigate = useNavigate();

  // Okunmamış bildirim sayısı
  const unreadCount = notifications.filter(n => !n.read).length;

  // =========================================================================
  // SIGNALR BAĞLANTISI
  // =========================================================================

  useEffect(() => {
    const unsubscribers = [];

    const setupSignalR = async () => {
      try {
        // Order Hub bağlantısı
        await signalRService.orderHub.connect();
        
        // Yeni sipariş bildirimi
        const unsubNewOrder = signalRService.orderHub.on(
          SignalREvents.NEW_ORDER,
          handleNewOrder
        );
        unsubscribers.push(unsubNewOrder);

        // Sipariş durumu değişikliği
        const unsubOrderStatus = signalRService.orderHub.on(
          SignalREvents.ORDER_STATUS_CHANGED,
          handleOrderStatusChange
        );
        unsubscribers.push(unsubOrderStatus);

        // Teslimat atama bildirimi
        const unsubDeliveryAssigned = signalRService.orderHub.on(
          SignalREvents.DELIVERY_ASSIGNED,
          handleDeliveryAssigned
        );
        unsubscribers.push(unsubDeliveryAssigned);

        // Delivery Hub bağlantısı
        await signalRService.deliveryHub.connect();

        // Teslimat durumu değişikliği
        const unsubDeliveryStatus = signalRService.deliveryHub.on(
          SignalREvents.DELIVERY_STATUS_CHANGED,
          handleDeliveryStatusChange
        );
        unsubscribers.push(unsubDeliveryStatus);

        // Kurye online/offline
        const unsubCourierOnline = signalRService.deliveryHub.on(
          SignalREvents.COURIER_ONLINE,
          handleCourierOnline
        );
        unsubscribers.push(unsubCourierOnline);

        const unsubCourierOffline = signalRService.deliveryHub.on(
          SignalREvents.COURIER_OFFLINE,
          handleCourierOffline
        );
        unsubscribers.push(unsubCourierOffline);

        // Bağlantı durumu
        const unsubState = signalRService.orderHub.onStateChange((state) => {
          setIsConnected(state === ConnectionState.CONNECTED);
        });
        unsubscribers.push(unsubState);

      } catch (error) {
        console.error("[NotificationBell] SignalR bağlantı hatası:", error);
      }
    };

    setupSignalR();

    return () => {
      unsubscribers.forEach(unsub => typeof unsub === "function" && unsub());
    };
  }, []);

  // =========================================================================
  // DROPDOWN DIŞ TIKLAMA
  // =========================================================================

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // =========================================================================
  // BİLDİRİM HANDLERLERİ
  // =========================================================================

  /**
   * Bildirim ekle
   */
  const addNotification = useCallback((notification) => {
    const newNotification = {
      id: Date.now() + Math.random(),
      timestamp: new Date().toISOString(),
      read: false,
      ...notification
    };

    setNotifications(prev => [newNotification, ...prev].slice(0, 50)); // Max 50 bildirim
  }, []);

  /**
   * Yeni sipariş
   */
  const handleNewOrder = useCallback((data) => {
    addNotification({
      type: NotificationType.NEW_ORDER,
      title: "Yeni Sipariş",
      message: `#${data.orderId} - ${data.customerName || "Müşteri"}`,
      data: data
    });
  }, [addNotification]);

  /**
   * Sipariş durumu değişikliği
   */
  const handleOrderStatusChange = useCallback((data) => {
    addNotification({
      type: NotificationType.ORDER_STATUS,
      title: "Sipariş Güncellendi",
      message: `#${data.orderId} - ${data.newStatus}`,
      data: data
    });
  }, [addNotification]);

  /**
   * Teslimat atandı
   */
  const handleDeliveryAssigned = useCallback((data) => {
    addNotification({
      type: NotificationType.DELIVERY_ASSIGNED,
      title: "Teslimat Atandı",
      message: `#${data.orderId} → ${data.courierName || "Kurye"}`,
      data: data
    });
  }, [addNotification]);

  /**
   * Teslimat durumu değişikliği
   */
  const handleDeliveryStatusChange = useCallback((data) => {
    addNotification({
      type: NotificationType.DELIVERY_STATUS,
      title: "Teslimat Güncellendi",
      message: `Görev #${data.taskId} - ${data.newStatus}`,
      data: data
    });
  }, [addNotification]);

  /**
   * Kurye online
   */
  const handleCourierOnline = useCallback((data) => {
    addNotification({
      type: NotificationType.COURIER_ONLINE,
      title: "Kurye Çevrimiçi",
      message: `${data.courierName} aktif oldu`,
      data: data
    });
  }, [addNotification]);

  /**
   * Kurye offline
   */
  const handleCourierOffline = useCallback((data) => {
    addNotification({
      type: NotificationType.COURIER_OFFLINE,
      title: "Kurye Çevrimdışı",
      message: `${data.courierName} çevrimdışı oldu`,
      data: data
    });
  }, [addNotification]);

  // =========================================================================
  // EYLEMLER
  // =========================================================================

  /**
   * Bildirimi okundu olarak işaretle
   */
  const markAsRead = useCallback((notificationId) => {
    setNotifications(prev =>
      prev.map(n => n.id === notificationId ? { ...n, read: true } : n)
    );
  }, []);

  /**
   * Tümünü okundu işaretle
   */
  const markAllAsRead = useCallback(() => {
    setNotifications(prev => prev.map(n => ({ ...n, read: true })));
  }, []);

  /**
   * Bildirimleri temizle
   */
  const clearAll = useCallback(() => {
    setNotifications([]);
    setIsOpen(false);
  }, []);

  /**
   * Bildirime tıklama
   */
  const handleNotificationClick = useCallback((notification) => {
    markAsRead(notification.id);
    setIsOpen(false);

    // Türe göre yönlendirme
    switch (notification.type) {
      case NotificationType.NEW_ORDER:
      case NotificationType.ORDER_STATUS:
        navigate(`/admin/orders`);
        break;
      case NotificationType.DELIVERY_ASSIGNED:
      case NotificationType.DELIVERY_STATUS:
        navigate(`/admin/delivery-tasks`);
        break;
      case NotificationType.COURIER_ONLINE:
      case NotificationType.COURIER_OFFLINE:
        navigate(`/admin/couriers`);
        break;
      default:
        break;
    }
  }, [markAsRead, navigate]);

  // =========================================================================
  // YARDIMCI FONKSİYONLAR
  // =========================================================================

  /**
   * Zaman formatlama (relative)
   */
  const formatTimeAgo = (timestamp) => {
    const now = new Date();
    const time = new Date(timestamp);
    const diffMs = now - time;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Şimdi";
    if (diffMins < 60) return `${diffMins} dk önce`;
    if (diffHours < 24) return `${diffHours} saat önce`;
    return `${diffDays} gün önce`;
  };

  /**
   * Bildirim konfigürasyonu al
   */
  const getConfig = (type) => {
    return NotificationConfig[type] || NotificationConfig[NotificationType.SYSTEM];
  };

  // =========================================================================
  // RENDER
  // =========================================================================

  return (
    <div className="position-relative" ref={dropdownRef}>
      {/* Bildirim Çanı Butonu */}
      <button
        className="btn btn-link position-relative p-2"
        onClick={() => setIsOpen(!isOpen)}
        style={{ color: "#6c757d" }}
        title={isConnected ? "Bildirimler (Canlı)" : "Bildirimler (Bağlanıyor...)"}
      >
        <i className="fas fa-bell" style={{ fontSize: "1.1rem" }}></i>
        
        {/* Okunmamış Sayısı Badge */}
        {unreadCount > 0 && (
          <span
            className="position-absolute badge rounded-pill bg-danger"
            style={{
              top: "2px",
              right: "2px",
              fontSize: "0.6rem",
              padding: "0.25em 0.45em",
              minWidth: "18px"
            }}
          >
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}

        {/* Bağlantı Göstergesi */}
        <span
          className={`position-absolute rounded-circle ${isConnected ? "bg-success" : "bg-warning"}`}
          style={{
            width: "8px",
            height: "8px",
            bottom: "5px",
            right: "5px",
            border: "1px solid white"
          }}
        ></span>
      </button>

      {/* Dropdown Menü */}
      {isOpen && (
        <div
          className="position-absolute bg-white shadow-lg rounded-3 border-0"
          style={{
            right: 0,
            top: "100%",
            width: "320px",
            maxWidth: "calc(100vw - 20px)",
            zIndex: 1050,
            animation: "fadeIn 0.2s ease"
          }}
        >
          {/* Header */}
          <div className="d-flex justify-content-between align-items-center p-3 border-bottom">
            <h6 className="mb-0 fw-bold" style={{ fontSize: "0.9rem" }}>
              <i className="fas fa-bell me-2 text-primary"></i>
              Bildirimler
              {unreadCount > 0 && (
                <span className="badge bg-primary ms-2" style={{ fontSize: "0.7rem" }}>
                  {unreadCount} yeni
                </span>
              )}
            </h6>
            <div className="d-flex gap-2">
              {notifications.length > 0 && (
                <>
                  <button
                    className="btn btn-sm btn-link text-muted p-0"
                    onClick={markAllAsRead}
                    title="Tümünü okundu işaretle"
                    style={{ fontSize: "0.75rem" }}
                  >
                    <i className="fas fa-check-double"></i>
                  </button>
                  <button
                    className="btn btn-sm btn-link text-danger p-0"
                    onClick={clearAll}
                    title="Tümünü temizle"
                    style={{ fontSize: "0.75rem" }}
                  >
                    <i className="fas fa-trash"></i>
                  </button>
                </>
              )}
            </div>
          </div>

          {/* Bildirim Listesi */}
          <div style={{ maxHeight: "350px", overflowY: "auto" }}>
            {notifications.length === 0 ? (
              <div className="text-center py-4 text-muted">
                <i className="fas fa-bell-slash fa-2x mb-2 opacity-50"></i>
                <p className="mb-0" style={{ fontSize: "0.85rem" }}>
                  Henüz bildirim yok
                </p>
                <small className="text-muted">
                  Yeni bildirimler burada görünecek
                </small>
              </div>
            ) : (
              notifications.map((notification) => {
                const config = getConfig(notification.type);
                return (
                  <div
                    key={notification.id}
                    className={`p-3 border-bottom cursor-pointer ${!notification.read ? "bg-light" : ""}`}
                    onClick={() => handleNotificationClick(notification)}
                    style={{
                      cursor: "pointer",
                      transition: "background-color 0.2s",
                      borderLeft: !notification.read ? `3px solid var(--bs-${config.color})` : "none"
                    }}
                    onMouseEnter={(e) => e.currentTarget.style.backgroundColor = "#f8f9fa"}
                    onMouseLeave={(e) => e.currentTarget.style.backgroundColor = notification.read ? "" : "#f8f9fa"}
                  >
                    <div className="d-flex align-items-start gap-2">
                      {/* İkon */}
                      <div
                        className={`rounded-circle d-flex align-items-center justify-content-center flex-shrink-0`}
                        style={{
                          width: "36px",
                          height: "36px",
                          backgroundColor: config.bgColor
                        }}
                      >
                        <i className={`${config.icon} text-${config.color}`} style={{ fontSize: "0.85rem" }}></i>
                      </div>

                      {/* İçerik */}
                      <div className="flex-grow-1 overflow-hidden">
                        <div className="d-flex justify-content-between align-items-start">
                          <span 
                            className={`fw-semibold ${!notification.read ? "text-dark" : "text-muted"}`}
                            style={{ fontSize: "0.8rem" }}
                          >
                            {notification.title}
                          </span>
                          <small 
                            className="text-muted flex-shrink-0 ms-2"
                            style={{ fontSize: "0.65rem" }}
                          >
                            {formatTimeAgo(notification.timestamp)}
                          </small>
                        </div>
                        <p 
                          className="mb-0 text-muted text-truncate"
                          style={{ fontSize: "0.75rem" }}
                        >
                          {notification.message}
                        </p>
                      </div>

                      {/* Okunmadı İşareti */}
                      {!notification.read && (
                        <span
                          className={`rounded-circle bg-${config.color} flex-shrink-0`}
                          style={{ width: "8px", height: "8px", marginTop: "6px" }}
                        ></span>
                      )}
                    </div>
                  </div>
                );
              })
            )}
          </div>

          {/* Footer */}
          {notifications.length > 0 && (
            <div className="p-2 border-top text-center">
              <button
                className="btn btn-link btn-sm text-primary p-0"
                onClick={() => {
                  setIsOpen(false);
                  navigate("/admin/notifications");
                }}
                style={{ fontSize: "0.8rem" }}
              >
                Tüm bildirimleri görüntüle
                <i className="fas fa-arrow-right ms-1"></i>
              </button>
            </div>
          )}
        </div>
      )}

      {/* CSS Animation */}
      <style>{`
        @keyframes fadeIn {
          from { opacity: 0; transform: translateY(-10px); }
          to { opacity: 1; transform: translateY(0); }
        }
      `}</style>
    </div>
  );
}
