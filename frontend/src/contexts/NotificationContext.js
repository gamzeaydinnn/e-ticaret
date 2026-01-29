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

// Bildirim sesi için key
const SOUND_ENABLED_KEY = "notificationSoundEnabled";

// ============================================================================
// AUDIO CONTEXT UNLOCK MEKANIZMASI
// Browser autoplay politikasını aşmak için kullanıcı etkileşimi gerekiyor
// İlk tıklamada AudioContext'i unlock eder
// ============================================================================
let audioContextUnlocked = false;
let sharedAudioContext = null;

/**
 * AudioContext'i unlock et - browser autoplay politikasını aşmak için
 * NEDEN: Modern browserlar kullanıcı etkileşimi olmadan ses çalmayı engelliyor
 * Bu fonksiyon ilk tıklamada sessiz bir ses çalarak AudioContext'i açar
 */
const unlockAudioContext = () => {
  if (audioContextUnlocked) return Promise.resolve(true);

  return new Promise((resolve) => {
    try {
      // AudioContext oluştur veya mevcut olanı kullan
      if (!sharedAudioContext) {
        sharedAudioContext = new (
          window.AudioContext || window.webkitAudioContext
        )();
      }

      // Suspended durumundaysa resume et
      if (sharedAudioContext.state === "suspended") {
        sharedAudioContext
          .resume()
          .then(() => {
            audioContextUnlocked = true;
            console.log("[NotificationSound] 🔓 AudioContext unlocked");
            resolve(true);
          })
          .catch(() => resolve(false));
      } else {
        audioContextUnlocked = true;
        resolve(true);
      }

      // Sessiz bir ses çal (unlock için)
      const buffer = sharedAudioContext.createBuffer(1, 1, 22050);
      const source = sharedAudioContext.createBufferSource();
      source.buffer = buffer;
      source.connect(sharedAudioContext.destination);
      source.start(0);
    } catch (error) {
      console.warn("[NotificationSound] ⚠️ AudioContext unlock hatası:", error);
      resolve(false);
    }
  });
};

/**
 * Kullanıcı etkileşimi event listener'ı ekle
 * NEDEN: İlk tıklama/dokunma olayında AudioContext'i unlock et
 */
const setupAudioUnlockListener = () => {
  const unlockHandler = () => {
    unlockAudioContext();
    // Bir kez çalıştıktan sonra listener'ı kaldır
    document.removeEventListener("click", unlockHandler);
    document.removeEventListener("touchstart", unlockHandler);
    document.removeEventListener("keydown", unlockHandler);
  };

  document.addEventListener("click", unlockHandler, { once: true });
  document.addEventListener("touchstart", unlockHandler, { once: true });
  document.addEventListener("keydown", unlockHandler, { once: true });
};

// Sayfa yüklendiğinde unlock listener'ı kur
if (typeof window !== "undefined") {
  setupAudioUnlockListener();

  // localStorage'da ses ayarı yoksa varsayılan olarak true yap
  if (localStorage.getItem(SOUND_ENABLED_KEY) === null) {
    localStorage.setItem(SOUND_ENABLED_KEY, "true");
    console.log("[NotificationSound] 📢 Bildirim sesi varsayılan olarak açık");
  }
}

// ============================================================================
// BİLDİRİM SESİ ÇALMA FONKSİYONU
// Browser autoplay politikasını aşmak için kullanıcı etkileşimi gerekebilir
// Ses dosyaları: /public/sounds/ klasöründe
// ============================================================================
const playNotificationSound = (soundType = "new_order") => {
  try {
    // Ses ayarını kontrol et - varsayılan olarak açık (null = true kabul edilir)
    const storedValue = localStorage.getItem(SOUND_ENABLED_KEY);
    const soundEnabled = storedValue === null || storedValue === "true";
    if (!soundEnabled) {
      console.log("[NotificationSound] 🔇 Ses kapalı");
      return;
    }

    // Ses dosyası seç
    const soundFiles = {
      new_order: "/sounds/mixkit-melodic-race-countdown-1955.wav",
      payment: "/sounds/mixkit-bell-notification-933.wav",
      alert: "/sounds/mixkit-happy-bells-notification-937.wav",
      default: "/sounds/mixkit-bell-notification-933.wav",
    };

    const soundFile = soundFiles[soundType] || soundFiles.default;
    const audio = new Audio(soundFile);
    audio.volume = 0.5;

    // Ses çalmayı dene
    audio
      .play()
      .then(() => {
        console.log("[NotificationSound] 🔊 Ses çalındı:", soundType);
      })
      .catch((error) => {
        // Browser autoplay politikası nedeniyle ses çalınamadı
        // Bu durumda sessizce devam et, kullanıcı etkileşimi gerekiyor
        console.warn(
          "[NotificationSound] ⚠️ Ses çalınamadı (autoplay politikası):",
          error.message,
        );

        // Fallback: Web Audio API ile basit beep sesi
        try {
          const audioContext = new (
            window.AudioContext || window.webkitAudioContext
          )();
          const oscillator = audioContext.createOscillator();
          const gainNode = audioContext.createGain();

          oscillator.connect(gainNode);
          gainNode.connect(audioContext.destination);

          oscillator.frequency.value = 800; // Hz
          oscillator.type = "sine";
          gainNode.gain.value = 0.1;

          oscillator.start();
          setTimeout(() => {
            oscillator.stop();
            audioContext.close();
          }, 200);

          console.log("[NotificationSound] 🔊 Fallback beep çalındı");
        } catch (beepError) {
          console.warn("[NotificationSound] ⚠️ Fallback beep de çalınamadı");
        }
      });
  } catch (error) {
    console.error("[NotificationSound] ❌ Ses çalma hatası:", error);
  }
};

/**
 * Bildirim sesini aç/kapa
 * @param {boolean} enabled - Ses açık mı?
 */
export const setSoundEnabled = (enabled) => {
  localStorage.setItem(SOUND_ENABLED_KEY, enabled ? "true" : "false");
  console.log(
    `[NotificationSound] ${enabled ? "🔊 Ses açıldı" : "🔇 Ses kapatıldı"}`,
  );
};

/**
 * Bildirim sesi açık mı kontrol et
 * @returns {boolean}
 */
export const isSoundEnabled = () => {
  const storedValue = localStorage.getItem(SOUND_ENABLED_KEY);
  return storedValue === null || storedValue === "true";
};

/**
 * NotificationProvider - Bildirim yönetimi provider
 *
 * Sağladığı özellikler:
 * - Bildirim listesi yönetimi
 * - Okundu/okunmadı takibi
 * - Toast bildirimleri
 * - Real-time SignalR entegrasyonu
 * - LocalStorage persistence
 * - Ses açma/kapama kontrolü
 */
export const NotificationProvider = ({ children, signalRConnection }) => {
  // State tanımları
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [toasts, setToasts] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [soundEnabled, setSoundEnabledState] = useState(isSoundEnabled());
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

    // ============================================================================
    // SIGNALR EVENT HANDLERS
    // Backend ile frontend arasındaki event isimlerinin eşleşmesi kritik
    // Backend: RealTimeNotificationService.cs'deki SendAsync çağrıları
    // ============================================================================

    // Genel bildirim handler
    const handleNotification = (notification) => {
      addNotification(notification);
    };

    // ============================================================================
    // YENİ SİPARİŞ BİLDİRİMİ (Admin için)
    // Backend: _adminHub.Clients.Group(AdminGroupName).SendAsync("NewOrder", notification)
    // ============================================================================
    const handleNewOrder = (data) => {
      console.log(
        "[NotificationContext] 🔔 Yeni sipariş bildirimi alındı:",
        data,
      );
      const notification = {
        id: data.id || `order-${data.orderId || Date.now()}`,
        type: "order",
        title: "🛒 Yeni Sipariş",
        message: `${data.customerName || "Müşteri"} - ₺${(data.totalAmount || 0).toFixed(2)} (${data.itemCount || 0} ürün)`,
        body: `Sipariş No: ${data.orderNumber || data.orderId}`,
        data: data,
        createdAt: data.timestamp || new Date().toISOString(),
        isRead: false,
        actionUrl: `/admin/orders/${data.orderId}`,
      };
      addNotification(notification);
    };

    // ============================================================================
    // SES BİLDİRİMİ
    // Backend: _adminHub.Clients.Group(AdminGroupName).SendAsync("PlaySound", { soundType, priority })
    // ============================================================================
    const handlePlaySound = (data) => {
      console.log("[NotificationContext] 🔊 Ses bildirimi alındı:", data);
      playNotificationSound(data?.soundType || "new_order");
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

    // Sipariş bildirimi handler (eski format için backward compat)
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

    // ============================================================================
    // ÖDEME BİLDİRİMLERİ
    // ============================================================================
    const handlePaymentSuccess = (data) => {
      console.log("[NotificationContext] 💳 Ödeme başarılı bildirimi:", data);
      const notification = {
        id: data.id || `payment-${data.orderId || Date.now()}`,
        type: "payment",
        title: "💳 Ödeme Başarılı",
        message: `Sipariş #${data.orderNumber} - ₺${(data.amount || 0).toFixed(2)}`,
        data: data,
        createdAt: data.timestamp || new Date().toISOString(),
        isRead: false,
        actionUrl: `/admin/orders/${data.orderId}`,
      };
      addNotification(notification);
    };

    const handlePaymentFailed = (data) => {
      console.log("[NotificationContext] ❌ Ödeme başarısız bildirimi:", data);
      const notification = {
        id: data.id || `payment-failed-${data.orderId || Date.now()}`,
        type: "alert",
        title: "❌ Ödeme Başarısız",
        message: `Sipariş #${data.orderNumber} - ${data.reason || "Bilinmeyen hata"}`,
        data: data,
        createdAt: data.timestamp || new Date().toISOString(),
        isRead: false,
        actionUrl: `/admin/orders/${data.orderId}`,
      };
      addNotification(notification);
    };

    // ============================================================================
    // SİPARİŞ DURUMU DEĞİŞİKLİĞİ
    // Backend: _adminHub.Clients.Group(AdminGroupName).SendAsync("OrderStatusChanged", ...)
    // ============================================================================
    const handleOrderStatusChanged = (data) => {
      console.log("[NotificationContext] 📦 Sipariş durumu değişti:", data);
      const notification = {
        id: data.id || `status-${data.orderId || Date.now()}`,
        type: "order",
        title: "📦 Sipariş Durumu Güncellendi",
        message: `Sipariş #${data.orderNumber} → ${data.newStatus || data.status}`,
        data: data,
        createdAt: data.timestamp || new Date().toISOString(),
        isRead: false,
        actionUrl: `/admin/orders/${data.orderId}`,
      };
      addNotification(notification);
    };

    // ============================================================================
    // EVENT LISTENER'LARI EKLE
    // Backend'deki SendAsync çağrılarındaki event isimleri ile eşleşmeli
    // ============================================================================
    signalRConnection.on("ReceiveNotification", handleNotification);
    signalRConnection.on("NewOrder", handleNewOrder); // Backend: "NewOrder"
    signalRConnection.on("PlaySound", handlePlaySound); // Backend: "PlaySound"
    signalRConnection.on("PaymentSuccess", handlePaymentSuccess); // Backend: "PaymentSuccess"
    signalRConnection.on("PaymentFailed", handlePaymentFailed); // Backend: "PaymentFailed"
    signalRConnection.on("OrderStatusChanged", handleOrderStatusChanged); // Backend: "OrderStatusChanged"
    signalRConnection.on("DeliveryNotification", handleDeliveryNotification);
    signalRConnection.on("CourierNotification", handleCourierNotification);
    signalRConnection.on("OrderNotification", handleOrderNotification);
    signalRConnection.on("NewTaskReceived", handleCourierNotification);
    signalRConnection.on("TaskStatusUpdated", handleDeliveryNotification);

    return () => {
      signalRConnection.off("ReceiveNotification", handleNotification);
      signalRConnection.off("NewOrder", handleNewOrder);
      signalRConnection.off("PlaySound", handlePlaySound);
      signalRConnection.off("PaymentSuccess", handlePaymentSuccess);
      signalRConnection.off("PaymentFailed", handlePaymentFailed);
      signalRConnection.off("OrderStatusChanged", handleOrderStatusChanged);
      signalRConnection.off("DeliveryNotification", handleDeliveryNotification);
      signalRConnection.off("CourierNotification", handleCourierNotification);
      signalRConnection.off("OrderNotification", handleOrderNotification);
      signalRConnection.off("NewTaskReceived", handleCourierNotification);
      signalRConnection.off("TaskStatusUpdated", handleDeliveryNotification);
    };
  }, [signalRConnection, addNotification]);

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

  /**
   * Ses ayarını değiştir
   * NEDEN: Kullanıcı bildirim sesini açıp kapatabilmeli
   */
  const toggleSound = useCallback(
    (enabled) => {
      const newValue = typeof enabled === "boolean" ? enabled : !soundEnabled;
      setSoundEnabled(newValue);
      setSoundEnabledState(newValue);

      // Ses açılırsa AudioContext'i unlock et
      if (newValue) {
        unlockAudioContext();
      }
    },
    [soundEnabled],
  );

  /**
   * Test sesi çal - kullanıcı ses ayarını test edebilsin
   */
  const playTestSound = useCallback(() => {
    playNotificationSound("default");
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

    // Ses kontrolü
    soundEnabled,
    toggleSound,
    playTestSound,
    playNotificationSound,

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
