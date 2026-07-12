// ==========================================================================
// OrderTracking.jsx - Müşteri Sipariş Takip Ekranı (Geliştirilmiş)
// ==========================================================================
// SignalR entegrasyonu ile real-time sipariş takibi.
// Stepper UI ile adım adım sipariş durumu gösterimi.
// ==========================================================================

import { useEffect, useState, useCallback } from "react";
import { OrderService } from "../services/orderService";
import signalRService, { ConnectionState } from "../services/signalRService";
import OrderActions from "./orders/OrderActions";
import { openWhatsAppSupportAsync } from "../utils/customerSupport";
import {
  isActiveOrder,
  isCompletedOrder,
  isCancelledOrRefundedOrder,
  countActiveOrders,
  countHistoryOrders,
  countCancelledOrders,
  normalizeStatus,
} from "../utils/orderCancelPolicy";
import {
  getOrderItemsList,
  getOrderItemLineTotal,
  getOrderItemUnitLabel,
  getOrderDisplayTotals,
  formatTry,
} from "../utils/orderDisplayTotals";
import "./orders/OrderActions.css";
import "./OrderTracking.css";

// ==========================================================================
// DURUM TANIMLARI VE RENKLER
// ==========================================================================

/**
 * Sipariş durumları ve özellikleri
 * NEDEN: Backend ile tutarlı durum yönetimi için merkezi tanımlama
 */
const ORDER_STATUSES = {
  // Sipariş oluşturma aşaması
  pending: {
    step: 0,
    label: "Siparişiniz Alındı",
    shortLabel: "Alındı",
    description: "Siparişiniz başarıyla oluşturuldu ve onay bekliyor",
    icon: "fa-shopping-cart",
    color: "#ffc107",
    bgColor: "#fff3cd",
  },
  new: {
    step: 0,
    label: "Siparişiniz Alındı",
    shortLabel: "Alındı",
    description: "Siparişiniz başarıyla oluşturuldu",
    icon: "fa-shopping-cart",
    color: "#ffc107",
    bgColor: "#fff3cd",
  },
  // Ödeme aşaması (kart ile ödeme başarılı)
  paid: {
    step: 0,
    label: "Ödeme Alındı",
    shortLabel: "Ödendi",
    description: "Ödemeniz başarıyla alındı, siparişiniz onay bekliyor",
    icon: "fa-credit-card",
    color: "#0ea5e9",
    bgColor: "#e0f2fe",
  },
  // KGL / provizyon: tutar bloke edildi, henüz tahsil edilmedi
  preauthorized: {
    step: 0,
    label: "Ödeme Onaylandı (Provizyon)",
    shortLabel: "Provizyon",
    description:
      "Ödemeniz için provizyon alındı. Tartı/ürün kesinleştiğinde tahsilat yapılacak.",
    icon: "fa-shield-alt",
    color: "#0ea5e9",
    bgColor: "#e0f2fe",
  },
  // Tartı bazlı ürünlerde ağırlık kesinleşmesi bekleniyor
  weightpending: {
    step: 2,
    label: "Tartı Bekleniyor",
    shortLabel: "Tartılıyor",
    description:
      "Tartı bazlı ürünleriniz hazırlanıyor, kesin tutar tartı sonrası belirlenecek",
    icon: "fa-balance-scale",
    color: "#fd7e14",
    bgColor: "#ffe5d0",
  },
  weight_pending: {
    step: 2,
    label: "Tartı Bekleniyor",
    shortLabel: "Tartılıyor",
    description:
      "Tartı bazlı ürünleriniz hazırlanıyor, kesin tutar tartı sonrası belirlenecek",
    icon: "fa-balance-scale",
    color: "#fd7e14",
    bgColor: "#ffe5d0",
  },
  // Onay aşaması
  confirmed: {
    step: 1,
    label: "Sipariş Onaylandı",
    shortLabel: "Onaylandı",
    description: "Siparişiniz mağaza tarafından onaylandı",
    icon: "fa-check-circle",
    color: "#17a2b8",
    bgColor: "#d1ecf1",
  },
  // Hazırlık aşaması
  preparing: {
    step: 2,
    label: "Hazırlanıyor",
    shortLabel: "Hazırlanıyor",
    description: "Siparişiniz hazırlanıyor ve paketleniyor",
    icon: "fa-box",
    color: "#fd7e14",
    bgColor: "#ffe5d0",
  },
  processing: {
    step: 2,
    label: "İşleniyor",
    shortLabel: "İşleniyor",
    description: "Siparişiniz işleme alındı",
    icon: "fa-cog",
    color: "#fd7e14",
    bgColor: "#ffe5d0",
  },
  // Hazır / Kurye ataması aşaması
  ready: {
    step: 2,
    label: "Sipariş Hazırlandı",
    shortLabel: "Hazır",
    description: "Siparişiniz hazırlandı, kurye ataması bekleniyor",
    icon: "fa-box",
    color: "#fd7e14",
    bgColor: "#ffe5d0",
  },
  assigned: {
    step: 3,
    label: "Kuryeniz Atandı",
    shortLabel: "Kurye Atandı",
    description: "Kurye siparişinizi teslim almak üzere yola çıktı",
    icon: "fa-motorcycle",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  pickedup: {
    step: 3,
    label: "Kurye Siparişi Aldı",
    shortLabel: "Kurye'de",
    description: "Siparişiniz kuryede, teslimata hazırlanıyor",
    icon: "fa-motorcycle",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  picked_up: {
    step: 3,
    label: "Kurye Siparişi Aldı",
    shortLabel: "Kurye'de",
    description: "Siparişiniz kuryede, teslimata hazırlanıyor",
    icon: "fa-motorcycle",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  // Kargo aşaması
  shipped: {
    step: 3,
    label: "Siparişiniz Yola Çıktı",
    shortLabel: "Yola Çıktı",
    description: "Siparişiniz teslimat için yola çıktı",
    icon: "fa-truck",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  out_for_delivery: {
    step: 3,
    label: "Siparişiniz Yola Çıktı",
    shortLabel: "Yola Çıktı",
    description: "Siparişiniz teslimat için yola çıktı",
    icon: "fa-shipping-fast",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  outfordelivery: {
    step: 3,
    label: "Siparişiniz Yola Çıktı",
    shortLabel: "Yola Çıktı",
    description: "Siparişiniz teslimat için yola çıktı",
    icon: "fa-shipping-fast",
    color: "#6f42c1",
    bgColor: "#e2d9f3",
  },
  // Teslim aşaması
  delivered: {
    step: 4,
    label: "Teslim Edildi ✅",
    shortLabel: "Teslim Edildi",
    description: "Siparişiniz başarıyla teslim edildi",
    icon: "fa-check-double",
    color: "#28a745",
    bgColor: "#d4edda",
  },
  // İptal/Problem durumları
  cancelled: {
    step: -1,
    label: "İptal Edildi",
    shortLabel: "İptal",
    description: "Siparişiniz iptal edildi",
    icon: "fa-times-circle",
    color: "#dc3545",
    bgColor: "#f8d7da",
  },
  delivery_failed: {
    step: -1,
    label: "Teslimat Başarısız",
    shortLabel: "Başarısız",
    description:
      "Teslimat gerçekleştirilemedi. Lütfen bizimle iletişime geçin.",
    icon: "fa-exclamation-triangle",
    color: "#dc3545",
    bgColor: "#f8d7da",
  },
  delivery_payment_pending: {
    step: 4, // Teslim edildi ama ödeme bekliyor
    label: "Ödeme Bekleniyor",
    shortLabel: "Ödeme Bekliyor",
    description:
      "Siparişiniz teslim edildi ancak ödeme işlemi beklemede. Kısa sürede tamamlanacak.",
    icon: "fa-credit-card",
    color: "#fd7e14",
    bgColor: "#fff3cd",
  },
  refunded: {
    step: -1,
    label: "İade Edildi",
    shortLabel: "İade",
    description: "Siparişiniz iade edildi",
    icon: "fa-undo",
    color: "#6c757d",
    bgColor: "#e9ecef",
  },
};

/**
 * Stepper adımları
 */
const STEPPER_STEPS = [
  { key: "pending", label: "Sipariş Alındı", icon: "fa-shopping-cart" },
  { key: "confirmed", label: "Onaylandı", icon: "fa-check-circle" },
  { key: "preparing", label: "Hazırlanıyor", icon: "fa-box" },
  { key: "shipped", label: "Yola Çıktı", icon: "fa-truck" },
  { key: "delivered", label: "Teslim Edildi", icon: "fa-check-double" },
];

// ==========================================================================
// HELPER FONKSİYONLAR
// ==========================================================================

const getStatusInfo = (status) => {
  const normalizedStatus = normalizeStatus(status) || "pending";
  return ORDER_STATUSES[normalizedStatus] || ORDER_STATUSES.pending;
};

/** SignalR / API id eşleştirmesi (string/number güvenli) */
const orderMatchesEvent = (order, data) =>
  String(order?.id ?? order?.orderId ?? "") === String(data?.orderId ?? "") ||
  (order?.orderNumber &&
    data?.orderNumber &&
    order.orderNumber === data.orderNumber);

const getStepperProgress = (status) => {
  const info = getStatusInfo(status);
  return info.step >= 0 ? ((info.step + 1) / STEPPER_STEPS.length) * 100 : 0;
};

const getDisplayOrderNumber = (order) =>
  order?.orderNumber || order?.id || order?.orderId || "-";

const getOrderDateText = (dateValue) => {
  if (!dateValue) return "-";
  const date = new Date(dateValue);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString("tr-TR");
};

const getOrderDateTimeText = (dateValue) => {
  if (!dateValue) return "-";
  const date = new Date(dateValue);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString("tr-TR", {
    day: "numeric",
    month: "long",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};

const getOrderAddress = (order) => {
  if (!order) return "Belirtilmedi";
  const candidates = [
    order.deliveryAddress,
    order.shippingAddress,
    order.address,
    order.fullAddress,
    order.addressSummary,
    order.raw?.deliveryAddress,
    order.raw?.shippingAddress,
    order.raw?.address,
    order.raw?.fullAddress,
    order.raw?.addressSummary,
  ];
  const address = candidates.find(
    (candidate) => typeof candidate === "string" && candidate.trim(),
  );
  return address ? address.trim() : "Belirtilmedi";
};

// ==========================================================================
// ANA COMPONENT
// ==========================================================================

const OrderTracking = () => {
  // State
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [trackingCode, setTrackingCode] = useState("");
  const [connectionStatus, setConnectionStatus] = useState(
    ConnectionState.DISCONNECTED,
  );
  const [pollingFallback, setPollingFallback] = useState(false);
  const [notification, setNotification] = useState(null);
  const [cancellingOrderId, setCancellingOrderId] = useState(null); // İptal işlemi yapılan sipariş ID'si
  const [refundRequests, setRefundRequests] = useState([]);
  const [activeTab, setActiveTab] = useState("active");

  // =========================================================================
  // MİSAFİR SİPARİŞ ARAMA STATE'LERİ
  // Giriş yapmamış kullanıcılar telefon no veya sipariş no ile sorgulama yapar
  // =========================================================================
  const [guestSearchPhone, setGuestSearchPhone] = useState("");
  const [guestSearchOrderNo, setGuestSearchOrderNo] = useState("");
  const [guestSearchEmail, setGuestSearchEmail] = useState("");
  const [guestSearchEmailOrderNo, setGuestSearchEmailOrderNo] = useState("");
  const [guestSearchTab, setGuestSearchTab] = useState("phone");
  const [guestSearchLoading, setGuestSearchLoading] = useState(false);
  const [guestSearchError, setGuestSearchError] = useState(null);

  // Token geçersiz/expired olduğunda (API 401) misafir paneline düşmek için.
  // NEDEN: localStorage'da bayat token kalırsa kullanıcı ne listesini görebiliyor
  // ne de misafir arama panelini görebiliyordu; bu fallback paneli tekrar açar.
  const [forceGuestMode, setForceGuestMode] = useState(false);

  // Misafir kullanıcı mı kontrolü (bayat token durumu da misafir sayılır)
  const isGuest =
    !localStorage.getItem("token") ||
    !localStorage.getItem("userId") ||
    forceGuestMode;

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================
  const loadOrders = useCallback(async () => {
    try {
      const userId = localStorage.getItem("userId");
      const token = localStorage.getItem("token");

      // ================================================================
      // MİSAFİR KULLANICI KONTROLÜ
      // Token yoksa veya userId yoksa misafir siparişlerini storage'dan oku
      // Önce sessionStorage, sonra localStorage kontrol edilir
      // Session ID ile filtreleme yapılır (farklı tarayıcı = farklı siparişler)
      // ================================================================
      if (!token || !userId) {
        console.log(
          "[OrderTracking] Misafir kullanıcı, storage'dan siparişler yükleniyor...",
        );
        try {
          // Önce sessionStorage'dan dene
          let guestOrders = JSON.parse(
            sessionStorage.getItem("guestOrders") || "[]",
          );

          // SessionStorage boşsa localStorage'dan dene
          if (guestOrders.length === 0) {
            guestOrders = JSON.parse(
              localStorage.getItem("guestOrders") || "[]",
            );

            // Session ID kontrolü - mevcut session'a ait siparişleri filtrele
            const currentSessionId = sessionStorage.getItem("guest_session_id");
            if (currentSessionId && guestOrders.length > 0) {
              guestOrders = guestOrders.filter(
                (o) => !o.sessionId || o.sessionId === currentSessionId,
              );
            }
          }

          if (guestOrders.length > 0) {
            console.log(
              "[OrderTracking] ✅ Storage'dan",
              guestOrders.length,
              "misafir siparişi bulundu",
            );
            // Misafir siparişlerini görüntüleme formatına dönüştür
            const formattedOrders = guestOrders.map((order) => ({
              id: order.orderId,
              orderNumber: order.orderNumber,
              status: order.status || "pending",
              totalAmount: order.totalPrice,
              finalPrice: order.totalPrice,
              orderDate: order.createdAt,
              customerEmail: order.email,
              isGuestOrder: true,
            }));
            setOrders(
              formattedOrders.sort(
                (a, b) => new Date(b.orderDate) - new Date(a.orderDate),
              ),
            );

            // Storage'daki durum bayat olabilir (admin iptali vb.) — API ile tazele
            try {
              const refreshPhone =
                guestOrders[0]?.phone || guestOrders[0]?.customerPhone;
              const refreshOrderNo = guestOrders[0]?.orderNumber;
              let refreshed = [];
              if (refreshPhone) {
                refreshed = await OrderService.trackGuestOrder({
                  phone: refreshPhone,
                });
              } else if (refreshOrderNo) {
                refreshed = await OrderService.trackGuestOrder({
                  orderNumber: refreshOrderNo,
                });
              }
              if (refreshed.length > 0) {
                setOrders(
                  refreshed.sort(
                    (a, b) =>
                      new Date(b.orderDate || b.createdAt) -
                      new Date(a.orderDate || a.createdAt),
                  ),
                );
              }
            } catch (refreshErr) {
              console.warn(
                "[OrderTracking] Misafir sipariş durumu yenilenemedi:",
                refreshErr,
              );
            }
          } else {
            setOrders([]);
          }
        } catch (e) {
          console.warn("[OrderTracking] Storage okuma hatası:", e);
          setOrders([]);
        }
        setLoading(false);
        return;
      }

      // Kayıtlı kullanıcı için API'den yükle
      const userOrders = await OrderService.list(userId);
      setOrders(userOrders || []);

      try {
        const refunds = await OrderService.getMyRefundRequests();
        setRefundRequests(Array.isArray(refunds) ? refunds : refunds?.data || []);
      } catch (refundErr) {
        console.warn("[OrderTracking] İade talepleri yüklenemedi:", refundErr);
      }
    } catch (error) {
      console.error("Siparişler yüklenemedi:", error);

      // Token geçersiz/expired ise misafir moduna düş (arama paneli görünsün)
      if (error?.status === 401 || error?.response?.status === 401) {
        console.warn(
          "[OrderTracking] Token geçersiz, misafir moduna geçiliyor.",
        );
        setForceGuestMode(true);
      }

      // ================================================================
      // API HATASI DURUMUNDA MİSAFİR SİPARİŞLERİNİ GÖSTER
      // ================================================================
      console.log(
        "[OrderTracking] API hatası, misafir siparişleri deneniyor...",
      );
      try {
        const guestOrders = JSON.parse(
          localStorage.getItem("guestOrders") || "[]",
        );
        if (guestOrders.length > 0) {
          const formattedOrders = guestOrders.map((order) => ({
            id: order.orderId,
            orderNumber: order.orderNumber,
            status: order.status || "pending",
            totalAmount: order.totalPrice,
            finalPrice: order.totalPrice,
            orderDate: order.createdAt,
            customerEmail: order.email,
            isGuestOrder: true,
          }));
          setOrders(
            formattedOrders.sort(
              (a, b) => new Date(b.orderDate) - new Date(a.orderDate),
            ),
          );
        }
      } catch (e) {
        console.warn("[OrderTracking] LocalStorage fallback hatası:", e);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  // =========================================================================
  // MİSAFİR SİPARİŞ ARAMA FONKSİYONU
  // Telefon numarası veya sipariş numarası ile backend'den sorgulama yapar
  // =========================================================================
  const handleGuestSearch = async (e) => {
    e.preventDefault();
    setGuestSearchError(null);

    // Sipariş numarası format ipucu (ORD-yyyyMMdd-HHmmss-xxxx)
    const ORDER_NO_HINT =
      "Sipariş numaranız onay/e-posta'da yer alan ORD- ile başlayan koddur (örn. ORD-20260628-102230-1234).";

    // ----- Validasyon -----
    if (guestSearchTab === "phone") {
      if (!guestSearchPhone?.trim()) {
        setGuestSearchError("Lütfen telefon numaranızı girin.");
        return;
      }
      const digits = guestSearchPhone.replace(/\D/g, "");
      if (digits.length < 10) {
        setGuestSearchError("Geçerli bir telefon numarası girin (en az 10 hane).");
        return;
      }
    }
    if (guestSearchTab === "orderNumber" && !guestSearchOrderNo?.trim()) {
      setGuestSearchError(`Lütfen sipariş numaranızı girin. ${ORDER_NO_HINT}`);
      return;
    }
    if (guestSearchTab === "email") {
      if (!guestSearchEmail?.trim() || !guestSearchEmailOrderNo?.trim()) {
        setGuestSearchError(
          "Lütfen e-posta adresinizi ve sipariş numaranızı girin.",
        );
        return;
      }
    }

    setGuestSearchLoading(true);
    try {
      // E-posta + sipariş numarası ile güvenli sorgulama (guest-lookup)
      if (guestSearchTab === "email") {
        const order = await OrderService.findGuestOrder(
          guestSearchEmail.trim(),
          guestSearchEmailOrderNo.trim(),
        );
        if (order) {
          setOrders([order]);
          setGuestSearchError(null);
        } else {
          setGuestSearchError(
            `Bu e-posta ve sipariş numarasıyla eşleşen sipariş bulunamadı. ${ORDER_NO_HINT}`,
          );
        }
        return;
      }

      // Telefon veya sipariş numarası ile sorgulama (guest-track)
      const params = {};
      if (guestSearchTab === "phone") params.phone = guestSearchPhone.trim();
      if (guestSearchTab === "orderNumber")
        params.orderNumber = guestSearchOrderNo.trim();

      const results = await OrderService.trackGuestOrder(params);

      if (results && results.length > 0) {
        setOrders(results);
        setGuestSearchError(null);
      } else {
        setGuestSearchError(
          guestSearchTab === "phone"
            ? "Bu telefon numarasıyla eşleşen sipariş bulunamadı."
            : `Bu sipariş numarasıyla eşleşen sipariş bulunamadı. ${ORDER_NO_HINT}`,
        );
      }
    } catch (err) {
      console.error("[OrderTracking] Misafir sipariş arama hatası:", err);
      setGuestSearchError(
        err?.status === 404
          ? "Bu bilgilerle eşleşen sipariş bulunamadı."
          : "Sipariş araması sırasında bir hata oluştu.",
      );
    } finally {
      setGuestSearchLoading(false);
    }
  };

  const handleOpenOrder = useCallback(async (order) => {
    if (!order) return;
    const identifier = order.id || order.orderId || order.orderNumber;
    if (!identifier) {
      setSelectedOrder(order);
      return;
    }

    setLoadingDetail(true);
    try {
      const detail = await OrderService.getById(identifier);
      setSelectedOrder(detail || order);
    } catch (error) {
      console.warn("[OrderTracking] Sipariş detayı alınamadı:", error);
      setSelectedOrder(order);
    } finally {
      setLoadingDetail(false);
    }
  }, []);

  useEffect(() => {
    if (!selectedOrder) return;
    if (window.matchMedia("(max-width: 991px)").matches) {
      requestAnimationFrame(() => {
        document
          .querySelector(".order-tracking-layout__detail.is-open")
          ?.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    }
  }, [selectedOrder]);

  // =========================================================================
  // SİPARİŞ İPTAL FONKSİYONU - MARKET KURALLARI
  // 1. Sadece aynı gün içinde iptal edilebilir
  // 2. Sadece kurye teslim süreci başlamadan önce iptal edilebilir
  // 3. Diğer durumlarda müşteri hizmetleriyle iletişime yönlendirilir
  // =========================================================================
  const handleCancelOrder = useCallback(
    async (orderId, orderNumber) => {
      setCancellingOrderId(orderId);

      try {
        const response = await OrderService.cancel(orderId);

        if (response.success) {
          setNotification({
            type: "success",
            title: "Sipariş İptal Edildi",
            message:
              response.message ||
              `${orderNumber || orderId} numaralı siparişiniz başarıyla iptal edildi.`,
            color: "#28a745",
            bgColor: "#d4edda",
          });

          setActiveTab("history");
          await loadOrders();
          try {
            const refunds = await OrderService.getMyRefundRequests();
            setRefundRequests(Array.isArray(refunds) ? refunds : []);
          } catch {
            // no-op
          }
          setSelectedOrder(null);
        } else {
          setNotification({
            type: "error",
            title: "İptal Edilemedi",
            message:
              response.message ||
              "Sipariş iptal edilemedi. Müşteri hizmetleriyle iletişime geçiniz.",
            color: "#dc3545",
            bgColor: "#f8d7da",
            showWhatsApp: true,
            orderNumber: orderNumber || `#${orderId}`,
            contactInfo: response.contactInfo,
          });
        }
      } catch (error) {
        console.error("[OrderTracking] Sipariş iptal hatası:", error);
        const errorMessage =
          error.response?.data?.message || "Bir hata oluştu.";

        setNotification({
          type: "error",
          title: "İptal Edilemedi",
          message: `${errorMessage} Müşteri hizmetleriyle iletişime geçiniz.`,
          color: "#dc3545",
          bgColor: "#f8d7da",
          showWhatsApp: true,
          orderNumber: orderNumber || `#${orderId}`,
        });
      } finally {
        setCancellingOrderId(null);
      }
    },
    [loadOrders],
  );

  const openWhatsAppSupport = useCallback((orderNumber) => {
    openWhatsAppSupportAsync(orderNumber || "Sipariş");
  }, []);

  // =========================================================================
  // SIGNALR BAĞLANTISI
  // =========================================================================
  // SES BİLDİRİMİ VE BROWSER NOTIFICATION
  // =========================================================================
  const playNotificationSound = useCallback(() => {
    try {
      const audio = new Audio(
        "/sounds/mixkit-happy-bells-notification-937.wav",
      );
      audio.volume = 0.6;
      audio.play().catch(() => {});
    } catch (e) {
      console.warn("[OrderTracking] Ses çalınamadı:", e);
    }
  }, []);

  const showBrowserNotification = useCallback(
    (title, body, icon = "fa-bell") => {
      // Ses çal
      playNotificationSound();

      // Browser notification
      if ("Notification" in window && Notification.permission === "granted") {
        new Notification(title, {
          body,
          icon: "/logo192.png",
          tag: "order-tracking",
          requireInteraction: false,
        });
      } else if (
        "Notification" in window &&
        Notification.permission !== "denied"
      ) {
        Notification.requestPermission();
      }
    },
    [playNotificationSound],
  );

  // Browser notification izni iste
  useEffect(() => {
    if ("Notification" in window && Notification.permission === "default") {
      Notification.requestPermission();
    }
  }, []);

  // =========================================================================
  // SIGNALR BAĞLANTISI
  // =========================================================================
  useEffect(() => {
    loadOrders();

    // ================================================================
    // MİSAFİR KULLANICI İÇİN SIGNALR BAĞLANTISI YAPMA
    // Token yoksa SignalR 401 hatası alınır, bu yüzden bağlanma
    // Bunun yerine polling ile siparişleri düzenli kontrol ederiz
    // ================================================================
    const token = localStorage.getItem("token");
    if (!token) {
      console.log(
        "[OrderTracking] Misafir kullanıcı: SignalR (guest) + polling fallback",
      );

      // ================================================================
      // MİSAFİR İÇİN CANLI TAKİP (SignalR)
      // OrderHub anonim bağlantıya izin verir; saklanan sipariş no + email ile
      // JoinGuestOrderTracking çağrılır. Bağlantı kurulamazsa polling devreye girer.
      // ================================================================
      const guestUnsubscribers = [];
      const setupGuestRealtime = async () => {
        try {
          const guestOrders = JSON.parse(
            localStorage.getItem("guestOrders") || "[]",
          );
          const trackable = guestOrders
            .filter((o) => o?.orderNumber && o?.email)
            .slice(0, 5);

          let anyJoined = false;
          for (const go of trackable) {
            const joinedId = await signalRService.connectGuestOrderTracking(
              go.orderNumber,
              go.email,
            );
            if (joinedId && joinedId > 0) anyJoined = true;
          }

          if (anyJoined) {
            setConnectionStatus(ConnectionState.CONNECTED);

            const unsubStatus = signalRService.onOrderStatusChanged((data) => {
              const statusInfo = getStatusInfo(data.newStatus || data.status);
              showBrowserNotification(
                `📦 Sipariş #${data.orderNumber || data.orderId}`,
                statusInfo.label + " - " + (statusInfo.description || ""),
                statusInfo.icon,
              );
              setNotification({
                type: "info",
                title: `Sipariş #${data.orderNumber || data.orderId}`,
                message: statusInfo.label,
                icon: statusInfo.icon,
                color: statusInfo.color,
              });
              setOrders((prev) =>
                prev.map((o) =>
                  o.id === data.orderId ||
                  o.orderNumber === data.orderNumber
                    ? { ...o, status: data.newStatus || data.status }
                    : o,
                ),
              );
              setTimeout(() => setNotification(null), 6000);
            });
            guestUnsubscribers.push(unsubStatus);
          }
        } catch (e) {
          console.warn("[OrderTracking] Misafir SignalR kurulum hatası:", e);
        }
      };
      setupGuestRealtime();

      // ================================================================
      // MİSAFİR İÇİN POLLİNG FALLBACK
      // SignalR çalışsa bile güvenlik ağı olarak periyodik kontrol.
      // ================================================================
      const pollInterval = setInterval(async () => {
        try {
          const guestOrders = JSON.parse(
            localStorage.getItem("guestOrders") || "[]",
          );
          if (guestOrders.length === 0) return;

          // Son sipariş için durum kontrolü yap
          for (const guestOrder of guestOrders.slice(0, 3)) {
            // Son 3 sipariş için
            try {
              const orderId = guestOrder.orderId;
              if (!orderId) continue;

              const freshOrder = await OrderService.getById(orderId);
              if (!freshOrder) continue;

              const oldStatus = guestOrder.status;
              const newStatus = freshOrder.status;

              // Durum değiştiyse bildirim göster
              if (
                oldStatus &&
                newStatus &&
                oldStatus.toLowerCase() !== newStatus.toLowerCase()
              ) {
                console.log(
                  `[OrderTracking] Sipariş durumu değişti: ${oldStatus} → ${newStatus}`,
                );

                // LocalStorage'daki durumu güncelle
                const updatedOrders = guestOrders.map((o) =>
                  o.orderId === orderId ? { ...o, status: newStatus } : o,
                );
                localStorage.setItem(
                  "guestOrders",
                  JSON.stringify(updatedOrders),
                );

                // State'i güncelle
                setOrders((prev) =>
                  prev.map((o) =>
                    o.id === orderId ? { ...o, status: newStatus } : o,
                  ),
                );

                // Bildirim göster
                const statusInfo = getStatusInfo(newStatus);
                showBrowserNotification(
                  `📦 Sipariş #${freshOrder.orderNumber || orderId}`,
                  statusInfo.label + " - " + (statusInfo.description || ""),
                  statusInfo.icon,
                );

                setNotification({
                  type: "info",
                  title: `Sipariş #${freshOrder.orderNumber || orderId}`,
                  message: statusInfo.label,
                  icon: statusInfo.icon,
                  color: statusInfo.color,
                });

                setTimeout(() => setNotification(null), 5000);
              }
            } catch (e) {
              // Tek sipariş için hata ana döngüyü durdurmasın
              console.warn("[OrderTracking] Sipariş kontrolü hatası:", e);
            }
          }
        } catch (e) {
          console.warn("[OrderTracking] Polling hatası:", e);
        }
      }, 30000); // 30 saniye (SignalR ana kanal, bu güvenlik ağı)

      return () => {
        clearInterval(pollInterval);
        guestUnsubscribers.forEach((unsub) => {
          try {
            unsub();
          } catch {
            /* no-op */
          }
        });
      };
    }

    // SignalR bağlantısı kur (sadece giriş yapmış kullanıcılar için)
    let authPollInterval = null;
    const startAuthPolling = () => {
      if (authPollInterval) return;
      setPollingFallback(true);
      authPollInterval = setInterval(() => {
        loadOrders().catch(() => {});
      }, 30000);
    };

    const connectSignalR = async () => {
      try {
        const connected = await signalRService.connectCustomer();
        if (connected) {
          setPollingFallback(false);
          setConnectionStatus(ConnectionState.CONNECTED);
          console.log("[OrderTracking] SignalR bağlantısı kuruldu");

          // ================================================================
          // TÜM SİPARİŞLERİN GRUPLARINA KATIL
          // NEDEN: Backend "order-{orderId}" grubuna bildirim gönderiyor
          // Müşteri bu gruplara katılmazsa bildirim alamaz
          // ================================================================
          try {
            const userOrders = await OrderService.list();
            if (userOrders && userOrders.length > 0) {
              for (const order of userOrders.slice(0, 10)) {
                // Son 10 sipariş
                try {
                  await signalRService.connectCustomer(order.id);
                  console.log(
                    `[OrderTracking] Sipariş #${order.id} grubuna katıldı`,
                  );
                } catch (e) {
                  console.warn(
                    `[OrderTracking] Sipariş #${order.id} grubuna katılınamadı:`,
                    e,
                  );
                }
              }
            }
          } catch (e) {
            console.warn(
              "[OrderTracking] Sipariş gruplarına katılma hatası:",
              e,
            );
          }
        } else {
          console.warn(
            "[OrderTracking] SignalR bağlanamadı, periyodik güncelleme kullanılacak",
          );
          startAuthPolling();
        }
      } catch (error) {
        console.error("[OrderTracking] SignalR bağlantı hatası:", error);
        startAuthPolling();
      }
    };

    connectSignalR();

    // Sipariş durum değişikliği dinle
    const unsubscribeStatus = signalRService.onOrderStatusChanged((data) => {
      console.log("[OrderTracking] Sipariş durumu değişti:", data);

      // Bildirimi göster
      const statusInfo = getStatusInfo(data.newStatus || data.status);

      // Browser notification ve ses
      showBrowserNotification(
        `📦 Sipariş #${data.orderNumber || data.orderId}`,
        statusInfo.label + " - " + (statusInfo.description || ""),
        statusInfo.icon,
      );

      setNotification({
        type: "info",
        title: `Sipariş #${data.orderId || data.orderNumber}`,
        message: statusInfo.label,
        icon: statusInfo.icon,
        color: statusInfo.color,
      });

      // Sipariş listesini güncelle
      const newStatus = data.newStatus || data.status;
      setOrders((prev) =>
        prev.map((order) =>
          orderMatchesEvent(order, data)
            ? { ...order, status: newStatus }
            : order,
        ),
      );

      // Seçili sipariş güncellemesi
      setSelectedOrder((prev) =>
        prev && orderMatchesEvent(prev, data)
          ? { ...prev, status: newStatus }
          : prev,
      );

      if (isCancelledOrRefundedOrder({ status: newStatus })) {
        setActiveTab("history");
      }

      // Header'daki aktif sipariş rozetinin de güncellenmesi için global olay yay
      try {
        window.dispatchEvent(
          new CustomEvent("orderStatusChanged", { detail: data }),
        );
      } catch (e) {}

      // Bildirimi 5 saniye sonra kaldır
      setTimeout(() => setNotification(null), 5000);
    });

    // Teslimat durum değişikliği dinle
    const unsubscribeDelivery = signalRService.onDeliveryStatusChanged(
      (data) => {
        console.log("[OrderTracking] Teslimat durumu değişti:", data);

        // Sipariş listesini güncelle (orderId eşleşirse)
        if (data.orderId) {
          loadOrders(); // Verileri yenile
        }
      },
    );

    // Teslimat tamamlandı dinle (backend "DeliveryCompleted" gönderiyor)
    const unsubscribeDeliveryCompleted = signalRService.onDeliveryCompleted(
      (data) => {
        console.log("[OrderTracking] Teslimat tamamlandı:", data);
        const statusInfo = getStatusInfo("delivered");
        showBrowserNotification(
          `📦 Sipariş #${data.orderNumber || data.orderId}`,
          statusInfo.label,
          statusInfo.icon,
        );
        setNotification({
          type: "success",
          title: `Sipariş #${data.orderNumber || data.orderId}`,
          message: statusInfo.label,
          icon: statusInfo.icon,
          color: statusInfo.color,
        });
        setOrders((prev) =>
          prev.map((o) =>
            o.id === data.orderId || o.orderNumber === data.orderNumber
              ? { ...o, status: "delivered" }
              : o,
          ),
        );
        setTimeout(() => setNotification(null), 6000);
      },
    );

    // Ağırlık farkı tahsilat bildirimi dinle
    const unsubscribeWeightCharge = signalRService.onWeightChargeApplied(
      (data) => {
        console.log("[OrderTracking] Ağırlık farkı bildirimi:", data);

        // Bildirim göster
        const isOverage = data.weightDifferenceAmount > 0;
        setNotification({
          type: isOverage ? "warning" : "info",
          title: `Sipariş #${data.orderNumber || data.orderId}`,
          message:
            data.message ||
            (isOverage
              ? `Tartı farkı nedeniyle ${Number(data.weightDifferenceAmount).toFixed(2)} TL ek tahsilat yapıldı.`
              : `Tartı farkı nedeniyle ${Math.abs(data.weightDifferenceAmount).toFixed(2)} TL iade edildi.`),
          icon: "⚖️",
          color: isOverage ? "#f59e0b" : "#10b981",
        });

        // Browser notification
        showBrowserNotification(
          `⚖️ Sipariş #${data.orderNumber || data.orderId}`,
          data.message || "Ağırlık farkı uygulandı",
          "⚖️",
        );

        // Sipariş verilerini yenile (güncel tutar bilgisi için)
        loadOrders();

        // Bildirimi 8 saniye sonra kaldır (ağırlık farkı daha uzun gösterilmeli)
        setTimeout(() => setNotification(null), 8000);
      },
    );

    // Bağlantı durumu değişikliği dinle
    const deliveryHub = signalRService.deliveryHub;
    const unsubscribeState = deliveryHub.onStateChange((newState) => {
      setConnectionStatus(newState);
    });

    // =========================================================================
    // SES BİLDİRİMİ DİNLEYİCİSİ (Müşteri için)
    // Backend "PlaySound" event'i gönderdiğinde ses çal
    // NEDEN: Sipariş durumu değişikliğinde müşteriyi uyar
    // =========================================================================
    const handlePlaySound = (data) => {
      console.log("[OrderTracking] 🔊 Backend'den ses bildirimi:", data);
      // Ses dosyası çal
      const soundEnabled =
        localStorage.getItem("notificationSoundEnabled") !== "false";
      if (soundEnabled) {
        try {
          const audio = new Audio("/sounds/mixkit-bell-notification-933.wav");
          audio.volume = 0.5;
          audio.play().catch(() => {});
        } catch (e) {
          console.warn("[OrderTracking] Ses çalınamadı");
        }
      }
    };

    deliveryHub.on("PlaySound", handlePlaySound);

    // Cleanup
    return () => {
      if (authPollInterval) clearInterval(authPollInterval);
      unsubscribeStatus();
      unsubscribeDelivery();
      unsubscribeDeliveryCompleted();
      unsubscribeWeightCharge();
      unsubscribeState();
      deliveryHub.off("PlaySound", handlePlaySound);
    };
  }, [loadOrders, showBrowserNotification]);

  // =========================================================================
  // SİPARİŞ TAKİP
  // =========================================================================
  const trackOrderByCode = async () => {
    if (!trackingCode.trim()) return;

    // Önce local listede ara
    const order = orders.find(
      (o) =>
        o.orderNumber === trackingCode || String(o.id) === String(trackingCode),
    );

    if (order) {
      setSelectedOrder(order);
      // SignalR ile bu siparişin grubuna katıl
      await signalRService.connectCustomer(order.id);
      return;
    }

    // Sunucudan getir
    try {
      const fetched = await OrderService.getById(trackingCode);
      if (fetched) {
        setSelectedOrder(fetched);
        await signalRService.connectCustomer(fetched.id);
      } else {
        setNotification({
          type: "error",
          title: "Sipariş Bulunamadı",
          message: "Lütfen takip kodunu kontrol edin.",
          icon: "fa-exclamation-circle",
          color: "#dc3545",
        });
        setTimeout(() => setNotification(null), 4000);
      }
    } catch (err) {
      setNotification({
        type: "error",
        title: "Hata",
        message: "Sipariş bulunamadı veya sunucuya erişilemiyor.",
        icon: "fa-exclamation-circle",
        color: "#dc3545",
      });
      setTimeout(() => setNotification(null), 4000);
    }
  };

  // =========================================================================
  // RENDER HELPERS
  // =========================================================================
  const renderConnectionBadge = () => {
    if (pollingFallback && connectionStatus !== ConnectionState.CONNECTED) {
      return (
        <span
          className="badge bg-warning text-dark ms-2"
          style={{ fontSize: "10px" }}
        >
          <i className="fas fa-sync me-1"></i>
          Periyodik Güncelleme
        </span>
      );
    }

    const statusConfig = {
      [ConnectionState.CONNECTED]: {
        color: "success",
        icon: "fa-wifi",
        text: "Canlı Takip Aktif",
      },
      [ConnectionState.CONNECTING]: {
        color: "warning",
        icon: "fa-spinner fa-spin",
        text: "Bağlanıyor...",
      },
      [ConnectionState.RECONNECTING]: {
        color: "warning",
        icon: "fa-sync fa-spin",
        text: "Yeniden Bağlanıyor...",
      },
      [ConnectionState.DISCONNECTED]: {
        color: "secondary",
        icon: "fa-wifi",
        text: "Çevrimdışı",
      },
      [ConnectionState.FAILED]: {
        color: "danger",
        icon: "fa-exclamation-triangle",
        text: "Bağlantı Hatası",
      },
    };
    const config =
      statusConfig[connectionStatus] ||
      statusConfig[ConnectionState.DISCONNECTED];

    return (
      <span
        className={`badge bg-${config.color} ms-2`}
        style={{ fontSize: "10px" }}
      >
        <i className={`fas ${config.icon} me-1`}></i>
        {config.text}
      </span>
    );
  };

  // Aktif sipariş kalmadığında Geçmiş sekmesine geç (iptal/teslim görünür kalsın)
  useEffect(() => {
    if (orders.length === 0) return;
    const activeCount = countActiveOrders(orders);
    const historyCount = countHistoryOrders(orders);
    if (activeCount === 0 && historyCount > 0 && activeTab === "active") {
      setActiveTab("history");
    }
  }, [orders, activeTab]);

  const activeOrderCount = countActiveOrders(orders);
  const historyOrderCount = countHistoryOrders(orders);
  const cancelledOrderCount = countCancelledOrders(orders);

  const displayedOrders = orders.filter((order) =>
    activeTab === "active" ? isActiveOrder(order) : isCompletedOrder(order),
  );

  // =========================================================================
  // LOADING STATE
  // =========================================================================
  if (loading) {
    return (
      <div className="text-center py-5">
        <div
          className="spinner-border mb-3"
          role="status"
          style={{ color: "#ff8f00", width: "3rem", height: "3rem" }}
        >
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
        <p className="text-muted fw-bold">Siparişleriniz yükleniyor...</p>
      </div>
    );
  }

  // =========================================================================
  // MAIN RENDER
  // =========================================================================
  return (
    <div className="order-tracking-page">
      <div className="container order-tracking-container px-3 px-md-4">
        {/* Real-time Bildirim */}
        {notification && (
          <div
            className="order-tracking-toast alert d-flex align-items-center shadow-lg"
            style={{
              borderLeft: `5px solid ${notification.color}`,
            }}
          >
            <i
              className={`fas ${notification.icon} order-tracking-toast__icon`}
              style={{ color: notification.color }}
            />
            <div className="flex-grow-1 min-w-0">
              <strong>{notification.title}</strong>
              <p className="mb-0 small text-muted">{notification.message}</p>
              {notification.showWhatsApp && (
                <button
                  type="button"
                  className="btn btn-success btn-sm mt-2 order-tracking-toast__whatsapp"
                  onClick={() => openWhatsAppSupport(notification.orderNumber)}
                >
                  <i className="fab fa-whatsapp me-2" />
                  WhatsApp ile İletişime Geç
                </button>
              )}
            </div>
            <button
              type="button"
              className="order-tracking-toast__close"
              onClick={() => setNotification(null)}
              title="Kapat"
              aria-label="Bildirimi kapat"
              style={{ color: notification.color }}
            >
              ×
            </button>
          </div>
        )}

        <header className="order-tracking-header">
          <div>
            <h1 className="order-tracking-header__title">Sipariş Takibi</h1>
            <p className="order-tracking-header__subtitle">
              Siparişlerinizi canlı takip edin ve geçmiş siparişlerinize ulaşın
            </p>
          </div>
          {!isGuest && renderConnectionBadge()}
        </header>

        {isGuest && (
          <div className="card order-panel">
            <div className="order-panel-header">
              <h5 style={{ fontSize: "1.05rem" }}>
                <i className="fas fa-search me-2" />
                Sipariş Sorgula
              </h5>
              <p>
                Telefon, sipariş numarası veya e-posta ile siparişinizi bulun
              </p>
            </div>

            <div className="order-panel-body">
              <div className="guest-search-tabs">
                <button
                  type="button"
                  className={`guest-search-tab ${guestSearchTab === "phone" ? "active" : ""}`}
                  onClick={() => {
                    setGuestSearchTab("phone");
                    setGuestSearchError(null);
                  }}
                >
                  <i className="fas fa-phone-alt" />
                  Telefon
                </button>
                <button
                  type="button"
                  className={`guest-search-tab ${guestSearchTab === "orderNumber" ? "active" : ""}`}
                  onClick={() => {
                    setGuestSearchTab("orderNumber");
                    setGuestSearchError(null);
                  }}
                >
                  <i className="fas fa-hashtag" />
                  Sipariş No
                </button>
                <button
                  type="button"
                  className={`guest-search-tab ${guestSearchTab === "email" ? "active" : ""}`}
                  onClick={() => {
                    setGuestSearchTab("email");
                    setGuestSearchError(null);
                  }}
                >
                  <i className="fas fa-envelope" />
                  E-posta
                </button>
              </div>

              <form onSubmit={handleGuestSearch} className="guest-search-form">
                <div className="guest-search-fields">
                  {guestSearchTab === "phone" && (
                    <div className="guest-phone-group">
                      <span className="guest-phone-prefix">+90</span>
                      <input
                        type="tel"
                        className="guest-phone-input"
                        placeholder="5XX XXX XX XX"
                        value={guestSearchPhone}
                        onChange={(e) => setGuestSearchPhone(e.target.value)}
                        disabled={guestSearchLoading}
                        maxLength={15}
                      />
                    </div>
                  )}

                  {guestSearchTab === "orderNumber" && (
                    <input
                      type="text"
                      className="guest-text-input"
                      placeholder="ORD-20260128-144042-7819"
                      value={guestSearchOrderNo}
                      onChange={(e) => setGuestSearchOrderNo(e.target.value)}
                      disabled={guestSearchLoading}
                    />
                  )}

                  {guestSearchTab === "email" && (
                    <div className="d-flex flex-column gap-2">
                      <input
                        type="email"
                        className="guest-text-input"
                        placeholder="ornek@eposta.com"
                        value={guestSearchEmail}
                        onChange={(e) => setGuestSearchEmail(e.target.value)}
                        disabled={guestSearchLoading}
                      />
                      <input
                        type="text"
                        className="guest-text-input"
                        placeholder="ORD-20260128-144042-7819"
                        value={guestSearchEmailOrderNo}
                        onChange={(e) =>
                          setGuestSearchEmailOrderNo(e.target.value)
                        }
                        disabled={guestSearchLoading}
                      />
                    </div>
                  )}
                </div>

                <button
                  type="submit"
                  className="guest-search-submit"
                  disabled={guestSearchLoading}
                >
                  {guestSearchLoading ? (
                    <span className="spinner-border spinner-border-sm" />
                  ) : (
                    <>
                      <i className="fas fa-search me-1" />
                      Sipariş Ara
                    </>
                  )}
                </button>

                <small className="guest-search-hint">
                  {guestSearchTab === "phone" &&
                    "Sipariş verirken kullandığınız telefon numarasını girin"}
                  {guestSearchTab === "orderNumber" &&
                    "Onay e-postanızdaki ORD- ile başlayan sipariş numaranızı girin"}
                  {guestSearchTab === "email" &&
                    "Sipariş e-postanız ve sipariş numaranızı birlikte girin"}
                </small>
              </form>

              {guestSearchError && (
                <div
                  className="alert py-2 px-3 mt-3 mb-0 d-flex align-items-center"
                  style={{
                    backgroundColor: "#FFF3E0",
                    border: "1px solid #FFE0B2",
                    borderRadius: "10px",
                    fontSize: "0.85rem",
                  }}
                >
                  <i
                    className="fas fa-exclamation-triangle me-2"
                    style={{ color: "#FF8C00" }}
                  />
                  <span style={{ color: "#E65100" }}>{guestSearchError}</span>
                </div>
              )}
            </div>
          </div>
        )}

        <div className="order-tracking-layout">
          {/* Sipariş listesi */}
          <div className="order-tracking-layout__list">
            <div className="card order-panel">
              <div className="order-panel-header">
                <h2 className="order-panel-header__title">
                  <i className="fas fa-box me-2" />
                  Siparişlerim
                  <span className="order-panel-count">{displayedOrders.length}</span>
                </h2>
              </div>
              <div className="order-panel-body">
                <div className="orders-tabs">
                  <button
                    type="button"
                    className={`orders-tab ${activeTab === "active" ? "active" : ""}`}
                    onClick={() => setActiveTab("active")}
                  >
                    Aktif
                    {activeOrderCount > 0 && (
                      <span className="orders-tab__count">{activeOrderCount}</span>
                    )}
                  </button>
                  <button
                    type="button"
                    className={`orders-tab ${activeTab === "history" ? "active" : ""}`}
                    onClick={() => setActiveTab("history")}
                  >
                    Geçmiş
                    {historyOrderCount > 0 && (
                      <span className="orders-tab__count">{historyOrderCount}</span>
                    )}
                  </button>
                </div>
                {displayedOrders.length === 0 ? (
                  activeTab === "active" && historyOrderCount > 0 ? (
                    <div className="text-center py-4 px-2">
                      <i className="fas fa-ban fa-2x text-muted mb-2" />
                      <p className="text-muted mb-2">
                        Devam eden siparişiniz yok.
                        {cancelledOrderCount > 0 &&
                          ` ${cancelledOrderCount} iptal/iade kaydı Geçmiş sekmesinde.`}
                      </p>
                      <button
                        type="button"
                        className="btn btn-outline-secondary btn-sm"
                        onClick={() => setActiveTab("history")}
                      >
                        Geçmiş siparişleri göster
                      </button>
                    </div>
                  ) : (
                    <EmptyOrdersState />
                  )
                ) : (
                  <div className="orders-list">
                    {displayedOrders.map((order) => (
                      <OrderCard
                        key={order.id || order.orderId || order.orderNumber}
                        order={order}
                        isSelected={
                          selectedOrder &&
                          (selectedOrder.id === order.id ||
                            selectedOrder.orderId === order.orderId ||
                            selectedOrder.orderNumber === order.orderNumber)
                        }
                        onClick={() => handleOpenOrder(order)}
                      />
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Sipariş detayı */}
          <div
            className={`order-tracking-layout__detail${selectedOrder ? " is-open" : ""}`}
          >
            {selectedOrder ? (
              <OrderDetailCard
                order={selectedOrder}
                onClose={() => setSelectedOrder(null)}
                onCancel={handleCancelOrder}
                isCancelling={
                  cancellingOrderId ===
                  (selectedOrder.id || selectedOrder.orderId)
                }
                refundRequests={refundRequests}
                isAuthenticated={!isGuest}
              />
            ) : (
              <div className="order-detail-placeholder">
                <i className="fas fa-hand-pointer order-detail-placeholder__icon" />
                <p>Detayları görmek için listeden bir sipariş seçin</p>
              </div>
            )}
          </div>
        </div>
      </div>

      {loadingDetail && (
        <div className="order-tracking-loading-overlay">
          <div
            className="spinner-border text-warning"
            role="status"
            aria-label="Yükleniyor"
          />
        </div>
      )}
    </div>
  );
};

// ==========================================================================
// ALT COMPONENTLER
// ==========================================================================

/**
 * Sipariş Kartı
 */
const OrderCard = ({ order, onClick, isSelected = false }) => {
  const statusInfo = getStatusInfo(order.status);
  const isCancelled = statusInfo.step === -1;
  const orderNumber = getDisplayOrderNumber(order);
  const orderDateText = getOrderDateText(order.orderDate);

  return (
    <article
      className={`order-list-card${isSelected ? " order-list-card--selected" : ""}`}
      onClick={onClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onClick();
        }
      }}
    >
      <div className="order-list-card__top">
        <h6 className="order-list-card__number">Sipariş #{orderNumber}</h6>
        <span
          className="order-status-pill"
          style={{
            backgroundColor: statusInfo.bgColor,
            color: statusInfo.color,
          }}
        >
          <i className={`fas ${statusInfo.icon}`} />
          {statusInfo.shortLabel}
        </span>
      </div>

      {!isCancelled && <MiniStepper status={order.status} />}

      {isCancelled && (
        <div
          className="alert mb-3 py-2"
          style={{
            backgroundColor: statusInfo.bgColor,
            borderRadius: "10px",
            border: `1px solid ${statusInfo.color}`,
          }}
        >
          <small
            className="d-flex align-items-center"
            style={{ color: statusInfo.color }}
          >
            <i className={`fas ${statusInfo.icon} me-2`} />
            {statusInfo.description}
          </small>
        </div>
      )}

      <div className="order-list-card__meta">
        <span>
          <i className="fas fa-calendar me-1" />
          {orderDateText}
        </span>
        <span className="order-list-card__price">
          {formatTry(getOrderDisplayTotals(order).total)}
        </span>
      </div>

      <div className="order-list-card__cta">
        Detayları gör
        <i className="fas fa-chevron-right" style={{ fontSize: "0.75rem" }} />
      </div>
    </article>
  );
};

/**
 * Mini Stepper (Sipariş kartı için)
 */
const MiniStepper = ({ status }) => {
  const statusInfo = getStatusInfo(status);
  const currentStep = statusInfo.step;

  return (
    <div className="order-mini-stepper">
      {STEPPER_STEPS.map((step, index) => (
        <div
          key={step.key}
          className={`order-mini-stepper__seg ${index <= currentStep ? "done" : ""}`}
        />
      ))}
    </div>
  );
};

/**
 * Sipariş Detay Kartı (Modal gibi)
 * MARKET SİPARİŞ İPTAL KURALLARI:
 * - Sadece aynı gün içinde iptal edilebilir
 * - Sadece kurye teslim süreci başlamadan önce iptal edilebilir
 * - Diğer durumlarda WhatsApp ile müşteri hizmetlerine yönlendirilir
 */
const OrderDetailCard = ({
  order,
  onClose,
  onCancel,
  isCancelling,
  refundRequests = [],
  isAuthenticated = false,
}) => {
  const statusInfo = getStatusInfo(order.status);
  const isCancelled = statusInfo.step === -1;
  const orderNumber = getDisplayOrderNumber(order);
  const orderDateText = getOrderDateTimeText(
    order.orderDate || order.createdAt,
  );
  const address = getOrderAddress(order);
  const items = getOrderItemsList(order);
  const totals = getOrderDisplayTotals(order);

  return (
    <div className="order-detail-card card shadow-lg border-0">
      <div
        className="order-detail-card__header"
        style={{
          background: `linear-gradient(45deg, ${statusInfo.color}, ${statusInfo.color}dd)`,
        }}
      >
        <h2 className="order-detail-card__title">
          <i className="fas fa-box me-2" />
          Sipariş #{orderNumber}
        </h2>
        <button
          className="order-detail-close-btn"
          onClick={onClose}
          type="button"
          title="Kapat"
          aria-label="Sipariş detayını kapat"
        >
          <i className="fas fa-times" />
        </button>
      </div>
      <div className="order-detail-card__body">
        {/* İptal/Problem Banner */}
        {isCancelled && (
          <div
            className="alert d-flex align-items-center mb-4"
            style={{
              backgroundColor: statusInfo.bgColor,
              borderRadius: "12px",
              border: `2px solid ${statusInfo.color}`,
            }}
          >
            <i
              className={`fas ${statusInfo.icon} me-3`}
              style={{ fontSize: "24px", color: statusInfo.color }}
            ></i>
            <div>
              <strong style={{ color: statusInfo.color }}>
                {statusInfo.label}
              </strong>
              <p className="mb-0 small text-muted">{statusInfo.description}</p>
            </div>
          </div>
        )}

        {/* Stepper Timeline */}
        {!isCancelled && <OrderStepper status={order.status} />}

        {/* Bilgiler */}
        <div className="order-detail-card__info-grid">
          <div className="order-detail-card__info-block">
            <h3 className="order-detail-card__section-title">
              <i className="fas fa-info-circle me-2" />
              Sipariş Bilgileri
            </h3>
            <p className="mb-2">
              <strong>Sipariş No:</strong> {orderNumber}
            </p>
            <p className="mb-2">
              <strong>Toplam Tutar:</strong>{" "}
              <span className="order-detail-card__price">
                {formatTry(totals.total)}
              </span>
            </p>
            <p className="mb-2">
              <strong>Sipariş Tarihi:</strong> {orderDateText}
            </p>
          </div>
          <div className="order-detail-card__info-block">
            <h3 className="order-detail-card__section-title">
              <i className="fas fa-truck me-2" />
              Teslimat Bilgileri
            </h3>
            <p className="mb-2">
              <strong>Adres:</strong> {address}
            </p>
            {order.shippingCompany && (
              <p className="mb-2">
                <strong>Kargo Firması:</strong> {order.shippingCompany}
              </p>
            )}
            {order.estimatedDeliveryDate && (
              <p className="mb-2">
                <strong>Tahmini Teslimat:</strong>{" "}
                {new Date(order.estimatedDeliveryDate).toLocaleDateString(
                  "tr-TR",
                )}
              </p>
            )}
          </div>
        </div>

        {/* Ürünler */}
        <div className="order-detail-card__products">
          <h3 className="order-detail-card__section-title">
            <i className="fas fa-shopping-basket me-2" />
            Sipariş Ürünleri
          </h3>
          {items.length > 0 ? (
            <div className="order-detail-card__product-list">
              {items.map((item, index) => (
                <div key={item.id || index} className="order-detail-product">
                  <div className="order-detail-product__icon">
                    <i className="fas fa-box" />
                  </div>
                  <div className="order-detail-product__info">
                    <h4 className="order-detail-product__name">
                      {item.name || item.productName}
                    </h4>
                    <p className="order-detail-product__qty">
                      {getOrderItemUnitLabel(item)}
                    </p>
                  </div>
                  <span className="order-detail-product__total">
                    {formatTry(getOrderItemLineTotal(item))}
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <div className="text-muted small">Ürün detayları bulunamadı.</div>
          )}

          {items.length > 0 && (
            <div className="order-detail-card__summary mt-3 pt-3 border-top">
              <div className="d-flex justify-content-between small mb-1">
                <span>Ürünler</span>
                <span>{formatTry(totals.itemsSubtotal)}</span>
              </div>
              {totals.hasShipping && (
                <div className="d-flex justify-content-between small mb-1">
                  <span>Kargo</span>
                  <span>{formatTry(totals.shippingCost)}</span>
                </div>
              )}
              {totals.hasDiscount && (
                <div className="d-flex justify-content-between small mb-1 text-success">
                  <span>İndirim</span>
                  <span>-{formatTry(totals.totalDiscount)}</span>
                </div>
              )}
              <div className="d-flex justify-content-between fw-bold mt-2">
                <span>Toplam</span>
                <span className="order-detail-card__price">
                  {formatTry(totals.total)}
                </span>
              </div>
            </div>
          )}
        </div>

        <div className="order-detail-card__actions order-actions--sticky">
          <OrderActions
            order={order}
            onCancel={onCancel}
            isCancelling={isCancelling}
            layout="sticky"
            refundRequests={refundRequests}
            isAuthenticated={isAuthenticated}
          />
        </div>
      </div>
    </div>
  );
};

/**
 * Sipariş Stepper (Büyük timeline)
 */
const OrderStepper = ({ status }) => {
  const statusInfo = getStatusInfo(status);
  const currentStep = statusInfo.step;

  return (
    <div className="order-stepper">
      <div className="order-stepper__progress">
        <div
          className="order-stepper__progress-bar"
          style={{ width: `${getStepperProgress(status)}%` }}
        />
      </div>

      <div className="order-stepper__steps">
        {STEPPER_STEPS.map((step, index) => {
          const isCompleted = index < currentStep;
          const isActive = index === currentStep;

          return (
            <div
              key={step.key}
              className={`order-stepper__step${isCompleted ? " is-completed" : ""}${isActive ? " is-active" : ""}`}
            >
              <div className="order-stepper__circle">
                {isCompleted ? (
                  <i className="fas fa-check" />
                ) : (
                  <i className={`fas ${step.icon}`} />
                )}
              </div>
              <span className="order-stepper__label">{step.label}</span>
            </div>
          );
        })}
      </div>

      <div
        className="order-stepper__status"
        style={{
          backgroundColor: statusInfo.bgColor,
          borderColor: statusInfo.color,
        }}
      >
        <i
          className={`fas ${statusInfo.icon} me-2`}
          style={{ color: statusInfo.color }}
        />
        <strong style={{ color: statusInfo.color }}>{statusInfo.label}</strong>
        <p className="mb-0 mt-1 small text-muted">{statusInfo.description}</p>
      </div>
    </div>
  );
};

/**
 * Boş Sipariş Durumu
 */
const EmptyOrdersState = () => {
  const isGuestUser = !localStorage.getItem("token") || !localStorage.getItem("userId");

  return (
    <div className="text-center py-5">
      <div
        className="p-4 rounded-circle mx-auto mb-4 shadow-lg"
        style={{
          backgroundColor: "#fff8f0",
          width: "120px",
          height: "120px",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <i
          className="fas fa-shopping-bag text-warning"
          style={{ fontSize: "3rem" }}
        ></i>
      </div>
      {isGuestUser ? (
        <>
          <h4 className="text-warning fw-bold mb-3">Sipariş Bulunamadı</h4>
          <p className="text-muted fs-6 mb-1">
            Yukarıdaki arama kutusundan telefon numaranız veya sipariş numaranız ile siparişinizi sorgulayabilirsiniz.
          </p>
          <p className="text-muted small">
            Tüm siparişlerinize erişmek için{" "}
            <a href="/login" style={{ color: "#FF8C00", fontWeight: "600" }}>giriş yapın</a>.
          </p>
        </>
      ) : (
        <>
          <h4 className="text-warning fw-bold mb-3">Henüz Siparişiniz Yok</h4>
          <p className="text-muted fs-5">
            İlk siparişinizi vermek için alışverişe başlayın!
          </p>
        </>
      )}
    </div>
  );
};

export default OrderTracking;
