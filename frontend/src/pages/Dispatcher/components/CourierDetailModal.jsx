// ==========================================================================
// CourierDetailModal.jsx - Kurye Detay Modal Komponenti
// ==========================================================================
// Kurye hakkında detaylı bilgi gösterir: aktif siparişler, performans vb.
// NEDEN: Dispatcher'ın kurye hakkında bilgi almasını sağlar.
// ==========================================================================

import React, { useState, useEffect } from "react";
import dispatcherService from "../../../services/dispatcherService";

export default function CourierDetailModal({ courier, onClose }) {
  // =========================================================================
  // STATE
  // =========================================================================
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [courierDetail, setCourierDetail] = useState(null);

  // =========================================================================
  // VERİ YÜKLEME
  // NEDEN: Kurye detaylarını ve aktif siparişlerini çeker
  // =========================================================================
  useEffect(() => {
    const fetchDetail = async () => {
      setLoading(true);
      setError(null);

      try {
        const courierId = courier.id || courier.courierId;
        const result = await dispatcherService.getCourierDetail(courierId);

        if (result.success) {
          setCourierDetail(result.data);
        } else {
          // Eğer detay API'si yoksa mevcut veriyi kullan
          setCourierDetail({
            ...courier,
            activeOrders: [],
          });
        }
      } catch (err) {
        console.error("[CourierDetailModal] Veri yükleme hatası:", err);
        // API hatası durumunda mevcut veriyi göster
        setCourierDetail({
          ...courier,
          activeOrders: [],
        });
      } finally {
        setLoading(false);
      }
    };

    if (courier) {
      fetchDetail();
    }
  }, [courier]);

  // =========================================================================
  // YARDIMCI FONKSİYONLAR
  // =========================================================================

  // Kurye durumu badge'i
  const getStatusBadge = () => {
    if (!courier.isOnline) {
      return { bg: "danger", text: "Çevrimdışı" };
    }

    const activeCount = courier.activeOrderCount || 0;
    if (activeCount === 0) {
      return { bg: "success", text: "Müsait" };
    } else if (activeCount < 3) {
      return { bg: "warning", text: "Meşgul" };
    } else {
      return { bg: "danger", text: "Çok Meşgul" };
    }
  };

  // Araç tipi metni ve ikonu
  const getVehicleInfo = (vehicleType) => {
    switch (vehicleType) {
      case "Motorcycle":
        return { text: "Motosiklet", icon: "fa-motorcycle" };
      case "Bicycle":
        return { text: "Bisiklet", icon: "fa-bicycle" };
      case "Car":
        return { text: "Araba", icon: "fa-car" };
      case "OnFoot":
        return { text: "Yaya", icon: "fa-walking" };
      default:
        return { text: "Belirtilmemiş", icon: "fa-question" };
    }
  };

  // Son görülme zamanını formatla
  const formatLastSeen = (lastSeenAt) => {
    if (!lastSeenAt) return "Bilinmiyor";

    const lastSeen = new Date(lastSeenAt);
    const now = new Date();
    const diffMs = now - lastSeen;
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return "Az önce";
    if (diffMins < 60) return `${diffMins} dakika önce`;

    const hours = Math.floor(diffMins / 60);
    if (hours < 24) return `${hours} saat önce`;

    return lastSeen.toLocaleDateString("tr-TR");
  };

  // =========================================================================
  // RENDER
  // =========================================================================
  const status = getStatusBadge();
  const vehicle = getVehicleInfo(courier.vehicleType);
  const name = courier.name || courier.courierName;

  return (
    <div
      className="modal fade show d-block"
      style={{ background: "rgba(0,0,0,0.7)" }}
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div className="modal-dialog modal-dialog-centered modal-lg">
        <div
          className="modal-content border-0"
          style={{
            background: "#2d2d44",
            borderRadius: "20px",
          }}
        >
          {/* Header */}
          <div
            className="modal-header border-0 pb-0"
            style={{
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              borderRadius: "20px 20px 0 0",
              padding: "2rem",
            }}
          >
            <div className="d-flex align-items-center">
              {/* Avatar */}
              <div
                className="d-flex align-items-center justify-content-center me-3"
                style={{
                  width: "70px",
                  height: "70px",
                  borderRadius: "50%",
                  background: "rgba(255,255,255,0.2)",
                  border: "3px solid rgba(255,255,255,0.3)",
                }}
              >
                <i
                  className={`fas ${vehicle.icon} text-white`}
                  style={{ fontSize: "1.8rem" }}
                ></i>
              </div>

              <div>
                <h4 className="modal-title text-white mb-1 fw-bold">{name}</h4>
                <div className="d-flex align-items-center gap-2">
                  <span className={`badge bg-${status.bg}`}>{status.text}</span>
                  <span className="text-white-50 small">
                    <i className={`fas ${vehicle.icon} me-1`}></i>
                    {vehicle.text}
                  </span>
                </div>
              </div>
            </div>

            <button
              className="btn-close btn-close-white"
              onClick={onClose}
              style={{ position: "absolute", top: "1rem", right: "1rem" }}
            ></button>
          </div>

          {/* Body */}
          <div className="modal-body p-4">
            {loading ? (
              <div className="text-center py-5">
                <div className="spinner-border text-info mb-3"></div>
                <p className="text-white-50">Yükleniyor...</p>
              </div>
            ) : error ? (
              <div className="alert alert-danger">{error}</div>
            ) : (
              <>
                {/* İstatistik Kartları */}
                <div className="row g-3 mb-4">
                  <div className="col-6 col-md-3">
                    <div
                      className="text-center p-3"
                      style={{
                        background: "rgba(255,193,7,0.15)",
                        borderRadius: "12px",
                      }}
                    >
                      <i
                        className="fas fa-box text-warning mb-2"
                        style={{ fontSize: "1.5rem" }}
                      ></i>
                      <h4 className="mb-0 fw-bold text-warning">
                        {courier.activeOrderCount || 0}
                      </h4>
                      <small className="text-white-50">Aktif Sipariş</small>
                    </div>
                  </div>

                  <div className="col-6 col-md-3">
                    <div
                      className="text-center p-3"
                      style={{
                        background: "rgba(40,167,69,0.15)",
                        borderRadius: "12px",
                      }}
                    >
                      <i
                        className="fas fa-check-circle text-success mb-2"
                        style={{ fontSize: "1.5rem" }}
                      ></i>
                      <h4 className="mb-0 fw-bold text-success">
                        {courier.completedToday || 0}
                      </h4>
                      <small className="text-white-50">Bugün Teslim</small>
                    </div>
                  </div>

                  <div className="col-6 col-md-3">
                    <div
                      className="text-center p-3"
                      style={{
                        background: "rgba(102,126,234,0.15)",
                        borderRadius: "12px",
                      }}
                    >
                      <i
                        className="fas fa-star text-primary mb-2"
                        style={{ fontSize: "1.5rem" }}
                      ></i>
                      <h4 className="mb-0 fw-bold" style={{ color: "#667eea" }}>
                        {courier.rating ? courier.rating.toFixed(1) : "-"}
                      </h4>
                      <small className="text-white-50">Puan</small>
                    </div>
                  </div>

                  <div className="col-6 col-md-3">
                    <div
                      className="text-center p-3"
                      style={{
                        background: "rgba(23,162,184,0.15)",
                        borderRadius: "12px",
                      }}
                    >
                      <i
                        className="fas fa-clock text-info mb-2"
                        style={{ fontSize: "1.5rem" }}
                      ></i>
                      <h4
                        className="mb-0 fw-bold text-info"
                        style={{ fontSize: "0.9rem" }}
                      >
                        {formatLastSeen(courier.lastSeenAt)}
                      </h4>
                      <small className="text-white-50">Son Görülme</small>
                    </div>
                  </div>
                </div>

                {/* İletişim Bilgileri */}
                <div
                  className="p-3 mb-4"
                  style={{
                    background: "rgba(255,255,255,0.05)",
                    borderRadius: "12px",
                  }}
                >
                  <h6 className="text-white mb-3">
                    <i className="fas fa-address-card me-2 text-info"></i>
                    İletişim Bilgileri
                  </h6>

                  <div className="row g-3">
                    {courier.phone && (
                      <div className="col-md-6">
                        <div className="d-flex align-items-center">
                          <div
                            className="d-flex align-items-center justify-content-center me-2"
                            style={{
                              width: "36px",
                              height: "36px",
                              borderRadius: "8px",
                              background: "rgba(40,167,69,0.2)",
                            }}
                          >
                            <i className="fas fa-phone text-success"></i>
                          </div>
                          <div>
                            <small className="text-white-50 d-block">
                              Telefon
                            </small>
                            <a
                              href={`tel:${courier.phone}`}
                              className="text-white text-decoration-none"
                            >
                              {courier.phone}
                            </a>
                          </div>
                        </div>
                      </div>
                    )}

                    {courier.email && (
                      <div className="col-md-6">
                        <div className="d-flex align-items-center">
                          <div
                            className="d-flex align-items-center justify-content-center me-2"
                            style={{
                              width: "36px",
                              height: "36px",
                              borderRadius: "8px",
                              background: "rgba(102,126,234,0.2)",
                            }}
                          >
                            <i
                              className="fas fa-envelope"
                              style={{ color: "#667eea" }}
                            ></i>
                          </div>
                          <div>
                            <small className="text-white-50 d-block">
                              E-posta
                            </small>
                            <span className="text-white">{courier.email}</span>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                </div>

                {/* Aktif Siparişler */}
                <div>
                  <h6 className="text-white mb-3">
                    <i className="fas fa-box me-2 text-warning"></i>
                    Aktif Siparişler
                    {courierDetail?.activeOrders?.length > 0 && (
                      <span className="badge bg-warning text-dark ms-2">
                        {courierDetail.activeOrders.length}
                      </span>
                    )}
                  </h6>

                  {courierDetail?.activeOrders?.length > 0 ? (
                    <div className="d-flex flex-column gap-2">
                      {courierDetail.activeOrders.map((order, index) => (
                        <div
                          key={order.orderId || index}
                          className="d-flex justify-content-between align-items-center p-3"
                          style={{
                            background: "rgba(255,255,255,0.05)",
                            borderRadius: "10px",
                          }}
                        >
                          <div>
                            <span className="text-white fw-semibold">
                              #{order.orderNumber}
                            </span>
                            <small className="text-white-50 d-block">
                              {order.customerName}
                            </small>
                          </div>
                          <div className="text-end">
                            <span
                              className="badge"
                              style={{
                                background: "rgba(102,126,234,0.2)",
                                color: "#667eea",
                              }}
                            >
                              {order.statusText || order.status}
                            </span>
                            <small className="text-white-50 d-block mt-1">
                              ₺{order.totalAmount?.toFixed(2)}
                            </small>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div
                      className="text-center py-4"
                      style={{
                        background: "rgba(255,255,255,0.05)",
                        borderRadius: "10px",
                      }}
                    >
                      <i
                        className="fas fa-inbox text-white-50 mb-2"
                        style={{ fontSize: "1.5rem" }}
                      ></i>
                      <p className="text-white-50 mb-0">Aktif sipariş yok</p>
                    </div>
                  )}
                </div>
              </>
            )}
          </div>

          {/* Footer */}
          <div className="modal-footer border-0 pt-0">
            <button className="btn btn-outline-light" onClick={onClose}>
              Kapat
            </button>

            {courier.phone && (
              <a
                href={`tel:${courier.phone}`}
                className="btn"
                style={{
                  background:
                    "linear-gradient(135deg, #28a745 0%, #20c997 100%)",
                  border: "none",
                  color: "#fff",
                }}
              >
                <i className="fas fa-phone me-2"></i>
                Ara
              </a>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
