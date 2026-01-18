/**
 * DeliveryTaskDetail.jsx - Teslimat Görevi Detay Sayfası
 * 
 * Bu component tek bir teslimat görevinin tüm detaylarını gösterir.
 * Özellikler:
 * - Görev bilgileri (sipariş, müşteri, adres)
 * - Kurye bilgileri
 * - Timeline (olay geçmişi)
 * - POD (Proof of Delivery) görüntüleme
 * - Durum geçişleri
 * - Real-time güncellemeler
 * 
 * NEDEN Ayrı Sayfa: Detaylı bilgi ve işlemler için yeterli alan sağlar
 */

import { useEffect, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { toast } from "react-toastify";
import { 
  DeliveryTaskService, 
  DeliveryStatus, 
  DeliveryStatusLabels, 
  DeliveryStatusColors,
  DeliveryPriorityLabels,
  DeliveryPriorityColors
} from "../../../services/deliveryTaskService";
import signalRService, { SignalREvents, ConnectionState } from "../../../services/signalRService";
import CourierAssignmentModal from "./CourierAssignmentModal";

export default function DeliveryTaskDetail() {
  // =========================================================================
  // STATE VE HOOKS
  // =========================================================================
  const { taskId } = useParams();
  const navigate = useNavigate();
  
  const [task, setTask] = useState(null);
  const [timeline, setTimeline] = useState([]);
  const [pod, setPod] = useState(null);
  const [loading, setLoading] = useState(true);
  const [signalRConnected, setSignalRConnected] = useState(false);
  
  // Modal durumları
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [showReassignModal, setShowReassignModal] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);

  // =========================================================================
  // SİGNALR BAĞLANTISI
  // =========================================================================

  useEffect(() => {
    let unsubscribers = [];

    const setupSignalR = async () => {
      try {
        const connected = await signalRService.connectAdmin();
        setSignalRConnected(connected);
        
        if (connected) {
          // Bu göreve özel durum değişikliklerini dinle
          const unsubStatus = signalRService.onDeliveryEvent(
            SignalREvents.DELIVERY_STATUS_CHANGED,
            (data) => {
              if (data.taskId === parseInt(taskId)) {
                handleStatusChanged(data);
              }
            }
          );
          unsubscribers.push(unsubStatus);

          // Kurye konum güncellemesi
          const unsubLocation = signalRService.onDeliveryEvent(
            SignalREvents.COURIER_LOCATION_UPDATED,
            (data) => {
              if (task?.courierId === data.courierId) {
                handleCourierLocation(data);
              }
            }
          );
          unsubscribers.push(unsubLocation);

          // Bağlantı durumu
          const unsubState = signalRService.deliveryHub.onStateChange((newState) => {
            setSignalRConnected(newState === ConnectionState.CONNECTED);
          });
          unsubscribers.push(unsubState);
        }
      } catch (error) {
        console.error("[DeliveryTaskDetail] SignalR hatası:", error);
      }
    };

    setupSignalR();
    return () => unsubscribers.forEach(unsub => typeof unsub === "function" && unsub());
  }, [taskId, task?.courierId]);

  const handleStatusChanged = useCallback((data) => {
    setTask(prev => prev ? { ...prev, status: data.newStatus } : prev);
    loadTimeline(); // Timeline'ı güncelle
    toast.info(`Görev durumu: ${DeliveryStatusLabels[data.newStatus]}`);
  }, []);

  const handleCourierLocation = useCallback((data) => {
    setTask(prev => prev ? {
      ...prev,
      courierLatitude: data.latitude,
      courierLongitude: data.longitude,
      courierLastUpdate: new Date().toISOString()
    } : prev);
  }, []);

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================

  useEffect(() => {
    loadData();
  }, [taskId]);

  const loadData = async () => {
    try {
      setLoading(true);
      
      // Paralel yükleme
      const [taskData, timelineData, podData] = await Promise.all([
        DeliveryTaskService.getById(parseInt(taskId)),
        DeliveryTaskService.getTimeline(parseInt(taskId)),
        DeliveryTaskService.getPOD(parseInt(taskId)).catch(() => null)
      ]);
      
      setTask(taskData);
      setTimeline(Array.isArray(timelineData) ? timelineData : []);
      setPod(podData);
    } catch (error) {
      console.error("Görev yükleme hatası:", error);
      toast.error("Görev detayları yüklenemedi");
      navigate("/admin/delivery-tasks");
    } finally {
      setLoading(false);
    }
  };

  const loadTimeline = async () => {
    try {
      const timelineData = await DeliveryTaskService.getTimeline(parseInt(taskId));
      setTimeline(Array.isArray(timelineData) ? timelineData : []);
    } catch (error) {
      console.error("Timeline yükleme hatası:", error);
    }
  };

  // =========================================================================
  // GÖREV İŞLEMLERİ
  // =========================================================================

  const handleAssignCourier = async (courierId) => {
    setActionLoading(true);
    try {
      await DeliveryTaskService.assignCourier(task.id, courierId);
      toast.success("Kurye atandı");
      setShowAssignModal(false);
      await loadData();
    } catch (error) {
      toast.error(error.message || "Kurye atanamadı");
    } finally {
      setActionLoading(false);
    }
  };

  const handleReassignCourier = async (courierId, reason) => {
    setActionLoading(true);
    try {
      await DeliveryTaskService.reassignCourier(task.id, courierId, reason);
      toast.success("Kurye değiştirildi");
      setShowReassignModal(false);
      await loadData();
    } catch (error) {
      toast.error(error.message || "Kurye değiştirilemedi");
    } finally {
      setActionLoading(false);
    }
  };

  const handleCancel = async () => {
    if (!window.confirm("Bu görevi iptal etmek istediğinizden emin misiniz?")) return;
    
    setActionLoading(true);
    try {
      await DeliveryTaskService.cancel(task.id, "Admin tarafından iptal edildi");
      toast.success("Görev iptal edildi");
      await loadData();
    } catch (error) {
      toast.error(error.message || "Görev iptal edilemedi");
    } finally {
      setActionLoading(false);
    }
  };

  // =========================================================================
  // YARDIMCI FONKSİYONLAR
  // =========================================================================

  const formatDate = (dateStr) => {
    if (!dateStr) return "-";
    return new Date(dateStr).toLocaleString("tr-TR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit"
    });
  };

  const getEventIcon = (eventType) => {
    const icons = {
      CREATED: "fa-plus-circle text-secondary",
      ASSIGNED: "fa-user-check text-info",
      ACCEPTED: "fa-thumbs-up text-primary",
      PICKED_UP: "fa-box text-warning",
      IN_TRANSIT: "fa-truck text-warning",
      DELIVERED: "fa-check-circle text-success",
      FAILED: "fa-times-circle text-danger",
      CANCELLED: "fa-ban text-dark",
      REASSIGNED: "fa-exchange-alt text-info"
    };
    return icons[eventType] || "fa-circle text-muted";
  };

  // =========================================================================
  // LOADING STATE
  // =========================================================================

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: "60vh" }}>
        <div className="spinner-border text-primary"></div>
      </div>
    );
  }

  if (!task) {
    return (
      <div className="text-center py-5">
        <i className="fas fa-exclamation-triangle fa-3x text-warning mb-3"></i>
        <h5>Görev bulunamadı</h5>
        <button className="btn btn-primary mt-3" onClick={() => navigate("/admin/delivery-tasks")}>
          Geri Dön
        </button>
      </div>
    );
  }

  // =========================================================================
  // RENDER
  // =========================================================================

  return (
    <div style={{ maxWidth: "100%" }}>
      {/* ===================================================================== */}
      {/* HEADER */}
      {/* ===================================================================== */}
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 gap-2 px-1">
        <div className="d-flex align-items-center gap-2">
          <button
            className="btn btn-outline-secondary btn-sm"
            onClick={() => navigate("/admin/delivery-tasks")}
          >
            <i className="fas fa-arrow-left"></i>
          </button>
          <div>
            <h5 className="fw-bold text-dark mb-0" style={{ fontSize: "1rem" }}>
              <i className="fas fa-truck me-2" style={{ color: "#10b981" }}></i>
              Görev #{task.id}
              {/* SignalR durumu */}
              <span 
                className={`ms-2 badge ${signalRConnected ? "bg-success" : "bg-secondary"}`}
                style={{ fontSize: "0.5rem", verticalAlign: "middle" }}
              >
                {signalRConnected ? "CANLI" : "ÇEVRIMDIŞI"}
              </span>
            </h5>
            <p className="text-muted mb-0" style={{ fontSize: "0.75rem" }}>
              Sipariş: {task.orderNumber}
            </p>
          </div>
        </div>
        
        <div className="d-flex gap-1">
          {/* Kurye Ata/Değiştir */}
          {task.status === DeliveryStatus.CREATED && (
            <button
              className="btn btn-success btn-sm"
              onClick={() => setShowAssignModal(true)}
              disabled={actionLoading}
            >
              <i className="fas fa-user-plus me-1"></i>
              Kurye Ata
            </button>
          )}
          
          {task.status === DeliveryStatus.ASSIGNED && (
            <button
              className="btn btn-warning btn-sm"
              onClick={() => setShowReassignModal(true)}
              disabled={actionLoading}
            >
              <i className="fas fa-exchange-alt me-1"></i>
              Kurye Değiştir
            </button>
          )}
          
          {/* İptal */}
          {![DeliveryStatus.DELIVERED, DeliveryStatus.CANCELLED].includes(task.status) && (
            <button
              className="btn btn-outline-danger btn-sm"
              onClick={handleCancel}
              disabled={actionLoading}
            >
              <i className="fas fa-times me-1"></i>
              İptal Et
            </button>
          )}
        </div>
      </div>

      <div className="row g-3 px-1">
        {/* ===================================================================== */}
        {/* SOL KOLON - GÖREV BİLGİLERİ */}
        {/* ===================================================================== */}
        <div className="col-12 col-lg-8">
          {/* Durum Kartı */}
          <div className="card border-0 shadow-sm mb-3" style={{ borderRadius: "10px" }}>
            <div className="card-body p-3">
              <div className="d-flex justify-content-between align-items-center">
                <div>
                  <span 
                    className={`badge bg-${DeliveryStatusColors[task.status]} fs-6 px-3 py-2`}
                  >
                    {DeliveryStatusLabels[task.status]}
                  </span>
                  <span 
                    className={`badge bg-${DeliveryPriorityColors[task.priority]} ms-2`}
                  >
                    {DeliveryPriorityLabels[task.priority]}
                  </span>
                </div>
                <div className="text-end">
                  <small className="text-muted d-block">Oluşturulma</small>
                  <span style={{ fontSize: "0.8rem" }}>{formatDate(task.createdAt)}</span>
                </div>
              </div>
            </div>
          </div>

          {/* Müşteri ve Adres Bilgileri */}
          <div className="card border-0 shadow-sm mb-3" style={{ borderRadius: "10px" }}>
            <div className="card-header bg-white border-0 py-2 px-3">
              <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
                <i className="fas fa-user me-2 text-primary"></i>
                Müşteri Bilgileri
              </h6>
            </div>
            <div className="card-body p-3">
              <div className="row g-3">
                <div className="col-md-6">
                  <label className="text-muted small">Ad Soyad</label>
                  <p className="mb-2 fw-semibold">{task.customerName}</p>
                  
                  <label className="text-muted small">Telefon</label>
                  <p className="mb-2">
                    <a href={`tel:${task.customerPhone}`} className="text-decoration-none">
                      <i className="fas fa-phone me-1"></i>
                      {task.customerPhone}
                    </a>
                  </p>
                </div>
                <div className="col-md-6">
                  <label className="text-muted small">Teslimat Adresi</label>
                  <p className="mb-2">{task.dropoffAddress}</p>
                  
                  {task.codAmount > 0 && (
                    <>
                      <label className="text-muted small">Kapıda Ödeme</label>
                      <p className="mb-0 fw-bold text-success fs-5">
                        {task.codAmount.toFixed(2)} ₺
                      </p>
                    </>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Kurye Notu */}
          {task.notesForCourier && (
            <div className="card border-0 shadow-sm mb-3 border-start border-warning border-3" style={{ borderRadius: "10px" }}>
              <div className="card-body p-3">
                <h6 className="fw-bold mb-2" style={{ fontSize: "0.85rem" }}>
                  <i className="fas fa-sticky-note me-2 text-warning"></i>
                  Kurye için Not
                </h6>
                <p className="mb-0" style={{ fontSize: "0.85rem" }}>{task.notesForCourier}</p>
              </div>
            </div>
          )}

          {/* Ürünler */}
          {task.items && task.items.length > 0 && (
            <div className="card border-0 shadow-sm mb-3" style={{ borderRadius: "10px" }}>
              <div className="card-header bg-white border-0 py-2 px-3">
                <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
                  <i className="fas fa-box me-2 text-primary"></i>
                  Ürünler ({task.items.length})
                </h6>
              </div>
              <div className="card-body p-0">
                <div className="table-responsive">
                  <table className="table table-sm mb-0" style={{ fontSize: "0.8rem" }}>
                    <thead className="bg-light">
                      <tr>
                        <th className="px-3">Ürün</th>
                        <th className="px-3 text-center">Miktar</th>
                      </tr>
                    </thead>
                    <tbody>
                      {task.items.map((item, index) => (
                        <tr key={index}>
                          <td className="px-3">{item.name}</td>
                          <td className="px-3 text-center">{item.quantity} {item.unit}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}

          {/* POD (Proof of Delivery) */}
          {pod && (
            <div className="card border-0 shadow-sm mb-3 border-start border-success border-3" style={{ borderRadius: "10px" }}>
              <div className="card-header bg-white border-0 py-2 px-3">
                <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
                  <i className="fas fa-camera me-2 text-success"></i>
                  Teslimat Kanıtı (POD)
                </h6>
              </div>
              <div className="card-body p-3">
                <div className="row g-3">
                  <div className="col-md-6">
                    <label className="text-muted small">Yöntem</label>
                    <p className="mb-2 fw-semibold">
                      {pod.method === "PHOTO" && <><i className="fas fa-camera me-1"></i>Fotoğraf</>}
                      {pod.method === "OTP" && <><i className="fas fa-key me-1"></i>OTP Kodu</>}
                      {pod.method === "SIGNATURE" && <><i className="fas fa-signature me-1"></i>İmza</>}
                    </p>
                    
                    <label className="text-muted small">Alıcı</label>
                    <p className="mb-2">{pod.recipientName || "-"}</p>
                    
                    <label className="text-muted small">Tarih</label>
                    <p className="mb-0">{formatDate(pod.capturedAt)}</p>
                  </div>
                  <div className="col-md-6">
                    {pod.photoUrl && (
                      <img 
                        src={pod.photoUrl} 
                        alt="POD Fotoğraf" 
                        className="img-fluid rounded"
                        style={{ maxHeight: "200px" }}
                      />
                    )}
                    {pod.signatureUrl && (
                      <img 
                        src={pod.signatureUrl} 
                        alt="İmza" 
                        className="img-fluid rounded border"
                        style={{ maxHeight: "100px" }}
                      />
                    )}
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* ===================================================================== */}
        {/* SAĞ KOLON - KURYE VE TİMELİNE */}
        {/* ===================================================================== */}
        <div className="col-12 col-lg-4">
          {/* Kurye Bilgileri */}
          <div className="card border-0 shadow-sm mb-3" style={{ borderRadius: "10px" }}>
            <div className="card-header bg-white border-0 py-2 px-3">
              <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
                <i className="fas fa-motorcycle me-2 text-success"></i>
                Kurye
              </h6>
            </div>
            <div className="card-body p-3">
              {task.courierName ? (
                <>
                  <div className="d-flex align-items-center mb-3">
                    <div 
                      className="rounded-circle bg-success text-white d-flex align-items-center justify-content-center me-3"
                      style={{ width: "50px", height: "50px" }}
                    >
                      <i className="fas fa-user"></i>
                    </div>
                    <div>
                      <h6 className="mb-0 fw-bold">{task.courierName}</h6>
                      <a href={`tel:${task.courierPhone}`} className="text-muted small">
                        {task.courierPhone}
                      </a>
                    </div>
                  </div>
                  
                  {task.assignedAt && (
                    <div className="mb-2">
                      <small className="text-muted">Atanma Zamanı</small>
                      <p className="mb-0" style={{ fontSize: "0.85rem" }}>{formatDate(task.assignedAt)}</p>
                    </div>
                  )}
                  
                  {task.estimatedDeliveryTime && (
                    <div className="alert alert-info py-2 mb-0" style={{ fontSize: "0.8rem" }}>
                      <i className="fas fa-clock me-1"></i>
                      Tahmini Teslim: {formatDate(task.estimatedDeliveryTime)}
                    </div>
                  )}
                </>
              ) : (
                <div className="text-center text-muted py-3">
                  <i className="fas fa-user-slash fa-2x mb-2"></i>
                  <p className="mb-0">Henüz kurye atanmadı</p>
                  {task.status === DeliveryStatus.CREATED && (
                    <button
                      className="btn btn-success btn-sm mt-2"
                      onClick={() => setShowAssignModal(true)}
                    >
                      <i className="fas fa-user-plus me-1"></i>
                      Kurye Ata
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>

          {/* Timeline */}
          <div className="card border-0 shadow-sm" style={{ borderRadius: "10px" }}>
            <div className="card-header bg-white border-0 py-2 px-3">
              <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
                <i className="fas fa-history me-2 text-primary"></i>
                Olay Geçmişi
              </h6>
            </div>
            <div className="card-body p-3">
              {timeline.length === 0 ? (
                <p className="text-muted text-center mb-0">Henüz olay yok</p>
              ) : (
                <div className="timeline">
                  {timeline.map((event, index) => (
                    <div key={event.id || index} className="d-flex mb-3">
                      <div className="me-3">
                        <i className={`fas ${getEventIcon(event.eventType)} fa-lg`}></i>
                      </div>
                      <div className="flex-grow-1">
                        <p className="mb-0 fw-semibold" style={{ fontSize: "0.8rem" }}>
                          {event.description}
                        </p>
                        <small className="text-muted">
                          {event.actorName} • {formatDate(event.timestamp)}
                        </small>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* ===================================================================== */}
      {/* MODALLER */}
      {/* ===================================================================== */}
      {showAssignModal && (
        <CourierAssignmentModal
          task={task}
          onAssign={handleAssignCourier}
          onClose={() => setShowAssignModal(false)}
        />
      )}

      {showReassignModal && (
        <CourierAssignmentModal
          task={task}
          isReassign={true}
          onAssign={(courierId) => handleReassignCourier(courierId, "Yeniden atama")}
          onClose={() => setShowReassignModal(false)}
        />
      )}
    </div>
  );
}
