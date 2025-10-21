import React, { useEffect, useState } from "react";
import AdminLayout from "../../components/AdminLayout";
import { AdminService } from "../../services/adminService";

export default function AdminReports() {
  const [lowStock, setLowStock] = useState({ threshold: 0, products: [] });
  const [loadingLow, setLoadingLow] = useState(true);
  const [from, setFrom] = useState(isoDate(new Date(Date.now() - 7 * 86400000)));
  const [to, setTo] = useState(isoDate(new Date()));
  const [movements, setMovements] = useState({ start: null, end: null, movements: [] });
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
    <AdminLayout>
      <div className="container-fluid p-4">
        <div className="d-flex justify-content-between align-items-center mb-4">
          <h2 className="fw-bold mb-0">Raporlar</h2>
        </div>
        {errorMsg ? (
          <div className="alert alert-danger" role="alert">
            {errorMsg}
          </div>
        ) : null}

        {/* Sales Summary */}
        <div className="card border-0 shadow-sm mb-4">
          <div className="card-header bg-white d-flex align-items-center justify-content-between">
            <div>
              <i className="fas fa-chart-line me-2 text-warning"></i>
              Satış Özeti
            </div>
            <div>
              <select
                className="form-select form-select-sm"
                style={{ width: 160 }}
                value={salesPeriod}
                onChange={(e) => setSalesPeriod(e.target.value)}
                onBlur={loadSales}
                onInput={loadSales}
              >
                <option value="daily">Günlük</option>
                <option value="weekly">Haftalık</option>
                <option value="monthly">Aylık</option>
              </select>
            </div>
          </div>
          <div className="card-body">
            {loadingSales ? (
              <div className="text-muted">Yükleniyor...</div>
            ) : sales ? (
              <div className="row text-center">
                <div className="col-md-3">
                  <div className="fw-bold">Sipariş</div>
                  <div>{sales.ordersCount}</div>
                </div>
                <div className="col-md-3">
                  <div className="fw-bold">Gelir</div>
                  <div>₺{Number(sales.revenue || 0).toLocaleString("tr-TR", { minimumFractionDigits: 2 })}</div>
                </div>
                <div className="col-md-3">
                  <div className="fw-bold">Adet</div>
                  <div>{sales.itemsSold}</div>
                </div>
                <div className="col-md-3">
                  <div className="fw-bold">En Çok Satan</div>
                  <div>{sales.topProducts?.map((p) => `#${p.productId}(${p.quantity})`).join(", ")}</div>
                </div>
              </div>
            ) : (
              <div className="text-muted">Veri yok</div>
            )}
          </div>
        </div>

        <div className="row g-4">
          {/* ERP Sync Status */}
          <div className="col-12">
            <div className="card border-0 shadow-sm">
              <div className="card-header bg-white d-flex justify-content-between align-items-center">
                <div>
                  <i className="fas fa-sync-alt me-2 text-primary"></i>
                  ERP Senkron Durumu
                </div>
                <div className="d-flex gap-2">
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
                  <button className="btn btn-sm btn-outline-secondary" onClick={loadErp}>
                    Getir
                  </button>
                </div>
              </div>
              <div className="card-body">
                {loadingErp ? (
                  <div className="text-muted">Yükleniyor...</div>
                ) : erp.groups?.length ? (
                  <div className="table-responsive">
                    <table className="table table-sm">
                      <thead>
                        <tr>
                          <th>Varlık</th>
                          <th>Yön</th>
                          <th>Son Deneme</th>
                          <th>Durum</th>
                          <th>Son Başarılı</th>
                          <th>Güncellenen</th>
                          <th>Son Hata</th>
                          <th>Kayıt Sayısı</th>
                        </tr>
                      </thead>
                      <tbody>
                        {erp.groups.map((g, idx) => (
                          <tr key={idx}>
                            <td>{g.entity}</td>
                            <td>{g.direction}</td>
                            <td>{g.lastAttemptAt ? new Date(g.lastAttemptAt).toLocaleString("tr-TR") : "-"}</td>
                            <td>
                              <span className={`badge ${g.lastStatus === "Success" ? "bg-success" : "bg-danger"}`}>
                                {g.lastStatus}
                              </span>
                            </td>
                            <td>{g.lastSuccessAt ? new Date(g.lastSuccessAt).toLocaleString("tr-TR") : "-"}</td>
                            <td>{g.updatedCount ?? "-"}</td>
                            <td className="text-danger">{g.lastError || "-"}</td>
                            <td>{g.recentCount}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="text-muted">Kayıt bulunamadı.</div>
                )}
              </div>
            </div>
          </div>
          {/* Low Stock */}
          <div className="col-lg-6">
            <div className="card border-0 shadow-sm h-100">
              <div className="card-header bg-white d-flex justify-content-between align-items-center">
                <div>
                  <i className="fas fa-exclamation-triangle me-2 text-danger"></i>
                  Düşük Stok Ürünleri
                  <small className="ms-2 text-muted">(eşik: {lowStock.threshold})</small>
                </div>
                <button className="btn btn-sm btn-outline-secondary" onClick={loadLowStock}>
                  Yenile
                </button>
              </div>
              <div className="card-body">
                {loadingLow ? (
                  <div className="text-muted">Yükleniyor...</div>
                ) : (
                  <div className="table-responsive">
                    <table className="table table-sm">
                      <thead>
                        <tr>
                          <th>ID</th>
                          <th>Ürün</th>
                          <th>Stok</th>
                        </tr>
                      </thead>
                      <tbody>
                        {lowStock.products?.length ? (
                          lowStock.products.map((p) => (
                            <tr key={p.id}>
                              <td>{p.id}</td>
                              <td>{p.name}</td>
                              <td className={p.stockQuantity <= lowStock.threshold ? "text-danger" : ""}>
                                {p.stockQuantity}
                              </td>
                            </tr>
                          ))
                        ) : (
                          <tr>
                            <td colSpan="3" className="text-muted">
                              Düşük stokta ürün yok.
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
          <div className="col-lg-6">
            <div className="card border-0 shadow-sm h-100">
              <div className="card-header bg-white d-flex justify-content-between align-items-center">
                <div>
                  <i className="fas fa-exchange-alt me-2 text-info"></i>
                  Stok Hareketleri
                </div>
                <div className="d-flex gap-2">
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
                  <button className="btn btn-sm btn-outline-secondary" onClick={loadMovements}>
                    Getir
                  </button>
                </div>
              </div>
              <div className="card-body">
                {loadingMov ? (
                  <div className="text-muted">Yükleniyor...</div>
                ) : (
                  <div className="table-responsive" style={{ maxHeight: 360 }}>
                    <table className="table table-sm">
                      <thead>
                        <tr>
                          <th>Tarih</th>
                          <th>Ürün</th>
                          <th>Adet</th>
                          <th>Tür</th>
                          <th>Not</th>
                        </tr>
                      </thead>
                      <tbody>
                        {movements.movements?.length ? (
                          movements.movements.map((m) => (
                            <tr key={m.id}>
                              <td>{new Date(m.createdAt).toLocaleString("tr-TR")}</td>
                              <td>#{m.productId} {m.productName}</td>
                              <td className={m.changeQuantity < 0 ? "text-danger" : "text-success"}>{m.changeQuantity}</td>
                              <td>{m.changeType}</td>
                              <td>{m.note || "-"}</td>
                            </tr>
                          ))
                        ) : (
                          <tr>
                            <td colSpan="5" className="text-muted">
                              Kayıt bulunamadı.
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
    </AdminLayout>
  );
}

function isoDate(d) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}
