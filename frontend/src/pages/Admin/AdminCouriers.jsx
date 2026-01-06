import React, { useState, useEffect } from "react";
import { CourierService } from "../../services/courierService";

export default function AdminCouriers() {
  const [couriers, setCouriers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedCourier, setSelectedCourier] = useState(null);
  const [performance, setPerformance] = useState(null);
  const [loadingPerformance, setLoadingPerformance] = useState(false);

  useEffect(() => {
    loadCouriers();
  }, []);

  const loadCouriers = async () => {
    try {
      const data = await CourierService.getAll();
      setCouriers(data);
    } catch (error) {
      console.error("Kurye listesi yüklenemedi:", error);
    } finally {
      setLoading(false);
    }
  };

  const loadCourierPerformance = async (courierId) => {
    setLoadingPerformance(true);
    try {
      const data = await CourierService.getCourierPerformance(courierId);
      setPerformance(data);
    } catch (error) {
      console.error("Performans verileri yüklenemedi:", error);
    } finally {
      setLoadingPerformance(false);
    }
  };

  const getStatusColor = (status) => {
    const colorMap = {
      active: "success",
      busy: "warning",
      offline: "secondary",
      break: "info",
    };
    return colorMap[status] || "secondary";
  };

  const getStatusText = (status) => {
    const statusMap = {
      active: "Aktif",
      busy: "Meşgul",
      offline: "Çevrimdışı",
      break: "Mola",
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
              className="fas fa-motorcycle me-2"
              style={{ color: "#f97316" }}
            ></i>
            Kurye Yönetimi
          </h5>
          <p
            className="text-muted mb-0 d-none d-sm-block"
            style={{ fontSize: "0.75rem" }}
          >
            Durum ve performans takibi
          </p>
        </div>
        <div className="d-flex gap-1">
          <button
            className="btn btn-outline-primary btn-sm px-2 py-1"
            style={{ fontSize: "0.7rem" }}
          >
            <i className="fas fa-plus me-1"></i>Yeni
          </button>
          <button
            onClick={loadCouriers}
            className="btn btn-outline-secondary btn-sm px-2 py-1"
            style={{ fontSize: "0.7rem" }}
          >
            <i className="fas fa-sync-alt"></i>
          </button>
        </div>
      </div>

      {/* Özet Kartlar - 2x2 mobil grid */}
      <div className="row g-2 mb-3 px-1">
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-success text-white"
            style={{ borderRadius: "8px" }}
          >
            <div className="card-body text-center p-2">
              <h5 className="fw-bold mb-0">
                {couriers.filter((c) => c.status === "active").length}
              </h5>
              <small style={{ fontSize: "0.65rem" }}>Aktif</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-warning text-white"
            style={{ borderRadius: "8px" }}
          >
            <div className="card-body text-center p-2">
              <h5 className="fw-bold mb-0">
                {couriers.filter((c) => c.status === "busy").length}
              </h5>
              <small style={{ fontSize: "0.65rem" }}>Meşgul</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-primary text-white"
            style={{ borderRadius: "8px" }}
          >
            <div className="card-body text-center p-2">
              <h5 className="fw-bold mb-0">
                {couriers.reduce((sum, c) => sum + c.activeOrders, 0)}
              </h5>
              <small style={{ fontSize: "0.65rem" }}>Aktif Sipariş</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-info text-white"
            style={{ borderRadius: "8px" }}
          >
            <div className="card-body text-center p-2">
              <h5 className="fw-bold mb-0">
                {(
                  couriers.reduce((sum, c) => sum + c.rating, 0) /
                    couriers.length || 0
                ).toFixed(1)}
              </h5>
              <small style={{ fontSize: "0.65rem" }}>Ort. Puan</small>
            </div>
          </div>
        </div>
      </div>

      {/* Kurye Listesi */}
      <div
        className="card border-0 shadow-sm mx-1"
        style={{ borderRadius: "10px" }}
      >
        <div className="card-header bg-white border-0 py-2 px-2 px-md-3">
          <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
            <i className="fas fa-users me-2 text-primary"></i>
            Kuryeler ({couriers.length})
          </h6>
        </div>
        <div className="card-body p-0">
          <div className="table-responsive">
            <table
              className="table table-sm mb-0"
              style={{ fontSize: "0.7rem" }}
            >
              <thead className="bg-light">
                <tr>
                  <th className="px-1 py-2">Kurye</th>
                  <th className="px-1 py-2 d-none d-md-table-cell">İletişim</th>
                  <th className="px-1 py-2">Durum</th>
                  <th className="px-1 py-2">Sipariş</th>
                  <th className="px-1 py-2 d-none d-sm-table-cell">Puan</th>
                  <th className="px-1 py-2">İşlem</th>
                </tr>
              </thead>
              <tbody>
                {couriers.map((courier) => (
                  <tr key={courier.id}>
                    <td className="px-1 py-2">
                      <div className="d-flex align-items-center">
                        <div
                          className="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center me-1"
                          style={{
                            width: "24px",
                            height: "24px",
                            minWidth: "24px",
                            fontSize: "0.6rem",
                          }}
                        >
                          <i className="fas fa-user"></i>
                        </div>
                        <div className="overflow-hidden">
                          <div
                            className="fw-semibold text-truncate"
                            style={{ maxWidth: "70px" }}
                          >
                            {courier.name}
                          </div>
                          <small
                            className="text-muted d-none d-sm-block"
                            style={{ fontSize: "0.6rem" }}
                          >
                            {courier.vehicle}
                          </small>
                        </div>
                      </div>
                    </td>
                    <td className="px-1 py-2 d-none d-md-table-cell">
                      <div
                        className="text-muted"
                        style={{ fontSize: "0.65rem" }}
                      >
                        {courier.phone}
                      </div>
                    </td>
                    <td className="px-1 py-2">
                      <span
                        className={`badge bg-${getStatusColor(courier.status)}`}
                        style={{ fontSize: "0.55rem", padding: "0.2em 0.4em" }}
                      >
                        {getStatusText(courier.status).substring(0, 5)}
                      </span>
                    </td>
                    <td className="px-1 py-2">
                      <span
                        className="badge bg-primary"
                        style={{ fontSize: "0.6rem" }}
                      >
                        {courier.activeOrders}
                      </span>
                      <span
                        className="badge bg-success ms-1 d-none d-sm-inline"
                        style={{ fontSize: "0.6rem" }}
                      >
                        {courier.completedToday}
                      </span>
                    </td>
                    <td className="px-1 py-2 d-none d-sm-table-cell">
                      <span style={{ fontSize: "0.7rem" }}>
                        {courier.rating}
                      </span>
                      <i
                        className="fas fa-star text-warning ms-1"
                        style={{ fontSize: "0.6rem" }}
                      ></i>
                    </td>
                    <td className="px-1 py-2">
                      <div className="d-flex gap-1">
                        <button
                          onClick={() => {
                            setSelectedCourier(courier);
                            loadCourierPerformance(courier.id);
                          }}
                          className="btn btn-outline-primary p-1"
                          style={{ fontSize: "0.6rem", lineHeight: 1 }}
                          title="Performans"
                        >
                          <i className="fas fa-chart-line"></i>
                        </button>
                        <button
                          className="btn btn-outline-secondary p-1 d-none d-sm-inline-block"
                          style={{ fontSize: "0.6rem", lineHeight: 1 }}
                          title="Düzenle"
                        >
                          <i className="fas fa-edit"></i>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Kurye Performans Modal */}
      {selectedCourier && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-dialog-centered mx-2">
            <div className="modal-content" style={{ borderRadius: "12px" }}>
              <div className="modal-header py-2 px-3">
                <h6 className="modal-title" style={{ fontSize: "0.9rem" }}>
                  <i className="fas fa-chart-line me-2"></i>
                  {selectedCourier.name} - Performans
                </h6>
                <button
                  onClick={() => {
                    setSelectedCourier(null);
                    setPerformance(null);
                  }}
                  className="btn-close btn-close-sm"
                ></button>
              </div>
              <div
                className="modal-body p-2 p-md-3"
                style={{ maxHeight: "70vh", overflowY: "auto" }}
              >
                {loadingPerformance ? (
                  <div className="text-center py-4">
                    <div className="spinner-border spinner-border-sm text-primary mb-2"></div>
                    <p className="small mb-0">Yükleniyor...</p>
                  </div>
                ) : performance ? (
                  <div>
                    <div className="row g-2 mb-3">
                      <div className="col-4">
                        <div
                          className="card border-0 bg-light"
                          style={{ borderRadius: "8px" }}
                        >
                          <div className="card-body text-center p-2">
                            <h5 className="text-primary mb-0">
                              {performance.deliveries.total}
                            </h5>
                            <small
                              className="text-muted"
                              style={{ fontSize: "0.6rem" }}
                            >
                              Toplam
                            </small>
                          </div>
                        </div>
                      </div>
                      <div className="col-4">
                        <div
                          className="card border-0 bg-light"
                          style={{ borderRadius: "8px" }}
                        >
                          <div className="card-body text-center p-2">
                            <h5 className="text-success mb-0">
                              {performance.deliveries.onTime}
                            </h5>
                            <small
                              className="text-muted"
                              style={{ fontSize: "0.6rem" }}
                            >
                              Zamanında
                            </small>
                          </div>
                        </div>
                      </div>
                      <div className="col-4">
                        <div
                          className="card border-0 bg-light"
                          style={{ borderRadius: "8px" }}
                        >
                          <div className="card-body text-center p-2">
                            <h5 className="text-warning mb-0">
                              {performance.deliveries.delayed}
                            </h5>
                            <small
                              className="text-muted"
                              style={{ fontSize: "0.6rem" }}
                            >
                              Geç
                            </small>
                          </div>
                        </div>
                      </div>
                    </div>

                    <h6 className="fw-bold mb-2" style={{ fontSize: "0.8rem" }}>
                      Bugünkü Aktiviteler
                    </h6>
                    <div>
                      {performance.timeline.map((item, index) => (
                        <div
                          key={index}
                          className="d-flex mb-2 align-items-center"
                        >
                          <div
                            className={`rounded-circle bg-${getStatusColor(
                              item.status
                            )} text-white d-flex align-items-center justify-content-center`}
                            style={{
                              width: "20px",
                              height: "20px",
                              minWidth: "20px",
                              fontSize: "0.5rem",
                            }}
                          >
                            <i className="fas fa-clock"></i>
                          </div>
                          <div
                            className="flex-grow-1 ms-2 d-flex justify-content-between"
                            style={{ fontSize: "0.7rem" }}
                          >
                            <span>{item.action}</span>
                            <small className="text-muted">{item.time}</small>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-4">
                    <p className="text-muted small mb-0">Veri yüklenemedi.</p>
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
