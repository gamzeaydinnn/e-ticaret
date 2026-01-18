// ==========================================================================
// CourierDeliveryList.jsx - Kurye Görev Listesi (Mobil Uyumlu)
// ==========================================================================
// Tüm teslimat görevlerinin listesi. Filtreleme, sıralama, arama özellikleri.
// Tabs: Bekleyen, Aktif, Tamamlanan, Tümü
// ==========================================================================

import React, { useState, useEffect, useCallback, useMemo } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useCourierAuth } from "../../contexts/CourierAuthContext";
import { useCourierSignalR } from "../../contexts/CourierSignalRContext";
import { CourierService } from "../../services/courierService";

export default function CourierDeliveryList() {
  // Context
  const { courier, isAuthenticated, loading: authLoading } = useCourierAuth();
  const { connectionState } = useCourierSignalR();

  // State
  const [tasks, setTasks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [activeTab, setActiveTab] = useState("active");
  const [searchQuery, setSearchQuery] = useState("");
  const [sortBy, setSortBy] = useState("priority"); // priority, time, distance
  const [showFilters, setShowFilters] = useState(false);

  const navigate = useNavigate();

  // =========================================================================
  // AUTH CHECK
  // =========================================================================
  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      navigate("/courier/login");
    }
  }, [isAuthenticated, authLoading, navigate]);

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================
  const loadTasks = useCallback(async () => {
    if (!courier?.id) return;

    try {
      const data = await CourierService.getAssignedOrders(courier.id);
      setTasks(data || []);
    } catch (error) {
      console.error("Görev yükleme hatası:", error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [courier?.id]);

  useEffect(() => {
    if (courier?.id) {
      loadTasks();
    }
  }, [courier?.id, loadTasks]);

  // Real-time event listener
  useEffect(() => {
    const handleTaskUpdate = () => loadTasks();

    window.addEventListener("courierTaskAssigned", handleTaskUpdate);
    window.addEventListener("courierTaskUpdated", handleTaskUpdate);
    window.addEventListener("courierTaskCancelled", handleTaskUpdate);

    return () => {
      window.removeEventListener("courierTaskAssigned", handleTaskUpdate);
      window.removeEventListener("courierTaskUpdated", handleTaskUpdate);
      window.removeEventListener("courierTaskCancelled", handleTaskUpdate);
    };
  }, [loadTasks]);

  // =========================================================================
  // FİLTRELEME VE SIRALAMA
  // =========================================================================
  const filteredTasks = useMemo(() => {
    let result = [...tasks];

    // Tab'a göre filtrele
    switch (activeTab) {
      case "pending":
        result = result.filter((t) =>
          ["Pending", "Assigned"].includes(t.status),
        );
        break;
      case "active":
        result = result.filter((t) =>
          ["PickedUp", "InTransit"].includes(t.status),
        );
        break;
      case "completed":
        result = result.filter((t) =>
          ["Delivered", "Failed", "Cancelled"].includes(t.status),
        );
        break;
      default:
        // all - filtreleme yok
        break;
    }

    // Arama sorgusu
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(
        (t) =>
          t.customerName?.toLowerCase().includes(query) ||
          t.deliveryAddress?.toLowerCase().includes(query) ||
          t.orderId?.toString().includes(query) ||
          t.id?.toString().includes(query),
      );
    }

    // Sıralama
    result.sort((a, b) => {
      switch (sortBy) {
        case "priority":
          const priorityOrder = { High: 0, Medium: 1, Low: 2, Normal: 1 };
          return (
            (priorityOrder[a.priority] || 1) - (priorityOrder[b.priority] || 1)
          );
        case "time":
          return (
            new Date(a.timeWindowStart || a.assignedAt) -
            new Date(b.timeWindowStart || b.assignedAt)
          );
        case "distance":
          return (a.distanceKm || 0) - (b.distanceKm || 0);
        default:
          return 0;
      }
    });

    return result;
  }, [tasks, activeTab, searchQuery, sortBy]);

  // Tab sayıları
  const tabCounts = useMemo(
    () => ({
      pending: tasks.filter((t) => ["Pending", "Assigned"].includes(t.status))
        .length,
      active: tasks.filter((t) => ["PickedUp", "InTransit"].includes(t.status))
        .length,
      completed: tasks.filter((t) =>
        ["Delivered", "Failed", "Cancelled"].includes(t.status),
      ).length,
      all: tasks.length,
    }),
    [tasks],
  );

  // =========================================================================
  // HELPERS
  // =========================================================================
  const getStatusText = (status) => {
    const statusMap = {
      Pending: "Bekliyor",
      Assigned: "Atandı",
      PickedUp: "Alındı",
      InTransit: "Yolda",
      Delivered: "Teslim Edildi",
      Failed: "Başarısız",
      Cancelled: "İptal",
    };
    return statusMap[status] || status;
  };

  const getStatusColor = (status) => {
    const colorMap = {
      Pending: "warning",
      Assigned: "info",
      PickedUp: "primary",
      InTransit: "success",
      Delivered: "secondary",
      Failed: "danger",
      Cancelled: "dark",
    };
    return colorMap[status] || "secondary";
  };

  const getPriorityIcon = (priority) => {
    switch (priority) {
      case "High":
        return { icon: "fa-arrow-up", color: "#dc3545" };
      case "Medium":
        return { icon: "fa-minus", color: "#ffc107" };
      case "Low":
        return { icon: "fa-arrow-down", color: "#28a745" };
      default:
        return { icon: "fa-minus", color: "#6c757d" };
    }
  };

  const formatTime = (dateStr) => {
    if (!dateStr) return "--:--";
    return new Date(dateStr).toLocaleTimeString("tr-TR", {
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const handleRefresh = async () => {
    setRefreshing(true);
    await loadTasks();
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
          <p className="text-muted">Görevler yükleniyor...</p>
        </div>
      </div>
    );
  }

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <>
      <style>{`
        .task-card {
          transition: all 0.2s ease;
          cursor: pointer;
        }
        .task-card:active {
          transform: scale(0.98);
        }
        .tab-btn {
          border: none;
          background: transparent;
          padding: 12px 16px;
          font-weight: 600;
          color: #6c757d;
          border-bottom: 3px solid transparent;
          transition: all 0.2s ease;
        }
        .tab-btn.active {
          color: #ff6b35;
          border-bottom-color: #ff6b35;
        }
        .filter-drawer {
          position: fixed;
          bottom: 0;
          left: 0;
          right: 0;
          background: white;
          border-radius: 20px 20px 0 0;
          z-index: 1060;
          transform: translateY(100%);
          transition: transform 0.3s ease;
          max-height: 60vh;
          overflow-y: auto;
        }
        .filter-drawer.show {
          transform: translateY(0);
        }
        .search-box {
          border: 2px solid #e9ecef;
          border-radius: 12px;
          transition: border-color 0.2s ease;
        }
        .search-box:focus {
          border-color: #ff6b35;
          box-shadow: none;
        }
        @media (max-width: 768px) {
          .mobile-header {
            padding: 12px 16px !important;
          }
        }
        .empty-state {
          padding: 60px 20px;
        }
        .priority-indicator {
          width: 4px;
          position: absolute;
          left: 0;
          top: 0;
          bottom: 0;
          border-radius: 4px 0 0 4px;
        }
      `}</style>

      <div className="min-vh-100 bg-light pb-5">
        {/* Header */}
        <nav
          className="navbar navbar-dark mobile-header sticky-top"
          style={{ background: "linear-gradient(135deg, #ff6b35, #ff8c00)" }}
        >
          <div className="container-fluid">
            <div className="d-flex align-items-center">
              <Link
                to="/courier/dashboard"
                className="text-white text-decoration-none me-3"
              >
                <i className="fas fa-arrow-left fs-5"></i>
              </Link>
              <span className="navbar-brand mb-0 fw-bold">
                <i className="fas fa-tasks me-2"></i>
                Görevlerim
              </span>
            </div>
            <div className="d-flex align-items-center gap-2">
              {/* Connection indicator */}
              <div
                className="px-2 py-1 rounded-pill d-flex align-items-center"
                style={{ backgroundColor: "rgba(255,255,255,0.2)" }}
              >
                <div
                  style={{
                    width: "8px",
                    height: "8px",
                    borderRadius: "50%",
                    backgroundColor:
                      connectionState === "connected" ? "#28a745" : "#dc3545",
                  }}
                ></div>
              </div>

              {/* Refresh */}
              <button
                className="btn btn-link text-white p-2"
                onClick={handleRefresh}
                disabled={refreshing}
              >
                <i
                  className={`fas fa-sync-alt ${refreshing ? "fa-spin" : ""}`}
                ></i>
              </button>

              {/* Filter */}
              <button
                className="btn btn-link text-white p-2"
                onClick={() => setShowFilters(!showFilters)}
              >
                <i className="fas fa-sliders-h"></i>
              </button>
            </div>
          </div>
        </nav>

        {/* Search Bar */}
        <div className="bg-white border-bottom p-3">
          <div className="position-relative">
            <i
              className="fas fa-search position-absolute text-muted"
              style={{
                left: "15px",
                top: "50%",
                transform: "translateY(-50%)",
              }}
            ></i>
            <input
              type="text"
              className="form-control search-box ps-5"
              placeholder="Müşteri, adres veya sipariş no ile ara..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              style={{ padding: "12px 16px 12px 45px" }}
            />
            {searchQuery && (
              <button
                className="btn btn-link position-absolute text-muted p-0"
                style={{
                  right: "15px",
                  top: "50%",
                  transform: "translateY(-50%)",
                }}
                onClick={() => setSearchQuery("")}
              >
                <i className="fas fa-times"></i>
              </button>
            )}
          </div>
        </div>

        {/* Tabs */}
        <div
          className="bg-white border-bottom d-flex overflow-auto"
          style={{ whiteSpace: "nowrap" }}
        >
          <button
            className={`tab-btn flex-shrink-0 ${activeTab === "pending" ? "active" : ""}`}
            onClick={() => setActiveTab("pending")}
          >
            Bekleyen
            {tabCounts.pending > 0 && (
              <span className="badge bg-warning text-dark ms-2 rounded-pill">
                {tabCounts.pending}
              </span>
            )}
          </button>
          <button
            className={`tab-btn flex-shrink-0 ${activeTab === "active" ? "active" : ""}`}
            onClick={() => setActiveTab("active")}
          >
            Aktif
            {tabCounts.active > 0 && (
              <span className="badge bg-success ms-2 rounded-pill">
                {tabCounts.active}
              </span>
            )}
          </button>
          <button
            className={`tab-btn flex-shrink-0 ${activeTab === "completed" ? "active" : ""}`}
            onClick={() => setActiveTab("completed")}
          >
            Tamamlanan
          </button>
          <button
            className={`tab-btn flex-shrink-0 ${activeTab === "all" ? "active" : ""}`}
            onClick={() => setActiveTab("all")}
          >
            Tümü
            <span className="badge bg-secondary ms-2 rounded-pill">
              {tabCounts.all}
            </span>
          </button>
        </div>

        {/* Task List */}
        <div className="container-fluid p-3">
          {filteredTasks.length === 0 ? (
            <div className="text-center empty-state">
              <div className="mb-4">
                <i
                  className={`fas ${
                    activeTab === "pending"
                      ? "fa-hourglass-half"
                      : activeTab === "active"
                        ? "fa-truck"
                        : activeTab === "completed"
                          ? "fa-check-circle"
                          : "fa-inbox"
                  } 
                    text-muted`}
                  style={{ fontSize: "64px" }}
                ></i>
              </div>
              <h5 className="text-muted fw-bold">
                {activeTab === "pending"
                  ? "Bekleyen görev yok"
                  : activeTab === "active"
                    ? "Aktif görev yok"
                    : activeTab === "completed"
                      ? "Tamamlanan görev yok"
                      : "Henüz görev atanmamış"}
              </h5>
              <p className="text-muted small">
                {searchQuery
                  ? "Arama kriterlerinize uygun görev bulunamadı."
                  : "Yeni görevler atandığında burada görünecektir."}
              </p>
              {searchQuery && (
                <button
                  className="btn btn-outline-secondary btn-sm"
                  onClick={() => setSearchQuery("")}
                >
                  Aramayı Temizle
                </button>
              )}
            </div>
          ) : (
            <div className="row g-3">
              {filteredTasks.map((task) => {
                const priority = getPriorityIcon(task.priority);

                return (
                  <div key={task.id} className="col-12">
                    <div
                      className="card border-0 shadow-sm task-card position-relative"
                      style={{ borderRadius: "16px", overflow: "hidden" }}
                      onClick={() => navigate(`/courier/delivery/${task.id}`)}
                    >
                      {/* Priority Indicator */}
                      <div
                        className="priority-indicator"
                        style={{ backgroundColor: priority.color }}
                      ></div>

                      <div className="card-body p-3 ps-4">
                        {/* Header Row */}
                        <div className="d-flex justify-content-between align-items-start mb-2">
                          <div className="d-flex align-items-center">
                            <div
                              className="rounded-circle p-2 me-2 d-flex align-items-center justify-content-center"
                              style={{
                                width: "40px",
                                height: "40px",
                                backgroundColor: priority.color + "20",
                              }}
                            >
                              <i
                                className={`fas ${priority.icon}`}
                                style={{
                                  color: priority.color,
                                  fontSize: "12px",
                                }}
                              ></i>
                            </div>
                            <div>
                              <h6 className="mb-0 fw-bold">
                                #{task.orderId || task.id}
                              </h6>
                              <small className="text-muted">
                                <i className="fas fa-clock me-1"></i>
                                {formatTime(task.timeWindowStart)}
                                {task.timeWindowEnd &&
                                  ` - ${formatTime(task.timeWindowEnd)}`}
                              </small>
                            </div>
                          </div>
                          <span
                            className={`badge bg-${getStatusColor(task.status)}`}
                            style={{ borderRadius: "8px" }}
                          >
                            {getStatusText(task.status)}
                          </span>
                        </div>

                        {/* Customer Info */}
                        <div className="mb-2">
                          <p className="mb-1 small">
                            <i className="fas fa-user me-2 text-muted"></i>
                            <span className="fw-semibold">
                              {task.customerName || "Müşteri"}
                            </span>
                          </p>
                          {task.customerPhone && (
                            <p className="mb-1 small">
                              <i className="fas fa-phone me-2 text-muted"></i>
                              {task.customerPhone}
                            </p>
                          )}
                        </div>

                        {/* Address */}
                        <p className="text-muted small mb-2">
                          <i className="fas fa-map-marker-alt me-2"></i>
                          {task.deliveryAddress ||
                            task.address ||
                            "Adres bilgisi yok"}
                        </p>

                        {/* Footer Row */}
                        <div className="d-flex justify-content-between align-items-center pt-2 border-top">
                          <div className="d-flex align-items-center gap-3">
                            <span
                              className="fw-bold"
                              style={{ color: "#ff6b35" }}
                            >
                              {task.orderTotal?.toFixed(2) || "0.00"} ₺
                            </span>
                            {task.distanceKm && (
                              <span className="text-muted small">
                                <i className="fas fa-route me-1"></i>
                                {task.distanceKm.toFixed(1)} km
                              </span>
                            )}
                          </div>
                          <button
                            className="btn btn-sm"
                            style={{
                              background:
                                "linear-gradient(135deg, #ff6b35, #ff8c00)",
                              color: "white",
                              borderRadius: "8px",
                              padding: "6px 16px",
                            }}
                            onClick={(e) => {
                              e.stopPropagation();
                              navigate(`/courier/delivery/${task.id}`);
                            }}
                          >
                            Detay
                            <i className="fas fa-chevron-right ms-2"></i>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Filter Drawer Overlay */}
        {showFilters && (
          <div
            className="position-fixed top-0 start-0 w-100 h-100"
            style={{ backgroundColor: "rgba(0,0,0,0.5)", zIndex: 1050 }}
            onClick={() => setShowFilters(false)}
          ></div>
        )}

        {/* Filter Drawer */}
        <div className={`filter-drawer shadow-lg ${showFilters ? "show" : ""}`}>
          <div className="p-4">
            <div className="d-flex justify-content-between align-items-center mb-4">
              <h5 className="fw-bold mb-0">
                <i
                  className="fas fa-sliders-h me-2"
                  style={{ color: "#ff6b35" }}
                ></i>
                Sıralama & Filtre
              </h5>
              <button
                className="btn-close"
                onClick={() => setShowFilters(false)}
              ></button>
            </div>

            {/* Sort Options */}
            <div className="mb-4">
              <h6 className="text-muted small mb-3">SIRALAMA</h6>
              <div className="d-flex flex-column gap-2">
                {[
                  {
                    value: "priority",
                    label: "Önceliğe Göre",
                    icon: "fa-sort-amount-up",
                  },
                  { value: "time", label: "Zamana Göre", icon: "fa-clock" },
                  {
                    value: "distance",
                    label: "Mesafeye Göre",
                    icon: "fa-route",
                  },
                ].map((option) => (
                  <button
                    key={option.value}
                    className={`btn ${sortBy === option.value ? "btn-primary" : "btn-outline-secondary"} text-start`}
                    style={{ borderRadius: "12px" }}
                    onClick={() => setSortBy(option.value)}
                  >
                    <i className={`fas ${option.icon} me-2`}></i>
                    {option.label}
                    {sortBy === option.value && (
                      <i className="fas fa-check float-end mt-1"></i>
                    )}
                  </button>
                ))}
              </div>
            </div>

            {/* Apply Button */}
            <button
              className="btn btn-lg w-100 text-white fw-bold"
              style={{
                background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                borderRadius: "12px",
              }}
              onClick={() => setShowFilters(false)}
            >
              Uygula
            </button>
          </div>
        </div>

        {/* Bottom Navigation (Mobile) */}
        <nav
          className="navbar fixed-bottom navbar-light bg-white border-top d-md-none"
          style={{
            paddingBottom: "env(safe-area-inset-bottom)",
            boxShadow: "0 -2px 10px rgba(0,0,0,0.1)",
          }}
        >
          <div className="container-fluid d-flex justify-content-around py-2">
            <Link
              to="/courier/dashboard"
              className="text-center text-decoration-none text-muted"
            >
              <i className="fas fa-home d-block fs-5"></i>
              <small style={{ fontSize: "10px" }}>Ana Sayfa</small>
            </Link>
            <Link
              to="/courier/orders"
              className="text-center text-decoration-none"
              style={{ color: "#ff6b35" }}
            >
              <i className="fas fa-list d-block fs-5"></i>
              <small style={{ fontSize: "10px" }}>Görevler</small>
            </Link>
            <Link
              to="/courier/map"
              className="text-center text-decoration-none text-muted"
            >
              <i className="fas fa-map d-block fs-5"></i>
              <small style={{ fontSize: "10px" }}>Harita</small>
            </Link>
            <Link
              to="/courier/profile"
              className="text-center text-decoration-none text-muted"
            >
              <i className="fas fa-user d-block fs-5"></i>
              <small style={{ fontSize: "10px" }}>Profil</small>
            </Link>
          </div>
        </nav>
      </div>
    </>
  );
}
