/**
 * AdminDeliveryTasks.jsx - Teslimat Görevleri Yönetimi Sayfası
 * 
 * Bu component admin panelinde teslimat görevlerini yönetir.
 * Özellikler:
 * - Teslimat görevleri listesi (filtreleme ve sıralama)
 * - Harita görünümü (tüm kurye ve teslimat konumları)
 * - Kurye atama paneli
 * - Real-time güncellemeler (SignalR)
 * - Durum değişikliği takibi
 * 
 * NEDEN Ayrı Sayfa: Teslimat görevleri siparişlerden bağımsız yönetilmeli.
 * Bir siparişe birden fazla teslimat görevi atanabilir (iade, yeniden deneme vb.)
 */

import { useEffect, useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "react-toastify";
import { 
  DeliveryTaskService, 
  DeliveryStatus, 
  DeliveryStatusLabels, 
  DeliveryStatusColors,
  DeliveryPriority,
  DeliveryPriorityLabels,
  DeliveryPriorityColors
} from "../../services/deliveryTaskService";
import signalRService, { SignalREvents, ConnectionState } from "../../services/signalRService";
import CourierAssignmentModal from "./components/CourierAssignmentModal";
import DeliveryMap from "./components/DeliveryMap";

export default function AdminDeliveryTasks() {
  // =========================================================================
  // STATE YÖNETİMİ
  // =========================================================================
  const navigate = useNavigate();
  
  // Görev listesi
  const [tasks, setTasks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  
  // Filtreleme
  const [statusFilter, setStatusFilter] = useState("all");
  const [priorityFilter, setPriorityFilter] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  
  // Görünüm modu (liste/harita)
  const [viewMode, setViewMode] = useState("list"); // "list" | "map"
  
  // Modal durumları
  const [selectedTask, setSelectedTask] = useState(null);
  const [showAssignModal, setShowAssignModal] = useState(false);
  
  // İstatistikler
  const [statistics, setStatistics] = useState(null);
  
  // SignalR durumu
  const [signalRConnected, setSignalRConnected] = useState(false);

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
          // Yeni teslimat görevi oluşturulduğunda
          const unsubCreated = signalRService.onDeliveryEvent(
            SignalREvents.DELIVERY_CREATED,
            handleDeliveryCreated
          );
          unsubscribers.push(unsubCreated);

          // Teslimat durumu değiştiğinde
          const unsubStatus = signalRService.onDeliveryEvent(
            SignalREvents.DELIVERY_STATUS_CHANGED,
            handleStatusChanged
          );
          unsubscribers.push(unsubStatus);

          // Kurye atandığında
          const unsubAssigned = signalRService.onDeliveryEvent(
            SignalREvents.DELIVERY_ASSIGNED,
            handleCourierAssigned
          );
          unsubscribers.push(unsubAssigned);

          // Kurye konumu güncellendiğinde
          const unsubLocation = signalRService.onDeliveryEvent(
            SignalREvents.COURIER_LOCATION_UPDATED,
            handleCourierLocation
          );
          unsubscribers.push(unsubLocation);

          // Bağlantı durumu
          const unsubState = signalRService.deliveryHub.onStateChange((newState) => {
            setSignalRConnected(newState === ConnectionState.CONNECTED);
          });
          unsubscribers.push(unsubState);
        }
      } catch (error) {
        console.error("[AdminDeliveryTasks] SignalR hatası:", error);
      }
    };

    setupSignalR();
    return () => unsubscribers.forEach(unsub => typeof unsub === "function" && unsub());
  }, []);

  /**
   * Yeni teslimat görevi oluşturulduğunda
   */
  const handleDeliveryCreated = useCallback((task) => {
    toast.info(`Yeni teslimat görevi #${task.id} oluşturuldu`);
    setTasks(prev => [{ ...task, isNew: true }, ...prev]);
    loadStatistics();
  }, []);

  /**
   * Teslimat durumu değiştiğinde
   */
  const handleStatusChanged = useCallback((data) => {
    setTasks(prev => prev.map(task =>
      task.id === data.taskId
        ? { ...task, status: data.newStatus }
        : task
    ));
    loadStatistics();
  }, []);

  /**
   * Kurye atandığında
   */
  const handleCourierAssigned = useCallback((data) => {
    toast.success(`Görev #${data.taskId} → ${data.courierName}`);
    setTasks(prev => prev.map(task =>
      task.id === data.taskId
        ? { 
            ...task, 
            status: DeliveryStatus.ASSIGNED,
            courierId: data.courierId,
            courierName: data.courierName
          }
        : task
    ));
  }, []);

  /**
   * Kurye konumu güncellendiğinde (harita için)
   */
  const handleCourierLocation = useCallback((data) => {
    // Kurye konumunu güncelle - harita bileşenine aktarılacak
    setTasks(prev => prev.map(task =>
      task.courierId === data.courierId
        ? { 
            ...task, 
            courierLatitude: data.latitude,
            courierLongitude: data.longitude
          }
        : task
    ));
  }, []);

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================

  useEffect(() => {
    loadData();
  }, [statusFilter]);

  const loadData = async () => {
    try {
      setLoading(true);
      
      // Görevleri ve istatistikleri paralel yükle
      const [tasksData, statsData] = await Promise.all([
        DeliveryTaskService.getAll(
          statusFilter !== "all" ? { status: parseInt(statusFilter) } : {}
        ),
        DeliveryTaskService.getStatistics()
      ]);
      
      setTasks(Array.isArray(tasksData) ? tasksData : []);
      setStatistics(statsData);
    } catch (error) {
      console.error("Veri yükleme hatası:", error);
      toast.error("Veriler yüklenemedi");
    } finally {
      setLoading(false);
    }
  };

  const loadStatistics = async () => {
    try {
      const stats = await DeliveryTaskService.getStatistics();
      setStatistics(stats);
    } catch (error) {
      console.error("İstatistik yükleme hatası:", error);
    }
  };

  const refreshData = async () => {
    setRefreshing(true);
    await loadData();
    setRefreshing(false);
  };

  // =========================================================================
  // FİLTRELEME VE SIRALAMA
  // =========================================================================

  /**
   * Filtrelenmiş görev listesi
   */
  const filteredTasks = tasks.filter(task => {
    // Arama filtresi
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesSearch = 
        task.orderNumber?.toLowerCase().includes(query) ||
        task.customerName?.toLowerCase().includes(query) ||
        task.courierName?.toLowerCase().includes(query) ||
        task.dropoffAddress?.toLowerCase().includes(query);
      
      if (!matchesSearch) return false;
    }
    
    // Öncelik filtresi
    if (priorityFilter !== "all" && task.priority !== parseInt(priorityFilter)) {
      return false;
    }
    
    return true;
  });

  // =========================================================================
  // GÖREV İŞLEMLERİ
  // =========================================================================

  /**
   * Kurye atama modal'ını açar
   */
  const openAssignModal = (task) => {
    setSelectedTask(task);
    setShowAssignModal(true);
  };

  /**
   * Kurye atama işlemi
   */
  const handleAssignCourier = async (courierId) => {
    if (!selectedTask) return;

    try {
      await DeliveryTaskService.assignCourier(selectedTask.id, courierId);
      
      toast.success("Kurye başarıyla atandı");
      setShowAssignModal(false);
      setSelectedTask(null);
      
      // Listeyi güncelle
      await loadData();
    } catch (error) {
      console.error("Kurye atama hatası:", error);
      toast.error(error.message || "Kurye atanamadı");
    }
  };

  /**
   * Görev iptal etme
   */
  const handleCancelTask = async (taskId) => {
    if (!window.confirm("Bu teslimat görevini iptal etmek istediğinizden emin misiniz?")) {
      return;
    }

    try {
      await DeliveryTaskService.cancel(taskId, "Admin tarafından iptal edildi");
      toast.success("Görev iptal edildi");
      await loadData();
    } catch (error) {
      console.error("Görev iptal hatası:", error);
      toast.error("Görev iptal edilemedi");
    }
  };

  /**
   * Görev detayına git
   */
  const goToTaskDetail = (taskId) => {
    navigate(`/admin/delivery-tasks/${taskId}`);
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

  // =========================================================================
  // RENDER
  // =========================================================================

  return (
    <div style={{ overflow: "hidden", maxWidth: "100%" }}>
      {/* ===================================================================== */}
      {/* HEADER */}
      {/* ===================================================================== */}
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 gap-2 px-1">
        <div>
          <h5 className="fw-bold text-dark mb-0" style={{ fontSize: "1rem" }}>
            <i className="fas fa-truck me-2" style={{ color: "#10b981" }}></i>
            Teslimat Görevleri
            {/* SignalR durumu */}
            <span 
              className={`ms-2 badge ${signalRConnected ? "bg-success" : "bg-secondary"}`}
              style={{ fontSize: "0.5rem", verticalAlign: "middle" }}
            >
              <i className={`fas ${signalRConnected ? "fa-wifi" : "fa-wifi-slash"} me-1`}></i>
              {signalRConnected ? "CANLI" : "ÇEVRIMDIŞI"}
            </span>
          </h5>
          <p className="text-muted mb-0 d-none d-sm-block" style={{ fontSize: "0.75rem" }}>
            {filteredTasks.length} görev listeleniyor
          </p>
        </div>
        
        <div className="d-flex gap-1">
          {/* Görünüm Değiştirici */}
          <div className="btn-group btn-group-sm" role="group">
            <button
              className={`btn ${viewMode === "list" ? "btn-primary" : "btn-outline-primary"}`}
              onClick={() => setViewMode("list")}
              style={{ fontSize: "0.7rem" }}
            >
              <i className="fas fa-list"></i>
            </button>
            <button
              className={`btn ${viewMode === "map" ? "btn-primary" : "btn-outline-primary"}`}
              onClick={() => setViewMode("map")}
              style={{ fontSize: "0.7rem" }}
            >
              <i className="fas fa-map-marked-alt"></i>
            </button>
          </div>
          
          <button
            onClick={refreshData}
            className="btn btn-outline-primary btn-sm px-2 py-1"
            style={{ fontSize: "0.75rem" }}
            disabled={refreshing}
          >
            <i className={`fas fa-sync-alt ${refreshing ? "fa-spin" : ""} me-1`}></i>
            Yenile
          </button>
        </div>
      </div>

      {/* ===================================================================== */}
      {/* İSTATİSTİK KARTLARI */}
      {/* ===================================================================== */}
      {statistics && (
        <div className="row g-2 mb-3 px-1">
          <div className="col-6 col-md-2">
            <div className="card border-0 shadow-sm bg-secondary text-white" style={{ borderRadius: "6px" }}>
              <div className="card-body text-center px-1 py-2">
                <h6 className="fw-bold mb-0">{statistics.pending || 0}</h6>
                <small style={{ fontSize: "0.55rem" }}>Bekleyen</small>
              </div>
            </div>
          </div>
          <div className="col-6 col-md-2">
            <div className="card border-0 shadow-sm bg-info text-white" style={{ borderRadius: "6px" }}>
              <div className="card-body text-center px-1 py-2">
                <h6 className="fw-bold mb-0">{statistics.assigned || 0}</h6>
                <small style={{ fontSize: "0.55rem" }}>Atanan</small>
              </div>
            </div>
          </div>
          <div className="col-6 col-md-2">
            <div className="card border-0 shadow-sm bg-warning text-dark" style={{ borderRadius: "6px" }}>
              <div className="card-body text-center px-1 py-2">
                <h6 className="fw-bold mb-0">{statistics.inProgress || 0}</h6>
                <small style={{ fontSize: "0.55rem" }}>Devam Eden</small>
              </div>
            </div>
          </div>
          <div className="col-6 col-md-2">
            <div className="card border-0 shadow-sm bg-success text-white" style={{ borderRadius: "6px" }}>
              <div className="card-body text-center px-1 py-2">
                <h6 className="fw-bold mb-0">{statistics.delivered || 0}</h6>
                <small style={{ fontSize: "0.55rem" }}>Teslim</small>
              </div>
            </div>
          </div>
          <div className="col-6 col-md-2">
            <div className="card border-0 shadow-sm bg-danger text-white" style={{ borderRadius: "6px" }}>
              <div className="card-body text-center px-1 py-2">
                <h6 className="fw-bold mb-0">{statistics.failed || 0}</h6>
                <small style={{ fontSize: "0.55rem" }}>Başarısız</small>
              </div>
            </div>
          </div>
          <div className="col-6 col-md-2">
            <div className="card border-0 shadow-sm bg-primary text-white" style={{ borderRadius: "6px" }}>
              <div className="card-body text-center px-1 py-2">
                <h6 className="fw-bold mb-0">{statistics.onTimeRate || 0}%</h6>
                <small style={{ fontSize: "0.55rem" }}>Zamanında</small>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ===================================================================== */}
      {/* FİLTRELER */}
      {/* ===================================================================== */}
      <div className="card border-0 shadow-sm mb-3 mx-1" style={{ borderRadius: "8px" }}>
        <div className="card-body p-2">
          <div className="row g-2 align-items-center">
            {/* Arama */}
            <div className="col-12 col-md-4">
              <div className="input-group input-group-sm">
                <span className="input-group-text" style={{ fontSize: "0.7rem" }}>
                  <i className="fas fa-search"></i>
                </span>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Sipariş, müşteri veya kurye ara..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  style={{ fontSize: "0.75rem" }}
                />
              </div>
            </div>
            
            {/* Durum Filtresi */}
            <div className="col-6 col-md-3">
              <select
                className="form-select form-select-sm"
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                style={{ fontSize: "0.75rem" }}
              >
                <option value="all">Tüm Durumlar</option>
                {Object.entries(DeliveryStatusLabels).map(([key, label]) => (
                  <option key={key} value={key}>{label}</option>
                ))}
              </select>
            </div>
            
            {/* Öncelik Filtresi */}
            <div className="col-6 col-md-3">
              <select
                className="form-select form-select-sm"
                value={priorityFilter}
                onChange={(e) => setPriorityFilter(e.target.value)}
                style={{ fontSize: "0.75rem" }}
              >
                <option value="all">Tüm Öncelikler</option>
                {Object.entries(DeliveryPriorityLabels).map(([key, label]) => (
                  <option key={key} value={key}>{label}</option>
                ))}
              </select>
            </div>
            
            {/* Temizle Butonu */}
            <div className="col-12 col-md-2 text-end">
              {(searchQuery || statusFilter !== "all" || priorityFilter !== "all") && (
                <button
                  className="btn btn-outline-secondary btn-sm"
                  onClick={() => {
                    setSearchQuery("");
                    setStatusFilter("all");
                    setPriorityFilter("all");
                  }}
                  style={{ fontSize: "0.7rem" }}
                >
                  <i className="fas fa-times me-1"></i>
                  Temizle
                </button>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* ===================================================================== */}
      {/* İÇERİK - LİSTE VEYA HARİTA */}
      {/* ===================================================================== */}
      {viewMode === "list" ? (
        // LİSTE GÖRÜNÜMÜ
        <div className="card border-0 shadow-sm mx-1" style={{ borderRadius: "10px" }}>
          <div className="card-header bg-white border-0 py-2 px-2 px-md-3">
            <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
              <i className="fas fa-tasks me-2 text-success"></i>
              Görevler ({filteredTasks.length})
            </h6>
          </div>
          <div className="card-body p-0">
            <div className="table-responsive">
              <table className="table table-sm table-hover mb-0" style={{ fontSize: "0.7rem" }}>
                <thead className="bg-light">
                  <tr>
                    <th className="px-2 py-2">Görev</th>
                    <th className="px-2 py-2 d-none d-md-table-cell">Müşteri</th>
                    <th className="px-2 py-2">Durum</th>
                    <th className="px-2 py-2">Öncelik</th>
                    <th className="px-2 py-2 d-none d-sm-table-cell">Kurye</th>
                    <th className="px-2 py-2 d-none d-lg-table-cell">Adres</th>
                    <th className="px-2 py-2">İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredTasks.length === 0 ? (
                    <tr>
                      <td colSpan="7" className="text-center py-4 text-muted">
                        <i className="fas fa-inbox fa-2x mb-2 d-block"></i>
                        Görev bulunamadı
                      </td>
                    </tr>
                  ) : (
                    filteredTasks.map((task) => (
                      <tr 
                        key={task.id} 
                        className={task.isNew ? "table-info" : ""}
                        style={{ cursor: "pointer" }}
                        onClick={() => goToTaskDetail(task.id)}
                      >
                        <td className="px-2 py-2">
                          <span className="fw-bold text-primary">#{task.id}</span>
                          <br />
                          <small className="text-muted" style={{ fontSize: "0.6rem" }}>
                            {task.orderNumber}
                          </small>
                        </td>
                        <td className="px-2 py-2 d-none d-md-table-cell">
                          <span className="fw-semibold">{task.customerName}</span>
                          <br />
                          <small className="text-muted" style={{ fontSize: "0.6rem" }}>
                            {task.customerPhone}
                          </small>
                        </td>
                        <td className="px-2 py-2">
                          <span 
                            className={`badge bg-${DeliveryStatusColors[task.status] || "secondary"}`}
                            style={{ fontSize: "0.6rem" }}
                          >
                            {DeliveryStatusLabels[task.status] || "Bilinmiyor"}
                          </span>
                        </td>
                        <td className="px-2 py-2">
                          <span 
                            className={`badge bg-${DeliveryPriorityColors[task.priority] || "secondary"}`}
                            style={{ fontSize: "0.6rem" }}
                          >
                            {DeliveryPriorityLabels[task.priority] || "Normal"}
                          </span>
                        </td>
                        <td className="px-2 py-2 d-none d-sm-table-cell">
                          {task.courierName ? (
                            <span className="text-success" style={{ fontSize: "0.7rem" }}>
                              <i className="fas fa-motorcycle me-1"></i>
                              {task.courierName}
                            </span>
                          ) : (
                            <span className="text-warning" style={{ fontSize: "0.65rem" }}>
                              <i className="fas fa-user-clock me-1"></i>
                              Atanmadı
                            </span>
                          )}
                        </td>
                        <td className="px-2 py-2 d-none d-lg-table-cell">
                          <span 
                            className="text-truncate d-block" 
                            style={{ maxWidth: "150px", fontSize: "0.65rem" }}
                            title={task.dropoffAddress}
                          >
                            {task.dropoffAddress}
                          </span>
                        </td>
                        <td className="px-2 py-2" onClick={(e) => e.stopPropagation()}>
                          <div className="d-flex gap-1">
                            <button
                              className="btn btn-outline-primary p-1"
                              style={{ fontSize: "0.55rem", lineHeight: 1 }}
                              title="Detay"
                              onClick={() => goToTaskDetail(task.id)}
                            >
                              <i className="fas fa-eye"></i>
                            </button>
                            
                            {/* Kurye atanmamışsa atama butonu */}
                            {task.status === DeliveryStatus.CREATED && (
                              <button
                                className="btn btn-success p-1"
                                style={{ fontSize: "0.55rem", lineHeight: 1 }}
                                title="Kurye Ata"
                                onClick={() => openAssignModal(task)}
                              >
                                <i className="fas fa-user-plus"></i>
                              </button>
                            )}
                            
                            {/* İptal butonu (teslim edilmemişse) */}
                            {![DeliveryStatus.DELIVERED, DeliveryStatus.CANCELLED].includes(task.status) && (
                              <button
                                className="btn btn-outline-danger p-1"
                                style={{ fontSize: "0.55rem", lineHeight: 1 }}
                                title="İptal Et"
                                onClick={() => handleCancelTask(task.id)}
                              >
                                <i className="fas fa-times"></i>
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      ) : (
        // HARİTA GÖRÜNÜMÜ
        <div className="card border-0 shadow-sm mx-1" style={{ borderRadius: "10px" }}>
          <div className="card-header bg-white border-0 py-2 px-2 px-md-3">
            <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
              <i className="fas fa-map-marked-alt me-2 text-success"></i>
              Teslimat Haritası
            </h6>
          </div>
          <div className="card-body p-0">
            <DeliveryMap 
              tasks={filteredTasks}
              onTaskClick={goToTaskDetail}
              onAssignClick={openAssignModal}
            />
          </div>
        </div>
      )}

      {/* ===================================================================== */}
      {/* KURYE ATAMA MODALI */}
      {/* ===================================================================== */}
      {showAssignModal && selectedTask && (
        <CourierAssignmentModal
          task={selectedTask}
          onAssign={handleAssignCourier}
          onClose={() => {
            setShowAssignModal(false);
            setSelectedTask(null);
          }}
        />
      )}
    </div>
  );
}
