// ==========================================================================
// AdminSignalRContext.js - Admin Paneli Merkezi SignalR Bağlantı Yönetimi
// ==========================================================================
// Tüm admin kullanıcıları için tek bir SignalR bağlantı konteksti.
// Birden fazla admin aynı anda giriş yaptığında her biri kendi bağlantısını
// kurar ve "admin-notifications" grubuna katılır.
// Backend tarafında grup tabanlı yayın yapıldığı için tüm aktif adminler
// aynı bildirimleri eşzamanlı olarak alır.
//
// NEDEN Context: Her admin sayfasının bağımsız bağlantı kurması yerine,
// tek bir merkezi bağlantı üzerinden tüm sayfalar bildirim alır.
// Sayfa değişikliklerinde bağlantı korunur, bildirim state'i kaybolmaz.
// ==========================================================================

import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  useRef,
} from "react";
import {
  signalRService,
  SignalREvents,
  ConnectionState,
} from "../services/signalRService";
import { useAuth } from "./AuthContext";
import { isSoundEnabled } from "./NotificationContext";

// Context tanımı
const AdminSignalRContext = createContext(null);

// ============================================================================
// BİLDİRİM SESİ YARDIMCISI
// NEDEN: Merkezi ses yönetimi — tüm admin bildirimleri için tek nokta
// ============================================================================
const playNotificationSound = (soundType = "new_order") => {
  try {
    if (!isSoundEnabled()) return;

    const soundFiles = {
      new_order: "/sounds/mixkit-melodic-race-countdown-1955.wav",
      alert: "/sounds/mixkit-happy-bells-notification-937.wav",
      default: "/sounds/mixkit-bell-notification-933.wav",
    };

    const soundFile = soundFiles[soundType] || soundFiles.default;
    const audio = new Audio(soundFile);
    audio.volume = 0.5;
    audio.play().catch(() => {
      // Kullanıcı etkileşimi olmadan ses çalınamaz, sessizce devam
    });
  } catch (error) {
    console.error("[AdminSignalR] Ses çalma hatası:", error);
  }
};

// ============================================================================
// ADMIN ROLLERİ — Bu roller admin bağlantısı kurabilir
// NEDEN: Yetki dışı kullanıcıların SignalR bağlantısı denemesini engelle
// ============================================================================
const ADMIN_ROLES = [
  "Admin",
  "SuperAdmin",
  "StoreManager",
  "CustomerSupport",
  "Logistics",
  "StoreAttendant",
  "Dispatcher",
];

// ============================================================================
// PROVIDER BİLEŞENİ
// ============================================================================
export function AdminSignalRProvider({ children }) {
  const { user, isAuthenticated } = useAuth();

  // Bağlantı durumu
  const [connectionState, setConnectionState] = useState("disconnected");
  // Bildirim listesi — tüm admin sayfaları arasında paylaşılır
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);

  // Bağlantı referansı — reconnect ve cleanup için
  const isConnectingRef = useRef(false);
  const isConnectedRef = useRef(false);
  // Event handler'ları cleanup için takip et
  const cleanupRef = useRef([]);
  // Cross-tab senkronizasyon kanalı
  const broadcastChannelRef = useRef(null);

  // Kullanıcının admin rolüne sahip olup olmadığını kontrol et
  const isAdminUser = user && ADMIN_ROLES.includes(user.role);

  // ==========================================================================
  // CROSS-TAB BİLDİRİM SENKRONİZASYONU (BroadcastChannel API)
  // NEDEN: Aynı domain'de birden fazla admin sekmesi açıkken bildirimlerin
  // tüm sekmelerde eşzamanlı güncellenmesini sağlar
  // ==========================================================================
  useEffect(() => {
    if (typeof BroadcastChannel === "undefined") return;

    const channel = new BroadcastChannel("admin-notifications-sync");
    broadcastChannelRef.current = channel;

    channel.onmessage = (event) => {
      const { type, payload } = event.data;

      switch (type) {
        case "NEW_NOTIFICATION":
          // Diğer sekmeden gelen bildirimi ekle (ses çalma — sadece orijinal sekme çalar)
          setNotifications((prev) => {
            // Duplikasyon kontrolü
            if (prev.some((n) => n.id === payload.id)) return prev;
            return [payload, ...prev].slice(0, 100);
          });
          setUnreadCount((prev) => prev + 1);
          break;
        case "MARK_READ":
          setNotifications((prev) =>
            prev.map((n) =>
              n.id === payload.notificationId ? { ...n, read: true } : n,
            ),
          );
          setUnreadCount((prev) => Math.max(0, prev - 1));
          break;
        case "MARK_ALL_READ":
          setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
          setUnreadCount(0);
          break;
        case "CLEAR_ALL":
          setNotifications([]);
          setUnreadCount(0);
          break;
        default:
          break;
      }
    };

    return () => {
      channel.close();
      broadcastChannelRef.current = null;
    };
  }, []);

  // ==========================================================================
  // BİLDİRİM YÖNETİMİ
  // ==========================================================================

  /**
   * Yeni bildirim ekle
   * NEDEN: Tüm admin event'lerini tek bir bildirim listesinde topla
   */
  const addNotification = useCallback((notification) => {
    const newNotification = {
      id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      timestamp: new Date().toISOString(),
      read: false,
      ...notification,
    };

    setNotifications((prev) => [newNotification, ...prev].slice(0, 100));
    setUnreadCount((prev) => prev + 1);

    // Cross-tab senkronizasyon: diğer sekmelere bildir
    try {
      broadcastChannelRef.current?.postMessage({
        type: "NEW_NOTIFICATION",
        payload: newNotification,
      });
    } catch (e) {
      // BroadcastChannel desteklenmiyorsa sessizce geç
    }

    // Browser Notification API desteği
    try {
      if ("Notification" in window && Notification.permission === "granted") {
        new Notification(notification.title || "Yeni Bildirim", {
          body: notification.message || "",
          icon: "/favicon.ico",
          tag: newNotification.id,
        });
      }
    } catch (e) {
      // Notification API desteklenmiyorsa sessizce geç
    }
  }, []);

  const markAsRead = useCallback((notificationId) => {
    setNotifications((prev) =>
      prev.map((n) => (n.id === notificationId ? { ...n, read: true } : n)),
    );
    setUnreadCount((prev) => Math.max(0, prev - 1));
    // Cross-tab senkronizasyon
    try {
      broadcastChannelRef.current?.postMessage({
        type: "MARK_READ",
        payload: { notificationId },
      });
    } catch (e) {}
  }, []);

  const markAllAsRead = useCallback(() => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
    setUnreadCount(0);
    // Cross-tab senkronizasyon
    try {
      broadcastChannelRef.current?.postMessage({ type: "MARK_ALL_READ" });
    } catch (e) {}
  }, []);

  const clearNotifications = useCallback(() => {
    setNotifications([]);
    setUnreadCount(0);
    // Cross-tab senkronizasyon
    try {
      broadcastChannelRef.current?.postMessage({ type: "CLEAR_ALL" });
    } catch (e) {}
  }, []);

  // ==========================================================================
  // SIGNALR EVENT HANDLERLAR
  // NEDEN: Backend'den gelen tüm admin event'lerini yakalayarak bildirime dönüştür
  // ==========================================================================

  const registerEventHandlers = useCallback(() => {
    const adminHub = signalRService.adminHub;
    const deliveryHub = signalRService.deliveryHub;
    const unsubs = [];

    // --- Admin Hub Event'leri ---

    // Yeni sipariş bildirimi
    const handleNewOrder = (data) => {
      console.log("[AdminSignalR] Yeni sipariş:", data);
      playNotificationSound("new_order");
      addNotification({
        type: "new_order",
        title: "Yeni Sipariş",
        message: `#${data.orderNumber || data.orderId || ""} numaralı yeni sipariş alındı`,
        data,
      });
      // Diğer bileşenlerin dinleyebilmesi için global event yayınla
      window.dispatchEvent(new CustomEvent("adminNewOrder", { detail: data }));
    };
    unsubs.push(adminHub.on("NewOrder", handleNewOrder));

    // Sipariş durumu değişikliği
    const handleOrderStatusChanged = (data) => {
      console.log("[AdminSignalR] Sipariş durumu değişti:", data);
      addNotification({
        type: "order_status",
        title: "Sipariş Güncellendi",
        message: `#${data.orderId} durumu: ${data.newStatus}`,
        data,
      });
      window.dispatchEvent(
        new CustomEvent("adminOrderStatusChanged", { detail: data }),
      );
    };
    unsubs.push(adminHub.on("OrderStatusChanged", handleOrderStatusChanged));

    // Ses bildirimi — backend tarafından tetiklenir
    const handlePlaySound = (data) => {
      console.log("[AdminSignalR] Backend ses bildirimi:", data);
      playNotificationSound(data?.soundType || "new_order");
    };
    unsubs.push(adminHub.on("PlaySound", handlePlaySound));

    // Ödeme başarılı
    const handlePaymentSuccess = (data) => {
      console.log("[AdminSignalR] Ödeme başarılı:", data);
      addNotification({
        type: "payment_success",
        title: "Ödeme Alındı",
        message: `#${data.orderId || data.orderNumber || ""} ödemesi başarılı`,
        data,
      });
      window.dispatchEvent(
        new CustomEvent("adminPaymentSuccess", { detail: data }),
      );
    };
    unsubs.push(adminHub.on("PaymentSuccess", handlePaymentSuccess));

    // Ödeme başarısız
    const handlePaymentFailed = (data) => {
      console.log("[AdminSignalR] Ödeme başarısız:", data);
      playNotificationSound("alert");
      addNotification({
        type: "payment_failed",
        title: "Ödeme Hatası",
        message: `#${data.orderId || data.orderNumber || ""} ödemesi başarısız`,
        data,
      });
      window.dispatchEvent(
        new CustomEvent("adminPaymentFailed", { detail: data }),
      );
    };
    unsubs.push(adminHub.on("PaymentFailed", handlePaymentFailed));

    // Teslimat sorunu
    const handleDeliveryProblem = (data) => {
      console.log("[AdminSignalR] Teslimat sorunu:", data);
      playNotificationSound("alert");
      addNotification({
        type: "delivery_problem",
        title: "Teslimat Sorunu",
        message: data.message || `Sipariş #${data.orderId} teslimat problemi`,
        data,
      });
      window.dispatchEvent(
        new CustomEvent("adminDeliveryProblem", { detail: data }),
      );
    };
    unsubs.push(adminHub.on("DeliveryProblem", handleDeliveryProblem));

    // Sipariş iptali
    const handleOrderCancelled = (data) => {
      console.log("[AdminSignalR] Sipariş iptal:", data);
      addNotification({
        type: "order_cancelled",
        title: "Sipariş İptal Edildi",
        message: `#${data.orderId || data.orderNumber || ""} iptal edildi`,
        data,
      });
      window.dispatchEvent(
        new CustomEvent("adminOrderCancelled", { detail: data }),
      );
    };
    unsubs.push(adminHub.on("OrderCancelled", handleOrderCancelled));

    // İade talebi
    const handleRefundRequested = (data) => {
      console.log("[AdminSignalR] İade talebi:", data);
      addNotification({
        type: "refund_requested",
        title: "İade Talebi",
        message: `#${data.orderId || ""} için iade talebi alındı`,
        data,
      });
      window.dispatchEvent(
        new CustomEvent("adminRefundRequested", { detail: data }),
      );
    };
    unsubs.push(adminHub.on("RefundRequested", handleRefundRequested));

    // Düşük stok uyarısı
    const handleLowStock = (data) => {
      console.log("[AdminSignalR] Düşük stok:", data);
      addNotification({
        type: "low_stock",
        title: "Düşük Stok Uyarısı",
        message: data.message || `${data.productName || "Ürün"} stok düşük`,
        data,
      });
    };
    unsubs.push(adminHub.on("LowStock", handleLowStock));

    // Admin genel uyarı
    const handleAdminAlert = (data) => {
      console.log("[AdminSignalR] Admin uyarısı:", data);
      playNotificationSound("alert");
      addNotification({
        type: "admin_alert",
        title: data.title || "Sistem Uyarısı",
        message: data.message || "Dikkat gerektiren bir durum var",
        data,
      });
    };
    unsubs.push(adminHub.on("AdminAlert", handleAdminAlert));

    // Dashboard güncelleme
    const handleDashboardUpdate = (data) => {
      console.log("[AdminSignalR] Dashboard güncellemesi:", data);
      window.dispatchEvent(
        new CustomEvent("adminDashboardUpdate", { detail: data }),
      );
    };
    unsubs.push(adminHub.on("DashboardUpdate", handleDashboardUpdate));

    // SLA ihlali
    const handleSlaViolation = (data) => {
      console.log("[AdminSignalR] SLA ihlali:", data);
      playNotificationSound("alert");
      addNotification({
        type: "sla_violation",
        title: "SLA İhlali",
        message: data.message || `Sipariş #${data.orderId} SLA süresi aşıldı`,
        data,
      });
    };
    unsubs.push(adminHub.on("SlaViolation", handleSlaViolation));

    // Teslimat takılı
    const handleDeliveryStuck = (data) => {
      console.log("[AdminSignalR] Teslimat takılı:", data);
      addNotification({
        type: "delivery_stuck",
        title: "Teslimat Takılı",
        message: data.message || `Sipariş #${data.orderId} teslimatı durdu`,
        data,
      });
    };
    unsubs.push(adminHub.on("DeliveryStuck", handleDeliveryStuck));

    // Kurye çevrimdışı
    const handleCourierOffline = (data) => {
      console.log("[AdminSignalR] Kurye offline:", data);
      addNotification({
        type: "courier_offline",
        title: "Kurye Çevrimdışı",
        message: `${data.courierName || "Kurye"} çevrimdışı oldu`,
        data,
      });
    };
    unsubs.push(adminHub.on("CourierOffline", handleCourierOffline));

    // Kurye durum değişikliği
    const handleCourierStatusChanged = (data) => {
      console.log("[AdminSignalR] Kurye durumu değişti:", data);
      window.dispatchEvent(
        new CustomEvent("adminCourierStatusChanged", { detail: data }),
      );
    };
    unsubs.push(
      adminHub.on("CourierStatusChanged", handleCourierStatusChanged),
    );

    // Ağırlık farkı tahsilatı
    const handleWeightCharge = (data) => {
      console.log("[AdminSignalR] Ağırlık farkı:", data);
      addNotification({
        type: "weight_charge",
        title: "Ağırlık Farkı",
        message: data.message || `Ağırlık farkı tahsilatı uygulandı`,
        data,
      });
    };
    unsubs.push(adminHub.on("WeightChargeApplied", handleWeightCharge));

    // Admin katılım bildirimi — başka adminler bağlandığında
    const handleAdminJoined = (data) => {
      console.log("[AdminSignalR] Başka admin katıldı:", data);
    };
    unsubs.push(adminHub.on("AdminJoined", handleAdminJoined));

    // --- Delivery Hub Event'leri ---

    // Sipariş oluşturuldu (delivery hub)
    const handleOrderCreated = (data) => {
      console.log("[AdminSignalR] Delivery - Sipariş oluşturuldu:", data);
      window.dispatchEvent(
        new CustomEvent("adminDeliveryOrderCreated", { detail: data }),
      );
    };
    unsubs.push(
      deliveryHub.on(SignalREvents.ORDER_CREATED, handleOrderCreated),
    );

    // Delivery durumu değişti
    const handleDeliveryStatusChanged = (data) => {
      console.log("[AdminSignalR] Delivery - Durum değişti:", data);
      window.dispatchEvent(
        new CustomEvent("adminDeliveryStatusChanged", { detail: data }),
      );
    };
    unsubs.push(
      deliveryHub.on(
        SignalREvents.ORDER_STATUS_CHANGED,
        handleDeliveryStatusChanged,
      ),
    );

    // Kurye atandı (delivery hub)
    const handleDeliveryAssigned = (data) => {
      console.log("[AdminSignalR] Delivery - Kurye atandı:", data);
      addNotification({
        type: "delivery_assigned",
        title: "Kurye Atandı",
        message: `#${data.orderId} siparişine ${data.courierName || "kurye"} atandı`,
        data,
      });
      window.dispatchEvent(
        new CustomEvent("adminDeliveryAssigned", { detail: data }),
      );
    };
    unsubs.push(
      deliveryHub.on(SignalREvents.DELIVERY_ASSIGNED, handleDeliveryAssigned),
    );

    // Teslimat tamamlandı (delivery hub)
    const handleDeliveryCompleted = (data) => {
      console.log("[AdminSignalR] Delivery - Tamamlandı:", data);
      addNotification({
        type: "delivery_completed",
        title: "Teslimat Tamamlandı",
        message: `#${data.orderId} siparişi teslim edildi`,
        data,
      });
      window.dispatchEvent(
        new CustomEvent("adminDeliveryCompleted", { detail: data }),
      );
    };
    unsubs.push(
      deliveryHub.on(SignalREvents.DELIVERY_COMPLETED, handleDeliveryCompleted),
    );

    // Teslimat başarısız (delivery hub)
    const handleDeliveryFailed = (data) => {
      console.log("[AdminSignalR] Delivery - Başarısız:", data);
      playNotificationSound("alert");
      addNotification({
        type: "delivery_failed",
        title: "Teslimat Başarısız",
        message: `#${data.orderId} teslimatı başarısız${data.reason ? ": " + data.reason : ""}`,
        data,
      });
      window.dispatchEvent(
        new CustomEvent("adminDeliveryFailed", { detail: data }),
      );
    };
    unsubs.push(
      deliveryHub.on(SignalREvents.DELIVERY_FAILED, handleDeliveryFailed),
    );

    // Yeni teslimat görevi oluşturuldu (delivery hub)
    const handleDeliveryCreated = (data) => {
      console.log("[AdminSignalR] Delivery - Yeni görev:", data);
      window.dispatchEvent(
        new CustomEvent("adminDeliveryCreated", { detail: data }),
      );
    };
    unsubs.push(
      deliveryHub.on(SignalREvents.DELIVERY_CREATED, handleDeliveryCreated),
    );

    // Cleanup referansını güncelle
    cleanupRef.current = unsubs;

    return unsubs;
  }, [addNotification]);

  // ==========================================================================
  // BAĞLANTI LİFECYCLE
  // NEDEN: Auth durumu değiştiğinde bağlantıyı kur/kes
  // Birden fazla admin kullanıcı aynı anda bağlanabilir — her biri
  // kendi connection ID'si ile aynı "admin-notifications" grubuna katılır
  // ==========================================================================

  useEffect(() => {
    // Koşul: Authenticated admin kullanıcı değilse bağlantı kurma
    if (!isAuthenticated || !isAdminUser) {
      // Bağlantı varsa temizle
      if (isConnectedRef.current) {
        cleanupRef.current.forEach((unsub) => {
          if (typeof unsub === "function") unsub();
        });
        cleanupRef.current = [];
        isConnectedRef.current = false;
        setConnectionState("disconnected");
      }
      return;
    }

    // Zaten bağlıysa veya bağlanma devam ediyorsa tekrar bağlanma
    if (isConnectedRef.current || isConnectingRef.current) return;

    const connect = async () => {
      isConnectingRef.current = true;
      setConnectionState("connecting");

      try {
        const connected = await signalRService.connectAdmin();

        if (connected) {
          setConnectionState("connected");
          isConnectedRef.current = true;

          // Event handler'ları kaydet
          registerEventHandlers();

          // Bağlantı durumu değişikliklerini dinle
          const adminHub = signalRService.adminHub;
          const deliveryHub = signalRService.deliveryHub;

          adminHub.onStateChange((newState) => {
            setConnectionState(newState);
            isConnectedRef.current = newState === ConnectionState.CONNECTED;

            // Yeniden bağlanma sonrası admin grubuna tekrar katıl
            if (newState === ConnectionState.CONNECTED) {
              signalRService.adminHub
                .invoke("JoinAdminRoom")
                .catch((err) =>
                  console.warn(
                    "[AdminSignalR] Gruba yeniden katılma hatası:",
                    err,
                  ),
                );
            }
          });

          deliveryHub.onStateChange((newState) => {
            // Delivery hub durumunu da takip et (bağlantı kopmasına karşı)
            if (newState === ConnectionState.DISCONNECTED) {
              console.warn("[AdminSignalR] Delivery Hub bağlantısı koptu");
            }
          });

          console.log(
            "[AdminSignalR] Bağlantı kuruldu — kullanıcı:",
            user?.name,
            "rol:",
            user?.role,
          );
        } else {
          setConnectionState("failed");
          console.warn("[AdminSignalR] Bağlantı kurulamadı");
        }
      } catch (error) {
        console.error("[AdminSignalR] Bağlantı hatası:", error);
        setConnectionState("failed");
      } finally {
        isConnectingRef.current = false;
      }
    };

    connect();

    // Cleanup — component unmount olduğunda event handler'ları temizle
    return () => {
      cleanupRef.current.forEach((unsub) => {
        if (typeof unsub === "function") unsub();
      });
      cleanupRef.current = [];
      // NOT: signalRService singleton olduğu için bağlantıyı kapatmıyoruz.
      // Kullanıcı farklı admin sayfasına geçtiğinde aynı bağlantı kullanılır.
      // Bağlantı sadece logout veya session timeout'ta kapanır.
    };
  }, [isAuthenticated, isAdminUser, user?.role, registerEventHandlers]);

  // Logout event listener — admin çıkış yaptığında temizlik yap
  useEffect(() => {
    const handleLogout = () => {
      cleanupRef.current.forEach((unsub) => {
        if (typeof unsub === "function") unsub();
      });
      cleanupRef.current = [];
      isConnectedRef.current = false;
      isConnectingRef.current = false;
      setConnectionState("disconnected");
      setNotifications([]);
      setUnreadCount(0);
      // Tüm bağlantıları kes
      signalRService.disconnectAll().catch(console.error);
    };

    window.addEventListener("adminLogout", handleLogout);
    return () => window.removeEventListener("adminLogout", handleLogout);
  }, []);

  // ==========================================================================
  // CONTEXT VALUE
  // ==========================================================================
  const value = {
    // Bağlantı durumu
    connectionState,
    isConnected: connectionState === "connected",

    // Bildirimler
    notifications,
    unreadCount,

    // Bildirim aksiyonları
    addNotification,
    markAsRead,
    markAllAsRead,
    clearNotifications,

    // Hub erişimi — alt bileşenler doğrudan hub'a event listener ekleyebilir
    adminHub: signalRService.adminHub,
    deliveryHub: signalRService.deliveryHub,
  };

  return (
    <AdminSignalRContext.Provider value={value}>
      {children}
    </AdminSignalRContext.Provider>
  );
}

// ==========================================================================
// CUSTOM HOOK
// NEDEN: Context'e güvenli erişim — Provider dışında kullanım hatası verir
// ==========================================================================
export function useAdminSignalR() {
  const context = useContext(AdminSignalRContext);
  if (!context) {
    throw new Error(
      "useAdminSignalR, AdminSignalRProvider içinde kullanılmalıdır",
    );
  }
  return context;
}

export default AdminSignalRContext;
