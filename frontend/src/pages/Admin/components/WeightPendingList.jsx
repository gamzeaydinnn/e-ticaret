// ==========================================================================
// WeightPendingList.jsx - Bekleyen Onaylar Listesi Komponenti
// ==========================================================================
// Admin'in onay bekleyen ağırlık fark kayıtlarını görmesi ve işlem yapması
// için kullanılan liste komponenti. Mobil uyumlu kart görünümü içerir.
// ==========================================================================

import React, { useState } from "react";
import PropTypes from "prop-types";

/**
 * WeightPendingList - Bekleyen Onaylar Listesi
 *
 * @param {Array} items - Bekleyen kayıtlar listesi
 * @param {boolean} loading - Yükleniyor durumu
 * @param {Function} onApprove - Onaylama callback
 * @param {Function} onReject - Reddetme callback
 * @param {Function} onManualAdjust - Manuel düzeltme callback
 * @param {Function} onRefresh - Yenileme callback
 */
export default function WeightPendingList({
  items,
  loading,
  onApprove,
  onReject,
  onManualAdjust,
  onRefresh,
}) {
  const [processingId, setProcessingId] = useState(null);
  const [expandedId, setExpandedId] = useState(null);

  /**
   * Onay işlemi
   */
  const handleApprove = async (id) => {
    setProcessingId(id);
    try {
      await onApprove(id);
    } finally {
      setProcessingId(null);
    }
  };

  /**
   * Red işlemi
   */
  const handleReject = async (id) => {
    setProcessingId(id);
    try {
      await onReject(id);
    } finally {
      setProcessingId(null);
    }
  };

  /**
   * Kart genişletme/daraltma
   */
  const toggleExpand = (id) => {
    setExpandedId(expandedId === id ? null : id);
  };

  /**
   * Tarih formatla
   */
  const formatDate = (dateString) => {
    if (!dateString) return "-";
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 60) return `${diffMins} dk önce`;
    if (diffHours < 24) return `${diffHours} saat önce`;
    if (diffDays < 7) return `${diffDays} gün önce`;

    return date.toLocaleDateString("tr-TR");
  };

  /**
   * Fark yüzdesine göre renk
   */
  const getDifferenceColor = (percent) => {
    if (Math.abs(percent) <= 10) return "success";
    if (Math.abs(percent) <= 20) return "warning";
    return "danger";
  };

  // Loading State
  if (loading && items.length === 0) {
    return (
      <div className="pending-list-loading">
        <div className="text-center py-5">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="text-muted mt-3">Bekleyen onaylar yükleniyor...</p>
        </div>
      </div>
    );
  }

  // Empty State
  if (items.length === 0) {
    return (
      <div className="pending-list-empty">
        <div className="empty-state">
          <div className="empty-icon">
            <i className="fas fa-check-double"></i>
          </div>
          <h5>Bekleyen Onay Yok</h5>
          <p className="text-muted">
            Şu anda onay bekleyen ağırlık farkı kaydı bulunmuyor.
          </p>
          <button className="btn btn-outline-primary" onClick={onRefresh}>
            <i className="fas fa-sync-alt me-2"></i>
            Yenile
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="pending-list">
      {/* Liste Header */}
      <div className="pending-list-header">
        <span className="pending-count">
          <i className="fas fa-exclamation-circle text-warning me-2"></i>
          {items.length} kayıt onay bekliyor
        </span>
        <button
          className="btn btn-sm btn-outline-secondary"
          onClick={onRefresh}
          disabled={loading}
        >
          <i className={`fas fa-sync-alt ${loading ? "fa-spin" : ""}`}></i>
        </button>
      </div>

      {/* Kart Listesi */}
      <div className="pending-cards">
        {items.map((item) => (
          <div
            key={item.id}
            className={`pending-card ${expandedId === item.id ? "expanded" : ""}`}
          >
            {/* Kart Header */}
            <div
              className="card-header-row"
              onClick={() => toggleExpand(item.id)}
            >
              <div className="order-info">
                <span className="order-number">
                  #{item.orderNumber || item.orderId}
                </span>
                <span className="product-name">{item.productName}</span>
              </div>
              <div className="difference-badge-wrapper">
                <span
                  className={`difference-badge bg-${getDifferenceColor(item.differencePercent)}`}
                >
                  {item.differenceAmount >= 0 ? "+" : ""}
                  {item.differenceAmount?.toFixed(2)} ₺
                </span>
                <i
                  className={`fas fa-chevron-${expandedId === item.id ? "up" : "down"} ms-2`}
                ></i>
              </div>
            </div>

            {/* Özet Bilgi */}
            <div className="card-summary">
              <div className="summary-item">
                <i className="fas fa-user text-muted"></i>
                <span>{item.customerName}</span>
              </div>
              <div className="summary-item">
                <i className="fas fa-motorcycle text-muted"></i>
                <span>{item.courierName}</span>
              </div>
              <div className="summary-item">
                <i className="fas fa-clock text-muted"></i>
                <span>{formatDate(item.createdAt)}</span>
              </div>
            </div>

            {/* Genişletilmiş İçerik */}
            {expandedId === item.id && (
              <div className="card-details">
                {/* Ağırlık Detayları */}
                <div className="weight-details">
                  <div className="weight-row">
                    <div className="weight-col">
                      <span className="weight-label">Tahmini</span>
                      <span className="weight-value">
                        {(item.estimatedWeightGrams / 1000).toFixed(2)} kg
                      </span>
                      <span className="price-value text-muted">
                        {item.estimatedPrice?.toFixed(2)} ₺
                      </span>
                    </div>
                    <div className="weight-arrow">
                      <i className="fas fa-arrow-right"></i>
                    </div>
                    <div className="weight-col">
                      <span className="weight-label">Gerçek</span>
                      <span className="weight-value highlight">
                        {(item.actualWeightGrams / 1000).toFixed(2)} kg
                      </span>
                      <span className="price-value text-primary">
                        {item.actualPrice?.toFixed(2)} ₺
                      </span>
                    </div>
                    <div className="weight-col difference">
                      <span className="weight-label">Fark</span>
                      <span
                        className={`weight-value text-${getDifferenceColor(item.differencePercent)}`}
                      >
                        {item.differenceGrams >= 0 ? "+" : ""}
                        {item.differenceGrams} g
                      </span>
                      <span
                        className={`percent-badge bg-${getDifferenceColor(item.differencePercent)}`}
                      >
                        %{item.differencePercent?.toFixed(1)}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Notlar */}
                {item.notes && (
                  <div className="notes-section">
                    <i className="fas fa-sticky-note text-warning me-2"></i>
                    <span>{item.notes}</span>
                  </div>
                )}

                {/* Aksiyon Butonları */}
                <div className="action-buttons">
                  <button
                    className="btn btn-success btn-action"
                    onClick={() => handleApprove(item.id)}
                    disabled={processingId === item.id}
                  >
                    {processingId === item.id ? (
                      <span className="spinner-border spinner-border-sm"></span>
                    ) : (
                      <>
                        <i className="fas fa-check me-2"></i>
                        Onayla
                      </>
                    )}
                  </button>
                  <button
                    className="btn btn-danger btn-action"
                    onClick={() => handleReject(item.id)}
                    disabled={processingId === item.id}
                  >
                    <i className="fas fa-times me-2"></i>
                    Reddet
                  </button>
                  <button
                    className="btn btn-outline-primary btn-action"
                    onClick={() => onManualAdjust(item)}
                    disabled={processingId === item.id}
                  >
                    <i className="fas fa-edit me-2"></i>
                    Düzenle
                  </button>
                </div>
              </div>
            )}

            {/* Hızlı Aksiyon (Daraltılmış Durumda) */}
            {expandedId !== item.id && (
              <div className="quick-actions">
                <button
                  className="btn btn-sm btn-success"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleApprove(item.id);
                  }}
                  disabled={processingId === item.id}
                  title="Onayla"
                >
                  <i className="fas fa-check"></i>
                </button>
                <button
                  className="btn btn-sm btn-danger"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleReject(item.id);
                  }}
                  disabled={processingId === item.id}
                  title="Reddet"
                >
                  <i className="fas fa-times"></i>
                </button>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

WeightPendingList.propTypes = {
  items: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.number.isRequired,
      orderId: PropTypes.number,
      orderNumber: PropTypes.string,
      productName: PropTypes.string,
      customerName: PropTypes.string,
      courierName: PropTypes.string,
      estimatedWeightGrams: PropTypes.number,
      actualWeightGrams: PropTypes.number,
      differenceGrams: PropTypes.number,
      differencePercent: PropTypes.number,
      estimatedPrice: PropTypes.number,
      actualPrice: PropTypes.number,
      differenceAmount: PropTypes.number,
      status: PropTypes.string,
      createdAt: PropTypes.string,
      notes: PropTypes.string,
    }),
  ).isRequired,
  loading: PropTypes.bool,
  onApprove: PropTypes.func.isRequired,
  onReject: PropTypes.func.isRequired,
  onManualAdjust: PropTypes.func.isRequired,
  onRefresh: PropTypes.func,
};
