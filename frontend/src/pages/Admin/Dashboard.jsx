import { useEffect, useState } from "react";
import { AdminService } from "../../services/adminService";
import { useAuth } from "../../contexts/AuthContext";
import { PERMISSIONS } from "../../services/permissionService";

// ============================================================================
// Dashboard - İzin Bazlı Widget Görünürlüğü
// ============================================================================
// Her widget, ilgili izne göre gösterilir/gizlenir:
// - dashboard.view: Temel dashboard erişimi (route seviyesinde kontrol edilir)
// - dashboard.statistics: İstatistik kartları (Kullanıcılar, Ürünler, Siparişler)
// - dashboard.revenue: Gelir bilgileri ve finansal veriler
// ============================================================================

export default function Dashboard() {
  const { hasPermission, user } = useAuth();

  const [stats, setStats] = useState({
    totalUsers: 0,
    totalOrders: 0,
    totalRevenue: 0,
    totalProducts: 0,
    recentOrders: [],
    topProducts: [],
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // İzin kontrolleri - SuperAdmin her şeyi görebilir
  const isSuperAdmin = user?.role === "SuperAdmin";
  const canViewStatistics =
    isSuperAdmin || hasPermission?.(PERMISSIONS.DASHBOARD_STATISTICS);
  const canViewRevenue =
    isSuperAdmin || hasPermission?.(PERMISSIONS.DASHBOARD_REVENUE);

  const loadDashboardStats = async () => {
    try {
      setLoading(true);
      setError(null);
      const dashboardData = await AdminService.getDashboardStats();
      setStats(dashboardData);
    } catch (err) {
      console.error("Dashboard stats yüklenirken hata:", err);
      setError("Veriler yüklenirken bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadDashboardStats();
  }, []);

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ height: "60vh" }}
      >
        <div className="text-center">
          <div
            className="spinner-border mb-3"
            style={{ color: "#f57c00" }}
            role="status"
          ></div>
          <p className="text-muted">Dashboard yükleniyor...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container mt-5">
        <div className="alert alert-danger border-0 rounded-4" role="alert">
          <i className="fas fa-exclamation-triangle me-2"></i>
          {error}
          <button
            className="btn btn-outline-danger ms-3"
            onClick={loadDashboardStats}
          >
            <i className="fas fa-redo me-1"></i>Tekrar Dene
          </button>
        </div>
      </div>
    );
  }

  return (
    <div style={{ overflow: "hidden", maxWidth: "100%" }}>
      <div className="container-fluid px-2 px-md-3">
        {/* Modern Stats Cards - İzin kontrolü ile */}
        <div className="row g-2 g-md-3 mb-3 mb-md-4">
          {/* Kullanıcılar - dashboard.statistics izni gerekli */}
          {canViewStatistics && (
            <div className="col-6 col-xl-3">
              <div
                className="card border-0 h-100"
                style={{
                  borderRadius: "10px",
                  background:
                    "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                  color: "white",
                  boxShadow: "0 4px 15px rgba(102, 126, 234, 0.25)",
                }}
              >
                <div className="card-body p-2 p-md-3">
                  <p
                    className="mb-0 opacity-75 text-uppercase"
                    style={{ fontSize: "0.65rem" }}
                  >
                    Kullanıcılar
                  </p>
                  <h5 className="mb-0 fw-bold">{stats.totalUsers}</h5>
                </div>
              </div>
            </div>
          )}

          {/* Ürünler - dashboard.statistics izni gerekli */}
          {canViewStatistics && (
            <div className="col-6 col-xl-3">
              <div
                className="card border-0 h-100"
                style={{
                  borderRadius: "10px",
                  background:
                    "linear-gradient(135deg, #11998e 0%, #38ef7d 100%)",
                  color: "white",
                  boxShadow: "0 4px 15px rgba(17, 153, 142, 0.25)",
                }}
              >
                <div className="card-body p-2 p-md-3">
                  <p
                    className="mb-0 opacity-75 text-uppercase"
                    style={{ fontSize: "0.65rem" }}
                  >
                    Ürünler
                  </p>
                  <h5 className="mb-0 fw-bold">{stats.totalProducts}</h5>
                </div>
              </div>
            </div>
          )}

          {/* Siparişler - dashboard.statistics izni gerekli */}
          {canViewStatistics && (
            <div className="col-6 col-xl-3">
              <div
                className="card border-0 h-100"
                style={{
                  borderRadius: "10px",
                  background:
                    "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
                  color: "white",
                  boxShadow: "0 4px 15px rgba(240, 147, 251, 0.25)",
                }}
              >
                <div className="card-body p-2 p-md-3">
                  <p
                    className="mb-0 opacity-75 text-uppercase"
                    style={{ fontSize: "0.65rem" }}
                  >
                    Siparişler
                  </p>
                  <h5 className="mb-0 fw-bold">{stats.totalOrders}</h5>
                </div>
              </div>
            </div>
          )}

          {/* Toplam Gelir - dashboard.revenue izni gerekli */}
          {canViewRevenue && (
            <div className="col-6 col-xl-3">
              <div
                className="card border-0 h-100"
                style={{
                  borderRadius: "10px",
                  background:
                    "linear-gradient(135deg, #f97316 0%, #fb923c 100%)",
                  color: "white",
                  boxShadow: "0 4px 15px rgba(249, 115, 22, 0.25)",
                }}
              >
                <div className="card-body p-2 p-md-3">
                  <p
                    className="mb-0 opacity-75 text-uppercase"
                    style={{ fontSize: "0.65rem" }}
                  >
                    Toplam Gelir
                  </p>
                  <h5 className="mb-0 fw-bold" style={{ fontSize: "1rem" }}>
                    ₺
                    {stats.totalRevenue?.toLocaleString("tr-TR", {
                      minimumFractionDigits: 0,
                    })}
                  </h5>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* İzin yoksa bilgi mesajı göster */}
        {!canViewStatistics && !canViewRevenue && (
          <div className="alert alert-info border-0 rounded-3 mb-4">
            <i className="fas fa-info-circle me-2"></i>
            Dashboard istatistiklerini görüntülemek için gerekli izinlere sahip
            değilsiniz.
          </div>
        )}

        {/* Son Siparişler ve En Çok Satan Ürünler */}
        <div className="row g-2 g-md-3">
          {/* Son Siparişler */}
          <div className="col-12 col-lg-8">
            <div
              className="card border-0 shadow-sm"
              style={{ borderRadius: "10px" }}
            >
              <div className="card-header bg-transparent border-0 p-2 p-md-3 pb-0">
                <h6
                  className="card-title mb-0 fw-bold"
                  style={{ color: "#1e293b", fontSize: "0.9rem" }}
                >
                  <i
                    className="fas fa-clock me-2"
                    style={{ color: "#f97316" }}
                  ></i>
                  Son Siparişler
                </h6>
              </div>
              <div className="card-body p-2 p-md-3">
                {stats.recentOrders?.length > 0 ? (
                  <div
                    className="table-responsive"
                    style={{ margin: "0 -0.25rem" }}
                  >
                    <table
                      className="table table-sm mb-0"
                      style={{ fontSize: "0.75rem" }}
                    >
                      <thead>
                        <tr style={{ borderBottom: "2px solid #f1f5f9" }}>
                          <th className="fw-semibold text-muted border-0 px-1">
                            Müşteri
                          </th>
                          <th className="fw-semibold text-muted border-0 px-1">
                            Tutar
                          </th>
                          <th className="fw-semibold text-muted border-0 px-1">
                            Durum
                          </th>
                          <th className="fw-semibold text-muted border-0 px-1 d-none d-sm-table-cell">
                            Tarih
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {stats.recentOrders.map((order) => (
                          <tr key={order.id}>
                            <td
                              className="fw-medium border-0 px-1 text-truncate"
                              style={{ maxWidth: "80px" }}
                            >
                              {order.customerName}
                            </td>
                            <td className="border-0 px-1">
                              <span
                                className="fw-bold"
                                style={{ color: "#f97316" }}
                              >
                                ₺
                                {order.amount?.toLocaleString("tr-TR", {
                                  minimumFractionDigits: 0,
                                })}
                              </span>
                            </td>
                            <td className="border-0 px-1">
                              <span
                                className={`badge rounded-pill ${
                                  order.status === "Completed"
                                    ? "bg-success"
                                    : order.status === "Processing"
                                    ? "bg-warning"
                                    : "bg-info"
                                }`}
                                style={{
                                  fontSize: "0.6rem",
                                  padding: "0.2em 0.5em",
                                }}
                              >
                                {order.status === "Completed"
                                  ? "Tamam"
                                  : order.status === "Processing"
                                  ? "İşlem"
                                  : "Kargo"}
                              </span>
                            </td>
                            <td className="text-muted border-0 px-1 d-none d-sm-table-cell">
                              {order.date}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="text-center py-3">
                    <i className="fas fa-inbox fa-2x text-muted mb-2"></i>
                    <p className="text-muted small mb-0">
                      Henüz sipariş bulunmuyor.
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* En Çok Satan Ürünler */}
          <div className="col-12 col-lg-4">
            <div
              className="card border-0 shadow-sm"
              style={{ borderRadius: "10px" }}
            >
              <div className="card-header bg-transparent border-0 p-2 p-md-3 pb-0">
                <h6
                  className="card-title mb-0 fw-bold"
                  style={{ color: "#1e293b", fontSize: "0.9rem" }}
                >
                  <i
                    className="fas fa-trophy me-2"
                    style={{ color: "#f97316" }}
                  ></i>
                  En Çok Satan
                </h6>
              </div>
              <div className="card-body p-2 p-md-3">
                {stats.topProducts?.length > 0 ? (
                  <div>
                    {stats.topProducts.map((product, index) => (
                      <div
                        key={index}
                        className="d-flex align-items-center py-1"
                        style={{
                          borderBottom:
                            index !== stats.topProducts.length - 1
                              ? "1px solid #f1f5f9"
                              : "none",
                        }}
                      >
                        <div className="flex-grow-1 overflow-hidden">
                          <p
                            className="mb-0 fw-medium text-truncate"
                            style={{ fontSize: "0.8rem" }}
                          >
                            {product.name}
                          </p>
                          <small
                            className="text-muted"
                            style={{ fontSize: "0.7rem" }}
                          >
                            {product.sales} satış
                          </small>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-3">
                    <i className="fas fa-chart-pie fa-2x text-muted mb-2"></i>
                    <p className="text-muted small mb-0">Veri bulunmuyor.</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
