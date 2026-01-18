/**
 * CourierAssignmentModal.jsx - Kurye Atama Modalı
 * 
 * Bu component manuel kurye seçimi için kullanılır.
 * Özellikler:
 * - Uygun kuryeleri listeleme
 * - Kurye uygunluk skoru gösterimi
 * - Online/offline durumu
 * - Kapasite ve mesafe bilgisi
 * - Arama ve filtreleme
 * 
 * NEDEN Modal: Hızlı kurye seçimi için sayfa değişikliği gerektirmeyen arayüz
 * NEDEN Skor: En uygun kuryenin seçilmesini kolaylaştırır
 */

import { useEffect, useState } from "react";
import { DeliveryTaskService } from "../../../services/deliveryTaskService";

export default function CourierAssignmentModal({ 
  task, 
  isReassign = false,
  onAssign, 
  onClose 
}) {
  // =========================================================================
  // STATE
  // =========================================================================
  const [couriers, setCouriers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [assigning, setAssigning] = useState(false);
  const [selectedCourier, setSelectedCourier] = useState(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [showOnlineOnly, setShowOnlineOnly] = useState(true);

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================

  useEffect(() => {
    loadCouriers();
  }, [task?.id]);

  const loadCouriers = async () => {
    try {
      setLoading(true);
      
      // Backend'den uygun kuryeleri (skor hesaplaması ile) al
      const data = await DeliveryTaskService.getAvailableCouriers(task?.id);
      setCouriers(Array.isArray(data) ? data : []);
    } catch (error) {
      console.error("Kurye yükleme hatası:", error);
      setCouriers([]);
    } finally {
      setLoading(false);
    }
  };

  // =========================================================================
  // FİLTRELEME
  // =========================================================================

  const filteredCouriers = couriers.filter(courier => {
    // Arama filtresi
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      if (!courier.name?.toLowerCase().includes(query) && 
          !courier.phone?.toLowerCase().includes(query)) {
        return false;
      }
    }
    
    // Online filtresi
    if (showOnlineOnly && !courier.isOnline) {
      return false;
    }
    
    // Mevcut kuryeyi hariç tut (reassign durumunda)
    if (isReassign && courier.id === task?.courierId) {
      return false;
    }
    
    return true;
  });

  // =========================================================================
  // KURYE ATAMA
  // =========================================================================

  const handleAssign = async () => {
    if (!selectedCourier) return;
    
    setAssigning(true);
    try {
      await onAssign(selectedCourier.id);
    } finally {
      setAssigning(false);
    }
  };

  // =========================================================================
  // YARDIMCI FONKSİYONLAR
  // =========================================================================

  /**
   * Uygunluk skoruna göre renk döndürür
   * NEDEN: Görsel olarak en uygun kuryeyi ayırt etmeyi kolaylaştırır
   */
  const getScoreColor = (score) => {
    if (score >= 80) return "success";
    if (score >= 60) return "warning";
    if (score >= 40) return "info";
    return "secondary";
  };

  /**
   * Uygunluk skoru badge'i
   */
  const ScoreBadge = ({ score }) => (
    <span 
      className={`badge bg-${getScoreColor(score)}`}
      style={{ fontSize: "0.75rem" }}
    >
      <i className="fas fa-star me-1"></i>
      {score}
    </span>
  );

  /**
   * Kapasite göstergesi
   */
  const CapacityBar = ({ current, max }) => {
    const percentage = max > 0 ? (current / max) * 100 : 0;
    const remaining = max - current;
    
    return (
      <div>
        <div className="progress" style={{ height: "6px" }}>
          <div 
            className={`progress-bar ${percentage >= 80 ? "bg-danger" : percentage >= 50 ? "bg-warning" : "bg-success"}`}
            style={{ width: `${percentage}%` }}
          />
        </div>
        <small className="text-muted" style={{ fontSize: "0.65rem" }}>
          {remaining} boş / {max} kapasite
        </small>
      </div>
    );
  };

  // =========================================================================
  // RENDER
  // =========================================================================

  return (
    <div
      className="modal fade show d-block"
      tabIndex="-1"
      style={{ backgroundColor: "rgba(0,0,0,0.6)" }}
      onClick={(e) => e.target === e.currentTarget && onClose()}
    >
      <div className="modal-dialog modal-dialog-centered modal-lg">
        <div className="modal-content" style={{ borderRadius: "12px" }}>
          {/* Header */}
          <div className="modal-header py-2 px-3 bg-success text-white">
            <h6 className="modal-title" style={{ fontSize: "0.9rem" }}>
              <i className="fas fa-user-plus me-2"></i>
              {isReassign ? "Kurye Değiştir" : "Kurye Ata"}
            </h6>
            <button
              onClick={onClose}
              className="btn-close btn-close-white btn-close-sm"
              disabled={assigning}
            ></button>
          </div>

          {/* Body */}
          <div className="modal-body p-3">
            {/* Görev Özeti */}
            <div className="alert alert-light mb-3" style={{ fontSize: "0.8rem" }}>
              <div className="row">
                <div className="col-6">
                  <strong>Görev #{task?.id}</strong>
                  <br />
                  <span className="text-muted">Sipariş: {task?.orderNumber}</span>
                </div>
                <div className="col-6 text-end">
                  <strong>{task?.customerName}</strong>
                  <br />
                  <span className="text-truncate d-block" style={{ maxWidth: "200px", marginLeft: "auto" }}>
                    {task?.dropoffAddress}
                  </span>
                </div>
              </div>
            </div>

            {/* Filtreler */}
            <div className="row g-2 mb-3">
              <div className="col-md-8">
                <div className="input-group input-group-sm">
                  <span className="input-group-text">
                    <i className="fas fa-search"></i>
                  </span>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="Kurye ara (isim veya telefon)..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    style={{ fontSize: "0.8rem" }}
                  />
                </div>
              </div>
              <div className="col-md-4">
                <div className="form-check form-switch">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id="onlineOnly"
                    checked={showOnlineOnly}
                    onChange={(e) => setShowOnlineOnly(e.target.checked)}
                  />
                  <label className="form-check-label" htmlFor="onlineOnly" style={{ fontSize: "0.8rem" }}>
                    Sadece çevrimiçi
                  </label>
                </div>
              </div>
            </div>

            {/* Kurye Listesi */}
            {loading ? (
              <div className="text-center py-4">
                <div className="spinner-border text-success"></div>
                <p className="text-muted mt-2">Kuryeler yükleniyor...</p>
              </div>
            ) : filteredCouriers.length === 0 ? (
              <div className="text-center py-4 text-muted">
                <i className="fas fa-user-slash fa-3x mb-3"></i>
                <p className="mb-0">
                  {showOnlineOnly 
                    ? "Çevrimiçi kurye bulunamadı" 
                    : "Kurye bulunamadı"}
                </p>
                {showOnlineOnly && (
                  <button 
                    className="btn btn-link btn-sm"
                    onClick={() => setShowOnlineOnly(false)}
                  >
                    Tüm kuryeleri göster
                  </button>
                )}
              </div>
            ) : (
              <div className="list-group" style={{ maxHeight: "400px", overflowY: "auto" }}>
                {filteredCouriers.map((courier) => (
                  <button
                    key={courier.id}
                    className={`list-group-item list-group-item-action ${
                      selectedCourier?.id === courier.id ? "active" : ""
                    } ${!courier.isOnline ? "bg-light" : ""}`}
                    onClick={() => setSelectedCourier(courier)}
                    disabled={!courier.isOnline || courier.currentCapacity >= courier.maxCapacity}
                    style={{ fontSize: "0.8rem" }}
                  >
                    <div className="d-flex justify-content-between align-items-start">
                      {/* Sol: Kurye Bilgileri */}
                      <div className="d-flex align-items-center">
                        {/* Online Durumu */}
                        <div className="me-3">
                          <div 
                            className={`rounded-circle ${courier.isOnline ? "bg-success" : "bg-secondary"} d-flex align-items-center justify-content-center`}
                            style={{ width: "40px", height: "40px" }}
                          >
                            <i className={`fas fa-${courier.isOnline ? "motorcycle" : "moon"} text-white`}></i>
                          </div>
                        </div>
                        
                        {/* İsim ve Detaylar */}
                        <div>
                          <div className="d-flex align-items-center gap-2">
                            <strong>{courier.name}</strong>
                            {!courier.isOnline && (
                              <span className="badge bg-secondary" style={{ fontSize: "0.6rem" }}>
                                Çevrimdışı
                              </span>
                            )}
                            {courier.currentCapacity >= courier.maxCapacity && (
                              <span className="badge bg-danger" style={{ fontSize: "0.6rem" }}>
                                Dolu
                              </span>
                            )}
                          </div>
                          <small className={selectedCourier?.id === courier.id ? "text-white-50" : "text-muted"}>
                            <i className="fas fa-phone me-1"></i>
                            {courier.phone}
                          </small>
                          <div className="mt-1">
                            <span className="badge bg-light text-dark me-1" style={{ fontSize: "0.6rem" }}>
                              <i className="fas fa-car me-1"></i>
                              {courier.vehicle}
                            </span>
                            {courier.zones?.slice(0, 2).map((zone, i) => (
                              <span key={i} className="badge bg-info me-1" style={{ fontSize: "0.55rem" }}>
                                {zone}
                              </span>
                            ))}
                          </div>
                        </div>
                      </div>

                      {/* Sağ: Skor ve Kapasite */}
                      <div className="text-end" style={{ minWidth: "120px" }}>
                        <ScoreBadge score={courier.assignmentScore} />
                        
                        <div className="mt-2">
                          <CapacityBar 
                            current={courier.currentCapacity} 
                            max={courier.maxCapacity} 
                          />
                        </div>
                        
                        {courier.distanceKm && (
                          <small className={selectedCourier?.id === courier.id ? "text-white-50" : "text-muted"}>
                            <i className="fas fa-location-arrow me-1"></i>
                            {courier.distanceKm.toFixed(1)} km
                          </small>
                        )}
                        
                        <div className="mt-1">
                          <i className="fas fa-star text-warning" style={{ fontSize: "0.7rem" }}></i>
                          <span className={`ms-1 ${selectedCourier?.id === courier.id ? "text-white" : ""}`} style={{ fontSize: "0.7rem" }}>
                            {courier.rating?.toFixed(1) || "-"}
                          </span>
                          <span className={`ms-2 ${selectedCourier?.id === courier.id ? "text-white-50" : "text-muted"}`} style={{ fontSize: "0.65rem" }}>
                            ({courier.completedToday || 0} bugün)
                          </span>
                        </div>
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            )}

            {/* Seçilen Kurye Özeti */}
            {selectedCourier && (
              <div className="alert alert-success mt-3 mb-0" style={{ fontSize: "0.8rem" }}>
                <i className="fas fa-check-circle me-2"></i>
                <strong>{selectedCourier.name}</strong> seçildi
                {selectedCourier.distanceKm && (
                  <span className="ms-2">
                    ({selectedCourier.distanceKm.toFixed(1)} km uzakta)
                  </span>
                )}
              </div>
            )}
          </div>

          {/* Footer */}
          <div className="modal-footer py-2 px-3">
            <button
              onClick={onClose}
              className="btn btn-outline-secondary btn-sm"
              style={{ fontSize: "0.8rem" }}
              disabled={assigning}
            >
              İptal
            </button>
            <button
              onClick={handleAssign}
              className="btn btn-success btn-sm"
              style={{ fontSize: "0.8rem" }}
              disabled={!selectedCourier || assigning}
            >
              {assigning ? (
                <>
                  <span className="spinner-border spinner-border-sm me-1"></span>
                  Atanıyor...
                </>
              ) : (
                <>
                  <i className="fas fa-check me-1"></i>
                  {isReassign ? "Kurye Değiştir" : "Kurye Ata"}
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
