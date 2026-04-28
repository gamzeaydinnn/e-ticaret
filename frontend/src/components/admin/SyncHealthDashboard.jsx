import React, { useState, useEffect, useCallback } from "react";
import { MicroService } from "../../services/microService";

// ═══════════════════════════════════════════════════════════════════════════════
// SyncHealthDashboard — Mikro↔EC Sync Sağlık & Monitoring Paneli
//
// 4 bölüm:
// 1. Genel Sağlık Özeti (overallStatus, kanallar)
// 2. Metrik Grafikleri (saatlik trend — bar chart)
// 3. Aktif Uyarılar
// 4. Ürün Bilgi Sync Operasyonları (kategori, resim, info sync)
// ═══════════════════════════════════════════════════════════════════════════════

const STATUS_CONFIG = {
  Healthy: { color: "success", icon: "fa-check-circle", label: "Sağlıklı" },
  Degraded: {
    color: "warning",
    icon: "fa-exclamation-triangle",
    label: "Bozulmuş",
  },
  Unhealthy: { color: "danger", icon: "fa-times-circle", label: "Sağlıksız" },
  Unknown: {
    color: "secondary",
    icon: "fa-question-circle",
    label: "Bilinmiyor",
  },
};

const SEVERITY_CONFIG = {
  Critical: { color: "danger", icon: "fa-fire" },
  Warning: { color: "warning", icon: "fa-exclamation-triangle" },
  Info: { color: "info", icon: "fa-info-circle" },
};

const formatDuration = (ms) => {
  if (!ms || ms <= 0) return "-";
  if (ms < 1000) return `${Math.round(ms)}ms`;
  const sec = Math.floor(ms / 1000);
  if (sec < 60) return `${sec}sn`;
  return `${Math.floor(sec / 60)}dk ${sec % 60}sn`;
};

const formatTimeAgo = (isoStr) => {
  if (!isoStr) return "-";
  const diffMs = Date.now() - new Date(isoStr).getTime();
  const mins = Math.floor(diffMs / 60000);
  if (mins < 1) return "Az önce";
  if (mins < 60) return `${mins} dk önce`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours} saat önce`;
  return `${Math.floor(hours / 24)} gün önce`;
};

export default function SyncHealthDashboard() {
  // ─── State ───
  const [health, setHealth] = useState(null);
  const [metrics, setMetrics] = useState(null);
  const [alerts, setAlerts] = useState([]);
  const [imageStatus, setImageStatus] = useState(null);
  const [unmappedGroups, setUnmappedGroups] = useState([]);

  const [loading, setLoading] = useState(true);
  const [opLoading, setOpLoading] = useState(""); // hangi operasyon yükleniyor
  const [opResult, setOpResult] = useState(null); // son operasyon sonucu
  const [metricHours, setMetricHours] = useState(24);

  // ─── Veri Yükleme ───
  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      const [healthRes, metricsRes, alertsRes, imageRes, unmappedRes] =
        await Promise.all([
          MicroService.getSyncHealth(),
          MicroService.getSyncMetrics(metricHours),
          MicroService.getSyncAlerts(),
          MicroService.getImageStatus(),
          MicroService.getUnmappedGroups(),
        ]);
      setHealth(healthRes);
      setMetrics(metricsRes);
      setAlerts(alertsRes?.alerts || []);
      setImageStatus(imageRes);
      setUnmappedGroups(unmappedRes?.unmappedGroups || []);
    } catch (err) {
      console.error("Sync sağlık verileri yüklenemedi:", err);
    } finally {
      setLoading(false);
    }
  }, [metricHours]);

  useEffect(() => {
    loadAll();
    // 60sn'de bir otomatik yenile
    const iv = setInterval(loadAll, 60000);
    return () => clearInterval(iv);
  }, [loadAll]);

  // ─── Operasyon Tetikleyiciler ───
  const handleOp = async (opName, fn) => {
    setOpLoading(opName);
    setOpResult(null);
    try {
      const res = await fn();
      setOpResult({
        op: opName,
        success: res?.success !== false,
        message: res?.message || "Tamamlandı",
        data: res,
      });
      // Veriyi yenile
      loadAll();
    } catch (err) {
      setOpResult({ op: opName, success: false, message: err.message });
    } finally {
      setOpLoading("");
    }
  };

  const statusCfg =
    STATUS_CONFIG[health?.overallStatus] || STATUS_CONFIG.Unknown;

  // ─── Basit bar chart (CSS only — harici kütüphane gerekmez) ───
  const maxOps =
    metrics?.hourlyBreakdown?.reduce(
      (m, h) => Math.max(m, h.totalOps || 0),
      1,
    ) || 1;

  if (loading && !health) {
    return (
      <div className="text-center py-5">
        <div className="spinner-border text-primary" role="status" />
        <p className="mt-2 text-muted">Sync sağlık verileri yükleniyor...</p>
      </div>
    );
  }

  return (
    <div>
      {/* ═══════════ 1. GENEL SAĞLIK ÖZETİ ═══════════ */}
      <div className="row g-3 mb-4">
        {/* Sol: Genel durum kartı */}
        <div className="col-md-4">
          <div className={`card border-${statusCfg.color} h-100`}>
            <div className="card-body text-center">
              <i
                className={`fas ${statusCfg.icon} fa-3x text-${statusCfg.color} mb-2`}
              ></i>
              <h4 className={`text-${statusCfg.color}`}>{statusCfg.label}</h4>
              <small className="text-muted">Genel Sync Durumu</small>
              <hr />
              <div className="row text-center">
                <div className="col-4">
                  <span className="fw-bold text-success">
                    {health?.healthyChannels ?? 0}
                  </span>
                  <br />
                  <small className="text-muted">Sağlıklı</small>
                </div>
                <div className="col-4">
                  <span className="fw-bold text-warning">
                    {health?.degradedChannels ?? 0}
                  </span>
                  <br />
                  <small className="text-muted">Bozulmuş</small>
                </div>
                <div className="col-4">
                  <span className="fw-bold text-danger">
                    {health?.unhealthyChannels ?? 0}
                  </span>
                  <br />
                  <small className="text-muted">Sağlıksız</small>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Orta: Metrik özeti */}
        <div className="col-md-4">
          <div className="card h-100">
            <div className="card-body">
              <h6 className="card-title">
                <i className="fas fa-chart-bar me-1 text-primary"></i>
                Son {metricHours} Saat
              </h6>
              <div className="row text-center mt-3">
                <div className="col-6 mb-2">
                  <span className="fs-4 fw-bold">
                    {metrics?.totalOperations ?? 0}
                  </span>
                  <br />
                  <small className="text-muted">Toplam İşlem</small>
                </div>
                <div className="col-6 mb-2">
                  <span
                    className={`fs-4 fw-bold ${(metrics?.successRate ?? 100) >= 90 ? "text-success" : "text-danger"}`}
                  >
                    %{(metrics?.successRate ?? 0).toFixed(1)}
                  </span>
                  <br />
                  <small className="text-muted">Başarı Oranı</small>
                </div>
                <div className="col-6">
                  <span className="fs-5 fw-bold text-danger">
                    {metrics?.failedOperations ?? 0}
                  </span>
                  <br />
                  <small className="text-muted">Başarısız</small>
                </div>
                <div className="col-6">
                  <span className="fs-5 fw-bold">
                    {formatDuration(metrics?.avgDurationMs)}
                  </span>
                  <br />
                  <small className="text-muted">Ort. Süre</small>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Sağ: Uyarı + Resim durumu */}
        <div className="col-md-4">
          <div className="card h-100">
            <div className="card-body">
              <h6 className="card-title">
                <i className="fas fa-bell me-1 text-warning"></i>
                Uyarılar & Resim Durumu
              </h6>
              <div className="d-flex justify-content-between align-items-center mt-3 mb-2">
                <span>Aktif Uyarı</span>
                <span
                  className={`badge bg-${alerts.length > 0 ? "warning text-dark" : "success"}`}
                >
                  {alerts.length}
                </span>
              </div>
              <div className="d-flex justify-content-between align-items-center mb-2">
                <span>Eşleştirilmemiş Grup</span>
                <span
                  className={`badge bg-${unmappedGroups.length > 0 ? "info" : "success"}`}
                >
                  {unmappedGroups.length}
                </span>
              </div>
              {imageStatus && (
                <>
                  <hr className="my-2" />
                  <div className="d-flex justify-content-between align-items-center mb-1">
                    <span>Resim Kapsamı</span>
                    <span
                      className={`badge bg-${(imageStatus.coveragePercent ?? 0) >= 90 ? "success" : "warning text-dark"}`}
                    >
                      %{(imageStatus.coveragePercent ?? 0).toFixed(1)}
                    </span>
                  </div>
                  <div className="progress" style={{ height: "6px" }}>
                    <div
                      className={`progress-bar bg-${(imageStatus.coveragePercent ?? 0) >= 90 ? "success" : "warning"}`}
                      style={{ width: `${imageStatus.coveragePercent ?? 0}%` }}
                    />
                  </div>
                  <small className="text-muted">
                    {imageStatus.productsWithImages ?? 0}/
                    {imageStatus.totalProducts ?? 0} ürünün görseli var
                  </small>
                </>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* ═══════════ 2. KANAL DETAYLARI ═══════════ */}
      {health?.channels?.length > 0 && (
        <div className="card mb-4">
          <div className="card-header d-flex justify-content-between align-items-center">
            <h6 className="mb-0">
              <i className="fas fa-exchange-alt me-1"></i> Sync Kanalları
            </h6>
            <button
              className="btn btn-sm btn-outline-primary"
              onClick={loadAll}
              disabled={loading}
            >
              <i
                className={`fas fa-sync-alt me-1 ${loading ? "fa-spin" : ""}`}
              ></i>{" "}
              Yenile
            </button>
          </div>
          <div className="table-responsive">
            <table className="table table-sm table-hover mb-0">
              <thead className="table-light">
                <tr>
                  <th>Kanal</th>
                  <th>Yön</th>
                  <th>Durum</th>
                  <th>Son Sync</th>
                  <th>Son Başarı</th>
                  <th className="text-end">Ard. Hata</th>
                  <th className="text-end">Son Süre</th>
                </tr>
              </thead>
              <tbody>
                {health.channels.map((ch, i) => {
                  const chStatus =
                    STATUS_CONFIG[ch.status] || STATUS_CONFIG.Unknown;
                  return (
                    <tr key={i}>
                      <td className="fw-semibold">{ch.syncType}</td>
                      <td>
                        <span
                          className={`badge bg-${ch.direction === "FromERP" ? "info" : "secondary"}`}
                        >
                          {ch.direction === "FromERP" ? "ERP→EC" : "EC→ERP"}
                        </span>
                      </td>
                      <td>
                        <i
                          className={`fas ${chStatus.icon} text-${chStatus.color} me-1`}
                        ></i>
                        {chStatus.label}
                      </td>
                      <td>{formatTimeAgo(ch.lastSyncTime)}</td>
                      <td>
                        {ch.lastSyncSuccess ? (
                          <span className="text-success">
                            <i className="fas fa-check"></i>
                          </span>
                        ) : (
                          <span className="text-danger">
                            <i className="fas fa-times"></i>
                          </span>
                        )}
                      </td>
                      <td className="text-end">
                        <span
                          className={
                            ch.consecutiveFailures > 0
                              ? "text-danger fw-bold"
                              : ""
                          }
                        >
                          {ch.consecutiveFailures ?? 0}
                        </span>
                      </td>
                      <td className="text-end">
                        {formatDuration(ch.lastSyncDurationMs)}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* ═══════════ 3. SAATLİK TREND GRAFİĞİ (CSS bar chart) ═══════════ */}
      {metrics?.hourlyBreakdown?.length > 0 && (
        <div className="card mb-4">
          <div className="card-header d-flex justify-content-between align-items-center">
            <h6 className="mb-0">
              <i className="fas fa-chart-line me-1"></i> Saatlik Sync Trendi
            </h6>
            <div className="btn-group btn-group-sm">
              {[6, 12, 24, 48].map((h) => (
                <button
                  key={h}
                  className={`btn btn-outline-primary ${metricHours === h ? "active" : ""}`}
                  onClick={() => setMetricHours(h)}
                >
                  {h}s
                </button>
              ))}
            </div>
          </div>
          <div className="card-body">
            <div
              className="d-flex align-items-end gap-1"
              style={{ height: 140, overflowX: "auto" }}
            >
              {metrics.hourlyBreakdown.map((h, i) => {
                const successPct =
                  maxOps > 0 ? ((h.successOps || 0) / maxOps) * 100 : 0;
                const failPct =
                  maxOps > 0 ? ((h.failedOps || 0) / maxOps) * 100 : 0;
                const hourLabel = h.hour
                  ? new Date(h.hour).toLocaleTimeString("tr-TR", {
                      hour: "2-digit",
                      minute: "2-digit",
                    })
                  : "";
                return (
                  <div
                    key={i}
                    className="d-flex flex-column align-items-center"
                    style={{ flex: "1 1 0", minWidth: 20 }}
                    title={`${hourLabel}\nBaşarılı: ${h.successOps || 0}\nBaşarısız: ${h.failedOps || 0}\nSüre: ${formatDuration(h.avgDurationMs)}`}
                  >
                    <div
                      className="d-flex flex-column justify-content-end"
                      style={{ height: 110, width: "100%" }}
                    >
                      {(h.failedOps || 0) > 0 && (
                        <div
                          className="bg-danger rounded-top"
                          style={{
                            height: `${failPct}%`,
                            minHeight: (h.failedOps || 0) > 0 ? 3 : 0,
                          }}
                        />
                      )}
                      <div
                        className="bg-success"
                        style={{
                          height: `${successPct}%`,
                          minHeight: (h.successOps || 0) > 0 ? 3 : 0,
                          borderRadius:
                            (h.failedOps || 0) > 0 ? "0" : "4px 4px 0 0",
                        }}
                      />
                    </div>
                    <small
                      className="text-muted"
                      style={{ fontSize: "0.6rem", whiteSpace: "nowrap" }}
                    >
                      {hourLabel}
                    </small>
                  </div>
                );
              })}
            </div>
            <div className="d-flex gap-3 mt-2 justify-content-center">
              <small>
                <span className="badge bg-success">&nbsp;</span> Başarılı
              </small>
              <small>
                <span className="badge bg-danger">&nbsp;</span> Başarısız
              </small>
            </div>
          </div>
        </div>
      )}

      {/* ═══════════ 4. AKTİF UYARILAR ═══════════ */}
      {alerts.length > 0 && (
        <div className="card mb-4">
          <div className="card-header">
            <h6 className="mb-0">
              <i className="fas fa-exclamation-triangle me-1 text-warning"></i>
              Aktif Uyarılar ({alerts.length})
            </h6>
          </div>
          <div className="list-group list-group-flush">
            {alerts.map((alert, i) => {
              const sev =
                SEVERITY_CONFIG[alert.severity] || SEVERITY_CONFIG.Info;
              return (
                <div key={i} className="list-group-item">
                  <div className="d-flex align-items-center gap-2">
                    <i className={`fas ${sev.icon} text-${sev.color}`}></i>
                    <span className={`badge bg-${sev.color}`}>
                      {alert.severity}
                    </span>
                    <strong>{alert.channel}</strong>
                    <span className="text-muted">—</span>
                    <span>{alert.message}</span>
                    <span className="ms-auto text-muted small">
                      {formatTimeAgo(alert.detectedAt)}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* ═══════════ 5. ÜRÜN BİLGİ SYNC OPERASYİONLARI ═══════════ */}
      <div className="card mb-4">
        <div className="card-header">
          <h6 className="mb-0">
            <i className="fas fa-tools me-1"></i> Ürün Bilgi Sync Operasyonları
          </h6>
        </div>
        <div className="card-body">
          {/* Son operasyon sonucu */}
          {opResult && (
            <div
              className={`alert alert-${opResult.success ? "success" : "danger"} alert-dismissible fade show py-2`}
            >
              <strong>{opResult.op}:</strong> {opResult.message}
              {opResult.data?.totalProcessed !== undefined && (
                <span className="ms-2">
                  ({opResult.data.totalProcessed} işlendi
                  {opResult.data.namesUpdated > 0 &&
                    `, ${opResult.data.namesUpdated} ad güncellendi`}
                  {opResult.data.categoriesUpdated > 0 &&
                    `, ${opResult.data.categoriesUpdated} kategori güncellendi`}
                  {opResult.data.weightInfoUpdated > 0 &&
                    `, ${opResult.data.weightInfoUpdated} ağırlık güncellendi`}
                  {opResult.data.statusUpdated > 0 &&
                    `, ${opResult.data.statusUpdated} durum güncellendi`}
                  )
                </span>
              )}
              <button
                type="button"
                className="btn-close"
                onClick={() => setOpResult(null)}
              ></button>
            </div>
          )}

          <div className="row g-3">
            {/* Ürün Bilgi Sync */}
            <div className="col-md-4">
              <div className="card border-primary h-100">
                <div className="card-body text-center">
                  <i className="fas fa-sync fa-2x text-primary mb-2"></i>
                  <h6>Ürün Bilgi Sync</h6>
                  <p className="small text-muted mb-3">
                    Cache'teki tüm ürünlerin ad, slug, birim, kategori ve durum
                    bilgilerini Product tablosuna senkronize eder.
                  </p>
                  <button
                    className="btn btn-primary btn-sm"
                    disabled={!!opLoading}
                    onClick={() =>
                      handleOp("Ürün Bilgi Sync", MicroService.syncProductInfo)
                    }
                  >
                    {opLoading === "Ürün Bilgi Sync" ? (
                      <span className="spinner-border spinner-border-sm me-1" />
                    ) : (
                      <i className="fas fa-play me-1"></i>
                    )}
                    Başlat
                  </button>
                </div>
              </div>
            </div>

            {/* Kategori Sync */}
            <div className="col-md-4">
              <div className="card border-success h-100">
                <div className="card-body text-center">
                  <i className="fas fa-sitemap fa-2x text-success mb-2"></i>
                  <h6>Kategori Eşleştirme</h6>
                  <p className="small text-muted mb-3">
                    MikroCategoryMapping tablosundan GrupKod → CategoryId
                    eşleştirmesini ürünlere uygular.
                  </p>
                  <button
                    className="btn btn-success btn-sm"
                    disabled={!!opLoading}
                    onClick={() =>
                      handleOp("Kategori Sync", MicroService.syncCategories)
                    }
                  >
                    {opLoading === "Kategori Sync" ? (
                      <span className="spinner-border spinner-border-sm me-1" />
                    ) : (
                      <i className="fas fa-play me-1"></i>
                    )}
                    Başlat
                  </button>
                </div>
              </div>
            </div>

            {/* Resim Durumu */}
            <div className="col-md-4">
              <div className="card border-info h-100">
                <div className="card-body text-center">
                  <i className="fas fa-image fa-2x text-info mb-2"></i>
                  <h6>Resim Durumu</h6>
                  {imageStatus ? (
                    <>
                      <p className="small text-muted mb-1">
                        {imageStatus.productsWithImages ?? 0}/
                        {imageStatus.totalProducts ?? 0} ürünün görseli mevcut
                      </p>
                      <div className="progress mb-2" style={{ height: "8px" }}>
                        <div
                          className={`progress-bar bg-${(imageStatus.coveragePercent ?? 0) >= 90 ? "success" : "warning"}`}
                          style={{
                            width: `${imageStatus.coveragePercent ?? 0}%`,
                          }}
                        />
                      </div>
                      <small className="text-muted">
                        Görselsiz: {imageStatus.productsWithoutImages ?? 0}
                      </small>
                    </>
                  ) : (
                    <p className="small text-muted">Yükleniyor...</p>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Eşleştirilmemiş GrupKod'lar */}
          {unmappedGroups.length > 0 && (
            <div className="mt-3">
              <h6 className="text-muted">
                <i className="fas fa-unlink me-1"></i> Eşleştirilmemiş
                GrupKodlar
              </h6>
              <div className="d-flex flex-wrap gap-2">
                {unmappedGroups.map((g, i) => (
                  <span key={i} className="badge bg-secondary">
                    {g.grupKod}{" "}
                    <span className="badge bg-dark">{g.productCount}</span>
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
