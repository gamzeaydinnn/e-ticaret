import React, { useEffect, useMemo, useState } from "react";
import { AdminService } from "../../services/adminService";
import "../../styles/adminReports.css";

const EMPTY_SALES = {
  ordersCount: 0,
  netOrdersCount: 0,
  revenue: 0,
  itemsSold: 0,
  soldProductCount: 0,
  zeroSalesProductCount: 0,
  activeProductCount: 0,
  topProducts: [],
  leastProducts: [],
  refundedProducts: [],
  refundOrdersCount: 0,
  refundAmountTotal: 0,
  refundedItemCount: 0,
};

export default function AdminReports() {
  const [lowStock, setLowStock] = useState({
    threshold: 0,
    outOfStockCount: 0,
    lowStockCount: 0,
    products: [],
  });
  const [loadingLow, setLoadingLow] = useState(true);
  const [lowStockError, setLowStockError] = useState("");

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
  const [movementsError, setMovementsError] = useState("");

  const [salesPeriod, setSalesPeriod] = useState("weekly");
  const [sales, setSales] = useState(EMPTY_SALES);
  const [loadingSales, setLoadingSales] = useState(true);
  const [salesError, setSalesError] = useState("");

  const [erp, setErp] = useState({ groups: [] });
  const [loadingErp, setLoadingErp] = useState(true);
  const [erpError, setErpError] = useState("");

  const periodLabel = useMemo(
    () => ({
      daily: "Günlük",
      weekly: "Haftalık",
      monthly: "Aylık",
    }),
    []
  );

  useEffect(() => {
    loadLowStock();
    loadMovements();
    loadSales();
    loadErp();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const applyDateRange = () => {
    loadErp();
    loadMovements();
  };

  async function loadLowStock() {
    try {
      setLoadingLow(true);
      setLowStockError("");
      const data = await AdminService.getLowStockProducts();
      setLowStock(data);
    } catch (e) {
      console.error("Low stock report error:", e);
      setLowStockError(resolveReportError(e, "Stok durumu alınamadı."));
      setLowStock({
        threshold: 0,
        outOfStockCount: 0,
        lowStockCount: 0,
        products: [],
      });
    } finally {
      setLoadingLow(false);
    }
  }

  async function loadMovements() {
    try {
      setLoadingMov(true);
      setMovementsError("");
      const data = await AdminService.getInventoryMovements({ from, to });
      setMovements(data);
    } catch (e) {
      console.error("Inventory movements report error:", e);
      setMovementsError(resolveReportError(e, "Stok hareketleri alınamadı."));
      setMovements({ start: null, end: null, movements: [] });
    } finally {
      setLoadingMov(false);
    }
  }

  async function loadSales(period = salesPeriod) {
    try {
      setLoadingSales(true);
      setSalesError("");
      const data = await AdminService.getSalesReport(period);
      setSales(data || EMPTY_SALES);
    } catch (e) {
      console.error("Sales report error:", e);
      setSalesError(resolveReportError(e, "Satış özeti alınamadı."));
      setSales(EMPTY_SALES);
    } finally {
      setLoadingSales(false);
    }
  }

  async function loadErp() {
    try {
      setLoadingErp(true);
      setErpError("");
      const data = await AdminService.getErpSyncStatus({ from, to });
      setErp(data || { groups: [] });
    } catch (e) {
      console.error("ERP sync status error:", e);
      setErpError(resolveReportError(e, "ERP senkron durumu alınamadı."));
      setErp({ groups: [] });
    } finally {
      setLoadingErp(false);
    }
  }

  const handleExportSales = () => {
    if (!sales || sales.ordersCount === 0) {
      return;
    }
    const topRows = (sales.topProducts || []).map((p, index) => [
      "En Çok Satan",
      index + 1,
      p.productName || `#${p.productId}`,
      p.quantity ?? 0,
      p.revenue ?? 0,
      p.orderCount ?? 0,
    ]);
    const leastRows = (sales.leastProducts || []).map((p, index) => [
      "En Az Satan",
      index + 1,
      p.productName || `#${p.productId}`,
      p.quantity ?? 0,
      p.revenue ?? 0,
      p.orderCount ?? 0,
    ]);
    const refundRows = (sales.refundedProducts || []).map((p, index) => [
      "İade Edilen",
      index + 1,
      p.productName || `#${p.productId}`,
      p.quantity ?? 0,
      p.amount ?? 0,
      p.orderCount ?? 0,
    ]);
    downloadCsv(
      `satis-urun-analizi-${salesPeriod}-${isoDate(new Date())}.csv`,
      ["Liste", "Sıra", "Ürün", "Adet", "Ciro/İade", "Sipariş"],
      [
        [
          "Özet",
          "-",
          `Sipariş ${sales.ordersCount} / İade sipariş ${sales.refundOrdersCount ?? 0} / İade tutar ${sales.refundAmountTotal ?? 0}`,
          sales.itemsSold ?? 0,
          sales.revenue ?? 0,
          "-",
        ],
        ...topRows,
        ...leastRows,
        ...refundRows,
      ],
    );
  };

  const handleExportErp = () => {
    if (!erp.groups?.length) return;
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
    if (!lowStock.products?.length) return;
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
    if (!movements.movements?.length) return;
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

      <div className="card report-card report-card--summary">
        <div className="report-card__header">
          <div className="report-card__title">
            <i className="fas fa-chart-line text-warning"></i>
            <span>Satış Özeti</span>
            {sales?.periodStart && sales?.periodEnd && (
              <small className="text-muted ms-2">
                ({sales.periodStart} — {sales.periodEnd})
              </small>
            )}
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
              disabled={!sales?.ordersCount}
            >
              Excel indir
            </button>
          </div>
        </div>
        <div className="card-body report-card__body">
          {loadingSales ? (
            <div className="text-muted small">Yükleniyor...</div>
          ) : salesError ? (
            <div className="text-danger small">{salesError}</div>
          ) : (
            <div className="report-metrics">
              <div className="report-metric">
                <span className="report-metric__label">Sipariş</span>
                <span className="report-metric__value">{sales.ordersCount}</span>
                {sales.netOrdersCount != null &&
                  sales.netOrdersCount !== sales.ordersCount && (
                    <small className="text-muted d-block" style={{ fontSize: "0.65rem" }}>
                      Net satış: {sales.netOrdersCount}
                    </small>
                  )}
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
                <span className="report-metric__label">Satılan Ürün</span>
                <span className="report-metric__value">
                  {sales.soldProductCount ?? sales.topProducts?.length ?? 0}
                </span>
                {(sales.zeroSalesProductCount ?? 0) > 0 && (
                  <small
                    className="text-muted d-block"
                    style={{ fontSize: "0.65rem" }}
                  >
                    Satışı yok: {sales.zeroSalesProductCount}
                  </small>
                )}
              </div>
            </div>
          )}

          {!loadingSales && !salesError && (
            <div className="report-product-analysis">
              <div className="report-product-panel">
                <div className="report-product-panel__title">
                  <i className="fas fa-arrow-up text-success"></i>
                  En çok satan ürünler
                </div>
                {(sales.topProducts || []).length === 0 ? (
                  <div className="text-muted small">Bu dönemde satış yok.</div>
                ) : (
                  <div className="table-responsive">
                    <table className="table table-sm mb-0 report-table">
                      <thead>
                        <tr>
                          <th>#</th>
                          <th>Ürün</th>
                          <th className="text-end">Adet</th>
                          <th className="text-end">Ciro</th>
                          <th className="text-end">Sipariş</th>
                        </tr>
                      </thead>
                      <tbody>
                        {sales.topProducts.map((p, index) => (
                          <tr key={`top-${p.productId}`}>
                            <td>{index + 1}</td>
                            <td title={p.productName}>
                              {p.productName || `#${p.productId}`}
                            </td>
                            <td className="text-end fw-semibold">{p.quantity}</td>
                            <td className="text-end">
                              ₺{Number(p.revenue || 0).toLocaleString("tr-TR")}
                            </td>
                            <td className="text-end">{p.orderCount || "-"}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>

              <div className="report-product-panel">
                <div className="report-product-panel__title">
                  <i className="fas fa-arrow-down text-danger"></i>
                  En az satılan ürünler
                </div>
                {(sales.leastProducts || []).length === 0 ? (
                  <div className="text-muted small">Bu dönemde satış yok.</div>
                ) : (
                  <>
                    <div className="table-responsive">
                      <table className="table table-sm mb-0 report-table">
                        <thead>
                          <tr>
                            <th>#</th>
                            <th>Ürün</th>
                            <th className="text-end">Adet</th>
                            <th className="text-end">Ciro</th>
                            <th className="text-end">Sipariş</th>
                          </tr>
                        </thead>
                        <tbody>
                          {sales.leastProducts.map((p, index) => (
                            <tr key={`least-${p.productId}`}>
                              <td>{index + 1}</td>
                              <td title={p.productName}>
                                {p.productName || `#${p.productId}`}
                              </td>
                              <td className="text-end fw-semibold">{p.quantity}</td>
                              <td className="text-end">
                                ₺{Number(p.revenue || 0).toLocaleString("tr-TR")}
                              </td>
                              <td className="text-end">{p.orderCount || "-"}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    {(sales.zeroSalesProductCount ?? 0) > 0 && (
                      <p className="report-product-panel__hint mb-0">
                        Dönemde hiç satılmayan aktif ürün:{" "}
                        <strong>{sales.zeroSalesProductCount}</strong>
                        {sales.activeProductCount
                          ? ` / ${sales.activeProductCount} aktif`
                          : ""}
                      </p>
                    )}
                  </>
                )}
              </div>
            </div>
          )}

          {!loadingSales && !salesError && (
            <div className="report-product-panel report-product-panel--refund mt-3">
              <div className="report-product-panel__title">
                <i className="fas fa-undo text-warning"></i>
                İade edilen ürünler
                {(sales.refundOrdersCount ?? 0) > 0 && (
                  <small className="text-muted ms-2 fw-normal">
                    {sales.refundOrdersCount} sipariş ·{" "}
                    {sales.refundedItemCount ?? 0} adet · ₺
                    {Number(sales.refundAmountTotal || 0).toLocaleString("tr-TR")}
                  </small>
                )}
              </div>
              {(sales.refundedProducts || []).length === 0 ? (
                <div className="text-muted small">
                  Bu dönemde iade edilen ürün kaydı yok.
                </div>
              ) : (
                <div className="table-responsive">
                  <table className="table table-sm mb-0 report-table">
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>Ürün</th>
                        <th className="text-end">Adet</th>
                        <th className="text-end">İade tutarı</th>
                        <th className="text-end">Sipariş</th>
                      </tr>
                    </thead>
                    <tbody>
                      {sales.refundedProducts.map((p, index) => (
                        <tr key={`refund-${p.productId}`}>
                          <td>{index + 1}</td>
                          <td title={p.productName}>
                            {p.productName || `#${p.productId}`}
                          </td>
                          <td className="text-end fw-semibold">{p.quantity}</td>
                          <td className="text-end">
                            ₺{Number(p.amount || 0).toLocaleString("tr-TR")}
                          </td>
                          <td className="text-end">{p.orderCount || "-"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}
        </div>
      </div>

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
              onClick={applyDateRange}
            >
              Güncelle
            </button>
            <button
              className="btn btn-sm btn-outline-dark report-button"
              onClick={handleExportErp}
              disabled={!erp.groups?.length}
            >
              Excel indir
            </button>
          </div>
        </div>
        <div className="card-body report-card__body">
          {loadingErp ? (
            <div className="text-muted small">Yükleniyor...</div>
          ) : erpError ? (
            <div className="text-danger small">{erpError}</div>
          ) : erp.groups?.length ? (
            <div className="table-responsive">
              <table className="table table-sm mb-0 report-table">
                <thead>
                  <tr>
                    <th className="px-1">Varlık</th>
                    <th className="px-1 d-none d-sm-table-cell">Yön</th>
                    <th className="px-1">Durum</th>
                    <th className="px-1 d-none d-md-table-cell">Son</th>
                    <th className="px-1">Kayıt</th>
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
                              : "bg-warning text-dark"
                          }`}
                          style={{ fontSize: "0.55rem" }}
                          title={g.lastError || g.lastMessage || ""}
                        >
                          {g.lastStatus === "Success"
                            ? "Başarılı"
                            : "Son deneme başarısız"}
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
            <div className="text-muted small">Seçilen tarih aralığında kayıt yok</div>
          )}
        </div>
      </div>

      <div className="row g-2">
        <div className="col-12 col-lg-6">
          <div className="card report-card report-card--compact h-100">
            <div className="report-card__header">
              <div className="report-card__title">
                <i className="fas fa-exclamation-triangle text-danger"></i>
                <span>
                  Stok Durumu
                  <small className="text-muted ms-1">
                    (eşik: {lowStock.threshold})
                  </small>
                </span>
                <span className="badge bg-danger ms-2">
                  Stokta Yok: {lowStock.outOfStockCount ?? 0}
                </span>
                <span className="badge bg-warning text-dark ms-1">
                  Kritik: {lowStock.lowStockCount ?? 0}
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
                  disabled={!lowStock.products?.length}
                >
                  Excel indir
                </button>
              </div>
            </div>
            <div className="card-body report-card__body">
              {loadingLow ? (
                <div className="text-muted small">Yükleniyor...</div>
              ) : lowStockError ? (
                <div className="text-danger small">{lowStockError}</div>
              ) : (
                <div
                  className="table-responsive"
                  style={{ maxHeight: "200px" }}
                >
                  <table className="table table-sm mb-0 report-table">
                    <thead>
                      <tr>
                        <th className="px-1">ID</th>
                        <th className="px-1">Ürün</th>
                        <th className="px-1">Stok</th>
                        <th className="px-1">Durum</th>
                      </tr>
                    </thead>
                    <tbody>
                      {lowStock.products?.length ? (
                        lowStock.products.map((p) => {
                          const isOutOfStock = (p.stockQuantity ?? 0) <= 0;
                          return (
                            <tr key={p.id}>
                              <td className="px-1">{p.id}</td>
                              <td
                                className="px-1 text-truncate"
                                style={{ maxWidth: "100px" }}
                              >
                                {p.name}
                              </td>
                              <td
                                className={`px-1 fw-bold ${
                                  isOutOfStock ? "text-danger" : "text-warning"
                                }`}
                              >
                                {p.stockQuantity}
                              </td>
                              <td className="px-1">
                                <span
                                  className={`badge ${
                                    isOutOfStock
                                      ? "bg-danger"
                                      : "bg-warning text-dark"
                                  }`}
                                  style={{ fontSize: "0.55rem" }}
                                >
                                  {isOutOfStock ? "Stokta Yok" : "Kritik"}
                                </span>
                              </td>
                            </tr>
                          );
                        })
                      ) : (
                        <tr>
                          <td colSpan="4" className="text-muted">
                            Stok sorunu yok
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
                  disabled={!movements.movements?.length}
                >
                  Excel indir
                </button>
              </div>
            </div>
            <div className="card-body report-card__body">
              {loadingMov ? (
                <div className="text-muted small">Yükleniyor...</div>
              ) : movementsError ? (
                <div className="text-danger small">{movementsError}</div>
              ) : (
                <div
                  className="table-responsive"
                  style={{ maxHeight: "200px" }}
                >
                  <table className="table table-sm mb-0 report-table">
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

function resolveReportError(error, fallback) {
  const status = error?.status ?? error?.response?.status;
  if (status === 401) {
    return "Oturum süresi doldu. Lütfen tekrar giriş yapın.";
  }
  if (status === 403) {
    return "Bu raporu görüntüleme yetkiniz yok.";
  }
  if (status === 404) {
    return "Rapor servisi bulunamadı. API güncellemesi gerekebilir.";
  }
  return error?.message || fallback;
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
