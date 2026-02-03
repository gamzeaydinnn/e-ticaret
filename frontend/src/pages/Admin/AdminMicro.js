import React, { useEffect, useState, useCallback } from "react";
import { MicroService } from "../../services/microService";

export default function AdminMicro() {
  // Ürün ve stok verileri
  const [products, setProducts] = useState([]);
  const [stocks, setStocks] = useState([]);

  // Mikro API'den gelen detaylı stok listesi
  const [stokListesi, setStokListesi] = useState([]);
  const [stokListesiMeta, setStokListesiMeta] = useState({
    totalCount: 0,
    page: 1,
    pageSize: 20,
  });

  // Bağlantı durumu
  const [connectionStatus, setConnectionStatus] = useState(null);
  const [connectionLoading, setConnectionLoading] = useState(false);

  // Genel durum
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("info"); // success | danger | info | warning
  const [loading, setLoading] = useState(false);

  // Aktif sekme: 'overview' | 'stokListesi' | 'settings'
  const [activeTab, setActiveTab] = useState("overview");

  // ============================================================================
  // Sayfa açılışında verileri yükle
  // ============================================================================
  useEffect(() => {
    loadInitial();
    testConnection();
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

  // ============================================================================
  // Mikro API Bağlantı Testi
  // ============================================================================
  const testConnection = useCallback(async () => {
    setConnectionLoading(true);
    try {
      const result = await MicroService.testConnection();

      // Backend'den gelen response: { isConnected, mikroApiOnline, databaseOnline, message, ... }
      // veya { success, message, toplamUrunSayisi, ... }
      const isConnected = result?.isConnected ?? result?.success ?? false;
      const mikroApiOnline = result?.mikroApiOnline ?? isConnected;
      const databaseOnline = result?.databaseOnline ?? true;

      setConnectionStatus({
        isConnected: isConnected || databaseOnline, // En az biri çalışıyorsa bağlı say
        mikroApiOnline,
        databaseOnline,
        message: result?.message,
        apiUrl: result?.apiUrl,
        toplamUrunSayisi:
          result?.toplamUrunSayisi || result?.veritabaniUrunSayisi || 0,
      });

      if (mikroApiOnline) {
        setMessage("Mikro API bağlantısı başarılı!");
        setMessageType("success");
      } else if (databaseOnline) {
        setMessage(
          result?.message || "Mikro API offline. Veritabanından çalışılıyor.",
        );
        setMessageType("warning");
      } else {
        setMessage(result?.message || "Bağlantı kurulamadı");
        setMessageType("danger");
      }
    } catch (err) {
      setConnectionStatus({ isConnected: false, message: err.message });
      setMessage(
        "Bağlantı testi başarısız: " + (err.message || "Bilinmeyen hata"),
      );
      setMessageType("danger");
    } finally {
      setConnectionLoading(false);
    }
  }, []);

  // ============================================================================
  // Mikro API'den Stok Listesi Çek (StokListesiV2)
  // ============================================================================
  const loadStokListesi = useCallback(
    async (sayfa = 1, sayfaBuyuklugu = 20) => {
      setLoading(true);
      try {
        const result = await MicroService.getStokListesi({
          sayfa,
          sayfaBuyuklugu,
          depoNo: 1, // Varsayılan depo
          sadeceAktif: true,
        });

        if (result?.success) {
          setStokListesi(result.data || []);
          // Backend'den gelen field isimleri: toplamKayit, sayfa, sayfaBuyuklugu
          setStokListesiMeta({
            totalCount: result.toplamKayit || result.totalCount || 0,
            page: result.sayfa || result.page || sayfa,
            pageSize:
              result.sayfaBuyuklugu || result.pageSize || sayfaBuyuklugu,
          });

          // Offline mod kontrolü
          const offlineWarning = result.isOfflineMode
            ? " (Veritabanından)"
            : " (Mikro API'den)";
          setMessage(
            `${result.data?.length || 0} ürün yüklendi${offlineWarning}`,
          );
          setMessageType(result.isOfflineMode ? "warning" : "success");
        } else {
          setMessage(result?.message || "Stok listesi alınamadı");
          setMessageType("danger");
        }
      } catch (err) {
        setMessage(
          "Stok listesi alınamadı: " + (err.message || "Bilinmeyen hata"),
        );
        setMessageType("danger");
      } finally {
        setLoading(false);
      }
    },
    [],
  );

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

  // Sayfalama için
  const handlePageChange = (newPage) => {
    loadStokListesi(newPage, stokListesiMeta.pageSize);
  };

  return (
    <div className="container-fluid p-2 p-md-4">
      {/* Sayfa Başlığı */}
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
      </div>

      {/* Bağlantı Durumu Kartı */}
      <div className="row mb-3">
        <div className="col-12">
          <div
            className={`card border-0 shadow-sm ${
              connectionStatus?.mikroApiOnline
                ? "border-start border-success border-4"
                : connectionStatus?.databaseOnline
                  ? "border-start border-warning border-4"
                  : "border-start border-danger border-4"
            }`}
            style={{ borderLeftWidth: "4px" }}
          >
            <div className="card-body py-3 d-flex align-items-center justify-content-between flex-wrap gap-2">
              <div className="d-flex align-items-center">
                <div
                  className={`rounded-circle d-flex align-items-center justify-content-center me-3`}
                  style={{
                    width: 48,
                    height: 48,
                    background: connectionStatus?.mikroApiOnline
                      ? "linear-gradient(135deg, #10b981, #059669)"
                      : connectionStatus?.databaseOnline
                        ? "linear-gradient(135deg, #f59e0b, #d97706)"
                        : "linear-gradient(135deg, #ef4444, #dc2626)",
                  }}
                >
                  <i
                    className={`fas ${
                      connectionStatus?.mikroApiOnline
                        ? "fa-check"
                        : connectionStatus?.databaseOnline
                          ? "fa-database"
                          : "fa-times"
                    } text-white`}
                  ></i>
                </div>
                <div>
                  <h6 className="mb-0 fw-bold">
                    {connectionStatus?.mikroApiOnline
                      ? "Mikro API Bağlantısı"
                      : connectionStatus?.databaseOnline
                        ? "Offline Mod (Veritabanı)"
                        : "Bağlantı Durumu"}
                  </h6>
                  <small
                    className={
                      connectionStatus?.mikroApiOnline
                        ? "text-success"
                        : connectionStatus?.databaseOnline
                          ? "text-warning"
                          : "text-danger"
                    }
                  >
                    {connectionStatus?.mikroApiOnline
                      ? `Bağlı - ${connectionStatus?.toplamUrunSayisi || 0} ürün`
                      : connectionStatus?.databaseOnline
                        ? `${connectionStatus?.toplamUrunSayisi || 0} ürün veritabanında`
                        : "Bağlantı Yok"}
                  </small>
                </div>
              </div>
              <button
                className="btn btn-outline-primary btn-sm"
                onClick={testConnection}
                disabled={connectionLoading}
              >
                {connectionLoading ? (
                  <>
                    <i className="fas fa-spinner fa-spin me-1"></i> Test
                    Ediliyor...
                  </>
                ) : (
                  <>
                    <i className="fas fa-sync-alt me-1"></i> Bağlantı Testi
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Tab Navigasyon */}
      <ul className="nav nav-tabs mb-3">
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "overview" ? "active" : ""}`}
            onClick={() => setActiveTab("overview")}
          >
            <i className="fas fa-home me-1"></i> Genel Bakış
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "stokListesi" ? "active" : ""}`}
            onClick={() => {
              setActiveTab("stokListesi");
              loadStokListesi();
            }}
          >
            <i className="fas fa-list me-1"></i> Mikro Stok Listesi
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "settings" ? "active" : ""}`}
            onClick={() => setActiveTab("settings")}
          >
            <i className="fas fa-cog me-1"></i> Ayarlar
          </button>
        </li>
      </ul>

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
        <div
          className={`alert alert-${messageType} py-2 alert-dismissible`}
          role="alert"
        >
          {message}
          <button
            type="button"
            className="btn-close"
            onClick={() => setMessage("")}
          ></button>
        </div>
      )}

      {/* Tab İçerikleri */}
      {activeTab === "overview" && (
        <>
          {/* Aksiyon Butonları */}
          <div className="row mb-4">
            <div className="col-12">
              <div className="card border-0 shadow-sm">
                <div className="card-header bg-white py-3">
                  <h6 className="mb-0 fw-bold">
                    <i className="fas fa-sync me-2 text-primary"></i>
                    Senkronizasyon İşlemleri
                  </h6>
                </div>
                <div className="card-body">
                  <div className="row g-2">
                    <div className="col-6 col-md-3">
                      <button
                        className="btn text-white fw-semibold w-100"
                        style={{
                          background:
                            "linear-gradient(135deg, #2b6cb0, #3182ce)",
                        }}
                        onClick={syncProducts}
                        disabled={loading}
                      >
                        <i className="fas fa-sync-alt me-2"></i>
                        Ürünleri Senkronize
                      </button>
                    </div>
                    <div className="col-6 col-md-3">
                      <button
                        className="btn text-white fw-semibold w-100"
                        style={{
                          background:
                            "linear-gradient(135deg, #553c9a, #6b46c1)",
                        }}
                        onClick={syncStocksFromERP}
                        disabled={loading}
                      >
                        <i className="fas fa-boxes me-2"></i>
                        Stokları ERP'den Çek
                      </button>
                    </div>
                    <div className="col-6 col-md-3">
                      <button
                        className="btn text-white fw-semibold w-100"
                        style={{
                          background:
                            "linear-gradient(135deg, #b83280, #d53f8c)",
                        }}
                        onClick={syncPricesFromERP}
                        disabled={loading}
                      >
                        <i className="fas fa-tags me-2"></i>
                        Fiyatları ERP'den Çek
                      </button>
                    </div>
                    <div className="col-6 col-md-3">
                      <button
                        className="btn text-white fw-semibold w-100"
                        style={{
                          background:
                            "linear-gradient(135deg, #6b46c1, #805ad5)",
                        }}
                        onClick={exportOrders}
                        disabled={loading}
                      >
                        <i className="fas fa-paper-plane me-2"></i>
                        Siparişleri Gönder
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Ürünler ve Stoklar Tabloları */}
          <div className="row g-3">
            {/* Ürünler */}
            <div className="col-12 col-xl-6">
              <div className="card border-0 shadow-sm h-100">
                <div className="card-header bg-white d-flex justify-content-between align-items-center py-2 px-3">
                  <div style={{ fontSize: "0.9rem" }}>
                    <i className="fas fa-box me-2 text-primary"></i>
                    Yerel Ürünler ({products?.length || 0})
                  </div>
                  <button
                    className="btn btn-sm btn-outline-secondary"
                    onClick={fetchProducts}
                    disabled={loading}
                  >
                    <i className="fas fa-refresh me-1"></i> Yenile
                  </button>
                </div>
                <div className="card-body p-2 p-md-3">
                  <div
                    className="table-responsive"
                    style={{ maxHeight: "400px", overflowY: "auto" }}
                  >
                    <table
                      className="table table-sm align-middle"
                      style={{ fontSize: "0.8rem" }}
                    >
                      <thead className="table-light sticky-top">
                        <tr>
                          <th style={{ width: 60 }}>ID</th>
                          <th>Ürün Adı</th>
                          <th style={{ width: 100 }}>Fiyat</th>
                          <th className="d-none d-md-table-cell">Kategori</th>
                        </tr>
                      </thead>
                      <tbody>
                        {products?.length ? (
                          products.map((p) => (
                            <tr key={p.id}>
                              <td>#{p.id}</td>
                              <td>{p.name}</td>
                              <td>
                                ₺
                                {typeof p.price === "number"
                                  ? p.price.toLocaleString("tr-TR", {
                                      minimumFractionDigits: 2,
                                    })
                                  : p.price}
                              </td>
                              <td className="d-none d-md-table-cell">
                                {p.category || p.categoryName || "-"}
                              </td>
                            </tr>
                          ))
                        ) : (
                          <tr>
                            <td
                              colSpan="4"
                              className="text-muted text-center py-3"
                            >
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
                    Yerel Stoklar ({stocks?.length || 0})
                  </div>
                  <button
                    className="btn btn-sm btn-outline-secondary"
                    onClick={fetchStocks}
                    disabled={loading}
                  >
                    <i className="fas fa-refresh me-1"></i> Yenile
                  </button>
                </div>
                <div className="card-body p-2 p-md-3">
                  <div
                    className="table-responsive"
                    style={{ maxHeight: "400px", overflowY: "auto" }}
                  >
                    <table
                      className="table table-sm align-middle"
                      style={{ fontSize: "0.8rem" }}
                    >
                      <thead className="table-light sticky-top">
                        <tr>
                          <th style={{ width: 100 }}>Ürün ID</th>
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
                            <td
                              colSpan="2"
                              className="text-muted text-center py-3"
                            >
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
        </>
      )}

      {/* Mikro Stok Listesi Tab'ı */}
      {activeTab === "stokListesi" && (
        <div className="card border-0 shadow-sm">
          <div className="card-header bg-white d-flex justify-content-between align-items-center py-3">
            <div>
              <h6 className="mb-0 fw-bold">
                <i className="fas fa-database me-2 text-success"></i>
                Mikro API Stok Listesi
              </h6>
              <small className="text-muted">
                Toplam: {stokListesiMeta.totalCount} kayıt
              </small>
            </div>
            <button
              className="btn btn-primary btn-sm"
              onClick={() => loadStokListesi(1, 20)}
              disabled={loading}
            >
              <i className="fas fa-download me-1"></i> Mikro'dan Çek
            </button>
          </div>
          <div className="card-body p-0">
            <div className="table-responsive">
              <table
                className="table table-hover mb-0"
                style={{ fontSize: "0.85rem" }}
              >
                <thead className="table-light">
                  <tr>
                    <th>Stok Kodu</th>
                    <th>Stok Adı</th>
                    <th>Barkod</th>
                    <th className="text-end">Fiyat</th>
                    <th className="text-end">Miktar</th>
                    <th>Birim</th>
                    <th>Grup</th>
                  </tr>
                </thead>
                <tbody>
                  {stokListesi?.length ? (
                    stokListesi.map((s, idx) => (
                      <tr key={s.stokKod || idx}>
                        <td>
                          <code>{s.stokKod}</code>
                        </td>
                        <td>{s.stokAd}</td>
                        <td>
                          <small className="text-muted">
                            {s.barkod || "-"}
                          </small>
                        </td>
                        <td className="text-end">
                          ₺
                          {typeof (s.satisFiyati ?? s.fiyat) === "number"
                            ? (s.satisFiyati ?? s.fiyat).toLocaleString(
                                "tr-TR",
                                {
                                  minimumFractionDigits: 2,
                                },
                              )
                            : (s.satisFiyati ?? s.fiyat) || "0.00"}
                        </td>
                        <td
                          className={`text-end ${Number(s.depoMiktari ?? s.satilabilirMiktar ?? s.stokMiktar) <= 0 ? "text-danger fw-bold" : ""}`}
                        >
                          {s.depoMiktari ??
                            s.satilabilirMiktar ??
                            s.stokMiktar ??
                            0}
                        </td>
                        <td>{s.birim || "-"}</td>
                        <td>
                          <span className="badge bg-secondary">
                            {s.grupKod || "-"}
                          </span>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="7" className="text-center py-4 text-muted">
                        <i className="fas fa-info-circle me-2"></i>
                        Mikro API'den veri çekmek için yukarıdaki butona
                        tıklayın.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
          {/* Sayfalama */}
          {stokListesiMeta.totalCount > stokListesiMeta.pageSize && (
            <div className="card-footer bg-white d-flex justify-content-between align-items-center">
              <small className="text-muted">
                Sayfa {stokListesiMeta.page} /{" "}
                {Math.ceil(
                  stokListesiMeta.totalCount / stokListesiMeta.pageSize,
                )}
              </small>
              <div>
                <button
                  className="btn btn-sm btn-outline-secondary me-2"
                  disabled={stokListesiMeta.page <= 1 || loading}
                  onClick={() => handlePageChange(stokListesiMeta.page - 1)}
                >
                  <i className="fas fa-chevron-left"></i> Önceki
                </button>
                <button
                  className="btn btn-sm btn-outline-secondary"
                  disabled={
                    stokListesiMeta.page >=
                      Math.ceil(
                        stokListesiMeta.totalCount / stokListesiMeta.pageSize,
                      ) || loading
                  }
                  onClick={() => handlePageChange(stokListesiMeta.page + 1)}
                >
                  Sonraki <i className="fas fa-chevron-right"></i>
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Ayarlar Tab'ı */}
      {activeTab === "settings" && (
        <div className="card border-0 shadow-sm">
          <div className="card-header bg-white py-3">
            <h6 className="mb-0 fw-bold">
              <i className="fas fa-cog me-2 text-secondary"></i>
              ERP Bağlantı Ayarları
            </h6>
          </div>
          <div className="card-body">
            <div className="alert alert-info">
              <i className="fas fa-info-circle me-2"></i>
              <strong>Not:</strong> ERP bağlantı ayarları{" "}
              <code>appsettings.json</code> dosyasından yönetilmektedir.
              Değişiklik yapmak için sunucu yapılandırmasını güncelleyin.
            </div>
            <div className="row">
              <div className="col-md-6">
                <div className="mb-3">
                  <label className="form-label fw-semibold">API URL</label>
                  <input
                    type="text"
                    className="form-control"
                    value={connectionStatus?.apiUrl || "Bilinmiyor"}
                    disabled
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label fw-semibold">
                    Bağlantı Durumu
                  </label>
                  <input
                    type="text"
                    className={`form-control ${connectionStatus?.isConnected ? "text-success" : "text-danger"}`}
                    value={
                      connectionStatus?.isConnected ? "Bağlı" : "Bağlantı Yok"
                    }
                    disabled
                  />
                </div>
              </div>
              <div className="col-md-6">
                <div className="mb-3">
                  <label className="form-label fw-semibold">
                    Son Bağlantı Zamanı
                  </label>
                  <input
                    type="text"
                    className="form-control"
                    value={
                      connectionStatus?.timestamp
                        ? new Date(connectionStatus.timestamp).toLocaleString(
                            "tr-TR",
                          )
                        : "-"
                    }
                    disabled
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label fw-semibold">
                    Varsayılan Depo No
                  </label>
                  <input
                    type="text"
                    className="form-control"
                    value="1"
                    disabled
                  />
                  <small className="text-muted">
                    Tek depo kullanılmaktadır.
                  </small>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
