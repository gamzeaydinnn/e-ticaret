import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useNavigate } from "react-router-dom";
import { useCourierAuth } from "../../contexts/CourierAuthContext";
import WeightApprovalWarningModal from "../../components/WeightApprovalWarningModal";
import { CourierService, formatPhoneDisplay, formatPhoneReadable, getPhoneTelUri } from "../../services/courierService";
import { WeightAdjustmentService } from "../../services/weightAdjustmentService";
import "./CourierOrders.css";

export default function CourierOrders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [updating, setUpdating] = useState(false);
  const [weightReports, setWeightReports] = useState({});
  const [showWeightModal, setShowWeightModal] = useState(false);
  const [pendingDeliveryOrder, setPendingDeliveryOrder] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);
  // FİLTRE STATE'LERİ (sunucu taraflı filtreleme - backend OrderDate'e göre süzer)
  // statusFilter boş ise backend varsayılan olarak yalnızca aktif siparişleri döner;
  // bir durum seçilirse (ör. delivered) geçmiş siparişler de görüntülenebilir.
  const [statusFilter, setStatusFilter] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  // Polling/SignalR çağrıları güncel filtre değerlerini okuyabilsin diye ref kullanıyoruz.
  // NEDEN ref: setInterval closure'ı kurulduğu anki state'i yakalar; ref ile her zaman
  //   en güncel filtreye erişiriz ve otomatik yenileme filtreyi sıfırlamaz.
  const filtersRef = useRef({ status: "", fromDate: "", toDate: "" });
  const navigate = useNavigate();
  const { courier, isAuthenticated, loading: authLoading } = useCourierAuth();

  useEffect(() => {
    // Auth yüklenene kadar bekle
    if (authLoading) return;

    // Kurye girişi yapılmamışsa login'e yönlendir
    if (!isAuthenticated || !courier?.id) {
      navigate("/courier/login");
      return;
    }

    // Kurye giriş yapmış, siparişleri yükle
    loadOrders();

    // 15 saniyede bir otomatik yenileme - yeni atamalar ve durum değişiklikleri için
    const interval = setInterval(() => {
      loadOrders();
    }, 15000);

    // SignalR üzerinden gelen anlık bildirimlerde siparişleri yenile
    const handleTaskAssigned = () => loadOrders();
    const handleTaskUpdated = () => loadOrders();
    const handleTaskCancelled = () => loadOrders();

    window.addEventListener("courierTaskAssigned", handleTaskAssigned);
    window.addEventListener("courierTaskUpdated", handleTaskUpdated);
    window.addEventListener("courierTaskCancelled", handleTaskCancelled);

    return () => {
      clearInterval(interval);
      window.removeEventListener("courierTaskAssigned", handleTaskAssigned);
      window.removeEventListener("courierTaskUpdated", handleTaskUpdated);
      window.removeEventListener("courierTaskCancelled", handleTaskCancelled);
    };
  }, [navigate, authLoading, isAuthenticated, courier?.id]);

  // Filtre değiştiğinde ref'i güncelle ve listeyi yeniden yükle.
  // İlk mount'taki yükleme ana useEffect içinde yapıldığından burada
  // sadece filtre değişimlerinde tetiklenir (auth hazırsa).
  useEffect(() => {
    filtersRef.current = {
      status: statusFilter,
      fromDate: dateFrom,
      toDate: dateTo,
    };
    if (authLoading || !isAuthenticated || !courier?.id) return;
    loadOrders();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter, dateFrom, dateTo]);

  const loadOrders = async () => {
    try {
      console.log("🔍 [CourierOrders] Kurye bilgisi:", courier);
      console.log("🔍 [CourierOrders] Kurye ID:", courier?.id);

      const { status, fromDate, toDate } = filtersRef.current;
      const response = await CourierService.getAssignedOrders({
        status: status || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
      });
      console.log("🔍 [CourierOrders] API yanıtı:", response);

      const { orders: orderData = [] } = response || {};
      console.log("🔍 [CourierOrders] Gelen siparişler:", orderData);

      setOrders(orderData);

      // Her sipariş için ağırlık raporlarını yükle
      const reportsMap = {};
      for (const order of orderData) {
        try {
          const reports = await CourierService.getOrderWeightReports(order.id);
          if (reports && reports.length > 0) {
            reportsMap[order.id] = reports[0]; // İlk raporu al
          }
        } catch (error) {
          console.error(
            `Sipariş ${order.id} için ağırlık raporu yüklenemedi:`,
            error,
          );
        }
      }
      setWeightReports(reportsMap);
    } catch (error) {
      // Auth hatası ise sessizce geç (kullanıcı zaten login'e yönlendiriliyor)
      if (error?.isCourierAuthError) return;
      console.error("Sipariş yükleme hatası:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleDeliveryAttempt = (order) => {
    const report = weightReports[order.id];

    // Ağırlık raporu varsa ve onay bekleniyorsa uyarı göster
    if (report && report.status === "Pending") {
      setPendingDeliveryOrder(order);
      setShowWeightModal(true);
      return;
    }

    // Onaylı rapor varsa veya rapor yoksa modal ile bilgilendirip onayla
    setPendingDeliveryOrder(order);
    setShowWeightModal(true);
  };

  const confirmDelivery = async () => {
    if (!pendingDeliveryOrder) return;

    setUpdating(true);
    setShowWeightModal(false);

    try {
      const orderId = pendingDeliveryOrder.id;

      // Backend'e teslimat isteği gönder - ödeme otomatik yapılacak
      const response = await CourierService.updateOrderStatus(
        orderId,
        "delivered",
      );

      if (response.success) {
        // Başarılı yanıt
        let message = "✅ Teslimat Başarıyla Tamamlandı!\n\n";
        message += `Sipariş: #${orderId}\n`;
        message += `Müşteri: ${pendingDeliveryOrder.customerName}\n`;
        message += `Tutar: ${pendingDeliveryOrder.totalAmount.toFixed(2)} ₺\n`;

        if (response.paymentProcessed && response.paymentAmount > 0) {
          message += `\n💰 EK ÖDEME TAHSİLATI:\n`;
          message += `Ağırlık Farkı Ücreti: +${response.paymentAmount.toFixed(
            2,
          )} ₺\n`;
          message += `\n📊 Toplam Tahsilat: ${(
            parseFloat(pendingDeliveryOrder.totalAmount) +
            parseFloat(response.paymentAmount)
          ).toFixed(2)} ₺`;

          if (response.paymentDetails && response.paymentDetails.length > 0) {
            message += `\n\nDetaylar:\n${response.paymentDetails.join("\n")}`;
          }
        }

        message += response.message ? `\n\n${response.message}` : "";

        alert(message);
      } else {
        alert(
          `⚠️ Uyarı!\n\n${
            response.message ||
            "Teslimat tamamlandı ancak bazı ödemeler başarısız oldu."
          }`,
        );
      }

      // Siparişleri yeniden yükle
      await loadOrders();

      setSelectedOrder(null);
      setPendingDeliveryOrder(null);
    } catch (error) {
      console.error("Teslimat hatası:", error);
      alert(
        `❌ Teslimat Hatası!\n\n${
          error.response?.data?.message ||
          error.message ||
          "Bilinmeyen bir hata oluştu"
        }`,
      );
    } finally {
      setUpdating(false);
    }
  };

  // Nakit tahsilat onayı - DeliveryPaymentPending durumunda kullanılır
  const handleCashSettlement = async (order) => {
    const priceDiff = order.totalPriceDifference || 0;
    
    if (!window.confirm(
      `💰 NAKİT TAHSİLAT ONAYI\n\n` +
      `Sipariş: #${order.id}\n` +
      `Müşteri: ${order.customerName}\n` +
      `Tahsil edilecek ek tutar: ${priceDiff.toFixed(2)} ₺\n\n` +
      `Bu tutarı müşteriden nakit olarak tahsil ettiniz mi?`
    )) {
      return;
    }

    setUpdating(true);
    try {
      const result = await WeightAdjustmentService.calculateCashSettlement(order.id, {
        collectedAmount: priceDiff,
        notes: "Kurye tarafından nakit tahsil edildi",
      });

      if (result.success) {
        alert(
          `✅ Tahsilat Kaydedildi!\n\n` +
          `Sipariş: #${order.id}\n` +
          `Tahsil edilen tutar: ${priceDiff.toFixed(2)} ₺\n\n` +
          `Sipariş tamamlandı.`
        );
        await loadOrders();
      } else {
        alert(`⚠️ Hata: ${result.message || "Tahsilat kaydedilemedi"}`);
      }
    } catch (error) {
      console.error("Nakit tahsilat hatası:", error);
      alert(
        `❌ Tahsilat Hatası!\n\n${
          error.response?.data?.message || error.message || "Bilinmeyen hata"
        }`
      );
    } finally {
      setUpdating(false);
    }
  };

  const updateOrderStatus = async (orderId, newStatus, notes = "") => {
    // Teslim durumu için özel kontrol
    if (newStatus === "delivered") {
      const order = orders.find((o) => o.id === orderId);
      if (order) {
        handleDeliveryAttempt(order);
        return;
      }
    }

    // Diğer durum güncellemeleri normal devam eder
    setUpdating(true);
    try {
      await CourierService.updateOrderStatus(orderId, newStatus, notes);
      await loadOrders();

      setSelectedOrder(null);
    } catch (error) {
      console.error("Durum güncelleme hatası:", error);
      const errorMessage =
        error.response?.data?.message ||
        error.response?.data?.Message ||
        error.message ||
        "Bilinmeyen bir hata oluştu";
      alert(`Durum güncellenemedi: ${errorMessage}`);
    } finally {
      setUpdating(false);
    }
  };

  const getStatusText = (status, fallbackText) => {
    if (fallbackText) return fallbackText;
    const normalized = (status || "").toLowerCase();
    const statusMap = {
      preparing: "Hazırlanıyor",
      ready: "Teslim Alınmaya Hazır",
      assigned: "Size Atandı",
      out_for_delivery: "Yolda",
      outfordelivery: "Yolda",
      in_transit: "Yolda",
      picked_up: "Teslim Alındı",
      delivered: "Teslim Edildi",
      delivery_failed: "Başarısız",
      deliveryfailed: "Başarısız",
      deliverypaymentpending: "Ek Ödeme Bekliyor",
      delivery_payment_pending: "Ek Ödeme Bekliyor",
    };
    return statusMap[normalized] || status;
  };

  const openOrderDetail = async (order) => {
    setDetailLoading(true);
    try {
      const detail = await CourierService.getTaskDetail(order.id);
      setSelectedOrder({
        ...order,
        ...detail,
        address: detail?.deliveryAddress || order.address,
        orderTime: detail?.orderDate || order.orderTime,
        orderTotal: detail?.orderTotal ?? order.totalAmount,
        totalAmount: detail?.orderTotal ?? order.totalAmount,
        finalAmount:
          detail?.finalAmount ??
          detail?.orderTotal ??
          order.finalAmount ??
          order.totalAmount,
        totalPriceDifference:
          detail?.totalPriceDifference ?? order.totalPriceDifference ?? 0,
        shippingCost: detail?.shippingCost ?? order.shippingCost ?? 0,
        paymentMethod: detail?.paymentMethod || order.paymentMethod,
        items: detail?.items || [],
      });
    } catch (error) {
      console.error("Sipariş detayları yüklenemedi:", error);
      setSelectedOrder(order);
    } finally {
      setDetailLoading(false);
    }
  };

  const formatItemWeightDiff = (item) => {
    const grams = Number(item?.weightDifferenceGrams);
    if (!Number.isFinite(grams) || grams === 0) return null;
    return {
      label: grams > 0 ? "Fazlalık" : "Eksik",
      gramsText: `${grams > 0 ? "+" : ""}${grams}g`,
      amount: Number(item?.weightDifferenceAmount) || 0,
      tone: grams > 0 ? "warning" : "info",
    };
  };

  const getOrderBreakdown = (order) => {
    const items = order?.items || [];
    const itemsSubtotal = items.reduce(
      (sum, item) => sum + Number(item.totalPrice || 0),
      0,
    );
    return {
      itemsSubtotal,
      shippingCost: Number(order?.shippingCost || 0),
      weightDiff: Number(order?.totalPriceDifference || 0),
      orderTotal: Number(order?.orderTotal || order?.totalAmount || 0),
      finalAmount: Number(
        order?.finalAmount || order?.orderTotal || order?.totalAmount || 0,
      ),
    };
  };
  const getStatusColor = (status, fallbackColor) => {
    if (fallbackColor) return fallbackColor;
    const normalized = (status || "").toLowerCase();
    const colorMap = {
      preparing: "warning",
      ready: "info",
      assigned: "warning", // 🟡 Sarı - Atandı
      picked_up: "info", // 🔵 Açık mavi - Teslim Alındı
      pickedup: "info",
      out_for_delivery: "primary", // 🔵 Mavi - Yolda
      outfordelivery: "primary",
      in_transit: "primary",
      delivered: "success", // 🟢 Yeşil - Teslim Edildi
      delivery_failed: "danger",
      deliveryfailed: "danger",
      deliverypaymentpending: "danger", // 🔴 Kırmızı - Ek ödeme gerekli
      delivery_payment_pending: "danger",
    };
    return colorMap[normalized] || "secondary";
  };

  // DeliveryPaymentPending durumu kontrolü
  const isDeliveryPaymentPending = (status) => {
    const normalized = (status || "").toLowerCase();
    return normalized === "deliverypaymentpending" || normalized === "delivery_payment_pending";
  };

  // =========================================================================
  // DURUM AKIŞI - Kurye için sipariş durumu geçişleri
  // Assigned → PickedUp → OutForDelivery → Delivered
  // =========================================================================
  const getNextStatus = (currentStatus) => {
    const normalized = (currentStatus || "").toLowerCase();
    const statusFlow = {
      preparing: null, // Hazırlanıyor - kurye henüz işlem yapamaz
      ready: null, // Hazır - kurye henüz atanmamış
      assigned: "picked_up", // ✅ Atandı → Teslim Aldım
      picked_up: "out_for_delivery", // ✅ Teslim Alındı → Yola Çık
      pickedup: "out_for_delivery",
      out_for_delivery: "delivered", // ✅ Yolda → Teslim Et
      outfordelivery: "delivered",
      in_transit: "delivered",
      delivered: null, // Tamamlandı - işlem yok
    };
    return statusFlow[normalized];
  };

  // Sonraki durum için buton metni
  const getNextStatusText = (currentStatus) => {
    const nextStatus = getNextStatus(currentStatus);
    const actionMap = {
      picked_up: "📦 Teslim Aldım", // Assigned durumunda gösterilir
      out_for_delivery: "🏍️ Yola Çıktım", // PickedUp durumunda gösterilir
      delivered: "✅ Teslim Et", // OutForDelivery durumunda gösterilir
    };
    return actionMap[nextStatus];
  };

  // Sonraki durum için buton ikonu
  const getNextStatusIcon = (currentStatus) => {
    const nextStatus = getNextStatus(currentStatus);
    const iconMap = {
      picked_up: "fa-hand-holding-box",
      out_for_delivery: "fa-motorcycle",
      delivered: "fa-check-circle",
    };
    return iconMap[nextStatus] || "fa-arrow-right";
  };

  // Sonraki durum için buton rengi
  const getNextStatusButtonClass = (currentStatus) => {
    const nextStatus = getNextStatus(currentStatus);
    const colorMap = {
      picked_up: "btn-info", // Mavi - Teslim Al
      out_for_delivery: "btn-primary", // Koyu mavi - Yola Çık
      delivered: "btn-success", // Yeşil - Teslim Et
    };
    return colorMap[nextStatus] || "btn-primary";
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center min-vh-100">
        <div className="spinner-border text-primary"></div>
      </div>
    );
  }

  return (
    <div className="min-vh-100 bg-light">
      {/* Header */}
      <nav
        className="navbar navbar-expand-lg navbar-dark"
        style={{ background: "linear-gradient(135deg, #ff6b35, #ff8c00)" }}
      >
        <div className="container-fluid">
          <button
            onClick={() => navigate("/courier/dashboard")}
            className="btn btn-link text-white text-decoration-none"
          >
            <i className="fas fa-arrow-left me-2"></i>
            Dashboard
          </button>
          <span className="navbar-brand mb-0">Siparişlerim</span>
        </div>
      </nav>

      <div className="container-fluid px-2 px-md-4 py-3">
        {/* Filtreler */}
        <div className="card border-0 shadow-sm mb-3">
          <div className="card-body p-2 p-md-3">
            <div className="d-flex flex-column gap-2">
              <div className="d-flex justify-content-between align-items-center">
                <h6 className="fw-bold mb-0">
                  <i className="fas fa-list-alt me-2 text-primary"></i>
                  Siparişler ({orders.length})
                </h6>
                {(statusFilter || dateFrom || dateTo) && (
                  <button
                    className="btn btn-sm btn-outline-secondary"
                    onClick={() => {
                      setStatusFilter("");
                      setDateFrom("");
                      setDateTo("");
                    }}
                  >
                    <i className="fas fa-times"></i>
                  </button>
                )}
              </div>
              <div className="d-flex flex-wrap gap-2">
                <select
                  className="form-select form-select-sm"
                  style={{ flex: "1", minWidth: "130px" }}
                  value={statusFilter}
                  onChange={(e) => setStatusFilter(e.target.value)}
                >
                  <option value="">Aktif Siparişler</option>
                  <option value="Assigned">Atandı</option>
                  <option value="PickedUp">Teslim Alındı</option>
                  <option value="OutForDelivery">Yolda</option>
                  <option value="Delivered">Teslim Edildi</option>
                  <option value="DeliveryFailed">Başarısız</option>
                </select>
                <input
                  type="date"
                  className="form-control form-control-sm"
                  style={{ width: "130px" }}
                  value={dateFrom}
                  placeholder="gg.aa.yyyy"
                  onChange={(e) => setDateFrom(e.target.value)}
                />
                <input
                  type="date"
                  className="form-control form-control-sm"
                  style={{ width: "130px" }}
                  value={dateTo}
                  placeholder="gg.aa.yyyy"
                  onChange={(e) => setDateTo(e.target.value)}
                />
              </div>
            </div>
          </div>
        </div>

        {/* Sipariş Listesi - Kart Görünümü */}
        {orders.length === 0 ? (
          <div className="text-center py-5">
            <i className="fas fa-inbox fs-1 text-muted mb-3 d-block"></i>
            <p className="text-muted">Henüz atanmış siparişiniz bulunmuyor.</p>
          </div>
        ) : (
          <div className="d-flex flex-column gap-2">
            {orders.map((order) => {
              const weightReport = weightReports[order.id];
              const hasPendingWeight = weightReport?.status === "Pending";
              const hasApprovedWeight = weightReport?.status === "Approved";
              const isPaymentPending = isDeliveryPaymentPending(order.status);
              const hasWeightDiff = order.hasWeightDifference || order.totalPriceDifference > 0;
              const finalAmount = order.finalAmount || order.totalAmount;
              const priceDiff = order.totalPriceDifference || 0;

              return (
                <div
                  key={order.id}
                  className={`card border-0 shadow-sm ${
                    isPaymentPending
                      ? "border-start border-danger border-4"
                      : hasPendingWeight
                        ? "border-start border-warning border-4"
                        : hasApprovedWeight
                          ? "border-start border-success border-4"
                          : ""
                  }`}
                >
                  <div className="card-body p-3">
                    {/* Üst Satır: Sipariş No, Durum, Tutar */}
                    <div className="d-flex justify-content-between align-items-start mb-2">
                      <div>
                        <span className="fw-bold text-primary">#{order.id}</span>
                        <span
                          className={`badge bg-${getStatusColor(order.status, order.statusColor)} ms-2`}
                          style={{ fontSize: "0.7rem" }}
                        >
                          {getStatusText(order.status, order.statusText)}
                        </span>
                      </div>
                      <div className="text-end">
                        <div className="fw-bold" style={{ color: "#ff6b35", fontSize: "1.1rem" }}>
                          {Number(finalAmount || 0).toFixed(2)} ₺
                        </div>
                        {priceDiff !== 0 && (
                          <small className={priceDiff > 0 ? "text-warning fw-bold" : "text-info"}>
                            {priceDiff > 0 ? "+" : ""}{priceDiff.toFixed(2)} ₺
                          </small>
                        )}
                      </div>
                    </div>

                    {/* Müşteri & Adres */}
                    <div className="mb-2">
                      <div className="d-flex align-items-center gap-2 mb-1">
                        <i className="fas fa-user text-muted" style={{ width: "16px" }}></i>
                        <span className="fw-semibold">{order.customerName}</span>
                        {order.customerPhone && (
                          <a href={getPhoneTelUri(order.customerPhone)} className="text-success ms-auto">
                            <i className="fas fa-phone-alt"></i>
                          </a>
                        )}
                      </div>
                      <div className="d-flex align-items-start gap-2">
                        <i className="fas fa-map-marker-alt text-muted mt-1" style={{ width: "16px" }}></i>
                        <small className="text-muted" style={{ lineHeight: "1.3" }}>
                          {(order.address || "-").length > 60
                            ? (order.address || "-").substring(0, 60) + "..."
                            : order.address || "-"}
                        </small>
                      </div>
                    </div>

                    {/* Uyarı Badges */}
                    {(hasPendingWeight || hasApprovedWeight || isPaymentPending) && (
                      <div className="mb-2">
                        {hasPendingWeight && (
                          <span className="badge bg-warning text-dark me-1">
                            <i className="fas fa-clock me-1"></i>Onay Bekliyor
                          </span>
                        )}
                        {hasApprovedWeight && (
                          <span className="badge bg-success me-1">
                            <i className="fas fa-check me-1"></i>+{weightReport.overageGrams}g
                          </span>
                        )}
                        {isPaymentPending && (
                          <span className="badge bg-danger">
                            <i className="fas fa-exclamation-triangle me-1"></i>Ek Tahsilat
                          </span>
                        )}
                      </div>
                    )}

                    {/* Aksiyon Butonları */}
                    <div className="d-flex flex-wrap gap-2 mt-2">
                      {/* Detay */}
                      <button
                        onClick={() => openOrderDetail(order)}
                        className="btn btn-sm btn-outline-primary"
                        disabled={detailLoading}
                        style={{ flex: "0 0 auto" }}
                      >
                        <i className="fas fa-info-circle me-1"></i>Detay
                      </button>

                      {/* Harita */}
                      {order.address && (
                        <button
                          onClick={() => {
                            window.open(
                              `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(order.address)}`,
                              "_blank"
                            );
                          }}
                          className="btn btn-sm btn-outline-secondary"
                          style={{ flex: "0 0 auto" }}
                        >
                          <i className="fas fa-map-marker-alt me-1"></i>Harita
                        </button>
                      )}

                      {/* Ana Aksiyon Butonu */}
                      {isPaymentPending && priceDiff > 0 ? (
                        <button
                          onClick={() => handleCashSettlement(order)}
                          disabled={updating}
                          className="btn btn-sm btn-danger flex-grow-1"
                        >
                          <i className="fas fa-hand-holding-usd me-1"></i>
                          {priceDiff.toFixed(2)} ₺ AL
                        </button>
                      ) : (order.status?.toLowerCase() === "assigned" || order.status?.toLowerCase() === "ready") ? (
                        <button
                          onClick={() => updateOrderStatus(order.id, "picked_up")}
                          disabled={updating}
                          className="btn btn-sm btn-primary flex-grow-1"
                        >
                          <i className="fas fa-box-open me-1"></i>TESLİM AL
                        </button>
                      ) : (order.status?.toLowerCase() === "picked_up" || order.status?.toLowerCase() === "pickedup") ? (
                        <button
                          onClick={() => updateOrderStatus(order.id, "out_for_delivery")}
                          disabled={updating}
                          className="btn btn-sm flex-grow-1"
                          style={{ background: "#ff6b35", color: "white" }}
                        >
                          <i className="fas fa-motorcycle me-1"></i>YOLA ÇIK
                        </button>
                      ) : (order.status?.toLowerCase() === "out_for_delivery" || 
                           order.status?.toLowerCase() === "outfordelivery" ||
                           order.status?.toLowerCase() === "in_transit") && !isPaymentPending ? (
                        <button
                          onClick={() => updateOrderStatus(order.id, "delivered")}
                          disabled={updating || hasPendingWeight}
                          className={`btn btn-sm flex-grow-1 ${hasPendingWeight || hasWeightDiff ? "btn-warning" : "btn-success"}`}
                        >
                          <i className={`fas ${hasPendingWeight ? "fa-clock" : "fa-check-circle"} me-1`}></i>
                          {hasPendingWeight ? "ONAY BEKLİYOR" : "TESLİM ET"}
                        </button>
                      ) : null}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Sipariş Detay Modal - body'ye portal (overflow kesmesini önler) */}
      {selectedOrder &&
        createPortal(
        <div
          className="modal fade show d-block courier-order-modal-root"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.6)" }}
          onClick={(e) => {
            if (e.target === e.currentTarget) setSelectedOrder(null);
          }}
        >
          <div className="modal-dialog courier-order-modal">
            <div className="modal-content courier-order-modal-content">
              {/* Header */}
              <div
                className="modal-header py-2"
                style={{ background: "linear-gradient(135deg, #ff6b35, #ff8c00)" }}
              >
                <h6 className="modal-title text-white mb-0">
                  <i className="fas fa-receipt me-2"></i>
                  Sipariş #{selectedOrder.id}
                </h6>
                <button
                  onClick={() => setSelectedOrder(null)}
                  className="btn-close btn-close-white"
                ></button>
              </div>

              {/* Body */}
              <div className="modal-body p-3 courier-order-modal-body">
                {/* Uyarı Badges */}
                {(() => {
                  const report = weightReports[selectedOrder.id];
                  if (report?.status === "Pending") {
                    return (
                      <div className="alert alert-warning py-2 mb-3">
                        <i className="fas fa-clock me-2"></i>
                        <strong>Admin Onayı Bekleniyor</strong>
                        <div className="small mt-1">
                          Fazlalık: +{report.overageGrams}g | Ek: +{report.overageAmount} ₺
                        </div>
                      </div>
                    );
                  } else if (report?.status === "Approved") {
                    return (
                      <div className="alert alert-success py-2 mb-3">
                        <i className="fas fa-check-circle me-2"></i>
                        <strong>Onaylandı</strong>
                        <div className="small mt-1">
                          Fazlalık: +{report.overageGrams}g | Tahsil: +{report.overageAmount} ₺
                        </div>
                      </div>
                    );
                  }
                  return null;
                })()}

                {/* Müşteri Bilgileri */}
                <div className="card border-0 shadow-sm mb-3 courier-customer-card">
                  <div className="card-body p-3">
                    <h6 className="fw-bold mb-2">
                      <i className="fas fa-user me-2 text-primary"></i>
                      Müşteri Bilgileri
                    </h6>
                    <div className="mb-2">
                      <div className="fw-semibold mb-2">{selectedOrder.customerName}</div>
                      {selectedOrder.customerPhone && (
                        <div className="courier-phone-wrapper">
                          <a
                            href={getPhoneTelUri(selectedOrder.customerPhone)}
                            className="courier-phone-block"
                            title={`Ara: ${formatPhoneReadable(selectedOrder.customerPhone)}`}
                          >
                            {formatPhoneReadable(selectedOrder.customerPhone)}
                          </a>
                        </div>
                      )}
                    </div>
                    <div>
                      <small className="text-muted">Adres</small>
                      <p className="mb-0 small">{selectedOrder.address}</p>
                    </div>
                  </div>
                </div>

                {/* Sipariş Bilgileri */}
                {(() => {
                  const breakdown = getOrderBreakdown(selectedOrder);
                  return (
                <div className="card border-0 shadow-sm mb-3">
                  <div className="card-body p-3">
                    <h6 className="fw-bold mb-2">
                      <i className="fas fa-box me-2 text-primary"></i>
                      Sipariş Bilgileri
                    </h6>
                    <div className="courier-info-row mb-2">
                      <span className="text-muted">Tarih</span>
                      <span className="fw-semibold courier-info-value">
                        {selectedOrder.orderTime
                          ? new Date(selectedOrder.orderTime).toLocaleDateString("tr-TR")
                          : "-"}
                      </span>
                    </div>
                    {selectedOrder.paymentMethod && (
                      <div className="courier-info-row mb-2">
                        <span className="text-muted">Ödeme</span>
                        <span className="fw-semibold courier-info-value">{selectedOrder.paymentMethod}</span>
                      </div>
                    )}
                    {breakdown.itemsSubtotal > 0 && (
                      <div className="courier-info-row mb-2">
                        <span className="text-muted">Ürünler Toplamı</span>
                        <span className="courier-info-value">{breakdown.itemsSubtotal.toFixed(2)} ₺</span>
                      </div>
                    )}
                    {breakdown.shippingCost > 0 && (
                      <div className="courier-info-row mb-2">
                        <span className="text-muted">Kargo</span>
                        <span className="courier-info-value">{breakdown.shippingCost.toFixed(2)} ₺</span>
                      </div>
                    )}
                    {breakdown.weightDiff !== 0 && (
                      <div className="courier-info-row mb-2">
                        <span className="text-muted">Tartı Farkı</span>
                        <span className={`courier-info-value ${breakdown.weightDiff > 0 ? "text-warning fw-bold" : "text-info fw-bold"}`}>
                          {breakdown.weightDiff > 0 ? "+" : ""}
                          {breakdown.weightDiff.toFixed(2)} ₺
                        </span>
                      </div>
                    )}
                    <div className="courier-info-row mb-2 pt-2 border-top">
                      <span className="text-muted fw-semibold">Tahsil Edilecek</span>
                      <span className="fw-bold courier-info-value" style={{ color: "#ff6b35", fontSize: "1.05rem" }}>
                        {breakdown.finalAmount.toFixed(2)} ₺
                      </span>
                    </div>
                    {breakdown.finalAmount !== breakdown.orderTotal && breakdown.orderTotal > 0 && (
                      <div className="courier-info-row mb-2">
                        <span className="text-muted small">Sipariş Tutarı</span>
                        <span className="text-muted small courier-info-value">{breakdown.orderTotal.toFixed(2)} ₺</span>
                      </div>
                    )}
                    <div className="courier-info-row">
                      <span className="text-muted">Durum</span>
                      <span className={`badge bg-${getStatusColor(selectedOrder.status, selectedOrder.statusColor)} courier-info-value`}>
                        {getStatusText(selectedOrder.status, selectedOrder.statusText)}
                      </span>
                    </div>
                  </div>
                </div>
                  );
                })()}

                {/* Ürünler */}
                <div className="card border-0 shadow-sm mb-3">
                  <div className="card-body p-3">
                    <h6 className="fw-bold mb-2">
                      <i className="fas fa-shopping-basket me-2 text-primary"></i>
                      Ürünler
                    </h6>
                    {selectedOrder.items && selectedOrder.items.length > 0 ? (
                      <div className="d-flex flex-column gap-2">
                        {selectedOrder.items.map((item, index) => {
                          const weightDiff = formatItemWeightDiff(item);
                          return (
                            <div key={item.id || index} className="border-bottom pb-2">
                              <div className="d-flex justify-content-between">
                                <span className="fw-semibold">{item.name}</span>
                                <span className="fw-bold">{Number(item.totalPrice || 0).toFixed(2)} ₺</span>
                              </div>
                              <small className="text-muted">
                                {item.quantity}{" "}
                                {(item.weightUnit || "adet").toLowerCase()} ×{" "}
                                {Number(item.price || 0).toFixed(2)} ₺
                              </small>
                              {item.isWeightBased && item.actualWeightGrams != null && (
                                <div className="small text-muted">
                                  Tartı: {item.expectedWeightGrams}g → {item.actualWeightGrams}g
                                </div>
                              )}
                              {weightDiff && (
                                <span className={`badge bg-${weightDiff.tone} mt-1`} style={{ fontSize: "0.7rem" }}>
                                  {weightDiff.label} {weightDiff.gramsText}
                                </span>
                              )}
                            </div>
                          );
                        })}
                      </div>
                    ) : (
                      <small className="text-muted">Ürün listesi yok</small>
                    )}
                  </div>
                </div>
              </div>

              {/* Footer - Aksiyon Butonları */}
              <div className="modal-footer courier-modal-footer p-2">
                <div className="courier-modal-actions">
                  {selectedOrder.address && (
                    <button
                      type="button"
                      onClick={() => {
                        window.open(
                          `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(selectedOrder.address)}`,
                          "_blank"
                        );
                      }}
                      className="btn btn-outline-secondary courier-modal-icon-btn"
                      title="Haritada aç"
                    >
                      <i className="fas fa-map-marker-alt"></i>
                    </button>
                  )}
                  {selectedOrder.customerPhone && (
                    <a
                      href={getPhoneTelUri(selectedOrder.customerPhone)}
                      className="btn btn-success courier-modal-icon-btn"
                      title={`Ara: ${formatPhoneReadable(selectedOrder.customerPhone)}`}
                      aria-label={`Müşteriyi ara: ${formatPhoneReadable(selectedOrder.customerPhone)}`}
                    >
                      <i className="fas fa-phone"></i>
                    </a>
                  )}
                  {getNextStatus(selectedOrder.status) && (
                    <button
                      type="button"
                      onClick={() => {
                        setSelectedOrder(null);
                        updateOrderStatus(selectedOrder.id, getNextStatus(selectedOrder.status));
                      }}
                      disabled={updating || (getNextStatus(selectedOrder.status) === "delivered" && weightReports[selectedOrder.id]?.status === "Pending")}
                      className={`btn courier-modal-main-btn ${
                        getNextStatus(selectedOrder.status) === "delivered"
                          ? "btn-success"
                          : getNextStatus(selectedOrder.status) === "out_for_delivery"
                            ? "btn-warning text-dark"
                            : "btn-primary"
                      }`}
                    >
                      {updating ? (
                        <span className="spinner-border spinner-border-sm"></span>
                      ) : (
                        <>
                          <i className={`fas me-2 ${
                            getNextStatus(selectedOrder.status) === "delivered"
                              ? "fa-check-circle"
                              : getNextStatus(selectedOrder.status) === "out_for_delivery"
                                ? "fa-motorcycle"
                                : "fa-box-open"
                          }`}></i>
                          {getNextStatus(selectedOrder.status) === "picked_up"
                            ? "TESLİM AL"
                            : getNextStatus(selectedOrder.status) === "out_for_delivery"
                              ? "YOLA ÇIK"
                              : "TESLİM ET"}
                        </>
                      )}
                    </button>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>,
        document.body,
      )}

      {/* Ağırlık Onay Uyarı Modal */}
      <WeightApprovalWarningModal
        isOpen={showWeightModal}
        onClose={() => {
          setShowWeightModal(false);
          setPendingDeliveryOrder(null);
        }}
        onConfirm={confirmDelivery}
        orderData={pendingDeliveryOrder}
        weightReport={
          pendingDeliveryOrder ? weightReports[pendingDeliveryOrder.id] : null
        }
      />
    </div>
  );
}
