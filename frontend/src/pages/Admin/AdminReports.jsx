import React, { useEffect, useState } from "react";
import { AdminService } from "../../services/adminService";

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

  async function loadSales() {
    try {
      setLoadingSales(true);
      const data = await AdminService.getSalesReport(salesPeriod);
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

  return (
    <div style={{ overflow: "hidden", maxWidth: "100%" }}>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 gap-2 px-1">
        <h5 className="fw-bold mb-0" style={{ fontSize: "1rem" }}>
          <i className="fas fa-chart-bar me-2" style={{ color: "#f97316" }}></i>
          Raporlar
        </h5>
      </div>
      {errorMsg && (
        <div
          className="alert alert-danger py-2 mx-1"
          style={{ fontSize: "0.75rem" }}
        >
          {errorMsg}
        </div>
      )}

      {/* Sales Summary */}
      <div
        className="card border-0 shadow-sm mb-3 mx-1"
        style={{ borderRadius: "8px" }}
      >
        <div className="card-header bg-white py-2 px-2 d-flex flex-wrap align-items-center justify-content-between gap-2">
          <span style={{ fontSize: "0.8rem" }}>
            <i className="fas fa-chart-line me-1 text-warning"></i>Satış Özeti
          </span>
          <select
            className="form-select form-select-sm"
            style={{
              width: "auto",
              fontSize: "0.7rem",
              padding: "0.2rem 0.5rem",
            }}
            value={salesPeriod}
            onChange={(e) => {
              setSalesPeriod(e.target.value);
              loadSales();
            }}
          >
            <option value="daily">Günlük</option>
            <option value="weekly">Haftalık</option>
            <option value="monthly">Aylık</option>
          </select>
        </div>
        <div className="card-body p-2">
          {loadingSales ? (
            <div className="text-muted small">Yükleniyor...</div>
          ) : sales ? (
            <div className="row g-2 text-center">
              <div className="col-6 col-md-3">
                <div className="fw-bold" style={{ fontSize: "0.7rem" }}>
                  Sipariş
                </div>
                <div style={{ fontSize: "0.85rem" }}>{sales.ordersCount}</div>
              </div>
              <div className="col-6 col-md-3">
                <div className="fw-bold" style={{ fontSize: "0.7rem" }}>
                  Gelir
                </div>
                <div style={{ fontSize: "0.85rem" }}>
                  ₺{Number(sales.revenue || 0).toLocaleString("tr-TR")}
                </div>
              </div>
              <div className="col-6 col-md-3">
                <div className="fw-bold" style={{ fontSize: "0.7rem" }}>
                  Adet
                </div>
                <div style={{ fontSize: "0.85rem" }}>{sales.itemsSold}</div>
              </div>
              <div className="col-6 col-md-3">
                <div className="fw-bold" style={{ fontSize: "0.7rem" }}>
                  En Çok
                </div>
                <div className="text-truncate" style={{ fontSize: "0.75rem" }}>
                  {sales.topProducts
                    ?.slice(0, 2)
                    .map((p) => `#${p.productId}`)
                    .join(", ") || "-"}
                </div>
              </div>
            </div>
          ) : (
            <div className="text-muted small">Veri yok</div>
          )}
        </div>
      </div>

      {/* ERP Sync Status */}
      <div
        className="card border-0 shadow-sm mb-3 mx-1"
        style={{ borderRadius: "8px" }}
      >
        <div className="card-header bg-white py-2 px-2 d-flex flex-wrap align-items-center justify-content-between gap-1">
          <span style={{ fontSize: "0.8rem" }}>
            <i className="fas fa-sync-alt me-1 text-primary"></i>ERP Senkron
          </span>
          <div className="d-flex gap-1 flex-wrap">
            <input
              type="date"
              className="form-control form-control-sm"
              style={{
                fontSize: "0.65rem",
                padding: "0.15rem 0.3rem",
                width: "100px",
              }}
              value={from}
              onChange={(e) => setFrom(e.target.value)}
            />
            <input
              type="date"
              className="form-control form-control-sm"
              style={{
                fontSize: "0.65rem",
                padding: "0.15rem 0.3rem",
                width: "100px",
              }}
              value={to}
              onChange={(e) => setTo(e.target.value)}
            />
            <button
              className="btn btn-sm btn-outline-secondary px-2 py-0"
              style={{ fontSize: "0.65rem" }}
              onClick={loadErp}
            >
              Git
            </button>
          </div>
        </div>
        <div className="card-body p-2">
          {loadingErp ? (
            <div className="text-muted small">Yükleniyor...</div>
          ) : erp.groups?.length ? (
            <div className="table-responsive">
              <table
                className="table table-sm mb-0"
                style={{ fontSize: "0.65rem" }}
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

      <div className="row g-2 mx-0">
        {/* Low Stock */}
        <div className="col-12 col-lg-6">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "8px" }}
          >
            <div className="card-header bg-white py-2 px-2 d-flex justify-content-between align-items-center">
              <span style={{ fontSize: "0.8rem" }}>
                <i className="fas fa-exclamation-triangle me-1 text-danger"></i>
                Düşük Stok
                <small
                  className="text-muted ms-1"
                  style={{ fontSize: "0.6rem" }}
                >
                  (eşik: {lowStock.threshold})
                </small>
              </span>
              <button
                className="btn btn-sm btn-outline-secondary px-2 py-0"
                style={{ fontSize: "0.6rem" }}
                onClick={loadLowStock}
              >
                Yenile
              </button>
            </div>
            <div className="card-body p-2">
              {loadingLow ? (
                <div className="text-muted small">Yükleniyor...</div>
              ) : (
                <div
                  className="table-responsive"
                  style={{ maxHeight: "200px" }}
                >
                  <table
                    className="table table-sm mb-0"
                    style={{ fontSize: "0.65rem" }}
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
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "8px" }}
          >
            <div className="card-header bg-white py-2 px-2 d-flex flex-wrap justify-content-between align-items-center gap-1">
              <span style={{ fontSize: "0.8rem" }}>
                <i className="fas fa-exchange-alt me-1 text-info"></i>Stok
                Hareketleri
              </span>
              <button
                className="btn btn-sm btn-outline-secondary px-2 py-0"
                style={{ fontSize: "0.6rem" }}
                onClick={loadMovements}
              >
                Yenile
              </button>
            </div>
            <div className="card-body p-2">
              {loadingMov ? (
                <div className="text-muted small">Yükleniyor...</div>
              ) : (
                <div
                  className="table-responsive"
                  style={{ maxHeight: "200px" }}
                >
                  <table
                    className="table table-sm mb-0"
                    style={{ fontSize: "0.65rem" }}
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
                              #{m.productId}
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
