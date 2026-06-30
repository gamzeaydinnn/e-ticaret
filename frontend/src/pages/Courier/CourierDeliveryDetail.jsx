// ==========================================================================
// CourierDeliveryDetail.jsx - Teslimat Detay Sayfası (Mobil Uyumlu)
// ==========================================================================
// Tek bir teslimat görevinin detayları. Müşteri bilgileri, adres, harita linki,
// ürün listesi ve teslimat aksiyon butonları.
// ==========================================================================

import React, { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { useCourierAuth } from "../../contexts/CourierAuthContext";
import { useCourierSignalR } from "../../contexts/CourierSignalRContext";
import { CourierService } from "../../services/courierService";
import CourierActionButtons from "./CourierActionButtons";
import CourierFailureModal from "./CourierFailureModal";

export default function CourierDeliveryDetail() {
  const { taskId } = useParams();
  const navigate = useNavigate();

  // Context
  const { courier, isAuthenticated, loading: authLoading } = useCourierAuth();
  const { sendStatusUpdate } = useCourierSignalR();

  // State
  const [task, setTask] = useState(null);
  const [loading, setLoading] = useState(true);
  const [updating, setUpdating] = useState(false);
  const [error, setError] = useState(null);
  const [showFailureModal, setShowFailureModal] = useState(false);
  const [activeTab, setActiveTab] = useState("info"); // info, items, timeline

  // =========================================================================
  // AUTH CHECK
  // =========================================================================
  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated || !courier?.id) {
      navigate("/courier/login");
    }
  }, [isAuthenticated, courier?.id, authLoading, navigate]);

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================
  const loadTaskDetail = useCallback(async () => {
    if (!taskId) return;

    try {
      setLoading(true);
      const data = await CourierService.getTaskDetail?.(taskId);

      if (data) {
        setTask(data);
      } else {
        setError("Görev bulunamadı");
      }
    } catch (err) {
      console.error("Görev yükleme hatası:", err);
      setError("Görev yüklenirken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  }, [taskId, courier?.id]);

  useEffect(() => {
    if (courier?.id && taskId) {
      loadTaskDetail();
    }
  }, [courier?.id, taskId, loadTaskDetail]);

  // Real-time event listener
  useEffect(() => {
    const handleTaskUpdate = (e) => {
      if (e.detail?.id === parseInt(taskId)) {
        loadTaskDetail();
      }
    };

    window.addEventListener("courierTaskUpdated", handleTaskUpdate);
    return () =>
      window.removeEventListener("courierTaskUpdated", handleTaskUpdate);
  }, [taskId, loadTaskDetail]);

  // =========================================================================
  // DURUM GÜNCELLEMELERİ
  // =========================================================================
  const handleStatusChange = async (newStatus) => {
    if (!task?.id) return;

    try {
      setUpdating(true);
      await CourierService.updateTaskStatus?.(task.id, newStatus);
      sendStatusUpdate(task.id, newStatus);

      // Duruma göre işlem yap
      if (newStatus === "Delivered") {
        // Teslim başarılı - durumu güncelle ve listeye dön
        setTask((prev) => ({ ...prev, status: "Delivered" }));
        alert("Sipariş başarıyla teslim edildi!");
        setTimeout(() => {
          navigate("/courier/orders");
        }, 500);
      } else if (newStatus === "Failed") {
        setShowFailureModal(true);
      } else {
        // Görev bilgisini yeniden yükle (backend'den güncel durumu al)
        await loadTaskDetail();
      }
    } catch (err) {
      console.error("Durum güncelleme hatası:", err);
      const errorMessage =
        err.response?.data?.message ||
        err.response?.data?.Message ||
        err.message ||
        "Bilinmeyen bir hata oluştu";
      alert(`Durum güncellenemedi: ${errorMessage}`);
    } finally {
      setUpdating(false);
    }
  };

  const handleFailureSubmit = async (failureData) => {
    try {
      setUpdating(true);
      await CourierService.submitDeliveryFailure?.(task.id, failureData);
      setShowFailureModal(false);
      setTask((prev) => ({ ...prev, status: "Failed" }));

      // Listeye dön
      setTimeout(() => {
        navigate("/courier/orders");
      }, 1500);
    } catch (err) {
      console.error("Başarısızlık raporu hatası:", err);
      alert("Rapor gönderilemedi");
    } finally {
      setUpdating(false);
    }
  };

  // =========================================================================
  // HELPERS
  // =========================================================================
  const getStatusText = (status) => {
    const statusMap = {
      Pending: "Bekliyor",
      Preparing: "Hazırlanıyor",
      Ready: "Teslim Alınmaya Hazır",
      Assigned: "Atandı",
      PickedUp: "Alındı",
      InTransit: "Yolda",
      OutForDelivery: "Yolda",
      Delivered: "Teslim Edildi",
      DeliveryFailed: "Başarısız",
      DeliveryPaymentPending: "Ödeme Bekliyor",
      Failed: "Başarısız",
      Cancelled: "İptal",
    };
    return statusMap[status] || status;
  };

  const getStatusColor = (status) => {
    const colorMap = {
      Pending: "warning",
      Assigned: "info",
      PickedUp: "primary",
      InTransit: "success",
      OutForDelivery: "primary",
      Delivered: "secondary",
      DeliveryFailed: "danger",
      DeliveryPaymentPending: "warning",
      Failed: "danger",
      Cancelled: "dark",
    };
    return colorMap[status] || "secondary";
  };

  const formatItemWeightDiff = (item) => {
    const grams = Number(item?.weightDifferenceGrams);
    if (!Number.isFinite(grams) || grams === 0) return null;
    return {
      label: grams > 0 ? "Fazlalık" : "Eksik",
      gramsText: `${grams > 0 ? "+" : ""}${grams}g`,
      amount: Number(item?.weightDifferenceAmount) || 0,
      tone: grams > 0 ? "warning" : "info",
    };
  };

  const collectionAmount =
    task?.finalAmount ?? task?.orderTotal ?? task?.cashOnDeliveryAmount ?? 0;
  const priceDiff = task?.totalPriceDifference ?? 0;

  const openGoogleMaps = () => {
    if (task?.googleMapsUrl) {
      window.open(task.googleMapsUrl, "_blank");
    } else if (task?.deliveryLatitude && task?.deliveryLongitude) {
      window.open(
        `https://www.google.com/maps/dir/?api=1&destination=${task.deliveryLatitude},${task.deliveryLongitude}`,
        "_blank",
      );
    } else if (task?.deliveryAddress) {
      window.open(
        `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(task.deliveryAddress)}`,
        "_blank",
      );
    } else {
      alert("Adres bilgisi yok");
    }
  };

  const callCustomer = () => {
    if (task?.customerPhone) {
      window.location.href = `tel:${task.customerPhone}`;
    } else {
      alert("Telefon bilgisi yok");
    }
  };

  // =========================================================================
  // LOADING STATE
  // =========================================================================
  if (authLoading || loading) {
    return (
      <div className="d-flex justify-content-center align-items-center min-vh-100 bg-light">
        <div className="text-center">
          <div className="spinner-border text-primary mb-3" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="text-muted">Görev detayları yükleniyor...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
        <div className="text-center p-4">
          <i
            className="fas fa-exclamation-triangle text-danger mb-3"
            style={{ fontSize: "48px" }}
          ></i>
          <h5 className="text-danger">{error}</h5>
          <button
            className="btn btn-outline-primary mt-3"
            onClick={() => navigate("/courier/orders")}
          >
            <i className="fas fa-arrow-left me-2"></i>
            Görev Listesine Dön
          </button>
        </div>
      </div>
    );
  }

  if (!task) return null;

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <>
      <style>{`
        .detail-tab {
          border: none;
          background: transparent;
          padding: 12px 20px;
          font-weight: 600;
          color: #6c757d;
          border-bottom: 3px solid transparent;
        }
        .detail-tab.active {
          color: #ff6b35;
          border-bottom-color: #ff6b35;
        }
        .info-card {
          background: white;
          border-radius: 16px;
          padding: 16px;
          margin-bottom: 12px;
        }
        .quick-action-btn {
          width: 40px;
          height: 40px;
          border-radius: 999px;
          display: inline-flex;
          align-items: center;
          justify-content: center;
          padding: 0;
        }
        .quick-action-btn i {
          font-size: 16px;
          line-height: 1;
        }
        .action-btn-group {
          position: fixed;
          bottom: 0;
          left: 0;
          right: 0;
          background: white;
          padding: 16px;
          padding-bottom: calc(16px + env(safe-area-inset-bottom));
          box-shadow: 0 -4px 20px rgba(0,0,0,0.1);
          z-index: 1000;
        }
        .timeline-item {
          position: relative;
          padding-left: 30px;
          padding-bottom: 20px;
        }
        .timeline-item::before {
          content: '';
          position: absolute;
          left: 8px;
          top: 24px;
          bottom: 0;
          width: 2px;
          background: #e9ecef;
        }
        .timeline-item:last-child::before {
          display: none;
        }
        .timeline-dot {
          position: absolute;
          left: 0;
          top: 4px;
          width: 18px;
          height: 18px;
          border-radius: 50%;
          background: #e9ecef;
          border: 3px solid white;
          box-shadow: 0 0 0 2px #e9ecef;
        }
        .timeline-dot.active {
          background: #ff6b35;
          box-shadow: 0 0 0 2px #ff6b35;
        }
        .timeline-dot.completed {
          background: #28a745;
          box-shadow: 0 0 0 2px #28a745;
        }
        @media (max-width: 768px) {
          .mobile-header {
            padding: 12px 16px !important;
          }
        }
      `}</style>

      <div className="min-vh-100 bg-light" style={{ paddingBottom: "100px" }}>
        {/* Header */}
        <nav
          className="navbar navbar-dark mobile-header sticky-top"
          style={{ background: "linear-gradient(135deg, #ff6b35, #ff8c00)" }}
        >
          <div className="container-fluid">
            <div className="d-flex align-items-center">
              <button
                className="btn btn-link text-white p-0 me-3"
                onClick={() => navigate(-1)}
              >
                <i className="fas fa-arrow-left fs-5"></i>
              </button>
              <div>
                <span className="navbar-brand mb-0 fw-bold">
                  Sipariş #{task.orderId || task.id}
                </span>
                <div>
                  <span
                    className={`badge bg-${getStatusColor(task.status)}`}
                    style={{ fontSize: "10px" }}
                  >
                    {task.statusText || getStatusText(task.status)}
                  </span>
                </div>
              </div>
            </div>

            {/* Quick Actions */}
            <div className="d-flex gap-2">
              <button
                className="btn btn-light quick-action-btn shadow-sm"
                onClick={callCustomer}
                aria-label="Müşteriyi ara"
                title="Müşteriyi ara"
              >
                <i className="fas fa-phone-alt text-success"></i>
              </button>
              <button
                className="btn btn-light quick-action-btn shadow-sm"
                onClick={openGoogleMaps}
                aria-label="Haritada aç"
                title="Haritada aç"
              >
                <i className="fas fa-map-marker-alt text-primary"></i>
              </button>
            </div>
          </div>
        </nav>

        {/* Tabs */}
        <div className="bg-white border-bottom d-flex overflow-auto">
          <button
            className={`detail-tab flex-shrink-0 ${activeTab === "info" ? "active" : ""}`}
            onClick={() => setActiveTab("info")}
          >
            <i className="fas fa-info-circle me-2"></i>
            Bilgiler
          </button>
          <button
            className={`detail-tab flex-shrink-0 ${activeTab === "items" ? "active" : ""}`}
            onClick={() => setActiveTab("items")}
          >
            <i className="fas fa-box me-2"></i>
            Ürünler
          </button>
          <button
            className={`detail-tab flex-shrink-0 ${activeTab === "timeline" ? "active" : ""}`}
            onClick={() => setActiveTab("timeline")}
          >
            <i className="fas fa-history me-2"></i>
            Zaman Çizelgesi
          </button>
        </div>

        {/* Content */}
        <div className="container-fluid p-3">
          {/* INFO TAB */}
          {activeTab === "info" && (
            <>
              {/* Customer Card */}
              <div className="info-card shadow-sm">
                <div className="d-flex align-items-center mb-3">
                  <div
                    className="rounded-circle d-flex align-items-center justify-content-center me-3"
                    style={{
                      width: "50px",
                      height: "50px",
                      backgroundColor: "#fff3e0",
                    }}
                  >
                    <i className="fas fa-user" style={{ color: "#ff6b35" }}></i>
                  </div>
                  <div className="flex-grow-1">
                    <h6 className="mb-0 fw-bold">
                      {task.customerName || "Müşteri"}
                    </h6>
                    <small className="text-muted">
                      {task.customerPhone || "Telefon bilgisi yok"}
                    </small>
                  </div>
                  <button
                    className="btn btn-success quick-action-btn flex-shrink-0"
                    onClick={callCustomer}
                    aria-label="Müşteriyi ara"
                    title="Müşteriyi ara"
                  >
                    <i className="fas fa-phone-alt"></i>
                  </button>
                </div>

                {task.notesForCourier && (
                  <div
                    className="alert alert-info mb-0"
                    style={{ borderRadius: "10px" }}
                  >
                    <i className="fas fa-sticky-note me-2"></i>
                    <strong>Not:</strong> {task.notesForCourier}
                  </div>
                )}
              </div>

              {/* Address Card */}
              <div className="info-card shadow-sm">
                <div className="d-flex justify-content-between align-items-start mb-3">
                  <h6 className="fw-bold mb-0">
                    <i
                      className="fas fa-map-marker-alt me-2"
                      style={{ color: "#dc3545" }}
                    ></i>
                    Teslimat Adresi
                  </h6>
                  <button
                    className="btn btn-outline-primary btn-sm"
                    onClick={openGoogleMaps}
                    style={{ borderRadius: "8px" }}
                  >
                    <i className="fas fa-directions me-1"></i>
                    Yol Tarifi
                  </button>
                </div>
                <p className="text-dark mb-2">
                  {task.deliveryAddress || "Adres bilgisi yok"}
                </p>
                {task.distanceKm && (
                  <div className="d-flex gap-3 text-muted small">
                    <span>
                      <i className="fas fa-route me-1"></i>
                      {task.distanceKm.toFixed(1)} km
                    </span>
                    {task.estimatedTime && (
                      <span>
                        <i className="fas fa-clock me-1"></i>~
                        {task.estimatedTime} dk
                      </span>
                    )}
                  </div>
                )}
              </div>

              {/* Tartı / Tahsilat Özeti (admin tarafından girilen) */}
              {(task.hasWeightBasedItems || priceDiff !== 0) && (
                <div
                  className="info-card shadow-sm mb-3"
                  style={{
                    background:
                      priceDiff > 0
                        ? "linear-gradient(135deg, #fff3cd 0%, #ffe69c 100%)"
                        : priceDiff < 0
                          ? "linear-gradient(135deg, #cff4fc 0%, #9eeaf9 100%)"
                          : "linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%)",
                    border: "1px solid #dee2e6",
                  }}
                >
                  <div className="d-flex align-items-start">
                    <div
                      className="rounded-circle d-flex align-items-center justify-content-center me-3 flex-shrink-0"
                      style={{
                        width: "48px",
                        height: "48px",
                        backgroundColor: "#ff6b35",
                        color: "white",
                      }}
                    >
                      <i className="fas fa-balance-scale"></i>
                    </div>
                    <div className="flex-grow-1">
                      <h6 className="mb-1 fw-bold">Tartı Bilgisi</h6>
                      <p className="mb-2 small text-muted">
                        Tartı mağaza/admin tarafından girildi. Teslimatta aşağıdaki tutarı tahsil edin.
                      </p>
                      <div className="d-flex flex-wrap gap-3">
                        <div>
                          <small className="text-muted d-block">Tahsil Edilecek</small>
                          <strong style={{ color: "#ff6b35", fontSize: "1.1rem" }}>
                            {Number(collectionAmount).toFixed(2)} ₺
                          </strong>
                        </div>
                        {priceDiff !== 0 && (
                          <div>
                            <small className="text-muted d-block">Tartı Farkı</small>
                            <strong
                              className={
                                priceDiff > 0 ? "text-warning" : "text-info"
                              }
                            >
                              {priceDiff > 0 ? "+" : ""}
                              {Number(priceDiff).toFixed(2)} ₺
                            </strong>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* Order Details Card */}
              <div className="info-card shadow-sm">
                <h6 className="fw-bold mb-3">
                  <i
                    className="fas fa-receipt me-2"
                    style={{ color: "#6c757d" }}
                  ></i>
                  Sipariş Detayları
                </h6>
                <div className="row g-2">
                  <div className="col-6">
                    <div className="bg-light rounded p-2 text-center">
                      <small className="text-muted d-block">Tahsilat</small>
                      <span className="fw-bold" style={{ color: "#ff6b35" }}>
                        {Number(collectionAmount).toFixed(2)} ₺
                      </span>
                      {priceDiff !== 0 && (
                        <small
                          className={`d-block ${priceDiff > 0 ? "text-warning" : "text-info"}`}
                        >
                          Fark: {priceDiff > 0 ? "+" : ""}
                          {Number(priceDiff).toFixed(2)} ₺
                        </small>
                      )}
                    </div>
                  </div>
                  <div className="col-6">
                    <div className="bg-light rounded p-2 text-center">
                      <small className="text-muted d-block">Ödeme</small>
                      <span className="fw-bold">
                        {task.paymentMethod || "Ödeme"}
                      </span>
                    </div>
                  </div>
                  <div className="col-6">
                    <div className="bg-light rounded p-2 text-center">
                      <small className="text-muted d-block">Öncelik</small>
                      <span
                        className={`fw-bold ${
                          task.priority === "High"
                            ? "text-danger"
                            : task.priority === "Low"
                              ? "text-success"
                              : ""
                        }`}
                      >
                        {task.priority === "High"
                          ? "Yüksek"
                          : task.priority === "Low"
                            ? "Düşük"
                            : "Normal"}
                      </span>
                    </div>
                  </div>
                  <div className="col-6">
                    <div className="bg-light rounded p-2 text-center">
                      <small className="text-muted d-block">
                        Zaman Penceresi
                      </small>
                      <span className="fw-bold">
                        {task.timeWindowStart
                          ? new Date(task.timeWindowStart).toLocaleTimeString(
                              "tr-TR",
                              {
                                hour: "2-digit",
                                minute: "2-digit",
                              },
                            )
                          : "--:--"}
                      </span>
                    </div>
                  </div>
                </div>
              </div>

            </>
          )}

          {/* ITEMS TAB */}
          {activeTab === "items" && (
            <div className="info-card shadow-sm">
              <h6 className="fw-bold mb-3">
                <i
                  className="fas fa-shopping-basket me-2"
                  style={{ color: "#ff6b35" }}
                ></i>
                Sipariş Ürünleri
              </h6>

              {task.items && task.items.length > 0 ? (
                <div className="list-group list-group-flush">
                  {task.items.map((item, index) => {
                    const weightDiff = formatItemWeightDiff(item);
                    return (
                      <div
                        key={item.id || item.orderItemId || index}
                        className="list-group-item px-0"
                      >
                        <div className="d-flex align-items-start">
                          {item.imageUrl && (
                            <img
                              src={item.imageUrl}
                              alt={item.name}
                              className="rounded me-3 flex-shrink-0"
                              style={{
                                width: "50px",
                                height: "50px",
                                objectFit: "cover",
                              }}
                            />
                          )}
                          <div className="flex-grow-1">
                            <h6 className="mb-1">{item.name}</h6>
                            <small className="text-muted d-block">
                              {item.quantity} ×{" "}
                              {Number(item.price || 0).toFixed(2)} ₺
                            </small>
                            {item.isWeightBased &&
                              item.actualWeightGrams != null && (
                                <small className="text-muted d-block">
                                  Tartı: {item.expectedWeightGrams}g →{" "}
                                  {item.actualWeightGrams}g
                                </small>
                              )}
                            {weightDiff && (
                              <span
                                className={`badge bg-${weightDiff.tone} mt-1`}
                              >
                                {weightDiff.label} {weightDiff.gramsText}
                                {weightDiff.amount !== 0 &&
                                  ` (${weightDiff.amount > 0 ? "+" : ""}${weightDiff.amount.toFixed(2)} ₺)`}
                              </span>
                            )}
                          </div>
                          <span className="fw-bold flex-shrink-0">
                            {Number(item.totalPrice || 0).toFixed(2)} ₺
                          </span>
                        </div>
                      </div>
                    );
                  })}

                  {/* Toplam */}
                  <div className="list-group-item px-0">
                    <div className="d-flex justify-content-between">
                      <span className="fw-bold">Tahsil Edilecek</span>
                      <span
                        className="fw-bold"
                        style={{ color: "#ff6b35", fontSize: "18px" }}
                      >
                        {Number(collectionAmount).toFixed(2)} ₺
                      </span>
                    </div>
                    {priceDiff !== 0 && (
                      <div className="d-flex justify-content-between mt-1">
                        <small className="text-muted">Tartı farkı</small>
                        <small
                          className={
                            priceDiff > 0
                              ? "text-warning fw-semibold"
                              : "text-info fw-semibold"
                          }
                        >
                          {priceDiff > 0 ? "+" : ""}
                          {Number(priceDiff).toFixed(2)} ₺
                        </small>
                      </div>
                    )}
                  </div>
                </div>
              ) : (
                <div className="text-center py-4 text-muted">
                  <i
                    className="fas fa-box-open mb-2"
                    style={{ fontSize: "32px" }}
                  ></i>
                  <p className="mb-0">Ürün listesi mevcut değil</p>
                </div>
              )}
            </div>
          )}

          {/* TIMELINE TAB */}
          {activeTab === "timeline" && (
            <div className="info-card shadow-sm">
              <h6 className="fw-bold mb-4">
                <i
                  className="fas fa-history me-2"
                  style={{ color: "#6c757d" }}
                ></i>
                Teslimat Geçmişi
              </h6>

              <div className="timeline">
                {/* Oluşturuldu */}
                <div className="timeline-item">
                  <div className="timeline-dot completed"></div>
                  <div>
                    <h6 className="mb-1">Sipariş Oluşturuldu</h6>
                    <small className="text-muted">
                      {task.createdAt
                        ? new Date(task.createdAt).toLocaleString("tr-TR")
                        : "-"}
                    </small>
                  </div>
                </div>

                {/* Atandı */}
                <div className="timeline-item">
                  <div
                    className={`timeline-dot ${task.assignedAt ? "completed" : ""}`}
                  ></div>
                  <div>
                    <h6 className="mb-1">Kuryeye Atandı</h6>
                    <small className="text-muted">
                      {task.assignedAt
                        ? new Date(task.assignedAt).toLocaleString("tr-TR")
                        : "Bekliyor"}
                    </small>
                  </div>
                </div>

                {/* Teslim Alındı */}
                <div className="timeline-item">
                  <div
                    className={`timeline-dot ${
                      task.pickedUpAt
                        ? "completed"
                        : ["PickedUp", "InTransit", "Delivered"].includes(
                              task.status,
                            )
                          ? "active"
                          : ""
                    }`}
                  ></div>
                  <div>
                    <h6 className="mb-1">Sipariş Alındı</h6>
                    <small className="text-muted">
                      {task.pickedUpAt
                        ? new Date(task.pickedUpAt).toLocaleString("tr-TR")
                        : "Bekliyor"}
                    </small>
                  </div>
                </div>

                {/* Yolda */}
                <div className="timeline-item">
                  <div
                    className={`timeline-dot ${
                      task.inTransitAt
                        ? "completed"
                        : ["InTransit", "Delivered"].includes(task.status)
                          ? "active"
                          : ""
                    }`}
                  ></div>
                  <div>
                    <h6 className="mb-1">Yola Çıkıldı</h6>
                    <small className="text-muted">
                      {task.inTransitAt
                        ? new Date(task.inTransitAt).toLocaleString("tr-TR")
                        : "Bekliyor"}
                    </small>
                  </div>
                </div>

                {/* Teslim Edildi */}
                <div className="timeline-item">
                  <div
                    className={`timeline-dot ${
                      task.deliveredAt
                        ? "completed"
                        : task.status === "Delivered"
                          ? "active"
                          : ""
                    }`}
                  ></div>
                  <div>
                    <h6 className="mb-1">
                      {task.status === "Failed"
                        ? "Teslimat Başarısız"
                        : task.status === "Cancelled"
                          ? "İptal Edildi"
                          : "Teslim Edildi"}
                    </h6>
                    <small className="text-muted">
                      {task.deliveredAt
                        ? new Date(task.deliveredAt).toLocaleString("tr-TR")
                        : task.failedAt
                          ? new Date(task.failedAt).toLocaleString("tr-TR")
                          : "Bekliyor"}
                    </small>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Action Buttons (Fixed Bottom) */}
        {!["Delivered", "Failed", "Cancelled"].includes(task.status) && (
          <div className="action-btn-group">
            <CourierActionButtons
              task={task}
              onStatusChange={handleStatusChange}
              loading={updating}
            />
          </div>
        )}

        {/* Tamamlanmış görevler için bilgi */}
        {["Delivered", "Failed", "Cancelled"].includes(task.status) && (
          <div className="action-btn-group">
            <div
              className={`alert mb-0 ${
                task.status === "Delivered"
                  ? "alert-success"
                  : task.status === "Failed"
                    ? "alert-danger"
                    : "alert-secondary"
              }`}
              style={{ borderRadius: "12px" }}
            >
              <div className="d-flex align-items-center">
                <i
                  className={`fas me-2 ${
                    task.status === "Delivered"
                      ? "fa-check-circle"
                      : task.status === "Failed"
                        ? "fa-times-circle"
                        : "fa-ban"
                  }`}
                ></i>
                <span className="fw-bold">
                  {task.status === "Delivered"
                    ? "Bu görev başarıyla tamamlandı"
                    : task.status === "Failed"
                      ? "Bu görev başarısız olarak işaretlendi"
                      : "Bu görev iptal edildi"}
                </span>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Failure Modal */}
      {showFailureModal && (
        <CourierFailureModal
          task={task}
          onSubmit={handleFailureSubmit}
          onClose={() => setShowFailureModal(false)}
          loading={updating}
        />
      )}
    </>
  );
}
