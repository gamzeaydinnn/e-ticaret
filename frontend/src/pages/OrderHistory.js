// ============================================================================
// SİPARİŞ GEÇMİŞİ SAYFASI
// Kayıtlı kullanıcılar için sipariş listesi ve detay görüntüleme
// Misafir siparişleri için email + sipariş numarası ile sorgulama
// Session bazlı misafir sipariş yönetimi (farklı tarayıcılarda farklı session)
// ============================================================================
import { useEffect, useState, useCallback } from "react";
import { OrderService } from "../services/orderService";
import { useAuth } from "../contexts/AuthContext";
import { CartService } from "../services/cartService";
import OrderDetailModal from "./OrderDetailModal";

/**
 * Sipariş iptal işlemi için yardımcı fonksiyon
 * @param {number} orderId - İptal edilecek sipariş ID
 * @param {Function} onSuccess - Başarılı işlem callback'i
 * @param {Function} onError - Hata callback'i
 */
async function cancelOrder(orderId, onSuccess, onError) {
  try {
    await OrderService.cancel(orderId);
    onSuccess && onSuccess();
  } catch (err) {
    console.error("[OrderHistory] Sipariş iptal hatası:", err);
    onError && onError(err);
  }
}

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

  // Misafir sipariş sorgulaması için state'ler
  const [guestSearchMode, setGuestSearchMode] = useState(false);
  const [guestEmail, setGuestEmail] = useState("");
  const [guestOrderNumber, setGuestOrderNumber] = useState("");
  const [guestSearchLoading, setGuestSearchLoading] = useState(false);
  const [guestSearchError, setGuestSearchError] = useState(null);

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
            console.log(
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
        console.log(
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
      console.log(
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
      console.log("[OrderHistory] Siparişler yükleniyor, userId:", user?.id);
      const orderList = await OrderService.list(user?.id);

      if (!orderList || orderList.length === 0) {
        console.log("[OrderHistory] Kullanıcının siparişi bulunamadı");
      } else {
        console.log(
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
        console.log(
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
  // MİSAFİR SİPARİŞ SORGULAMA
  // Email ve sipariş numarası ile sipariş arama
  // ============================================================================
  const handleGuestSearch = async (e) => {
    e.preventDefault();

    if (!guestEmail?.trim() || !guestOrderNumber?.trim()) {
      setGuestSearchError("Lütfen e-posta ve sipariş numaranızı girin.");
      return;
    }

    // Basit email validasyonu
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(guestEmail.trim())) {
      setGuestSearchError("Geçerli bir e-posta adresi girin.");
      return;
    }

    setGuestSearchLoading(true);
    setGuestSearchError(null);

    try {
      console.log("[OrderHistory] Misafir sipariş sorgulanıyor:", {
        email: guestEmail,
        orderNumber: guestOrderNumber,
      });

      const result = await OrderService.findGuestOrder(
        guestEmail.trim(),
        guestOrderNumber.trim(),
      );

      if (result) {
        // Tek sipariş döndü, listeye ekle
        setOrders([result]);
        setGuestSearchMode(false);
        console.log(
          "[OrderHistory] ✅ Misafir siparişi bulundu:",
          result.orderNumber,
        );
      } else {
        setGuestSearchError("Sipariş bulunamadı. Bilgilerinizi kontrol edin.");
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
          Giriş yapmamış kullanıcılar için email + sipariş no ile arama
          ================================================================ */}
      {!loading && guestSearchMode && (
        <div
          className="card mb-4"
          style={{ maxWidth: "500px", margin: "0 auto" }}
        >
          <div className="card-body">
            <h5 className="card-title mb-3">
              <i
                className="fas fa-search me-2"
                style={{ color: "#FF8C00" }}
              ></i>
              Sipariş Sorgula
            </h5>
            <p className="text-muted small mb-3">
              Siparişinizi görüntülemek için sipariş verirken kullandığınız
              e-posta adresinizi ve sipariş numaranızı girin.
            </p>

            <form onSubmit={handleGuestSearch}>
              <div className="mb-3">
                <label className="form-label">E-posta Adresi</label>
                <input
                  type="email"
                  className="form-control"
                  placeholder="ornek@email.com"
                  value={guestEmail}
                  onChange={(e) => setGuestEmail(e.target.value)}
                  disabled={guestSearchLoading}
                  required
                />
              </div>

              <div className="mb-3">
                <label className="form-label">Sipariş Numarası</label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="ORD-12345"
                  value={guestOrderNumber}
                  onChange={(e) => setGuestOrderNumber(e.target.value)}
                  disabled={guestSearchLoading}
                  required
                />
              </div>

              {guestSearchError && (
                <div className="alert alert-warning py-2 mb-3">
                  <i className="fas fa-exclamation-triangle me-2"></i>
                  {guestSearchError}
                </div>
              )}

              <button
                type="submit"
                className="btn w-100"
                style={{
                  backgroundColor: "#FF8C00",
                  color: "white",
                  fontWeight: "bold",
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
                    Sipariş Ara
                  </>
                )}
              </button>
            </form>

            {/* Giriş yapma önerisi */}
            <div className="mt-3 pt-3 border-top text-center">
              <p className="small text-muted mb-1">
                Tüm siparişlerinizi görmek için
              </p>
              <a href="/login" className="btn btn-outline-secondary btn-sm">
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
                className="alert alert-info mb-4"
                style={{
                  backgroundColor: "#FFF5E6",
                  border: "1px solid #FFE0B2",
                }}
              >
                <i
                  className="fas fa-info-circle me-2"
                  style={{ color: "#FF8C00" }}
                ></i>
                <strong>Misafir Siparişleriniz</strong>
                <p className="mb-0 small mt-1">
                  Bu siparişler sadece bu cihazda görüntülenebilir. Tüm
                  siparişlerinize erişmek için{" "}
                  <a href="/login" style={{ color: "#FF8C00" }}>
                    giriş yapın
                  </a>
                  .
                </p>
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
                      {["pending", "processing", "new", "confirmed"].includes(
                        order.status?.toLowerCase(),
                      ) && (
                        <button
                          className="btn btn-danger btn-sm"
                          onClick={async () => {
                            if (
                              !window.confirm(
                                "Siparişinizi iptal etmek istediğinize emin misiniz?",
                              )
                            ) {
                              return;
                            }
                            await cancelOrder(
                              order.id,
                              () => {
                                setOrders((prev) =>
                                  prev.map((o) =>
                                    o.id === order.id
                                      ? { ...o, status: "Cancelled" }
                                      : o,
                                  ),
                                );
                              },
                              (err) => {
                                alert(
                                  "Sipariş iptal edilemedi: " +
                                    (err?.message || "Bilinmeyen hata"),
                                );
                              },
                            );
                          }}
                        >
                          <i className="fas fa-times me-1"></i>
                          İptal Et
                        </button>
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
