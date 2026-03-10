// ==========================================================================
// CourierSignalRProvider.jsx - Kurye SignalR Bağlantı Yönetimi
// ==========================================================================
// Kurye paneli için real-time bildirimler ve görev güncellemeleri.
// Yeni görev atama, iptal, durum değişikliği gibi eventleri dinler.
// ==========================================================================

import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  useRef,
} from "react";
import * as signalR from "@microsoft/signalr";
import { useCourierAuth } from "./CourierAuthContext";

// Context
const CourierSignalRContext = createContext(null);

// SignalR Hub URL
const HUB_URL = process.env.REACT_APP_API_URL
  ? `${process.env.REACT_APP_API_URL}/hubs/courier`
  : "http://localhost:5000/hubs/courier";

export function CourierSignalRProvider({ children }) {
  const { token, courier, isAuthenticated, logout } = useCourierAuth();

  // State
  const [connection, setConnection] = useState(null);
  const [connectionState, setConnectionState] = useState("disconnected");
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);

  // Event handlers ref (component update'lerinde kaybolmaması için)
  const eventHandlersRef = useRef({});
  // Cross-tab senkronizasyon kanalı
  const broadcastChannelRef = useRef(null);

  // Cross-tab bildirim senkronizasyonu
  useEffect(() => {
    if (typeof BroadcastChannel === "undefined") return;

    const channel = new BroadcastChannel("courier-notifications-sync");
    broadcastChannelRef.current = channel;

    channel.onmessage = (event) => {
      const { type, payload } = event.data;
      switch (type) {
        case "NEW_NOTIFICATION":
          setNotifications((prev) => {
            if (prev.some((n) => n.id === payload.id)) return prev;
            return [payload, ...prev].slice(0, 50);
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

  // =========================================================================
  // BAĞLANTI OLUŞTURMA
  // =========================================================================
  const createConnection = useCallback(() => {
    if (!token) return null;

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => token,
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // İlk 5 deneme: 1-5 saniye arası
          // Sonraki denemeler: 10 saniye
          if (retryContext.previousRetryCount < 5) {
            return 1000 * (retryContext.previousRetryCount + 1);
          }
          return 10000;
        },
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    return newConnection;
  }, [token]);

  // =========================================================================
  // BAĞLANTIYI BAŞLAT
  // =========================================================================
  const startConnection = useCallback(
    async (conn) => {
      if (!conn) return;

      try {
        setConnectionState("connecting");
        await conn.start();
        setConnectionState("connected");
        console.log("✅ Kurye SignalR bağlantısı kuruldu");

        // Kurye grubuna katıl (backend metod adı: JoinCourierRoom)
        if (courier?.id) {
          await conn.invoke("JoinCourierRoom");
        }
      } catch (error) {
        console.error("❌ SignalR bağlantı hatası:", error);
        setConnectionState("error");

        // 5 saniye sonra tekrar dene
        setTimeout(() => {
          if (conn.state === signalR.HubConnectionState.Disconnected) {
            startConnection(conn);
          }
        }, 5000);
      }
    },
    [courier?.id],
  );

  // =========================================================================
  // EVENT HANDLERS KAYIT
  // =========================================================================
  const registerEventHandlers = useCallback(
    (conn) => {
      if (!conn) return;

      // Yeni görev atandı (Backend "OrderAssigned" event'i gönderir)
      conn.on("OrderAssigned", (task) => {
        console.log("📦 Yeni görev atandı:", task);

        const notification = {
          id: Date.now(),
          type: "task_assigned",
          title: "Yeni Görev",
          message: `#${task.orderId} numaralı sipariş için yeni teslimat görevi atandı`,
          data: task,
          timestamp: new Date().toISOString(),
          read: false,
        };

        addNotification(notification);

        // Custom event dispatch et (diğer componentler dinleyebilir)
        window.dispatchEvent(
          new CustomEvent("courierTaskAssigned", { detail: task }),
        );
      });

      // Sipariş durumu değişti (Backend "OrderStatusChanged" event'i gönderir)
      conn.on("OrderStatusChanged", (data) => {
        console.log("🔄 Sipariş durumu değişti:", data);
        window.dispatchEvent(
          new CustomEvent("courierTaskUpdated", { detail: data }),
        );
      });

      // Görev iptal / sipariş kaldırıldı (Backend "OrderUnassigned" event'i gönderir)
      conn.on("OrderUnassigned", (data) => {
        console.log("❌ Görev iptal edildi:", data);

        const notification = {
          id: Date.now(),
          type: "task_cancelled",
          title: "Görev İptal",
          message: `Sipariş #${data.orderId || data} iptal edildi${data.reason ? ": " + data.reason : ""}`,
          data: data,
          timestamp: new Date().toISOString(),
          read: false,
        };

        addNotification(notification);
        window.dispatchEvent(
          new CustomEvent("courierTaskCancelled", {
            detail: data,
          }),
        );
      });

      // SLA uyarısı (Backend "SlaWarning" event'i gönderir)
      conn.on("SlaWarning", (data) => {
        console.log("⚠️ SLA uyarısı:", data);

        const notification = {
          id: Date.now(),
          type: "sla_warning",
          title: "SLA Uyarısı",
          message:
            data.message || `Sipariş #${data.orderId} SLA süresine yaklaşıyor`,
          data: data,
          timestamp: new Date().toISOString(),
          read: false,
        };

        addNotification(notification);
      });

      // Admin mesajı
      conn.on("AdminMessage", (message) => {
        console.log("💬 Admin mesajı:", message);

        const notification = {
          id: Date.now(),
          type: "admin_message",
          title: "Admin Mesajı",
          message: message.text,
          data: message,
          timestamp: new Date().toISOString(),
          read: false,
        };

        addNotification(notification);
      });

      // Bağlantı durumu
      conn.onclose((error) => {
        console.log("🔌 SignalR bağlantısı kapandı", error);
        setConnectionState("disconnected");
      });

      conn.onreconnecting((error) => {
        console.log("🔄 Yeniden bağlanılıyor...", error);
        setConnectionState("reconnecting");
      });

      conn.onreconnected((connectionId) => {
        console.log("✅ Yeniden bağlandı:", connectionId);
        setConnectionState("connected");

        // Gruba yeniden katıl (backend metod adı: JoinCourierRoom)
        if (courier?.id) {
          conn.invoke("JoinCourierRoom").catch(console.error);
        }
      });
    },
    [courier?.id],
  );

  // =========================================================================
  // BİLDİRİM YÖNETİMİ
  // =========================================================================
  const addNotification = useCallback((notification) => {
    setNotifications((prev) => [notification, ...prev].slice(0, 50));
    setUnreadCount((prev) => prev + 1);

    // Cross-tab senkronizasyon
    try {
      broadcastChannelRef.current?.postMessage({
        type: "NEW_NOTIFICATION",
        payload: notification,
      });
    } catch (e) {}

    // Sesli bildirim (opsiyonel)
    try {
      if ("Notification" in window && Notification.permission === "granted") {
        new Notification(notification.title, {
          body: notification.message,
          icon: "/favicon.ico",
          tag: notification.id.toString(),
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
    try {
      broadcastChannelRef.current?.postMessage({ type: "MARK_ALL_READ" });
    } catch (e) {}
  }, []);

  const clearNotifications = useCallback(() => {
    setNotifications([]);
    setUnreadCount(0);
    try {
      broadcastChannelRef.current?.postMessage({ type: "CLEAR_ALL" });
    } catch (e) {}
  }, []);

  // =========================================================================
  // MESAJ GÖNDERME
  // =========================================================================
  const sendLocationUpdate = useCallback(
    async (latitude, longitude, accuracy) => {
      if (connection?.state === signalR.HubConnectionState.Connected) {
        try {
          await connection.invoke("UpdateLocation", {
            courierId: courier?.id,
            latitude,
            longitude,
            accuracy,
            timestamp: new Date().toISOString(),
          });
        } catch (error) {
          console.error("Konum güncelleme hatası:", error);
        }
      }
    },
    [connection, courier?.id],
  );

  const sendStatusUpdate = useCallback(
    async (taskId, status) => {
      if (connection?.state === signalR.HubConnectionState.Connected) {
        try {
          await connection.invoke("UpdateTaskStatus", taskId, status);
        } catch (error) {
          console.error("Durum güncelleme hatası:", error);
        }
      }
    },
    [connection],
  );

  // =========================================================================
  // BAĞLANTI LİFECYCLE
  // =========================================================================
  useEffect(() => {
    if (!isAuthenticated || !token) {
      // Authenticated değilse bağlantıyı kes
      if (connection) {
        connection.stop();
        setConnection(null);
        setConnectionState("disconnected");
      }
      return;
    }

    // Yeni bağlantı oluştur
    const conn = createConnection();
    if (conn) {
      setConnection(conn);
      registerEventHandlers(conn);
      startConnection(conn);
    }

    // Cleanup
    return () => {
      if (conn) {
        conn.stop();
      }
    };
  }, [
    isAuthenticated,
    token,
    createConnection,
    registerEventHandlers,
    startConnection,
  ]);

  // Logout event listener
  useEffect(() => {
    const handleLogout = () => {
      if (connection) {
        connection.stop();
        setConnection(null);
        setConnectionState("disconnected");
      }
      setNotifications([]);
      setUnreadCount(0);
    };

    window.addEventListener("courierLogout", handleLogout);
    return () => window.removeEventListener("courierLogout", handleLogout);
  }, [connection]);

  // =========================================================================
  // CONTEXT VALUE
  // =========================================================================
  const value = {
    // State
    connectionState,
    isConnected: connectionState === "connected",
    notifications,
    unreadCount,

    // Actions
    sendLocationUpdate,
    sendStatusUpdate,
    markAsRead,
    markAllAsRead,
    clearNotifications,

    // Event handler registration için
    registerHandler: (event, handler) => {
      eventHandlersRef.current[event] = handler;
    },
    unregisterHandler: (event) => {
      delete eventHandlersRef.current[event];
    },
  };

  return (
    <CourierSignalRContext.Provider value={value}>
      {children}
    </CourierSignalRContext.Provider>
  );
}

// =========================================================================
// CUSTOM HOOK
// =========================================================================
export function useCourierSignalR() {
  const context = useContext(CourierSignalRContext);
  if (!context) {
    throw new Error(
      "useCourierSignalR must be used within CourierSignalRProvider",
    );
  }
  return context;
}

export default CourierSignalRContext;
