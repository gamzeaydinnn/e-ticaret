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
    <div className="container-fluid p-4">
      {/* Sayfa Başlığı */}
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h3 fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i className="fas fa-plug me-2" style={{ color: "#f57c00" }} />
            ERP / Mikro Entegrasyon
          </h1>
          <div className="text-muted" style={{ fontSize: "0.9rem" }}>
            ERP ile ürün, stok ve fiyat senkronizasyonu
          </div>
        </div>
        <div className="d-flex gap-2">
          <button
            className="btn btn-sm text-white fw-semibold"
            style={{
              background: "linear-gradient(135deg, #2b6cb0, #3182ce)",
            }}
            onClick={syncProducts}
            disabled={loading}
          >
            <i className="fas fa-sync-alt me-2"></i>
            Ürünleri Senkronize Et
          </button>
          <button
            className="btn btn-sm text-white fw-semibold"
            style={{
              background: "linear-gradient(135deg, #553c9a, #6b46c1)",
            }}
            onClick={syncStocksFromERP}
            disabled={loading}
          >
            <i className="fas fa-boxes me-2"></i>
            Stokları ERP'den Çek
          </button>
          <button
            className="btn btn-sm text-white fw-semibold"
            style={{
              background: "linear-gradient(135deg, #b83280, #d53f8c)",
            }}
            onClick={syncPricesFromERP}
            disabled={loading}
          >
            <i className="fas fa-tags me-2"></i>
            Fiyatları ERP'den Çek
          </button>
          <button
            className="btn btn-sm text-white fw-semibold"
            style={{
              background: "linear-gradient(135deg, #6b46c1, #805ad5)",
            }}
            onClick={exportOrders}
            disabled={loading}
          >
            <i className="fas fa-paper-plane me-2"></i>
            Siparişleri Gönder
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
      <div className="row g-4">
        {/* Ürünler */}
        <div className="col-12 col-xl-6">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-header bg-white d-flex justify-content-between align-items-center">
              <div>
                <i className="fas fa-box me-2 text-primary"></i>
                Ürünler
              </div>
              <button
                className="btn btn-sm btn-outline-secondary"
                onClick={fetchProducts}
                disabled={loading}
              >
                Yenile
              </button>
            </div>
            <div className="card-body">
              <div className="table-responsive">
                <table className="table table-sm align-middle">
                  <thead>
                    <tr>
                      <th style={{ width: 70 }}>ID</th>
                      <th>Ürün Adı</th>
                      <th style={{ width: 120 }}>Fiyat</th>
                      <th style={{ width: 160 }}>Kategori</th>
                    </tr>
                  </thead>
                  <tbody>
                    {products?.length ? (
                      products.map((p) => (
                        <tr key={p.id}>
                          <td>#{p.id}</td>
                          <td>{p.name}</td>
                          <td>
                            {typeof p.price === "number"
                              ? `₺${p.price.toLocaleString("tr-TR", {
                                  minimumFractionDigits: 2,
                                })}`
                              : p.price}
                          </td>
                          <td>{p.category || p.categoryName || "-"}</td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan="4" className="text-muted">
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
            <div className="card-header bg-white d-flex justify-content-between align-items-center">
              <div>
                <i className="fas fa-warehouse me-2 text-warning"></i>
                Stoklar
              </div>
              <button
                className="btn btn-sm btn-outline-secondary"
                onClick={fetchStocks}
                disabled={loading}
              >
                Yenile
              </button>
            </div>
            <div className="card-body">
              <div className="table-responsive">
                <table className="table table-sm align-middle">
                  <thead>
                    <tr>
                      <th style={{ width: 120 }}>Ürün ID</th>
                      <th>Stok Miktarı</th>
                    </tr>
                  </thead>
                  <tbody>
                    {stocks?.length ? (
                      stocks.map((s) => (
                        <tr key={s.productId}>
                          <td>#{s.productId}</td>
                          <td
                            className={
                              Number(s.quantity) <= 0 ? "text-danger" : ""
                            }
                          >
                            {s.quantity}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan="2" className="text-muted">
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
