// ==========================================================================
// OrderTracking.jsx - Müşteri Sipariş Takip Ekranı (Geliştirilmiş)
// ==========================================================================
// SignalR entegrasyonu ile real-time sipariş takibi.
// Stepper UI ile adım adım sipariş durumu gösterimi.
// ==========================================================================

import { useEffect, useState, useCallback } from "react";
import { OrderService } from "../services/orderService";
import signalRService, { ConnectionState } from "../services/signalRService";

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
  const normalizedStatus = (status || "pending")
    .toLowerCase()
    .replace(/ /g, "_");
  return ORDER_STATUSES[normalizedStatus] || ORDER_STATUSES.pending;
};

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

const getOrderItems = (order) => {
  if (Array.isArray(order?.items) && order.items.length > 0) {
    return order.items;
  }
  if (Array.isArray(order?.orderItems) && order.orderItems.length > 0) {
    return order.orderItems;
  }
  if (
    Array.isArray(order?.raw?.orderItems) &&
    order.raw.orderItems.length > 0
  ) {
    return order.raw.orderItems;
  }
  if (Array.isArray(order?.raw?.items) && order.raw.items.length > 0) {
    return order.raw.items;
  }
  return [];
};

// ==========================================================================
// ANA COMPONENT
// ==========================================================================

// ===========================================================================
// WHATSAPP VE MÜŞTERİ HİZMETLERİ SABİTLERİ
// Market sipariş iptal politikası için iletişim bilgileri
// ===========================================================================
const CUSTOMER_SUPPORT = {
  whatsappNumber: "905334783072", // WhatsApp için (başında + yok, ülke kodu ile)
  phoneDisplay: "+90 533 478 30 72", // Görüntüleme için
  email: "golturkbuku@golkoygurme.com.tr",
  // WhatsApp mesaj şablonu - sipariş numarası dinamik olarak eklenir
  getWhatsAppMessage: (orderNumber) =>
    `Merhaba, ${orderNumber} numaralı siparişim hakkında destek almak istiyorum.`,
};

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
  const [notification, setNotification] = useState(null);
  const [cancellingOrderId, setCancellingOrderId] = useState(null); // İptal işlemi yapılan sipariş ID'si

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
    } catch (error) {
      console.error("Siparişler yüklenemedi:", error);

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

  // =========================================================================
  // SİPARİŞ İPTAL FONKSİYONU - MARKET KURALLARI
  // 1. Sadece aynı gün içinde iptal edilebilir
  // 2. Sadece hazırlanmadan önce (new, pending, confirmed) iptal edilebilir
  // 3. Diğer durumlarda müşteri hizmetleriyle iletişime yönlendirilir
  // =========================================================================
  const handleCancelOrder = useCallback(
    async (order) => {
      const orderId = order.id || order.orderId;
      const orderNumber = getDisplayOrderNumber(order);

      // Frontend tarafında da kontrol yap (backend zaten yapıyor ama UX için)
      const orderDate = new Date(order.orderDate || order.createdAt);
      const today = new Date();
      const isSameDay = orderDate.toDateString() === today.toDateString();
      const cancellableStatuses = ["new", "pending", "confirmed"];
      const status = (order.status || "").toLowerCase();
      const isCancellableStatus = cancellableStatuses.includes(status);

      // Aynı gün değilse veya iptal edilemez durumdaysa uyarı göster
      if (!isSameDay || !isCancellableStatus) {
        const reason = !isSameDay
          ? "Sipariş sadece aynı gün içinde iptal edilebilir."
          : "Siparişiniz hazırlanmaya başladı.";

        setNotification({
          type: "warning",
          title: "İptal Edilemiyor",
          message: `${reason} İptal için müşteri hizmetleriyle iletişime geçiniz.`,
          color: "#dc3545",
          bgColor: "#f8d7da",
          showWhatsApp: true,
          orderNumber,
        });
        return;
      }

      // Kullanıcıdan onay al
      const confirmCancel = window.confirm(
        `${orderNumber} numaralı siparişinizi iptal etmek istediğinize emin misiniz?\n\n` +
          `Bu işlem geri alınamaz.`,
      );

      if (!confirmCancel) return;

      setCancellingOrderId(orderId);

      try {
        const response = await OrderService.cancel(orderId);

        if (response.success) {
          // Başarılı iptal
          setNotification({
            type: "success",
            title: "Sipariş İptal Edildi",
            message: `${orderNumber} numaralı siparişiniz başarıyla iptal edildi.`,
            color: "#28a745",
            bgColor: "#d4edda",
          });

          // Sipariş listesini güncelle
          await loadOrders();
          setSelectedOrder(null);
        } else {
          // Backend hatası - müşteri hizmetlerine yönlendir
          setNotification({
            type: "error",
            title: "İptal Edilemedi",
            message:
              response.message ||
              "Sipariş iptal edilemedi. Müşteri hizmetleriyle iletişime geçiniz.",
            color: "#dc3545",
            bgColor: "#f8d7da",
            showWhatsApp: true,
            orderNumber,
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
          orderNumber,
        });
      } finally {
        setCancellingOrderId(null);
      }
    },
    [loadOrders],
  );

  // =========================================================================
  // WHATSAPP İLETİŞİM FONKSİYONU
  // Müşteri hizmetlerine hızlı erişim için
  // =========================================================================
  const openWhatsAppSupport = useCallback((orderNumber) => {
    const message = CUSTOMER_SUPPORT.getWhatsAppMessage(
      orderNumber || "Sipariş",
    );
    const whatsappUrl = `https://wa.me/${CUSTOMER_SUPPORT.whatsappNumber}?text=${encodeURIComponent(message)}`;
    window.open(whatsappUrl, "_blank");
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
      console.log("[OrderTracking] Misafir kullanıcı, polling aktif edilecek");

      // ================================================================
      // MİSAFİR İÇİN POLLİNG MEKANİZMASI
      // Her 15 saniyede sipariş durumunu kontrol et
      // NEDEN: SignalR yetkisiz kullanıcılar için çalışmaz
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
      }, 15000); // 15 saniye

      return () => clearInterval(pollInterval);
    }

    // SignalR bağlantısı kur (sadece giriş yapmış kullanıcılar için)
    const connectSignalR = async () => {
      try {
        const connected = await signalRService.connectCustomer();
        if (connected) {
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
        }
      } catch (error) {
        console.error("[OrderTracking] SignalR bağlantı hatası:", error);
        setConnectionStatus(ConnectionState.FAILED);
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
      setOrders((prev) =>
        prev.map((order) =>
          order.id === data.orderId || order.orderNumber === data.orderNumber
            ? { ...order, status: data.newStatus || data.status }
            : order,
        ),
      );

      // Seçili sipariş güncellemesi
      setSelectedOrder((prev) =>
        prev &&
        (prev.id === data.orderId || prev.orderNumber === data.orderNumber)
          ? { ...prev, status: data.newStatus || data.status }
          : prev,
      );

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
      unsubscribeStatus();
      unsubscribeDelivery();
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
    <div
      style={{
        minHeight: "100vh",
        background:
          "linear-gradient(135deg, #fff3e0 0%, #ffe0b2 50%, #ffcc80 100%)",
        paddingTop: "2rem",
        paddingBottom: "2rem",
      }}
    >
      <div className="container">
        {/* Real-time Bildirim */}
        {notification && (
          <div
            className="alert d-flex align-items-center shadow-lg mb-4"
            style={{
              backgroundColor: notification.color + "15",
              borderLeft: `4px solid ${notification.color}`,
              borderRadius: "12px",
              animation: "slideIn 0.3s ease",
            }}
          >
            <i
              className={`fas ${notification.icon} me-3`}
              style={{ fontSize: "24px", color: notification.color }}
            ></i>
            <div className="flex-grow-1">
              <strong>{notification.title}</strong>
              <p className="mb-0 small text-muted">{notification.message}</p>
              {/* WhatsApp ile İletişim Butonu - İptal edilemezse göster */}
              {notification.showWhatsApp && (
                <button
                  className="btn btn-success btn-sm mt-2"
                  onClick={() => openWhatsAppSupport(notification.orderNumber)}
                  style={{
                    background: "#25D366",
                    border: "none",
                    borderRadius: "20px",
                    padding: "6px 16px",
                  }}
                >
                  <i className="fab fa-whatsapp me-2"></i>
                  WhatsApp ile İletişime Geç
                </button>
              )}
            </div>
            {/* Bildirim kapat butonu */}
            <button
              className="btn btn-sm ms-auto"
              onClick={() => setNotification(null)}
              style={{
                background: "transparent",
                border: "none",
                fontSize: "20px",
                fontWeight: "bold",
                color: notification.color,
                lineHeight: 1,
                padding: "0 8px",
              }}
              title="Kapat"
            >
              ×
            </button>
          </div>
        )}

        {/* Sipariş No ile Arama kaldırıldı */}

        {/* Seçilen Sipariş Detayı */}
        {selectedOrder && (
          <OrderDetailCard
            order={selectedOrder}
            onClose={() => setSelectedOrder(null)}
            onCancel={handleCancelOrder}
            onWhatsAppSupport={openWhatsAppSupport}
            isCancelling={
              cancellingOrderId === (selectedOrder.id || selectedOrder.orderId)
            }
          />
        )}
        {loadingDetail && (
          <div
            className="position-fixed top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center"
            style={{ background: "rgba(255,255,255,0.5)", zIndex: 1060 }}
          >
            <div
              className="spinner-border text-warning"
              style={{ width: "4rem", height: "4rem" }}
            ></div>
          </div>
        )}

        {/* Tüm Siparişler */}
        <div
          className="card shadow-lg border-0"
          style={{ borderRadius: "20px" }}
        >
          <div
            className="card-header text-white border-0"
            style={{
              background: "linear-gradient(45deg, #6f42c1, #e83e8c)",
              borderTopLeftRadius: "20px",
              borderTopRightRadius: "20px",
              padding: "1.5rem",
            }}
          >
            <h4 className="mb-0 fw-bold">
              <i className="fas fa-list me-2"></i>Tüm Siparişlerim
              <span className="badge bg-white text-primary ms-2">
                {orders.length}
              </span>
            </h4>
          </div>
          <div className="card-body" style={{ padding: "2rem" }}>
            {orders.length === 0 ? (
              <EmptyOrdersState />
            ) : (
              <div className="row">
                {orders.map((order) => (
                  <div
                    key={order.id || order.orderId || order.orderNumber}
                    className="col-md-6 mb-4"
                  >
                    <OrderCard
                      order={order}
                      onClick={() => handleOpenOrder(order)}
                    />
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* CSS Animations */}
      <style>{`
        @keyframes slideIn {
          from {
            opacity: 0;
            transform: translateY(-20px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
        @keyframes pulse {
          0%, 100% { transform: scale(1); }
          50% { transform: scale(1.05); }
        }
        .order-detail-close-btn {
          width: 40px;
          height: 40px;
          border-radius: 12px;
          border: none;
          background: rgba(255, 255, 255, 0.2);
          color: #ffffff;
          display: inline-flex;
          align-items: center;
          justify-content: center;
          font-size: 16px;
          cursor: pointer;
          transition: all 0.2s ease;
          box-shadow: 0 6px 16px rgba(0, 0, 0, 0.15);
          backdrop-filter: blur(4px);
        }
        .order-detail-close-btn:hover {
          transform: translateY(-1px) rotate(6deg);
          background: rgba(255, 255, 255, 0.3);
        }
        .order-detail-close-btn:active {
          transform: translateY(0);
          box-shadow: none;
        }
      `}</style>
    </div>
  );
};

// ==========================================================================
// ALT COMPONENTLER
// ==========================================================================

/**
 * Sipariş Kartı
 */
const OrderCard = ({ order, onClick }) => {
  const statusInfo = getStatusInfo(order.status);
  const isCancelled = statusInfo.step === -1;
  const orderNumber = getDisplayOrderNumber(order);
  const orderDateText = getOrderDateText(order.orderDate);

  return (
    <div
      className="card shadow-sm border-0 h-100"
      style={{
        borderRadius: "15px",
        cursor: "pointer",
        transition: "transform 0.2s, box-shadow 0.2s",
      }}
      onClick={onClick}
      onMouseOver={(e) => {
        e.currentTarget.style.transform = "translateY(-4px)";
        e.currentTarget.style.boxShadow = "0 8px 25px rgba(0,0,0,0.15)";
      }}
      onMouseOut={(e) => {
        e.currentTarget.style.transform = "translateY(0)";
        e.currentTarget.style.boxShadow = "";
      }}
    >
      <div className="card-body" style={{ padding: "1.5rem" }}>
        {/* Header */}
        <div className="d-flex justify-content-between align-items-start mb-3">
          <h6 className="fw-bold mb-0">Sipariş #{orderNumber}</h6>
          <span
            className="badge px-3 py-2"
            style={{
              backgroundColor: statusInfo.bgColor,
              color: statusInfo.color,
              borderRadius: "20px",
            }}
          >
            <i className={`fas ${statusInfo.icon} me-1`}></i>
            {statusInfo.shortLabel}
          </span>
        </div>

        {/* Mini Stepper (iptal/problem durumlarında gösterme) */}
        {!isCancelled && <MiniStepper status={order.status} />}

        {/* İptal/Problem Banner */}
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
              <i className={`fas ${statusInfo.icon} me-2`}></i>
              {statusInfo.description}
            </small>
          </div>
        )}

        {/* Bilgiler */}
        <p className="text-muted mb-2">
          <i className="fas fa-calendar me-2"></i>
          {orderDateText}
        </p>

        <p className="fw-bold mb-3" style={{ color: "#ff6f00" }}>
          <i className="fas fa-tag me-2"></i>₺
          {Number(order.totalAmount || order.totalPrice || 0).toFixed(2)}
        </p>

        <button
          className="btn btn-outline-warning btn-sm fw-bold w-100"
          style={{ borderRadius: "15px" }}
        >
          <i className="fas fa-eye me-2"></i>
          Detayları Görüntüle
        </button>
      </div>
    </div>
  );
};

/**
 * Mini Stepper (Sipariş kartı için)
 */
const MiniStepper = ({ status }) => {
  const statusInfo = getStatusInfo(status);
  const currentStep = statusInfo.step;

  return (
    <div className="d-flex justify-content-between mb-3" style={{ gap: "4px" }}>
      {STEPPER_STEPS.map((step, index) => (
        <div
          key={step.key}
          className="flex-grow-1"
          style={{
            height: "6px",
            borderRadius: "3px",
            backgroundColor: index <= currentStep ? "#28a745" : "#e9ecef",
            transition: "background-color 0.3s",
          }}
        />
      ))}
    </div>
  );
};

/**
 * Sipariş Detay Kartı (Modal gibi)
 * MARKET SİPARİŞ İPTAL KURALLARI:
 * - Sadece aynı gün içinde iptal edilebilir
 * - Sadece hazırlanmadan önce (new, pending, confirmed) iptal edilebilir
 * - Diğer durumlarda WhatsApp ile müşteri hizmetlerine yönlendirilir
 */
const OrderDetailCard = ({
  order,
  onClose,
  onCancel,
  onWhatsAppSupport,
  isCancelling,
}) => {
  const statusInfo = getStatusInfo(order.status);
  const isCancelled = statusInfo.step === -1;
  const orderNumber = getDisplayOrderNumber(order);
  const orderDateText = getOrderDateTimeText(
    order.orderDate || order.createdAt,
  );
  const address = getOrderAddress(order);
  const items = getOrderItems(order);

  // İptal edilebilirlik kontrolü
  const orderDate = new Date(order.orderDate || order.createdAt);
  const today = new Date();
  const isSameDay = orderDate.toDateString() === today.toDateString();
  const cancellableStatuses = ["new", "pending", "confirmed"];
  const status = (order.status || "").toLowerCase();
  const isCancellableStatus = cancellableStatuses.includes(status);
  const canCancel = isSameDay && isCancellableStatus && !isCancelled;

  // İptal edilememe sebebi mesajı
  const getCancelDisabledReason = () => {
    if (isCancelled) return null;
    if (!isSameDay) return "Sipariş sadece aynı gün içinde iptal edilebilir";
    if (!isCancellableStatus) {
      const statusMessages = {
        preparing: "Siparişiniz hazırlanmaya başladı",
        processing: "Siparişiniz işleniyor",
        ready: "Siparişiniz teslimata hazır",
        assigned: "Kuryeniz atandı",
        pickedup: "Kurye siparişinizi aldı",
        intransit: "Siparişiniz yolda",
        outfordelivery: "Siparişiniz dağıtımda",
        delivered: "Siparişiniz teslim edildi",
      };
      return statusMessages[status] || "Bu aşamada iptal edilemiyor";
    }
    return null;
  };

  const cancelDisabledReason = getCancelDisabledReason();

  return (
    <div
      className="card shadow-lg border-0 mb-4"
      style={{ borderRadius: "20px" }}
    >
      <div
        className="card-header text-white border-0 d-flex justify-content-between align-items-center"
        style={{
          background: `linear-gradient(45deg, ${statusInfo.color}, ${statusInfo.color}dd)`,
          borderTopLeftRadius: "20px",
          borderTopRightRadius: "20px",
          padding: "1.5rem",
        }}
      >
        <h5 className="mb-0 fw-bold">
          <i className="fas fa-package me-2"></i>
          Sipariş #{orderNumber}
        </h5>
        {/* Kapat butonu - × simgesi ile */}
        <button
          className="order-detail-close-btn"
          onClick={onClose}
          type="button"
          title="Kapat"
          aria-label="Sipariş detayını kapat"
        >
          <i className="fas fa-times"></i>
        </button>
      </div>
      <div className="card-body" style={{ padding: "2rem" }}>
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
        <div className="row mt-4">
          <div className="col-md-6">
            <h6 className="fw-bold mb-3" style={{ color: "#ff6f00" }}>
              <i className="fas fa-info-circle me-2"></i>Sipariş Bilgileri
            </h6>
            <p className="mb-2">
              <strong>Sipariş No:</strong> {orderNumber}
            </p>
            <p className="mb-2">
              <strong>Toplam Tutar:</strong>{" "}
              <span className="fw-bold" style={{ color: "#ff6f00" }}>
                ₺{Number(order.totalAmount || order.totalPrice || 0).toFixed(2)}
              </span>
            </p>
            <p className="mb-2">
              <strong>Sipariş Tarihi:</strong> {orderDateText}
            </p>
          </div>
          <div className="col-md-6">
            <h6 className="fw-bold mb-3" style={{ color: "#ff6f00" }}>
              <i className="fas fa-truck me-2"></i>Teslimat Bilgileri
            </h6>
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
        <div className="mt-4">
          <h6 className="fw-bold mb-3" style={{ color: "#ff6f00" }}>
            <i className="fas fa-shopping-basket me-2"></i>Sipariş Ürünleri
          </h6>
          {items.length > 0 ? (
            <div className="row">
              {items.map((item, index) => (
                <div key={item.id || index} className="col-md-6 mb-3">
                  <div
                    className="card border-0 shadow-sm"
                    style={{ borderRadius: "12px" }}
                  >
                    <div className="card-body p-3">
                      <div className="d-flex align-items-center">
                        <div
                          className="me-3 d-flex align-items-center justify-content-center"
                          style={{
                            width: "50px",
                            height: "50px",
                            backgroundColor: "#fff8f0",
                            borderRadius: "10px",
                          }}
                        >
                          <i
                            className="fas fa-box"
                            style={{ color: "#ff6f00" }}
                          ></i>
                        </div>
                        <div className="flex-grow-1">
                          <h6 className="mb-1 fw-bold">
                            {item.name || item.productName}
                          </h6>
                          <p className="mb-0 text-muted small">
                            {item.quantity} adet × ₺
                            {Number(item.unitPrice || item.price || 0).toFixed(
                              2,
                            )}
                          </p>
                        </div>
                        <span className="fw-bold" style={{ color: "#ff6f00" }}>
                          ₺
                          {Number(
                            (item.quantity || 1) *
                              (item.unitPrice || item.price || 0),
                          ).toFixed(2)}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="text-muted small">Ürün detayları bulunamadı.</div>
          )}
        </div>

        {/* ================================================================
            SİPARİŞ İPTAL VE DESTEK BUTONLARI
            Market kurallarına göre iptal butonu veya WhatsApp desteği gösterilir
            ================================================================ */}
        <div className="mt-4 pt-4 border-top">
          <div className="d-flex flex-wrap gap-2 justify-content-center">
            {/* İptal Butonu - Sadece iptal edilebilir siparişlerde göster */}
            {canCancel && onCancel && (
              <button
                className="btn btn-outline-danger"
                onClick={() => onCancel(order)}
                disabled={isCancelling}
                style={{
                  borderRadius: "25px",
                  padding: "10px 24px",
                  fontWeight: "600",
                }}
              >
                {isCancelling ? (
                  <>
                    <span className="spinner-border spinner-border-sm me-2"></span>
                    İptal Ediliyor...
                  </>
                ) : (
                  <>
                    <i className="fas fa-times-circle me-2"></i>
                    Siparişi İptal Et
                  </>
                )}
              </button>
            )}

            {/* İptal edilemezse sebep ve WhatsApp butonu göster */}
            {!canCancel && !isCancelled && cancelDisabledReason && (
              <div className="w-100 text-center">
                <div
                  className="alert alert-warning d-inline-flex align-items-center mb-3"
                  style={{ borderRadius: "12px", padding: "12px 20px" }}
                >
                  <i className="fas fa-info-circle me-2"></i>
                  <span>
                    {cancelDisabledReason}. İptal için müşteri hizmetleriyle
                    iletişime geçiniz.
                  </span>
                </div>
              </div>
            )}

            {/* WhatsApp Destek Butonu - Her zaman göster (iptal edilemezse öne çıkar) */}
            {onWhatsAppSupport && !isCancelled && (
              <button
                className="btn"
                onClick={() => onWhatsAppSupport(orderNumber)}
                style={{
                  background: canCancel ? "#f8f9fa" : "#25D366",
                  color: canCancel ? "#25D366" : "white",
                  border: canCancel ? "2px solid #25D366" : "none",
                  borderRadius: "25px",
                  padding: "10px 24px",
                  fontWeight: "600",
                }}
              >
                <i className="fab fa-whatsapp me-2"></i>
                {canCancel ? "Destek Al" : "WhatsApp ile İletişime Geç"}
              </button>
            )}
          </div>

          {/* İletişim bilgileri */}
          {!canCancel && !isCancelled && (
            <div className="text-center mt-3">
              <small className="text-muted">
                <i className="fas fa-phone me-1"></i>
                Telefon: +90 533 478 30 72
              </small>
            </div>
          )}
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
    <div className="stepper-container">
      {/* Progress Bar */}
      <div
        className="position-relative mb-4"
        style={{
          height: "6px",
          backgroundColor: "#e9ecef",
          borderRadius: "3px",
        }}
      >
        <div
          className="position-absolute top-0 start-0 h-100"
          style={{
            width: `${getStepperProgress(status)}%`,
            backgroundColor: "#28a745",
            borderRadius: "3px",
            transition: "width 0.5s ease",
          }}
        />
      </div>

      {/* Steps */}
      <div className="d-flex justify-content-between">
        {STEPPER_STEPS.map((step, index) => {
          const isCompleted = index < currentStep;
          const isActive = index === currentStep;

          return (
            <div
              key={step.key}
              className="text-center"
              style={{ flex: 1, maxWidth: "100px" }}
            >
              {/* Step Circle */}
              <div
                className={`mx-auto mb-2 d-flex align-items-center justify-content-center rounded-circle ${
                  isActive ? "shadow-lg" : ""
                }`}
                style={{
                  width: isActive ? "56px" : "44px",
                  height: isActive ? "56px" : "44px",
                  backgroundColor: isCompleted
                    ? "#28a745"
                    : isActive
                      ? "#ff6f00"
                      : "#e9ecef",
                  color: isCompleted || isActive ? "white" : "#6c757d",
                  transition: "all 0.3s ease",
                  animation: isActive ? "pulse 2s infinite" : "none",
                }}
              >
                {isCompleted ? (
                  <i className="fas fa-check"></i>
                ) : (
                  <i
                    className={`fas ${step.icon}`}
                    style={{ fontSize: isActive ? "18px" : "14px" }}
                  ></i>
                )}
              </div>

              {/* Step Label */}
              <small
                className={`d-block ${
                  isCompleted
                    ? "text-success fw-bold"
                    : isActive
                      ? "fw-bold"
                      : "text-muted"
                }`}
                style={{
                  fontSize: isActive ? "13px" : "11px",
                  color: isActive ? "#ff6f00" : undefined,
                }}
              >
                {step.label}
              </small>
            </div>
          );
        })}
      </div>

      {/* Mevcut Durum Açıklaması */}
      <div
        className="text-center mt-4 p-3"
        style={{
          backgroundColor: statusInfo.bgColor,
          borderRadius: "12px",
          border: `2px solid ${statusInfo.color}`,
        }}
      >
        <i
          className={`fas ${statusInfo.icon} me-2`}
          style={{ color: statusInfo.color }}
        ></i>
        <strong style={{ color: statusInfo.color }}>{statusInfo.label}</strong>
        <p className="mb-0 mt-1 small text-muted">{statusInfo.description}</p>
      </div>
    </div>
  );
};

/**
 * Boş Sipariş Durumu
 */
const EmptyOrdersState = () => (
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
    <h4 className="text-warning fw-bold mb-3">Henüz Siparişiniz Yok</h4>
    <p className="text-muted fs-5">
      İlk siparişinizi vermek için alışverişe başlayın!
    </p>
  </div>
);

export default OrderTracking;
