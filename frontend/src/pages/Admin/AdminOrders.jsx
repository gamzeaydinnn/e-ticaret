import { useEffect, useState, useCallback, useRef } from "react";
import { AdminService } from "../../services/adminService";
import { CourierService } from "../../services/courierService";
import storeAttendantService from "../../services/storeAttendantService";
import {
  assignCourier as dispatcherAssignCourier,
  getCouriers as dispatcherGetCouriers,
} from "../../services/dispatcherService";
import { useAuth } from "../../contexts/AuthContext";
import { useAdminSignalR } from "../../contexts/AdminSignalRContext";

// ============================================================
// ADMIN ORDERS - Sipariş Yönetimi
// ============================================================
// Bu sayfa admin panelinde siparişlerin yönetimini sağlar.
// SignalR ile real-time güncellemeler alır, fallback olarak polling kullanır.
// StoreAttendant rolü: Sadece sipariş hazırlık + kurye atama
// ============================================================

// Polling aralığı (milisaniye) - SignalR bağlı değilken fallback olarak kullanılır
const POLLING_INTERVAL = 15000;

export default function AdminOrders() {
  // Kullanıcı rolü kontrolü
  const { user } = useAuth();
  const userRole = user?.role || "";
  const isStoreAttendant = userRole === "StoreAttendant";

  // Merkezi SignalR bağlantısı — AdminSignalRProvider tarafından yönetilir
  const { isConnected: signalRConnected } = useAdminSignalR();

  const [orders, setOrders] = useState([]);
  const [couriers, setCouriers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [assigningCourier, setAssigningCourier] = useState(false);

  // ============================================================
  // ANLIK GÜNCELLEME (POLLING ve SignalR) STATELERİ
  // ============================================================
  const [autoRefresh, setAutoRefresh] = useState(true); // Otomatik yenileme aktif mi?
  const [lastUpdate, setLastUpdate] = useState(null); // Son güncelleme zamanı
  const [isRefreshing, setIsRefreshing] = useState(false); // Yenileme animasyonu
  const pollingRef = useRef(null);

  // ============================================================
  // VERİ YÜKLEME FONKSİYONU
  // ============================================================
  const loadData = useCallback(
    async (showLoading = true) => {
      try {
        if (showLoading) setIsRefreshing(true);

        const couriersData = isStoreAttendant
          ? await dispatcherGetCouriers()
          : await CourierService.getAll();
        // Gerçek siparişleri backend'den çek
        const ordersData = await AdminService.getOrders();
        let filteredOrders = Array.isArray(ordersData) ? ordersData : [];

        // ============================================================================
        // STORE ATTENDANT SİPARİŞ FİLTRELEME
        // StoreAttendant artık Admin ile aynı yetkilere sahip olduğundan
        // tüm sipariş statülerini görebilir. Sadece tamamlanmış siparişler
        // (delivered, refunded) isteğe bağlı olarak filtrelenebilir.
        // ============================================================================
        // NOT: Artık StoreAttendant için filtreleme yapılmıyor
        // Admin ile aynı listeyi görüntüler
        // Eski kod referans için aşağıda tutulmuştur:
        // if (isStoreAttendant) {
        //   const allowedStatuses = ["new", "pending", "paid", "confirmed", "preparing", "ready", "assigned"];
        //   filteredOrders = filteredOrders.filter((o) => allowedStatuses.includes((o.status || "").toLowerCase()));
        // }

        setOrders(filteredOrders);

        // Kurye listesini set et
        let courierList = [];
        if (isStoreAttendant) {
          // dispatcherGetCouriers { success, data: { couriers: [...] } } döner
          const result = couriersData;
          console.log(
            "🚴 [AdminOrders] StoreAttendant kurye API yanıtı:",
            result,
          );
          if (result?.success && result?.data) {
            courierList = result.data.couriers || result.data || [];
          } else {
            courierList = result?.couriers || [];
          }
        } else {
          // CourierService.getAll() direkt array döner
          courierList = Array.isArray(couriersData)
            ? couriersData
            : couriersData?.data || [];
        }
        console.log("🚴 [AdminOrders] Final kurye listesi:", courierList);
        setCouriers(Array.isArray(courierList) ? courierList : []);

        setLastUpdate(new Date());
      } catch (error) {
        console.error("Veri yükleme hatası:", error);
      } finally {
        setLoading(false);
        setIsRefreshing(false);
      }
    },
    [isStoreAttendant],
  );

  // ============================================================
  // İLK YÜKLEME
  // ============================================================
  useEffect(() => {
    loadData();
  }, [loadData]);

  // ============================================================
  // SIGNALR ENTEGRASYONU — Merkezi AdminSignalR context'i kullanır
  // Artık kendi bağlantısını kurmaz, global event'leri dinler.
  // NEDEN: Birden fazla admin aynı anda girdiğinde her biri
  // merkezi context üzerinden aynı bildirimleri alır.
  // ============================================================
  useEffect(() => {
    // Yeni sipariş handler
    const handleOrderCreated = (e) => {
      const notification = e.detail;
      console.log("📦 Yeni sipariş bildirimi alındı:", notification);
      playNotificationSound();

      if (notification && (notification.orderId || notification.id)) {
        loadData(false);
      } else if (notification) {
        setOrders((prev) => {
          const exists = prev.some(
            (o) =>
              o.id === notification.id ||
              o.orderNumber === notification.orderNumber,
          );
          if (exists) return prev;
          return [notification, ...prev];
        });
      }
      setLastUpdate(new Date());
    };

    // Sipariş durumu değişikliği handler
    const handleOrderStatusChanged = (e) => {
      const data = e.detail;
      console.log("🔄 Sipariş durumu değişti:", data);
      setOrders((prev) =>
        prev.map((o) =>
          o.id === data.orderId ? { ...o, status: data.newStatus } : o,
        ),
      );
      setLastUpdate(new Date());
    };

    // Kurye atandı handler
    const handleDeliveryAssigned = (e) => {
      const data = e.detail;
      console.log("🚚 Kurye atandı:", data);
      setOrders((prev) =>
        prev.map((o) =>
          o.id === data.orderId
            ? {
                ...o,
                courierId: data.courierId,
                courierName: data.courierName,
                status: "assigned",
              }
            : o,
        ),
      );
      setLastUpdate(new Date());
    };

    // Teslimat tamamlandı handler
    const handleDeliveryCompleted = (e) => {
      const data = e.detail;
      console.log("✅ Teslimat tamamlandı:", data);
      setOrders((prev) =>
        prev.map((o) =>
          o.id === data.orderId ? { ...o, status: "delivered" } : o,
        ),
      );
      setLastUpdate(new Date());
    };

    // Teslimat başarısız handler
    const handleDeliveryFailed = (e) => {
      const data = e.detail;
      console.log("❌ Teslimat başarısız:", data);
      setOrders((prev) =>
        prev.map((o) =>
          o.id === data.orderId
            ? { ...o, status: "delivery_failed", failureReason: data.reason }
            : o,
        ),
      );
      setLastUpdate(new Date());
      playNotificationSound();
    };

    // Merkezi context'in yayınladığı global event'leri dinle
    window.addEventListener("adminNewOrder", handleOrderCreated);
    window.addEventListener("adminOrderStatusChanged", handleOrderStatusChanged);
    window.addEventListener("adminDeliveryAssigned", handleDeliveryAssigned);
    window.addEventListener("adminDeliveryCompleted", handleDeliveryCompleted);
    window.addEventListener("adminDeliveryFailed", handleDeliveryFailed);
    // Delivery hub'ın doğrudan yayınladığı event'leri de dinle
    window.addEventListener("adminDeliveryOrderCreated", handleOrderCreated);
    window.addEventListener("adminDeliveryStatusChanged", handleOrderStatusChanged);

    return () => {
      window.removeEventListener("adminNewOrder", handleOrderCreated);
      window.removeEventListener("adminOrderStatusChanged", handleOrderStatusChanged);
      window.removeEventListener("adminDeliveryAssigned", handleDeliveryAssigned);
      window.removeEventListener("adminDeliveryCompleted", handleDeliveryCompleted);
      window.removeEventListener("adminDeliveryFailed", handleDeliveryFailed);
      window.removeEventListener("adminDeliveryOrderCreated", handleOrderCreated);
      window.removeEventListener("adminDeliveryStatusChanged", handleOrderStatusChanged);
    };
  }, [loadData]);

  // Bildirim sesi çalma - Mixkit ses dosyası
  const playNotificationSound = () => {
    const soundEnabled =
      localStorage.getItem("notificationSoundEnabled") !== "false";
    if (soundEnabled) {
      try {
        const audio = new Audio(
          "/sounds/mixkit-melodic-race-countdown-1955.wav",
        );
        audio.volume = 0.5;
        audio.play().catch(() => {
          // Kullanıcı etkileşimi olmadan ses çalınamaz, sessizce devam et
        });
      } catch (error) {
        console.warn("Bildirim sesi çalınamadı:", error);
      }
    }
  };

  // Polling mekanizması - SignalR bağlı değilken fallback olarak kullan
  useEffect(() => {
    // SignalR bağlıysa polling'i devre dışı bırak
    if (signalRConnected) {
      if (pollingRef.current) {
        clearInterval(pollingRef.current);
        pollingRef.current = null;
        console.log("⏹️ SignalR aktif, polling durduruldu");
      }
      return;
    }

    // SignalR bağlı değilse ve autoRefresh açıksa polling kullan
    if (autoRefresh && !signalRConnected) {
      pollingRef.current = setInterval(() => {
        loadData(false);
      }, POLLING_INTERVAL);
      console.log("🔄 SignalR bağlı değil, polling aktif (15 saniye)");
    }

    return () => {
      if (pollingRef.current) {
        clearInterval(pollingRef.current);
        console.log("⏹️ Sipariş polling durduruldu");
      }
    };
  }, [autoRefresh, signalRConnected, loadData]);

  // ============================================================
  // FİLTRE STATE'LERİ
  // ============================================================
  const [statusFilter, setStatusFilter] = useState("all"); // Durum filtresi
  const [paymentFilter, setPaymentFilter] = useState("all"); // Ödeme filtresi
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  // ============================================================
  // İADE TALEBİ YÖNETİM STATELERİ
  // ============================================================
  const [refundRequests, setRefundRequests] = useState([]);
  const [refundLoading, setRefundLoading] = useState(false);
  const [showRefundPanel, setShowRefundPanel] = useState(false);
  const [refundProcessing, setRefundProcessing] = useState(null); // İşlenen iade talebi ID
  const [refundAdminNote, setRefundAdminNote] = useState("");

  // ============================================================
  // İADE TALEPLERİNİ YÜKLEME
  // ============================================================
  const loadRefundRequests = useCallback(async () => {
    try {
      setRefundLoading(true);
      const result = await AdminService.getRefundRequests();
      const data = result?.data || result || [];
      setRefundRequests(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error("[AdminOrders] İade talepleri yükleme hatası:", err);
    } finally {
      setRefundLoading(false);
    }
  }, []);

  // İade paneli açılınca verileri yükle
  useEffect(() => {
    if (showRefundPanel) {
      loadRefundRequests();
    }
  }, [showRefundPanel, loadRefundRequests]);

  // İade talebi onay/ret işlemi
  const handleProcessRefund = async (refundRequestId, approve) => {
    const actionText = approve ? "onaylamak" : "reddetmek";
    if (
      !window.confirm(
        `Bu iade talebini ${actionText} istediğinize emin misiniz?`,
      )
    )
      return;

    setRefundProcessing(refundRequestId);
    try {
      await AdminService.processRefundRequest(refundRequestId, {
        approve,
        adminNote: refundAdminNote || null,
      });
      alert(
        approve
          ? "İade onaylandı ve para iadesi başlatıldı."
          : "İade talebi reddedildi.",
      );
      setRefundAdminNote("");
      await loadRefundRequests();
      await loadData(false);
    } catch (err) {
      const msg =
        err?.response?.data?.message || err?.message || "İşlem başarısız.";
      alert("Hata: " + msg);
    } finally {
      setRefundProcessing(null);
    }
  };

  // Başarısız iadeyi yeniden dene
  const handleRetryRefund = async (refundRequestId) => {
    if (!window.confirm("Para iadesini yeniden denemek istiyor musunuz?"))
      return;

    setRefundProcessing(refundRequestId);
    try {
      await AdminService.retryRefund(refundRequestId);
      alert("Para iadesi yeniden denendi.");
      await loadRefundRequests();
      await loadData(false);
    } catch (err) {
      const msg =
        err?.response?.data?.message || err?.message || "İşlem başarısız.";
      alert("Hata: " + msg);
    } finally {
      setRefundProcessing(null);
    }
  };

  const normalizeStatus = (status) => {
    const raw = (status || "").toString().trim().toLowerCase();
    switch (raw) {
      case "outfordelivery":
      case "out_for_delivery":
      case "out-for-delivery":
      case "in_transit":
      case "intransit":
        return "out_for_delivery";
      case "pickedup":
      case "picked_up":
      case "picked-up":
        return "picked_up";
      case "deliverypaymentpending":
      case "delivery_payment_pending":
      case "delivery-payment-pending":
        return "delivery_payment_pending";
      case "deliveryfailed":
      case "delivery_failed":
      case "delivery-failed":
        return "delivery_failed";
      case "paymentfailed":
      case "payment_failed":
      case "payment-failed":
        return "payment_failed";
      case "chargebackpending":
      case "chargeback_pending":
      case "chargeback-pending":
        return "chargeback_pending";
      case "readyforpickup":
      case "ready_for_pickup":
      case "ready-for-pickup":
        return "ready_for_pickup";
      case "partialrefund":
      case "partial_refund":
      case "partial-refund":
        return "partial_refund";
      case "processing":
        return "preparing";
      case "shipped":
        return "out_for_delivery";
      case "completed":
        return "delivered";
      default:
        return raw.replace(/\s+/g, "_");
    }
  };

  const getOrderAmount = (order) => {
    const candidates = [
      order?.finalAmount,
      order?.finalPrice,
      order?.totalPrice,
      order?.totalAmount,
      order?.amount,
      order?.total,
    ];
    for (const value of candidates) {
      if (value === null || value === undefined) continue;
      const num = Number(value);
      if (!Number.isNaN(num) && num > 0) return num;
    }
    const fallback =
      order?.finalAmount ??
      order?.finalPrice ??
      order?.totalPrice ??
      order?.totalAmount ??
      order?.amount ??
      order?.total ??
      0;
    return Number(fallback) || 0;
  };

  // Filtrelenmiş siparişler
  const filteredOrders = orders.filter((order) => {
    const normalizedStatus = normalizeStatus(order.status);
    // Durum filtresi
    if (statusFilter !== "all") {
      if (statusFilter === "pending") {
        if (!["pending", "new"].includes(normalizedStatus)) return false;
      } else if (normalizedStatus !== statusFilter) {
        return false;
      }
    }
    // Ödeme durumu filtresi
    if (paymentFilter !== "all") {
      const paymentStr = (order.paymentStatus || "").toString().toLowerCase();
      const isPaid = paymentStr === "paid" || order.isPaid === true;
      if (paymentFilter === "paid" && !isPaid) return false;
      if (paymentFilter === "pending" && isPaid) return false;
    }
    return true;
  });

  const totalFiltered = filteredOrders.length;
  const totalPages = Math.max(1, Math.ceil(totalFiltered / pageSize));
  const pagedOrders = filteredOrders.slice(
    (page - 1) * pageSize,
    page * pageSize,
  );

  useEffect(() => {
    setPage(1);
  }, [statusFilter, paymentFilter, pageSize]);

  // ============================================================
  // SİPARİŞ İŞLEMLERİ
  // ============================================================

  const updateOrderStatus = async (orderId, newStatus) => {
    try {
      console.log(
        `📝 Sipariş durumu güncelleniyor: #${orderId} → ${newStatus}`,
      );

      // StoreAttendant kendi endpoint'ini kullanır
      if (isStoreAttendant) {
        const result = await storeAttendantService.updateOrderStatus(
          orderId,
          newStatus,
        );
        if (!result.success) {
          throw new Error(result.error || "Durum güncellenemedi");
        }
        await loadData(false);
      } else {
        // Admin endpoint
        await AdminService.updateOrderStatus(orderId, newStatus);
        // Listeyi yeniden çek
        const updated = await AdminService.getOrders();
        setOrders(Array.isArray(updated) ? updated : []);
      }

      console.log(`✅ Sipariş durumu güncellendi: #${orderId} → ${newStatus}`);

      // Seçili sipariş varsa onu da güncelle
      if (selectedOrder && selectedOrder.id === orderId) {
        setSelectedOrder((prev) =>
          prev ? { ...prev, status: newStatus } : null,
        );
      }

      // Başarı bildirimi (opsiyonel - toast eklenebilir)
      console.log(
        `🔔 Bildirimler gönderildi: müşteri, kurye, mağaza görevlisi`,
      );
    } catch (error) {
      console.error("❌ Durum güncelleme hatası:", error);
      alert(
        `Sipariş durumu güncellenemedi: ${error.message || "Bilinmeyen hata"}`,
      );
    }
  };

  // ============================================================
  // SİPARİŞ SİLME (ADMIN)
  // ============================================================
  const deleteOrder = async (orderId) => {
    try {
      if (
        !window.confirm(
          "Bu siparişi kalıcı olarak silmek istiyor musunuz?\nBu işlem geri alınamaz!",
        )
      ) {
        return;
      }

      await AdminService.deleteOrder(orderId);

      // Silme sonrası listeyi tazele
      const updated = await AdminService.getOrders();
      setOrders(Array.isArray(updated) ? updated : []);

      if (selectedOrder?.id === orderId) {
        setSelectedOrder(null);
      }
    } catch (error) {
      console.error("Sipariş silme hatası:", error);
      const msg =
        error?.response?.data?.message ||
        error?.response?.data?.detail ||
        error?.message ||
        "Bilinmeyen hata";
      alert(`Sipariş silinemedi: ${msg}`);
    }
  };

  // ============================================================
  // KURYE ATAMA - Backend'e POST isteği gönderir
  // ============================================================
  const assignCourier = async (orderId, courierId) => {
    setAssigningCourier(true);
    try {
      console.log(
        `[AdminOrders] Kurye atama: Siparis #${orderId}, Kurye ID: ${courierId}`,
      );

      // Backend'e kurye atama isteği gönder
      let updatedOrder;
      if (isStoreAttendant) {
        updatedOrder = await storeAttendantService.assignCourier(
          orderId,
          courierId,
        );
      } else {
        updatedOrder = await AdminService.assignCourier(orderId, courierId);
      }

      console.log("[AdminOrders] Kurye atama API yaniti:", updatedOrder);

      if (isStoreAttendant && updatedOrder?.success === false) {
        throw new Error(updatedOrder.error || "Kurye atama başarısız");
      }

      // Başarılı olursa listeyi güncelle
      if (updatedOrder) {
        // Tüm listeyi yeniden çek (en güncel veri için)
        const updated = await AdminService.getOrders();
        const orderList = Array.isArray(updated) ? updated : [];
        setOrders(orderList);

        // Seçili siparişi de güncelle (kurye bilgisi hemen görünsün)
        const refreshedOrder = orderList.find((o) => o.id === orderId);
        if (refreshedOrder) {
          setSelectedOrder(refreshedOrder);
        }

        alert("Kurye başarıyla atandı!");
      }
    } catch (error) {
      console.error("Kurye atama hatasi:", error);
      alert(`Kurye atama basarisiz: ${error.message || "Bilinmeyen hata"}`);
    } finally {
      setAssigningCourier(false);
    }
  };

  // =========================================================================
  // DURUM RENKLERİ - Sipariş akış durumlarına göre renkler
  // Akış: New → Confirmed → Preparing → Ready → Assigned → PickedUp → OutForDelivery → Delivered
  // =========================================================================
  const getStatusColor = (status) => {
    const colorMap = {
      // Ana Akış Durumları
      new: "secondary", // 🔘 Gri - Yeni sipariş
      pending: "warning", // 🟡 Sarı - Beklemede (eski için uyumluluk)
      confirmed: "info", // 🔵 Mavi - Onaylanıyor
      preparing: "orange", // 🟠 Turuncu - Hazırlanıyor
      ready: "success", // 🟢 Yeşil - Hazır
      assigned: "primary", // 🔵 Koyu Mavi - Kuryeye Atandı
      picked_up: "teal", // 🩵 Turkuaz - Teslim Alındı
      pickedup: "teal",
      out_for_delivery: "purple", // 🟣 Mor - Yolda
      outfordelivery: "purple",
      in_transit: "purple", // 🟣 Mor - Yolda (alternatif)
      delivered: "dark", // ⬛ Koyu - Teslim Edildi
      cancelled: "danger", // 🔴 Kırmızı - İptal
      refunded: "secondary", // ⬜ Gri - İade Edildi
      partialrefund: "info", // 🔵 Mavi - Kısmi İade

      // Özel Durumlar
      delivery_failed: "danger",
      delivery_payment_pending: "warning",
      weight_pending: "info",
      payment_captured: "success",
    };
    const normalized = (status || "").toLowerCase();
    return colorMap[normalized] || "secondary";
  };

  // Durum renk hex kodları (timeline için)
  const getStatusHexColor = (status) => {
    const hexMap = {
      new: "#6c757d",
      pending: "#ffc107",
      confirmed: "#17a2b8",
      preparing: "#fd7e14",
      ready: "#28a745",
      assigned: "#0d6efd",
      picked_up: "#20c997",
      pickedup: "#20c997",
      out_for_delivery: "#6f42c1",
      outfordelivery: "#6f42c1",
      in_transit: "#6f42c1",
      delivered: "#343a40",
      cancelled: "#dc3545",
      refunded: "#6c757d",
      partialrefund: "#17a2b8",
      delivery_failed: "#dc3545",
      deliveryfailed: "#dc3545",
      delivery_payment_pending: "#ffc107",
      deliverypaymentpending: "#ffc107",
    };
    const normalized = (status || "").toLowerCase();
    return hexMap[normalized] || "#6c757d";
  };

  // =========================================================================
  // DURUM METİNLERİ - Türkçe durum açıklamaları
  // =========================================================================
  const getStatusText = (status) => {
    const statusMap = {
      // Ana Akış
      new: "Yeni Sipariş",
      pending: "Beklemede",
      confirmed: "Onaylandı",
      preparing: "Hazırlanıyor",
      ready: "Hazır - Kurye Bekliyor",
      assigned: "Kuryeye Atandı",
      picked_up: "Kurye Teslim Aldı",
      pickedup: "Kurye Teslim Aldı",
      out_for_delivery: "Yolda - Teslimat",
      outfordelivery: "Yolda - Teslimat",
      in_transit: "Yolda",
      delivered: "Teslim Edildi ✓",
      cancelled: "İptal Edildi",
      refunded: "İade Edildi",
      partialrefund: "Kısmi İade",

      // Özel Durumlar
      delivery_failed: "Teslimat Başarısız",
      deliveryfailed: "Teslimat Başarısız",
      delivery_payment_pending: "Ödeme Bekliyor",
      deliverypaymentpending: "Ödeme Bekliyor",
      weight_pending: "Tartı Onayı Bekliyor",
      payment_captured: "Ödeme Tamamlandı",
    };
    // Status'u küçük harfe çevir ve eşle
    const normalized = (status || "").toLowerCase();
    return statusMap[normalized] || status;
  };

  // =========================================================================
  // DURUM İKONLARI - Timeline ve badge'ler için
  // =========================================================================
  const getStatusIcon = (status) => {
    const iconMap = {
      new: "fa-circle",
      pending: "fa-hourglass-half",
      confirmed: "fa-check-circle",
      preparing: "fa-utensils",
      ready: "fa-box",
      assigned: "fa-user-check",
      picked_up: "fa-hand-holding-box",
      pickedup: "fa-hand-holding-box",
      out_for_delivery: "fa-motorcycle",
      outfordelivery: "fa-motorcycle",
      in_transit: "fa-truck",
      delivered: "fa-check-double",
      cancelled: "fa-times-circle",
      refunded: "fa-undo",
      partialrefund: "fa-undo",
      delivery_failed: "fa-exclamation-triangle",
      deliveryfailed: "fa-exclamation-triangle",
      delivery_payment_pending: "fa-credit-card",
      deliverypaymentpending: "fa-credit-card",
    };
    const normalized = (status || "").toLowerCase();
    return iconMap[normalized] || "fa-circle";
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
              className={`fas ${isStoreAttendant ? "fa-box" : "fa-shopping-bag"} me-2`}
              style={{ color: isStoreAttendant ? "#3b82f6" : "#f97316" }}
            ></i>
            {isStoreAttendant ? "Sipariş Hazırlık Paneli" : "Sipariş Yönetimi"}
            {/* SignalR Bağlantı Durumu */}
            <span
              className={`ms-2 badge ${signalRConnected ? "bg-success" : "bg-secondary"}`}
              style={{ fontSize: "0.55rem", verticalAlign: "middle" }}
              title={
                signalRConnected
                  ? "Real-time bağlantı aktif"
                  : "Polling modu aktif"
              }
            >
              <i
                className={`fas fa-${signalRConnected ? "bolt" : "clock"} me-1`}
              ></i>
              {signalRConnected ? "CANLI" : "POLLING"}
            </span>
          </h5>
          <p
            className="text-muted mb-0 d-none d-sm-block"
            style={{ fontSize: "0.75rem" }}
          >
            {isStoreAttendant
              ? "Siparişleri hazırla ve kuryeye teslim et"
              : "Siparişleri takip edin"}
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

          {/* İade Talepleri Toggle Butonu */}
          <button
            onClick={() => setShowRefundPanel(!showRefundPanel)}
            className={`btn btn-sm px-2 py-1 ${showRefundPanel ? "btn-warning" : "btn-outline-warning"}`}
            style={{ fontSize: "0.75rem" }}
          >
            <i className="fas fa-undo me-1"></i>
            İade Talepleri
            {refundRequests.filter(
              (r) => r.status === 0 || r.statusText === "Beklemede",
            ).length > 0 && (
              <span
                className="badge bg-danger ms-1"
                style={{ fontSize: "0.6rem" }}
              >
                {
                  refundRequests.filter(
                    (r) => r.status === 0 || r.statusText === "Beklemede",
                  ).length
                }
              </span>
            )}
          </button>
        </div>
      </div>

      {/* ═══════════════════════════════════════════════════════════════════════
          İADE TALEBİ YÖNETİM PANELİ
          Admin / Müşteri hizmetleri iade taleplerini buradan yönetir
          ═══════════════════════════════════════════════════════════════════════ */}
      {showRefundPanel && (
        <div className="card border-warning mb-3">
          <div className="card-header bg-warning text-dark d-flex justify-content-between align-items-center">
            <h6 className="mb-0">
              <i className="fas fa-undo me-2"></i>
              İade Talepleri Yönetimi
            </h6>
            <div className="d-flex gap-2">
              <button
                className="btn btn-sm btn-outline-dark"
                onClick={loadRefundRequests}
                disabled={refundLoading}
              >
                <i
                  className={`fas fa-sync-alt ${refundLoading ? "fa-spin" : ""}`}
                ></i>
              </button>
              <button
                className="btn btn-sm btn-outline-dark"
                onClick={() => setShowRefundPanel(false)}
              >
                <i className="fas fa-times"></i>
              </button>
            </div>
          </div>
          <div className="card-body p-0">
            {refundLoading ? (
              <div className="text-center py-4">
                <div className="spinner-border text-warning"></div>
                <p className="mt-2 text-muted">Yükleniyor...</p>
              </div>
            ) : refundRequests.length === 0 ? (
              <div className="text-center py-4 text-muted">
                <i className="fas fa-inbox fa-2x mb-2 d-block"></i>
                İade talebi bulunmuyor
              </div>
            ) : (
              <div className="table-responsive">
                <table
                  className="table table-hover table-sm mb-0"
                  style={{ fontSize: "0.8rem" }}
                >
                  <thead className="table-light">
                    <tr>
                      <th>#</th>
                      <th>Sipariş</th>
                      <th>Müşteri</th>
                      <th>Tutar</th>
                      <th>Sebep</th>
                      <th>Durum</th>
                      <th>Tarih</th>
                      <th>İşlem</th>
                    </tr>
                  </thead>
                  <tbody>
                    {refundRequests.map((req) => (
                      <tr
                        key={req.id}
                        className={
                          req.status === 0 || req.statusText === "Beklemede"
                            ? "table-warning"
                            : req.status === 5 ||
                                req.statusText === "İade Başarısız"
                              ? "table-danger"
                              : ""
                        }
                      >
                        <td>{req.id}</td>
                        <td>
                          <strong>
                            {req.orderNumber || `#${req.orderId}`}
                          </strong>
                          <br />
                          <small className="text-muted">
                            {req.orderStatusAtRequest}
                          </small>
                        </td>
                        <td>
                          {req.customerName || "-"}
                          {req.customerPhone && (
                            <>
                              <br />
                              <small>{req.customerPhone}</small>
                            </>
                          )}
                        </td>
                        <td className="fw-bold text-danger">
                          {req.refundAmount?.toFixed(2)} TL
                          <br />
                          <small className="text-muted">
                            Sipariş: {req.orderTotalPrice?.toFixed(2)} TL
                          </small>
                        </td>
                        <td style={{ maxWidth: "200px" }}>
                          <small>{req.reason}</small>
                        </td>
                        <td>
                          <span
                            className={`badge ${
                              req.statusText === "Beklemede"
                                ? "bg-warning text-dark"
                                : req.statusText === "Onaylandı"
                                  ? "bg-info"
                                  : req.statusText === "Reddedildi"
                                    ? "bg-secondary"
                                    : req.statusText === "İade Edildi"
                                      ? "bg-success"
                                      : req.statusText ===
                                          "Otomatik İptal Edildi"
                                        ? "bg-primary"
                                        : req.statusText === "İade Başarısız"
                                          ? "bg-danger"
                                          : "bg-secondary"
                            }`}
                          >
                            {req.statusText}
                          </span>
                        </td>
                        <td>
                          <small>
                            {new Date(req.requestedAt).toLocaleDateString(
                              "tr-TR",
                            )}
                          </small>
                          <br />
                          <small className="text-muted">
                            {new Date(req.requestedAt).toLocaleTimeString(
                              "tr-TR",
                              { hour: "2-digit", minute: "2-digit" },
                            )}
                          </small>
                        </td>
                        <td>
                          {(req.status === 0 ||
                            req.statusText === "Beklemede") && (
                            <div className="d-flex flex-column gap-1">
                              <input
                                type="text"
                                className="form-control form-control-sm"
                                placeholder="Admin notu..."
                                style={{
                                  fontSize: "0.7rem",
                                  minWidth: "120px",
                                }}
                                value={
                                  refundProcessing === req.id
                                    ? refundAdminNote
                                    : ""
                                }
                                onChange={(e) => {
                                  setRefundProcessing(req.id);
                                  setRefundAdminNote(e.target.value);
                                }}
                              />
                              <div className="btn-group btn-group-sm">
                                <button
                                  className="btn btn-success btn-sm"
                                  onClick={() =>
                                    handleProcessRefund(req.id, true)
                                  }
                                  disabled={
                                    refundProcessing === req.id && refundLoading
                                  }
                                  title="İade Onayla"
                                >
                                  <i className="fas fa-check"></i> Onayla
                                </button>
                                <button
                                  className="btn btn-danger btn-sm"
                                  onClick={() =>
                                    handleProcessRefund(req.id, false)
                                  }
                                  disabled={
                                    refundProcessing === req.id && refundLoading
                                  }
                                  title="İade Reddet"
                                >
                                  <i className="fas fa-times"></i> Reddet
                                </button>
                              </div>
                            </div>
                          )}
                          {(req.status === 5 ||
                            req.statusText === "İade Başarısız") && (
                            <button
                              className="btn btn-outline-warning btn-sm"
                              onClick={() => handleRetryRefund(req.id)}
                              disabled={refundProcessing === req.id}
                            >
                              <i className="fas fa-redo me-1"></i>
                              Tekrar Dene
                            </button>
                          )}
                          {req.processedAt && (
                            <small className="text-muted d-block mt-1">
                              {req.processedByName || "Admin"} -{" "}
                              {new Date(req.processedAt).toLocaleDateString(
                                "tr-TR",
                              )}
                            </small>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Yeni Sipariş Bildirimi - Onay bekleyen sipariş varsa göster */}
      {orders.filter((o) =>
        ["pending", "new"].includes(normalizeStatus(o.status)),
      ).length > 0 && (
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
              {
                orders.filter((o) =>
                  ["pending", "new"].includes(normalizeStatus(o.status)),
                ).length
              }
            </strong>{" "}
            adet onay bekleyen sipariş var!
          </span>
        </div>
      )}

      {/* ================================================================
          ÖZET KARTLAR - Sipariş Akış Durumları
          New → Confirmed → Preparing → Ready → Assigned → PickedUp → Delivered
          ================================================================ */}
      <div className="row g-2 mb-3 px-1">
        {/* Yeni/Onay Bekleyen */}
        <div className="col-4 col-md-2">
          <div
            className="card border-0 shadow-sm text-white"
            style={{ borderRadius: "6px", backgroundColor: "#6c757d", cursor: "pointer", opacity: statusFilter === "pending" ? 1 : 0.85, border: statusFilter === "pending" ? "2px solid #fff" : "2px solid transparent" }}
            onClick={() => setStatusFilter(statusFilter === "pending" ? "all" : "pending")}
          >
            <div className="card-body text-center px-1 py-2">
              <i
                className="fas fa-circle mb-1"
                style={{ fontSize: "0.7rem" }}
              ></i>
              <h6 className="fw-bold mb-0">
                {
                  orders.filter((o) =>
                    ["pending", "new"].includes(normalizeStatus(o.status)),
                  ).length
                }
              </h6>
              <small style={{ fontSize: "0.55rem" }}>Yeni</small>
            </div>
          </div>
        </div>

        {/* Onaylandı */}
        <div className="col-4 col-md-2">
          <div
            className="card border-0 shadow-sm text-white"
            style={{ borderRadius: "6px", backgroundColor: "#17a2b8", cursor: "pointer", opacity: statusFilter === "confirmed" ? 1 : 0.85, border: statusFilter === "confirmed" ? "2px solid #fff" : "2px solid transparent" }}
            onClick={() => setStatusFilter(statusFilter === "confirmed" ? "all" : "confirmed")}
          >
            <div className="card-body text-center px-1 py-2">
              <i
                className="fas fa-check-circle mb-1"
                style={{ fontSize: "0.7rem" }}
              ></i>
              <h6 className="fw-bold mb-0">
                {
                  orders.filter(
                    (o) => normalizeStatus(o.status) === "confirmed",
                  ).length
                }
              </h6>
              <small style={{ fontSize: "0.55rem" }}>Onaylı</small>
            </div>
          </div>
        </div>

        {/* Hazırlanıyor */}
        <div className="col-4 col-md-2">
          <div
            className="card border-0 shadow-sm text-white"
            style={{ borderRadius: "6px", backgroundColor: "#fd7e14", cursor: "pointer", opacity: statusFilter === "preparing" ? 1 : 0.85, border: statusFilter === "preparing" ? "2px solid #fff" : "2px solid transparent" }}
            onClick={() => setStatusFilter(statusFilter === "preparing" ? "all" : "preparing")}
          >
            <div className="card-body text-center px-1 py-2">
              <i
                className="fas fa-utensils mb-1"
                style={{ fontSize: "0.7rem" }}
              ></i>
              <h6 className="fw-bold mb-0">
                {
                  orders.filter(
                    (o) => normalizeStatus(o.status) === "preparing",
                  ).length
                }
              </h6>
              <small style={{ fontSize: "0.55rem" }}>Hazırlanan</small>
            </div>
          </div>
        </div>

        {/* Hazır - Kurye Bekliyor */}
        <div className="col-4 col-md-2">
          <div
            className="card border-0 shadow-sm text-white"
            style={{ borderRadius: "6px", backgroundColor: "#28a745", cursor: "pointer", opacity: statusFilter === "ready" ? 1 : 0.85, border: statusFilter === "ready" ? "2px solid #fff" : "2px solid transparent" }}
            onClick={() => setStatusFilter(statusFilter === "ready" ? "all" : "ready")}
          >
            <div className="card-body text-center px-1 py-2">
              <i className="fas fa-box mb-1" style={{ fontSize: "0.7rem" }}></i>
              <h6 className="fw-bold mb-0">
                {
                  orders.filter((o) => normalizeStatus(o.status) === "ready")
                    .length
                }
              </h6>
              <small style={{ fontSize: "0.55rem" }}>Hazır</small>
            </div>
          </div>
        </div>

        {/* Kuryede (Assigned + PickedUp + OutForDelivery) */}
        <div className="col-4 col-md-2">
          <div
            className="card border-0 shadow-sm text-white"
            style={{ borderRadius: "6px", backgroundColor: "#6f42c1", cursor: "pointer", opacity: ["assigned", "out_for_delivery"].includes(statusFilter) ? 1 : 0.85, border: ["assigned", "out_for_delivery"].includes(statusFilter) ? "2px solid #fff" : "2px solid transparent" }}
            onClick={() => setStatusFilter(statusFilter === "assigned" ? "all" : "assigned")}
          >
            <div className="card-body text-center px-1 py-2">
              <i
                className="fas fa-motorcycle mb-1"
                style={{ fontSize: "0.7rem" }}
              ></i>
              <h6 className="fw-bold mb-0">
                {
                  orders.filter((o) =>
                    [
                      "assigned",
                      "picked_up",
                      "out_for_delivery",
                      "in_transit",
                    ].includes(normalizeStatus(o.status)),
                  ).length
                }
              </h6>
              <small style={{ fontSize: "0.55rem" }}>Kuryede</small>
            </div>
          </div>
        </div>

        {/* Teslim Edildi */}
        <div className="col-4 col-md-2">
          <div
            className="card border-0 shadow-sm text-white"
            style={{ borderRadius: "6px", backgroundColor: "#343a40", cursor: "pointer", opacity: statusFilter === "delivered" ? 1 : 0.85, border: statusFilter === "delivered" ? "2px solid #fff" : "2px solid transparent" }}
            onClick={() => setStatusFilter(statusFilter === "delivered" ? "all" : "delivered")}
          >
            <div className="card-body text-center px-1 py-2">
              <i
                className="fas fa-check-double mb-1"
                style={{ fontSize: "0.7rem" }}
              ></i>
              <h6 className="fw-bold mb-0">
                {
                  orders.filter(
                    (o) => normalizeStatus(o.status) === "delivered",
                  ).length
                }
              </h6>
              <small style={{ fontSize: "0.55rem" }}>Teslim</small>
            </div>
          </div>
        </div>
      </div>

      {/* Sorunlu Siparişler Satırı */}
      {(orders.filter((o) => normalizeStatus(o.status) === "delivery_failed")
        .length > 0 ||
        orders.filter(
          (o) => normalizeStatus(o.status) === "delivery_payment_pending",
        ).length > 0) && (
        <div className="row g-2 mb-3 px-1">
          {/* Teslimat Başarısız */}
          <div className="col-6 col-md-3">
            <div
              className="card border-0 shadow-sm bg-danger text-white"
              style={{ borderRadius: "6px", cursor: "pointer", opacity: statusFilter === "delivery_failed" ? 1 : 0.85, border: statusFilter === "delivery_failed" ? "2px solid #fff" : "2px solid transparent" }}
              onClick={() => setStatusFilter(statusFilter === "delivery_failed" ? "all" : "delivery_failed")}
            >
              <div className="card-body text-center px-1 py-2">
                <i
                  className="fas fa-exclamation-triangle mb-1"
                  style={{ fontSize: "0.7rem" }}
                ></i>
                <h6 className="fw-bold mb-0">
                  {
                    orders.filter(
                      (o) => normalizeStatus(o.status) === "delivery_failed",
                    ).length
                  }
                </h6>
                <small style={{ fontSize: "0.55rem" }}>Başarısız</small>
              </div>
            </div>
          </div>

          {/* Ödeme Bekliyor */}
          <div className="col-6 col-md-3">
            <div
              className="card border-0 shadow-sm text-dark"
              style={{ borderRadius: "6px", backgroundColor: "#ffc107", cursor: "pointer", opacity: statusFilter === "delivery_payment_pending" ? 1 : 0.85, border: statusFilter === "delivery_payment_pending" ? "2px solid #fff" : "2px solid transparent" }}
              onClick={() => setStatusFilter(statusFilter === "delivery_payment_pending" ? "all" : "delivery_payment_pending")}
            >
              <div className="card-body text-center px-1 py-2">
                <i
                  className="fas fa-credit-card mb-1"
                  style={{ fontSize: "0.7rem" }}
                ></i>
                <h6 className="fw-bold mb-0">
                  {
                    orders.filter(
                      (o) =>
                        normalizeStatus(o.status) ===
                        "delivery_payment_pending",
                    ).length
                  }
                </h6>
                <small style={{ fontSize: "0.55rem" }}>Ödeme Bekl.</small>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ================================================================
          FİLTRE BUTONLARI - Sipariş Akış Durumları
          Yeni akış: New → Confirmed → Preparing → Ready → Assigned → PickedUp → OutForDelivery → Delivered
          ================================================================ */}
      <div className="d-flex flex-wrap gap-2 mb-3 px-1">
        {/* Ana Durum Filtresi */}
        <div className="btn-group btn-group-sm flex-wrap" role="group">
          <button
            className={`btn ${statusFilter === "all" ? "btn-dark" : "btn-outline-dark"}`}
            onClick={() => setStatusFilter("all")}
            style={{ fontSize: "0.65rem" }}
          >
            <i className="fas fa-list me-1"></i>Tümü
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.length}</span>
          </button>
          <button
            className={`btn ${statusFilter === "pending" || statusFilter === "new" ? "btn-secondary" : "btn-outline-secondary"}`}
            onClick={() => setStatusFilter("pending")}
            style={{ fontSize: "0.65rem" }}
          >
            <i className="fas fa-circle me-1"></i>Yeni
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => ["pending", "new"].includes(normalizeStatus(o.status))).length}</span>
          </button>
          <button
            className={`btn ${statusFilter === "confirmed" ? "btn-info" : "btn-outline-info"}`}
            onClick={() => setStatusFilter("confirmed")}
            style={{ fontSize: "0.65rem" }}
          >
            <i className="fas fa-check-circle me-1"></i>Onaylı
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => normalizeStatus(o.status) === "confirmed").length}</span>
          </button>
          <button
            className={`btn ${statusFilter === "preparing" ? "btn-warning" : "btn-outline-warning"}`}
            onClick={() => setStatusFilter("preparing")}
            style={{
              fontSize: "0.65rem",
              backgroundColor: statusFilter === "preparing" ? "#fd7e14" : "",
              borderColor: "#fd7e14",
              color: statusFilter === "preparing" ? "white" : "#fd7e14",
            }}
          >
            <i className="fas fa-utensils me-1"></i>Hazırlanan
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => normalizeStatus(o.status) === "preparing").length}</span>
          </button>
          <button
            className={`btn ${statusFilter === "ready" ? "btn-success" : "btn-outline-success"}`}
            onClick={() => setStatusFilter("ready")}
            style={{ fontSize: "0.65rem" }}
          >
            <i className="fas fa-box me-1"></i>Hazır
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => normalizeStatus(o.status) === "ready").length}</span>
          </button>
          <button
            className={`btn ${statusFilter === "assigned" ? "btn-primary" : "btn-outline-primary"}`}
            onClick={() => setStatusFilter("assigned")}
            style={{ fontSize: "0.65rem" }}
          >
            <i className="fas fa-user-check me-1"></i>Atandı
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => normalizeStatus(o.status) === "assigned").length}</span>
          </button>
          <button
            className={`btn ${statusFilter === "out_for_delivery" ? "btn-primary" : "btn-outline-primary"}`}
            onClick={() => setStatusFilter("out_for_delivery")}
            style={{
              fontSize: "0.65rem",
              backgroundColor:
                statusFilter === "out_for_delivery" ? "#6f42c1" : "",
              borderColor: "#6f42c1",
              color: statusFilter === "out_for_delivery" ? "white" : "#6f42c1",
            }}
          >
            <i className="fas fa-motorcycle me-1"></i>Yolda
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => normalizeStatus(o.status) === "out_for_delivery").length}</span>
          </button>
          <button
            className={`btn ${statusFilter === "delivered" ? "btn-dark" : "btn-outline-dark"}`}
            onClick={() => setStatusFilter("delivered")}
            style={{ fontSize: "0.65rem" }}
          >
            <i className="fas fa-check-double me-1"></i>Teslim
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => normalizeStatus(o.status) === "delivered").length}</span>
          </button>
        </div>

        {/* Sorun Durumları Filtresi */}
        <div className="btn-group btn-group-sm" role="group">
          <button
            className={`btn ${statusFilter === "delivery_failed" ? "btn-danger" : "btn-outline-danger"}`}
            onClick={() => setStatusFilter("delivery_failed")}
            style={{ fontSize: "0.65rem" }}
          >
            <i className="fas fa-exclamation-triangle me-1"></i>Başarısız
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => normalizeStatus(o.status) === "delivery_failed").length}</span>
          </button>
          <button
            className={`btn ${statusFilter === "delivery_payment_pending" ? "btn-warning" : "btn-outline-warning"}`}
            onClick={() => setStatusFilter("delivery_payment_pending")}
            style={{ fontSize: "0.65rem" }}
          >
            <i className="fas fa-credit-card me-1"></i>Ödeme Bekl.
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => normalizeStatus(o.status) === "delivery_payment_pending").length}</span>
          </button>
        </div>

        {/* Ödeme Durumu Filtresi */}
        <div className="btn-group btn-group-sm" role="group">
          <button
            className={`btn ${paymentFilter === "all" ? "btn-dark" : "btn-outline-dark"}`}
            onClick={() => setPaymentFilter("all")}
            style={{ fontSize: "0.65rem" }}
          >
            <i className="fas fa-wallet me-1"></i>Tüm Ödemeler
          </button>
          <button
            className={`btn ${paymentFilter === "pending" ? "btn-danger" : "btn-outline-danger"}`}
            onClick={() => setPaymentFilter("pending")}
            style={{ fontSize: "0.65rem" }}
          >
            <i className="fas fa-clock me-1"></i>Ödeme Bekleyen
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => { const ps = (o.paymentStatus || "").toString().toLowerCase(); return ps !== "paid" && o.isPaid !== true; }).length}</span>
          </button>
          <button
            className={`btn ${paymentFilter === "paid" ? "btn-success" : "btn-outline-success"}`}
            onClick={() => setPaymentFilter("paid")}
            style={{ fontSize: "0.7rem" }}
          >
            <i className="fas fa-check me-1"></i>Ödendi
            <span className="badge bg-light text-dark ms-1" style={{ fontSize: "0.55rem" }}>{orders.filter((o) => { const ps = (o.paymentStatus || "").toString().toLowerCase(); return ps === "paid" || o.isPaid === true; }).length}</span>
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
                  pagedOrders.map((order) => {
                    const normalizedStatus = normalizeStatus(order.status);
                    const amount = getOrderAmount(order);
                    return (
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
                            {amount.toFixed(0)}₺
                          </span>
                        </td>
                        <td className="px-1 py-2">
                          <span
                            className={`badge`}
                            style={{
                              fontSize: "0.55rem",
                              padding: "0.25em 0.5em",
                              backgroundColor: getStatusHexColor(order.status),
                              color: "white",
                              whiteSpace: "nowrap",
                            }}
                          >
                            <i
                              className={`fas ${getStatusIcon(order.status)} me-1`}
                            ></i>
                            {getStatusText(order.status)}
                          </span>
                        </td>
                        {/* Ödeme Durumu Sütunu */}
                        <td className="px-1 py-2 d-none d-sm-table-cell">
                          {(order.paymentStatus || "")
                            .toString()
                            .toLowerCase() === "paid" ||
                          order.isPaid === true ? (
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
                          <div className="d-flex gap-1 flex-wrap">
                            {/* Detay Butonu */}
                            <button
                              onClick={() => setSelectedOrder(order)}
                              className="btn btn-outline-secondary p-1"
                              style={{ fontSize: "0.6rem", lineHeight: 1 }}
                              title="Sipariş Detayı"
                            >
                              <i className="fas fa-eye"></i>
                            </button>

                            {/* ================================================================
                              MVP AKIŞ BUTONLARI
                              New/Pending → Confirmed → Preparing → Ready → Assigned → Delivered
                              ================================================================ */}

                            {/* ✅ ONAYLA - Yeni/Bekleyen sipariş için */}
                            {(normalizedStatus === "new" ||
                              normalizedStatus === "pending" ||
                              normalizedStatus === "paid") && (
                              <button
                                onClick={() =>
                                  updateOrderStatus(order.id, "confirmed")
                                }
                                className="btn btn-info p-1"
                                style={{
                                  fontSize: "0.6rem",
                                  lineHeight: 1,
                                }}
                                title="✅ Onayla"
                              >
                                <i className="fas fa-check"></i>
                              </button>
                            )}

                            {/* 🍳 HAZIRLANIYOR - Onaylı sipariş için */}
                            {(normalizedStatus === "confirmed" ||
                              normalizedStatus === "paid") && (
                              <button
                                onClick={() =>
                                  updateOrderStatus(order.id, "preparing")
                                }
                                className="btn p-1"
                                style={{
                                  fontSize: "0.6rem",
                                  lineHeight: 1,
                                  backgroundColor: "#fd7e14",
                                  borderColor: "#fd7e14",
                                  color: "white",
                                }}
                                title="🍳 Hazırlanıyor Yap"
                              >
                                <i className="fas fa-fire"></i>
                              </button>
                            )}

                            {/* 📦 HAZIR - Hazırlanan sipariş için */}
                            {normalizedStatus === "preparing" && (
                              <button
                                onClick={() =>
                                  updateOrderStatus(order.id, "ready")
                                }
                                className="btn btn-success p-1"
                                style={{ fontSize: "0.6rem", lineHeight: 1 }}
                                title="📦 Hazır Yap"
                              >
                                <i className="fas fa-box-open"></i>
                              </button>
                            )}

                            {/* 🚴 KURYE ATA - Hazır sipariş için */}
                            {normalizedStatus === "ready" && (
                              <button
                                onClick={() => setSelectedOrder(order)}
                                className="btn btn-primary p-1"
                                style={{ fontSize: "0.6rem", lineHeight: 1 }}
                                title="🚴 Kuryeye Ata"
                              >
                                <i className="fas fa-motorcycle"></i>
                              </button>
                            )}

                            {/* 🛵 DAĞITIMA ÇIKTI - Kuryeye atanan sipariş için */}
                            {(normalizedStatus === "assigned" ||
                              normalizedStatus === "picked_up" ||
                              normalizedStatus === "pickedup") && (
                              <button
                                onClick={() =>
                                  updateOrderStatus(
                                    order.id,
                                    "out_for_delivery",
                                  )
                                }
                                className="btn p-1"
                                style={{
                                  fontSize: "0.6rem",
                                  lineHeight: 1,
                                  backgroundColor: "#6f42c1",
                                  borderColor: "#6f42c1",
                                  color: "white",
                                }}
                                title="🛵 Dağıtıma Çıktı"
                              >
                                <i className="fas fa-shipping-fast"></i>
                              </button>
                            )}

                            {/* ✅ TESLİM EDİLDİ - Dağıtımdaki sipariş için */}
                            {(normalizedStatus === "out_for_delivery" ||
                              normalizedStatus === "outfordelivery") && (
                              <button
                                onClick={() =>
                                  updateOrderStatus(order.id, "delivered")
                                }
                                className="btn btn-dark p-1"
                                style={{ fontSize: "0.6rem", lineHeight: 1 }}
                                title="✅ Teslim Edildi"
                              >
                                <i className="fas fa-check-double"></i>
                              </button>
                            )}

                            {/* 🚫 İPTAL + PARA İADESİ - Admin ve StoreAttendant için
                                İptal edilince POSNET üzerinden para iadesi de tetiklenir */}
                            {normalizedStatus !== "delivered" &&
                              normalizedStatus !== "cancelled" &&
                              normalizedStatus !== "refunded" && (
                                <button
                                  onClick={async () => {
                                    if (
                                      !window.confirm(
                                        "Siparişi iptal etmek istediğinize emin misiniz?\nÖdeme yapılmışsa otomatik para iadesi yapılacaktır.",
                                      )
                                    )
                                      return;
                                    try {
                                      await AdminService.cancelOrderWithRefund(
                                        order.id,
                                        "Admin/Görevli tarafından iptal edildi",
                                      );
                                      await loadData(false);
                                      loadRefundRequests();
                                    } catch (err) {
                                      alert(
                                        "İptal hatası: " +
                                          (err?.response?.data?.message ||
                                            err?.message ||
                                            "Bilinmeyen hata"),
                                      );
                                    }
                                  }}
                                  className="btn btn-outline-danger p-1"
                                  style={{ fontSize: "0.6rem", lineHeight: 1 }}
                                  title="🚫 İptal Et + Para İadesi"
                                >
                                  <i className="fas fa-times"></i>
                                </button>
                              )}

                            {/* 🗑️ SİL - Sadece Admin */}
                            {!isStoreAttendant && (
                              <button
                                onClick={() => deleteOrder(order.id)}
                                className="btn btn-outline-dark p-1"
                                style={{ fontSize: "0.6rem", lineHeight: 1 }}
                                title="🗑️ Siparişi Sil"
                              >
                                <i className="fas fa-trash"></i>
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
          {totalPages > 1 && (
            <div className="d-flex flex-wrap align-items-center justify-content-between px-3 py-2 border-top">
              <div className="text-muted" style={{ fontSize: "0.7rem" }}>
                Toplam {totalFiltered} sipariş • Sayfa {page}/{totalPages}
              </div>
              <div className="d-flex align-items-center gap-2">
                <select
                  className="form-select form-select-sm"
                  style={{ width: "90px" }}
                  value={pageSize}
                  onChange={(e) => setPageSize(Number(e.target.value))}
                >
                  {[10, 20, 30, 50].map((size) => (
                    <option key={size} value={size}>
                      {size} / sayfa
                    </option>
                  ))}
                </select>
                <div className="btn-group btn-group-sm">
                  <button
                    className="btn btn-outline-secondary"
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page <= 1}
                  >
                    ‹
                  </button>
                  <button
                    className="btn btn-outline-secondary"
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    disabled={page >= totalPages}
                  >
                    ›
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Sipariş Detay Modal */}
      {selectedOrder &&
        (() => {
          // Status'u normalize et (backend büyük harfle gönderebilir)
          const normalizedStatus = normalizeStatus(selectedOrder.status);

          return (
            <div
              className="modal fade show d-block"
              tabIndex="-1"
              style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
              onClick={(e) => {
                // Modal dışına tıklayınca kapat
                if (e.target === e.currentTarget) setSelectedOrder(null);
              }}
            >
              <div
                className="modal-dialog modal-dialog-centered"
                style={{ maxWidth: "500px", margin: "auto" }}
              >
                <div className="modal-content" style={{ borderRadius: "12px" }}>
                  <div className="modal-header py-2 px-3">
                    <h6 className="modal-title" style={{ fontSize: "0.9rem" }}>
                      <i className="fas fa-receipt me-2"></i>
                      Sipariş #{selectedOrder.id}
                    </h6>
                    {/* Kapat butonu - × simgesi ile */}
                    <button
                      onClick={() => setSelectedOrder(null)}
                      className="btn btn-outline-secondary btn-sm rounded-circle d-flex align-items-center justify-content-center"
                      style={{
                        width: "28px",
                        height: "28px",
                        fontSize: "18px",
                        fontWeight: "bold",
                        lineHeight: 1,
                        padding: 0,
                        border: "1px solid #dee2e6",
                      }}
                      title="Kapat"
                    >
                      ×
                    </button>
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
                        <h6
                          className="fw-bold mb-1"
                          style={{ fontSize: "0.8rem" }}
                        >
                          Müşteri
                        </h6>
                        <p className="mb-1">
                          <strong>Ad:</strong>{" "}
                          {selectedOrder.customerName || "-"}
                        </p>
                        <p className="mb-1">
                          <strong>Tel:</strong>{" "}
                          {selectedOrder.customerPhone || "-"}
                        </p>
                        <p className="mb-1">
                          <strong>Adres:</strong>{" "}
                          {selectedOrder.shippingAddress ||
                            selectedOrder.address ||
                            "-"}
                        </p>
                      </div>
                      <div className="col-12 col-md-6">
                        <h6
                          className="fw-bold mb-1"
                          style={{ fontSize: "0.8rem" }}
                        >
                          <i className="fas fa-receipt me-1 text-primary"></i>
                          Sipariş Bilgileri
                        </h6>
                        <p className="mb-1">
                          <strong>Tarih:</strong>{" "}
                          {selectedOrder.orderDate
                            ? new Date(
                                selectedOrder.orderDate,
                              ).toLocaleDateString("tr-TR")
                            : "-"}
                        </p>
                        <p className="mb-1">
                          <strong>Tutar:</strong>{" "}
                          <span className="text-success fw-bold">
                            {(
                              selectedOrder.finalPrice ??
                              selectedOrder.totalPrice ??
                              selectedOrder.totalAmount ??
                              0
                            ).toFixed(2)}{" "}
                            ₺
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
                                  : selectedOrder.paymentMethod ===
                                      "bank_transfer"
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
                                : selectedOrder.paymentMethod ===
                                    "bank_transfer"
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
                          {(Array.isArray(selectedOrder.orderItems)
                            ? selectedOrder.orderItems
                            : Array.isArray(selectedOrder.items)
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
                          {/* Tartı Farkı - Eğer varsa göster */}
                          {selectedOrder.weightDifference !== undefined &&
                            selectedOrder.weightDifference !== 0 && (
                              <tr className="bg-warning bg-opacity-25">
                                <td
                                  colSpan="3"
                                  className="px-1 text-end fw-bold"
                                >
                                  <i className="fas fa-balance-scale me-1"></i>
                                  Tartı Farkı:
                                </td>
                                <td className="px-1 text-end">
                                  <span
                                    className={`fw-bold ${selectedOrder.weightDifference > 0 ? "text-success" : "text-danger"}`}
                                    style={{ fontSize: "0.8rem" }}
                                  >
                                    {selectedOrder.weightDifference > 0
                                      ? "+"
                                      : ""}
                                    {(
                                      selectedOrder.weightDifference ?? 0
                                    ).toFixed(2)}{" "}
                                    ₺
                                  </span>
                                </td>
                              </tr>
                            )}
                          {/* Final Tutar - Tartı farkı varsa göster */}
                          {selectedOrder.finalAmount !== undefined &&
                            selectedOrder.finalAmount !==
                              selectedOrder.totalAmount && (
                              <tr className="bg-success bg-opacity-25">
                                <td
                                  colSpan="3"
                                  className="px-1 text-end fw-bold"
                                >
                                  <i className="fas fa-calculator me-1"></i>
                                  Final Tutar:
                                </td>
                                <td className="px-1 text-end">
                                  <span
                                    className="fw-bold text-success"
                                    style={{ fontSize: "0.9rem" }}
                                  >
                                    {(selectedOrder.finalAmount ?? 0).toFixed(
                                      2,
                                    )}{" "}
                                    ₺
                                  </span>
                                </td>
                              </tr>
                            )}
                        </tfoot>
                      </table>
                    </div>

                    {/* ================================================================
                    TARTI FARKI BİLGİSİ - Tartı onayı bekleyenler için
                    ================================================================ */}
                    {(normalizedStatus === "weight_pending" ||
                      normalizedStatus === "delivery_payment_pending") && (
                      <div
                        className="alert alert-info mt-2 py-2"
                        style={{ fontSize: "0.75rem" }}
                      >
                        <h6
                          className="fw-bold mb-1"
                          style={{ fontSize: "0.8rem" }}
                        >
                          <i className="fas fa-balance-scale me-1"></i>
                          Tartı Bilgisi
                        </h6>
                        <div className="row">
                          <div className="col-6">
                            <small className="text-muted">
                              Sipariş Ağırlığı:
                            </small>
                            <div className="fw-bold">
                              {(selectedOrder.estimatedWeight ?? 0).toFixed(2)}{" "}
                              kg
                            </div>
                          </div>
                          <div className="col-6">
                            <small className="text-muted">
                              Tartılan Ağırlık:
                            </small>
                            <div className="fw-bold">
                              {(selectedOrder.actualWeight ?? 0).toFixed(2)} kg
                            </div>
                          </div>
                        </div>
                        {selectedOrder.weightDifferenceReason && (
                          <div className="mt-1">
                            <small className="text-muted">Fark Sebebi:</small>
                            <div className="fw-semibold">
                              {selectedOrder.weightDifferenceReason}
                            </div>
                          </div>
                        )}
                      </div>
                    )}

                    {/* ================================================================
                    TESLİMAT BAŞARISIZ BİLGİSİ
                    ================================================================ */}
                    {normalizedStatus === "delivery_failed" && (
                      <div
                        className="alert alert-danger mt-2 py-2"
                        style={{ fontSize: "0.75rem" }}
                      >
                        <h6
                          className="fw-bold mb-1"
                          style={{ fontSize: "0.8rem" }}
                        >
                          <i className="fas fa-exclamation-triangle me-1"></i>
                          Teslimat Başarısız
                        </h6>
                        <div>
                          <small className="text-muted">
                            Başarısızlık Sebebi:
                          </small>
                          <div className="fw-semibold">
                            {selectedOrder.failureReason || "Belirtilmemiş"}
                          </div>
                        </div>
                        {selectedOrder.failedAt && (
                          <div className="mt-1">
                            <small className="text-muted">Tarih:</small>
                            <div>
                              {new Date(selectedOrder.failedAt).toLocaleString(
                                "tr-TR",
                              )}
                            </div>
                          </div>
                        )}
                      </div>
                    )}

                    {/* ================================================================
                    KURYE BİLGİSİ
                    ================================================================ */}
                    {selectedOrder.courierName && (
                      <div
                        className="alert alert-success mt-2 py-2"
                        style={{ fontSize: "0.75rem" }}
                      >
                        <h6
                          className="fw-bold mb-1"
                          style={{ fontSize: "0.8rem" }}
                        >
                          <i className="fas fa-motorcycle me-1"></i>
                          Kurye Bilgisi
                        </h6>
                        <div className="d-flex justify-content-between align-items-center">
                          <div>
                            <strong>{selectedOrder.courierName}</strong>
                            {selectedOrder.courierPhone && (
                              <div className="small text-muted">
                                <i className="fas fa-phone me-1"></i>
                                {selectedOrder.courierPhone}
                              </div>
                            )}
                          </div>
                          {selectedOrder.assignedAt && (
                            <div className="text-end small text-muted">
                              <div>Atandı:</div>
                              <div>
                                {new Date(
                                  selectedOrder.assignedAt,
                                ).toLocaleTimeString("tr-TR")}
                              </div>
                            </div>
                          )}
                        </div>
                      </div>
                    )}

                    {/* ================================================================
                    SİPARİŞ AKIŞ TİMELINE'I - Görsel durum takibi
                    Hangi personelin hangi aksiyonu yaptığını gösterir
                    ================================================================ */}
                    <div className="mt-3">
                      <h6
                        className="fw-bold mb-2"
                        style={{ fontSize: "0.8rem" }}
                      >
                        <i className="fas fa-stream me-1 text-primary"></i>
                        Sipariş Akış Durumu
                      </h6>

                      {/* Akış Timeline'ı */}
                      <div
                        className="d-flex align-items-center justify-content-between mb-2 flex-wrap gap-1"
                        style={{ fontSize: "0.6rem" }}
                      >
                        {/* Yeni */}
                        <div className="text-center">
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center mx-auto mb-1"
                            style={{
                              width: "28px",
                              height: "28px",
                              backgroundColor: [
                                "pending",
                                "new",
                                "confirmed",
                                "preparing",
                                "ready",
                                "assigned",
                                "picked_up",
                                "pickedup",
                                "out_for_delivery",
                                "outfordelivery",
                                "delivered",
                              ].includes(normalizedStatus)
                                ? "#6c757d"
                                : "#e9ecef",
                              color: [
                                "pending",
                                "new",
                                "confirmed",
                                "preparing",
                                "ready",
                                "assigned",
                                "picked_up",
                                "pickedup",
                                "out_for_delivery",
                                "outfordelivery",
                                "delivered",
                              ].includes(normalizedStatus)
                                ? "white"
                                : "#6c757d",
                            }}
                          >
                            <i
                              className="fas fa-circle"
                              style={{ fontSize: "0.5rem" }}
                            ></i>
                          </div>
                          <small>Yeni</small>
                        </div>
                        <div
                          className="flex-grow-1 mx-1"
                          style={{
                            height: "2px",
                            backgroundColor: [
                              "confirmed",
                              "preparing",
                              "ready",
                              "assigned",
                              "picked_up",
                              "pickedup",
                              "out_for_delivery",
                              "outfordelivery",
                              "delivered",
                            ].includes(normalizedStatus)
                              ? "#17a2b8"
                              : "#e9ecef",
                          }}
                        ></div>

                        {/* Onaylandı */}
                        <div className="text-center">
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center mx-auto mb-1"
                            style={{
                              width: "28px",
                              height: "28px",
                              backgroundColor: [
                                "confirmed",
                                "preparing",
                                "ready",
                                "assigned",
                                "picked_up",
                                "pickedup",
                                "out_for_delivery",
                                "outfordelivery",
                                "delivered",
                              ].includes(normalizedStatus)
                                ? "#17a2b8"
                                : "#e9ecef",
                              color: [
                                "confirmed",
                                "preparing",
                                "ready",
                                "assigned",
                                "picked_up",
                                "pickedup",
                                "out_for_delivery",
                                "outfordelivery",
                                "delivered",
                              ].includes(normalizedStatus)
                                ? "white"
                                : "#6c757d",
                            }}
                          >
                            <i
                              className="fas fa-check-circle"
                              style={{ fontSize: "0.5rem" }}
                            ></i>
                          </div>
                          <small>Onaylı</small>
                        </div>
                        <div
                          className="flex-grow-1 mx-1"
                          style={{
                            height: "2px",
                            backgroundColor: [
                              "preparing",
                              "ready",
                              "assigned",
                              "picked_up",
                              "pickedup",
                              "out_for_delivery",
                              "outfordelivery",
                              "delivered",
                            ].includes(normalizedStatus)
                              ? "#fd7e14"
                              : "#e9ecef",
                          }}
                        ></div>

                        {/* Hazırlanıyor */}
                        <div className="text-center">
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center mx-auto mb-1"
                            style={{
                              width: "28px",
                              height: "28px",
                              backgroundColor: [
                                "preparing",
                                "ready",
                                "assigned",
                                "picked_up",
                                "pickedup",
                                "out_for_delivery",
                                "outfordelivery",
                                "delivered",
                              ].includes(normalizedStatus)
                                ? "#fd7e14"
                                : "#e9ecef",
                              color: [
                                "preparing",
                                "ready",
                                "assigned",
                                "picked_up",
                                "pickedup",
                                "out_for_delivery",
                                "outfordelivery",
                                "delivered",
                              ].includes(normalizedStatus)
                                ? "white"
                                : "#6c757d",
                            }}
                          >
                            <i
                              className="fas fa-utensils"
                              style={{ fontSize: "0.5rem" }}
                            ></i>
                          </div>
                          <small>Hazırl.</small>
                        </div>
                        <div
                          className="flex-grow-1 mx-1"
                          style={{
                            height: "2px",
                            backgroundColor: [
                              "ready",
                              "assigned",
                              "picked_up",
                              "pickedup",
                              "out_for_delivery",
                              "outfordelivery",
                              "delivered",
                            ].includes(normalizedStatus)
                              ? "#28a745"
                              : "#e9ecef",
                          }}
                        ></div>

                        {/* Hazır */}
                        <div className="text-center">
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center mx-auto mb-1"
                            style={{
                              width: "28px",
                              height: "28px",
                              backgroundColor: [
                                "ready",
                                "assigned",
                                "picked_up",
                                "pickedup",
                                "out_for_delivery",
                                "outfordelivery",
                                "delivered",
                              ].includes(normalizedStatus)
                                ? "#28a745"
                                : "#e9ecef",
                              color: [
                                "ready",
                                "assigned",
                                "picked_up",
                                "pickedup",
                                "out_for_delivery",
                                "outfordelivery",
                                "delivered",
                              ].includes(normalizedStatus)
                                ? "white"
                                : "#6c757d",
                            }}
                          >
                            <i
                              className="fas fa-box"
                              style={{ fontSize: "0.5rem" }}
                            ></i>
                          </div>
                          <small>Hazır</small>
                        </div>
                        <div
                          className="flex-grow-1 mx-1"
                          style={{
                            height: "2px",
                            backgroundColor: [
                              "assigned",
                              "picked_up",
                              "pickedup",
                              "out_for_delivery",
                              "outfordelivery",
                              "delivered",
                            ].includes(normalizedStatus)
                              ? "#6f42c1"
                              : "#e9ecef",
                          }}
                        ></div>

                        {/* Yolda */}
                        <div className="text-center">
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center mx-auto mb-1"
                            style={{
                              width: "28px",
                              height: "28px",
                              backgroundColor: [
                                "assigned",
                                "picked_up",
                                "pickedup",
                                "out_for_delivery",
                                "outfordelivery",
                                "delivered",
                              ].includes(normalizedStatus)
                                ? "#6f42c1"
                                : "#e9ecef",
                              color: [
                                "assigned",
                                "picked_up",
                                "pickedup",
                                "out_for_delivery",
                                "outfordelivery",
                                "delivered",
                              ].includes(normalizedStatus)
                                ? "white"
                                : "#6c757d",
                            }}
                          >
                            <i
                              className="fas fa-motorcycle"
                              style={{ fontSize: "0.5rem" }}
                            ></i>
                          </div>
                          <small>Yolda</small>
                        </div>
                        <div
                          className="flex-grow-1 mx-1"
                          style={{
                            height: "2px",
                            backgroundColor:
                              normalizedStatus === "delivered"
                                ? "#343a40"
                                : "#e9ecef",
                          }}
                        ></div>

                        {/* Teslim */}
                        <div className="text-center">
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center mx-auto mb-1"
                            style={{
                              width: "28px",
                              height: "28px",
                              backgroundColor:
                                normalizedStatus === "delivered"
                                  ? "#343a40"
                                  : "#e9ecef",
                              color:
                                normalizedStatus === "delivered"
                                  ? "white"
                                  : "#6c757d",
                            }}
                          >
                            <i
                              className="fas fa-check-double"
                              style={{ fontSize: "0.5rem" }}
                            ></i>
                          </div>
                          <small>Teslim</small>
                        </div>
                      </div>

                      {/* Mevcut Durum Badge */}
                      <div className="text-center mb-2">
                        <span
                          className="badge px-3 py-2"
                          style={{
                            backgroundColor: getStatusHexColor(
                              selectedOrder.status,
                            ),
                            color: "white",
                            fontSize: "0.75rem",
                          }}
                        >
                          <i
                            className={`fas ${getStatusIcon(selectedOrder.status)} me-1`}
                          ></i>
                          {getStatusText(selectedOrder.status)}
                        </span>
                      </div>
                    </div>

                    {/* ================================================================
                    DETAYLI DURUM GEÇMİŞİ - Kim, ne zaman, ne yaptı
                    ================================================================ */}
                    {selectedOrder.statusHistory &&
                      selectedOrder.statusHistory.length > 0 && (
                        <div className="mt-2">
                          <h6
                            className="fw-bold mb-2"
                            style={{ fontSize: "0.8rem" }}
                          >
                            <i className="fas fa-history me-1 text-info"></i>
                            Detaylı Geçmiş
                          </h6>
                          <div
                            style={{ maxHeight: "150px", overflowY: "auto" }}
                          >
                            {selectedOrder.statusHistory.map(
                              (history, index) => (
                                <div
                                  key={index}
                                  className="d-flex align-items-start mb-2 pb-2 border-bottom"
                                  style={{ fontSize: "0.7rem" }}
                                >
                                  {/* Timeline Noktası */}
                                  <div
                                    className="rounded-circle d-flex align-items-center justify-content-center me-2 flex-shrink-0"
                                    style={{
                                      width: "24px",
                                      height: "24px",
                                      backgroundColor: getStatusHexColor(
                                        history.status,
                                      ),
                                      color: "white",
                                    }}
                                  >
                                    <i
                                      className={`fas ${getStatusIcon(history.status)}`}
                                      style={{ fontSize: "0.5rem" }}
                                    ></i>
                                  </div>

                                  {/* Detay */}
                                  <div className="flex-grow-1">
                                    <div className="d-flex justify-content-between align-items-start">
                                      <span className="fw-bold">
                                        {getStatusText(history.status)}
                                      </span>
                                      <small className="text-muted">
                                        {new Date(
                                          history.changedAt,
                                        ).toLocaleString("tr-TR", {
                                          day: "2-digit",
                                          month: "2-digit",
                                          hour: "2-digit",
                                          minute: "2-digit",
                                        })}
                                      </small>
                                    </div>
                                    {/* Personel bilgisi varsa göster */}
                                    {history.changedBy && (
                                      <small className="text-primary">
                                        <i className="fas fa-user me-1"></i>
                                        {history.changedBy}
                                      </small>
                                    )}
                                    {/* Not varsa göster */}
                                    {history.note && (
                                      <small className="text-muted d-block">
                                        <i className="fas fa-sticky-note me-1"></i>
                                        {history.note}
                                      </small>
                                    )}
                                  </div>
                                </div>
                              ),
                            )}
                          </div>
                        </div>
                      )}

                    {/* Kurye Atama - Teslim edilmiş, iptal, iade hariç tüm durumlarda ve kurye atanmamışsa */}
                    {!["delivered", "cancelled", "refunded"].includes(
                      normalizedStatus,
                    ) &&
                      !selectedOrder.courierId && (
                        <div className="mt-2 p-2 border border-success rounded bg-light">
                          <h6
                            className="fw-bold mb-1 text-success"
                            style={{ fontSize: "0.8rem" }}
                          >
                            <i className="fas fa-motorcycle me-1"></i>
                            Kurye Ata
                          </h6>
                          <div className="d-flex gap-1 flex-wrap">
                            {couriers.length > 0 ? (
                              couriers.map((courier) => (
                                <button
                                  key={courier.id}
                                  onClick={() =>
                                    assignCourier(selectedOrder.id, courier.id)
                                  }
                                  disabled={assigningCourier}
                                  className="btn btn-outline-success btn-sm px-2 py-1"
                                  style={{ fontSize: "0.65rem" }}
                                  title={`Durum: ${courier.status || "Bilinmiyor"}`}
                                >
                                  <i className="fas fa-motorcycle me-1"></i>
                                  {courier.courierName ||
                                    courier.name?.split(" ")[0] ||
                                    `Kurye ${courier.id}`}
                                  {courier.isOnline && (
                                    <span className="ms-1 text-success">●</span>
                                  )}
                                </button>
                              ))
                            ) : (
                              <span className="text-muted small">
                                Kurye bulunamadı
                              </span>
                            )}
                          </div>
                        </div>
                      )}

                    {/* Kurye Yeniden Atama - Kurye atanmış ama değiştirmek istenirse */}
                    {selectedOrder.courierId &&
                      !["delivered", "cancelled", "refunded"].includes(
                        normalizedStatus,
                      ) && (
                      <div className="mt-2 p-2 border border-info rounded bg-info bg-opacity-10">
                        <h6
                          className="fw-bold mb-1 text-info"
                          style={{ fontSize: "0.8rem" }}
                        >
                          <i className="fas fa-motorcycle me-1"></i>
                          Atanan Kurye
                        </h6>
                        <div className="d-flex align-items-center flex-wrap gap-1">
                          <span className="badge bg-info me-2">
                            Kurye #{selectedOrder.courierId}
                          </span>
                          {selectedOrder.courierName && (
                            <span className="fw-bold">
                              {selectedOrder.courierName}
                            </span>
                          )}
                        </div>
                        {/* Kurye Değiştir */}
                        <div className="mt-2">
                          <small className="text-muted d-block mb-1">Kurye Değiştir:</small>
                          <div className="d-flex gap-1 flex-wrap">
                            {couriers
                              .filter((c) => c.id !== selectedOrder.courierId)
                              .map((courier) => (
                                <button
                                  key={courier.id}
                                  onClick={() =>
                                    assignCourier(selectedOrder.id, courier.id)
                                  }
                                  disabled={assigningCourier}
                                  className="btn btn-outline-warning btn-sm px-2 py-1"
                                  style={{ fontSize: "0.6rem" }}
                                >
                                  <i className="fas fa-exchange-alt me-1"></i>
                                  {courier.courierName ||
                                    courier.name?.split(" ")[0] ||
                                    `Kurye ${courier.id}`}
                                </button>
                              ))}
                          </div>
                        </div>
                      </div>
                    )}

                    {/* ================================================================
                    HIZLI AKSİYON BUTONLARI
                    Sipariş durumuna göre uygun aksiyonlar gösterilir
                    ================================================================ */}
                    <div className="mt-3 p-2 border rounded bg-white">
                      <h6
                        className="fw-bold mb-2"
                        style={{ fontSize: "0.8rem" }}
                      >
                        <i className="fas fa-bolt me-1 text-warning"></i>
                        Hızlı Aksiyonlar
                      </h6>
                      <div className="d-flex gap-2 flex-wrap">
                        {/* Onayla butonu - Yeni/Bekleyen siparişler için */}
                        {(normalizedStatus === "new" ||
                          normalizedStatus === "pending" ||
                          normalizedStatus === "paid") && (
                          <button
                            className="btn btn-info btn-sm"
                            style={{ fontSize: "0.75rem" }}
                            onClick={() => {
                              updateOrderStatus(selectedOrder.id, "confirmed");
                              setSelectedOrder({
                                ...selectedOrder,
                                status: "confirmed",
                              });
                            }}
                          >
                            <i className="fas fa-check me-1"></i>
                            Onayla
                          </button>
                        )}

                        {/* Hazırlanıyor butonu - Onaylı siparişler için */}
                        {normalizedStatus === "confirmed" && (
                          <button
                            className="btn btn-warning btn-sm"
                            style={{ fontSize: "0.75rem" }}
                            onClick={() => {
                              updateOrderStatus(selectedOrder.id, "preparing");
                              setSelectedOrder({
                                ...selectedOrder,
                                status: "preparing",
                              });
                            }}
                          >
                            <i className="fas fa-fire me-1"></i>
                            Hazırlamaya Başla
                          </button>
                        )}

                        {/* Hazır butonu - Hazırlanan siparişler için */}
                        {normalizedStatus === "preparing" && (
                          <button
                            className="btn btn-success btn-sm"
                            style={{ fontSize: "0.75rem" }}
                            onClick={() => {
                              updateOrderStatus(selectedOrder.id, "ready");
                              setSelectedOrder({
                                ...selectedOrder,
                                status: "ready",
                              });
                            }}
                          >
                            <i className="fas fa-box me-1"></i>
                            Hazır
                          </button>
                        )}
                      </div>
                    </div>

                    {/* ================================================================
                    ADMİN / MARKET GÖREVLİSİ MANUEL DURUM DEĞİŞTİRME
                    Acil durumlar için tüm durumları değiştirebilir
                    ================================================================ */}
                    {
                      <div className="mt-3 p-2 border rounded bg-light">
                        <h6
                          className="fw-bold mb-2"
                          style={{ fontSize: "0.8rem" }}
                        >
                          <i className="fas fa-cog me-1 text-danger"></i>
                          Admin Kontrol Paneli
                        </h6>
                        <p
                          className="small text-muted mb-2"
                          style={{ fontSize: "0.65rem" }}
                        >
                          Acil durumlarda siparişin durumunu manuel olarak
                          değiştirebilirsiniz. Bu işlem tüm taraflara (müşteri,
                          kurye, mağaza) bildirim gönderir. Para iadesi
                          gerekiyorsa İptal Et butonunu kullanın.
                        </p>

                        <div className="row g-2 align-items-end">
                          <div className="col-8">
                            <label
                              className="form-label small mb-1"
                              style={{ fontSize: "0.7rem" }}
                            >
                              Yeni Durum Seç:
                            </label>
                            <select
                              className="form-select form-select-sm"
                              style={{ fontSize: "0.75rem" }}
                              value={(selectedOrder.status || "").toLowerCase()}
                              onChange={(e) => {
                                const newStatus = e.target.value;
                                const currentStatus = (
                                  selectedOrder.status || ""
                                ).toLowerCase();

                                // "assigned" durumuna manuel geçiş engelle - kurye atama ile yapılmalı
                                if (newStatus === "assigned") {
                                  alert(
                                    "⚠️ Kurye ataması için lütfen 'Kurye Ata' bölümünü kullanın.\n\nSiparişi önce 'Hazır' durumuna getirin, sonra bir kurye seçin.",
                                  );
                                  return;
                                }

                                if (newStatus !== currentStatus) {
                                  if (
                                    window.confirm(
                                      `Siparişi "${getStatusText(newStatus)}" durumuna güncellemek istediğinize emin misiniz?`,
                                    )
                                  ) {
                                    updateOrderStatus(
                                      selectedOrder.id,
                                      newStatus,
                                    );
                                    setSelectedOrder({
                                      ...selectedOrder,
                                      status: newStatus,
                                    });
                                  }
                                }
                              }}
                            >
                              <option value="new">🆕 Yeni Sipariş</option>
                              <option value="confirmed">✅ Onaylandı</option>
                              <option value="preparing">🍳 Hazırlanıyor</option>
                              <option value="ready">📦 Hazır</option>
                              <option value="assigned" disabled>
                                🚴 Kuryeye Atandı (Kurye Ata bölümünden)
                              </option>
                              <option value="picked_up">
                                🤝 Kurye Teslim Aldı
                              </option>
                              <option value="out_for_delivery">🛵 Yolda</option>
                              <option value="delivered">✓ Teslim Edildi</option>
                              <option value="delivery_failed">
                                ❌ Teslimat Başarısız
                              </option>
                              <option value="cancelled">🚫 İptal Edildi</option>
                            </select>
                          </div>
                          <div className="col-4">
                            <button
                              className="btn btn-danger btn-sm w-100"
                              style={{ fontSize: "0.7rem" }}
                              onClick={() => {
                                if (
                                  window.confirm(
                                    "Bu siparişi İPTAL etmek istediğinize emin misiniz?",
                                  )
                                ) {
                                  updateOrderStatus(
                                    selectedOrder.id,
                                    "cancelled",
                                  );
                                  setSelectedOrder({
                                    ...selectedOrder,
                                    status: "cancelled",
                                  });
                                }
                              }}
                            >
                              <i className="fas fa-times me-1"></i>
                              İptal Et
                            </button>
                          </div>
                        </div>
                      </div>
                    }
                  </div>
                </div>
              </div>
            </div>
          );
        })()}
    </div>
  );
}
