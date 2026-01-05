import React, { useState, useEffect } from "react";
import "./WeightReportsPanel.css";

/**
 * Admin Ağırlık Raporları Paneli
 * Tartı cihazından gelen fazla ağırlık bildirimlerini yönetir
 */
const WeightReportsPanel = () => {
  const [reports, setReports] = useState([]);
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(false);
  const [filter, setFilter] = useState("pending");
  const [processingId, setProcessingId] = useState(null);

  const loadDemoData = React.useCallback(() => {
    const demoStats = {
      pendingCount: 3,
      approvedCount: 12,
      chargedCount: 8,
      totalChargedAmount: 425.5,
    };

    const demoReports = [
      {
        id: 1,
        orderId: 1001,
        externalReportId: "SCALE_1001_001",
        expectedWeightGrams: 2000,
        reportedWeightGrams: 2150,
        overageGrams: 150,
        overageAmount: 75.0,
        status: "Pending",
        receivedAt: new Date(Date.now() - 3600000).toISOString(),
        notes: "Domates ve elma siparişi",
      },
      {
        id: 2,
        orderId: 1002,
        externalReportId: "SCALE_1002_001",
        expectedWeightGrams: 1500,
        reportedWeightGrams: 1680,
        overageGrams: 180,
        overageAmount: 90.0,
        status: "Pending",
        receivedAt: new Date(Date.now() - 7200000).toISOString(),
        notes: "Sebze paketi",
      },
      {
        id: 3,
        orderId: 1003,
        externalReportId: "SCALE_1003_001",
        expectedWeightGrams: 3000,
        reportedWeightGrams: 3040,
        overageGrams: 40,
        overageAmount: 20.0,
        status: "AutoApproved",
        receivedAt: new Date(Date.now() - 10800000).toISOString(),
        approvedAt: new Date(Date.now() - 10800000).toISOString(),
      },
    ];

    setStats(demoStats);
    setReports(
      filter === "all"
        ? demoReports
        : demoReports.filter(
            (r) => r.status.toLowerCase() === filter.toLowerCase()
          )
    );
    setLoading(false);
  }, [filter]);

  useEffect(() => {
    loadDemoData();
    const interval = setInterval(loadDemoData, 30000);
    return () => clearInterval(interval);
  }, [loadDemoData]);

  const handleApprove = async (reportId) => {
    if (!window.confirm("Bu raporu onaylamak istediğinize emin misiniz?"))
      return;

    setProcessingId(reportId);
    // Demo: Raporu onaylanmış olarak işaretle
    setTimeout(() => {
      setReports(
        reports.map((r) =>
          r.id === reportId
            ? { ...r, status: "Approved", approvedAt: new Date().toISOString() }
            : r
        )
      );
      setProcessingId(null);
      alert("Rapor onaylandı. Kurye bilgilendirildi.");
    }, 1000);
  };

  const handleReject = async (reportId) => {
    const reason = prompt("Red nedeni girin:");
    if (!reason || reason.trim() === "") return;

    setProcessingId(reportId);
    // Demo: Raporu reddedilmiş olarak işaretle
    setTimeout(() => {
      setReports(
        reports.map((r) =>
          r.id === reportId
            ? {
                ...r,
                status: "Rejected",
                rejectionReason: reason,
                rejectedAt: new Date().toISOString(),
              }
            : r
        )
      );
      setProcessingId(null);
      alert("Rapor reddedildi.");
    }, 1000);
  };

  const getStatusBadge = (status) => {
    const badges = {
      Pending: { label: "Bekliyor", class: "status-pending" },
      Approved: { label: "Onaylandı", class: "status-approved" },
      Rejected: { label: "Reddedildi", class: "status-rejected" },
      Charged: { label: "Tahsil Edildi", class: "status-charged" },
      Failed: { label: "Başarısız", class: "status-failed" },
      AutoApproved: { label: "Oto-Onay", class: "status-auto" },
    };

    const badge = badges[status] || { label: status, class: "status-unknown" };
    return <span className={`status-badge ${badge.class}`}>{badge.label}</span>;
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleString("tr-TR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  if (loading) {
    return <div className="weight-reports-loading">Yükleniyor...</div>;
  }

  return (
    <div className="weight-reports-panel">
      <div className="panel-header">
        <h2>
          Ağırlık Raporları
          {stats && stats.pendingCount > 0 && (
            <span className="notification-badge">{stats.pendingCount}</span>
          )}
        </h2>

        {/* İstatistikler */}
        {stats && (
          <div className="stats-cards">
            <div className="stat-card">
              <div className="stat-value">{stats.pendingCount}</div>
              <div className="stat-label">Bekleyen</div>
            </div>
            <div className="stat-card">
              <div className="stat-value">{stats.approvedCount}</div>
              <div className="stat-label">Onaylanan</div>
            </div>
            <div className="stat-card">
              <div className="stat-value">{stats.chargedCount}</div>
              <div className="stat-label">Tahsil Edilen</div>
            </div>
            <div className="stat-card highlight">
              <div className="stat-value">
                {stats.totalChargedAmount?.toFixed(2)} ₺
              </div>
              <div className="stat-label">Toplam Tahsilat</div>
            </div>
          </div>
        )}
      </div>

      {/* Filtre */}
      <div className="filter-tabs">
        <button
          className={filter === "all" ? "active" : ""}
          onClick={() => setFilter("all")}
        >
          Tümü
        </button>
        <button
          className={filter === "pending" ? "active" : ""}
          onClick={() => setFilter("pending")}
        >
          Bekleyen {stats?.pendingCount > 0 && `(${stats.pendingCount})`}
        </button>
        <button
          className={filter === "approved" ? "active" : ""}
          onClick={() => setFilter("approved")}
        >
          Onaylanan
        </button>
        <button
          className={filter === "rejected" ? "active" : ""}
          onClick={() => setFilter("rejected")}
        >
          Reddedilen
        </button>
      </div>

      {/* Rapor Listesi */}
      <div className="reports-list">
        {reports.length === 0 ? (
          <div className="empty-state">
            <p>Gösterilecek rapor yok</p>
          </div>
        ) : (
          reports.map((report) => (
            <div
              key={report.id}
              className={`report-card ${report.status.toLowerCase()}`}
            >
              <div className="report-header">
                <div className="report-id">
                  <strong>Rapor #{report.id}</strong>
                  <span className="order-link">Sipariş #{report.orderId}</span>
                </div>
                {getStatusBadge(report.status)}
              </div>

              <div className="report-body">
                <div className="weight-comparison">
                  <div className="weight-item">
                    <span className="label">Beklenen Ağırlık:</span>
                    <span className="value">{report.expectedWeightGrams}g</span>
                  </div>
                  <div className="weight-item">
                    <span className="label">Gelen Ağırlık:</span>
                    <span className="value">{report.reportedWeightGrams}g</span>
                  </div>
                  <div
                    className={`weight-item overage ${
                      report.overageGrams > 0 ? "positive" : ""
                    }`}
                  >
                    <span className="label">Fark:</span>
                    <span className="value">
                      {report.overageGrams > 0 ? "+" : ""}
                      {report.overageGrams}g
                    </span>
                  </div>
                </div>

                {report.overageAmount > 0 && (
                  <div className="overage-amount">
                    <span className="label">Ek Ücret:</span>
                    <span className="amount">
                      {report.overageAmount.toFixed(2)} ₺
                    </span>
                  </div>
                )}

                <div className="report-meta">
                  <small>Alındı: {formatDate(report.receivedAt)}</small>
                  {report.approvedAt && (
                    <small>Onaylandı: {formatDate(report.approvedAt)}</small>
                  )}
                  {report.chargedAt && (
                    <small>Tahsil: {formatDate(report.chargedAt)}</small>
                  )}
                </div>

                {report.rejectionReason && (
                  <div className="rejection-reason">
                    <strong>Red Nedeni:</strong> {report.rejectionReason}
                  </div>
                )}

                {report.notes && (
                  <div className="report-notes">
                    <strong>Notlar:</strong> {report.notes}
                  </div>
                )}
              </div>

              {/* Aksiyon Butonları */}
              {report.status === "Pending" && (
                <div className="report-actions">
                  <button
                    className="btn btn-approve"
                    onClick={() => handleApprove(report.id)}
                    disabled={processingId === report.id}
                  >
                    {processingId === report.id
                      ? "İşleniyor..."
                      : "✓ Onayla & Kuryeye Bildir"}
                  </button>
                  <button
                    className="btn btn-reject"
                    onClick={() => handleReject(report.id)}
                    disabled={processingId === report.id}
                  >
                    ✗ Reddet
                  </button>
                </div>
              )}

              {report.status === "Approved" && (
                <div className="info-message">
                  ℹ️ Kurye teslim ettiğinde otomatik olarak ücret tahsil
                  edilecektir.
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default WeightReportsPanel;
