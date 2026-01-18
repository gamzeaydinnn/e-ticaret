import React, { useEffect, useMemo, useState } from "react";
import { AdminService } from "../../services/adminService";
import "../../styles/adminReports.css";

export default function AdminReports() {
  const [lowStock, setLowStock] = useState({ threshold: 0, products: [] });
  const [loadingLow, setLoadingLow] = useState(true);
  const [from, setFrom] = useState(
    isoDate(new Date(Date.now() - 7 * 86400000))
  );
  const [to, setTo] = useState(isoDate(new Date()));
  const [movements, setMovements] = useState({
    start: null,
    end: null,
    movements: [],
  });
  const [loadingMov, setLoadingMov] = useState(true);
  const [salesPeriod, setSalesPeriod] = useState("daily");
  const [sales, setSales] = useState(null);
  const [loadingSales, setLoadingSales] = useState(true);
  const [erp, setErp] = useState({ groups: [] });
  const [loadingErp, setLoadingErp] = useState(true);
  const [errorMsg, setErrorMsg] = useState("");

  const periodLabel = useMemo(
    () => ({
      daily: "Günlük",
      weekly: "Haftalık",
      monthly: "Aylık",
    }),
    []
  );

  const showError = (message) => {
    setErrorMsg(message);
    setTimeout(() => setErrorMsg(""), 3500);
  };

  useEffect(() => {
    loadLowStock();
    loadMovements();
    loadSales();
    loadErp();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadLowStock() {
    try {
      setLoadingLow(true);
      const data = await AdminService.getLowStockProducts();
      setLowStock(data);
    } catch (e) {
      console.error("Low stock report error:", e);
      setErrorMsg(e.message || "Low stock verisi alınamadı.");
      setLowStock({ threshold: 0, products: [] });
    } finally {
      setLoadingLow(false);
    }
  }

  async function loadMovements() {
    try {
      setLoadingMov(true);
      const data = await AdminService.getInventoryMovements({ from, to });
      setMovements(data);
    } catch (e) {
      console.error("Inventory movements report error:", e);
      setErrorMsg(e.message || "Stok hareketleri alınamadı.");
      setMovements({ start: null, end: null, movements: [] });
    } finally {
      setLoadingMov(false);
    }
  }

  async function loadSales(period = salesPeriod) {
    try {
      setLoadingSales(true);
      const data = await AdminService.getSalesReport(period);
      setSales(data);
    } catch (e) {
      console.error("Sales report error:", e);
      setErrorMsg(e.message || "Satış özeti alınamadı.");
      setSales(null);
    } finally {
      setLoadingSales(false);
    }
  }

  async function loadErp() {
    try {
      setLoadingErp(true);
      const data = await AdminService.getErpSyncStatus({ from, to });
      setErp(data);
    } catch (e) {
      console.error("ERP sync status error:", e);
      setErrorMsg(e.message || "ERP senkron durumu alınamadı.");
      setErp({ groups: [] });
    } finally {
      setLoadingErp(false);
    }
  }

  const handleExportSales = () => {
    if (!sales) {
      showError("Satış özeti için veri bulunamadı.");
      return;
    }
    const topProducts = sales.topProducts?.length
      ? sales.topProducts.map((p) => `#${p.productId} (${p.quantity})`).join(", ")
      : "-";
    const rows = [
      [
        periodLabel[salesPeriod] || salesPeriod,
        sales.ordersCount ?? 0,
        sales.revenue ?? 0,
        sales.itemsSold ?? 0,
        topProducts,
      ],
    ];
    downloadCsv(
      `satis-ozeti-${salesPeriod}-${isoDate(new Date())}.csv`,
      ["Dönem", "Sipariş", "Gelir", "Adet", "En Çok Satanlar"],
      rows
    );
  };

  const handleExportErp = () => {
    if (!erp.groups?.length) {
      showError("ERP senkron verisi bulunamadı.");
      return;
    }
    const rows = erp.groups.map((g) => [
      g.entity || "-",
      g.direction || "-",
      g.lastStatus || "-",
      formatDate(g.lastAttemptAt),
      formatDate(g.lastSuccessAt),
      g.lastMessage || "-",
      g.lastError || "-",
      g.updatedCount ?? "-",
      g.totalAttempts ?? "-",
      g.recentCount ?? "-",
    ]);
    downloadCsv(
      `erp-senkron-${isoDate(new Date())}.csv`,
      [
        "Varlık",
        "Yön",
        "Durum",
        "Son Deneme",
        "Son Başarılı",
        "Mesaj",
        "Hata",
        "Güncellenen",
        "Toplam Deneme",
        "Kayıt",
      ],
      rows
    );
  };

  const handleExportLowStock = () => {
    if (!lowStock.products?.length) {
      showError("Düşük stok verisi bulunamadı.");
      return;
    }
    const rows = lowStock.products.map((p) => [
      p.id ?? "-",
      p.name || "-",
      p.stockQuantity ?? 0,
    ]);
    downloadCsv(
      `dusuk-stok-${isoDate(new Date())}.csv`,
      ["ID", "Ürün", "Stok"],
      rows
    );
  };

  const handleExportMovements = () => {
    if (!movements.movements?.length) {
      showError("Stok hareketi verisi bulunamadı.");
      return;
    }
    const rows = movements.movements.map((m) => [
      formatDate(m.createdAt),
      m.productId ?? "-",
      m.productName || "-",
      m.changeQuantity ?? 0,
      m.changeType || "-",
      m.oldStock ?? "-",
      m.newStock ?? "-",
      m.referenceId ?? "-",
    ]);
    downloadCsv(
      `stok-hareketleri-${isoDate(new Date())}.csv`,
      [
        "Tarih",
        "Ürün ID",
        "Ürün",
        "Miktar",
        "Tür",
        "Eski Stok",
        "Yeni Stok",
        "Referans",
      ],
      rows
    );
  };

  return (
    <div className="admin-reports">
      <div className="reports-header">
        <div>
          <h4 className="reports-title">
            <span className="reports-title__icon">
              <i className="fas fa-chart-bar"></i>
            </span>
            Raporlar
          </h4>
          <p className="reports-subtitle">
            Satış, stok ve ERP süreçlerini tek ekranda izleyin.
          </p>
        </div>
      </div>
      {errorMsg && (
        <div className="alert alert-danger py-2 px-3 reports-alert">
          {errorMsg}
        </div>
      )}

      {/* Sales Summary */}
      <div className="card report-card report-card--summary">
        <div className="report-card__header">
          <div className="report-card__title">
            <i className="fas fa-chart-line text-warning"></i>
            <span>Satış Özeti</span>
          </div>
          <div className="report-card__actions">
            <select
              className="form-select form-select-sm report-select"
              value={salesPeriod}
              onChange={(e) => {
                const nextPeriod = e.target.value;
                setSalesPeriod(nextPeriod);
                loadSales(nextPeriod);
              }}
            >
              <option value="daily">Günlük</option>
              <option value="weekly">Haftalık</option>
              <option value="monthly">Aylık</option>
            </select>
            <button
              className="btn btn-sm btn-outline-dark report-button"
              onClick={handleExportSales}
            >
              Excel indir
            </button>
          </div>
        </div>
        <div className="card-body report-card__body">
          {loadingSales ? (
            <div className="text-muted small">Yükleniyor...</div>
          ) : sales ? (
            <div className="report-metrics">
              <div className="report-metric">
                <span className="report-metric__label">Sipariş</span>
                <span className="report-metric__value">
                  {sales.ordersCount}
                </span>
              </div>
              <div className="report-metric">
                <span className="report-metric__label">Gelir</span>
                <span className="report-metric__value">
                  ₺{Number(sales.revenue || 0).toLocaleString("tr-TR")}
                </span>
              </div>
              <div className="report-metric">
                <span className="report-metric__label">Adet</span>
                <span className="report-metric__value">{sales.itemsSold}</span>
              </div>
              <div className="report-metric">
                <span className="report-metric__label">En Çok</span>
                <span className="report-metric__value report-metric__value--muted">
                  {sales.topProducts
                    ?.slice(0, 2)
                    .map((p) => `#${p.productId}`)
                    .join(", ") || "-"}
                </span>
              </div>
            </div>
          ) : (
            <div className="text-muted small">Veri yok</div>
          )}
        </div>
      </div>

      {/* ERP Sync Status */}
      <div className="card report-card">
        <div className="report-card__header">
          <div className="report-card__title">
            <i className="fas fa-sync-alt text-primary"></i>
            <span>ERP Senkron</span>
          </div>
          <div className="report-card__actions report-card__actions--dense">
            <div className="report-date">
              <input
                type="date"
                className="form-control form-control-sm"
                value={from}
                onChange={(e) => setFrom(e.target.value)}
              />
              <input
                type="date"
                className="form-control form-control-sm"
                value={to}
                onChange={(e) => setTo(e.target.value)}
              />
            </div>
            <button
              className="btn btn-sm btn-outline-secondary report-button"
              onClick={loadErp}
            >
              Güncelle
            </button>
            <button
              className="btn btn-sm btn-outline-dark report-button"
              onClick={handleExportErp}
            >
              Excel indir
            </button>
          </div>
        </div>
        <div className="card-body report-card__body">
          {loadingErp ? (
            <div className="text-muted small">Yükleniyor...</div>
          ) : erp.groups?.length ? (
            <div className="table-responsive">
              <table
                className="table table-sm mb-0 report-table"
              >
                <thead>
                  <tr>
                    <th className="px-1">Varlık</th>
                    <th className="px-1 d-none d-sm-table-cell">Yön</th>
                    <th className="px-1">Durum</th>
                    <th className="px-1 d-none d-md-table-cell">Son</th>
                    <th className="px-1">Sayı</th>
                  </tr>
                </thead>
                <tbody>
                  {erp.groups.map((g, idx) => (
                    <tr key={idx}>
                      <td className="px-1">{g.entity}</td>
                      <td className="px-1 d-none d-sm-table-cell">
                        {g.direction}
                      </td>
                      <td className="px-1">
                        <span
                          className={`badge ${
                            g.lastStatus === "Success"
                              ? "bg-success"
                              : "bg-danger"
                          }`}
                          style={{ fontSize: "0.55rem" }}
                        >
                          {g.lastStatus === "Success" ? "OK" : "Hata"}
                        </span>
                      </td>
                      <td
                        className="px-1 d-none d-md-table-cell"
                        style={{ fontSize: "0.6rem" }}
                      >
                        {g.lastAttemptAt
                          ? new Date(g.lastAttemptAt).toLocaleDateString(
                              "tr-TR"
                            )
                          : "-"}
                      </td>
                      <td className="px-1">{g.recentCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="text-muted small">Kayıt yok</div>
          )}
        </div>
      </div>

      <div className="row g-2">
        {/* Low Stock */}
        <div className="col-12 col-lg-6">
          <div className="card report-card report-card--compact h-100">
            <div className="report-card__header">
              <div className="report-card__title">
                <i className="fas fa-exclamation-triangle text-danger"></i>
                <span>
                  Düşük Stok
                  <small className="text-muted ms-1">
                    (eşik: {lowStock.threshold})
                  </small>
                </span>
              </div>
              <div className="report-card__actions">
                <button
                  className="btn btn-sm btn-outline-secondary report-button"
                  onClick={loadLowStock}
                >
                  Yenile
                </button>
                <button
                  className="btn btn-sm btn-outline-dark report-button"
                  onClick={handleExportLowStock}
                >
                  Excel indir
                </button>
              </div>
            </div>
            <div className="card-body report-card__body">
              {loadingLow ? (
                <div className="text-muted small">Yükleniyor...</div>
              ) : (
                <div
                  className="table-responsive"
                  style={{ maxHeight: "200px" }}
                >
                  <table
                    className="table table-sm mb-0 report-table"
                  >
                    <thead>
                      <tr>
                        <th className="px-1">ID</th>
                        <th className="px-1">Ürün</th>
                        <th className="px-1">Stok</th>
                      </tr>
                    </thead>
                    <tbody>
                      {lowStock.products?.length ? (
                        lowStock.products.map((p) => (
                          <tr key={p.id}>
                            <td className="px-1">{p.id}</td>
                            <td
                              className="px-1 text-truncate"
                              style={{ maxWidth: "100px" }}
                            >
                              {p.name}
                            </td>
                            <td
                              className={`px-1 ${
                                p.stockQuantity <= lowStock.threshold
                                  ? "text-danger fw-bold"
                                  : ""
                              }`}
                            >
                              {p.stockQuantity}
                            </td>
                          </tr>
                        ))
                      ) : (
                        <tr>
                          <td colSpan="3" className="text-muted">
                            Düşük stok yok
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>

      {/* Inventory Movements */}
      <div className="col-12 col-lg-6">
          <div className="card report-card report-card--compact h-100">
            <div className="report-card__header">
              <div className="report-card__title">
                <i className="fas fa-exchange-alt text-info"></i>
                <span>Stok Hareketleri</span>
              </div>
              <div className="report-card__actions">
                <button
                  className="btn btn-sm btn-outline-secondary report-button"
                  onClick={loadMovements}
                >
                  Yenile
                </button>
                <button
                  className="btn btn-sm btn-outline-dark report-button"
                  onClick={handleExportMovements}
                >
                  Excel indir
                </button>
              </div>
            </div>
            <div className="card-body report-card__body">
              {loadingMov ? (
                <div className="text-muted small">Yükleniyor...</div>
              ) : (
                <div
                  className="table-responsive"
                  style={{ maxHeight: "200px" }}
                >
                  <table
                    className="table table-sm mb-0 report-table"
                  >
                    <thead>
                      <tr>
                        <th className="px-1">Tarih</th>
                        <th className="px-1">Ürün</th>
                        <th className="px-1">Adet</th>
                        <th className="px-1 d-none d-sm-table-cell">Tür</th>
                      </tr>
                    </thead>
                    <tbody>
                      {movements.movements?.length ? (
                        movements.movements.map((m) => (
                          <tr key={m.id}>
                            <td className="px-1" style={{ fontSize: "0.6rem" }}>
                              {new Date(m.createdAt).toLocaleDateString(
                                "tr-TR"
                              )}
                            </td>
                            <td
                              className="px-1 text-truncate"
                              style={{ maxWidth: "80px" }}
                            >
                              {m.productName
                                ? `${m.productName} (#${m.productId})`
                                : `#${m.productId}`}
                            </td>
                            <td
                              className={`px-1 ${
                                m.changeQuantity < 0
                                  ? "text-danger"
                                  : "text-success"
                              }`}
                            >
                              {m.changeQuantity}
                            </td>
                            <td className="px-1 d-none d-sm-table-cell">
                              {m.changeType}
                            </td>
                          </tr>
                        ))
                      ) : (
                        <tr>
                          <td colSpan="4" className="text-muted">
                            Kayıt yok
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function isoDate(d) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function formatDate(value) {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleString("tr-TR");
}

function downloadCsv(filename, headers, rows) {
  const escapeCsv = (value) => {
    const normalized = value == null ? "" : String(value);
    if (/[\";\n]/.test(normalized)) {
      return `"${normalized.replace(/"/g, '""')}"`;
    }
    return normalized;
  };

  const lines = [
    headers.map(escapeCsv).join(";"),
    ...rows.map((row) => row.map(escapeCsv).join(";")),
  ];
  const csvContent = `\ufeff${lines.join("\n")}`;
  const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
