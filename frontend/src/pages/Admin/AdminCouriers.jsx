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
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h2 className="fw-bold text-dark mb-1">Kurye Yönetimi</h2>
          <p className="text-muted mb-0">
            Kuryelerin durumu ve performans takibi
          </p>
        </div>
        <div className="d-flex gap-2">
          <button className="btn btn-outline-primary">
            <i className="fas fa-plus me-2"></i>
            Yeni Kurye
          </button>
          <button onClick={loadCouriers} className="btn btn-outline-secondary">
            <i className="fas fa-sync-alt me-2"></i>
            Yenile
          </button>
        </div>
      </div>

      {/* Özet Kartlar */}
      <div className="row mb-4">
        <div className="col-md-3 mb-3">
          <div className="card border-0 shadow-sm bg-success text-white">
            <div className="card-body text-center">
              <div className="mb-2">
                <i className="fas fa-user-check fs-1"></i>
              </div>
              <h4 className="fw-bold mb-1">
                {couriers.filter((c) => c.status === "active").length}
              </h4>
              <small>Aktif Kurye</small>
            </div>
          </div>
        </div>
        <div className="col-md-3 mb-3">
          <div className="card border-0 shadow-sm bg-warning text-white">
            <div className="card-body text-center">
              <div className="mb-2">
                <i className="fas fa-clock fs-1"></i>
              </div>
              <h4 className="fw-bold mb-1">
                {couriers.filter((c) => c.status === "busy").length}
              </h4>
              <small>Meşgul Kurye</small>
            </div>
          </div>
        </div>
        <div className="col-md-3 mb-3">
          <div className="card border-0 shadow-sm bg-primary text-white">
            <div className="card-body text-center">
              <div className="mb-2">
                <i className="fas fa-motorcycle fs-1"></i>
              </div>
              <h4 className="fw-bold mb-1">
                {couriers.reduce((sum, c) => sum + c.activeOrders, 0)}
              </h4>
              <small>Toplam Aktif Sipariş</small>
            </div>
          </div>
        </div>
        <div className="col-md-3 mb-3">
          <div className="card border-0 shadow-sm bg-info text-white">
            <div className="card-body text-center">
              <div className="mb-2">
                <i className="fas fa-star fs-1"></i>
              </div>
              <h4 className="fw-bold mb-1">
                {(
                  couriers.reduce((sum, c) => sum + c.rating, 0) /
                  couriers.length
                ).toFixed(1)}
              </h4>
              <small>Ortalama Puan</small>
            </div>
          </div>
        </div>
      </div>

      {/* Kurye Listesi */}
      <div className="card border-0 shadow-sm">
        <div className="card-header bg-white border-0 py-3">
          <h5 className="fw-bold mb-0">
            <i className="fas fa-users me-2 text-primary"></i>
            Kurye Listesi ({couriers.length})
          </h5>
        </div>
        <div className="card-body p-0">
          <div className="table-responsive">
            <table className="table table-hover mb-0">
              <thead className="bg-light">
                <tr>
                  <th>Kurye</th>
                  <th>İletişim</th>
                  <th>Araç</th>
                  <th>Durum</th>
                  <th>Aktif Sipariş</th>
                  <th>Bugün Teslim</th>
                  <th>Puan</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {couriers.map((courier) => (
                  <tr key={courier.id}>
                    <td>
                      <div className="d-flex align-items-center">
                        <div
                          className="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center me-3"
                          style={{ width: "40px", height: "40px" }}
                        >
                          <i className="fas fa-user"></i>
                        </div>
                        <div>
                          <div className="fw-semibold">{courier.name}</div>
                          <small className="text-muted">
                            {courier.location}
                          </small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div>
                        <div className="text-muted small">{courier.email}</div>
                        <div className="text-muted small">{courier.phone}</div>
                      </div>
                    </td>
                    <td>
                      <span className="badge bg-light text-dark">
                        {courier.vehicle}
                      </span>
                    </td>
                    <td>
                      <span
                        className={`badge bg-${getStatusColor(courier.status)}`}
                      >
                        {getStatusText(courier.status)}
                      </span>
                    </td>
                    <td>
                      <span className="badge bg-primary">
                        {courier.activeOrders}
                      </span>
                    </td>
                    <td>
                      <span className="badge bg-success">
                        {courier.completedToday}
                      </span>
                    </td>
                    <td>
                      <div className="d-flex align-items-center">
                        <span className="me-2">{courier.rating}</span>
                        <div className="text-warning">
                          {[...Array(5)].map((_, i) => (
                            <i
                              key={i}
                              className={`fas fa-star ${
                                i < Math.floor(courier.rating)
                                  ? "text-warning"
                                  : "text-muted"
                              }`}
                              style={{ fontSize: "0.8rem" }}
                            ></i>
                          ))}
                        </div>
                      </div>
                    </td>
                    <td>
                      <div className="d-flex gap-2">
                        <button
                          onClick={() => {
                            setSelectedCourier(courier);
                            loadCourierPerformance(courier.id);
                          }}
                          className="btn btn-outline-primary btn-sm"
                          title="Performans Görüntüle"
                        >
                          <i className="fas fa-chart-line"></i>
                        </button>
                        <button
                          className="btn btn-outline-secondary btn-sm"
                          title="Düzenle"
                        >
                          <i className="fas fa-edit"></i>
                        </button>
                        <button
                          className="btn btn-outline-danger btn-sm"
                          title="Deaktif Et"
                        >
                          <i className="fas fa-ban"></i>
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
          <div className="modal-dialog modal-xl">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">
                  <i className="fas fa-chart-line me-2"></i>
                  {selectedCourier.name} - Performans Raporu
                </h5>
                <button
                  onClick={() => {
                    setSelectedCourier(null);
                    setPerformance(null);
                  }}
                  className="btn-close"
                ></button>
              </div>
              <div className="modal-body">
                {loadingPerformance ? (
                  <div className="text-center py-5">
                    <div className="spinner-border text-primary mb-3"></div>
                    <p>Performans verileri yükleniyor...</p>
                  </div>
                ) : performance ? (
                  <div className="row">
                    <div className="col-md-4">
                      <div className="card border-0 bg-light">
                        <div className="card-body text-center">
                          <h3 className="text-primary">
                            {performance.deliveries.total}
                          </h3>
                          <p className="text-muted mb-0">Toplam Teslimat</p>
                        </div>
                      </div>
                    </div>
                    <div className="col-md-4">
                      <div className="card border-0 bg-light">
                        <div className="card-body text-center">
                          <h3 className="text-success">
                            {performance.deliveries.onTime}
                          </h3>
                          <p className="text-muted mb-0">Zamanında Teslimat</p>
                        </div>
                      </div>
                    </div>
                    <div className="col-md-4">
                      <div className="card border-0 bg-light">
                        <div className="card-body text-center">
                          <h3 className="text-warning">
                            {performance.deliveries.delayed}
                          </h3>
                          <p className="text-muted mb-0">Geç Teslimat</p>
                        </div>
                      </div>
                    </div>

                    <div className="col-12 mt-4">
                      <h6 className="fw-bold">Bugünkü Aktiviteler</h6>
                      <div className="timeline">
                        {performance.timeline.map((item, index) => (
                          <div key={index} className="d-flex mb-3">
                            <div className="flex-shrink-0">
                              <div
                                className={`rounded-circle bg-${getStatusColor(
                                  item.status
                                )} text-white d-flex align-items-center justify-content-center`}
                                style={{ width: "30px", height: "30px" }}
                              >
                                <i
                                  className="fas fa-clock"
                                  style={{ fontSize: "0.7rem" }}
                                ></i>
                              </div>
                            </div>
                            <div className="flex-grow-1 ms-3">
                              <div className="d-flex justify-content-between">
                                <span className="fw-semibold">
                                  {item.action}
                                </span>
                                <small className="text-muted">
                                  {item.time}
                                </small>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-5">
                    <p className="text-muted">
                      Performans verileri yüklenemedi.
                    </p>
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
