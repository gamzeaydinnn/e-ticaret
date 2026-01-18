import { useEffect, useState, useCallback, useRef } from "react";
import { AdminService } from "../../services/adminService";
import { CourierService } from "../../services/courierService";

// ============================================================
// ADMIN ORDERS - Sipariş Yönetimi
// ============================================================
// Bu sayfa admin panelinde siparişlerin yönetimini sağlar.
// Anlık güncelleme için 15 saniyelik polling mekanizması kullanır.
// ============================================================

// Polling aralığı (milisaniye) - 15 saniyede bir kontrol
const POLLING_INTERVAL = 15000;

export default function AdminOrders() {
  const [orders, setOrders] = useState([]);
  const [couriers, setCouriers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [assigningCourier, setAssigningCourier] = useState(false);

  // ============================================================
  // ANLIK GÜNCELLEME (POLLING) STATELERİ
  // ============================================================
  const [autoRefresh, setAutoRefresh] = useState(true); // Otomatik yenileme aktif mi?
  const [lastUpdate, setLastUpdate] = useState(null); // Son güncelleme zamanı
  const [isRefreshing, setIsRefreshing] = useState(false); // Yenileme animasyonu
  const pollingRef = useRef(null);

  // ============================================================
  // VERİ YÜKLEME FONKSİYONU
  // ============================================================
  const loadData = useCallback(async (showLoading = true) => {
    try {
      if (showLoading) setIsRefreshing(true);

      const couriersData = await CourierService.getAll();
      // Gerçek siparişleri backend'den çek
      const ordersData = await AdminService.getOrders();
      setOrders(Array.isArray(ordersData) ? ordersData : []);
      setCouriers(couriersData);
      setLastUpdate(new Date());
    } catch (error) {
      console.error("Veri yükleme hatası:", error);
    } finally {
      setLoading(false);
      setIsRefreshing(false);
    }
  }, []);

  // ============================================================
  // İLK YÜKLEME VE POLLING KURULUMU
  // ============================================================
  useEffect(() => {
    loadData();
  }, [loadData]);

  // Polling mekanizması - otomatik yenileme
  useEffect(() => {
    if (autoRefresh) {
      // Her POLLING_INTERVAL ms'de bir veri çek
      pollingRef.current = setInterval(() => {
        loadData(false); // Loading göstermeden sessiz güncelleme
      }, POLLING_INTERVAL);

      console.log("🔄 Sipariş otomatik yenileme aktif (15 saniye)");
    }

    // Cleanup - component unmount olduğunda veya autoRefresh değiştiğinde
    return () => {
      if (pollingRef.current) {
        clearInterval(pollingRef.current);
        console.log("⏹️ Sipariş otomatik yenileme durduruldu");
      }
    };
  }, [autoRefresh, loadData]);

  // ============================================================
  // FİLTRE STATE'LERİ
  // ============================================================
  const [statusFilter, setStatusFilter] = useState("all"); // Durum filtresi
  const [paymentFilter, setPaymentFilter] = useState("all"); // Ödeme filtresi

  // Filtrelenmiş siparişler
  const filteredOrders = orders.filter((order) => {
    // Durum filtresi
    if (statusFilter !== "all" && order.status !== statusFilter) {
      return false;
    }
    // Ödeme durumu filtresi
    if (paymentFilter !== "all") {
      const isPaid = order.paymentStatus === "paid" || order.isPaid;
      if (paymentFilter === "paid" && !isPaid) return false;
      if (paymentFilter === "pending" && isPaid) return false;
    }
    return true;
  });

  // ============================================================
  // SİPARİŞ İŞLEMLERİ
  // ============================================================

  const updateOrderStatus = async (orderId, newStatus) => {
    try {
      // Backend'e durumu güncelle ve listiyi yeniden çek
      await AdminService.updateOrderStatus(orderId, newStatus);
      const updated = await AdminService.getOrders();
      setOrders(Array.isArray(updated) ? updated : []);
    } catch (error) {
      console.error("Durum güncelleme hatası:", error);
    }
  };

  // ============================================================
  // KURYE ATAMA - Backend'e POST isteği gönderir
  // ============================================================
  const assignCourier = async (orderId, courierId) => {
    setAssigningCourier(true);
    try {
      // Backend'e kurye atama isteği gönder
      const updatedOrder = await AdminService.assignCourier(orderId, courierId);

      // Başarılı olursa listeyi güncelle
      if (updatedOrder) {
        // Tüm listeyi yeniden çek (en güncel veri için)
        const updated = await AdminService.getOrders();
        setOrders(Array.isArray(updated) ? updated : []);

        // Başarı bildirimi (opsiyonel)
        console.log(`✅ Kurye başarıyla atandı: Sipariş #${orderId}`);
      }
    } catch (error) {
      console.error("Kurye atama hatası:", error);
      // Kullanıcıya hata göster (ileride toast notification eklenebilir)
      alert(`Kurye atama başarısız: ${error.message || "Bilinmeyen hata"}`);
    } finally {
      setAssigningCourier(false);
    }
  };

  const getStatusColor = (status) => {
    const colorMap = {
      pending: "warning",
      preparing: "info",
      ready: "primary",
      assigned: "success",
      picked_up: "success",
      in_transit: "success",
      delivered: "secondary",
      cancelled: "danger",
    };
    return colorMap[status] || "secondary";
  };

  const getStatusText = (status) => {
    const statusMap = {
      pending: "Beklemede",
      preparing: "Hazırlanıyor",
      ready: "Hazır",
      assigned: "Kuryeye Atandı",
      picked_up: "Teslim Alındı",
      in_transit: "Yolda",
      delivered: "Teslim Edildi",
      cancelled: "İptal Edildi",
    };
    return statusMap[status] || status;
  };

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ minHeight: "60vh" }}
      >
        <div className="spinner-border text-primary"></div>
      </div>
    );
  }

  return (
    <div style={{ overflow: "hidden", maxWidth: "100%" }}>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 gap-2 px-1">
        <div>
          <h5 className="fw-bold text-dark mb-0" style={{ fontSize: "1rem" }}>
            <i
              className="fas fa-shopping-bag me-2"
              style={{ color: "#f97316" }}
            ></i>
            Sipariş Yönetimi
          </h5>
          <p
            className="text-muted mb-0 d-none d-sm-block"
            style={{ fontSize: "0.75rem" }}
          >
            Siparişleri takip edin
            {lastUpdate && (
              <span className="ms-2">
                • Son güncelleme: {lastUpdate.toLocaleTimeString("tr-TR")}
              </span>
            )}
          </p>
        </div>

        {/* Kontrol Butonları */}
        <div className="d-flex align-items-center gap-2">
          {/* Otomatik Yenileme Toggle */}
          <div className="form-check form-switch mb-0">
            <input
              className="form-check-input"
              type="checkbox"
              id="autoRefreshToggle"
              checked={autoRefresh}
              onChange={(e) => setAutoRefresh(e.target.checked)}
              style={{ cursor: "pointer" }}
            />
            <label
              className="form-check-label"
              htmlFor="autoRefreshToggle"
              style={{ fontSize: "0.7rem", cursor: "pointer" }}
            >
              Otomatik
            </label>
          </div>

          {/* Manuel Yenile Butonu */}
          <button
            onClick={() => loadData(true)}
            className="btn btn-outline-primary btn-sm px-2 py-1"
            style={{ fontSize: "0.75rem" }}
            disabled={isRefreshing}
          >
            <i
              className={`fas fa-sync-alt me-1 ${isRefreshing ? "fa-spin" : ""}`}
            ></i>
            Yenile
          </button>
        </div>
      </div>

      {/* Yeni Sipariş Bildirimi - Bekleyen sipariş varsa göster */}
      {orders.filter((o) => o.status === "pending").length > 0 && (
        <div
          className="alert alert-warning d-flex align-items-center mb-3 py-2"
          style={{ fontSize: "0.85rem" }}
        >
          <i
            className="fas fa-bell me-2"
            style={{ animation: "pulse 1s infinite" }}
          ></i>
          <span>
            <strong>
              {orders.filter((o) => o.status === "pending").length}
            </strong>{" "}
            adet bekleyen sipariş var!
          </span>
        </div>
      )}

      {/* Özet Kartlar - daha kompakt */}
      <div className="row g-2 mb-3 px-1">
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-warning text-white"
            style={{ borderRadius: "6px" }}
          >
            <div className="card-body text-center px-1 py-2">
              <h6 className="fw-bold mb-0">
                {orders.filter((o) => o.status === "pending").length}
              </h6>
              <small style={{ fontSize: "0.6rem" }}>Bekleyen</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-info text-white"
            style={{ borderRadius: "6px" }}
          >
            <div className="card-body text-center px-1 py-2">
              <h6 className="fw-bold mb-0">
                {orders.filter((o) => o.status === "preparing").length}
              </h6>
              <small style={{ fontSize: "0.6rem" }}>Hazırlanan</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-success text-white"
            style={{ borderRadius: "6px" }}
          >
            <div className="card-body text-center px-1 py-2">
              <h6 className="fw-bold mb-0">
                {
                  orders.filter((o) =>
                    ["assigned", "picked_up", "in_transit"].includes(o.status),
                  ).length
                }
              </h6>
              <small style={{ fontSize: "0.6rem" }}>Kuryede</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-secondary text-white"
            style={{ borderRadius: "6px" }}
          >
            <div className="card-body text-center px-1 py-2">
              <h6 className="fw-bold mb-0">
                {orders.filter((o) => o.status === "delivered").length}
              </h6>
              <small style={{ fontSize: "0.6rem" }}>Teslim</small>
            </div>
          </div>
        </div>
      </div>

      {/* ================================================================
          FİLTRE BUTONLARI - Durum ve Ödeme Durumu
          ================================================================ */}
      <div className="d-flex flex-wrap gap-2 mb-3 px-1">
        {/* Durum Filtresi */}
        <div className="btn-group btn-group-sm" role="group">
          <button
            className={`btn ${statusFilter === "all" ? "btn-primary" : "btn-outline-primary"}`}
            onClick={() => setStatusFilter("all")}
            style={{ fontSize: "0.7rem" }}
          >
            Tümü
          </button>
          <button
            className={`btn ${statusFilter === "pending" ? "btn-warning" : "btn-outline-warning"}`}
            onClick={() => setStatusFilter("pending")}
            style={{ fontSize: "0.7rem" }}
          >
            Bekleyen
          </button>
          <button
            className={`btn ${statusFilter === "preparing" ? "btn-info" : "btn-outline-info"}`}
            onClick={() => setStatusFilter("preparing")}
            style={{ fontSize: "0.7rem" }}
          >
            Hazırlanan
          </button>
          <button
            className={`btn ${statusFilter === "delivered" ? "btn-success" : "btn-outline-success"}`}
            onClick={() => setStatusFilter("delivered")}
            style={{ fontSize: "0.7rem" }}
          >
            Teslim
          </button>
        </div>

        {/* Ödeme Durumu Filtresi */}
        <div className="btn-group btn-group-sm" role="group">
          <button
            className={`btn ${paymentFilter === "all" ? "btn-dark" : "btn-outline-dark"}`}
            onClick={() => setPaymentFilter("all")}
            style={{ fontSize: "0.7rem" }}
          >
            <i className="fas fa-wallet me-1"></i>Tüm Ödemeler
          </button>
          <button
            className={`btn ${paymentFilter === "pending" ? "btn-danger" : "btn-outline-danger"}`}
            onClick={() => setPaymentFilter("pending")}
            style={{ fontSize: "0.7rem" }}
          >
            <i className="fas fa-clock me-1"></i>Ödeme Bekleyen
          </button>
          <button
            className={`btn ${paymentFilter === "paid" ? "btn-success" : "btn-outline-success"}`}
            onClick={() => setPaymentFilter("paid")}
            style={{ fontSize: "0.7rem" }}
          >
            <i className="fas fa-check me-1"></i>Ödendi
          </button>
        </div>
      </div>

      {/* Sipariş Listesi */}
      <div
        className="card border-0 shadow-sm mx-1"
        style={{ borderRadius: "10px" }}
      >
        <div className="card-header bg-white border-0 py-2 px-2 px-md-3">
          <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
            <i className="fas fa-list-alt me-2 text-primary"></i>
            Siparişler ({filteredOrders.length}/{orders.length})
          </h6>
        </div>
        <div className="card-body p-0">
          <div className="table-responsive" style={{ margin: "0" }}>
            <table
              className="table table-sm mb-0"
              style={{ fontSize: "0.7rem" }}
            >
              <thead className="bg-light">
                <tr>
                  <th className="px-1 py-2">Sipariş</th>
                  <th className="px-1 py-2 d-none d-md-table-cell">Müşteri</th>
                  <th className="px-1 py-2">Tutar</th>
                  <th className="px-1 py-2">Durum</th>
                  <th className="px-1 py-2 d-none d-sm-table-cell">Ödeme</th>
                  <th className="px-1 py-2 d-none d-sm-table-cell">Kurye</th>
                  <th className="px-1 py-2">İşlem</th>
                </tr>
              </thead>
              <tbody>
                {filteredOrders.length === 0 ? (
                  <tr>
                    <td colSpan="7" className="text-center py-4 text-muted">
                      <i className="fas fa-inbox fa-2x mb-2 d-block"></i>
                      {orders.length === 0
                        ? "Henüz sipariş bulunmuyor"
                        : "Filtreye uygun sipariş bulunamadı"}
                    </td>
                  </tr>
                ) : (
                  filteredOrders.map((order) => (
                    <tr key={order.id}>
                      <td className="px-1 py-2">
                        <span className="fw-bold">#{order.id}</span>
                        <br />
                        <small
                          className="text-muted d-none d-sm-inline"
                          style={{ fontSize: "0.6rem" }}
                        >
                          {new Date(order.orderDate).toLocaleDateString(
                            "tr-TR",
                          )}
                        </small>
                      </td>
                      <td className="px-1 py-2 d-none d-md-table-cell">
                        <span
                          className="fw-semibold text-truncate d-block"
                          style={{ maxWidth: "80px" }}
                        >
                          {order.customerName}
                        </span>
                      </td>
                      <td className="px-1 py-2">
                        <span
                          className="fw-bold text-success"
                          style={{ fontSize: "0.7rem" }}
                        >
                          {(order.totalAmount ?? 0).toFixed(0)}₺
                        </span>
                      </td>
                      <td className="px-1 py-2">
                        <span
                          className={`badge bg-${getStatusColor(order.status)}`}
                          style={{
                            fontSize: "0.55rem",
                            padding: "0.2em 0.4em",
                          }}
                        >
                          {getStatusText(order.status).substring(0, 6)}
                        </span>
                      </td>
                      {/* Ödeme Durumu Sütunu */}
                      <td className="px-1 py-2 d-none d-sm-table-cell">
                        {order.paymentStatus === "paid" || order.isPaid ? (
                          <span
                            className="badge bg-success"
                            style={{ fontSize: "0.55rem" }}
                          >
                            <i className="fas fa-check me-1"></i>Ödendi
                          </span>
                        ) : (
                          <span
                            className="badge bg-danger"
                            style={{ fontSize: "0.55rem" }}
                          >
                            <i className="fas fa-clock me-1"></i>Bekliyor
                          </span>
                        )}
                      </td>
                      <td className="px-1 py-2 d-none d-sm-table-cell">
                        {order.courierName ? (
                          <span
                            className="text-success"
                            style={{ fontSize: "0.65rem" }}
                          >
                            <i className="fas fa-motorcycle me-1"></i>
                            {order.courierName.split(" ")[0]}
                          </span>
                        ) : (
                          <span
                            className="text-muted"
                            style={{ fontSize: "0.6rem" }}
                          >
                            -
                          </span>
                        )}
                      </td>
                      <td className="px-1 py-2">
                        <div className="d-flex gap-1">
                          <button
                            onClick={() => setSelectedOrder(order)}
                            className="btn btn-outline-primary p-1"
                            style={{ fontSize: "0.6rem", lineHeight: 1 }}
                            title="Detay"
                          >
                            <i className="fas fa-eye"></i>
                          </button>
                          {order.status === "pending" && (
                            <button
                              onClick={() =>
                                updateOrderStatus(order.id, "preparing")
                              }
                              className="btn btn-warning p-1"
                              style={{ fontSize: "0.6rem", lineHeight: 1 }}
                            >
                              <i className="fas fa-clock"></i>
                            </button>
                          )}
                          {order.status === "preparing" && (
                            <button
                              onClick={() =>
                                updateOrderStatus(order.id, "ready")
                              }
                              className="btn btn-info p-1"
                              style={{ fontSize: "0.6rem", lineHeight: 1 }}
                            >
                              <i className="fas fa-check"></i>
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Sipariş Detay Modal */}
      {selectedOrder && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-dialog-centered mx-2">
            <div className="modal-content" style={{ borderRadius: "12px" }}>
              <div className="modal-header py-2 px-3">
                <h6 className="modal-title" style={{ fontSize: "0.9rem" }}>
                  <i className="fas fa-receipt me-2"></i>
                  Sipariş #{selectedOrder.id}
                </h6>
                <button
                  onClick={() => setSelectedOrder(null)}
                  className="btn-close btn-close-sm"
                ></button>
              </div>
              <div
                className="modal-body p-2 p-md-3"
                style={{
                  fontSize: "0.75rem",
                  maxHeight: "70vh",
                  overflowY: "auto",
                }}
              >
                <div className="row g-2">
                  <div className="col-12 col-md-6">
                    <h6 className="fw-bold mb-1" style={{ fontSize: "0.8rem" }}>
                      Müşteri
                    </h6>
                    <p className="mb-1">
                      <strong>Ad:</strong> {selectedOrder.customerName}
                    </p>
                    <p className="mb-1">
                      <strong>Tel:</strong> {selectedOrder.customerPhone}
                    </p>
                    <p className="mb-1 text-truncate">
                      <strong>Adres:</strong> {selectedOrder.address || "-"}
                    </p>
                  </div>
                  <div className="col-12 col-md-6">
                    <h6 className="fw-bold mb-1" style={{ fontSize: "0.8rem" }}>
                      <i className="fas fa-receipt me-1 text-primary"></i>
                      Sipariş Bilgileri
                    </h6>
                    <p className="mb-1">
                      <strong>Tarih:</strong>{" "}
                      {selectedOrder.orderDate
                        ? new Date(selectedOrder.orderDate).toLocaleDateString(
                            "tr-TR",
                          )
                        : "-"}
                    </p>
                    <p className="mb-1">
                      <strong>Tutar:</strong>{" "}
                      <span className="text-success fw-bold">
                        {(selectedOrder.totalAmount ?? 0).toFixed(2)} ₺
                      </span>
                    </p>
                    {/* Ödeme Yöntemi */}
                    <p className="mb-1">
                      <strong>Ödeme:</strong>{" "}
                      <span
                        className={`badge ${
                          selectedOrder.paymentMethod === "cash"
                            ? "bg-warning text-dark"
                            : selectedOrder.paymentMethod === "cash_card"
                              ? "bg-info"
                              : selectedOrder.paymentMethod === "bank_transfer"
                                ? "bg-primary"
                                : selectedOrder.paymentMethod === "card"
                                  ? "bg-success"
                                  : "bg-secondary"
                        }`}
                        style={{ fontSize: "0.6rem" }}
                      >
                        {selectedOrder.paymentMethod === "cash"
                          ? "💵 Kapıda Nakit"
                          : selectedOrder.paymentMethod === "cash_card"
                            ? "💳 Kapıda Kart"
                            : selectedOrder.paymentMethod === "bank_transfer"
                              ? "🏦 Havale/EFT"
                              : selectedOrder.paymentMethod === "card"
                                ? "💳 Online Kart"
                                : selectedOrder.paymentMethod ||
                                  "Belirtilmemiş"}
                      </span>
                    </p>
                    <p className="mb-1">
                      <strong>Durum:</strong>
                      <span
                        className={`badge bg-${getStatusColor(
                          selectedOrder.status,
                        )} ms-1`}
                        style={{ fontSize: "0.6rem" }}
                      >
                        {getStatusText(selectedOrder.status)}
                      </span>
                    </p>
                    {/* Sipariş Numarası varsa göster */}
                    {selectedOrder.orderNumber && (
                      <p className="mb-1">
                        <strong>Sipariş No:</strong>{" "}
                        <span
                          className="badge bg-dark"
                          style={{ fontSize: "0.6rem" }}
                        >
                          {selectedOrder.orderNumber}
                        </span>
                      </p>
                    )}
                  </div>
                </div>

                {/* ================================================================
                    ÜRÜNLER TABLOSU - VARYANT BİLGİSİ DAHİL
                    SKU, varyant başlığı varsa gösterilir
                    ================================================================ */}
                <h6
                  className="fw-bold mt-2 mb-1"
                  style={{ fontSize: "0.8rem" }}
                >
                  <i className="fas fa-box-open me-1 text-primary"></i>
                  Ürünler
                </h6>
                <div className="table-responsive">
                  <table
                    className="table table-sm mb-0"
                    style={{ fontSize: "0.7rem" }}
                  >
                    <thead className="bg-light">
                      <tr>
                        <th className="px-1">Ürün</th>
                        <th className="px-1 d-none d-sm-table-cell">SKU</th>
                        <th className="px-1 text-center">Adet</th>
                        <th className="px-1 text-end">Fiyat</th>
                      </tr>
                    </thead>
                    <tbody>
                      {(Array.isArray(selectedOrder.items)
                        ? selectedOrder.items
                        : []
                      ).map((item, index) => (
                        <tr key={index}>
                          <td className="px-1">
                            <div className="d-flex flex-column">
                              <span
                                className="text-truncate fw-semibold"
                                style={{ maxWidth: "120px" }}
                              >
                                {item.name || item.productName || "Ürün"}
                              </span>
                              {/* Varyant bilgisi varsa göster */}
                              {item.variantTitle && (
                                <span
                                  className="badge mt-1"
                                  style={{
                                    background:
                                      "linear-gradient(135deg, #10b981, #059669)",
                                    color: "white",
                                    fontSize: "0.55rem",
                                    padding: "2px 6px",
                                    borderRadius: "4px",
                                    width: "fit-content",
                                  }}
                                >
                                  {item.variantTitle}
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="px-1 d-none d-sm-table-cell">
                            {item.sku ? (
                              <span
                                className="badge bg-secondary"
                                style={{ fontSize: "0.55rem" }}
                              >
                                {item.sku}
                              </span>
                            ) : (
                              <span className="text-muted">-</span>
                            )}
                          </td>
                          <td className="px-1 text-center">
                            <span className="badge bg-primary">
                              {item.quantity}
                            </span>
                          </td>
                          <td className="px-1 text-end">
                            <span className="fw-bold text-success">
                              {(
                                (item.quantity ?? 0) *
                                (item.price ?? item.unitPrice ?? 0)
                              ).toFixed(0)}
                              ₺
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                    {/* Toplam satırı */}
                    <tfoot className="bg-light">
                      <tr>
                        <td colSpan="3" className="px-1 text-end fw-bold">
                          Toplam:
                        </td>
                        <td className="px-1 text-end">
                          <span
                            className="fw-bold text-success"
                            style={{ fontSize: "0.8rem" }}
                          >
                            {(selectedOrder.totalAmount ?? 0).toFixed(2)} ₺
                          </span>
                        </td>
                      </tr>
                    </tfoot>
                  </table>
                </div>

                {/* Kurye Atama */}
                {selectedOrder.status === "ready" &&
                  !selectedOrder.courierId && (
                    <div className="mt-2">
                      <h6
                        className="fw-bold mb-1"
                        style={{ fontSize: "0.8rem" }}
                      >
                        Kurye Ata
                      </h6>
                      <div className="d-flex gap-1 flex-wrap">
                        {couriers
                          .filter((c) => c.status === "active")
                          .map((courier) => (
                            <button
                              key={courier.id}
                              onClick={() =>
                                assignCourier(selectedOrder.id, courier.id)
                              }
                              disabled={assigningCourier}
                              className="btn btn-outline-success btn-sm px-2 py-1"
                              style={{ fontSize: "0.65rem" }}
                            >
                              <i className="fas fa-motorcycle me-1"></i>
                              {courier.name.split(" ")[0]}
                            </button>
                          ))}
                      </div>
                    </div>
                  )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
