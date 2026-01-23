// ==========================================================================
// WeightReportingPanel.jsx - Ağırlık Raporlama Paneli
// ==========================================================================
// Admin için ağırlık ayarlamalarının detaylı raporlarını ve grafiklerini
// gösteren panel. İstatistikler, trendler ve export özellikleri içerir.
// ==========================================================================

import React, { useState, useEffect, useCallback } from "react";
import { WeightAdjustmentService } from "../../../services/weightAdjustmentService";
import WeightCourierPerformance from "./WeightCourierPerformance";
import "./WeightReportingPanel.css";

/**
 * WeightReportingPanel - Ana raporlama komponenti
 */
export default function WeightReportingPanel({ onExport }) {
  const [loading, setLoading] = useState(true);
  const [reportData, setReportData] = useState(null);
  const [dateRange, setDateRange] = useState("week");
  const [activeTab, setActiveTab] = useState("overview"); // overview, products, couriers

  /**
   * Rapor verilerini yükle
   */
  const loadReportData = useCallback(async () => {
    setLoading(true);
    try {
      // Demo veri (gerçek API'den gelecek)
      setReportData({
        summary: {
          totalAdjustments: 156,
          totalDifferenceAmount: 2450.75,
          averageDifferencePercent: 12.3,
          completionRate: 94.2,
          refundRate: 15.4,
        },
        dailyStats: [
          { date: "2024-01-15", adjustments: 25, difference: 350.5 },
          { date: "2024-01-16", adjustments: 32, difference: 420.25 },
          { date: "2024-01-17", adjustments: 18, difference: 280.0 },
          { date: "2024-01-18", adjustments: 28, difference: 375.0 },
          { date: "2024-01-19", adjustments: 22, difference: 295.5 },
          { date: "2024-01-20", adjustments: 15, difference: 185.25 },
          { date: "2024-01-21", adjustments: 16, difference: 544.25 },
        ],
        topProducts: [
          { name: "Kıyma (Kg)", adjustments: 45, avgDifference: 18.5 },
          { name: "Domates (Kg)", adjustments: 38, avgDifference: 12.2 },
          { name: "Peynir (Gr)", adjustments: 32, avgDifference: 15.8 },
          { name: "Elma (Kg)", adjustments: 28, avgDifference: 10.5 },
          { name: "Sucuk (Gr)", adjustments: 25, avgDifference: 14.3 },
        ],
        statusDistribution: {
          completed: 128,
          pending: 12,
          refundRequired: 8,
          adminReview: 5,
          cancelled: 3,
        },
      });
    } catch (err) {
      console.error("Rapor verileri yüklenemedi:", err);
    } finally {
      setLoading(false);
    }
  }, [dateRange]);

  useEffect(() => {
    loadReportData();
  }, [loadReportData]);

  /**
   * CSV Export
   */
  const handleExport = () => {
    if (onExport) {
      onExport("csv", reportData);
    } else {
      alert("Rapor indirme hazırlanıyor...");
    }
  };

  /**
   * Basit bar grafik render
   */
  const renderSimpleBarChart = (data, maxValue) => {
    return (
      <div className="simple-bar-chart">
        {data.map((item, index) => (
          <div key={index} className="bar-item">
            <div className="bar-label">{item.label}</div>
            <div className="bar-wrapper">
              <div
                className="bar-fill"
                style={{
                  width: `${(item.value / maxValue) * 100}%`,
                  backgroundColor: item.color || "var(--primary-color)",
                }}
              ></div>
              <span className="bar-value">{item.value}</span>
            </div>
          </div>
        ))}
      </div>
    );
  };

  // Loading state
  if (loading) {
    return (
      <div className="reporting-panel-loading">
        <div className="text-center py-5">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="text-muted mt-3">Raporlar hazırlanıyor...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="weight-reporting-panel">
      {/* Header */}
      <div className="reporting-header">
        <div className="header-title">
          <h5>
            <i className="fas fa-chart-bar me-2"></i>
            Ağırlık Ayarlama Raporları
          </h5>
          <p className="text-muted mb-0">
            Detaylı istatistikler ve trend analizi
          </p>
        </div>
        <div className="header-actions">
          <select
            className="form-select form-select-sm me-2"
            value={dateRange}
            onChange={(e) => setDateRange(e.target.value)}
          >
            <option value="today">Bugün</option>
            <option value="week">Bu Hafta</option>
            <option value="month">Bu Ay</option>
            <option value="quarter">Son 3 Ay</option>
            <option value="year">Bu Yıl</option>
          </select>
          <button
            className="btn btn-sm btn-outline-primary"
            onClick={handleExport}
          >
            <i className="fas fa-download me-1"></i>
            Export
          </button>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="reporting-tabs">
        <button
          className={`tab-btn ${activeTab === "overview" ? "active" : ""}`}
          onClick={() => setActiveTab("overview")}
        >
          <i className="fas fa-tachometer-alt me-2"></i>
          Genel Bakış
        </button>
        <button
          className={`tab-btn ${activeTab === "products" ? "active" : ""}`}
          onClick={() => setActiveTab("products")}
        >
          <i className="fas fa-box me-2"></i>
          Ürün Analizi
        </button>
        <button
          className={`tab-btn ${activeTab === "couriers" ? "active" : ""}`}
          onClick={() => setActiveTab("couriers")}
        >
          <i className="fas fa-motorcycle me-2"></i>
          Kurye Performansı
        </button>
      </div>

      {/* Content */}
      <div className="reporting-content">
        {/* Genel Bakış Tab */}
        {activeTab === "overview" && (
          <div className="overview-tab">
            {/* Özet Kartları */}
            <div className="summary-cards">
              <div className="summary-card">
                <div className="card-icon bg-primary-light">
                  <i className="fas fa-scale-balanced"></i>
                </div>
                <div className="card-info">
                  <span className="card-value">
                    {reportData.summary.totalAdjustments}
                  </span>
                  <span className="card-label">Toplam Ayarlama</span>
                </div>
              </div>
              <div className="summary-card">
                <div className="card-icon bg-success-light">
                  <i className="fas fa-lira-sign"></i>
                </div>
                <div className="card-info">
                  <span className="card-value">
                    {reportData.summary.totalDifferenceAmount.toFixed(2)} ₺
                  </span>
                  <span className="card-label">Toplam Fark</span>
                </div>
              </div>
              <div className="summary-card">
                <div className="card-icon bg-warning-light">
                  <i className="fas fa-percent"></i>
                </div>
                <div className="card-info">
                  <span className="card-value">
                    %{reportData.summary.averageDifferencePercent.toFixed(1)}
                  </span>
                  <span className="card-label">Ortalama Fark</span>
                </div>
              </div>
              <div className="summary-card">
                <div className="card-icon bg-info-light">
                  <i className="fas fa-check-circle"></i>
                </div>
                <div className="card-info">
                  <span className="card-value">
                    %{reportData.summary.completionRate.toFixed(1)}
                  </span>
                  <span className="card-label">Tamamlanma</span>
                </div>
              </div>
            </div>

            {/* Günlük Trend */}
            <div className="daily-trend-section">
              <h6 className="section-title">
                <i className="fas fa-chart-line me-2"></i>
                Günlük Trend
              </h6>
              <div className="trend-chart">
                <div className="chart-container">
                  {reportData.dailyStats.map((day, index) => {
                    const maxAdjustments = Math.max(
                      ...reportData.dailyStats.map((d) => d.adjustments),
                    );
                    const height = (day.adjustments / maxAdjustments) * 100;
                    const dayName = new Date(day.date).toLocaleDateString(
                      "tr-TR",
                      { weekday: "short" },
                    );

                    return (
                      <div key={index} className="chart-bar-wrapper">
                        <div className="chart-bar-container">
                          <div
                            className="chart-bar"
                            style={{ height: `${height}%` }}
                            title={`${day.adjustments} ayarlama - ${day.difference.toFixed(2)} ₺`}
                          >
                            <span className="bar-tooltip">
                              {day.adjustments}
                            </span>
                          </div>
                        </div>
                        <span className="chart-label">{dayName}</span>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>

            {/* Durum Dağılımı */}
            <div className="status-distribution-section">
              <h6 className="section-title">
                <i className="fas fa-pie-chart me-2"></i>
                Durum Dağılımı
              </h6>
              <div className="status-grid">
                <div className="status-item completed">
                  <div className="status-bar">
                    <div
                      className="status-fill"
                      style={{
                        width: `${(reportData.statusDistribution.completed / reportData.summary.totalAdjustments) * 100}%`,
                      }}
                    ></div>
                  </div>
                  <div className="status-info">
                    <span className="status-name">
                      <i className="fas fa-check-circle me-1"></i>
                      Tamamlandı
                    </span>
                    <span className="status-count">
                      {reportData.statusDistribution.completed}
                    </span>
                  </div>
                </div>
                <div className="status-item pending">
                  <div className="status-bar">
                    <div
                      className="status-fill"
                      style={{
                        width: `${(reportData.statusDistribution.pending / reportData.summary.totalAdjustments) * 100}%`,
                      }}
                    ></div>
                  </div>
                  <div className="status-info">
                    <span className="status-name">
                      <i className="fas fa-clock me-1"></i>
                      Beklemede
                    </span>
                    <span className="status-count">
                      {reportData.statusDistribution.pending}
                    </span>
                  </div>
                </div>
                <div className="status-item refund">
                  <div className="status-bar">
                    <div
                      className="status-fill"
                      style={{
                        width: `${(reportData.statusDistribution.refundRequired / reportData.summary.totalAdjustments) * 100}%`,
                      }}
                    ></div>
                  </div>
                  <div className="status-info">
                    <span className="status-name">
                      <i className="fas fa-undo me-1"></i>
                      İade Gerekli
                    </span>
                    <span className="status-count">
                      {reportData.statusDistribution.refundRequired}
                    </span>
                  </div>
                </div>
                <div className="status-item admin-review">
                  <div className="status-bar">
                    <div
                      className="status-fill"
                      style={{
                        width: `${(reportData.statusDistribution.adminReview / reportData.summary.totalAdjustments) * 100}%`,
                      }}
                    ></div>
                  </div>
                  <div className="status-info">
                    <span className="status-name">
                      <i className="fas fa-user-shield me-1"></i>
                      Admin İnceleme
                    </span>
                    <span className="status-count">
                      {reportData.statusDistribution.adminReview}
                    </span>
                  </div>
                </div>
                <div className="status-item cancelled">
                  <div className="status-bar">
                    <div
                      className="status-fill"
                      style={{
                        width: `${(reportData.statusDistribution.cancelled / reportData.summary.totalAdjustments) * 100}%`,
                      }}
                    ></div>
                  </div>
                  <div className="status-info">
                    <span className="status-name">
                      <i className="fas fa-times-circle me-1"></i>
                      İptal
                    </span>
                    <span className="status-count">
                      {reportData.statusDistribution.cancelled}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Ürün Analizi Tab */}
        {activeTab === "products" && (
          <div className="products-tab">
            <h6 className="section-title">
              <i className="fas fa-box me-2"></i>
              En Çok Ayarlama Yapılan Ürünler
            </h6>
            <div className="products-list">
              {reportData.topProducts.map((product, index) => (
                <div key={index} className="product-item">
                  <div className="product-rank">#{index + 1}</div>
                  <div className="product-info">
                    <span className="product-name">{product.name}</span>
                    <div className="product-stats">
                      <span className="stat">
                        <i className="fas fa-balance-scale me-1"></i>
                        {product.adjustments} ayarlama
                      </span>
                      <span
                        className={`stat ${product.avgDifference > 15 ? "text-warning" : "text-success"}`}
                      >
                        <i className="fas fa-percent me-1"></i>
                        Ort. fark: %{product.avgDifference.toFixed(1)}
                      </span>
                    </div>
                  </div>
                  <div className="product-bar">
                    <div
                      className="bar-fill"
                      style={{
                        width: `${(product.adjustments / reportData.topProducts[0].adjustments) * 100}%`,
                        backgroundColor:
                          product.avgDifference > 15
                            ? "var(--warning-color)"
                            : "var(--success-color)",
                      }}
                    ></div>
                  </div>
                </div>
              ))}
            </div>

            {/* Ürün Önerileri */}
            <div className="product-recommendations">
              <h6 className="section-title">
                <i className="fas fa-lightbulb me-2"></i>
                Öneriler
              </h6>
              <div className="recommendation-cards">
                <div className="recommendation-card">
                  <i className="fas fa-exclamation-triangle text-warning"></i>
                  <div>
                    <strong>Yüksek Fark Uyarısı</strong>
                    <p>
                      Kıyma (Kg) ürünü ortalama %18.5 fark ile dikkat
                      gerektirebilir. Tedarikçi kalibrasyon kontrolü önerilir.
                    </p>
                  </div>
                </div>
                <div className="recommendation-card">
                  <i className="fas fa-chart-line text-info"></i>
                  <div>
                    <strong>Trend Analizi</strong>
                    <p>
                      Tartımlı ürünlerde son hafta içinde %12 artış gözlemlendi.
                      Talep artışı takip edilmeli.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Kurye Performansı Tab */}
        {activeTab === "couriers" && <WeightCourierPerformance />}
      </div>
    </div>
  );
}
