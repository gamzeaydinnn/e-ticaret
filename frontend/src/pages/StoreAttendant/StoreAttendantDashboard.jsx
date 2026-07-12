// ==========================================================================
// StoreAttendantDashboard.jsx - Market Görevlisi Ana Dashboard
// ==========================================================================
// Sipariş hazırlama, durum güncelleme ve tartı girişi paneli.
// SignalR ile real-time güncellemeler.
// Mobil-first responsive tasarım.
// ==========================================================================

import React, { useState, useEffect, useCallback, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useOptionalStoreAttendantAuth } from "../../contexts/StoreAttendantAuthContext";
import { useAuth } from "../../contexts/AuthContext";
import { isStrictVariableWeightProduct } from "../../utils/weightBasedProduct";
import storeAttendantService from "../../services/storeAttendantService";
import { AdminService } from "../../services/adminService";
import { getCouriers } from "../../services/dispatcherService";
import { signalRService, SignalREvents } from "../../services/signalRService";
import { WeightAdjustmentService } from "../../services/weightAdjustmentService";

// Ses dosyaları - Mixkit ücretsiz sesler
const SOUNDS = {
  newOrder: "/sounds/mixkit-melodic-race-countdown-1955.wav",
  orderReady: "/sounds/mixkit-happy-bells-notification-937.wav",
  alert: "/sounds/mixkit-bell-notification-933.wav",
};

export default function StoreAttendantDashboard({
  mode = "store",
  weightOnly = false,
}) {
  const navigate = useNavigate();
  const storeAuth = useOptionalStoreAttendantAuth();
  const { user: adminUser, logout: adminLogout } = useAuth();
  const isAdminMode = mode === "admin";
  const attendant = isAdminMode ? adminUser : storeAuth?.attendant;
  const logout = isAdminMode ? adminLogout : storeAuth?.logout;
  const isAuthenticated = isAdminMode
    ? !!adminUser
    : !!storeAuth?.isAuthenticated;
  const authLoading = isAdminMode ? false : (storeAuth?.loading ?? false);

  // =========================================================================
  // STATE TANIMLARI
  // =========================================================================
  const [orders, setOrders] = useState([]);
  const [summary, setSummary] = useState({
    confirmedCount: 0,
    preparingCount: 0,
    readyCount: 0,
    readyOrAssignedCount: 0,
    todayCompletedCount: 0,
  });
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  // Tab ve filtre
  // Ağırlık Raporları (weightOnly): varsayılan Preparing — tartı yalnız bu aşamada
  const [activeTab, setActiveTab] = useState(weightOnly ? "preparing" : "all");
  const [searchQuery, setSearchQuery] = useState("");

  // Tartı modal
  const [showWeightModal, setShowWeightModal] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [weightItems, setWeightItems] = useState([]);
  const [weightLoading, setWeightLoading] = useState(false);

  // Kurye atama
  const [couriers, setCouriers] = useState([]);
  const [showCourierModal, setShowCourierModal] = useState(false);
  const [courierOrderId, setCourierOrderId] = useState(null);
  const [assigningCourier, setAssigningCourier] = useState(false);

  // Ses bildirimi
  const [soundEnabled, setSoundEnabled] = useState(() => {
    return localStorage.getItem("storeAttendantSound") !== "false";
  });

  // Yeni sipariş animasyonu
  const [newOrderAnimation, setNewOrderAnimation] = useState(false);

  // Son güncelleme
  const [lastUpdated, setLastUpdated] = useState(new Date());

  // Audio ref
  const audioRef = useRef(null);

  // =========================================================================
  // SES BİLDİRİMİ
  // =========================================================================
  const playSound = useCallback(
    (soundType) => {
      if (!soundEnabled) return;

      try {
        if (audioRef.current) {
          audioRef.current.pause();
          audioRef.current.currentTime = 0;
        }

        const audio = new Audio(SOUNDS[soundType] || SOUNDS.alert);
        audioRef.current = audio;
        audio.volume = 0.7;
        audio.play().catch((err) => {
          console.warn("[StoreAttendant] Ses çalınamadı:", err.message);
        });
      } catch (err) {
        console.warn("[StoreAttendant] Ses hatası:", err);
      }
    },
    [soundEnabled],
  );

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================
  const fetchData = useCallback(
    async (showRefreshIndicator = false) => {
      if (showRefreshIndicator) {
        setRefreshing(true);
      }

      try {
        // Paralel API çağrıları
        const [ordersRes, summaryRes] = await Promise.all([
          storeAttendantService.getOrders(
            getStatusFromTab(activeTab),
            page,
            pageSize,
          ),
          storeAttendantService.getSummary(),
        ]);

        if (ordersRes.success) {
          const payload = ordersRes.data || {};
          const list = payload.orders || payload.Orders || [];
          const filteredList = weightOnly
            ? list.filter((order) => {
                const items = order.items || order.Items || [];
                return (
                  order.hasWeightBasedItems ||
                  order.HasWeightBasedItems ||
                  items.some((item) => {
                    const unit = (item.unit || item.Unit || "")
                      .toString()
                      .toLowerCase();

                    return isStrictVariableWeightProduct({
                      ...item,
                      name: item.productName || item.ProductName || item.name || item.Name,
                      categoryName:
                        item.categoryName ||
                        item.CategoryName ||
                        item.category?.name ||
                        item.Category?.Name ||
                        "",
                      unit,
                      weightUnit: item.weightUnit || item.WeightUnit || null,
                    });
                  })
                );
              })
            : list;

          setOrders(filteredList);
          setTotalPages(payload.totalPages || payload.TotalPages || 1);
          setTotalCount(
            weightOnly
              ? filteredList.length
              : payload.totalCount || payload.TotalCount || 0,
          );
          setPage(payload.currentPage || payload.CurrentPage || page);
        }

        if (summaryRes.success) {
          const raw = summaryRes.data || {};
          setSummary({
            confirmedCount: raw.confirmedCount ?? raw.ConfirmedCount ?? raw.pendingCount ?? 0,
            preparingCount: raw.preparingCount ?? raw.PreparingCount ?? 0,
            readyCount: raw.readyCount ?? raw.ReadyCount ?? 0,
            readyOrAssignedCount:
              raw.readyOrAssignedCount ?? raw.ReadyOrAssignedCount ?? raw.readyCount ?? 0,
            todayCompletedCount:
              raw.todayCompletedCount ??
              raw.TodayCompletedCount ??
              raw.completedTodayCount ??
              0,
          });
        }

        setLastUpdated(new Date());
        setError(null);
      } catch (err) {
        console.error("[StoreAttendant] Veri yükleme hatası:", err);
        setError("Veriler yüklenirken bir hata oluştu");
      } finally {
        setLoading(false);
        setRefreshing(false);
      }
    },
    [activeTab, page, pageSize, weightOnly],
  );

  // =========================================================================
  // TAB → STATUS DÖNÜŞÜMÜ
  // NEDEN: Backend'e hangi durumların çekileceğini iletir.
  // "all" veya null → Tüm izin verilen siparişler (Pending, Confirmed, New, Paid, Preparing, Ready)
  // "pending" → Pending + Confirmed + New + Paid (bekleyen siparişler)
  // "completed" → Ready + Assigned (ağırlık paneli Tamamlandı)
  // =========================================================================
  const getStatusFromTab = (tab) => {
    switch (tab) {
      case "all":
        // Tüm siparişleri getir (status filtresi gönderme)
        return null;
      case "confirmed":
        // "pending" değeri backend'de Pending, Confirmed, New, Paid durumlarını döndürür
        return "pending";
      case "preparing":
        return "Preparing";
      case "ready":
        return "Ready";
      case "completed":
        // Tartı bitti + kurye atandı — admin/müşteri ile aynı Order.Status
        return "completed";
      default:
        return null; // Tüm siparişler
    }
  };

  // =========================================================================
  // İLK YÜKLEME VE POLLING
  // =========================================================================
  useEffect(() => {
    if (!authLoading && isAuthenticated) {
      fetchData();

      // Polling - her 30 saniyede güncelle
      const pollInterval = setInterval(() => {
        fetchData(false);
      }, 30000);

      return () => clearInterval(pollInterval);
    }
  }, [authLoading, isAuthenticated, fetchData]);

  // Tab değiştiğinde verileri yeniden çek
  useEffect(() => {
    if (!loading && isAuthenticated) {
      fetchData(true);
    }
  }, [activeTab, page, pageSize]);

  useEffect(() => {
    setPage(1);
  }, [activeTab, searchQuery, pageSize]);

  // =========================================================================
  // BROWSER NOTIFICATION
  // NEDEN: Sekme arka planda olsa bile kullanıcıyı uyarmak için
  // =========================================================================
  const showBrowserNotification = useCallback((title, body) => {
    if (Notification.permission === "granted") {
      new Notification(title, {
        body,
        icon: "/logo192.png",
        tag: "store-notification",
        requireInteraction: true,
      });
    } else if (Notification.permission !== "denied") {
      Notification.requestPermission().then((permission) => {
        if (permission === "granted") {
          new Notification(title, { body, icon: "/logo192.png" });
        }
      });
    }
  }, []);

  // Browser notification izni iste (ilk yüklemede)
  useEffect(() => {
    if (isAuthenticated && Notification.permission === "default") {
      Notification.requestPermission();
    }
  }, [isAuthenticated]);

  // =========================================================================
  // SIGNALR ENTEGRASYONU
  // =========================================================================
  useEffect(() => {
    if (!isAuthenticated) return;

    // SignalR bağlantısını başlat
    const connectSignalR = async () => {
      try {
        await signalRService.connectStoreAttendant();
        console.log("✅ SignalR Store Hub bağlantısı kuruldu");
      } catch (error) {
        console.warn("⚠️ SignalR bağlantısı kurulamadı:", error);
      }
    };

    connectSignalR();

    // Event listener'lar
    const handleNewOrder = (data) => {
      console.log("📦 Yeni sipariş geldi:", data);
      playSound("newOrder");
      showBrowserNotification(
        "🛒 Yeni Sipariş!",
        `Sipariş #${data.orderNumber || data.orderId} geldi. Hazırlanması bekleniyor.`,
      );
      fetchData(false);

      // Animasyon
      setNewOrderAnimation(true);
      setTimeout(() => setNewOrderAnimation(false), 3000);
    };

    const handleOrderStatusChanged = (data) => {
      console.log("🔄 Sipariş durumu değişti:", data);
      fetchData(false);
    };

    // =========================================================================
    // SES BİLDİRİMİ DİNLEYİCİSİ
    // Backend "PlaySound" event'i gönderdiğinde ses çal
    // NEDEN: Merkezi ses yönetimi için backend kontrollü bildirim
    // =========================================================================
    const handlePlaySound = (data) => {
      console.log("🔊 [StoreAttendant] Backend'den ses bildirimi:", data);
      const soundType = data?.soundType || "newOrder";
      playSound(soundType === "new_order" ? "newOrder" : soundType);
    };

    // Listener'ları kaydet
    signalRService.onStoreAttendantEvent("NewOrderForStore", handleNewOrder);
    signalRService.onStoreAttendantEvent(
      "OrderStatusChanged",
      handleOrderStatusChanged,
    );
    signalRService.onStoreAttendantEvent("OrderConfirmed", handleNewOrder);
    signalRService.onStoreAttendantEvent("PlaySound", handlePlaySound);

    // Cleanup
    return () => {
      signalRService.disconnectStoreAttendant();
    };
  }, [isAuthenticated, playSound, fetchData]);

  // =========================================================================
  // SİPARİŞ DURUMU GÜNCELLEME
  // =========================================================================
  const updateOrderStatus = async (orderId, newStatus) => {
    try {
      const normalized = normalizeStatus(newStatus);
      const usesStoreAttendantFlow = ["confirmed", "preparing", "ready"].includes(
        normalized,
      );

      const result = usesStoreAttendantFlow
        ? await storeAttendantService.updateOrderStatus(orderId, newStatus)
        : await AdminService.updateOrderStatus(orderId, newStatus);

      if (result?.success === false) {
        return { success: false, error: result.error || result.message };
      }

      if (result || usesStoreAttendantFlow) {
        // Ses çal
        if (newStatus === "Ready") {
          playSound("orderReady");
        }

        // Verileri yenile
        fetchData(true);

        return { success: true };
      } else {
        return { success: false, error: result.error };
      }
    } catch (err) {
      console.error("[StoreAttendant] Durum güncelleme hatası:", err);
      return { success: false, error: "Durum güncellenemedi" };
    }
  };

  const deleteOrder = async (orderId) => {
    if (!window.confirm("Bu siparişi kalıcı olarak silmek istiyor musunuz?")) {
      return;
    }
    try {
      await AdminService.deleteOrder(orderId);
      fetchData(true);
    } catch (err) {
      console.error("[StoreAttendant] Sipariş silme hatası:", err);
      alert("Sipariş silinemedi");
    }
  };

  // Hazırlamaya Başla
  const handleStartPreparing = (order) => {
    updateOrderStatus(order.id, "Preparing");
  };

  // Hazır İşaretle — kg ürünlerde tartı modalı yalnız Preparing'de açılır
  const handleMarkReady = async (order) => {
    if (!order?.hasWeightBasedItems && !order?.HasWeightBasedItems) {
      updateOrderStatus(order.id, "Ready");
      return;
    }

    const status = normalizeStatus(order.status);
    if (status !== "preparing") {
      alert(
        "Tartı girişi yalnızca Hazırlanıyor aşamasında yapılabilir. Önce siparişi onaylayıp hazırlamaya başlayın.",
      );
      return;
    }

    setWeightLoading(true);
    try {
      const result = await storeAttendantService.getOrderDetail(order.id);
      if (!result.success) {
        alert(result.error || "Sipariş detayı yüklenemedi");
        return;
      }

      const detail = result.data;
      const orderItems = detail?.orderItems || detail?.OrderItems || [];
      const editableWeightItems = orderItems
        .filter((item) => {
          const weightUnit = (
            item.weightUnit ||
            item.WeightUnit ||
            ""
          ).toString().toLowerCase();

          return isStrictVariableWeightProduct({
            ...item,
            name: item.productName || item.ProductName || item.name || item.Name,
            categoryName:
              item.categoryName ||
              item.CategoryName ||
              item.category?.name ||
              item.Category?.Name ||
              "",
            unit: item.unit || item.Unit || "",
            weightUnit: item.weightUnit || item.WeightUnit || weightUnit,
            isWeightBased: item.isWeightBased ?? item.IsWeightBased,
            pricePerUnit: item.pricePerUnit ?? item.PricePerUnit ?? item.unitPrice,
          });
        })
        .map((item) => {
          const quantity = Number(item.quantity ?? item.Quantity ?? 1) || 1;
          const unitPrice = Number(
            item.unitPrice ?? item.UnitPrice ?? item.pricePerUnit ?? item.PricePerUnit ?? 0,
          );
          const lineTotal = Number(item.lineTotal ?? item.LineTotal ?? 0);
          // Backend gram tutar; eski kayıtlarda 0 olabilir → quantity×1000 (1 adet ≈ 1 kg) fallback
          let estimatedWeight = Number(
            item.estimatedWeight ?? item.EstimatedWeight ?? 0,
          );
          if (!estimatedWeight || estimatedWeight <= 0) {
            estimatedWeight = quantity * 1000;
          }
          let estimatedPrice = Number(
            item.estimatedPrice ?? item.EstimatedPrice ?? 0,
          );
          if (!estimatedPrice || estimatedPrice <= 0) {
            estimatedPrice =
              lineTotal > 0
                ? lineTotal
                : unitPrice > 0
                  ? Math.round(unitPrice * (estimatedWeight / 1000) * 100) / 100
                  : 0;
          }
          const actualWeightRaw =
            item.actualWeight ?? item.ActualWeight ?? null;
          const actualWeight =
            actualWeightRaw != null && Number(actualWeightRaw) > 0
              ? Number(actualWeightRaw)
              : estimatedWeight;

          return {
            id: item.id || item.Id,
            productName: item.productName || item.ProductName || "Ürün",
            estimatedWeight,
            actualWeight,
            estimatedPrice,
            unitPrice,
            actualPrice: item.actualPrice ?? item.ActualPrice ?? null,
            priceDifference: item.priceDifference ?? item.PriceDifference ?? null,
          };
        });

      if (editableWeightItems.length === 0) {
        updateOrderStatus(order.id, "Ready");
        return;
      }

      setSelectedOrder(order);
      setWeightItems(editableWeightItems);
      setShowWeightModal(true);
    } catch (err) {
      console.error("[StoreAttendant] Sipariş detayı yükleme hatası:", err);
      alert("Sipariş detayı yüklenemedi");
    } finally {
      setWeightLoading(false);
    }
  };

  // Tartı girişi — yalnız Preparing; otomatik Onayla/Hazırlamaya Başla YOK
  const handleWeightSubmit = async () => {
    if (!selectedOrder || weightItems.length === 0) return;

    const currentStatus = normalizeStatus(selectedOrder.status);
    if (currentStatus !== "preparing") {
      alert(
        "Tartı girişi yalnızca Hazırlanıyor aşamasında yapılabilir. Önce siparişi onaylayıp hazırlamaya başlayın.",
      );
      return;
    }

    setWeightLoading(true);
    try {
      let latestWeightUpdate = null;

      for (const item of weightItems) {
        const actualWeight = Number(item.actualWeight);
        if (!actualWeight || actualWeight <= 0) {
          alert(`${item.productName} için geçerli bir ağırlık girin`);
          return;
        }

        const updateResponse = await WeightAdjustmentService.updateManualWeight(
          selectedOrder.id,
          item.id,
          actualWeight,
        );
        latestWeightUpdate = updateResponse?.data?.data ?? null;
      }

      const result = await storeAttendantService.markAsReady(selectedOrder.id);
      if (!result.success) {
        alert(result.error || "Sipariş hazır işaretlenemedi");
        return;
      }

      setShowWeightModal(false);
      setSelectedOrder(null);
      setWeightItems([]);

      // Tartı bitti → Tamamlandı sekmesi (activeTab değişince useEffect fetch eder)
      if (weightOnly) {
        setActiveTab("completed");
      } else {
        fetchData(true);
      }

      if (latestWeightUpdate?.requiresManualCollection) {
        const flow = latestWeightUpdate.paymentFlowType || "";
        const overage = Number(latestWeightUpdate.uncollectableOverageAmount || 0).toFixed(2);
        const finalAmt = Number(latestWeightUpdate.finalAmount || 0).toFixed(2);

        if (flow === "SaleImmediate") {
          alert(
            `Sipariş yeni tutarı ${finalAmt} TL oldu. Ödeme Sale (anında çekim) modunda olduğu için ${overage} TL fark karttan otomatik çekilemez — teslimatta manuel tahsilat gerekir.`,
          );
        } else if (latestWeightUpdate.exceedsPreAuthLimit) {
          alert(
            `Sipariş yeni tutarı ${finalAmt} TL oldu. Provizyon limiti (Auth×1.20) en fazla ${Number(latestWeightUpdate.maxCaptureAmountFromPreAuth || 0).toFixed(2)} TL çekime izin veriyor; kalan ${overage} TL manuel tahsilat gerektirir.`,
          );
        }
      } else if (latestWeightUpdate?.requiresManualRefund) {
        alert(
          `Tartı eksik geldi. ${Math.abs(Number(latestWeightUpdate.priceDifference || 0)).toFixed(2)} TL iade/manuel düzeltme gerekebilir (Sale modu).`,
        );
      }
    } catch (err) {
      console.error("[StoreAttendant] Tartı girişi hatası:", err);
      const apiMessage =
        err?.response?.data?.message ||
        err?.response?.data?.error ||
        err?.response?.data?.title ||
        err?.message;
      alert(apiMessage || "Ağırlık düzenlenirken bir hata oluştu");
    } finally {
      setWeightLoading(false);
    }
  };

  // =========================================================================
  // KURYE ATAMA (MVP)
  // =========================================================================

  // Kuryeleri yükle
  const loadCouriers = useCallback(async () => {
    try {
      const result = await getCouriers();
      console.log("🚴 [StoreAttendant] Kurye API sonucu:", result);

      if (result.success && result.data) {
        // Backend DispatcherCourierListResponseDto döner: { couriers: [...], onlineCount, ... }
        const courierList = result.data.couriers || result.data || [];
        console.log("🚴 [StoreAttendant] Kurye listesi:", courierList);
        setCouriers(Array.isArray(courierList) ? courierList : []);
      } else {
        console.warn(
          "🚴 [StoreAttendant] Kurye listesi alınamadı:",
          result.error,
        );
        setCouriers([]);
      }
    } catch (err) {
      console.error("[StoreAttendant] Kurye listesi hatası:", err);
      setCouriers([]);
    }
  }, []); // Kurye modal aç
  const handleOpenCourierModal = (orderId) => {
    setCourierOrderId(orderId);
    setShowCourierModal(true);
    loadCouriers();
  };

  // Kurye ata — StoreAttendant API (SignalR ile admin/kurye/müşteri senkron)
  const handleAssignCourier = async (courierId) => {
    if (!courierOrderId || !courierId) return;

    setAssigningCourier(true);
    try {
      const result = await storeAttendantService.assignCourier(
        courierOrderId,
        courierId,
      );
      if (result.success) {
        playSound("orderReady");
        setShowCourierModal(false);
        setCourierOrderId(null);
        if (weightOnly) {
          setActiveTab("completed");
        }
        // activeTab aynı kalsa bile listeyi yenile (kurye adı görünsün)
        fetchData(true);
      } else {
        alert(result.error || result.message || "Kurye atanamadı");
      }
    } catch (err) {
      console.error("[StoreAttendant] Kurye atama hatası:", err);
      alert("Kurye atama sırasında hata oluştu");
    } finally {
      setAssigningCourier(false);
    }
  };

  // =========================================================================
  // FİLTRELEME
  // =========================================================================
  const filteredOrders = orders.filter((order) => {
    if (!searchQuery) return true;
    const query = searchQuery.toLowerCase();
    return (
      order.orderNumber?.toLowerCase().includes(query) ||
      order.customerName?.toLowerCase().includes(query)
    );
  });

  // =========================================================================
  // SES TOGGLE
  // =========================================================================
  const handleSoundToggle = () => {
    const newValue = !soundEnabled;
    setSoundEnabled(newValue);
    localStorage.setItem("storeAttendantSound", newValue.toString());

    if (newValue) {
      playSound("alert");
    }
  };

  // =========================================================================
  // LOGOUT
  // =========================================================================
  const handleLogout = () => {
    if (typeof logout === "function") {
      logout();
    }
    navigate(isAdminMode ? "/admin/login" : "/store/login");
  };

  // =========================================================================
  // DURUM YARDIMCI FONKSİYONLARI
  // =========================================================================
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

  const getStatusText = (status) => {
    const normalized = normalizeStatus(status);
    if (weightOnly && normalized === "ready") {
      return "Tamamlandı";
    }
    const statusMap = {
      new: "Yeni",
      pending: "Beklemede",
      paid: "Ödendi",
      preauthorized: "Provizyon",
      confirmed: "Onaylandı",
      preparing: "Hazırlanıyor",
      ready: "Hazır",
      assigned: "Kuryeye Atandı",
      picked_up: "Teslim Alındı",
      out_for_delivery: "Yolda",
      delivered: "Teslim Edildi",
    };
    return statusMap[normalized] || status || "-";
  };

  const getStatusColor = (status) => {
    const normalized = normalizeStatus(status);
    const colorMap = {
      new: "secondary",
      pending: "secondary",
      paid: "success",
      preauthorized: "info",
      confirmed: "primary",
      preparing: "warning",
      ready: "success",
      assigned: "info",
      picked_up: "info",
      out_for_delivery: "primary",
      delivered: "success",
    };
    return colorMap[normalized] || "secondary";
  };

  const getStatusIcon = (status) => {
    const normalized = normalizeStatus(status);
    const iconMap = {
      new: "fa-circle",
      pending: "fa-clock",
      confirmed: "fa-check",
      preparing: "fa-utensils",
      ready: "fa-box",
      assigned: "fa-user-check",
      picked_up: "fa-hand-holding",
      out_for_delivery: "fa-motorcycle",
      delivered: "fa-check-circle",
    };
    return iconMap[normalized] || "fa-circle";
  };

  // =========================================================================
  // AUTH KONTROLÜ
  // =========================================================================
  if (authLoading) {
    return (
      <div
        className="min-vh-100 d-flex align-items-center justify-content-center"
        style={{
          background: "linear-gradient(135deg, #2E7D32 0%, #66BB6A 100%)",
        }}
      >
        <div className="text-center text-white">
          <div
            className="spinner-border mb-3"
            style={{ width: "3rem", height: "3rem" }}
          ></div>
          <p>Yükleniyor...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    navigate(isAdminMode ? "/admin/login" : "/store/login");
    return null;
  }

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <>
      {/* CSS Animasyonları */}
      <style>{`
        @keyframes newOrderPulse {
          0%, 100% { box-shadow: 0 0 0 0 rgba(46, 125, 50, 0.7); }
          50% { box-shadow: 0 0 0 15px rgba(46, 125, 50, 0); }
        }
        .new-order-pulse { animation: newOrderPulse 1s infinite; }

        @keyframes shake {
          0%, 100% { transform: translateX(0); }
          10%, 30%, 50%, 70%, 90% { transform: translateX(-2px); }
          20%, 40%, 60%, 80% { transform: translateX(2px); }
        }
        .shake-animation { animation: shake 0.5s ease-in-out; }

        .order-card {
          transition: all 0.2s ease;
          border-radius: 16px !important;
        }
        .order-card:hover {
          transform: translateY(-2px);
          box-shadow: 0 8px 25px rgba(0,0,0,0.1) !important;
        }

        .status-confirmed { border-left: 4px solid #0d6efd !important; }
        .status-preparing { border-left: 4px solid #fd7e14 !important; }
        .status-ready { border-left: 4px solid #198754 !important; }

        /* Mobil optimizasyonlar */
        @media (max-width: 768px) {
          .mobile-action-btn {
            min-height: 48px;
            font-size: 0.9rem !important;
            padding: 12px 16px !important;
          }
          .stat-card-mobile .card-body {
            padding: 12px !important;
          }
          .stat-card-mobile h3 {
            font-size: 1.5rem !important;
          }
          .mobile-search {
            position: fixed;
            bottom: 70px;
            left: 0;
            right: 0;
            z-index: 1040;
            padding: 10px 15px;
            background: white;
            box-shadow: 0 -2px 10px rgba(0,0,0,0.1);
          }
        }

        /* Bottom Navigation - Mobil */
        .mobile-bottom-nav {
          position: fixed;
          bottom: 0;
          left: 0;
          right: 0;
          background: white;
          box-shadow: 0 -2px 10px rgba(0,0,0,0.1);
          z-index: 1050;
          padding-bottom: env(safe-area-inset-bottom);
        }
        @media (min-width: 769px) {
          .mobile-bottom-nav { display: none !important; }
        }

        .bottom-nav-item {
          flex: 1;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          padding: 10px 5px;
          color: #6c757d;
          text-decoration: none;
          font-size: 0.7rem;
          transition: all 0.2s;
          min-height: 56px;
        }
        .bottom-nav-item.active {
          color: #2E7D32;
        }
        .bottom-nav-item i {
          font-size: 1.2rem;
          margin-bottom: 3px;
        }

        .store-attendant-topbar {
          z-index: 1030;
        }

        .store-attendant-admin-toolbar {
          position: relative;
          z-index: 1;
        }
      `}</style>

      <div className="min-vh-100 bg-light" style={{ paddingBottom: "70px" }}>
        {/* ================================================================ */}
        {/* HEADER - Admin modunda AdminLayout üst barı kullanılır */}
        {/* ================================================================ */}
        {!isAdminMode ? (
        <nav
          className="navbar navbar-dark sticky-top py-2 px-3 store-attendant-topbar"
          style={{
            background: "linear-gradient(135deg, #2E7D32 0%, #43A047 100%)",
          }}
        >
          <div className="container-fluid">
            {/* Sol - Logo */}
            <span className="navbar-brand d-flex align-items-center">
              <i className="fas fa-store me-2"></i>
              <span className="d-none d-sm-inline">
                {isAdminMode ? "Sipariş Hazırlama ve Tartı" : "Hazırlama Paneli"}
              </span>
              <span className="d-sm-none">
                {isAdminMode ? "Tartı" : "Hazırlama"}
              </span>
            </span>

            {/* Sağ - Butonlar */}
            <div className="d-flex align-items-center gap-2">
              {/* Yenile */}
              <button
                className="btn btn-sm btn-outline-light"
                onClick={() => fetchData(true)}
                disabled={refreshing}
                style={{ fontSize: "0.8rem" }}
              >
                <i
                  className={`fas fa-sync-alt ${refreshing ? "fa-spin" : ""}`}
                ></i>
              </button>

              {/* Ses Toggle */}
              <button
                className={`btn btn-sm ${soundEnabled ? "btn-light" : "btn-outline-light"}`}
                onClick={handleSoundToggle}
                style={{ fontSize: "0.8rem" }}
                title={soundEnabled ? "Sesi Kapat" : "Sesi Aç"}
              >
                <i
                  className={`fas fa-${soundEnabled ? "bell" : "bell-slash"}`}
                ></i>
              </button>

              {/* Kullanıcı & Çıkış */}
              <div className="dropdown">
                <button
                  className="btn btn-sm btn-outline-light d-flex align-items-center gap-1"
                  data-bs-toggle="dropdown"
                  style={{ fontSize: "0.8rem" }}
                >
                  <i className="fas fa-user-circle"></i>
                  <span className="d-none d-md-inline">
                    {attendant?.name?.split(" ")[0]}
                  </span>
                  <i
                    className="fas fa-chevron-down"
                    style={{ fontSize: "0.6rem" }}
                  ></i>
                </button>
                <ul className="dropdown-menu dropdown-menu-end shadow">
                  <li>
                    <span className="dropdown-item-text text-muted small">
                      {attendant?.email}
                    </span>
                  </li>
                  <li>
                    <hr className="dropdown-divider" />
                  </li>
                  <li>
                    <button
                      className="dropdown-item text-danger"
                      onClick={handleLogout}
                    >
                      <i className="fas fa-sign-out-alt me-2"></i>
                      Çıkış Yap
                    </button>
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </nav>
        ) : (
          <div className="d-flex justify-content-end align-items-center gap-2 px-3 py-2 bg-white border-bottom store-attendant-admin-toolbar">
            <button
              className="btn btn-sm btn-outline-success"
              onClick={() => fetchData(true)}
              disabled={refreshing}
              title="Yenile"
            >
              <i
                className={`fas fa-sync-alt ${refreshing ? "fa-spin" : ""}`}
              ></i>
            </button>
            <button
              className={`btn btn-sm ${soundEnabled ? "btn-success" : "btn-outline-success"}`}
              onClick={handleSoundToggle}
              title={soundEnabled ? "Sesi Kapat" : "Sesi Aç"}
            >
              <i
                className={`fas fa-${soundEnabled ? "bell" : "bell-slash"}`}
              ></i>
            </button>
          </div>
        )}

        {/* ================================================================ */}
        {/* ANA İÇERİK */}
        {/* ================================================================ */}
        <div className="container-fluid p-3">
          {/* Son Güncelleme */}
          <div className="d-flex justify-content-between align-items-center mb-3">
            <small className="text-muted">
              Son güncelleme: {lastUpdated.toLocaleTimeString("tr-TR")}
            </small>
            {newOrderAnimation && (
              <span className="badge bg-success new-order-pulse">
                <i className="fas fa-bell me-1"></i> Yeni sipariş!
              </span>
            )}
          </div>

          {/* İstatistik Kartları */}
          <div className="row g-2 mb-3">
            {weightOnly ? (
              <div className="col-12">
                <div
                  className="alert alert-info py-2 mb-0 small"
                  role="status"
                >
                  <i className="fas fa-weight me-2"></i>
                  <strong>Bekleyen</strong> sekmesinde gram girin; kayıt sonrası
                  sipariş <strong>Tamamlandı</strong>ya geçer. Oradan kurye
                  atayın — durum Admin, kurye ve müşteri panellerinde aynı anda
                  güncellenir.
                </div>
              </div>
            ) : null}

            {!weightOnly && (
            <div className="col-3">
              <div
                className={`card border-0 shadow-sm stat-card-mobile h-100 ${activeTab === "confirmed" ? "border-primary border-2" : ""}`}
                onClick={() => setActiveTab("confirmed")}
                style={{ cursor: "pointer", borderRadius: "12px" }}
              >
                <div className="card-body text-center p-2">
                  <i
                    className="fas fa-check text-primary mb-1"
                    style={{ fontSize: "1.2rem" }}
                  ></i>
                  <h3 className="fw-bold mb-0 text-primary">
                    {summary.confirmedCount || 0}
                  </h3>
                  <small className="text-muted" style={{ fontSize: "0.65rem" }}>
                    Onaylı
                  </small>
                </div>
              </div>
            </div>
            )}

            <div className={weightOnly ? "col-6" : "col-3"}>
              <div
                className={`card border-0 shadow-sm stat-card-mobile h-100 ${activeTab === "preparing" ? "border-warning border-2" : ""}`}
                onClick={() => setActiveTab("preparing")}
                style={{ cursor: "pointer", borderRadius: "12px" }}
              >
                <div className="card-body text-center p-2">
                  <i
                    className="fas fa-utensils text-warning mb-1"
                    style={{ fontSize: "1.2rem" }}
                  ></i>
                  <h3 className="fw-bold mb-0 text-warning">
                    {summary.preparingCount || 0}
                  </h3>
                  <small className="text-muted" style={{ fontSize: "0.65rem" }}>
                    {weightOnly ? "Bekleyen" : "Hazırlanıyor"}
                  </small>
                </div>
              </div>
            </div>

            {weightOnly ? (
            <div className="col-6">
              <div
                className={`card border-0 shadow-sm stat-card-mobile h-100 ${activeTab === "completed" ? "border-success border-2" : ""}`}
                onClick={() => setActiveTab("completed")}
                style={{ cursor: "pointer", borderRadius: "12px" }}
              >
                <div className="card-body text-center p-2">
                  <i
                    className="fas fa-check-circle text-success mb-1"
                    style={{ fontSize: "1.2rem" }}
                  ></i>
                  <h3 className="fw-bold mb-0 text-success">
                    {summary.readyOrAssignedCount || summary.readyCount || 0}
                  </h3>
                  <small className="text-muted" style={{ fontSize: "0.65rem" }}>
                    Tamamlandı
                  </small>
                </div>
              </div>
            </div>
            ) : (
            <>
            <div className="col-3">
              <div
                className={`card border-0 shadow-sm stat-card-mobile h-100 ${activeTab === "ready" ? "border-success border-2" : ""}`}
                onClick={() => setActiveTab("ready")}
                style={{ cursor: "pointer", borderRadius: "12px" }}
              >
                <div className="card-body text-center p-2">
                  <i
                    className="fas fa-box text-success mb-1"
                    style={{ fontSize: "1.2rem" }}
                  ></i>
                  <h3 className="fw-bold mb-0 text-success">
                    {summary.readyCount || 0}
                  </h3>
                  <small className="text-muted" style={{ fontSize: "0.65rem" }}>
                    Hazır
                  </small>
                </div>
              </div>
            </div>

            <div className="col-3">
              <div
                className="card border-0 shadow-sm stat-card-mobile h-100"
                style={{ borderRadius: "12px" }}
              >
                <div className="card-body text-center p-2">
                  <i
                    className="fas fa-check-circle text-info mb-1"
                    style={{ fontSize: "1.2rem" }}
                  ></i>
                  <h3 className="fw-bold mb-0 text-info">
                    {summary.todayCompletedCount || 0}
                  </h3>
                  <small className="text-muted" style={{ fontSize: "0.65rem" }}>
                    Bugün
                  </small>
                </div>
              </div>
            </div>
            </>
            )}
          </div>

          {/* Arama - Desktop */}
          <div className="d-none d-md-block mb-3">
            <div className="input-group">
              <span className="input-group-text bg-white border-end-0">
                <i className="fas fa-search text-muted"></i>
              </span>
              <input
                type="text"
                className="form-control border-start-0"
                placeholder="Sipariş no veya müşteri adı ara..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                style={{ borderRadius: "0 8px 8px 0" }}
              />
            </div>
          </div>

          {/* Hata Mesajı */}
          {error && (
            <div className="alert alert-danger d-flex align-items-center py-2 mb-3">
              <i className="fas fa-exclamation-circle me-2"></i>
              {error}
            </div>
          )}

          {/* Sipariş Listesi */}
          {loading ? (
            <div className="text-center py-5">
              <div className="spinner-border text-success mb-3"></div>
              <p className="text-muted">Yükleniyor...</p>
            </div>
          ) : filteredOrders.length === 0 ? (
            <div className="text-center py-5">
              <i className="fas fa-inbox fa-3x text-muted mb-3"></i>
              <p className="text-muted">
                {activeTab === "confirmed" && "Onaylanmış sipariş yok"}
                {activeTab === "preparing" &&
                  (weightOnly
                    ? "Tartı bekleyen sipariş yok"
                    : "Hazırlanmakta olan sipariş yok")}
                {activeTab === "ready" && "Hazır sipariş yok"}
                {activeTab === "completed" &&
                  "Tamamlanmış (tartılmış) sipariş yok"}
              </p>
            </div>
          ) : (
            <div className="row g-3">
              {filteredOrders.map((order) => (
                <div key={order.id} className="col-12 col-md-6 col-lg-4">
                  <div
                    className={`card order-card border-0 shadow-sm status-${(order.status || "").toLowerCase()}`}
                  >
                    <div className="card-body p-3">
                      {/* Header */}
                      <div className="d-flex justify-content-between align-items-start mb-2">
                        <div>
                          <h6 className="fw-bold mb-0">#{order.orderNumber}</h6>
                          <small className="text-muted">
                            {order.customerName}
                          </small>
                        </div>
                        <span
                          className={`badge bg-${getStatusColor(order.status)}`}
                        >
                          <i
                            className={`fas ${getStatusIcon(order.status)} me-1`}
                          ></i>
                          {getStatusText(order.status)}
                        </span>
                      </div>

                      {/* Ürün Sayısı */}
                      <div className="d-flex align-items-center mb-2 text-muted small">
                        <i className="fas fa-shopping-basket me-2"></i>
                        <span>
                          {order.itemCount || order.items?.length || 0} ürün
                        </span>
                        {(() => {
                          const amount = getOrderAmount(order);
                          if (!Number.isFinite(amount)) return null;
                          return (
                            <>
                              <span className="mx-2">•</span>
                              <span className="fw-semibold text-dark">
                                {amount.toFixed(2)} ₺
                              </span>
                            </>
                          );
                        })()}
                      </div>

                      {/* Zaman Bilgisi */}
                      <div className="d-flex align-items-center mb-3 text-muted small">
                        <i className="fas fa-clock me-2"></i>
                        <span>
                          {order.createdAt
                            ? new Date(order.createdAt).toLocaleTimeString(
                                "tr-TR",
                                {
                                  hour: "2-digit",
                                  minute: "2-digit",
                                },
                              )
                            : "-"}
                        </span>
                      </div>

                      {weightOnly &&
                        (order.allItemsWeighed ||
                          order.AllItemsWeighed ||
                          ["ready", "assigned"].includes(
                            normalizeStatus(order.status),
                          )) && (
                          <div className="mb-2">
                            <span className="badge bg-success-subtle text-success border border-success-subtle">
                              <i className="fas fa-check me-1"></i>
                              Tartı tamamlandı
                            </span>
                            {(order.courierName || order.CourierName) && (
                              <span className="badge bg-info-subtle text-info border border-info-subtle ms-1">
                                <i className="fas fa-motorcycle me-1"></i>
                                {order.courierName || order.CourierName}
                              </span>
                            )}
                          </div>
                        )}

                      {/* Aksiyon Butonları */}
                      <div className="d-grid gap-2">
                        {/* Onayla: Ağırlık Raporları panelinde YOK (admin sipariş ekranından) */}
                        {!weightOnly &&
                          (() => {
                            const status = normalizeStatus(order.status);
                            return (
                              status === "new" ||
                              status === "pending" ||
                              status === "paid" ||
                              status === "preauthorized"
                            );
                          })() && (
                            <button
                              className="btn btn-info mobile-action-btn fw-semibold"
                              onClick={() =>
                                updateOrderStatus(order.id, "Confirmed")
                              }
                            >
                              <i className="fas fa-check me-2"></i>
                              Onayla
                            </button>
                          )}

                        {/* Confirmed → Preparing (tartı yok; hazırlama panelinde — weightOnly'de gizli) */}
                        {!weightOnly &&
                          normalizeStatus(order.status) === "confirmed" && (
                          <button
                            className="btn btn-warning mobile-action-btn fw-semibold"
                            onClick={() => handleStartPreparing(order)}
                          >
                            <i className="fas fa-play me-2"></i>
                            Hazırlamaya Başla
                          </button>
                        )}

                        {/* Preparing → tartı + Hazır (tek tartı yolu) */}
                        {normalizeStatus(order.status) === "preparing" && (
                          <button
                            className="btn btn-success mobile-action-btn fw-semibold"
                            onClick={() => handleMarkReady(order)}
                          >
                            <i className="fas fa-check me-2"></i>
                            {weightOnly ? "Tartı Gir" : "Hazır"}
                            {(order.hasWeightBasedItems ||
                              order.HasWeightBasedItems) && (
                              <span className="badge bg-light text-dark ms-2">
                                <i className="fas fa-weight me-1"></i>
                                Tartı
                              </span>
                            )}
                          </button>
                        )}

                        {/* Ready - Kurye Ata (ağırlık paneli Tamamlandı dahil) */}
                        {normalizeStatus(order.status) === "ready" && (
                          <button
                            className="btn btn-primary mobile-action-btn fw-semibold"
                            onClick={() => handleOpenCourierModal(order.id)}
                          >
                            <i className="fas fa-motorcycle me-2"></i>
                            Kurye Ata
                          </button>
                        )}

                        {/* Assigned: weightOnly'de sadece bilgi; teslim akışı kurye/admin'de */}
                        {normalizeStatus(order.status) === "assigned" &&
                          weightOnly && (
                            <div className="alert alert-success py-2 mb-0 small">
                              <i className="fas fa-user-check me-1"></i>
                              Kuryeye atandı
                              {(order.courierName || order.CourierName) &&
                                `: ${order.courierName || order.CourierName}`}
                              . Admin ve müşteri panellerinde de aynı durum
                              görünür.
                            </div>
                          )}

                        {/* Assigned/PickedUp → Yolda (ağırlık panelinde yok) */}
                        {!weightOnly &&
                          ["assigned", "picked_up"].includes(
                            normalizeStatus(order.status),
                          ) && (
                          <button
                            className="btn btn-primary mobile-action-btn fw-semibold"
                            onClick={() =>
                              updateOrderStatus(order.id, "out_for_delivery")
                            }
                          >
                            <i className="fas fa-motorcycle me-2"></i>
                            Yola Çıktı
                          </button>
                        )}

                        {/* Yolda → Teslim */}
                        {!weightOnly &&
                          normalizeStatus(order.status) ===
                          "out_for_delivery" && (
                          <button
                            className="btn btn-dark mobile-action-btn fw-semibold"
                            onClick={() =>
                              updateOrderStatus(order.id, "delivered")
                            }
                          >
                            <i className="fas fa-check-double me-2"></i>
                            Teslim Edildi
                          </button>
                        )}

                        {/* İptal */}
                        {!weightOnly &&
                          !["delivered", "cancelled"].includes(
                          normalizeStatus(order.status),
                        ) && (
                          <button
                            className="btn btn-outline-danger mobile-action-btn fw-semibold"
                            onClick={() => updateOrderStatus(order.id, "cancelled")}
                          >
                            <i className="fas fa-times me-2"></i>
                            İptal
                          </button>
                        )}

                        {/* Sil */}
                        {!weightOnly && (
                        <button
                          className="btn btn-outline-dark mobile-action-btn fw-semibold"
                          onClick={() => deleteOrder(order.id)}
                        >
                          <i className="fas fa-trash me-2"></i>
                          Sil
                        </button>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Sayfalama */}
          {totalPages > 1 && (
            <div className="d-flex flex-wrap align-items-center justify-content-between mt-4">
              <div className="text-muted small">
                Toplam {totalCount} sipariş • Sayfa {page}/{totalPages}
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
                    onClick={() =>
                      setPage((p) => Math.min(totalPages, p + 1))
                    }
                    disabled={page >= totalPages}
                  >
                    ›
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* ================================================================ */}
        {/* MOBİL BOTTOM NAVİGATİON */}
        {/* ================================================================ */}
        {!weightOnly && (
        <nav className="mobile-bottom-nav d-md-none">
          <div className="d-flex">
            <button
              className={`bottom-nav-item ${activeTab === "confirmed" ? "active" : ""}`}
              onClick={() => setActiveTab("confirmed")}
            >
              <i className="fas fa-check"></i>
              <span>Onaylı</span>
              {summary.confirmedCount > 0 && (
                <span
                  className="badge bg-primary"
                  style={{
                    fontSize: "0.6rem",
                    position: "absolute",
                    top: "5px",
                    right: "20%",
                  }}
                >
                  {summary.confirmedCount}
                </span>
              )}
            </button>

            <button
              className={`bottom-nav-item ${activeTab === "preparing" ? "active" : ""}`}
              onClick={() => setActiveTab("preparing")}
            >
              <i className="fas fa-utensils"></i>
              <span>Hazırlanıyor</span>
              {summary.preparingCount > 0 && (
                <span
                  className="badge bg-warning"
                  style={{
                    fontSize: "0.6rem",
                    position: "absolute",
                    top: "5px",
                    right: "20%",
                  }}
                >
                  {summary.preparingCount}
                </span>
              )}
            </button>

            <button
              className={`bottom-nav-item ${activeTab === "ready" ? "active" : ""}`}
              onClick={() => setActiveTab("ready")}
            >
              <i className="fas fa-box"></i>
              <span>Hazır</span>
              {summary.readyCount > 0 && (
                <span
                  className="badge bg-success"
                  style={{
                    fontSize: "0.6rem",
                    position: "absolute",
                    top: "5px",
                    right: "20%",
                  }}
                >
                  {summary.readyCount}
                </span>
              )}
            </button>

            <button className="bottom-nav-item" onClick={handleLogout}>
              <i className="fas fa-sign-out-alt"></i>
              <span>Çıkış</span>
            </button>
          </div>
        </nav>
        )}

        {/* ================================================================ */}
        {/* TARTI MODAL */}
        {/* ================================================================ */}
        {showWeightModal && selectedOrder && (
          <div
            className="modal fade show d-block"
            style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
            onClick={() => {
              if (weightLoading) return;
              setShowWeightModal(false);
              setSelectedOrder(null);
              setWeightItems([]);
            }}
          >
            <div
              className="modal-dialog modal-dialog-centered"
              onClick={(e) => e.stopPropagation()}
            >
              <div className="modal-content" style={{ borderRadius: "20px" }}>
                <div className="modal-header border-0 pb-0">
                  <h5 className="modal-title fw-bold">
                    <i className="fas fa-weight text-success me-2"></i>
                    Ağırlık Düzenleme
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => {
                      setShowWeightModal(false);
                      setSelectedOrder(null);
                      setWeightItems([]);
                    }}
                  ></button>
                </div>
                <div className="modal-body">
                  <p className="text-muted mb-3">
                    <strong>#{selectedOrder.orderNumber}</strong> siparişindeki{" "}
                    <strong>tartılı (kg)</strong> ürünler aşağıda. Müşterinin
                    sipariş ettiği miktarı görün; gerçek tartıyı <strong>gram</strong>{" "}
                    olarak girin.
                  </p>
                  <div className="d-flex flex-column gap-3">
                    {weightItems.map((item, index) => (
                      <div
                        key={item.id}
                        className="border rounded-4 p-3"
                        style={{ backgroundColor: "#f8f9fa" }}
                      >
                        <div className="d-flex justify-content-between align-items-start gap-3 mb-2">
                          <div>
                            <div className="fw-semibold d-flex align-items-center gap-2">
                              <i className="fas fa-weight text-success"></i>
                              {item.productName}
                            </div>
                            <div className="small mt-1">
                              <span className="badge bg-success-subtle text-success border border-success-subtle me-1">
                                Tartılı ürün
                              </span>
                              <span className="text-muted">Kalem #{index + 1}</span>
                            </div>
                            <div className="mt-2 small">
                              <div>
                                <strong>Sipariş:</strong>{" "}
                                {(Number(item.estimatedWeight) / 1000).toFixed(2)} kg
                                {" "}
                                <span className="text-muted">
                                  ({Math.round(Number(item.estimatedWeight))} gr)
                                </span>
                              </div>
                              {Number(item.unitPrice) > 0 && (
                                <div>
                                  <strong>Birim fiyat:</strong>{" "}
                                  {Number(item.unitPrice).toFixed(2)} ₺/kg
                                </div>
                              )}
                              <div>
                                <strong>Tahmini tutar:</strong>{" "}
                                {Number(item.estimatedPrice || 0).toFixed(2)} ₺
                              </div>
                            </div>
                          </div>
                        </div>

                        <label className="form-label small mb-1">
                          Gerçek tartı (gram)
                        </label>
                        <div className="input-group">
                          <input
                            type="number"
                            step="1"
                            min="1"
                            className="form-control"
                            placeholder={String(Math.round(Number(item.estimatedWeight) || 0))}
                            value={item.actualWeight}
                            onChange={(e) =>
                              setWeightItems((prev) =>
                                prev.map((current) =>
                                  current.id === item.id
                                    ? {
                                        ...current,
                                        actualWeight: e.target.value,
                                      }
                                    : current,
                                ),
                              )
                            }
                          />
                          <span className="input-group-text">gr</span>
                        </div>

                        <small className="text-muted d-block mt-2">
                          Yeni giriş: {(Number(item.actualWeight || 0) / 1000).toFixed(2)} kg
                          {Number(item.unitPrice) > 0 && Number(item.actualWeight) > 0
                            ? ` · Yeni tutar ≈ ${(
                                (Number(item.actualWeight) / 1000) *
                                Number(item.unitPrice)
                              ).toFixed(2)} ₺`
                            : ""}
                        </small>
                      </div>
                    ))}
                  </div>
                </div>
                <div className="modal-footer border-0 pt-0">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={() => {
                      setShowWeightModal(false);
                      setSelectedOrder(null);
                      setWeightItems([]);
                    }}
                    disabled={weightLoading}
                  >
                    İptal
                  </button>
                  <button
                    type="button"
                    className="btn btn-success px-4"
                    onClick={handleWeightSubmit}
                    disabled={weightLoading || weightItems.length === 0}
                    style={{ borderRadius: "10px" }}
                  >
                    {weightLoading ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2"></span>
                        Kaydediliyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-check me-2"></i>
                        Siparişi Güncelle ve Hazır İşaretle
                      </>
                    )}
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* ================================================================ */}
        {/* KURYE ATAMA MODAL */}
        {/* ================================================================ */}
        {showCourierModal && (
          <div
            className="modal fade show d-block"
            style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
          >
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content" style={{ borderRadius: "16px" }}>
                <div className="modal-header border-0 pb-0">
                  <h5 className="modal-title">
                    <i className="fas fa-motorcycle me-2 text-primary"></i>
                    Kurye Seç
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => setShowCourierModal(false)}
                    disabled={assigningCourier}
                  ></button>
                </div>
                <div className="modal-body">
                  {couriers.length === 0 ? (
                    <div className="text-center text-muted py-4">
                      <i className="fas fa-user-slash fa-2x mb-2"></i>
                      <p>Aktif kurye bulunamadı</p>
                    </div>
                  ) : (
                    <div className="list-group">
                      {couriers.map((courier) => (
                        <button
                          key={courier.id}
                          type="button"
                          className="list-group-item list-group-item-action d-flex justify-content-between align-items-center"
                          onClick={() => handleAssignCourier(courier.id)}
                          disabled={assigningCourier}
                        >
                          <div>
                            <i className="fas fa-user me-2 text-primary"></i>
                            <strong>{courier.name || courier.fullName}</strong>
                            {courier.phone && (
                              <small className="text-muted ms-2">
                                {courier.phone}
                              </small>
                            )}
                          </div>
                          <span className="badge bg-success">
                            <i
                              className="fas fa-circle me-1"
                              style={{ fontSize: "0.5rem" }}
                            ></i>
                            Aktif
                          </span>
                        </button>
                      ))}
                    </div>
                  )}
                </div>
                <div className="modal-footer border-0 pt-0">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={() => setShowCourierModal(false)}
                    disabled={assigningCourier}
                  >
                    İptal
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </>
  );
}
