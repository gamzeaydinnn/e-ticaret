import { useEffect, useMemo, useState, useCallback, useRef } from "react";
import { AdminService } from "../../services/adminService";
import { MicroService } from "../../services/microService";
import { useAuth } from "../../contexts/AuthContext";
import { useAdminSignalR } from "../../contexts/AdminSignalRContext";
import { PERMISSIONS } from "../../services/permissionService";
import "./styles/AdminDashboard.css";

/** Canlı yenileme: SignalR event'lerini kısa debounce ile birleştir */
const LIVE_REFRESH_DEBOUNCE_MS = 400;
/** Sekme açıkken periyodik güncel veri */
const LIVE_POLL_MS = 15000;
/** Sekme gizliyken daha seyrek */
const HIDDEN_POLL_MS = 60000;

const formatCurrency = (value) =>
  `₺${Number(value || 0).toLocaleString("tr-TR", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  })}`;

const readField = (obj, ...keys) => {
  for (const key of keys) {
    if (obj && obj[key] !== undefined && obj[key] !== null) {
      return obj[key];
    }
  }
  return undefined;
};

const statusLabelMap = {
  Delivered: "Teslim Edildi",
  Completed: "Tamamlandı",
  OutForDelivery: "Yolda",
  Preparing: "Hazırlanıyor",
  Confirmed: "Onaylandı",
  Pending: "Beklemede",
  Cancelled: "İptal",
  Assigned: "Atandı",
  Ready: "Hazır",
  Paid: "Ödendi",
  PaymentFailed: "Ödeme Hatası",
  New: "Yeni",
  Refunded: "İade Edildi",
  PartialRefund: "Kısmi İade",
  InTransit: "Kargoda",
  PickedUp: "Alındı",
  Shipped: "Gönderildi",
  DeliveryFailed: "Teslimat Başarısız",
};

const DashboardBarChart = ({ data, valueKey, colorClass }) => {
  const maxValue = Math.max(1, ...data.map((x) => Number(x[valueKey] || 0)));

  return (
    <div className="dashboard-chart">
      {data.map((point) => {
        const value = Number(point[valueKey] || 0);
        const height = Math.max(6, Math.round((value / maxValue) * 96));
        const shortDate = point.date?.slice(5) || "-";

        return (
          <div key={`${valueKey}-${point.date}`} className="dashboard-bar-wrap">
            <div
              className={`dashboard-bar ${colorClass}`}
              style={{ height: `${height}px` }}
              title={`${shortDate}: ${value.toLocaleString("tr-TR")}`}
            />
            <span className="dashboard-bar-label">{shortDate}</span>
          </div>
        );
      })}
    </div>
  );
};

const DistributionList = ({ title, items, theme }) => {
  const total = items.reduce((sum, x) => sum + Number(x.count || 0), 0);

  return (
    <div className="dashboard-panel h-100">
      <div className="dashboard-panel-header">
        <h6>{title}</h6>
        <span>{total.toLocaleString("tr-TR")}</span>
      </div>
      <div className="dashboard-distribution-list">
        {items.length === 0 && (
          <p className="text-muted small mb-0">Veri bulunamadı.</p>
        )}
        {items.map((item) => {
          const count = Number(item.count || 0);
          const percent = total > 0 ? Math.round((count / total) * 100) : 0;
          const label = statusLabelMap[item.label] || item.label;

          return (
            <div
              key={`${title}-${item.label}`}
              className="dashboard-distribution-item"
            >
              <div className="dashboard-distribution-top">
                <span>{label}</span>
                <strong>{count.toLocaleString("tr-TR")}</strong>
              </div>
              <div className="dashboard-progress-track">
                <div
                  className={`dashboard-progress-fill ${theme}`}
                  style={{ width: `${percent}%` }}
                />
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default function Dashboard() {
  const { hasPermission, user } = useAuth();
  // Merkezi SignalR bağlantısı — real-time event'ler için
  const { isConnected: signalRConnected } = useAdminSignalR();

  const [stats, setStats] = useState({
    totalUsers: 0,
    totalOrders: 0,
    totalRevenue: 0,
    totalProducts: 0,
    outOfStockCount: 0,
    lowStockCount: 0,
    todayOrders: 0,
    activeCouriers: 0,
    pendingOrders: 0,
    deliveredOrders: 0,
    cancelledOrders: 0,
    refundedOrders: 0,
    pendingRefundRequests: 0,
    failedRefunds: 0,
    totalRefundedAmount: 0,
    recentOrders: [],
    topProducts: [],
    dailyMetrics: [],
    orderStatusDistribution: [],
    paymentStatusDistribution: [],
    userRoleDistribution: [],
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [lastUpdated, setLastUpdated] = useState(null);
  const [initialLoadDone, setInitialLoadDone] = useState(false);

  const [erpStatus, setErpStatus] = useState({
    isConnected: null,
    message: "",
    lastSync: null,
    loading: false,
  });

  const isSuperAdmin = user?.role === "SuperAdmin";
  const canViewStatistics =
    isSuperAdmin || hasPermission?.(PERMISSIONS.DASHBOARD_STATISTICS);
  const canViewRevenue =
    isSuperAdmin || hasPermission?.(PERMISSIONS.DASHBOARD_REVENUE);

  const chartData = useMemo(() => {
    if (!Array.isArray(stats.dailyMetrics) || stats.dailyMetrics.length === 0) {
      return [];
    }
    return stats.dailyMetrics.slice(-7);
  }, [stats.dailyMetrics]);

  const checkErpConnection = async () => {
    setErpStatus((prev) => ({ ...prev, loading: true }));
    try {
      const result = await MicroService.testConnection();
      const connected = Boolean(
        result?.isConnected ?? result?.success ?? result?.mikroApiOnline,
      );
      setErpStatus({
        isConnected: connected,
        message: result?.message || "",
        lastSync: new Date().toISOString(),
        loading: false,
      });
    } catch (err) {
      setErpStatus({
        isConnected: false,
        message: err.message || "Bağlantı hatası",
        lastSync: new Date().toISOString(),
        loading: false,
      });
    }
  };

  const loadDashboardStats = useCallback(async (isRefresh = false) => {
    try {
      if (!isRefresh) setLoading(true);
      setError(null);
      const data = await AdminService.getDashboardStats();

      const totalUsers = readField(data, "totalUsers", "TotalUsers") || 0;
      const totalOrders = readField(data, "totalOrders", "TotalOrders") || 0;
      const totalRevenue =
        readField(data, "totalRevenue", "TotalRevenue", "revenue", "Revenue") ||
        0;
      const totalProducts =
        readField(data, "totalProducts", "TotalProducts") || 0;
      const outOfStockCount =
        readField(data, "outOfStockCount", "OutOfStockCount") || 0;
      const lowStockCount =
        readField(data, "lowStockCount", "LowStockCount") || 0;
      const todayOrders = readField(data, "todayOrders", "TodayOrders") || 0;
      const activeCouriers =
        readField(
          data,
          "activeCouriers",
          "ActiveCouriers",
          "totalCouriers",
          "TotalCouriers",
        ) || 0;
      const pendingOrders =
        readField(data, "pendingOrders", "PendingOrders") || 0;
      const deliveredOrders =
        readField(data, "deliveredOrders", "DeliveredOrders") || 0;
      const recentOrders =
        readField(data, "recentOrders", "RecentOrders") || [];
      const topProducts = readField(data, "topProducts", "TopProducts") || [];
      const dailyMetrics =
        readField(data, "dailyMetrics", "DailyMetrics") || [];
      const orderStatusDistribution =
        readField(data, "orderStatusDistribution", "OrderStatusDistribution") ||
        [];
      const paymentStatusDistribution =
        readField(
          data,
          "paymentStatusDistribution",
          "PaymentStatusDistribution",
        ) || [];
      const userRoleDistribution =
        readField(data, "userRoleDistribution", "UserRoleDistribution") || [];

      const cancelledOrders =
        readField(data, "cancelledOrders", "CancelledOrders") || 0;
      const refundedOrders =
        readField(data, "refundedOrders", "RefundedOrders") || 0;
      const pendingRefundRequests =
        readField(data, "pendingRefundRequests", "PendingRefundRequests") || 0;
      const failedRefunds =
        readField(data, "failedRefunds", "FailedRefunds") || 0;
      const totalRefundedAmount =
        readField(data, "totalRefundedAmount", "TotalRefundedAmount") || 0;

      setStats({
        totalUsers,
        totalOrders,
        totalRevenue,
        totalProducts,
        outOfStockCount,
        lowStockCount,
        todayOrders,
        activeCouriers,
        pendingOrders,
        deliveredOrders,
        cancelledOrders,
        refundedOrders,
        pendingRefundRequests,
        failedRefunds,
        totalRefundedAmount,
        recentOrders,
        topProducts,
        dailyMetrics,
        orderStatusDistribution,
        paymentStatusDistribution,
        userRoleDistribution,
      });
      setLastUpdated(new Date());
    } catch (err) {
      console.error("Dashboard stats yüklenirken hata:", err);
      const serverMsg =
        err?.response?.data?.message ||
        err?.data?.message ||
        err?.message;
      setError(
        serverMsg
          ? `Dashboard verileri yüklenemedi: ${serverMsg}`
          : "Dashboard verileri yüklenemedi. Sunucu bağlantısını kontrol edin.",
      );
    } finally {
      setLoading(false);
      setInitialLoadDone(true);
    }
  }, []);

  const liveRefreshTimerRef = useRef(null);
  const scheduleLiveRefresh = useCallback(() => {
    if (liveRefreshTimerRef.current) {
      clearTimeout(liveRefreshTimerRef.current);
    }
    liveRefreshTimerRef.current = setTimeout(() => {
      loadDashboardStats(true);
    }, LIVE_REFRESH_DEBOUNCE_MS);
  }, [loadDashboardStats]);

  useEffect(() => {
    let cancelled = false;
    let pollTimer = null;

    const boot = async () => {
      await loadDashboardStats();
      if (!cancelled) {
        setTimeout(() => {
          if (!cancelled) checkErpConnection();
        }, 0);
      }
    };

    boot();

    const startPolling = () => {
      if (pollTimer) clearInterval(pollTimer);
      const interval =
        document.visibilityState === "hidden" ? HIDDEN_POLL_MS : LIVE_POLL_MS;
      pollTimer = setInterval(() => {
        if (document.visibilityState === "visible") {
          loadDashboardStats(true);
        }
      }, interval);
    };

    startPolling();

    const onVisibility = () => {
      if (document.visibilityState === "visible") {
        loadDashboardStats(true);
      }
      startPolling();
    };

    const onFocus = () => loadDashboardStats(true);

    document.addEventListener("visibilitychange", onVisibility);
    window.addEventListener("focus", onFocus);

    const erpInterval = setInterval(checkErpConnection, 120000);

    return () => {
      cancelled = true;
      if (pollTimer) clearInterval(pollTimer);
      if (liveRefreshTimerRef.current) clearTimeout(liveRefreshTimerRef.current);
      clearInterval(erpInterval);
      document.removeEventListener("visibilitychange", onVisibility);
      window.removeEventListener("focus", onFocus);
    };
  }, [loadDashboardStats]);

  useEffect(() => {
    const handleDashboardRefresh = () => scheduleLiveRefresh();

    window.addEventListener("adminDashboardUpdate", handleDashboardRefresh);
    window.addEventListener("adminNewOrder", handleDashboardRefresh);
    window.addEventListener("adminOrderStatusChanged", handleDashboardRefresh);
    window.addEventListener("adminPaymentSuccess", handleDashboardRefresh);
    window.addEventListener("adminOrderCancelled", handleDashboardRefresh);
    window.addEventListener("adminPaymentFailed", handleDashboardRefresh);

    return () => {
      window.removeEventListener("adminDashboardUpdate", handleDashboardRefresh);
      window.removeEventListener("adminNewOrder", handleDashboardRefresh);
      window.removeEventListener(
        "adminOrderStatusChanged",
        handleDashboardRefresh,
      );
      window.removeEventListener("adminPaymentSuccess", handleDashboardRefresh);
      window.removeEventListener("adminOrderCancelled", handleDashboardRefresh);
      window.removeEventListener("adminPaymentFailed", handleDashboardRefresh);
    };
  }, [scheduleLiveRefresh]);

  if (loading && !initialLoadDone) {
    return (
      <div className="dashboard-loading">
        <div className="spinner-border" role="status"></div>
        <p>Dashboard yükleniyor...</p>
      </div>
    );
  }

  if (error && !initialLoadDone) {
    return (
      <div className="container-fluid mt-4">
        <div className="alert alert-danger d-flex justify-content-between align-items-center">
          <span>
            <i className="fas fa-circle-exclamation me-2"></i>
            {error}
          </span>
          <button
            className="btn btn-outline-danger btn-sm"
            onClick={loadDashboardStats}
          >
            Tekrar Dene
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-dashboard-pro container-fluid py-3 py-md-4">
      <div className="dashboard-hero">
        <div>
          <h1>Yönetim Dashboard</h1>
          <p>Güncel iş metrikleri — sipariş ve operasyon canlı izlenir</p>
          <div className="d-flex flex-wrap align-items-center gap-2 mt-2">
            <span
              className={`badge ${signalRConnected ? "bg-success" : "bg-secondary"}`}
              title={
                signalRConnected
                  ? "SignalR bağlı — anlık güncelleme aktif"
                  : "SignalR kapalı — periyodik yenileme kullanılıyor"
              }
            >
              <i
                className={`fas ${signalRConnected ? "fa-bolt" : "fa-clock"} me-1`}
              ></i>
              {signalRConnected ? "Canlı" : "Periyodik"}
            </span>
            {lastUpdated && (
              <small className="text-muted">
                Son güncelleme: {lastUpdated.toLocaleTimeString("tr-TR")}
              </small>
            )}
          </div>
        </div>
        <div className="dashboard-hero-actions">
          <button
            className="btn btn-dark"
            onClick={() => loadDashboardStats(false)}
            disabled={loading}
          >
            <i
              className={`fas ${loading ? "fa-spinner fa-spin" : "fa-rotate"} me-2`}
            ></i>
            {loading ? "Yükleniyor..." : "Yenile"}
          </button>
        </div>
      </div>

      {error && initialLoadDone && (
        <div className="alert alert-warning d-flex justify-content-between align-items-center mt-3">
          <span>
            <i className="fas fa-triangle-exclamation me-2"></i>
            {error}
          </span>
          <button
            className="btn btn-outline-warning btn-sm"
            onClick={() => loadDashboardStats(false)}
          >
            Tekrar Dene
          </button>
        </div>
      )}

      {!canViewStatistics && !canViewRevenue && (
        <div className="alert alert-info mt-3">
          Dashboard verilerini görmek için gerekli izinler bulunamadı.
        </div>
      )}

      <div className="row g-3 mt-1">
        {canViewStatistics && (
          <>
            <div className="col-6 col-lg-3">
              <div className="dashboard-kpi kpi-indigo">
                <span>Toplam Kullanıcı</span>
                <strong>{stats.totalUsers.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="dashboard-kpi kpi-green">
                <span>Web&apos;de Aktif Ürün</span>
                <strong>{stats.totalProducts.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            {/* Stokta Yok: stoğu tamamen biten ürünler — kırmızı vurgu ile dikkat çeker */}
            <div className="col-6 col-lg-3">
              <div
                className="dashboard-kpi kpi-pink"
                style={{ borderLeft: "4px solid #ef4444" }}
              >
                <span>Stokta Yok</span>
                <strong>{stats.outOfStockCount.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            {/* Kritik Stok: eşik altındaki (ama sıfır olmayan) ürünler — sarı vurgu */}
            <div className="col-6 col-lg-3">
              <div
                className="dashboard-kpi kpi-amber"
                style={{ borderLeft: "4px solid #f59e0b" }}
              >
                <span>Kritik Stok</span>
                <strong>{stats.lowStockCount.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="dashboard-kpi kpi-pink">
                <span>Toplam Sipariş</span>
                <strong>{stats.totalOrders.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="dashboard-kpi kpi-cyan">
                <span>Aktif Kurye</span>
                <strong>{stats.activeCouriers.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="dashboard-kpi kpi-amber">
                <span>Bugünkü Sipariş</span>
                <strong>{stats.todayOrders.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="dashboard-kpi kpi-violet">
                <span>Bekleyen Sipariş</span>
                <strong>{stats.pendingOrders.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="dashboard-kpi kpi-slate">
                <span>Teslim Edilen</span>
                <strong>{stats.deliveredOrders.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="dashboard-kpi kpi-pink">
                <span>İptal Edilen</span>
                <strong>{stats.cancelledOrders.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="dashboard-kpi kpi-cyan">
                <span>İade Edilen</span>
                <strong>{stats.refundedOrders.toLocaleString("tr-TR")}</strong>
              </div>
            </div>
            {stats.pendingRefundRequests > 0 && (
              <div className="col-6 col-lg-3">
                <div className="dashboard-kpi kpi-amber" style={{borderLeft: '4px solid #f59e0b'}}>
                  <span>Bekleyen İade Talebi</span>
                  <strong>{stats.pendingRefundRequests.toLocaleString("tr-TR")}</strong>
                </div>
              </div>
            )}
            {stats.failedRefunds > 0 && (
              <div className="col-6 col-lg-3">
                <div className="dashboard-kpi kpi-pink" style={{borderLeft: '4px solid #ef4444'}}>
                  <span>Başarısız İade</span>
                  <strong>{stats.failedRefunds.toLocaleString("tr-TR")}</strong>
                </div>
              </div>
            )}
          </>
        )}

        {canViewRevenue && (
          <>
            <div className="col-6 col-lg-3">
              <div className="dashboard-kpi kpi-orange">
                <span>Toplam Gelir</span>
                <strong>{formatCurrency(stats.totalRevenue)}</strong>
              </div>
            </div>
            {stats.totalRefundedAmount > 0 && (
              <div className="col-6 col-lg-3">
                <div className="dashboard-kpi kpi-violet">
                  <span>Toplam İade Tutarı</span>
                  <strong>{formatCurrency(stats.totalRefundedAmount)}</strong>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      <div className="row g-3 mt-1">
        <div className="col-12 col-xl-8">
          <div className="dashboard-panel h-100">
            <div className="dashboard-panel-header">
              <h6>Son 7 Gün Sipariş Trendi</h6>
              <span>
                {chartData.reduce((s, p) => s + Number(p.orders || 0), 0)}{" "}
                sipariş
              </span>
            </div>
            <DashboardBarChart
              data={chartData}
              valueKey="orders"
              colorClass="bar-orders"
            />
          </div>
        </div>
        <div className="col-12 col-xl-4">
          <div className="dashboard-panel h-100">
            <div className="dashboard-panel-header">
              <h6>Son 7 Gün Gelir Trendi</h6>
              <span>
                {formatCurrency(
                  chartData.reduce((s, p) => s + Number(p.revenue || 0), 0),
                )}
              </span>
            </div>
            <DashboardBarChart
              data={chartData}
              valueKey="revenue"
              colorClass="bar-revenue"
            />
          </div>
        </div>
      </div>

      <div className="row g-3 mt-1">
        <div className="col-12 col-lg-4">
          <DistributionList
            title="Sipariş Durumları"
            items={stats.orderStatusDistribution}
            theme="theme-orange"
          />
        </div>
        <div className="col-12 col-lg-4">
          <DistributionList
            title="Ödeme Durumları"
            items={stats.paymentStatusDistribution}
            theme="theme-teal"
          />
        </div>
        <div className="col-12 col-lg-4">
          <DistributionList
            title="Kullanıcı Rolleri"
            items={stats.userRoleDistribution}
            theme="theme-indigo"
          />
        </div>
      </div>

      <div className="row g-3 mt-1">
        <div className="col-12 col-xl-8">
          <div className="dashboard-panel h-100">
            <div className="dashboard-panel-header">
              <h6>Son Siparişler</h6>
              <span>{stats.recentOrders.length} kayıt</span>
            </div>
            <div className="table-responsive">
              <table className="table table-sm align-middle mb-0 dashboard-table">
                <thead>
                  <tr>
                    <th>Sipariş</th>
                    <th>Müşteri</th>
                    <th>Tutar</th>
                    <th>Durum</th>
                    <th>Tarih</th>
                  </tr>
                </thead>
                <tbody>
                  {stats.recentOrders.length === 0 ? (
                    <tr>
                      <td colSpan="5" className="text-center text-muted py-4">
                        Sipariş verisi yok.
                      </td>
                    </tr>
                  ) : (
                    stats.recentOrders.map((order) => (
                      <tr key={order.id}>
                        <td className="fw-semibold">
                          {order.orderNumber || `#${order.id}`}
                        </td>
                        <td>{order.customerName}</td>
                        <td className="fw-semibold">
                          {formatCurrency(order.amount)}
                        </td>
                        <td>
                          <span className="dashboard-pill">
                            {statusLabelMap[order.status] || order.status}
                          </span>
                        </td>
                        <td>{order.date}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <div className="col-12 col-xl-4">
          <div className="dashboard-panel h-100">
            <div className="dashboard-panel-header">
              <h6>En Çok Satan Ürünler</h6>
              <span>{stats.topProducts.length} ürün</span>
            </div>
            <div className="dashboard-top-products">
              {stats.topProducts.length === 0 && (
                <p className="text-muted small mb-0">Veri bulunamadı.</p>
              )}
              {stats.topProducts.map((product, index) => {
                const maxSales = Math.max(
                  1,
                  ...(stats.topProducts || []).map((x) => Number(x.sales || 0)),
                );
                const width = Math.round(
                  (Number(product.sales || 0) / maxSales) * 100,
                );

                return (
                  <div
                    key={`${product.productId}-${index}`}
                    className="dashboard-top-item"
                  >
                    <div className="dashboard-top-row">
                      <span className="dashboard-rank">#{index + 1}</span>
                      <strong className="text-truncate">{product.name}</strong>
                      <small>
                        {Number(product.sales || 0).toLocaleString("tr-TR")}{" "}
                        satış
                      </small>
                    </div>
                    <div className="dashboard-progress-track">
                      <div
                        className="dashboard-progress-fill theme-orange"
                        style={{ width: `${width}%` }}
                      />
                    </div>
                    <div className="dashboard-top-revenue">
                      {formatCurrency(product.revenue)}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      </div>

      <div className="dashboard-panel mt-3">
        <div className="dashboard-panel-header">
          <h6>ERP / Mikro Bağlantısı</h6>
          <button
            className="btn btn-outline-secondary btn-sm"
            onClick={checkErpConnection}
          >
            <i
              className={`fas ${erpStatus.loading ? "fa-spinner fa-spin" : "fa-plug"} me-1`}
            ></i>
            Kontrol Et
          </button>
        </div>
        <div className="d-flex flex-wrap align-items-center gap-2">
          <span
            className={`dashboard-erp-dot ${erpStatus.isConnected ? "ok" : erpStatus.isConnected === false ? "fail" : "unknown"}`}
          ></span>
          <strong>
            {erpStatus.loading
              ? "Bağlantı kontrol ediliyor..."
              : erpStatus.isConnected
                ? "ERP bağlantısı aktif"
                : "ERP bağlantısı pasif"}
          </strong>
          {erpStatus.message && (
            <span className="text-muted">- {erpStatus.message}</span>
          )}
          {erpStatus.lastSync && (
            <small className="text-muted">
              Son kontrol:{" "}
              {new Date(erpStatus.lastSync).toLocaleTimeString("tr-TR")}
            </small>
          )}
        </div>
      </div>
    </div>
  );
}
