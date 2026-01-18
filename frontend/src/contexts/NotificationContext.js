// ==========================================================================
// NotificationContext.js - Bildirim Context Provider
// ==========================================================================
// Uygulama genelinde bildirim yönetimi için React Context.
// SignalR entegrasyonu, localStorage persistence ve real-time
// güncellemeler sağlar. Mobil uyumlu toast bildirimleri içerir.
// ==========================================================================

import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  useRef,
} from "react";

// Context oluştur
const NotificationContext = createContext(null);

// Maksimum saklanacak bildirim sayısı
const MAX_NOTIFICATIONS = 50;

// LocalStorage key
const STORAGE_KEY = "notifications";

/**
 * NotificationProvider - Bildirim yönetimi provider
 *
 * Sağladığı özellikler:
 * - Bildirim listesi yönetimi
 * - Okundu/okunmadı takibi
 * - Toast bildirimleri
 * - Real-time SignalR entegrasyonu
 * - LocalStorage persistence
 */
export const NotificationProvider = ({ children, signalRConnection }) => {
  // State tanımları
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [toasts, setToasts] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const toastIdRef = useRef(0);

  // LocalStorage'dan bildirimleri yükle
  useEffect(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const parsed = JSON.parse(stored);
        setNotifications(parsed);
        setUnreadCount(parsed.filter((n) => !n.isRead).length);
      }
    } catch (error) {
      console.error("Bildirimler yüklenirken hata:", error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Bildirimleri LocalStorage'a kaydet
  useEffect(() => {
    if (!isLoading) {
      try {
        localStorage.setItem(
          STORAGE_KEY,
          JSON.stringify(notifications.slice(0, MAX_NOTIFICATIONS)),
        );
      } catch (error) {
        console.error("Bildirimler kaydedilirken hata:", error);
      }
    }
  }, [notifications, isLoading]);

  // Okunmamış sayısını güncelle
  useEffect(() => {
    setUnreadCount(notifications.filter((n) => !n.isRead).length);
  }, [notifications]);

  // SignalR bağlantısını dinle
  useEffect(() => {
    if (!signalRConnection) return;

    // Genel bildirim handler
    const handleNotification = (notification) => {
      addNotification(notification);
    };

    // Teslimat bildirimi handler
    const handleDeliveryNotification = (data) => {
      const notification = {
        id: `delivery-${Date.now()}`,
        type: data.type || "delivery",
        title: data.title || "Teslimat Bildirimi",
        message: data.message,
        body: data.body,
        data: data,
        createdAt: new Date().toISOString(),
        isRead: false,
      };
      addNotification(notification);
    };

    // Kurye bildirimi handler
    const handleCourierNotification = (data) => {
      const notification = {
        id: `courier-${Date.now()}`,
        type: data.type || "courier_update",
        title: data.title || "Kurye Bildirimi",
        message: data.message,
        body: data.body,
        data: data,
        createdAt: new Date().toISOString(),
        isRead: false,
      };
      addNotification(notification);
    };

    // Sipariş bildirimi handler
    const handleOrderNotification = (data) => {
      const notification = {
        id: `order-${Date.now()}`,
        type: data.type || "new_order",
        title: data.title || "Sipariş Bildirimi",
        message: data.message,
        body: data.body,
        data: data,
        createdAt: new Date().toISOString(),
        isRead: false,
      };
      addNotification(notification);
    };

    // Event listener'ları ekle
    signalRConnection.on("ReceiveNotification", handleNotification);
    signalRConnection.on("DeliveryNotification", handleDeliveryNotification);
    signalRConnection.on("CourierNotification", handleCourierNotification);
    signalRConnection.on("OrderNotification", handleOrderNotification);
    signalRConnection.on("NewTaskReceived", handleCourierNotification);
    signalRConnection.on("TaskStatusUpdated", handleDeliveryNotification);

    return () => {
      signalRConnection.off("ReceiveNotification", handleNotification);
      signalRConnection.off("DeliveryNotification", handleDeliveryNotification);
      signalRConnection.off("CourierNotification", handleCourierNotification);
      signalRConnection.off("OrderNotification", handleOrderNotification);
      signalRConnection.off("NewTaskReceived", handleCourierNotification);
      signalRConnection.off("TaskStatusUpdated", handleDeliveryNotification);
    };
  }, [signalRConnection]);

  /**
   * Yeni bildirim ekle
   */
  const addNotification = useCallback((notification) => {
    const newNotification = {
      id:
        notification.id ||
        `notif-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      type: notification.type || "info",
      title: notification.title,
      message: notification.message,
      body: notification.body,
      data: notification.data,
      createdAt: notification.createdAt || new Date().toISOString(),
      isRead: notification.isRead || false,
      actionUrl: notification.actionUrl,
    };

    setNotifications((prev) => {
      // Duplicate kontrolü
      if (prev.some((n) => n.id === newNotification.id)) {
        return prev;
      }
      // Maksimum sayıya ulaşıldıysa eski bildirimleri sil
      const updated = [newNotification, ...prev].slice(0, MAX_NOTIFICATIONS);
      return updated;
    });

    // Toast göster
    if (!notification.silent) {
      showToast({
        type: notification.type,
        title: notification.title,
        message: notification.message || notification.body,
        duration: notification.duration || 5000,
      });
    }

    // Browser notification (izin varsa)
    if (
      "Notification" in window &&
      Notification.permission === "granted" &&
      !notification.silent
    ) {
      new Notification(notification.title || "Bildirim", {
        body: notification.message || notification.body,
        icon: "/icons/notification-icon.png",
        badge: "/icons/badge-icon.png",
        tag: newNotification.id,
        renotify: true,
      });
    }
  }, []);

  /**
   * Bildirimi okundu olarak işaretle
   */
  const markAsRead = useCallback((notificationId) => {
    setNotifications((prev) =>
      prev.map((n) => (n.id === notificationId ? { ...n, isRead: true } : n)),
    );
  }, []);

  /**
   * Tüm bildirimleri okundu olarak işaretle
   */
  const markAllAsRead = useCallback(() => {
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
  }, []);

  /**
   * Bildirimi sil
   */
  const removeNotification = useCallback((notificationId) => {
    setNotifications((prev) => prev.filter((n) => n.id !== notificationId));
  }, []);

  /**
   * Bildirimi sil (alias for NotificationBell compatibility)
   */
  const deleteNotification = useCallback(
    (notificationId) => {
      removeNotification(notificationId);
    },
    [removeNotification],
  );

  /**
   * Tüm bildirimleri temizle
   */
  const clearAllNotifications = useCallback(() => {
    setNotifications([]);
  }, []);

  /**
   * Toast bildirim göster
   */
  const showToast = useCallback(
    ({ type = "info", title, message, duration = 5000, action }) => {
      const id = `toast-${++toastIdRef.current}`;

      const toast = {
        id,
        type,
        title,
        message,
        action,
        createdAt: Date.now(),
      };

      setToasts((prev) => [...prev, toast]);

      // Otomatik kaldırma
      if (duration > 0) {
        setTimeout(() => {
          dismissToast(id);
        }, duration);
      }

      return id;
    },
    [],
  );

  /**
   * Toast'ı kapat
   */
  const dismissToast = useCallback((toastId) => {
    setToasts((prev) => prev.filter((t) => t.id !== toastId));
  }, []);

  /**
   * Tüm toast'ları kapat
   */
  const dismissAllToasts = useCallback(() => {
    setToasts([]);
  }, []);

  /**
   * Browser notification izni iste
   */
  const requestNotificationPermission = useCallback(async () => {
    if (!("Notification" in window)) {
      console.warn("Bu tarayıcı bildirim desteklemiyor");
      return false;
    }

    if (Notification.permission === "granted") {
      return true;
    }

    if (Notification.permission !== "denied") {
      const permission = await Notification.requestPermission();
      return permission === "granted";
    }

    return false;
  }, []);

  // Context değeri
  const value = {
    // Bildirimler
    notifications,
    unreadCount,
    loading: isLoading,
    isLoading,

    // Bildirim işlemleri
    addNotification,
    markAsRead,
    markAllAsRead,
    removeNotification,
    deleteNotification,
    clearAllNotifications,

    // Toast işlemleri
    toasts,
    showToast,
    dismissToast,
    dismissAllToasts,
    removeToast: dismissToast,

    // Yardımcı
    requestNotificationPermission,
  };

  return (
    <NotificationContext.Provider value={value}>
      {children}
    </NotificationContext.Provider>
  );
};

/**
 * useNotifications - Bildirim context hook
 */
export const useNotifications = () => {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error(
      "useNotifications must be used within a NotificationProvider",
    );
  }
  return context;
};

export default NotificationContext;
