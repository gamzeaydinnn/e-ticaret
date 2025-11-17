import { useEffect, useState } from "react";
import AdminLayout from "../../components/AdminLayout";
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
      <AdminLayout>
        <div
          className="d-flex justify-content-center align-items-center"
          style={{ minHeight: "60vh" }}
        >
          <div className="spinner-border text-primary"></div>
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h2 className="fw-bold text-dark mb-1">Sipariş Yönetimi</h2>
          <p className="text-muted mb-0">
            Siparişleri takip edin ve kurye ataması yapın
          </p>
        </div>
        <button onClick={loadData} className="btn btn-outline-primary">
          <i className="fas fa-sync-alt me-2"></i>
          Yenile
        </button>
      </div>

      {/* Özet Kartlar */}
      <div className="row mb-4">
        <div className="col-md-3 mb-3">
          <div className="card border-0 shadow-sm bg-warning text-white">
            <div className="card-body text-center">
              <h4 className="fw-bold">
                {orders.filter((o) => o.status === "pending").length}
              </h4>
              <small>Bekleyen Sipariş</small>
            </div>
          </div>
        </div>
        <div className="col-md-3 mb-3">
          <div className="card border-0 shadow-sm bg-info text-white">
            <div className="card-body text-center">
              <h4 className="fw-bold">
                {orders.filter((o) => o.status === "preparing").length}
              </h4>
              <small>Hazırlanan</small>
            </div>
          </div>
        </div>
        <div className="col-md-3 mb-3">
          <div className="card border-0 shadow-sm bg-success text-white">
            <div className="card-body text-center">
              <h4 className="fw-bold">
                {
                  orders.filter((o) =>
                    ["assigned", "picked_up", "in_transit"].includes(o.status)
                  ).length
                }
              </h4>
              <small>Kuryede</small>
            </div>
          </div>
        </div>
        <div className="col-md-3 mb-3">
          <div className="card border-0 shadow-sm bg-secondary text-white">
            <div className="card-body text-center">
              <h4 className="fw-bold">
                {orders.filter((o) => o.status === "delivered").length}
              </h4>
              <small>Teslim Edildi</small>
            </div>
          </div>
        </div>
      </div>

      {/* Sipariş Listesi */}
      <div className="card border-0 shadow-sm">
        <div className="card-header bg-white border-0 py-3">
          <h5 className="fw-bold mb-0">
            <i className="fas fa-list-alt me-2 text-primary"></i>
            Sipariş Listesi ({orders.length})
          </h5>
        </div>
        <div className="card-body p-0">
          <div className="table-responsive">
            <table className="table table-hover mb-0">
              <thead className="bg-light">
                <tr>
                  <th>Sipariş</th>
                  <th>Müşteri</th>
                  <th>Adres</th>
                  <th>Tutar</th>
                  <th>Durum</th>
                  <th>Kurye</th>
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
                          {new Date(order.orderDate).toLocaleString("tr-TR")}
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
                      <span className="text-muted" title={order.address}>
                        {order.address.length > 30
                          ? order.address.substring(0, 30) + "..."
                          : order.address}
                      </span>
                    </td>
                    <td>
                      <span className="fw-bold text-success">
                        {order.totalAmount.toFixed(2)} ₺
                      </span>
                    </td>
                    <td>
                      <span
                        className={`badge bg-${getStatusColor(order.status)}`}
                      >
                        {getStatusText(order.status)}
                      </span>
                    </td>
                    <td>
                      {order.courierName ? (
                        <span className="text-success">
                          <i className="fas fa-motorcycle me-1"></i>
                          {order.courierName}
                        </span>
                      ) : (
                        <span className="text-muted">Atanmamış</span>
                      )}
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
                        {order.status === "pending" && (
                          <button
                            onClick={() =>
                              updateOrderStatus(order.id, "preparing")
                            }
                            className="btn btn-warning btn-sm"
                            title="Hazırlanıyor Yap"
                          >
                            <i className="fas fa-clock"></i>
                          </button>
                        )}
                        {order.status === "preparing" && (
                          <button
                            onClick={() => updateOrderStatus(order.id, "ready")}
                            className="btn btn-info btn-sm"
                            title="Hazır Yap"
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
                      <strong>E-posta:</strong> {selectedOrder.customerEmail}
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
                      {new Date(selectedOrder.orderDate).toLocaleString(
                        "tr-TR"
                      )}
                    </p>
                    <p>
                      <strong>Tutar:</strong>{" "}
                      {selectedOrder.totalAmount.toFixed(2)} ₺
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
                    {selectedOrder.courierName && (
                      <p>
                        <strong>Kurye:</strong> {selectedOrder.courierName}
                      </p>
                    )}
                  </div>
                </div>

                <h6 className="fw-bold mt-3">Ürünler</h6>
                <div className="table-responsive">
                  <table className="table table-sm">
                    <thead>
                      <tr>
                        <th>Ürün</th>
                        <th>Miktar</th>
                        <th>Birim Fiyat</th>
                        <th>Toplam</th>
                      </tr>
                    </thead>
                    <tbody>
                      {selectedOrder.items.map((item, index) => (
                        <tr key={index}>
                          <td>{item.name}</td>
                          <td>
                            {item.quantity} {item.unit}
                          </td>
                          <td>{item.price.toFixed(2)} ₺</td>
                          <td>{(item.quantity * item.price).toFixed(2)} ₺</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Kurye Atama */}
                {selectedOrder.status === "ready" &&
                  !selectedOrder.courierId && (
                    <div className="mt-4">
                      <h6 className="fw-bold">Kurye Atama</h6>
                      <div className="d-flex gap-2 flex-wrap">
                        {couriers
                          .filter((c) => c.status === "active")
                          .map((courier) => (
                            <button
                              key={courier.id}
                              onClick={() =>
                                assignCourier(selectedOrder.id, courier.id)
                              }
                              disabled={assigningCourier}
                              className="btn btn-outline-success btn-sm"
                            >
                              {assigningCourier ? (
                                <span className="spinner-border spinner-border-sm me-2"></span>
                              ) : (
                                <i className="fas fa-motorcycle me-2"></i>
                              )}
                              {courier.name}
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
    </AdminLayout>
  );
}
