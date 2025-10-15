import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { CourierService } from "../../services/courierService";

export default function CourierDashboard() {
  const [courier, setCourier] = useState(null);
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [stats, setStats] = useState({
    activeOrders: 0,
    completedToday: 0,
    totalEarnings: 0,
  });
  const navigate = useNavigate();

  useEffect(() => {
    const courierData = localStorage.getItem("courierData");
    if (!courierData) {
      navigate("/courier/login");
      return;
    }

    const parsedCourier = JSON.parse(courierData);
    setCourier(parsedCourier);
    loadOrders(parsedCourier.id);
  }, [navigate]);

  const loadOrders = async (courierId) => {
    try {
      const orderData = await CourierService.getAssignedOrders(courierId);
      setOrders(orderData);

      // İstatistikleri hesapla
      const activeCount = orderData.filter((o) =>
        ["preparing", "ready", "picked_up", "in_transit"].includes(o.status)
      ).length;
      const completedCount = orderData.filter(
        (o) => o.status === "delivered"
      ).length;

      setStats({
        activeOrders: activeCount,
        completedToday: completedCount,
        totalEarnings: completedCount * 15, // Basit hesaplama
      });
    } catch (error) {
      console.error("Sipariş yükleme hatası:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem("courierToken");
    localStorage.removeItem("courierData");
    navigate("/courier/login");
  };

  const getStatusText = (status) => {
    const statusMap = {
      preparing: "Hazırlanıyor",
      ready: "Teslim Alınmaya Hazır",
      picked_up: "Teslim Alındı",
      in_transit: "Yolda",
      delivered: "Teslim Edildi",
    };
    return statusMap[status] || status;
  };

  const getStatusColor = (status) => {
    const colorMap = {
      preparing: "warning",
      ready: "info",
      picked_up: "primary",
      in_transit: "success",
      delivered: "secondary",
    };
    return colorMap[status] || "secondary";
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center min-vh-100">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="min-vh-100 bg-light">
      {/* Header */}
      <nav
        className="navbar navbar-expand-lg navbar-dark"
        style={{ background: "linear-gradient(135deg, #ff6b35, #ff8c00)" }}
      >
        <div className="container-fluid">
          <span className="navbar-brand">
            <i className="fas fa-motorcycle me-2"></i>
            Kurye Paneli
          </span>
          <div className="d-flex align-items-center text-white">
            <span className="me-3">
              <i className="fas fa-user-circle me-2"></i>
              {courier?.name}
            </span>
            <button
              onClick={handleLogout}
              className="btn btn-outline-light btn-sm"
            >
              <i className="fas fa-sign-out-alt me-1"></i>
              Çıkış
            </button>
          </div>
        </div>
      </nav>

      <div className="container-fluid p-4">
        <div className="row mb-4">
          <div className="col-12">
            <h2 className="fw-bold text-dark mb-0">
              Hoş geldin, {courier?.name}!
            </h2>
            <p className="text-muted">
              Bugünkü performansın ve aktif siparişlerin
            </p>
          </div>
        </div>

        {/* İstatistik Kartları */}
        <div className="row mb-4">
          <div className="col-md-4 mb-3">
            <div className="card border-0 shadow-sm h-100">
              <div className="card-body text-center">
                <div className="text-primary mb-2">
                  <i className="fas fa-clock fs-1"></i>
                </div>
                <h3 className="fw-bold text-primary">{stats.activeOrders}</h3>
                <p className="text-muted mb-0">Aktif Sipariş</p>
              </div>
            </div>
          </div>
          <div className="col-md-4 mb-3">
            <div className="card border-0 shadow-sm h-100">
              <div className="card-body text-center">
                <div className="text-success mb-2">
                  <i className="fas fa-check-circle fs-1"></i>
                </div>
                <h3 className="fw-bold text-success">{stats.completedToday}</h3>
                <p className="text-muted mb-0">Bugün Teslim Edilen</p>
              </div>
            </div>
          </div>
          <div className="col-md-4 mb-3">
            <div className="card border-0 shadow-sm h-100">
              <div className="card-body text-center">
                <div className="text-warning mb-2">
                  <i className="fas fa-lira-sign fs-1"></i>
                </div>
                <h3 className="fw-bold text-warning">
                  {stats.totalEarnings} ₺
                </h3>
                <p className="text-muted mb-0">Bugünkü Kazanç</p>
              </div>
            </div>
          </div>
        </div>

        {/* Aktif Siparişler */}
        <div className="row">
          <div className="col-12">
            <div className="card border-0 shadow-sm">
              <div className="card-header bg-white border-0 py-3">
                <div className="d-flex justify-content-between align-items-center">
                  <h5 className="fw-bold mb-0">
                    <i className="fas fa-list-alt me-2 text-primary"></i>
                    Aktif Siparişler
                  </h5>
                  <button
                    onClick={() => navigate("/courier/orders")}
                    className="btn btn-outline-primary btn-sm"
                  >
                    <i className="fas fa-eye me-1"></i>
                    Tümünü Gör
                  </button>
                </div>
              </div>
              <div className="card-body">
                {orders.length === 0 ? (
                  <div className="text-center py-5">
                    <i className="fas fa-inbox fs-1 text-muted mb-3"></i>
                    <p className="text-muted">
                      Henüz atanmış siparişiniz bulunmuyor.
                    </p>
                  </div>
                ) : (
                  <div className="row">
                    {orders.slice(0, 4).map((order) => (
                      <div key={order.id} className="col-md-6 mb-3">
                        <div className="card border-0 bg-light">
                          <div className="card-body">
                            <div className="d-flex justify-content-between align-items-start mb-2">
                              <h6 className="fw-bold mb-0">
                                Sipariş #{order.id}
                              </h6>
                              <span
                                className={`badge bg-${getStatusColor(
                                  order.status
                                )}`}
                              >
                                {getStatusText(order.status)}
                              </span>
                            </div>
                            <p className="text-muted mb-2">
                              <i className="fas fa-user me-1"></i>
                              {order.customerName}
                            </p>
                            <p className="text-muted mb-2">
                              <i className="fas fa-map-marker-alt me-1"></i>
                              {order.address.length > 50
                                ? order.address.substring(0, 50) + "..."
                                : order.address}
                            </p>
                            <div className="d-flex justify-content-between align-items-center">
                              <span className="fw-bold text-primary">
                                {order.totalAmount.toFixed(2)} ₺
                              </span>
                              <small className="text-muted">
                                {new Date(order.orderTime).toLocaleTimeString(
                                  "tr-TR",
                                  { hour: "2-digit", minute: "2-digit" }
                                )}
                              </small>
                            </div>
                          </div>
                        </div>
                      </div>
                    ))}
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
