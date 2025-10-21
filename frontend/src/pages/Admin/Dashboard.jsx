import { useState, useEffect } from "react";
import { AdminService } from "../../services/adminService";
import AdminLayout from "../../components/AdminLayout";
import BannerManagement from "./BannerManagement";

export default function Dashboard() {
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

  const loadDashboardStats = async () => {
    try {
      setLoading(true);
      setError(null); // Tekrar denemeden önce hatayı sıfırla
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // 1. Yüklenme durumu varsa, sadece yüklenme ekranını göster ve dur.
  if (loading) {
    return (
      <AdminLayout>
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
      </AdminLayout>
    );
  }

  // 2. Hata durumu varsa, sadece hata ekranını göster ve dur.
  if (error) {
    return (
      <AdminLayout>
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
      </AdminLayout>
    );
  }

  // 3. Yüklenme ve hata yoksa, ana dashboard içeriğini göster.
  return (
    <AdminLayout>
      <div className="container-fluid py-4">
        {/* Sayfa Başlığı (Orijinal kodunuzda bu bölüm eksikti, yapıya uygun olarak eklendi) */}
        <div className="d-flex justify-content-between align-items-center mb-4">
          <div>
            <h2 className="fw-bold mb-1" style={{ color: "#2d3748" }}>
              Yönetim Paneli
            </h2>
            <p className="text-muted mb-0">
              Sistemin genel durumu ve son aktiviteler
            </p>
          </div>
          <div className="text-end">
            <small className="text-muted d-block">Son güncelleme</small>
            <small className="fw-semibold" style={{ color: "#f57c00" }}>
              {new Date().toLocaleString("tr-TR")}
            </small>
          </div>
        </div>

        {/* Modern Stats Cards */}
        <div className="row g-4 mb-5">
          {/* Kullanıcılar */}
          <div className="col-xl-3 col-md-6">
            <div
              className="card border-0 h-100 position-relative overflow-hidden"
              style={{
                borderRadius: "20px",
                background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                color: "white",
                boxShadow: "0 10px 30px rgba(102, 126, 234, 0.3)",
                transition: "all 0.3s ease",
              }}
            >
              <div className="card-body p-4">
                <div className="d-flex align-items-center justify-content-between">
                  <div>
                    <p
                      className="mb-1 opacity-75 fw-semibold text-uppercase"
                      style={{ fontSize: "0.75rem" }}
                    >
                      Kullanıcılar
                    </p>
                    <h3 className="mb-0 fw-bold">{stats.totalUsers}</h3>
                  </div>
                  <i className="fas fa-users fa-2x opacity-75"></i>
                </div>
              </div>
            </div>
          </div>

          {/* Ürünler */}
          <div className="col-xl-3 col-md-6">
            <div
              className="card border-0 h-100 position-relative overflow-hidden"
              style={{
                borderRadius: "20px",
                background: "linear-gradient(135deg, #11998e 0%, #38ef7d 100%)",
                color: "white",
                boxShadow: "0 10px 30px rgba(17, 153, 142, 0.3)",
                transition: "all 0.3s ease",
              }}
            >
              <div className="card-body p-4">
                <div className="d-flex align-items-center justify-content-between">
                  <div>
                    <p
                      className="mb-1 opacity-75 fw-semibold text-uppercase"
                      style={{ fontSize: "0.75rem" }}
                    >
                      Ürünler
                    </p>
                    <h3 className="mb-0 fw-bold">{stats.totalProducts}</h3>
                  </div>
                  <i className="fas fa-box fa-2x opacity-75"></i>
                </div>
              </div>
            </div>
          </div>

          {/* Siparişler */}
          <div className="col-xl-3 col-md-6">
            <div
              className="card border-0 h-100 position-relative overflow-hidden"
              style={{
                borderRadius: "20px",
                background: "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
                color: "white",
                boxShadow: "0 10px 30px rgba(240, 147, 251, 0.3)",
                transition: "all 0.3s ease",
              }}
            >
              <div className="card-body p-4">
                <div className="d-flex align-items-center justify-content-between">
                  <div>
                    <p
                      className="mb-1 opacity-75 fw-semibold text-uppercase"
                      style={{ fontSize: "0.75rem" }}
                    >
                      Siparişler
                    </p>
                    <h3 className="mb-0 fw-bold">{stats.totalOrders}</h3>
                  </div>
                  <i className="fas fa-shopping-cart fa-2x opacity-75"></i>
                </div>
              </div>
            </div>
          </div>

          {/* Toplam Gelir */}
          <div className="col-xl-3 col-md-6">
            <div
              className="card border-0 h-100 position-relative overflow-hidden"
              style={{
                borderRadius: "20px",
                background: "linear-gradient(135deg, #f57c00 0%, #ff9800 100%)",
                color: "white",
                boxShadow: "0 10px 30px rgba(245, 124, 0, 0.3)",
                transition: "all 0.3s ease",
              }}
            >
              <div className="card-body p-4">
                <div className="d-flex align-items-center justify-content-between">
                  <div>
                    <p
                      className="mb-1 opacity-75 fw-semibold text-uppercase"
                      style={{ fontSize: "0.75rem" }}
                    >
                      Toplam Gelir
                    </p>
                    <h3 className="mb-0 fw-bold">
                      ₺
                      {stats.totalRevenue?.toLocaleString("tr-TR", {
                        minimumFractionDigits: 2,
                      })}
                    </h3>
                  </div>
                  <i className="fas fa-lira-sign fa-2x opacity-75"></i>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Son Siparişler ve En Çok Satan Ürünler */}
        <div className="row g-4">
          {/* Son Siparişler */}
          <div className="col-lg-8">
            <div
              className="card border-0 shadow-sm"
              style={{ borderRadius: "20px" }}
            >
              <div className="card-header bg-transparent border-0 p-4 pb-0">
                <h5
                  className="card-title mb-0 fw-bold"
                  style={{ color: "#2d3748" }}
                >
                  <i
                    className="fas fa-clock me-2"
                    style={{ color: "#f57c00" }}
                  ></i>
                  Son Siparişler
                </h5>
              </div>
              <div className="card-body p-4">
                {stats.recentOrders?.length > 0 ? (
                  <div className="table-responsive">
                    <table className="table table-hover">
                      <thead>
                        <tr style={{ borderBottom: "2px solid #f8f9fa" }}>
                          <th className="fw-bold text-muted border-0">
                            Müşteri
                          </th>
                          <th className="fw-bold text-muted border-0">Tutar</th>
                          <th className="fw-bold text-muted border-0">Durum</th>
                          <th className="fw-bold text-muted border-0">Tarih</th>
                        </tr>
                      </thead>
                      <tbody>
                        {stats.recentOrders.map((order) => (
                          <tr
                            key={order.id}
                            style={{ borderBottom: "1px solid #f8f9fa" }}
                          >
                            <td className="fw-semibold border-0">
                              {order.customerName}
                            </td>
                            <td className="border-0">
                              <span
                                className="fw-bold"
                                style={{ color: "#f57c00" }}
                              >
                                ₺
                                {order.amount?.toLocaleString("tr-TR", {
                                  minimumFractionDigits: 2,
                                })}
                              </span>
                            </td>
                            <td className="border-0">
                              <span
                                className={`badge rounded-pill ${
                                  order.status === "Completed"
                                    ? "bg-success"
                                    : order.status === "Processing"
                                    ? "bg-warning"
                                    : "bg-info"
                                }`}
                              >
                                {order.status === "Completed"
                                  ? "Tamamlandı"
                                  : order.status === "Processing"
                                  ? "İşleniyor"
                                  : "Kargoda"}
                              </span>
                            </td>
                            <td className="text-muted border-0">
                              {order.date}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="text-center py-4">
                    <i className="fas fa-inbox fa-3x text-muted mb-3"></i>
                    <p className="text-muted">Henüz sipariş bulunmuyor.</p>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* En Çok Satan Ürünler */}
          <div className="col-lg-4">
            <div
              className="card border-0 shadow-sm"
              style={{ borderRadius: "20px" }}
            >
              <div className="card-header bg-transparent border-0 p-4 pb-0">
                <h5
                  className="card-title mb-0 fw-bold"
                  style={{ color: "#2d3748" }}
                >
                  <i
                    className="fas fa-trophy me-2"
                    style={{ color: "#f57c00" }}
                  ></i>
                  En Çok Satan Ürünler
                </h5>
              </div>
              <div className="card-body p-4">
                {stats.topProducts?.length > 0 ? (
                  <div>
                    {stats.topProducts.map((product, index) => (
                      <div
                        key={index}
                        className="d-flex align-items-center mb-3 pb-3"
                        style={{
                          borderBottom:
                            index !== stats.topProducts.length - 1
                              ? "1px solid #f8f9fa"
                              : "none",
                        }}
                      >
                        {/* Bu bölüm orijinal kodda eksikti, örnek bir yapı eklendi */}
                        <div className="flex-grow-1">
                          <h6 className="mb-0 fw-semibold">{product.name}</h6>
                          <small className="text-muted">
                            {product.sales} satış
                          </small>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-4">
                    <i className="fas fa-chart-pie fa-3x text-muted mb-3"></i>
                    <p className="text-muted">Veri bulunmuyor.</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Banner Yönetimini burada gösterebilirsin */}
        {/* <BannerManagement /> */}
      </div>
    </AdminLayout>
  );
}
