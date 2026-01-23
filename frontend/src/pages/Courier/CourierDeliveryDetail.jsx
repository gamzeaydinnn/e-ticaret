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
import {
  WeightAdjustmentService,
  WeightPaymentService,
} from "../../services/weightAdjustmentService";
import CourierActionButtons from "./CourierActionButtons";
import CourierPODCapture from "./CourierPODCapture";
import CourierFailureModal from "./CourierFailureModal";
import WeightEntryCard from "./components/WeightEntryCard";
import WeightDifferenceSummary from "./components/WeightDifferenceSummary";

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
  const [showPODModal, setShowPODModal] = useState(false);
  const [showFailureModal, setShowFailureModal] = useState(false);
  const [activeTab, setActiveTab] = useState("info"); // info, items, weight, timeline

  // Ağırlık giriş state'leri
  const [weightItems, setWeightItems] = useState([]);
  const [weightSummary, setWeightSummary] = useState(null);
  const [weightLoading, setWeightLoading] = useState(false);
  const [weightSubmitting, setWeightSubmitting] = useState(false);

  // =========================================================================
  // AUTH CHECK
  // =========================================================================
  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      navigate("/courier/login");
    }
  }, [isAuthenticated, authLoading, navigate]);

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================
  const loadTaskDetail = useCallback(async () => {
    if (!taskId) return;

    try {
      setLoading(true);
      const data =
        (await CourierService.getTaskDetail?.(taskId)) ||
        (await CourierService.getAssignedOrders(courier?.id).then((tasks) =>
          tasks.find((t) => t.id === parseInt(taskId)),
        ));

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

  // =========================================================================
  // AĞIRLIK VERİLERİNİ YÜKLEME
  // =========================================================================
  const loadWeightData = useCallback(async () => {
    if (!task?.orderId && !taskId) return;

    const orderId = task?.orderId || taskId;

    try {
      setWeightLoading(true);
      const summaryData =
        await WeightAdjustmentService.getOrderWeightSummary(orderId);

      if (summaryData) {
        setWeightSummary(summaryData);

        // Ağırlık bazlı ürünleri ayarla
        if (summaryData.adjustments) {
          setWeightItems(
            summaryData.adjustments.map((adj) => ({
              ...adj,
              id: adj.orderItemId,
              isWeightBased: true,
            })),
          );
        } else if (task?.items) {
          // Task'tan weight-based ürünleri filtrele
          const weightBasedItems = task.items.filter(
            (item) =>
              item.isWeightBased ||
              item.weightUnit === "Gram" ||
              item.weightUnit === "Kilogram",
          );
          setWeightItems(weightBasedItems);
        }
      }
    } catch (err) {
      console.error(
        "[CourierDeliveryDetail] Ağırlık verileri yüklenemedi:",
        err,
      );
      // Hata durumunda sessizce devam et - ağırlık bazlı ürün olmayabilir
    } finally {
      setWeightLoading(false);
    }
  }, [task, taskId]);

  // Sekme değiştiğinde ağırlık verilerini yükle
  useEffect(() => {
    if (activeTab === "weight" && task) {
      loadWeightData();
    }
  }, [activeTab, task, loadWeightData]);

  // =========================================================================
  // AĞIRLIK GİRİŞ HANDLERLERİ
  // =========================================================================

  /**
   * Tek ürün için ağırlık kaydet
   */
  const handleWeightSubmit = async (weightData) => {
    const orderId = task?.orderId || taskId;

    try {
      setWeightSubmitting(true);

      await WeightAdjustmentService.recordWeight(
        orderId,
        weightData.orderItemId,
        {
          actualWeightGrams: weightData.actualWeightGrams,
          notes: weightData.notes,
        },
      );

      // Verileri yenile
      await loadWeightData();
    } catch (err) {
      console.error("[CourierDeliveryDetail] Ağırlık kaydedilemedi:", err);
      throw new Error(err.response?.data?.message || "Ağırlık kaydedilemedi");
    } finally {
      setWeightSubmitting(false);
    }
  };

  /**
   * Ağırlık bazlı teslimatı tamamla
   */
  const handleWeightBasedDelivery = async () => {
    const orderId = task?.orderId || taskId;

    // Tüm ürünler tartıldı mı kontrol et
    if (weightSummary && !weightSummary.allItemsWeighed) {
      if (
        !window.confirm(
          "Bazı ürünler henüz tartılmadı. Yine de devam etmek istiyor musunuz?",
        )
      ) {
        return;
      }
    }

    // Yüksek fark uyarısı
    if (weightSummary && Math.abs(weightSummary.differencePercent) > 20) {
      const confirmMsg =
        weightSummary.differencePercent > 0
          ? `Toplam fark +%${weightSummary.differencePercent.toFixed(1)} (${weightSummary.totalDifference.toFixed(2)} ₺ ek ödeme). Devam edilsin mi?`
          : `Toplam fark ${weightSummary.differencePercent.toFixed(1)}% (${Math.abs(weightSummary.totalDifference).toFixed(2)} ₺ iade). Devam edilsin mi?`;

      if (!window.confirm(confirmMsg)) {
        return;
      }
    }

    try {
      setUpdating(true);

      // Teslimatı tamamla
      const result = await WeightPaymentService.finalizeDelivery(orderId, {
        courierNotes: `Ağırlık tartımı tamamlandı. Kurye: ${courier?.name || "Bilinmiyor"}`,
      });

      if (result.success) {
        alert("Teslimat başarıyla tamamlandı!");
        // POD modal aç veya listeye dön
        if (task.requiredProofMethods?.length > 0) {
          setShowPODModal(true);
        } else {
          setTask((prev) => ({ ...prev, status: "Delivered" }));
          setTimeout(() => navigate("/courier/orders"), 1500);
        }
      } else if (result.requiresAdminApproval) {
        alert("Yüksek fark tespit edildi. Admin onayı bekleniyor.");
      } else {
        alert(result.message || "Teslimat tamamlanamadı");
      }
    } catch (err) {
      console.error("[CourierDeliveryDetail] Teslimat tamamlanamadı:", err);
      alert(
        err.response?.data?.message || "Teslimat tamamlanırken bir hata oluştu",
      );
    } finally {
      setUpdating(false);
    }
  };

  /**
   * Siparişte ağırlık bazlı ürün var mı?
   */
  const hasWeightBasedItems = useCallback(() => {
    if (weightItems.length > 0) return true;
    if (task?.items) {
      return task.items.some(
        (item) =>
          item.isWeightBased ||
          item.weightUnit === "Gram" ||
          item.weightUnit === "Kilogram",
      );
    }
    return false;
  }, [weightItems, task]);

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

      // Duruma göre modal aç
      if (newStatus === "Delivered") {
        setShowPODModal(true);
      } else if (newStatus === "Failed") {
        setShowFailureModal(true);
      } else {
        // Görev bilgisini güncelle
        setTask((prev) => ({ ...prev, status: newStatus }));
      }
    } catch (err) {
      console.error("Durum güncelleme hatası:", err);
      alert("Durum güncellenirken bir hata oluştu");
    } finally {
      setUpdating(false);
    }
  };

  const handlePODComplete = async (podData) => {
    try {
      setUpdating(true);
      await CourierService.submitProofOfDelivery?.(task.id, podData);
      setShowPODModal(false);
      setTask((prev) => ({ ...prev, status: "Delivered" }));

      // Başarılı teslim sonrası listeye dön
      setTimeout(() => {
        navigate("/courier/orders");
      }, 1500);
    } catch (err) {
      console.error("POD gönderme hatası:", err);
      alert("Teslimat onayı gönderilemedi");
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
      Assigned: "Atandı",
      PickedUp: "Alındı",
      InTransit: "Yolda",
      Delivered: "Teslim Edildi",
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
      Delivered: "secondary",
      Failed: "danger",
      Cancelled: "dark",
    };
    return colorMap[status] || "secondary";
  };

  const openGoogleMaps = () => {
    if (task?.deliveryLatitude && task?.deliveryLongitude) {
      window.open(
        `https://www.google.com/maps/dir/?api=1&destination=${task.deliveryLatitude},${task.deliveryLongitude}`,
        "_blank",
      );
    } else if (task?.deliveryAddress) {
      window.open(
        `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(task.deliveryAddress)}`,
        "_blank",
      );
    }
  };

  const callCustomer = () => {
    if (task?.customerPhone) {
      window.location.href = `tel:${task.customerPhone}`;
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
                    {getStatusText(task.status)}
                  </span>
                </div>
              </div>
            </div>

            {/* Quick Actions */}
            <div className="d-flex gap-2">
              {task.customerPhone && (
                <button
                  className="btn btn-light btn-sm rounded-circle"
                  onClick={callCustomer}
                  style={{ width: "36px", height: "36px" }}
                >
                  <i className="fas fa-phone text-success"></i>
                </button>
              )}
              <button
                className="btn btn-light btn-sm rounded-circle"
                onClick={openGoogleMaps}
                style={{ width: "36px", height: "36px" }}
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
          {/* Ağırlık Sekmesi - Sadece ağırlık bazlı ürün varsa göster */}
          {hasWeightBasedItems() && (
            <button
              className={`detail-tab flex-shrink-0 ${activeTab === "weight" ? "active" : ""}`}
              onClick={() => setActiveTab("weight")}
              style={{ position: "relative" }}
            >
              <i className="fas fa-balance-scale me-2"></i>
              Tartı
              {/* Bekleyen tartı sayısı badge */}
              {weightSummary && !weightSummary.allItemsWeighed && (
                <span
                  className="badge bg-danger ms-1"
                  style={{
                    fontSize: "10px",
                    position: "absolute",
                    top: "5px",
                    right: "5px",
                    minWidth: "18px",
                    height: "18px",
                    borderRadius: "50%",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                  }}
                >
                  {(weightSummary.weightBasedItemCount || 0) -
                    (weightSummary.weighedItemCount || 0)}
                </span>
              )}
            </button>
          )}
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
                    {task.customerPhone && (
                      <small className="text-muted">{task.customerPhone}</small>
                    )}
                  </div>
                  {task.customerPhone && (
                    <button
                      className="btn btn-success btn-sm rounded-circle"
                      onClick={callCustomer}
                      style={{ width: "40px", height: "40px" }}
                    >
                      <i className="fas fa-phone"></i>
                    </button>
                  )}
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
                      <small className="text-muted d-block">Tutar</small>
                      <span className="fw-bold" style={{ color: "#ff6b35" }}>
                        {task.orderTotal?.toFixed(2) || "0.00"} ₺
                      </span>
                    </div>
                  </div>
                  <div className="col-6">
                    <div className="bg-light rounded p-2 text-center">
                      <small className="text-muted d-block">Ödeme</small>
                      <span className="fw-bold">
                        {task.paymentMethod === "Cash"
                          ? "Nakit"
                          : task.paymentMethod === "Card"
                            ? "Kart"
                            : "Online"}
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

              {/* POD Requirements */}
              {task.requiredProofMethods && (
                <div className="info-card shadow-sm">
                  <h6 className="fw-bold mb-3">
                    <i className="fas fa-check-circle me-2 text-success"></i>
                    Teslimat Onay Gereksinimleri
                  </h6>
                  <div className="d-flex flex-wrap gap-2">
                    {task.requiredProofMethods.includes("Photo") && (
                      <span className="badge bg-primary">
                        <i className="fas fa-camera me-1"></i>
                        Fotoğraf
                      </span>
                    )}
                    {task.requiredProofMethods.includes("Signature") && (
                      <span className="badge bg-info">
                        <i className="fas fa-signature me-1"></i>
                        İmza
                      </span>
                    )}
                    {task.requiredProofMethods.includes("Otp") && (
                      <span className="badge bg-warning text-dark">
                        <i className="fas fa-key me-1"></i>
                        OTP Kodu
                      </span>
                    )}
                  </div>
                </div>
              )}
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
                  {task.items.map((item, index) => (
                    <div
                      key={index}
                      className="list-group-item px-0 d-flex align-items-center"
                    >
                      {item.imageUrl && (
                        <img
                          src={item.imageUrl}
                          alt={item.name}
                          className="rounded me-3"
                          style={{
                            width: "50px",
                            height: "50px",
                            objectFit: "cover",
                          }}
                        />
                      )}
                      <div className="flex-grow-1">
                        <h6 className="mb-0">{item.name}</h6>
                        <small className="text-muted">
                          {item.quantity} adet × {item.price?.toFixed(2)} ₺
                        </small>
                      </div>
                      <span className="fw-bold">
                        {(item.quantity * item.price).toFixed(2)} ₺
                      </span>
                    </div>
                  ))}

                  {/* Toplam */}
                  <div className="list-group-item px-0 d-flex justify-content-between">
                    <span className="fw-bold">Toplam</span>
                    <span
                      className="fw-bold"
                      style={{ color: "#ff6b35", fontSize: "18px" }}
                    >
                      {task.orderTotal?.toFixed(2)} ₺
                    </span>
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

          {/* WEIGHT TAB - AĞIRLIK GİRİŞ SEKMESİ */}
          {activeTab === "weight" && (
            <div className="weight-entry-section">
              {/* Yükleniyor */}
              {weightLoading ? (
                <div className="text-center py-5">
                  <div className="spinner-border text-primary mb-3"></div>
                  <p className="text-muted">Ağırlık bilgileri yükleniyor...</p>
                </div>
              ) : (
                <>
                  {/* Bilgi Kartı */}
                  <div
                    className="info-card shadow-sm mb-3"
                    style={{
                      background:
                        "linear-gradient(135deg, #fff5f0 0%, #ffe8dc 100%)",
                      border: "1px solid #ffccb8",
                    }}
                  >
                    <div className="d-flex align-items-center">
                      <div
                        className="rounded-circle d-flex align-items-center justify-content-center me-3"
                        style={{
                          width: "48px",
                          height: "48px",
                          backgroundColor: "#ff6b35",
                          color: "white",
                        }}
                      >
                        <i className="fas fa-balance-scale"></i>
                      </div>
                      <div>
                        <h6 className="mb-1 fw-bold">Ağırlık Bazlı Ürünler</h6>
                        <small className="text-muted">
                          Lütfen her ürünü tartarak gerçek ağırlığını girin.
                          {task.paymentMethod === "Cash" &&
                            " Fark nakit olarak alınacak/verilecek."}
                          {(task.paymentMethod === "Card" ||
                            task.paymentMethod === "Online") &&
                            " Fark karta yansıtılacak."}
                        </small>
                      </div>
                    </div>
                  </div>

                  {/* Ağırlık Fark Özeti */}
                  <WeightDifferenceSummary
                    summary={weightSummary}
                    paymentMethod={task.paymentMethod || "Cash"}
                    loading={weightSubmitting}
                  />

                  {/* Ürün Kartları */}
                  {weightItems.length > 0 ? (
                    <>
                      <h6 className="text-muted mb-3 d-flex align-items-center justify-content-between">
                        <span>
                          <i className="fas fa-list me-2"></i>
                          Tartılacak Ürünler
                        </span>
                        <span className="badge bg-primary">
                          {weightSummary?.weighedItemCount || 0}/
                          {weightSummary?.weightBasedItemCount ||
                            weightItems.length}
                        </span>
                      </h6>

                      {weightItems.map((item, index) => (
                        <WeightEntryCard
                          key={item.id || item.orderItemId || index}
                          item={item}
                          onWeightSubmit={handleWeightSubmit}
                          disabled={weightSubmitting || updating}
                          loading={weightSubmitting}
                        />
                      ))}

                      {/* Teslimatı Tamamla Butonu */}
                      {!["Delivered", "Failed", "Cancelled"].includes(
                        task.status,
                      ) && (
                        <div className="mt-4">
                          <button
                            className="btn btn-success w-100 py-3"
                            onClick={handleWeightBasedDelivery}
                            disabled={
                              updating ||
                              weightSubmitting ||
                              (weightSummary &&
                                weightSummary.weighedItemCount === 0)
                            }
                            style={{
                              borderRadius: "12px",
                              fontSize: "16px",
                              fontWeight: "600",
                              boxShadow: "0 4px 15px rgba(40, 167, 69, 0.3)",
                            }}
                          >
                            {updating || weightSubmitting ? (
                              <>
                                <span className="spinner-border spinner-border-sm me-2"></span>
                                İşleniyor...
                              </>
                            ) : (
                              <>
                                <i className="fas fa-check-circle me-2"></i>
                                Teslimatı Tamamla
                                {weightSummary?.totalDifference !== 0 &&
                                  weightSummary?.totalDifference !==
                                    undefined && (
                                    <span className="ms-2 badge bg-light text-dark">
                                      {weightSummary.totalDifference >= 0
                                        ? "+"
                                        : ""}
                                      {weightSummary.totalDifference.toFixed(2)}{" "}
                                      ₺
                                    </span>
                                  )}
                              </>
                            )}
                          </button>

                          {/* Uyarı */}
                          {weightSummary && !weightSummary.allItemsWeighed && (
                            <div className="text-center mt-2">
                              <small className="text-warning">
                                <i className="fas fa-exclamation-triangle me-1"></i>
                                {(weightSummary.weightBasedItemCount || 0) -
                                  (weightSummary.weighedItemCount || 0)}{" "}
                                ürün henüz tartılmadı
                              </small>
                            </div>
                          )}
                        </div>
                      )}
                    </>
                  ) : (
                    <div className="text-center py-5">
                      <i
                        className="fas fa-box-open text-muted mb-3"
                        style={{ fontSize: "48px" }}
                      ></i>
                      <p className="text-muted mb-0">
                        Bu siparişte ağırlık bazlı ürün bulunmuyor.
                      </p>
                    </div>
                  )}
                </>
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

      {/* POD Modal */}
      {showPODModal && (
        <CourierPODCapture
          task={task}
          onComplete={handlePODComplete}
          onClose={() => setShowPODModal(false)}
          loading={updating}
        />
      )}

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
