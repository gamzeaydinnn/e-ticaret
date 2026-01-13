import React, { useEffect, useState } from "react";
import { MicroService } from "../../services/microService";

export default function AdminMicro() {
  const [products, setProducts] = useState([]);
  const [stocks, setStocks] = useState([]);
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("info"); // success | danger | info
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    // Mevcut mimariye uygun olarak sayfa açılışında verileri yükleyelim
    loadInitial();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadInitial = async () => {
    try {
      setLoading(true);
      const [prods, stk] = await Promise.all([
        MicroService.getProducts(),
        MicroService.getStocks(),
      ]);
      setProducts(prods || []);
      setStocks(stk || []);
    } catch (err) {
      setMessage(err.message || "Veriler yüklenemedi");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  };

  const syncProducts = async () => {
    setLoading(true);
    try {
      const res = await MicroService.syncProducts();
      setMessage(res?.message || "Ürünler senkronize edildi");
      setMessageType("success");
      await fetchProducts();
    } catch (err) {
      setMessage(err.message || "Ürün senkronizasyonu başarısız");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  };

  const fetchProducts = async () => {
    try {
      const res = await MicroService.getProducts();
      setProducts(res || []);
    } catch (err) {
      setMessage(err.message || "Ürünler getirilemedi");
      setMessageType("danger");
    }
  };

  const fetchStocks = async () => {
    try {
      const res = await MicroService.getStocks();
      setStocks(res || []);
    } catch (err) {
      setMessage(err.message || "Stoklar getirilemedi");
      setMessageType("danger");
    }
  };

  const exportOrders = async () => {
    setLoading(true);
    try {
      const orders = []; // Geliştirme: seçim listesi entegre edilebilir
      const res = await MicroService.exportOrders(orders);
      setMessage(res?.message || "Siparişler ERP'ye aktarıldı");
      setMessageType("success");
    } catch (err) {
      setMessage(err.message || "Siparişler ERP'ye gönderilemedi");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  };

  const syncStocksFromERP = async () => {
    setLoading(true);
    try {
      const res = await MicroService.syncStocksFromERP();
      setMessage(res?.message || "Stoklar ERP'den alınıp güncellendi");
      setMessageType("success");
      await fetchStocks();
    } catch (err) {
      setMessage(err.message || "Stok senkronizasyonu başarısız");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  };

  const syncPricesFromERP = async () => {
    setLoading(true);
    try {
      const res = await MicroService.syncPricesFromERP();
      setMessage(res?.message || "Fiyatlar ERP'den alınıp güncellendi");
      setMessageType("success");
      await fetchProducts();
    } catch (err) {
      setMessage(err.message || "Fiyat senkronizasyonu başarısız");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container-fluid p-2 p-md-4">
      {/* Sayfa Başlığı - Responsive */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-3 mb-md-4 gap-2">
        <div className="mb-2 mb-md-0">
          <h1 className="h4 h3-md fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i className="fas fa-plug me-2" style={{ color: "#f57c00" }} />
            ERP / Mikro Entegrasyon
          </h1>
          <div className="text-muted" style={{ fontSize: "0.8rem" }}>
            ERP ile ürün, stok ve fiyat senkronizasyonu
          </div>
        </div>
        {/* Butonlar - Mobilde grid, desktop'ta flex */}
        <div
          className="d-grid d-md-flex gap-2 w-100 w-md-auto"
          style={{ maxWidth: "100%" }}
        >
          <button
            className="btn btn-sm text-white fw-semibold"
            style={{
              background: "linear-gradient(135deg, #2b6cb0, #3182ce)",
              minHeight: "44px",
            }}
            onClick={syncProducts}
            disabled={loading}
          >
            <i className="fas fa-sync-alt me-2"></i>
            <span className="d-none d-lg-inline">Ürünleri </span>Senkronize
          </button>
          <button
            className="btn btn-sm text-white fw-semibold"
            style={{
              background: "linear-gradient(135deg, #553c9a, #6b46c1)",
              minHeight: "44px",
            }}
            onClick={syncStocksFromERP}
            disabled={loading}
          >
            <i className="fas fa-boxes me-2"></i>
            <span className="d-none d-lg-inline">Stokları </span>ERP'den Çek
          </button>
          <button
            className="btn btn-sm text-white fw-semibold"
            style={{
              background: "linear-gradient(135deg, #b83280, #d53f8c)",
              minHeight: "44px",
            }}
            onClick={syncPricesFromERP}
            disabled={loading}
          >
            <i className="fas fa-tags me-2"></i>
            <span className="d-none d-lg-inline">Fiyatları </span>ERP'den Çek
          </button>
          <button
            className="btn btn-sm text-white fw-semibold"
            style={{
              background: "linear-gradient(135deg, #6b46c1, #805ad5)",
              minHeight: "44px",
            }}
            onClick={exportOrders}
            disabled={loading}
          >
            <i className="fas fa-paper-plane me-2"></i>
            <span className="d-none d-lg-inline">Siparişleri </span>Gönder
          </button>
        </div>
      </div>

      {/* Operasyon Bilgileri */}
      {loading && (
        <div
          className="alert alert-warning d-flex align-items-center py-2"
          role="alert"
        >
          <i className="fas fa-spinner fa-spin me-2"></i>
          İşlem yapılıyor, lütfen bekleyin...
        </div>
      )}
      {message && (
        <div className={`alert alert-${messageType} py-2`} role="alert">
          {message}
        </div>
      )}

      {/* Liste ve Tablolar */}
      <div className="row g-2 g-md-4">
        {/* Ürünler */}
        <div className="col-12 col-xl-6">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-header bg-white d-flex justify-content-between align-items-center py-2 px-3">
              <div style={{ fontSize: "0.9rem" }}>
                <i className="fas fa-box me-2 text-primary"></i>
                Ürünler
              </div>
              <button
                className="btn btn-sm btn-outline-secondary"
                onClick={fetchProducts}
                disabled={loading}
                style={{ minHeight: "36px" }}
              >
                Yenile
              </button>
            </div>
            <div className="card-body p-2 p-md-3">
              <div className="table-responsive">
                <table
                  className="table table-sm align-middle admin-mobile-table"
                  style={{ fontSize: "0.8rem" }}
                >
                  <thead>
                    <tr>
                      <th style={{ width: 60 }}>ID</th>
                      <th>Ürün Adı</th>
                      <th style={{ width: 100 }}>Fiyat</th>
                      <th
                        className="d-none d-md-table-cell"
                        style={{ width: 140 }}
                      >
                        Kategori
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {products?.length ? (
                      products.map((p) => (
                        <tr key={p.id}>
                          <td data-label="ID">#{p.id}</td>
                          <td data-label="Ürün Adı">{p.name}</td>
                          <td data-label="Fiyat">
                            {typeof p.price === "number"
                              ? `₺${p.price.toLocaleString("tr-TR", {
                                  minimumFractionDigits: 2,
                                })}`
                              : p.price}
                          </td>
                          <td
                            data-label="Kategori"
                            className="d-none d-md-table-cell"
                          >
                            {p.category || p.categoryName || "-"}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan="4" className="text-muted text-center py-3">
                          Kayıt bulunamadı.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>

        {/* Stoklar */}
        <div className="col-12 col-xl-6">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-header bg-white d-flex justify-content-between align-items-center py-2 px-3">
              <div style={{ fontSize: "0.9rem" }}>
                <i className="fas fa-warehouse me-2 text-warning"></i>
                Stoklar
              </div>
              <button
                className="btn btn-sm btn-outline-secondary"
                onClick={fetchStocks}
                disabled={loading}
                style={{ minHeight: "36px" }}
              >
                Yenile
              </button>
            </div>
            <div className="card-body p-2 p-md-3">
              <div className="table-responsive">
                <table
                  className="table table-sm align-middle admin-mobile-table"
                  style={{ fontSize: "0.8rem" }}
                >
                  <thead>
                    <tr>
                      <th style={{ width: 100 }}>Ürün ID</th>
                      <th>Stok Miktarı</th>
                    </tr>
                  </thead>
                  <tbody>
                    {stocks?.length ? (
                      stocks.map((s) => (
                        <tr key={s.productId}>
                          <td data-label="Ürün ID">#{s.productId}</td>
                          <td
                            data-label="Stok"
                            className={
                              Number(s.quantity) <= 0
                                ? "text-danger fw-bold"
                                : ""
                            }
                          >
                            {s.quantity}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan="2" className="text-muted text-center py-3">
                          Kayıt bulunamadı.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
