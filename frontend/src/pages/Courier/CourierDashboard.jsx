// ==========================================================================
// CourierDashboard.jsx - Kurye Ana Sayfası
// ==========================================================================
// SignalR ile real-time sipariş güncellemeleri, istatistikler ve sipariş listesi.
// Mobil uyumlu tasarım, durum badge'leri ve hızlı aksiyonlar.
// ==========================================================================

import React, { useState, useEffect, useCallback } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useCourierAuth } from "../../contexts/CourierAuthContext";
import { CourierService } from "../../services/courierService";
import {
  signalRService,
  SignalREvents,
  ConnectionState,
} from "../../services/signalRService";

export default function CourierDashboard() {
  // Auth context
  const {
    courier,
    isAuthenticated,
    loading: authLoading,
    logout,
    isOnline,
    toggleOnlineStatus,
  } = useCourierAuth();

  // State
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [signalRConnected, setSignalRConnected] = useState(false);
  const [lastUpdate, setLastUpdate] = useState(null);
  const [stats, setStats] = useState({
    activeOrders: 0,
    completedToday: 0,
    totalEarnings: 0,
    pendingOrders: 0,
    failedOrders: 0,
  });

  // =========================================================================
  // SES BİLDİRİM TOGGLEs
  // =========================================================================
  const [soundEnabled, setSoundEnabled] = useState(() => {
    // localStorage'dan başlangıç değeri oku
    return localStorage.getItem("courierNotificationSound") !== "false";
  });

  // Yeni sipariş animasyonu için state
  const [newOrderAnimation, setNewOrderAnimation] = useState(false);

  const navigate = useNavigate();

  // =========================================================================
  // SİPARİŞLERİ YÜKLE
  // =========================================================================
  const loadOrders = useCallback(async () => {
    if (!courier?.id) return;

    try {
      const { orders: orderData = [], summary } =
        (await CourierService.getAssignedOrders()) || {};
      setOrders(orderData);
      setLastUpdate(new Date());

      // İstatistikleri hesapla (API summary varsa onu kullan)
      if (summary) {
        setStats({
          activeOrders: summary.activeOrders ?? 0,
          completedToday: summary.todayDelivered ?? 0,
          totalEarnings: summary.todayEarnings ?? 0,
          pendingOrders: summary.pendingOrders ?? 0,
          failedOrders: summary.todayFailed ?? 0,
        });
      } else {
        calculateStats(orderData);
      }
    } catch (error) {
      console.error("Sipariş yükleme hatası:", error);
    } finally {
      setLoading(false);
    }
  }, [courier?.id]);

  // =========================================================================
  // İSTATİSTİKLERİ HESAPLA
  // =========================================================================
  const calculateStats = (orderList) => {
    const normalize = (status) => (status || "").toLowerCase();
    const activeCount = orderList.filter((o) =>
      [
        "assigned",
        "out_for_delivery",
        "outfordelivery",
        "picked_up",
        "pickedup",
        "in_transit",
        "intransit",
      ].includes(normalize(o.status)),
    ).length;

    const completedCount = orderList.filter(
      (o) => normalize(o.status) === "delivered",
    ).length;

    const pendingCount = orderList.filter((o) =>
      ["preparing", "ready", "assigned", "confirmed", "pending"].includes(
        normalize(o.status),
      ),
    ).length;

    const failedCount = orderList.filter((o) =>
      ["delivery_failed", "failed"].includes(normalize(o.status)),
    ).length;

    setStats({
      activeOrders: activeCount,
      completedToday: completedCount,
      totalEarnings: completedCount * 15, // Sabit teslimat ücreti
      pendingOrders: pendingCount,
      failedOrders: failedCount,
    });
  };

  // =========================================================================
  // AUTH KONTROLÜ
  // =========================================================================
  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      navigate("/courier/login");
    }
  }, [authLoading, isAuthenticated, navigate]);

  // =========================================================================
  // İLK YÜKLEME
  // =========================================================================
  useEffect(() => {
    if (courier?.id) {
      loadOrders();
    }
  }, [courier?.id, loadOrders]);

  // =========================================================================
  // SIGNALR ENTEGRASYONU
  // =========================================================================
  useEffect(() => {
    if (!courier?.id) return;

    // SignalR bağlantısını başlat
    const connectSignalR = async () => {
      try {
        await signalRService.connectCourier(courier.id);
        setSignalRConnected(true);
        console.log("✅ SignalR Courier Hub bağlantısı kuruldu");
      } catch (error) {
        console.warn("⚠️ SignalR bağlantısı kurulamadı:", error);
        setSignalRConnected(false);
      }
    };

    connectSignalR();

    // SignalR event listener'ları
    const courierHub = signalRService.courierHub;

    // Yeni sipariş atandığında (Backend "NewOrderAssigned" gönderiyor)
    const handleOrderAssigned = (data) => {
      console.log("📦 Yeni sipariş atandı:", data);
      loadOrders(); // Listeyi yenile
      playNotificationSound();
      showNotification(
        "🚴 Yeni Sipariş Atandı!",
        `Sipariş #${data.orderNumber || data.orderId} size atandı! Teslim alınmayı bekliyor.`,
      );

      // Yeni sipariş animasyonu başlat
      setNewOrderAnimation(true);
      setTimeout(() => setNewOrderAnimation(false), 3000);
    };

    // Sipariş durumu değiştiğinde
    const handleOrderStatusChanged = (data) => {
      console.log("🔄 Sipariş durumu değişti:", data);
      setOrders((prev) =>
        prev.map((o) =>
          o.id === data.orderId ? { ...o, status: data.newStatus } : o,
        ),
      );
      setLastUpdate(new Date());
    };

    // Sipariş iptal edildiğinde
    const handleOrderCancelled = (data) => {
      console.log("❌ Sipariş iptal edildi:", data);
      setOrders((prev) => prev.filter((o) => o.id !== data.orderId));
      setLastUpdate(new Date());
      showNotification(
        "Sipariş İptal",
        `Sipariş #${data.orderId} iptal edildi.`,
      );
    };

    // Event listener'ları kaydet - Backend event isimleriyle eşleştir
    // Backend "NewOrderAssigned" gönderiyor (DispatcherOrderController.cs L175)
    courierHub.on("NewOrderAssigned", handleOrderAssigned);
    courierHub.on("OrderAssigned", handleOrderAssigned);
    courierHub.on("OrderUpdated", handleOrderStatusChanged);
    courierHub.on("OrderStatusChanged", handleOrderStatusChanged);
    courierHub.on("OrderCancelled", handleOrderCancelled);
    courierHub.on("OrderUnassigned", handleOrderCancelled);

    // Ses bildirimi event'i
    courierHub.on("PlaySound", (data) => {
      console.log("🔊 Ses bildirimi:", data);
      playNotificationSound();
    });

    // Cleanup
    return () => {
      courierHub.off("NewOrderAssigned", handleOrderAssigned);
      courierHub.off("OrderAssigned", handleOrderAssigned);
      courierHub.off("OrderUpdated", handleOrderStatusChanged);
      courierHub.off("OrderStatusChanged", handleOrderStatusChanged);
      courierHub.off("OrderCancelled", handleOrderCancelled);
      courierHub.off("OrderUnassigned", handleOrderCancelled);
      courierHub.off("PlaySound");
    };
  }, [courier?.id, loadOrders]);

  // =========================================================================
  // BİLDİRİM SESİ VE TOGGLE
  // =========================================================================
  const playNotificationSound = () => {
    if (soundEnabled) {
      try {
        const audio = new Audio(
          "/sounds/mixkit-melodic-race-countdown-1955.wav",
        );
        audio.volume = 0.7;
        audio.play().catch(() => {});
      } catch (error) {
        console.warn("Bildirim sesi çalınamadı:", error);
      }
    }
  };

  // Ses toggle fonksiyonu
  const toggleSound = () => {
    const newValue = !soundEnabled;
    setSoundEnabled(newValue);
    localStorage.setItem("courierNotificationSound", newValue.toString());

    // Test sesi çal
    if (newValue) {
      try {
        const audio = new Audio("/sounds/mixkit-bell-notification-933.wav");
        audio.volume = 0.5;
        audio.play().catch(() => {});
      } catch (e) {
        console.warn("Test sesi çalınamadı");
      }
    }
  };

  // =========================================================================
  // BROWSER BİLDİRİMİ
  // =========================================================================
  const showNotification = (title, body) => {
    if ("Notification" in window && Notification.permission === "granted") {
      new Notification(title, { body, icon: "/favicon.ico" });
    }
  };

  // Bildirim izni iste
  useEffect(() => {
    if ("Notification" in window && Notification.permission === "default") {
      Notification.requestPermission();
    }
  }, []);

  // =========================================================================
  // DURUM YARDIMCI FONKSİYONLARI
  // =========================================================================
  const getStatusText = (status, fallbackText) => {
    if (fallbackText) return fallbackText;
    const statusMap = {
      new: "Yeni",
      confirmed: "Onaylandı",
      preparing: "Hazırlanıyor",
      ready: "Teslim Alınmaya Hazır",
      assigned: "Size Atandı",
      picked_up: "Teslim Alındı",
      out_for_delivery: "Yolda",
      outfordelivery: "Yolda",
      in_transit: "Yolda",
      delivery_failed: "Başarısız",
      deliveryfailed: "Başarısız",
      deliverypaymentpending: "Ödeme Bekliyor",
      delivered: "Teslim Edildi",
      cancelled: "İptal Edildi",
    };
    const normalized = (status || "").toLowerCase();
    return statusMap[normalized] || status || "-";
  };

  const getStatusColor = (status, fallbackColor) => {
    if (fallbackColor) return fallbackColor;
    const colorMap = {
      new: "secondary",
      confirmed: "secondary",
      preparing: "secondary",
      ready: "info",
      assigned: "warning", // 🟡 Sarı
      picked_up: "primary",
      out_for_delivery: "primary", // 🔵 Mavi
      outfordelivery: "primary",
      in_transit: "primary",
      delivered: "success", // 🟢 Yeşil
      delivery_failed: "danger", // 🔴 Kırmızı
      deliveryfailed: "danger",
      deliverypaymentpending: "warning", // 🟠 Turuncu/Sarı
      cancelled: "dark",
    };
    const normalized = (status || "").toLowerCase();
    return colorMap[normalized] || "secondary";
  };

  const getStatusIcon = (status) => {
    const iconMap = {
      new: "fa-circle",
      confirmed: "fa-check",
      preparing: "fa-clock",
      ready: "fa-box",
      assigned: "fa-bell",
      picked_up: "fa-hand-holding-box",
      out_for_delivery: "fa-motorcycle",
      outfordelivery: "fa-motorcycle",
      in_transit: "fa-motorcycle",
      delivered: "fa-check-circle",
      delivery_failed: "fa-times-circle",
      deliveryfailed: "fa-times-circle",
      deliverypaymentpending: "fa-credit-card",
      cancelled: "fa-ban",
    };
    const normalized = (status || "").toLowerCase();
    return iconMap[normalized] || "fa-circle";
  };

  // =========================================================================
  // ONLINE/OFFLINE TOGGLE
  // =========================================================================
  const handleToggleOnline = async () => {
    const result = await toggleOnlineStatus(!isOnline);
    if (!result.success) {
      alert("Durum güncellenemedi: " + result.error);
    }
  };

  // =========================================================================
  // LOGOUT
  // =========================================================================
  const handleLogout = () => {
    if (window.confirm("Çıkış yapmak istediğinize emin misiniz?")) {
      logout();
      navigate("/courier/login");
    }
  };

  // =========================================================================
  // LOADING STATE
  // =========================================================================
  if (authLoading || loading) {
    return (
      <div className="d-flex justify-content-center align-items-center min-vh-100 bg-light">
        <div className="text-center">
          <div className="spinner-border text-primary mb-3" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="text-muted">Yükleniyor...</p>
        </div>
      </div>
    );
  }

  // =========================================================================
  // AKTİF SİPARİŞLERİ FİLTRELE (Teslim edilmemiş)
  // =========================================================================
  const activeOrders = orders.filter((o) => {
    const normalized = (o.status || "").toLowerCase();
    return !["delivered", "cancelled"].includes(normalized);
  });

  return (
    <>
      <style>{`
        @keyframes pulseBadge {
          0%, 100% { transform: scale(1); }
          50% { transform: scale(1.1); }
        }
        .pulse-badge {
          animation: pulseBadge 1.5s infinite;
        }
        
        /* Yeni sipariş animasyonu */
        @keyframes newOrderPulse {
          0%, 100% { box-shadow: 0 0 0 0 rgba(255, 107, 53, 0.7); }
          50% { box-shadow: 0 0 0 15px rgba(255, 107, 53, 0); }
        }
        .new-order-pulse {
          animation: newOrderPulse 1s infinite;
        }
        
        /* Shake animasyonu */
        @keyframes shake {
          0%, 100% { transform: translateX(0); }
          10%, 30%, 50%, 70%, 90% { transform: translateX(-2px); }
          20%, 40%, 60%, 80% { transform: translateX(2px); }
        }
        .shake-animation {
          animation: shake 0.5s ease-in-out;
        }
        
        /* Glow efekti yeni siparişler için */
        @keyframes glow {
          0%, 100% { box-shadow: 0 0 5px rgba(255, 107, 53, 0.5); }
          50% { box-shadow: 0 0 20px rgba(255, 107, 53, 0.8), 0 0 30px rgba(255, 107, 53, 0.4); }
        }
        .glow-effect {
          animation: glow 1.5s ease-in-out infinite;
        }
        
        .order-card {
          transition: all 0.2s ease;
          cursor: pointer;
        }
        .order-card:hover {
          transform: translateY(-2px);
          box-shadow: 0 8px 25px rgba(0,0,0,0.1) !important;
        }
        .status-assigned { border-left: 4px solid #ffc107 !important; }
        .status-picked_up, .status-pickedup { border-left: 4px solid #17a2b8 !important; }
        .status-out_for_delivery, .status-in_transit { border-left: 4px solid #0d6efd !important; }
        .status-delivered { border-left: 4px solid #198754 !important; }
        .status-delivery_failed { border-left: 4px solid #dc3545 !important; }
        .online-indicator {
          width: 10px;
          height: 10px;
          border-radius: 50%;
          display: inline-block;
          margin-right: 8px;
        }
        .online-indicator.online { background-color: #198754; }
        .online-indicator.offline { background-color: #6c757d; }
      `}</style>

      <div className="min-vh-100 bg-light">
        {/* Header */}
        <nav
          className="navbar navbar-expand-lg navbar-dark sticky-top"
          style={{ background: "linear-gradient(135deg, #ff6b35, #ff8c00)" }}
        >
          <div className="container-fluid px-3">
            <span className="navbar-brand d-flex align-items-center">
              <i className="fas fa-motorcycle me-2"></i>
              <span className="d-none d-sm-inline">Kurye Paneli</span>
              {/* SignalR Bağlantı Durumu */}
              <span
                className={`badge ${signalRConnected ? "bg-success" : "bg-secondary"} ms-2`}
                style={{ fontSize: "0.55rem" }}
                title={
                  signalRConnected ? "Real-time bağlantı aktif" : "Bağlantı yok"
                }
              >
                <i
                  className={`fas fa-${signalRConnected ? "bolt" : "clock"}`}
                ></i>
              </span>
            </span>

            <div className="d-flex align-items-center gap-2">
              {/* Ses Toggle Butonu */}
              <button
                onClick={toggleSound}
                className={`btn btn-sm ${soundEnabled ? "btn-light" : "btn-outline-light"}`}
                style={{ fontSize: "0.75rem" }}
                title={
                  soundEnabled ? "Bildirimleri Sessize Al" : "Bildirimleri Aç"
                }
              >
                <i
                  className={`fas fa-${soundEnabled ? "bell" : "bell-slash"}`}
                ></i>
              </button>

              {/* Online/Offline Toggle */}
              <button
                onClick={handleToggleOnline}
                className={`btn btn-sm ${isOnline ? "btn-success" : "btn-secondary"}`}
                style={{ fontSize: "0.75rem" }}
              >
                <span
                  className={`online-indicator ${isOnline ? "online" : "offline"}`}
                ></span>
                {isOnline ? "Aktif" : "Pasif"}
              </button>

              {/* Kullanıcı Bilgisi */}
              <span
                className="text-white d-none d-md-inline"
                style={{ fontSize: "0.85rem" }}
              >
                <i className="fas fa-user-circle me-1"></i>
                {courier?.name?.split(" ")[0]}
              </span>

              {/* Çıkış */}
              <button
                onClick={handleLogout}
                className="btn btn-outline-light btn-sm"
                style={{ fontSize: "0.75rem" }}
              >
                <i className="fas fa-sign-out-alt"></i>
                <span className="d-none d-sm-inline ms-1">Çıkış</span>
              </button>
            </div>
          </div>
        </nav>

        <div className="container-fluid p-3 p-md-4">
          {/* Hoşgeldin Mesajı */}
          <div className="row mb-3">
            <div className="col-12">
              <div className="d-flex justify-content-between align-items-center flex-wrap gap-2">
                <div>
                  <h4
                    className="fw-bold text-dark mb-0"
                    style={{ fontSize: "1.1rem" }}
                  >
                    Hoş geldin, {courier?.name?.split(" ")[0]}! 👋
                  </h4>
                  <p className="text-muted mb-0" style={{ fontSize: "0.8rem" }}>
                    {lastUpdate && (
                      <span>
                        Son güncelleme: {lastUpdate.toLocaleTimeString("tr-TR")}
                      </span>
                    )}
                  </p>
                </div>
                <button
                  onClick={loadOrders}
                  className="btn btn-outline-primary btn-sm"
                  style={{ fontSize: "0.75rem" }}
                >
                  <i className="fas fa-sync-alt me-1"></i>
                  Yenile
                </button>
              </div>
            </div>
          </div>

          {/* İstatistik Kartları */}
          <div className="row g-2 mb-4">
            {/* Aktif Sipariş */}
            <div className="col-6 col-md-3">
              <div
                className="card border-0 shadow-sm h-100"
                style={{ borderRadius: "12px" }}
              >
                <div className="card-body text-center p-2 p-md-3">
                  <div className="text-primary mb-1">
                    <i className="fas fa-clock fs-4"></i>
                  </div>
                  <h4 className="fw-bold text-primary mb-0">
                    {stats.activeOrders}
                  </h4>
                  <small className="text-muted" style={{ fontSize: "0.7rem" }}>
                    Aktif Sipariş
                  </small>
                </div>
              </div>
            </div>

            {/* Bekleyen */}
            <div className="col-6 col-md-3">
              <div
                className="card border-0 shadow-sm h-100"
                style={{ borderRadius: "12px" }}
              >
                <div className="card-body text-center p-2 p-md-3">
                  <div className="text-warning mb-1">
                    <i className="fas fa-hourglass-half fs-4"></i>
                  </div>
                  <h4 className="fw-bold text-warning mb-0">
                    {stats.pendingOrders}
                  </h4>
                  <small className="text-muted" style={{ fontSize: "0.7rem" }}>
                    Bekleyen
                  </small>
                </div>
              </div>
            </div>

            {/* Teslim Edilen */}
            <div className="col-6 col-md-3">
              <div
                className="card border-0 shadow-sm h-100"
                style={{ borderRadius: "12px" }}
              >
                <div className="card-body text-center p-2 p-md-3">
                  <div className="text-success mb-1">
                    <i className="fas fa-check-circle fs-4"></i>
                  </div>
                  <h4 className="fw-bold text-success mb-0">
                    {stats.completedToday}
                  </h4>
                  <small className="text-muted" style={{ fontSize: "0.7rem" }}>
                    Bugün Teslim
                  </small>
                </div>
              </div>
            </div>

            {/* Kazanç */}
            <div className="col-6 col-md-3">
              <div
                className="card border-0 shadow-sm h-100"
                style={{ borderRadius: "12px" }}
              >
                <div className="card-body text-center p-2 p-md-3">
                  <div className="text-info mb-1">
                    <i className="fas fa-lira-sign fs-4"></i>
                  </div>
                  <h4 className="fw-bold text-info mb-0">
                    {stats.totalEarnings}₺
                  </h4>
                  <small className="text-muted" style={{ fontSize: "0.7rem" }}>
                    Bugün Kazanç
                  </small>
                </div>
              </div>
            </div>
          </div>

          {/* Başarısız Teslimat Uyarısı */}
          {stats.failedOrders > 0 && (
            <div
              className="alert alert-danger d-flex align-items-center mb-3 py-2"
              style={{ fontSize: "0.85rem" }}
            >
              <i className="fas fa-exclamation-triangle me-2"></i>
              <strong>{stats.failedOrders}</strong> adet başarısız teslimat var.
              Lütfen kontrol edin.
            </div>
          )}

          {/* Aktif Siparişler */}
          <div
            className={`card border-0 shadow-sm ${newOrderAnimation ? "glow-effect" : ""}`}
            style={{ borderRadius: "12px" }}
          >
            <div className="card-header bg-white border-0 py-2 px-3">
              <div className="d-flex justify-content-between align-items-center">
                <h6
                  className={`fw-bold mb-0 ${newOrderAnimation ? "shake-animation" : ""}`}
                  style={{ fontSize: "0.9rem" }}
                >
                  <i
                    className={`fas fa-list-alt me-2 ${newOrderAnimation ? "text-warning" : "text-primary"}`}
                  ></i>
                  Aktif Siparişler ({activeOrders.length})
                  {newOrderAnimation && (
                    <span className="badge bg-warning text-dark ms-2 pulse-badge">
                      Yeni!
                    </span>
                  )}
                </h6>
                <Link
                  to="/courier/orders"
                  className="btn btn-outline-primary btn-sm"
                  style={{ fontSize: "0.7rem" }}
                >
                  <i className="fas fa-eye me-1"></i>
                  Tümü
                </Link>
              </div>
            </div>
            <div className="card-body p-2 p-md-3">
              {activeOrders.length === 0 ? (
                <div className="text-center py-5">
                  <i className="fas fa-inbox fs-1 text-muted mb-3 d-block"></i>
                  <p className="text-muted mb-2">
                    Henüz atanmış siparişiniz yok
                  </p>
                  <small className="text-muted">
                    Yeni siparişler otomatik olarak burada görünecek
                  </small>
                </div>
              ) : (
                <div className="row g-2">
                  {activeOrders.slice(0, 6).map((order) => (
                    <div key={order.id} className="col-12">
                      <div
                        className={`card border-0 order-card status-${order.status}`}
                        style={{
                          borderRadius: "10px",
                          backgroundColor: "#f8f9fa",
                        }}
                      >
                        <div className="card-body p-2 p-md-3">
                          {/* Üst Satır: Sipariş No + Durum + Tutar */}
                          <div className="d-flex justify-content-between align-items-center mb-2">
                            <div className="d-flex align-items-center gap-2">
                              <h6
                                className="fw-bold mb-0 text-dark"
                                style={{ fontSize: "1rem" }}
                              >
                                #{order.id}
                              </h6>
                              <span
                                className={`badge bg-${getStatusColor(order.status, order.statusColor)}`}
                                style={{ fontSize: "0.7rem" }}
                              >
                                {getStatusText(order.status, order.statusText)}
                              </span>
                            </div>
                            <span
                              className="fw-bold text-success"
                              style={{ fontSize: "1.1rem" }}
                            >
                              {(order.totalAmount || 0).toFixed(0)} ₺
                            </span>
                          </div>

                          {/* Müşteri ve Adres */}
                          <div className="mb-2">
                            <p
                              className="text-dark mb-1"
                              style={{ fontSize: "0.85rem" }}
                            >
                              <i className="fas fa-user me-2 text-primary"></i>
                              <strong>{order.customerName}</strong>
                              {order.customerPhone && (
                                <a
                                  href={`tel:${order.customerPhone}`}
                                  className="ms-2 text-success"
                                >
                                  <i className="fas fa-phone"></i>
                                </a>
                              )}
                            </p>
                            <p
                              className="text-muted mb-0"
                              style={{ fontSize: "0.8rem" }}
                            >
                              <i className="fas fa-map-marker-alt me-2 text-danger"></i>
                              {order.address || "Adres belirtilmemiş"}
                            </p>
                          </div>

                          {/* MVP: TEK TIK AKSİYON BUTONLARI */}
                          <div className="d-flex gap-2 mt-2">
                            {/* 📍 HARİTADA GÖR */}
                            <button
                              onClick={(e) => {
                                e.preventDefault();
                                const address =
                                  order.address || order.deliveryAddress;
                                if (address) {
                                  window.open(
                                    `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}`,
                                    "_blank",
                                  );
                                }
                              }}
                              className="btn btn-outline-info btn-sm flex-grow-1"
                              style={{ fontSize: "0.75rem" }}
                            >
                              <i className="fas fa-map-marked-alt me-1"></i>
                              Harita
                            </button>

                            {/* 🛵 YOLA ÇIK - assigned durumunda */}
                            {(order.status === "assigned" ||
                              order.status === "ready") && (
                              <button
                                onClick={async (e) => {
                                  e.preventDefault();
                                  try {
                                    await CourierService.startDelivery(
                                      order.id,
                                    );
                                    playNotificationSound();
                                    loadOrders();
                                  } catch (err) {
                                    alert("Hata: " + err.message);
                                  }
                                }}
                                className="btn btn-primary btn-sm flex-grow-1"
                                style={{ fontSize: "0.75rem" }}
                              >
                                <i className="fas fa-play me-1"></i>
                                Yola Çık
                              </button>
                            )}

                            {/* ✅ TESLİM ET - out_for_delivery durumunda */}
                            {(order.status === "out_for_delivery" ||
                              order.status === "outfordelivery" ||
                              order.status === "in_transit") && (
                              <button
                                onClick={async (e) => {
                                  e.preventDefault();
                                  try {
                                    await CourierService.markDelivered(
                                      order.id,
                                    );
                                    playNotificationSound();
                                    loadOrders();
                                  } catch (err) {
                                    alert("Hata: " + err.message);
                                  }
                                }}
                                className="btn btn-success btn-sm flex-grow-1"
                                style={{
                                  fontSize: "0.75rem",
                                  fontWeight: "bold",
                                }}
                              >
                                <i className="fas fa-check-double me-1"></i>
                                TESLİM ET
                              </button>
                            )}

                            {/* ❌ PROBLEM BİLDİR */}
                            {order.status !== "delivered" && (
                              <button
                                onClick={(e) => {
                                  e.preventDefault();
                                  navigate(`/courier/orders/${order.id}`);
                                }}
                                className="btn btn-outline-danger btn-sm"
                                style={{ fontSize: "0.75rem" }}
                                title="Problem Bildir"
                              >
                                <i className="fas fa-exclamation-triangle"></i>
                              </button>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {/* Daha fazla sipariş varsa */}
              {activeOrders.length > 6 && (
                <div className="text-center mt-3">
                  <Link to="/courier/orders" className="btn btn-primary btn-sm">
                    <i className="fas fa-list me-1"></i>+
                    {activeOrders.length - 6} sipariş daha
                  </Link>
                </div>
              )}
            </div>
          </div>

          {/* Hızlı Aksiyonlar */}
          <div className="row g-2 mt-3">
            <div className="col-6">
              <Link
                to="/courier/orders"
                className="btn btn-outline-primary w-100 py-3"
                style={{ borderRadius: "10px" }}
              >
                <i className="fas fa-list-alt d-block mb-1 fs-4"></i>
                <span style={{ fontSize: "0.8rem" }}>Tüm Siparişler</span>
              </Link>
            </div>
            <div className="col-6">
              <Link
                to="/courier/weight-entry"
                className="btn btn-outline-warning w-100 py-3"
                style={{ borderRadius: "10px" }}
              >
                <i className="fas fa-balance-scale d-block mb-1 fs-4"></i>
                <span style={{ fontSize: "0.8rem" }}>Tartı Girişi</span>
              </Link>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
