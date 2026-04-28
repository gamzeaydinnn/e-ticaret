// ============================================================================
// SİPARİŞ GEÇMİŞİ SAYFASI
// Kayıtlı kullanıcılar için sipariş listesi ve detay görüntüleme
// Misafir siparişleri için email + sipariş numarası ile sorgulama
// Session bazlı misafir sipariş yönetimi (farklı tarayıcılarda farklı session)
// Real-time sipariş durumu güncellemeleri (SignalR)
// ============================================================================
import { useEffect, useState, useCallback, useRef } from "react";
import { OrderService } from "../services/orderService";
import { useAuth } from "../contexts/AuthContext";
import { CartService } from "../services/cartService";
import OrderDetailModal from "./OrderDetailModal";
import signalRService, { SignalREvents } from "../services/signalRService";

// ============================================================================
// BUTON DURUM GRUPLARI
// Sipariş durumuna göre 3 farklı aksiyon butonu gösterilir:
//   1. İptal Et       → Kargo çıkmamış (New/Pending/Confirmed/Paid)
//                        Otomatik POSNET reverse + stok iade
//   2. İptal Talebi   → Hazırlanıyor/Hazır/Kurye atandı (Preparing/Ready/Assigned)
//                        Admin onayı gerekli
//   3. İade Talebi    → Kargo yolda/teslim edildi (PickedUp → Completed)
//                        Müşteri hizmetleri inceleyecek
// ============================================================================
const AUTO_CANCEL_STATUSES = ["new", "pending", "confirmed", "paid"];
const CANCEL_REQUEST_STATUSES = ["preparing", "ready", "assigned"];
const REFUND_REQUEST_STATUSES = [
  "pickedup", "intransit", "outfordelivery", "shipped",
  "delivered", "completed", "deliveryfailed", "deliverypaymentpending"
];

// GÜVENLİK: Production'da debug log'ları kapalı
const DEBUG = process.env.NODE_ENV === "development";

export default function OrderHistory() {
  // ============================================================================
  // STATE YÖNETİMİ
  // ============================================================================
  const [orders, setOrders] = useState([]);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // İade/İptal talebi için state'ler
  const [refundModalOpen, setRefundModalOpen] = useState(false);
  const [refundOrderId, setRefundOrderId] = useState(null);
  const [refundReason, setRefundReason] = useState("");
  const [refundLoading, setRefundLoading] = useState(false);
  // Modal tipi: "auto_cancel" | "cancel_request" | "refund_request"
  const [refundModalType, setRefundModalType] = useState("refund_request");

  // Misafir sipariş sorgulaması için state'ler
  const [guestSearchMode, setGuestSearchMode] = useState(false);
  const [guestPhone, setGuestPhone] = useState("");
  const [guestOrderNumber, setGuestOrderNumber] = useState("");
  const [guestSearchLoading, setGuestSearchLoading] = useState(false);
  const [guestSearchError, setGuestSearchError] = useState(null);
  // Sorgulama yöntemi: "phone" veya "orderNumber"
  const [guestSearchTab, setGuestSearchTab] = useState("phone");

  // SignalR bağlantı durumu
  const [signalRConnected, setSignalRConnected] = useState(false);
  const signalRUnsubscribesRef = useRef([]);

  const { user, isAuthenticated } = useAuth();

  // ============================================================================
  // MİSAFİR SİPARİŞLERİNİ SESSION/LOCALSTORAGE'DAN YÜKLE
  // Önce sessionStorage kontrol edilir (mevcut oturum), sonra localStorage
  // Bu sayede farklı tarayıcılarda farklı misafir kullanıcılar ayrı siparişler görür
  // ============================================================================
  const loadGuestOrdersFromStorage = useCallback(() => {
    try {
      // Önce sessionStorage'dan dene (bu oturumdaki siparişler)
      let guestOrders = JSON.parse(
        sessionStorage.getItem("guestOrders") || "[]",
      );

      // SessionStorage boşsa localStorage'dan dene
      if (guestOrders.length === 0) {
        guestOrders = JSON.parse(localStorage.getItem("guestOrders") || "[]");

        // Session ID kontrolü - sadece bu session'a ait siparişleri filtrele
        const currentSessionId = CartService.getGuestSessionId?.();
        if (currentSessionId && guestOrders.length > 0) {
          // Session ID'si eşleşen veya session ID'si olmayan (eski siparişler) siparişleri filtrele
          const filteredOrders = guestOrders.filter(
            (o) => !o.sessionId || o.sessionId === currentSessionId,
          );
          if (filteredOrders.length !== guestOrders.length) {
            DEBUG && console.log(
              "[OrderHistory] Session bazlı filtreleme:",
              guestOrders.length,
              "->",
              filteredOrders.length,
            );
            guestOrders = filteredOrders;
          }
        }
      }

      if (guestOrders.length > 0) {
        DEBUG && console.log(
          "[OrderHistory] ✅ Storage'dan",
          guestOrders.length,
          "misafir siparişi bulundu",
        );
        // En yeniden eskiye sırala
        setOrders(
          guestOrders.sort(
            (a, b) => new Date(b.createdAt) - new Date(a.createdAt),
          ),
        );
        setGuestSearchMode(false); // Form değil, liste göster
        return true;
      }
    } catch (e) {
      console.warn("[OrderHistory] ⚠️ Storage okuma hatası:", e);
    }
    return false;
  }, []);

  // ============================================================================
  // SİPARİŞLERİ YÜKLEME FONKSİYONU
  // Kayıtlı kullanıcı için otomatik, misafir için localStorage + manuel sorgulama
  // ============================================================================
  const loadOrders = useCallback(async () => {
    // Token kontrolü - localStorage'da token var mı?
    const token = localStorage.getItem("token");
    const hasValidAuth = token && isAuthenticated && user;

    // Giriş yapmamış kullanıcılar için misafir modunu aç
    if (!hasValidAuth) {
      DEBUG && console.log(
        "[OrderHistory] Kullanıcı giriş yapmamış, misafir modu aktif",
      );

      // MİSAFİR SİPARİŞLERİNİ STORAGE'DAN OKU
      const hasGuestOrders = loadGuestOrdersFromStorage();
      if (!hasGuestOrders) {
        setGuestSearchMode(true);
      }

      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      DEBUG && console.log("[OrderHistory] Siparişler yükleniyor, userId:", user?.id);
      const orderList = await OrderService.list(user?.id);

      if (!orderList || orderList.length === 0) {
        DEBUG && console.log("[OrderHistory] Kullanıcının siparişi bulunamadı");
      } else {
        DEBUG && console.log(
          "[OrderHistory] ✅ Siparişler yüklendi:",
          orderList.length,
          "adet",
        );
      }

      setOrders(orderList || []);
    } catch (err) {
      console.error("[OrderHistory] ❌ Sipariş yükleme hatası:", err);

      // HTTP 400/401/403 hatası - misafir siparişlerini göster
      if (err?.status === 400 || err?.status === 401 || err?.status === 403) {
        DEBUG && console.log(
          "[OrderHistory] API hatası, misafir siparişleri deneniyor...",
        );
        const hasGuestOrders = loadGuestOrdersFromStorage();
        if (hasGuestOrders) {
          setError(null);
        } else {
          setError("Oturumunuz sona ermiş. Lütfen tekrar giriş yapın.");
          setGuestSearchMode(true);
        }
      } else {
        setError(
          "Siparişleriniz yüklenirken bir hata oluştu. Lütfen tekrar deneyin.",
        );
        // Hata durumunda da misafir siparişlerini göstermeye çalış
        loadGuestOrdersFromStorage();
      }
    } finally {
      setLoading(false);
    }
  }, [user, isAuthenticated, loadGuestOrdersFromStorage]);

  // Component mount olduğunda siparişleri yükle
  useEffect(() => {
    loadOrders();
  }, [loadOrders]);

  // ============================================================================
  // SIGNALR REAL-TIME BAĞLANTI VE EVENT DİNLEME
  // Sipariş durumu değişikliklerini anlık olarak günceller
  // Hem kayıtlı hem misafir kullanıcılar için çalışır
  // ============================================================================
  useEffect(() => {
    // SignalR bağlantısı kur
    const setupSignalR = async () => {
      try {
        // Token varsa müşteri olarak bağlan
        const token = localStorage.getItem("token") || localStorage.getItem("authToken");
        if (token || orders.length > 0) {
          await signalRService.connectCustomer();
          setSignalRConnected(true);
          DEBUG && console.log("[OrderHistory] SignalR bağlantısı kuruldu");
        }
      } catch (error) {
        console.warn("[OrderHistory] SignalR bağlantı hatası:", error);
        setSignalRConnected(false);
      }
    };

    setupSignalR();

    // Cleanup: component unmount olduğunda bağlantıları temizle
    return () => {
      signalRUnsubscribesRef.current.forEach(unsub => {
        if (typeof unsub === 'function') unsub();
      });
      signalRUnsubscribesRef.current = [];
    };
  }, [orders.length]);

  // SignalR event listener'ları
  useEffect(() => {
    if (!signalRConnected || orders.length === 0) return;

    // Önceki listener'ları temizle
    signalRUnsubscribesRef.current.forEach(unsub => {
      if (typeof unsub === 'function') unsub();
    });
    signalRUnsubscribesRef.current = [];

    // Sipariş durumu değişikliği dinle
    const handleOrderStatusChanged = (data) => {
      DEBUG && console.log("[OrderHistory] SignalR: OrderStatusChanged", data);
      
      // Gelen sipariş ID'si ile eşleşen siparişi güncelle
      setOrders(prevOrders => 
        prevOrders.map(order => {
          // ID eşleşmesi kontrol et (hem string hem number olabilir)
          const matchesId = String(order.id) === String(data.orderId) || 
                           order.orderNumber === data.orderNumber;
          
          if (matchesId) {
            DEBUG && console.log("[OrderHistory] Sipariş güncellendi:", order.orderNumber, "->", data.status);
            return {
              ...order,
              status: data.status || data.newStatus,
              statusText: data.statusText,
              updatedAt: data.timestamp || new Date().toISOString(),
            };
          }
          return order;
        })
      );

      // Eğer modal açıksa ve bu sipariş gösteriliyorsa, onu da güncelle
      setSelectedOrder(prev => {
        if (!prev) return prev;
        const matchesId = String(prev.id) === String(data.orderId) || 
                         prev.orderNumber === data.orderNumber;
        if (matchesId) {
          return {
            ...prev,
            status: data.status || data.newStatus,
            statusText: data.statusText,
            updatedAt: data.timestamp || new Date().toISOString(),
          };
        }
        return prev;
      });
    };

    // Ağırlık farkı tahsilatı/iadesi bildirimi
    const handleWeightChargeApplied = (data) => {
      DEBUG && console.log("[OrderHistory] SignalR: WeightChargeApplied", data);
      
      setOrders(prevOrders =>
        prevOrders.map(order => {
          const matchesId = String(order.id) === String(data.orderId) ||
                           order.orderNumber === data.orderNumber;
          if (matchesId) {
            return {
              ...order,
              finalAmount: data.finalAmount,
              weightDifferenceAmount: data.weightDifferenceAmount,
              weightAdjustmentStatus: data.status || 'completed',
            };
          }
          return order;
        })
      );

      // Modal'ı da güncelle
      setSelectedOrder(prev => {
        if (!prev) return prev;
        const matchesId = String(prev.id) === String(data.orderId) ||
                         prev.orderNumber === data.orderNumber;
        if (matchesId) {
          return {
            ...prev,
            finalAmount: data.finalAmount,
            weightDifferenceAmount: data.weightDifferenceAmount,
            weightAdjustmentStatus: data.status || 'completed',
          };
        }
        return prev;
      });
    };

    // Teslimat tamamlandı bildirimi
    const handleDeliveryCompleted = (data) => {
      DEBUG && console.log("[OrderHistory] SignalR: DeliveryCompleted", data);
      
      setOrders(prevOrders =>
        prevOrders.map(order => {
          const matchesId = String(order.id) === String(data.orderId) ||
                           order.orderNumber === data.orderNumber;
          if (matchesId) {
            return {
              ...order,
              status: 'delivered',
              deliveredAt: data.deliveredAt || new Date().toISOString(),
            };
          }
          return order;
        })
      );
    };

    // Event listener'ları kaydet
    const unsub1 = signalRService.onOrderStatusChanged(handleOrderStatusChanged);
    const unsub2 = signalRService.onWeightChargeApplied(handleWeightChargeApplied);
    const unsub3 = signalRService.onDeliveryStatusChanged(handleDeliveryCompleted);

    signalRUnsubscribesRef.current = [unsub1, unsub2, unsub3];

    DEBUG && console.log("[OrderHistory] SignalR event listener'ları kuruldu");

    // Cleanup
    return () => {
      signalRUnsubscribesRef.current.forEach(unsub => {
        if (typeof unsub === 'function') unsub();
      });
      signalRUnsubscribesRef.current = [];
    };
  }, [signalRConnected, orders.length]);

  // ============================================================================
  // MİSAFİR SİPARİŞ SORGULAMA
  // Telefon numarası veya sipariş numarası ile sipariş arama
  // Backend: GET /api/orders/guest-track
  // ============================================================================
  const handleGuestSearch = async (e) => {
    e.preventDefault();

    // Seçili sekmeye göre validasyon
    if (guestSearchTab === "phone" && !guestPhone?.trim()) {
      setGuestSearchError("Lütfen telefon numaranızı girin.");
      return;
    }
    if (guestSearchTab === "orderNumber" && !guestOrderNumber?.trim()) {
      setGuestSearchError("Lütfen sipariş numaranızı girin.");
      return;
    }

    // Telefon numarası formatı kontrolü (en az 10 rakam)
    if (guestSearchTab === "phone") {
      const digits = guestPhone.replace(/\D/g, "");
      if (digits.length < 10) {
        setGuestSearchError("Geçerli bir telefon numarası girin (en az 10 hane).");
        return;
      }
    }

    setGuestSearchLoading(true);
    setGuestSearchError(null);

    try {
      const params = {};
      if (guestSearchTab === "phone") {
        params.phone = guestPhone.trim();
      }
      if (guestSearchTab === "orderNumber") {
        params.orderNumber = guestOrderNumber.trim();
      }

      DEBUG && console.log("[OrderHistory] Misafir sipariş sorgulanıyor:", params);

      const results = await OrderService.trackGuestOrder(params);

      if (results && results.length > 0) {
        setOrders(results);
        setGuestSearchMode(false);
        DEBUG && console.log(
          "[OrderHistory] ✅ Misafir siparişleri bulundu:",
          results.length, "adet",
        );
      } else {
        setGuestSearchError(
          guestSearchTab === "phone"
            ? "Bu telefon numarasıyla eşleşen sipariş bulunamadı."
            : "Bu sipariş numarasıyla eşleşen sipariş bulunamadı."
        );
      }
    } catch (err) {
      console.error("[OrderHistory] ❌ Misafir sipariş arama hatası:", err);

      if (err?.status === 404) {
        setGuestSearchError("Bu bilgilerle eşleşen sipariş bulunamadı.");
      } else {
        setGuestSearchError("Sipariş araması sırasında bir hata oluştu.");
      }
    } finally {
      setGuestSearchLoading(false);
    }
  };

  // ============================================================================
  // SİPARİŞ DETAY GÖSTERME
  // ID veya orderNumber ile detay çekme (misafir için orderNumber kullan)
  // ============================================================================
  const handleShowDetail = async (orderId, orderNumber) => {
    setLoadingDetail(true);
    try {
      // Önce orderId ile dene, yoksa orderNumber ile dene
      const identifier = orderId || orderNumber;
      const { data } = await OrderService.getById(identifier);
      setSelectedOrder(data);
      setModalOpen(true);
    } catch (err) {
      console.error("[OrderHistory] Sipariş detay hatası:", err);
      setSelectedOrder(null);
      setModalOpen(false);

      // Misafir kullanıcı için hata mesajı
      if (!user) {
        alert(
          "Sipariş detayları görüntülenemedi. Lütfen giriş yaparak tekrar deneyin.",
        );
      }
    } finally {
      setLoadingDetail(false);
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold mb-6">Sipariş Geçmişi</h1>
      {/* ================================================================
          YÜKLEME DURUMU
          ================================================================ */}
      {loading && (
        <div className="d-flex justify-content-center py-5">
          <div className="spinner-border text-warning" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
        </div>
      )}
      {/* ================================================================
          HATA DURUMU
          ================================================================ */}
      {!loading && error && (
        <div className="alert alert-danger mb-4">
          <i className="fas fa-exclamation-circle me-2"></i>
          {error}
          <button className="btn btn-link p-0 ms-2" onClick={loadOrders}>
            Tekrar Dene
          </button>
        </div>
      )}
      {/* ================================================================
          MİSAFİR SİPARİŞ SORGULAMA FORMU
          Giriş yapmamış kullanıcılar için telefon no veya sipariş no ile arama
          Profesyonel tasarım: Tab yapısı, ikon, gradient başlık
          ================================================================ */}
      {!loading && guestSearchMode && (
        <div
          className="card border-0 shadow-sm mb-4"
          style={{ maxWidth: "520px", margin: "0 auto", borderRadius: "16px", overflow: "hidden" }}
        >
          {/* Gradient başlık */}
          <div
            style={{
              background: "linear-gradient(135deg, #FF8C00, #ff6b35)",
              padding: "24px 24px 20px",
              color: "white",
            }}
          >
            <h5 className="mb-1 fw-bold" style={{ fontSize: "1.15rem" }}>
              <i className="fas fa-search me-2"></i>
              Sipariş Sorgula
            </h5>
            <p className="mb-0" style={{ fontSize: "0.85rem", opacity: 0.9 }}>
              Telefon numaranız veya sipariş numaranız ile siparişinizi takip edin
            </p>
          </div>

          <div className="card-body p-4">
            {/* Sekme butonları */}
            <div
              className="d-flex mb-4 p-1"
              style={{
                backgroundColor: "#f5f5f5",
                borderRadius: "12px",
                gap: "4px",
              }}
            >
              <button
                type="button"
                className={`btn flex-grow-1 ${guestSearchTab === "phone" ? "btn-white shadow-sm" : ""}`}
                style={{
                  borderRadius: "10px",
                  fontSize: "0.85rem",
                  fontWeight: guestSearchTab === "phone" ? "600" : "400",
                  backgroundColor: guestSearchTab === "phone" ? "white" : "transparent",
                  border: "none",
                  padding: "10px 16px",
                  color: guestSearchTab === "phone" ? "#FF8C00" : "#666",
                  transition: "all 0.2s ease",
                }}
                onClick={() => { setGuestSearchTab("phone"); setGuestSearchError(null); }}
              >
                <i className="fas fa-phone-alt me-2"></i>
                Telefon No
              </button>
              <button
                type="button"
                className={`btn flex-grow-1 ${guestSearchTab === "orderNumber" ? "btn-white shadow-sm" : ""}`}
                style={{
                  borderRadius: "10px",
                  fontSize: "0.85rem",
                  fontWeight: guestSearchTab === "orderNumber" ? "600" : "400",
                  backgroundColor: guestSearchTab === "orderNumber" ? "white" : "transparent",
                  border: "none",
                  padding: "10px 16px",
                  color: guestSearchTab === "orderNumber" ? "#FF8C00" : "#666",
                  transition: "all 0.2s ease",
                }}
                onClick={() => { setGuestSearchTab("orderNumber"); setGuestSearchError(null); }}
              >
                <i className="fas fa-hashtag me-2"></i>
                Sipariş No
              </button>
            </div>

            <form onSubmit={handleGuestSearch}>
              {/* Telefon numarası girişi */}
              {guestSearchTab === "phone" && (
                <div className="mb-3">
                  <label className="form-label fw-semibold" style={{ fontSize: "0.9rem" }}>
                    <i className="fas fa-phone-alt me-2" style={{ color: "#FF8C00" }}></i>
                    Telefon Numarası
                  </label>
                  <div className="input-group">
                    <span
                      className="input-group-text"
                      style={{
                        backgroundColor: "#FFF5E6",
                        border: "2px solid #FFE0B2",
                        borderRight: "none",
                        color: "#FF8C00",
                        fontWeight: "600",
                      }}
                    >
                      +90
                    </span>
                    <input
                      type="tel"
                      className="form-control"
                      placeholder="5XX XXX XX XX"
                      value={guestPhone}
                      onChange={(e) => setGuestPhone(e.target.value)}
                      disabled={guestSearchLoading}
                      style={{
                        border: "2px solid #FFE0B2",
                        borderLeft: "none",
                        fontSize: "1rem",
                        padding: "12px 16px",
                      }}
                      maxLength={15}
                    />
                  </div>
                  <small className="text-muted mt-1 d-block">
                    Sipariş verirken kullandığınız telefon numarasını girin
                  </small>
                </div>
              )}

              {/* Sipariş numarası girişi */}
              {guestSearchTab === "orderNumber" && (
                <div className="mb-3">
                  <label className="form-label fw-semibold" style={{ fontSize: "0.9rem" }}>
                    <i className="fas fa-hashtag me-2" style={{ color: "#FF8C00" }}></i>
                    Sipariş Numarası
                  </label>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="ORD-12345"
                    value={guestOrderNumber}
                    onChange={(e) => setGuestOrderNumber(e.target.value)}
                    disabled={guestSearchLoading}
                    style={{
                      border: "2px solid #FFE0B2",
                      fontSize: "1rem",
                      padding: "12px 16px",
                      borderRadius: "8px",
                    }}
                  />
                  <small className="text-muted mt-1 d-block">
                    Sipariş onay sayfasında veya e-posta ile gönderilen sipariş numaranız
                  </small>
                </div>
              )}

              {/* Hata mesajı */}
              {guestSearchError && (
                <div
                  className="alert py-2 px-3 mb-3 d-flex align-items-center"
                  style={{
                    backgroundColor: "#FFF3E0",
                    border: "1px solid #FFE0B2",
                    borderRadius: "10px",
                    fontSize: "0.85rem",
                  }}
                >
                  <i className="fas fa-exclamation-triangle me-2" style={{ color: "#FF8C00" }}></i>
                  <span style={{ color: "#E65100" }}>{guestSearchError}</span>
                </div>
              )}

              {/* Sorgula butonu */}
              <button
                type="submit"
                className="btn w-100"
                style={{
                  background: "linear-gradient(135deg, #FF8C00, #ff6b35)",
                  color: "white",
                  fontWeight: "bold",
                  padding: "14px",
                  borderRadius: "12px",
                  fontSize: "1rem",
                  border: "none",
                  boxShadow: "0 4px 15px rgba(255, 140, 0, 0.3)",
                  transition: "all 0.2s ease",
                }}
                disabled={guestSearchLoading}
              >
                {guestSearchLoading ? (
                  <>
                    <span className="spinner-border spinner-border-sm me-2"></span>
                    Aranıyor...
                  </>
                ) : (
                  <>
                    <i className="fas fa-search me-2"></i>
                    Sipariş Sorgula
                  </>
                )}
              </button>
            </form>

            {/* Giriş yapma önerisi */}
            <div className="mt-4 pt-3 border-top text-center">
              <p className="small text-muted mb-2">
                Tüm siparişlerinize kolayca erişmek için
              </p>
              <a
                href="/login"
                className="btn btn-outline-secondary btn-sm"
                style={{ borderRadius: "8px", padding: "8px 20px" }}
              >
                <i className="fas fa-sign-in-alt me-1"></i>
                Giriş Yapın
              </a>
            </div>
          </div>
        </div>
      )}
      {/* ================================================================
          SİPARİŞ LİSTESİ (Kayıtlı kullanıcı ve misafir için)
          ================================================================ */}
      {!loading && !guestSearchMode && orders.length === 0 ? (
        <div className="text-center py-5">
          <i className="fas fa-shopping-bag fa-3x text-muted mb-3"></i>
          <p className="text-muted">Henüz siparişiniz bulunmuyor.</p>
          <a href="/" className="btn btn-primary">
            <i className="fas fa-shopping-cart me-2"></i>
            Alışverişe Başla
          </a>
        </div>
      ) : (
        !loading &&
        !guestSearchMode && (
          <>
            {/* Misafir kullanıcı için bilgi mesajı */}
            {!user && orders.length > 0 && (
              <div
                className="alert alert-info mb-4 d-flex align-items-start"
                style={{
                  backgroundColor: "#FFF5E6",
                  border: "1px solid #FFE0B2",
                  borderRadius: "12px",
                }}
              >
                <i
                  className="fas fa-info-circle me-2 mt-1"
                  style={{ color: "#FF8C00" }}
                ></i>
                <div className="flex-grow-1">
                  <strong>Misafir Siparişleriniz</strong>
                  <p className="mb-0 small mt-1">
                    Bu siparişler sorgulamanıza göre listelenmiştir. Tüm
                    siparişlerinize erişmek için{" "}
                    <a href="/login" style={{ color: "#FF8C00" }}>
                      giriş yapın
                    </a>
                    .
                  </p>
                </div>
                <button
                  className="btn btn-sm ms-2"
                  style={{
                    backgroundColor: "#FF8C00",
                    color: "white",
                    borderRadius: "8px",
                    whiteSpace: "nowrap",
                    fontSize: "0.8rem",
                  }}
                  onClick={() => {
                    setGuestSearchMode(true);
                    setOrders([]);
                    setGuestSearchError(null);
                  }}
                >
                  <i className="fas fa-search me-1"></i>
                  Yeni Sorgulama
                </button>
              </div>
            )}
            <ul className="space-y-4">
              {orders.map((order) => (
                <li
                  key={order.id || order.orderNumber}
                  className="p-4 border rounded shadow"
                >
                  <div className="d-flex justify-content-between align-items-center">
                    <div>
                      <div className="fw-bold">
                        Sipariş #{order.orderNumber || order.id}
                      </div>
                      <div className="text-muted small">
                        {order.orderDate || order.createdAt
                          ? new Date(
                              order.orderDate || order.createdAt,
                            ).toLocaleDateString("tr-TR")
                          : ""}
                      </div>
                      <div>
                        Tutar: ₺
                        {(
                          order.totalAmount ||
                          order.finalPrice ||
                          order.totalPrice ||
                          0
                        ).toFixed(2)}
                      </div>
                      <div>
                        Durum:
                        <span
                          className={`ms-1 badge ${
                            order.status?.toLowerCase() === "delivered"
                              ? "bg-success"
                              : order.status?.toLowerCase() === "cancelled"
                                ? "bg-danger"
                                : order.status?.toLowerCase() === "pending"
                                  ? "bg-warning text-dark"
                                  : "bg-info"
                          }`}
                        >
                          {order.status}
                        </span>
                      </div>
                    </div>
                    <div>
                      <button
                        className="btn btn-info btn-sm me-2"
                        onClick={() =>
                          handleShowDetail(order.id, order.orderNumber)
                        }
                      >
                        <i className="fas fa-eye me-1"></i>
                        Detay
                      </button>
                      {/* ═══════════════════════════════════════════════════
                          BUTON 1: İPTAL ET
                          New/Pending/Confirmed/Paid → Otomatik iptal + POSNET reverse
                          ═══════════════════════════════════════════════════ */}
                      {AUTO_CANCEL_STATUSES.includes(
                        order.status?.toLowerCase(),
                      ) && (
                        <button
                          className="btn btn-danger btn-sm"
                          onClick={() => {
                            setRefundOrderId(order.id);
                            setRefundReason("");
                            setRefundModalType("auto_cancel");
                            setRefundModalOpen(true);
                          }}
                        >
                          <i className="fas fa-times me-1"></i>
                          İptal Et
                        </button>
                      )}
                      {/* ═══════════════════════════════════════════════════
                          BUTON 2: İPTAL TALEBİ
                          Preparing/Ready/Assigned → Admin onayı gerekli
                          ═══════════════════════════════════════════════════ */}
                      {CANCEL_REQUEST_STATUSES.includes(
                        order.status?.toLowerCase(),
                      ) && (
                        <button
                          className="btn btn-outline-danger btn-sm"
                          onClick={() => {
                            setRefundOrderId(order.id);
                            setRefundReason("");
                            setRefundModalType("cancel_request");
                            setRefundModalOpen(true);
                          }}
                        >
                          <i className="fas fa-exclamation-triangle me-1"></i>
                          İptal Talebi
                        </button>
                      )}
                      {/* ═══════════════════════════════════════════════════
                          BUTON 3: WHATSAPP İLE İLETİŞİM
                          Kargo yoldayken/teslim edildikten sonra müşteri
                          doğrudan iptal edemez → WhatsApp'a yönlendirilir
                          ═══════════════════════════════════════════════════ */}
                      {REFUND_REQUEST_STATUSES.includes(
                        order.status?.toLowerCase(),
                      ) && (
                        <a
                          className="btn btn-success btn-sm"
                          href={`https://wa.me/905334783072?text=${encodeURIComponent(
                            `Merhaba, ${order.orderNumber || '#' + order.id} numaralı siparişim için iade/iptal talebi oluşturmak istiyorum.`
                          )}`}
                          target="_blank"
                          rel="noopener noreferrer"
                        >
                          <i className="fab fa-whatsapp me-1"></i>
                          WhatsApp ile İptal/İade
                        </a>
                      )}
                      {/* İade edilmiş siparişler için bilgi badge */}
                      {order.status?.toLowerCase() === "refunded" && (
                        <span className="badge bg-secondary">
                          <i className="fas fa-check-circle me-1"></i>
                          İade Edildi
                        </span>
                      )}
                      {/* İptal edilmiş siparişler için bilgi badge */}
                      {order.status?.toLowerCase() === "cancelled" && (
                        <span className="badge bg-danger">
                          <i className="fas fa-ban me-1"></i>
                          İptal Edildi
                        </span>
                      )}
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          </>
        )
      )}{" "}
      <OrderDetailModal
        show={modalOpen}
        onHide={() => {
          setModalOpen(false);
          setSelectedOrder(null);
        }}
        order={selectedOrder}
      />

      {/* ═══════════════════════════════════════════════════════════════════════
          İPTAL / İADE TALEBİ MODAL
          Modal tipi (refundModalType) bazında farklı başlık, açıklama ve davranış:
          - auto_cancel:    Otomatik iptal + POSNET reverse (para iadesi garantili)
          - cancel_request: Admin onayı gerektiren iptal talebi
          - refund_request: Müşteri hizmetleri incelemesi gerektiren iade talebi
          ═══════════════════════════════════════════════════════════════════════ */}
      {refundModalOpen && (
        <div className="modal d-block" tabIndex="-1" style={{ background: "rgba(0,0,0,0.5)" }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
              <div className={`modal-header ${
                refundModalType === "auto_cancel"
                  ? "bg-danger text-white"
                  : refundModalType === "cancel_request"
                    ? "bg-warning text-dark"
                    : "bg-info text-white"
              }`}>
                <h5 className="modal-title">
                  <i className={`fas ${
                    refundModalType === "auto_cancel"
                      ? "fa-times-circle"
                      : refundModalType === "cancel_request"
                        ? "fa-exclamation-triangle"
                        : "fa-undo"
                  } me-2`}></i>
                  {refundModalType === "auto_cancel"
                    ? "Sipariş İptali"
                    : refundModalType === "cancel_request"
                      ? "İptal Talebi Oluştur"
                      : "İade Talebi Oluştur"}
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setRefundModalOpen(false)}
                  disabled={refundLoading}
                ></button>
              </div>
              <div className="modal-body">
                {/* ── Tip bazlı bilgilendirme mesajı ── */}
                {refundModalType === "auto_cancel" && (
                  <div className="alert alert-danger mb-3">
                    <i className="fas fa-info-circle me-2"></i>
                    <strong>Otomatik İptal:</strong> Siparişiniz henüz hazırlanmadığı için
                    anında iptal edilecek ve ödemeniz kartınıza iade edilecektir.
                  </div>
                )}
                {refundModalType === "cancel_request" && (
                  <div className="alert alert-warning mb-3">
                    <i className="fas fa-info-circle me-2"></i>
                    <strong>İptal Talebi:</strong> Siparişiniz hazırlık aşamasında olduğu için
                    iptal talebiniz müşteri hizmetlerimize iletilecektir. En kısa sürede
                    işlem yapılacak ve bilgilendirileceksiniz.
                  </div>
                )}
                {refundModalType === "refund_request" && (
                  <div className="alert alert-info mb-3">
                    <i className="fas fa-info-circle me-2"></i>
                    <strong>İade Talebi:</strong> Siparişiniz kargoya verildiği/teslim edildiği
                    için iade talebiniz müşteri hizmetlerimiz tarafından incelenecektir.
                    Onaylanması halinde para iadeniz başlatılacaktır.
                  </div>
                )}
                <div className="mb-3">
                  <label className="form-label fw-bold">
                    {refundModalType === "auto_cancel"
                      ? "İptal Sebebi *"
                      : refundModalType === "cancel_request"
                        ? "İptal Talebi Sebebi *"
                        : "İade Sebebi *"}
                  </label>
                  <textarea
                    className="form-control"
                    rows="3"
                    placeholder={
                      refundModalType === "auto_cancel"
                        ? "Lütfen iptal sebebinizi açıklayınız..."
                        : refundModalType === "cancel_request"
                          ? "Lütfen iptal talebi sebebinizi açıklayınız..."
                          : "Lütfen iade sebebinizi açıklayınız..."
                    }
                    value={refundReason}
                    onChange={(e) => setRefundReason(e.target.value)}
                    disabled={refundLoading}
                    maxLength={1000}
                  ></textarea>
                  <div className="form-text">{refundReason.length}/1000 karakter</div>
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setRefundModalOpen(false)}
                  disabled={refundLoading}
                >
                  Vazgeç
                </button>
                <button
                  type="button"
                  className={`btn ${
                    refundModalType === "auto_cancel"
                      ? "btn-danger"
                      : refundModalType === "cancel_request"
                        ? "btn-warning"
                        : "btn-info text-white"
                  }`}
                  disabled={!refundReason.trim() || refundLoading}
                  onClick={async () => {
                    // auto_cancel için onay iste
                    if (refundModalType === "auto_cancel") {
                      if (!window.confirm(
                        "Siparişiniz iptal edilecek ve para iadeniz başlatılacaktır. Onaylıyor musunuz?"
                      )) {
                        return;
                      }
                    }
                    setRefundLoading(true);
                    try {
                      const result = await OrderService.createRefundRequest(
                        refundOrderId,
                        {
                          reason: refundReason.trim(),
                          refundType: refundModalType === "refund_request" ? "return" : "cancel"
                        }
                      );
                      const resp = result?.data || result;
                      setRefundModalOpen(false);

                      if (resp?.autoCancelled) {
                        // Otomatik iptal edildi - listeyi güncelle
                        setOrders((prev) =>
                          prev.map((o) =>
                            o.id === refundOrderId
                              ? { ...o, status: "Cancelled" }
                              : o,
                          ),
                        );
                        alert(resp?.message || "Siparişiniz iptal edildi ve para iadeniz başlatıldı.");
                      } else {
                        // Admin onayı bekliyor
                        alert(
                          resp?.message ||
                          (refundModalType === "cancel_request"
                            ? "İptal talebiniz alınmıştır. Müşteri hizmetlerimiz en kısa sürede işlem yapacaktır."
                            : "İade talebiniz alınmıştır. Müşteri hizmetlerimiz en kısa sürede inceleyecektir.")
                        );
                      }
                      // Sipariş listesini yeniden yükle
                      loadOrders();
                    } catch (err) {
                      const msg =
                        err?.response?.data?.message ||
                        err?.message ||
                        "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.";
                      alert(msg);
                    } finally {
                      setRefundLoading(false);
                    }
                  }}
                >
                  {refundLoading ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2"></span>
                      İşleniyor...
                    </>
                  ) : (
                    <>
                      <i className={`fas ${
                        refundModalType === "auto_cancel"
                          ? "fa-times"
                          : "fa-paper-plane"
                      } me-1`}></i>
                      {refundModalType === "auto_cancel"
                        ? "İptal Et ve Para İadesi Al"
                        : refundModalType === "cancel_request"
                          ? "İptal Talebi Gönder"
                          : "İade Talebi Gönder"}
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {loadingDetail && (
        <div
          className="position-fixed top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center"
          style={{ background: "rgba(255,255,255,0.5)", zIndex: 9999 }}
        >
          <div
            className="spinner-border text-warning"
            style={{ width: "4rem", height: "4rem" }}
          ></div>
        </div>
      )}
    </div>
  );
}
