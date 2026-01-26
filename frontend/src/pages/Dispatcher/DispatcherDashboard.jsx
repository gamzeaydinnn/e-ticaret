// ==========================================================================
// DispatcherDashboard.jsx - Sevkiyat Görevlisi Ana Dashboard
// ==========================================================================
// Hazır siparişleri yönetme, kurye atama ve izleme paneli.
// SignalR ile real-time güncellemeler alır.
// NEDEN: Dispatcher'ın merkezi kontrol paneli olarak işlev görür.
// ==========================================================================

import React, { useState, useEffect, useCallback, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useDispatcherAuth } from "../../contexts/DispatcherAuthContext";
import dispatcherService from "../../services/dispatcherService";
import signalRService from "../../services/signalRService";
import ReadyOrderCard from "./components/ReadyOrderCard";
import CourierStatusBar from "./components/CourierStatusBar";
import CourierDetailModal from "./components/CourierDetailModal";
import CourierSelect from "./components/CourierSelect";

// Ses bildirimleri için - Mixkit ücretsiz sesler
const SOUNDS = {
  newOrder: "/sounds/mixkit-melodic-race-countdown-1955.wav",
  orderAssigned: "/sounds/mixkit-bell-notification-933.wav",
  alert: "/sounds/mixkit-happy-bells-notification-937.wav",
};

export default function DispatcherDashboard() {
  const navigate = useNavigate();
  const {
    dispatcher,
    logout,
    isAuthenticated,
    loading: authLoading,
  } = useDispatcherAuth();

  // =========================================================================
  // STATE TANIMLARI
  // =========================================================================
  const [orders, setOrders] = useState([]);
  const [couriers, setCouriers] = useState([]);
  const [summary, setSummary] = useState({
    readyCount: 0,
    assignedCount: 0,
    outForDeliveryCount: 0,
    deliveredTodayCount: 0,
  });
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);

  // Tab ve filtre state'leri
  const [activeTab, setActiveTab] = useState("ready"); // ready, assigned, outForDelivery
  const [searchQuery, setSearchQuery] = useState("");

  // Modal state'leri
  const [selectedCourier, setSelectedCourier] = useState(null);
  const [showCourierModal, setShowCourierModal] = useState(false);

  // Atama işlemi state'leri
  const [assigningOrderId, setAssigningOrderId] = useState(null);
  const [showAssignModal, setShowAssignModal] = useState(false);

  // Ses bildirimi state'i
  const [soundEnabled, setSoundEnabled] = useState(() => {
    return localStorage.getItem("dispatcherSoundEnabled") !== "false";
  });

  // Son güncelleme zamanı
  const [lastUpdated, setLastUpdated] = useState(new Date());

  // Audio referansları
  const audioRef = useRef(null);

  // =========================================================================
  // SES BİLDİRİMİ
  // NEDEN: Yeni sipariş geldiğinde kullanıcıyı uyarmak için
  // =========================================================================
  const playSound = useCallback(
    (soundType) => {
      if (!soundEnabled) return;

      try {
        if (audioRef.current) {
          audioRef.current.pause();
          audioRef.current.currentTime = 0;
        }

        const audio = new Audio(SOUNDS[soundType] || SOUNDS.alert);
        audioRef.current = audio;
        audio.volume = 0.7;
        audio.play().catch((err) => {
          console.warn("[Dispatcher] Ses çalınamadı:", err.message);
        });
      } catch (err) {
        console.warn("[Dispatcher] Ses hatası:", err);
      }
    },
    [soundEnabled],
  );

  // =========================================================================
  // VERİ YÜKLEME
  // NEDEN: Dashboard açıldığında ve periyodik olarak verileri çeker
  // =========================================================================
  const fetchData = useCallback(
    async (showRefreshIndicator = false) => {
      if (showRefreshIndicator) {
        setRefreshing(true);
      }

      try {
        // Paralel API çağrıları - performans için
        const [ordersRes, couriersRes, summaryRes] = await Promise.all([
          dispatcherService.getOrders(getStatusFromTab(activeTab)),
          dispatcherService.getCouriers(),
          dispatcherService.getSummary(),
        ]);

        if (ordersRes.success) {
          setOrders(ordersRes.data?.orders || []);
        }

        if (couriersRes.success) {
          setCouriers(couriersRes.data?.couriers || couriersRes.data || []);
        }

        if (summaryRes.success) {
          setSummary(
            summaryRes.data || {
              readyCount: 0,
              assignedCount: 0,
              outForDeliveryCount: 0,
              deliveredTodayCount: 0,
            },
          );
        }

        setLastUpdated(new Date());
        setError(null);
      } catch (err) {
        console.error("[Dispatcher] Veri yükleme hatası:", err);
        setError("Veriler yüklenirken bir hata oluştu");
      } finally {
        setLoading(false);
        setRefreshing(false);
      }
    },
    [activeTab],
  );

  // Tab'a göre status değeri
  const getStatusFromTab = (tab) => {
    switch (tab) {
      case "ready":
        return "Ready";
      case "assigned":
        return "Assigned";
      case "outForDelivery":
        return "OutForDelivery";
      default:
        return "Ready";
    }
  };

  // =========================================================================
  // İLK YÜKLEME VE POLLING
  // NEDEN: Component mount olduğunda veri çeker ve periyodik günceller
  // =========================================================================
  useEffect(() => {
    if (!authLoading && isAuthenticated) {
      fetchData();

      // Polling - her 30 saniyede bir güncelle (SignalR yedeği olarak)
      const pollInterval = setInterval(() => {
        fetchData(false);
      }, 30000);

      return () => clearInterval(pollInterval);
    }
  }, [authLoading, isAuthenticated, fetchData]);

  // =========================================================================
  // SIGNALR BAĞLANTISI - REAL-TIME BİLDİRİMLER
  // NEDEN: Sipariş hazır olduğunda anlık bildirim almak için
  // =========================================================================
  useEffect(() => {
    if (!authLoading && isAuthenticated) {
      // SignalR bağlantısını kur
      signalRService.connectDispatcher().then((connected) => {
        if (connected) {
          console.log("[Dispatcher] SignalR bağlantısı kuruldu ✓");
        }
      });

      // Sipariş hazır bildirimi - Market sipariş hazırladığında
      const unsubOrderReady = signalRService.onDispatcherEvent(
        "OrderReady",
        (data) => {
          console.log("[Dispatcher] 🔔 Yeni sipariş hazır:", data);
          playSound("newOrder");
          showBrowserNotification(
            "Sipariş Hazır!",
            `Sipariş #${data.orderNumber} kurye ataması bekliyor.`,
          );
          fetchData(false);
        },
      );

      // Sipariş hazır bildirimi (alternatif event adı)
      const unsubOrderReadyForDispatch = signalRService.onDispatcherEvent(
        "OrderReadyForDispatch",
        (data) => {
          console.log("[Dispatcher] 🔔 Sipariş sevkiyata hazır:", data);
          playSound("newOrder");
          showBrowserNotification(
            "Sipariş Sevkiyata Hazır!",
            `Sipariş #${data.orderNumber || data.orderId} kurye bekliyor.`,
          );
          fetchData(false);
        },
      );

      // Sipariş durumu değişti bildirimi
      const unsubStatusChanged = signalRService.onDispatcherEvent(
        "OrderStatusChanged",
        (data) => {
          console.log("[Dispatcher] 📋 Sipariş durumu değişti:", data);
          fetchData(false);
        },
      );

      // Kurye durumu değişti bildirimi
      const unsubCourierStatus = signalRService.onDispatcherEvent(
        "CourierStatusChanged",
        (data) => {
          console.log("[Dispatcher] 🚴 Kurye durumu değişti:", data);
          fetchData(false);
        },
      );

      // Ses çalma bildirimi
      const unsubPlaySound = signalRService.onDispatcherEvent(
        "PlaySound",
        (data) => {
          console.log("[Dispatcher] 🔊 Ses bildirimi:", data);
          if (
            data.soundType === "newOrder" ||
            data.soundType === "orderReady"
          ) {
            playSound("newOrder");
          } else if (data.soundType === "alert") {
            playSound("alert");
          }
        },
      );

      // Cleanup
      return () => {
        unsubOrderReady?.();
        unsubOrderReadyForDispatch?.();
        unsubStatusChanged?.();
        unsubCourierStatus?.();
        unsubPlaySound?.();
        signalRService.disconnectDispatcher();
      };
    }
  }, [authLoading, isAuthenticated, playSound, fetchData]);

  // =========================================================================
  // BROWSER NOTIFICATION
  // NEDEN: Sekme arka planda olsa bile kullanıcıyı uyarmak için
  // =========================================================================
  const showBrowserNotification = useCallback((title, body) => {
    // Browser notification izni kontrolü
    if (Notification.permission === "granted") {
      new Notification(title, {
        body,
        icon: "/logo192.png",
        tag: "dispatcher-notification",
        requireInteraction: true,
      });
    } else if (Notification.permission !== "denied") {
      Notification.requestPermission().then((permission) => {
        if (permission === "granted") {
          new Notification(title, { body, icon: "/logo192.png" });
        }
      });
    }
  }, []);

  // Browser notification izni iste (ilk yüklemede)
  useEffect(() => {
    if (isAuthenticated && Notification.permission === "default") {
      Notification.requestPermission();
    }
  }, [isAuthenticated]);

  // Tab değiştiğinde verileri yeniden çek
  useEffect(() => {
    if (!loading && isAuthenticated) {
      fetchData(true);
    }
  }, [activeTab]);

  // =========================================================================
  // KURYE ATAMA
  // NEDEN: Siparişe kurye atamak için ana fonksiyon
  // =========================================================================
  const handleAssignCourier = async (orderId, courierId) => {
    try {
      const result = await dispatcherService.assignCourier(orderId, courierId);

      if (result.success) {
        // Ses bildirimi çal
        playSound("orderAssigned");

        // Verileri yenile
        fetchData(true);

        // Modal'ı kapat
        setShowAssignModal(false);
        setAssigningOrderId(null);

        return { success: true };
      } else {
        return { success: false, error: result.error };
      }
    } catch (err) {
      console.error("[Dispatcher] Kurye atama hatası:", err);
      return { success: false, error: "Kurye atanamadı" };
    }
  };

  // =========================================================================
  // KURYE DEĞİŞTİRME
  // NEDEN: Atanmış kuryeyi değiştirmek için
  // =========================================================================
  const handleReassignCourier = async (orderId, newCourierId, reason) => {
    try {
      const result = await dispatcherService.reassignCourier(
        orderId,
        newCourierId,
        reason,
      );

      if (result.success) {
        playSound("orderAssigned");
        fetchData(true);
        return { success: true };
      } else {
        return { success: false, error: result.error };
      }
    } catch (err) {
      console.error("[Dispatcher] Kurye değiştirme hatası:", err);
      return { success: false, error: "Kurye değiştirilemedi" };
    }
  };

  // =========================================================================
  // FİLTRELEME
  // NEDEN: Siparişleri arama sorgusuna göre filtreler
  // =========================================================================
  const filteredOrders = orders.filter((order) => {
    if (!searchQuery) return true;
    const query = searchQuery.toLowerCase();
    return (
      order.orderNumber?.toLowerCase().includes(query) ||
      order.customerName?.toLowerCase().includes(query) ||
      order.address?.toLowerCase().includes(query)
    );
  });

  // =========================================================================
  // SES TOGGLE
  // =========================================================================
  const handleSoundToggle = () => {
    const newValue = !soundEnabled;
    setSoundEnabled(newValue);
    localStorage.setItem("dispatcherSoundEnabled", newValue.toString());

    // Test sesi çal
    if (newValue) {
      playSound("alert");
    }
  };

  // =========================================================================
  // LOGOUT
  // =========================================================================
  const handleLogout = () => {
    logout();
    navigate("/dispatch/login");
  };

  // =========================================================================
  // AUTH KONTROLÜ
  // =========================================================================
  if (authLoading) {
    return (
      <div
        className="min-vh-100 d-flex align-items-center justify-content-center"
        style={{
          background: "linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)",
        }}
      >
        <div className="text-center">
          <div
            className="spinner-border text-info mb-3"
            style={{ width: "3rem", height: "3rem" }}
          ></div>
          <p className="text-white-50">Yükleniyor...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    navigate("/dispatch/login");
    return null;
  }

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <>
      {/* Mobil CSS Eklentileri */}
      <style>{`
        /* Mobil için bottom navigation padding */
        @media (max-width: 768px) {
          .dispatch-content {
            padding-bottom: 80px !important;
          }
          .dispatch-stat-card .card-body {
            padding: 10px !important;
          }
          .dispatch-stat-card h3 {
            font-size: 1.3rem !important;
          }
          .dispatch-stat-card small {
            font-size: 0.65rem !important;
          }
          .dispatch-header-search {
            display: none !important;
          }
          .dispatch-tab-btn {
            padding: 8px 12px !important;
            font-size: 0.8rem !important;
          }
        }
        
        /* Bottom Navigation */
        .dispatch-bottom-nav {
          position: fixed;
          bottom: 0;
          left: 0;
          right: 0;
          background: rgba(26, 26, 46, 0.98);
          backdrop-filter: blur(10px);
          box-shadow: 0 -2px 15px rgba(0, 0, 0, 0.3);
          z-index: 1050;
          padding-bottom: env(safe-area-inset-bottom);
        }
        @media (min-width: 769px) {
          .dispatch-bottom-nav { display: none !important; }
        }
        
        .dispatch-nav-item {
          flex: 1;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          padding: 10px 5px;
          color: rgba(255,255,255,0.5);
          font-size: 0.65rem;
          border: none;
          background: transparent;
          cursor: pointer;
          transition: all 0.2s;
          min-height: 56px;
          position: relative;
        }
        .dispatch-nav-item.active {
          color: #667eea;
        }
        .dispatch-nav-item i {
          font-size: 1.1rem;
          margin-bottom: 3px;
        }
        .dispatch-nav-badge {
          position: absolute;
          top: 5px;
          right: 15%;
          min-width: 16px;
          height: 16px;
          padding: 0 4px;
          font-size: 0.55rem;
          font-weight: 700;
          line-height: 16px;
          border-radius: 8px;
        }
      `}</style>

      <div
        className="min-vh-100"
        style={{
          background: "linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)",
          color: "#fff",
        }}
      >
        {/* ================================================================== */}
        {/* HEADER */}
        {/* ================================================================== */}
        <header
          className="sticky-top py-3 px-4"
          style={{
            background: "rgba(26, 26, 46, 0.95)",
            backdropFilter: "blur(10px)",
            borderBottom: "1px solid rgba(255,255,255,0.1)",
          }}
        >
          <div className="container-fluid">
            <div className="row align-items-center">
              {/* Logo ve Başlık */}
              <div className="col-auto">
                <div className="d-flex align-items-center">
                  <div
                    className="d-flex align-items-center justify-content-center me-3"
                    style={{
                      width: "45px",
                      height: "45px",
                      borderRadius: "12px",
                      background:
                        "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                    }}
                  >
                    <i
                      className="fas fa-shipping-fast"
                      style={{ fontSize: "1.2rem" }}
                    ></i>
                  </div>
                  <div>
                    <h5 className="mb-0 fw-bold">Sevkiyat Paneli</h5>
                    <small className="text-white-50">
                      Son güncelleme: {lastUpdated.toLocaleTimeString("tr-TR")}
                    </small>
                  </div>
                </div>
              </div>

              {/* Arama */}
              <div className="col d-none d-md-block">
                <div className="input-group" style={{ maxWidth: "400px" }}>
                  <span
                    className="input-group-text border-0"
                    style={{ background: "rgba(255,255,255,0.1)" }}
                  >
                    <i className="fas fa-search text-white-50"></i>
                  </span>
                  <input
                    type="text"
                    className="form-control border-0 text-white"
                    placeholder="Sipariş no, müşteri adı veya adres ara..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    style={{
                      background: "rgba(255,255,255,0.1)",
                      borderRadius: "0 8px 8px 0",
                    }}
                  />
                </div>
              </div>

              {/* Sağ Taraf Butonları */}
              <div className="col-auto">
                <div className="d-flex align-items-center gap-3">
                  {/* Yenile Butonu */}
                  <button
                    className="btn btn-sm px-3"
                    onClick={() => fetchData(true)}
                    disabled={refreshing}
                    style={{
                      background: "rgba(255,255,255,0.1)",
                      color: "#fff",
                      border: "none",
                    }}
                  >
                    <i
                      className={`fas fa-sync-alt ${refreshing ? "fa-spin" : ""}`}
                    ></i>
                  </button>

                  {/* Ses Toggle */}
                  <button
                    className="btn btn-sm px-3"
                    onClick={handleSoundToggle}
                    title={soundEnabled ? "Sesi kapat" : "Sesi aç"}
                    style={{
                      background: soundEnabled
                        ? "rgba(40, 167, 69, 0.3)"
                        : "rgba(255,255,255,0.1)",
                      color: soundEnabled ? "#28a745" : "#fff",
                      border: soundEnabled
                        ? "1px solid rgba(40, 167, 69, 0.5)"
                        : "none",
                    }}
                  >
                    <i
                      className={`fas ${soundEnabled ? "fa-volume-up" : "fa-volume-mute"}`}
                    ></i>
                  </button>

                  {/* Kullanıcı Dropdown */}
                  <div className="dropdown">
                    <button
                      className="btn btn-sm d-flex align-items-center gap-2 px-3"
                      type="button"
                      data-bs-toggle="dropdown"
                      style={{
                        background: "rgba(255,255,255,0.1)",
                        color: "#fff",
                        border: "none",
                      }}
                    >
                      <div
                        className="d-flex align-items-center justify-content-center"
                        style={{
                          width: "28px",
                          height: "28px",
                          borderRadius: "50%",
                          background:
                            "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                        }}
                      >
                        <i
                          className="fas fa-user"
                          style={{ fontSize: "0.8rem" }}
                        ></i>
                      </div>
                      <span className="d-none d-md-inline">
                        {dispatcher?.name?.split(" ")[0] || "Kullanıcı"}
                      </span>
                      <i
                        className="fas fa-chevron-down"
                        style={{ fontSize: "0.7rem" }}
                      ></i>
                    </button>
                    <ul
                      className="dropdown-menu dropdown-menu-end shadow"
                      style={{
                        background: "#2d2d44",
                        border: "1px solid rgba(255,255,255,0.1)",
                      }}
                    >
                      <li>
                        <span
                          className="dropdown-item text-white-50"
                          style={{ background: "transparent" }}
                        >
                          <small>{dispatcher?.email}</small>
                        </span>
                      </li>
                      <li>
                        <hr
                          className="dropdown-divider"
                          style={{ borderColor: "rgba(255,255,255,0.1)" }}
                        />
                      </li>
                      <li>
                        <button
                          className="dropdown-item text-danger"
                          onClick={handleLogout}
                          style={{ background: "transparent" }}
                        >
                          <i className="fas fa-sign-out-alt me-2"></i>
                          Çıkış Yap
                        </button>
                      </li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </header>

        {/* ================================================================== */}
        {/* KURYE DURUM ÇUBUĞU */}
        {/* ================================================================== */}
        <CourierStatusBar
          couriers={couriers}
          onCourierClick={(courier) => {
            setSelectedCourier(courier);
            setShowCourierModal(true);
          }}
        />

        {/* ================================================================== */}
        {/* ANA İÇERİK */}
        {/* ================================================================== */}
        <main className="container-fluid py-4 px-4 dispatch-content">
          {/* İstatistik Kartları */}
          <div className="row g-3 mb-4">
            <div className="col-6 col-md-3">
              <div
                className="card border-0 h-100 dispatch-stat-card"
                style={{
                  background: "rgba(255,193,7,0.15)",
                  borderRadius: "16px",
                }}
              >
                <div className="card-body text-center py-3">
                  <i
                    className="fas fa-box text-warning mb-2"
                    style={{ fontSize: "1.5rem" }}
                  ></i>
                  <h3 className="mb-0 fw-bold text-warning">
                    {summary.readyCount || 0}
                  </h3>
                  <small className="text-white-50">Hazır Sipariş</small>
                </div>
              </div>
            </div>

            <div className="col-6 col-md-3">
              <div
                className="card border-0 h-100"
                style={{
                  background: "rgba(0,123,255,0.15)",
                  borderRadius: "16px",
                }}
              >
                <div className="card-body text-center py-3">
                  <i
                    className="fas fa-user-check text-info mb-2"
                    style={{ fontSize: "1.5rem" }}
                  ></i>
                  <h3 className="mb-0 fw-bold text-info">
                    {summary.assignedCount || 0}
                  </h3>
                  <small className="text-white-50">Atanan</small>
                </div>
              </div>
            </div>

            <div className="col-6 col-md-3">
              <div
                className="card border-0 h-100"
                style={{
                  background: "rgba(102,126,234,0.15)",
                  borderRadius: "16px",
                }}
              >
                <div className="card-body text-center py-3">
                  <i
                    className="fas fa-motorcycle text-primary mb-2"
                    style={{ fontSize: "1.5rem" }}
                  ></i>
                  <h3 className="mb-0 fw-bold" style={{ color: "#667eea" }}>
                    {summary.outForDeliveryCount || 0}
                  </h3>
                  <small className="text-white-50">Yolda</small>
                </div>
              </div>
            </div>

            <div className="col-6 col-md-3">
              <div
                className="card border-0 h-100"
                style={{
                  background: "rgba(40,167,69,0.15)",
                  borderRadius: "16px",
                }}
              >
                <div className="card-body text-center py-3">
                  <i
                    className="fas fa-check-circle text-success mb-2"
                    style={{ fontSize: "1.5rem" }}
                  ></i>
                  <h3 className="mb-0 fw-bold text-success">
                    {summary.deliveredTodayCount || 0}
                  </h3>
                  <small className="text-white-50">Bugün Teslim</small>
                </div>
              </div>
            </div>
          </div>

          {/* Tab Navigation */}
          <div className="mb-4">
            <ul className="nav nav-pills gap-2">
              <li className="nav-item">
                <button
                  className={`nav-link px-4 ${activeTab === "ready" ? "active" : ""}`}
                  onClick={() => setActiveTab("ready")}
                  style={{
                    background:
                      activeTab === "ready"
                        ? "linear-gradient(135deg, #ffc107 0%, #ff9800 100%)"
                        : "rgba(255,255,255,0.1)",
                    border: "none",
                    color: activeTab === "ready" ? "#000" : "#fff",
                    fontWeight: activeTab === "ready" ? "600" : "400",
                  }}
                >
                  <i className="fas fa-box me-2"></i>
                  Hazır
                  {summary.readyCount > 0 && (
                    <span className="badge bg-dark ms-2">
                      {summary.readyCount}
                    </span>
                  )}
                </button>
              </li>
              <li className="nav-item">
                <button
                  className={`nav-link px-4 ${activeTab === "assigned" ? "active" : ""}`}
                  onClick={() => setActiveTab("assigned")}
                  style={{
                    background:
                      activeTab === "assigned"
                        ? "linear-gradient(135deg, #0d6efd 0%, #0dcaf0 100%)"
                        : "rgba(255,255,255,0.1)",
                    border: "none",
                    color: "#fff",
                    fontWeight: activeTab === "assigned" ? "600" : "400",
                  }}
                >
                  <i className="fas fa-user-check me-2"></i>
                  Atanan
                  {summary.assignedCount > 0 && (
                    <span className="badge bg-light text-dark ms-2">
                      {summary.assignedCount}
                    </span>
                  )}
                </button>
              </li>
              <li className="nav-item">
                <button
                  className={`nav-link px-4 ${activeTab === "outForDelivery" ? "active" : ""}`}
                  onClick={() => setActiveTab("outForDelivery")}
                  style={{
                    background:
                      activeTab === "outForDelivery"
                        ? "linear-gradient(135deg, #667eea 0%, #764ba2 100%)"
                        : "rgba(255,255,255,0.1)",
                    border: "none",
                    color: "#fff",
                    fontWeight: activeTab === "outForDelivery" ? "600" : "400",
                  }}
                >
                  <i className="fas fa-motorcycle me-2"></i>
                  Yolda
                  {summary.outForDeliveryCount > 0 && (
                    <span className="badge bg-light text-dark ms-2">
                      {summary.outForDeliveryCount}
                    </span>
                  )}
                </button>
              </li>
            </ul>
          </div>

          {/* Hata Mesajı */}
          {error && (
            <div
              className="alert alert-danger d-flex align-items-center mb-4"
              role="alert"
            >
              <i className="fas fa-exclamation-triangle me-2"></i>
              {error}
              <button
                className="btn btn-sm btn-outline-danger ms-auto"
                onClick={() => fetchData(true)}
              >
                Tekrar Dene
              </button>
            </div>
          )}

          {/* Yükleniyor */}
          {loading && (
            <div className="text-center py-5">
              <div className="spinner-border text-info mb-3"></div>
              <p className="text-white-50">Siparişler yükleniyor...</p>
            </div>
          )}

          {/* Sipariş Listesi */}
          {!loading && (
            <div className="row g-3">
              {filteredOrders.length === 0 ? (
                <div className="col-12">
                  <div
                    className="card border-0 text-center py-5"
                    style={{
                      background: "rgba(255,255,255,0.05)",
                      borderRadius: "16px",
                    }}
                  >
                    <div className="card-body">
                      <i
                        className="fas fa-inbox text-white-50 mb-3"
                        style={{ fontSize: "3rem" }}
                      ></i>
                      <h5 className="text-white-50 mb-2">
                        {searchQuery
                          ? "Arama sonucu bulunamadı"
                          : `${activeTab === "ready" ? "Hazır" : activeTab === "assigned" ? "Atanmış" : "Yolda"} sipariş yok`}
                      </h5>
                      <p className="text-white-50 mb-0">
                        {searchQuery
                          ? "Farklı bir arama terimi deneyin"
                          : "Yeni siparişler geldiğinde burada görünecek"}
                      </p>
                    </div>
                  </div>
                </div>
              ) : (
                filteredOrders.map((order) => (
                  <div
                    key={order.orderId || order.id}
                    className="col-12 col-md-6 col-lg-4"
                  >
                    <ReadyOrderCard
                      order={order}
                      couriers={couriers}
                      onAssignCourier={handleAssignCourier}
                      onReassignCourier={handleReassignCourier}
                      showAssignButton={activeTab === "ready"}
                      showReassignButton={activeTab === "assigned"}
                    />
                  </div>
                ))
              )}
            </div>
          )}
        </main>

        {/* ================================================================== */}
        {/* KURYE DETAY MODAL */}
        {/* ================================================================== */}
        {showCourierModal && selectedCourier && (
          <CourierDetailModal
            courier={selectedCourier}
            onClose={() => {
              setShowCourierModal(false);
              setSelectedCourier(null);
            }}
          />
        )}

        {/* ================================================================== */}
        {/* KURYE ATAMA MODAL */}
        {/* ================================================================== */}
        {showAssignModal && assigningOrderId && (
          <div
            className="modal fade show d-block"
            style={{ background: "rgba(0,0,0,0.7)" }}
          >
            <div className="modal-dialog modal-dialog-centered">
              <div
                className="modal-content border-0"
                style={{
                  background: "#2d2d44",
                  borderRadius: "16px",
                }}
              >
                <div className="modal-header border-0">
                  <h5 className="modal-title text-white">Kurye Seç</h5>
                  <button
                    className="btn-close btn-close-white"
                    onClick={() => {
                      setShowAssignModal(false);
                      setAssigningOrderId(null);
                    }}
                  ></button>
                </div>
                <div className="modal-body">
                  <CourierSelect
                    couriers={couriers}
                    onSelect={(courierId) => {
                      handleAssignCourier(assigningOrderId, courierId);
                    }}
                  />
                </div>
              </div>
            </div>
          </div>
        )}

        {/* ================================================================== */}
        {/* MOBİL BOTTOM NAVİGATİON */}
        {/* ================================================================== */}
        <nav className="dispatch-bottom-nav d-md-none">
          <div className="d-flex">
            <button
              className={`dispatch-nav-item ${activeTab === "ready" ? "active" : ""}`}
              onClick={() => setActiveTab("ready")}
            >
              <i className="fas fa-box"></i>
              <span>Hazır</span>
              {summary.readyCount > 0 && (
                <span className="dispatch-nav-badge bg-warning text-dark">
                  {summary.readyCount}
                </span>
              )}
            </button>

            <button
              className={`dispatch-nav-item ${activeTab === "assigned" ? "active" : ""}`}
              onClick={() => setActiveTab("assigned")}
            >
              <i className="fas fa-user-check"></i>
              <span>Atanan</span>
              {summary.assignedCount > 0 && (
                <span className="dispatch-nav-badge bg-info">
                  {summary.assignedCount}
                </span>
              )}
            </button>

            <button
              className={`dispatch-nav-item ${activeTab === "outForDelivery" ? "active" : ""}`}
              onClick={() => setActiveTab("outForDelivery")}
            >
              <i className="fas fa-motorcycle"></i>
              <span>Yolda</span>
              {summary.outForDeliveryCount > 0 && (
                <span className="dispatch-nav-badge bg-primary">
                  {summary.outForDeliveryCount}
                </span>
              )}
            </button>

            <button
              className="dispatch-nav-item"
              onClick={() => fetchData(true)}
              disabled={refreshing}
            >
              <i
                className={`fas fa-sync-alt ${refreshing ? "fa-spin" : ""}`}
              ></i>
              <span>Yenile</span>
            </button>

            <button className="dispatch-nav-item" onClick={handleLogout}>
              <i className="fas fa-sign-out-alt"></i>
              <span>Çıkış</span>
            </button>
          </div>
        </nav>
      </div>
    </>
  );
}
