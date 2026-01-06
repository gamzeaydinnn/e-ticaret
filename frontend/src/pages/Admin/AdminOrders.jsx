import { useEffect, useState } from "react";
import { AdminService } from "../../services/adminService";
import { CourierService } from "../../services/courierService";

export default function AdminOrders() {
  const [orders, setOrders] = useState([]);
  const [couriers, setCouriers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [assigningCourier, setAssigningCourier] = useState(false);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const couriersData = await CourierService.getAll();
      // Gerçek siparişleri backend'den çek
      const ordersData = await AdminService.getOrders();
      setOrders(Array.isArray(ordersData) ? ordersData : []);
      setCouriers(couriersData);
    } catch (error) {
      console.error("Veri yükleme hatası:", error);
    } finally {
      setLoading(false);
    }
  };

  const updateOrderStatus = async (orderId, newStatus) => {
    try {
      // Backend'e durumu güncelle ve listiyi yeniden çek
      await AdminService.updateOrderStatus(orderId, newStatus);
      const updated = await AdminService.getOrders();
      setOrders(Array.isArray(updated) ? updated : []);
    } catch (error) {
      console.error("Durum güncelleme hatası:", error);
    }
  };

  const assignCourier = async (orderId, courierId) => {
    setAssigningCourier(true);
    try {
      const orderIndex = orders.findIndex((o) => o.id === orderId);
      const courierName = couriers.find((c) => c.id === courierId)?.name;

      if (orderIndex !== -1) {
        const updatedOrders = [...orders];
        updatedOrders[orderIndex] = {
          ...updatedOrders[orderIndex],
          courierId,
          courierName,
          status: "assigned",
        };
        setOrders(updatedOrders);
      }
    } catch (error) {
      console.error("Kurye atama hatası:", error);
    } finally {
      setAssigningCourier(false);
    }
  };

  const getStatusColor = (status) => {
    const colorMap = {
      pending: "warning",
      preparing: "info",
      ready: "primary",
      assigned: "success",
      picked_up: "success",
      in_transit: "success",
      delivered: "secondary",
      cancelled: "danger",
    };
    return colorMap[status] || "secondary";
  };

  const getStatusText = (status) => {
    const statusMap = {
      pending: "Beklemede",
      preparing: "Hazırlanıyor",
      ready: "Hazır",
      assigned: "Kuryeye Atandı",
      picked_up: "Teslim Alındı",
      in_transit: "Yolda",
      delivered: "Teslim Edildi",
      cancelled: "İptal Edildi",
    };
    return statusMap[status] || status;
  };

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ minHeight: "60vh" }}
      >
        <div className="spinner-border text-primary"></div>
      </div>
    );
  }

  return (
    <div style={{ overflow: "hidden", maxWidth: "100%" }}>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 gap-2 px-1">
        <div>
          <h5 className="fw-bold text-dark mb-0" style={{ fontSize: "1rem" }}>
            <i
              className="fas fa-shopping-bag me-2"
              style={{ color: "#f97316" }}
            ></i>
            Sipariş Yönetimi
          </h5>
          <p
            className="text-muted mb-0 d-none d-sm-block"
            style={{ fontSize: "0.75rem" }}
          >
            Siparişleri takip edin
          </p>
        </div>
        <button
          onClick={loadData}
          className="btn btn-outline-primary btn-sm px-2 py-1"
          style={{ fontSize: "0.75rem" }}
        >
          <i className="fas fa-sync-alt me-1"></i>Yenile
        </button>
      </div>

      {/* Özet Kartlar - daha kompakt */}
      <div className="row g-2 mb-3 px-1">
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-warning text-white"
            style={{ borderRadius: "6px" }}
          >
            <div className="card-body text-center px-1 py-2">
              <h6 className="fw-bold mb-0">
                {orders.filter((o) => o.status === "pending").length}
              </h6>
              <small style={{ fontSize: "0.6rem" }}>Bekleyen</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-info text-white"
            style={{ borderRadius: "6px" }}
          >
            <div className="card-body text-center px-1 py-2">
              <h6 className="fw-bold mb-0">
                {orders.filter((o) => o.status === "preparing").length}
              </h6>
              <small style={{ fontSize: "0.6rem" }}>Hazırlanan</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-success text-white"
            style={{ borderRadius: "6px" }}
          >
            <div className="card-body text-center px-1 py-2">
              <h6 className="fw-bold mb-0">
                {
                  orders.filter((o) =>
                    ["assigned", "picked_up", "in_transit"].includes(o.status)
                  ).length
                }
              </h6>
              <small style={{ fontSize: "0.6rem" }}>Kuryede</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-secondary text-white"
            style={{ borderRadius: "6px" }}
          >
            <div className="card-body text-center px-1 py-2">
              <h6 className="fw-bold mb-0">
                {orders.filter((o) => o.status === "delivered").length}
              </h6>
              <small style={{ fontSize: "0.6rem" }}>Teslim</small>
            </div>
          </div>
        </div>
      </div>

      {/* Sipariş Listesi */}
      <div
        className="card border-0 shadow-sm mx-1"
        style={{ borderRadius: "10px" }}
      >
        <div className="card-header bg-white border-0 py-2 px-2 px-md-3">
          <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
            <i className="fas fa-list-alt me-2 text-primary"></i>
            Siparişler ({orders.length})
          </h6>
        </div>
        <div className="card-body p-0">
          <div className="table-responsive" style={{ margin: "0" }}>
            <table
              className="table table-sm mb-0"
              style={{ fontSize: "0.7rem" }}
            >
              <thead className="bg-light">
                <tr>
                  <th className="px-1 py-2">Sipariş</th>
                  <th className="px-1 py-2 d-none d-md-table-cell">Müşteri</th>
                  <th className="px-1 py-2">Tutar</th>
                  <th className="px-1 py-2">Durum</th>
                  <th className="px-1 py-2 d-none d-sm-table-cell">Kurye</th>
                  <th className="px-1 py-2">İşlem</th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => (
                  <tr key={order.id}>
                    <td className="px-1 py-2">
                      <span className="fw-bold">#{order.id}</span>
                      <br />
                      <small
                        className="text-muted d-none d-sm-inline"
                        style={{ fontSize: "0.6rem" }}
                      >
                        {new Date(order.orderDate).toLocaleDateString("tr-TR")}
                      </small>
                    </td>
                    <td className="px-1 py-2 d-none d-md-table-cell">
                      <span
                        className="fw-semibold text-truncate d-block"
                        style={{ maxWidth: "80px" }}
                      >
                        {order.customerName}
                      </span>
                    </td>
                    <td className="px-1 py-2">
                      <span
                        className="fw-bold text-success"
                        style={{ fontSize: "0.7rem" }}
                      >
                        {(order.totalAmount ?? 0).toFixed(0)}₺
                      </span>
                    </td>
                    <td className="px-1 py-2">
                      <span
                        className={`badge bg-${getStatusColor(order.status)}`}
                        style={{ fontSize: "0.55rem", padding: "0.2em 0.4em" }}
                      >
                        {getStatusText(order.status).substring(0, 6)}
                      </span>
                    </td>
                    <td className="px-1 py-2 d-none d-sm-table-cell">
                      {order.courierName ? (
                        <span
                          className="text-success"
                          style={{ fontSize: "0.65rem" }}
                        >
                          <i className="fas fa-motorcycle me-1"></i>
                          {order.courierName.split(" ")[0]}
                        </span>
                      ) : (
                        <span
                          className="text-muted"
                          style={{ fontSize: "0.6rem" }}
                        >
                          -
                        </span>
                      )}
                    </td>
                    <td className="px-1 py-2">
                      <div className="d-flex gap-1">
                        <button
                          onClick={() => setSelectedOrder(order)}
                          className="btn btn-outline-primary p-1"
                          style={{ fontSize: "0.6rem", lineHeight: 1 }}
                          title="Detay"
                        >
                          <i className="fas fa-eye"></i>
                        </button>
                        {order.status === "pending" && (
                          <button
                            onClick={() =>
                              updateOrderStatus(order.id, "preparing")
                            }
                            className="btn btn-warning p-1"
                            style={{ fontSize: "0.6rem", lineHeight: 1 }}
                          >
                            <i className="fas fa-clock"></i>
                          </button>
                        )}
                        {order.status === "preparing" && (
                          <button
                            onClick={() => updateOrderStatus(order.id, "ready")}
                            className="btn btn-info p-1"
                            style={{ fontSize: "0.6rem", lineHeight: 1 }}
                          >
                            <i className="fas fa-check"></i>
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
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
          <div className="modal-dialog modal-dialog-centered mx-2">
            <div className="modal-content" style={{ borderRadius: "12px" }}>
              <div className="modal-header py-2 px-3">
                <h6 className="modal-title" style={{ fontSize: "0.9rem" }}>
                  <i className="fas fa-receipt me-2"></i>
                  Sipariş #{selectedOrder.id}
                </h6>
                <button
                  onClick={() => setSelectedOrder(null)}
                  className="btn-close btn-close-sm"
                ></button>
              </div>
              <div
                className="modal-body p-2 p-md-3"
                style={{
                  fontSize: "0.75rem",
                  maxHeight: "70vh",
                  overflowY: "auto",
                }}
              >
                <div className="row g-2">
                  <div className="col-12 col-md-6">
                    <h6 className="fw-bold mb-1" style={{ fontSize: "0.8rem" }}>
                      Müşteri
                    </h6>
                    <p className="mb-1">
                      <strong>Ad:</strong> {selectedOrder.customerName}
                    </p>
                    <p className="mb-1">
                      <strong>Tel:</strong> {selectedOrder.customerPhone}
                    </p>
                    <p className="mb-1 text-truncate">
                      <strong>Adres:</strong> {selectedOrder.address || "-"}
                    </p>
                  </div>
                  <div className="col-12 col-md-6">
                    <h6 className="fw-bold mb-1" style={{ fontSize: "0.8rem" }}>
                      Sipariş
                    </h6>
                    <p className="mb-1">
                      <strong>Tarih:</strong>{" "}
                      {selectedOrder.orderDate
                        ? new Date(selectedOrder.orderDate).toLocaleDateString(
                            "tr-TR"
                          )
                        : "-"}
                    </p>
                    <p className="mb-1">
                      <strong>Tutar:</strong>{" "}
                      {(selectedOrder.totalAmount ?? 0).toFixed(2)} ₺
                    </p>
                    <p className="mb-1">
                      <strong>Durum:</strong>
                      <span
                        className={`badge bg-${getStatusColor(
                          selectedOrder.status
                        )} ms-1`}
                        style={{ fontSize: "0.6rem" }}
                      >
                        {getStatusText(selectedOrder.status)}
                      </span>
                    </p>
                  </div>
                </div>

                <h6
                  className="fw-bold mt-2 mb-1"
                  style={{ fontSize: "0.8rem" }}
                >
                  Ürünler
                </h6>
                <div className="table-responsive">
                  <table
                    className="table table-sm mb-0"
                    style={{ fontSize: "0.7rem" }}
                  >
                    <thead>
                      <tr>
                        <th className="px-1">Ürün</th>
                        <th className="px-1">Adet</th>
                        <th className="px-1">Fiyat</th>
                      </tr>
                    </thead>
                    <tbody>
                      {(Array.isArray(selectedOrder.items)
                        ? selectedOrder.items
                        : []
                      ).map((item, index) => (
                        <tr key={index}>
                          <td
                            className="px-1 text-truncate"
                            style={{ maxWidth: "100px" }}
                          >
                            {item.name}
                          </td>
                          <td className="px-1">{item.quantity}</td>
                          <td className="px-1">
                            {((item.quantity ?? 0) * (item.price ?? 0)).toFixed(
                              0
                            )}
                            ₺
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Kurye Atama */}
                {selectedOrder.status === "ready" &&
                  !selectedOrder.courierId && (
                    <div className="mt-2">
                      <h6
                        className="fw-bold mb-1"
                        style={{ fontSize: "0.8rem" }}
                      >
                        Kurye Ata
                      </h6>
                      <div className="d-flex gap-1 flex-wrap">
                        {couriers
                          .filter((c) => c.status === "active")
                          .map((courier) => (
                            <button
                              key={courier.id}
                              onClick={() =>
                                assignCourier(selectedOrder.id, courier.id)
                              }
                              disabled={assigningCourier}
                              className="btn btn-outline-success btn-sm px-2 py-1"
                              style={{ fontSize: "0.65rem" }}
                            >
                              <i className="fas fa-motorcycle me-1"></i>
                              {courier.name.split(" ")[0]}
                            </button>
                          ))}
                      </div>
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
