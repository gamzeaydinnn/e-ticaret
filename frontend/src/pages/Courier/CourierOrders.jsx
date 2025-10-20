import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { CourierService } from "../../services/courierService";

export default function CourierOrders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [updating, setUpdating] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    const courierData = localStorage.getItem("courierData");
    if (!courierData) {
      navigate("/courier/login");
      return;
    }

    const courier = JSON.parse(courierData);
    loadOrders(courier.id);
  }, [navigate]);

  const loadOrders = async (courierId) => {
    try {
      const orderData = await CourierService.getAssignedOrders(courierId);
      setOrders(orderData);
    } catch (error) {
      console.error("Sipariş yükleme hatası:", error);
    } finally {
      setLoading(false);
    }
  };

  const updateOrderStatus = async (orderId, newStatus, notes = "") => {
    setUpdating(true);
    try {
      await CourierService.updateOrderStatus(orderId, newStatus, notes);

      // Siparişleri yeniden yükle
      const courierData = JSON.parse(localStorage.getItem("courierData"));
      await loadOrders(courierData.id);

      setSelectedOrder(null);
    } catch (error) {
      console.error("Durum güncelleme hatası:", error);
    } finally {
      setUpdating(false);
    }
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

  const getNextStatus = (currentStatus) => {
    const statusFlow = {
      preparing: null, // Hazırlanıyor durumunda kurye bekler
      ready: "picked_up", // Hazır -> Teslim Al
      picked_up: "in_transit", // Teslim Alındı -> Yola Çık
      in_transit: "delivered", // Yolda -> Teslim Et
      delivered: null, // Son durum
    };
    return statusFlow[currentStatus];
  };

  const getNextStatusText = (currentStatus) => {
    const nextStatus = getNextStatus(currentStatus);
    const actionMap = {
      picked_up: "Teslim Al",
      in_transit: "Yola Çık",
      delivered: "Teslim Et",
    };
    return actionMap[nextStatus];
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center min-vh-100">
        <div className="spinner-border text-primary"></div>
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
          <button
            onClick={() => navigate("/courier/dashboard")}
            className="btn btn-link text-white text-decoration-none"
          >
            <i className="fas fa-arrow-left me-2"></i>
            Dashboard
          </button>
          <span className="navbar-brand mb-0">Siparişlerim</span>
        </div>
      </nav>

      <div className="container-fluid p-4">
        <div className="row">
          <div className="col-12">
            <div className="card border-0 shadow-sm">
              <div className="card-header bg-white border-0 py-3">
                <h5 className="fw-bold mb-0">
                  <i className="fas fa-list-alt me-2 text-primary"></i>
                  Tüm Siparişler ({orders.length})
                </h5>
              </div>
              <div className="card-body p-0">
                {orders.length === 0 ? (
                  <div className="text-center py-5">
                    <i className="fas fa-inbox fs-1 text-muted mb-3"></i>
                    <p className="text-muted">
                      Henüz atanmış siparişiniz bulunmuyor.
                    </p>
                  </div>
                ) : (
                  <div className="table-responsive">
                    <table className="table table-hover mb-0">
                      <thead className="bg-light">
                        <tr>
                          <th>Sipariş</th>
                          <th>Müşteri</th>
                          <th>Adres</th>
                          <th>Tutar</th>
                          <th>Teslimat</th>
                          <th>Durum</th>
                          <th>İşlemler</th>
                        </tr>
                      </thead>
                      <tbody>
                        {orders.map((order) => (
                          <tr key={order.id}>
                            <td>
                              <div>
                                <span className="fw-bold">#{order.id}</span>
                                <br />
                                <small className="text-muted">
                                  {new Date(order.orderTime).toLocaleString(
                                    "tr-TR"
                                  )}
                                </small>
                              </div>
                            </td>
                            <td>
                              <div>
                                <span className="fw-semibold">
                                  {order.customerName}
                                </span>
                                <br />
                                <small className="text-muted">
                                  {order.customerPhone}
                                </small>
                              </div>
                            </td>
                            <td>
                              <span
                                className="text-muted"
                                title={order.address}
                              >
                                {order.address.length > 40
                                  ? order.address.substring(0, 40) + "..."
                                  : order.address}
                              </span>
                            </td>
                            <td>
                              <span className="fw-bold text-success">
                                {order.totalAmount.toFixed(2)} ₺
                              </span>
                            </td>
                            <td>
                              <span className="badge bg-light text-dark border px-2 py-1">
                                {order.shippingMethod === "car"
                                  ? "Araç"
                                  : order.shippingMethod === "motorcycle"
                                  ? "Motosiklet"
                                  : order.shippingMethod || "-"}
                              </span>
                            </td>
                            <td>
                              <span
                                className={`badge bg-${getStatusColor(
                                  order.status
                                )}`}
                              >
                                {getStatusText(order.status)}
                              </span>
                            </td>
                            <td>
                              <div className="d-flex gap-2">
                                <button
                                  onClick={() => setSelectedOrder(order)}
                                  className="btn btn-outline-primary btn-sm"
                                  title="Detayları Gör"
                                >
                                  <i className="fas fa-eye"></i>
                                </button>
                                {getNextStatus(order.status) && (
                                  <button
                                    onClick={() =>
                                      updateOrderStatus(
                                        order.id,
                                        getNextStatus(order.status)
                                      )
                                    }
                                    disabled={updating}
                                    className="btn btn-success btn-sm"
                                    title={getNextStatusText(order.status)}
                                  >
                                    {updating ? (
                                      <span className="spinner-border spinner-border-sm"></span>
                                    ) : (
                                      <i className="fas fa-arrow-right"></i>
                                    )}
                                  </button>
                                )}
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Sipariş Detay Modal */}
      {selectedOrder && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-lg">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">
                  <i className="fas fa-receipt me-2"></i>
                  Sipariş #{selectedOrder.id} Detayı
                </h5>
                <button
                  onClick={() => setSelectedOrder(null)}
                  className="btn-close"
                ></button>
              </div>
              <div className="modal-body">
                <div className="row">
                  <div className="col-md-6">
                    <h6 className="fw-bold">Müşteri Bilgileri</h6>
                    <p>
                      <strong>Ad:</strong> {selectedOrder.customerName}
                    </p>
                    <p>
                      <strong>Telefon:</strong> {selectedOrder.customerPhone}
                    </p>
                    <p>
                      <strong>Adres:</strong> {selectedOrder.address}
                    </p>
                  </div>
                  <div className="col-md-6">
                    <h6 className="fw-bold">Sipariş Bilgileri</h6>
                    <p>
                      <strong>Sipariş Zamanı:</strong>{" "}
                      {new Date(selectedOrder.orderTime).toLocaleString(
                        "tr-TR"
                      )}
                    </p>
                    <p>
                      <strong>Tutar:</strong>{" "}
                      {selectedOrder.totalAmount.toFixed(2)} ₺
                    </p>
                    <p>
                      <strong>Teslimat Türü:</strong>{" "}
                      <span className="badge bg-light text-dark border ms-2">
                        {selectedOrder.shippingMethod === "car"
                          ? "Araç"
                          : selectedOrder.shippingMethod === "motorcycle"
                          ? "Motosiklet"
                          : selectedOrder.shippingMethod || "-"}
                      </span>
                    </p>
                    <p>
                      <strong>Durum:</strong>
                      <span
                        className={`badge bg-${getStatusColor(
                          selectedOrder.status
                        )} ms-2`}
                      >
                        {getStatusText(selectedOrder.status)}
                      </span>
                    </p>
                  </div>
                </div>

                <h6 className="fw-bold mt-3">Ürünler</h6>
                <ul className="list-group">
                  {selectedOrder.items.map((item, index) => (
                    <li
                      key={index}
                      className="list-group-item d-flex justify-content-between"
                    >
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>

                {getNextStatus(selectedOrder.status) && (
                  <div className="mt-4 text-center">
                    <button
                      onClick={() =>
                        updateOrderStatus(
                          selectedOrder.id,
                          getNextStatus(selectedOrder.status)
                        )
                      }
                      disabled={updating}
                      className="btn btn-success"
                    >
                      {updating ? (
                        <>
                          <span className="spinner-border spinner-border-sm me-2"></span>
                          Güncelleniyor...
                        </>
                      ) : (
                        <>
                          <i className="fas fa-arrow-right me-2"></i>
                          {getNextStatusText(selectedOrder.status)}
                        </>
                      )}
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
