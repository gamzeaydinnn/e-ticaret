// ==========================================================================
// WeightStatsDashboard.jsx - Ağırlık Fark İstatistikleri Dashboard
// ==========================================================================
// Admin panelinde ağırlık fark istatistiklerini gösteren dashboard kartları.
// Toplam, bekleyen, onaylanan, reddedilen ve mali özet bilgileri içerir.
// ==========================================================================

import React from "react";
import PropTypes from "prop-types";

/**
 * WeightStatsDashboard - İstatistik Kartları
 *
 * @param {Object} statistics - Backend'den gelen istatistik verileri
 * @param {boolean} loading - Yükleniyor durumu
 */
export default function WeightStatsDashboard({ statistics, loading }) {
  if (loading && !statistics) {
    return (
      <div className="stats-dashboard loading">
        <div className="row g-3 px-3">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="col-6 col-lg-3">
              <div className="stat-card skeleton">
                <div className="skeleton-icon"></div>
                <div className="skeleton-text"></div>
                <div className="skeleton-number"></div>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  const stats = statistics || {};

  return (
    <div className="stats-dashboard">
      <div className="row g-3 px-3">
        {/* Toplam Kayıt */}
        <div className="col-6 col-lg-3">
          <div className="stat-card total">
            <div className="stat-icon">
              <i className="fas fa-clipboard-list"></i>
            </div>
            <div className="stat-content">
              <span className="stat-label">Toplam Kayıt</span>
              <span className="stat-value">{stats.totalAdjustments || 0}</span>
              <span className="stat-trend">
                <i className="fas fa-calendar me-1"></i>
                Bu ay: {stats.thisMonthCount || 0}
              </span>
            </div>
          </div>
        </div>

        {/* Bekleyen Onaylar */}
        <div className="col-6 col-lg-3">
          <div className="stat-card pending">
            <div className="stat-icon">
              <i className="fas fa-clock"></i>
            </div>
            <div className="stat-content">
              <span className="stat-label">Bekleyen Onay</span>
              <span className="stat-value">{stats.pendingCount || 0}</span>
              <span className="stat-trend warning">
                <i className="fas fa-exclamation-triangle me-1"></i>
                Acil işlem gerekli
              </span>
            </div>
          </div>
        </div>

        {/* Onaylanan */}
        <div className="col-6 col-lg-3">
          <div className="stat-card approved">
            <div className="stat-icon">
              <i className="fas fa-check-circle"></i>
            </div>
            <div className="stat-content">
              <span className="stat-label">Onaylanan</span>
              <span className="stat-value">{stats.approvedCount || 0}</span>
              <span className="stat-trend success">
                <i className="fas fa-arrow-up me-1"></i>
                Bu hafta: {stats.thisWeekCount || 0}
              </span>
            </div>
          </div>
        </div>

        {/* Toplam Fark Tutarı */}
        <div className="col-6 col-lg-3">
          <div className="stat-card amount">
            <div className="stat-icon">
              <i className="fas fa-lira-sign"></i>
            </div>
            <div className="stat-content">
              <span className="stat-label">Toplam Fark</span>
              <span className="stat-value">
                {(stats.totalDifferenceAmount || 0).toFixed(2)} ₺
              </span>
              <span className="stat-trend">
                <i className="fas fa-percent me-1"></i>
                Ort: %{(stats.averageDifferencePercent || 0).toFixed(1)}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Alt İstatistikler */}
      <div className="sub-stats px-3 mt-3">
        <div className="row g-2">
          <div className="col-4 col-md-2">
            <div className="sub-stat-item">
              <span className="sub-stat-value text-danger">
                {stats.rejectedCount || 0}
              </span>
              <span className="sub-stat-label">Reddedilen</span>
            </div>
          </div>
          <div className="col-4 col-md-2">
            <div className="sub-stat-item">
              <span className="sub-stat-value text-info">
                {stats.todayCount || 0}
              </span>
              <span className="sub-stat-label">Bugün</span>
            </div>
          </div>
          <div className="col-4 col-md-2">
            <div className="sub-stat-item">
              <span className="sub-stat-value text-primary">
                {stats.thisWeekCount || 0}
              </span>
              <span className="sub-stat-label">Bu Hafta</span>
            </div>
          </div>
          <div className="col-6 col-md-3">
            <div className="sub-stat-item">
              <span className="sub-stat-value text-success">
                +{(stats.totalExtraPayment || 0).toFixed(2)} ₺
              </span>
              <span className="sub-stat-label">Ek Tahsilat</span>
            </div>
          </div>
          <div className="col-6 col-md-3">
            <div className="sub-stat-item">
              <span className="sub-stat-value text-warning">
                -{(stats.totalRefund || 0).toFixed(2)} ₺
              </span>
              <span className="sub-stat-label">Toplam İade</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

WeightStatsDashboard.propTypes = {
  statistics: PropTypes.shape({
    totalAdjustments: PropTypes.number,
    pendingCount: PropTypes.number,
    approvedCount: PropTypes.number,
    rejectedCount: PropTypes.number,
    totalDifferenceAmount: PropTypes.number,
    averageDifferencePercent: PropTypes.number,
    todayCount: PropTypes.number,
    thisWeekCount: PropTypes.number,
    thisMonthCount: PropTypes.number,
    totalExtraPayment: PropTypes.number,
    totalRefund: PropTypes.number,
  }),
  loading: PropTypes.bool,
};
