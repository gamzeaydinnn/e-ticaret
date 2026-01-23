// ==========================================================================
// WeightCourierPerformance.jsx - Kurye Performans Takibi Komponenti
// ==========================================================================
// Kuryelerin ağırlık tartım performansını gösteren rapor komponenti.
// Doğruluk oranları, ortalama farklar ve detaylı istatistikler içerir.
// ==========================================================================

import React, { useState, useEffect, useCallback } from "react";
import { WeightAdjustmentService } from "../../../services/weightAdjustmentService";

/**
 * WeightCourierPerformance - Kurye Performans Raporu
 */
export default function WeightCourierPerformance() {
  const [couriers, setCouriers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedCourier, setSelectedCourier] = useState(null);
  const [dateRange, setDateRange] = useState("week"); // today, week, month, all

  /**
   * Kurye listesini yükle
   */
  const loadCouriers = useCallback(async () => {
    setLoading(true);
    try {
      // Demo veri kullan (gerçek API'den gelecek)
      setCouriers([
        {
          id: 1,
          name: "Mehmet Demir",
          totalWeighings: 45,
          accurateWeighings: 38,
          accuracyPercent: 84.4,
          averageDifferencePercent: 8.2,
          totalDifferenceAmount: 125.5,
          lastActiveAt: new Date(Date.now() - 3600000).toISOString(),
        },
        {
          id: 2,
          name: "Ali Veli",
          totalWeighings: 32,
          accurateWeighings: 29,
          accuracyPercent: 90.6,
          averageDifferencePercent: 5.1,
          totalDifferenceAmount: 78.25,
          lastActiveAt: new Date(Date.now() - 7200000).toISOString(),
        },
        {
          id: 3,
          name: "Ahmet Yılmaz",
          totalWeighings: 28,
          accurateWeighings: 22,
          accuracyPercent: 78.6,
          averageDifferencePercent: 12.3,
          totalDifferenceAmount: 156.0,
          lastActiveAt: new Date(Date.now() - 10800000).toISOString(),
        },
      ]);
    } catch (err) {
      console.error("Kurye verileri yüklenemedi:", err);
    } finally {
      setLoading(false);
    }
  }, [dateRange]);

  useEffect(() => {
    loadCouriers();
  }, [loadCouriers]);

  /**
   * Doğruluk yüzdesine göre renk
   */
  const getAccuracyColor = (percent) => {
    if (percent >= 90) return "success";
    if (percent >= 80) return "warning";
    return "danger";
  };

  /**
   * Tarih formatla
   */
  const formatDate = (dateString) => {
    if (!dateString) return "-";
    const date = new Date(dateString);
    const now = new Date();
    const diffHours = Math.floor((now - date) / 3600000);

    if (diffHours < 1) return "Az önce";
    if (diffHours < 24) return `${diffHours} saat önce`;
    return date.toLocaleDateString("tr-TR");
  };

  // Loading
  if (loading) {
    return (
      <div className="courier-performance-loading">
        <div className="text-center py-5">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="text-muted mt-3">Kurye performansları yükleniyor...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="courier-performance">
      {/* Header */}
      <div className="performance-header">
        <h6 className="section-title">
          <i className="fas fa-motorcycle me-2"></i>
          Kurye Tartım Performansı
        </h6>
        <div className="date-filter">
          <select
            className="form-select form-select-sm"
            value={dateRange}
            onChange={(e) => setDateRange(e.target.value)}
          >
            <option value="today">Bugün</option>
            <option value="week">Bu Hafta</option>
            <option value="month">Bu Ay</option>
            <option value="all">Tümü</option>
          </select>
        </div>
      </div>

      {/* Kurye Kartları */}
      <div className="courier-cards">
        {couriers.map((courier) => (
          <div
            key={courier.id}
            className={`courier-card ${selectedCourier === courier.id ? "selected" : ""}`}
            onClick={() =>
              setSelectedCourier(
                selectedCourier === courier.id ? null : courier.id,
              )
            }
          >
            {/* Avatar ve İsim */}
            <div className="courier-header">
              <div className="courier-avatar">
                <i className="fas fa-user"></i>
              </div>
              <div className="courier-info">
                <span className="courier-name">{courier.name}</span>
                <span className="courier-last-active">
                  <i className="fas fa-clock me-1"></i>
                  {formatDate(courier.lastActiveAt)}
                </span>
              </div>
              <div
                className={`accuracy-badge bg-${getAccuracyColor(courier.accuracyPercent)}`}
              >
                %{courier.accuracyPercent.toFixed(0)}
              </div>
            </div>

            {/* İstatistikler */}
            <div className="courier-stats">
              <div className="stat-item">
                <span className="stat-value">{courier.totalWeighings}</span>
                <span className="stat-label">Toplam Tartım</span>
              </div>
              <div className="stat-item">
                <span className="stat-value text-success">
                  {courier.accurateWeighings}
                </span>
                <span className="stat-label">Doğru</span>
              </div>
              <div className="stat-item">
                <span className="stat-value text-warning">
                  {courier.totalWeighings - courier.accurateWeighings}
                </span>
                <span className="stat-label">Yüksek Fark</span>
              </div>
            </div>

            {/* Detaylar (Seçildiğinde) */}
            {selectedCourier === courier.id && (
              <div className="courier-details">
                <div className="detail-row">
                  <span className="detail-label">Ortalama Fark</span>
                  <span
                    className={`detail-value text-${courier.averageDifferencePercent > 10 ? "warning" : "success"}`}
                  >
                    %{courier.averageDifferencePercent.toFixed(1)}
                  </span>
                </div>
                <div className="detail-row">
                  <span className="detail-label">Toplam Fark Tutarı</span>
                  <span className="detail-value">
                    {courier.totalDifferenceAmount.toFixed(2)} ₺
                  </span>
                </div>
                <div className="progress-wrapper">
                  <div className="progress" style={{ height: "8px" }}>
                    <div
                      className={`progress-bar bg-${getAccuracyColor(courier.accuracyPercent)}`}
                      style={{ width: `${courier.accuracyPercent}%` }}
                    ></div>
                  </div>
                  <small className="text-muted">Doğruluk Oranı</small>
                </div>
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Özet */}
      <div className="performance-summary">
        <div className="summary-card">
          <i className="fas fa-chart-line text-primary"></i>
          <div>
            <span className="summary-value">
              {couriers.length > 0
                ? (
                    couriers.reduce((acc, c) => acc + c.accuracyPercent, 0) /
                    couriers.length
                  ).toFixed(1)
                : 0}
              %
            </span>
            <span className="summary-label">Ortalama Doğruluk</span>
          </div>
        </div>
        <div className="summary-card">
          <i className="fas fa-balance-scale text-warning"></i>
          <div>
            <span className="summary-value">
              {couriers.reduce((acc, c) => acc + c.totalWeighings, 0)}
            </span>
            <span className="summary-label">Toplam Tartım</span>
          </div>
        </div>
        <div className="summary-card">
          <i className="fas fa-lira-sign text-success"></i>
          <div>
            <span className="summary-value">
              {couriers
                .reduce((acc, c) => acc + c.totalDifferenceAmount, 0)
                .toFixed(2)}{" "}
              ₺
            </span>
            <span className="summary-label">Toplam Fark</span>
          </div>
        </div>
      </div>
    </div>
  );
}
