// ==========================================================================
// OrderDetailModal.jsx - Sipariş Detay Modal Komponenti
// ==========================================================================
// Sipariş detaylarını ve ağırlık fark bilgilerini gösteren modal.
// Real-time güncellemeler için SignalR entegrasyonu.
// Profesyonel tasarım, mobil uyumlu.
// ==========================================================================

import React, { useState, useEffect, useCallback, useRef } from "react";
import "./OrderDetailModal.css";
import signalRService from "../services/signalRService";

/**
 * Durum badge'ini render et
 */
const StatusBadge = ({ status }) => {
  const statusConfig = {
    // Standart Akış Durumları
    new: { label: "Yeni Sipariş", color: "#6c757d", icon: "circle" },
    New: { label: "Yeni Sipariş", color: "#6c757d", icon: "circle" },
    pending: { label: "Beklemede", color: "#ffc107", icon: "clock" },
    Pending: { label: "Beklemede", color: "#ffc107", icon: "clock" },
    confirmed: { label: "Onaylandı", color: "#17a2b8", icon: "check-circle" },
    Confirmed: { label: "Onaylandı", color: "#17a2b8", icon: "check-circle" },
    preparing: { label: "Hazırlanıyor", color: "#fd7e14", icon: "utensils" },
    Preparing: { label: "Hazırlanıyor", color: "#fd7e14", icon: "utensils" },
    Processing: { label: "Hazırlanıyor", color: "#fd7e14", icon: "cog" },
    ready: { label: "Hazır", color: "#28a745", icon: "box" },
    Ready: { label: "Hazır", color: "#28a745", icon: "box" },
    assigned: { label: "Kuryeye Atandı", color: "#0d6efd", icon: "user-check" },
    Assigned: { label: "Kuryeye Atandı", color: "#0d6efd", icon: "user-check" },
    picked_up: {
      label: "Kurye Teslim Aldı",
      color: "#20c997",
      icon: "hand-holding-box",
    },
    PickedUp: {
      label: "Kurye Teslim Aldı",
      color: "#20c997",
      icon: "hand-holding-box",
    },
    out_for_delivery: { label: "Yolda", color: "#6f42c1", icon: "motorcycle" },
    OutForDelivery: { label: "Yolda", color: "#6f42c1", icon: "motorcycle" },
    delivered: {
      label: "Teslim Edildi",
      color: "#28a745",
      icon: "check-double",
    },
    Delivered: {
      label: "Teslim Edildi",
      color: "#28a745",
      icon: "check-double",
    },
    cancelled: { label: "İptal", color: "#dc3545", icon: "times-circle" },
    Cancelled: { label: "İptal", color: "#dc3545", icon: "times-circle" },
    delivery_failed: {
      label: "Teslimat Başarısız",
      color: "#dc3545",
      icon: "exclamation-triangle",
    },
    DeliveryFailed: {
      label: "Teslimat Başarısız",
      color: "#dc3545",
      icon: "exclamation-triangle",
    },

    // Eski Durumlar (Geriye Uyumluluk)
    Shipped: { label: "Kargoda", color: "#6f42c1", icon: "truck" },
    Refunded: { label: "İade Edildi", color: "#6c757d", icon: "undo" },
    WeightPending: {
      label: "Tartım Bekleniyor",
      color: "#ff9800",
      icon: "balance-scale",
    },
    WeightAdjusted: { label: "Tartıldı", color: "#4caf50", icon: "check" },
    AdminReview: {
      label: "Admin İncelemede",
      color: "#9c27b0",
      icon: "user-shield",
    },
  };

  const config = statusConfig[status] || {
    label: status,
    color: "#6c757d",
    icon: "info",
  };

  return (
    <span className="status-badge" style={{ backgroundColor: config.color }}>
      <i className={`fas fa-${config.icon}`}></i>
      {config.label}
    </span>
  );
};

/**
 * Ağırlık Fark Kartı Komponenti
 */
const WeightDifferenceCard = ({ weightAdjustment }) => {
  const [isExpanded, setIsExpanded] = useState(false);

  if (!weightAdjustment) return null;

  const estimatedTotal =
    weightAdjustment.estimatedTotal || weightAdjustment.expectedWeight || 0;
  const actualTotal =
    weightAdjustment.actualTotal || weightAdjustment.reportedWeight || 0;
  const difference =
    weightAdjustment.differenceAmount ||
    weightAdjustment.overageAmount ||
    actualTotal - estimatedTotal;
  const isRefund = difference < 0;

  const getStatusInfo = () => {
    const status = weightAdjustment.status;
    switch (status) {
      case "Completed":
      case "Approved":
        return { label: "Tamamlandı", color: "success", icon: "check-circle" };
      case "Pending":
      case "Weighed":
        return { label: "Beklemede", color: "warning", icon: "clock" };
      case "AdminReviewRequired":
        return {
          label: "Admin İncelemede",
          color: "info",
          icon: "user-shield",
        };
      case "RefundRequired":
        return { label: "İade Bekleniyor", color: "primary", icon: "undo" };
      default:
        return { label: "İşleniyor", color: "secondary", icon: "spinner" };
    }
  };

  const statusInfo = getStatusInfo();

  return (
    <div className={`weight-difference-card ${isRefund ? "refund" : "charge"}`}>
      <div className="wdc-header">
        <div className="wdc-icon">
          <i className="fas fa-balance-scale"></i>
        </div>
        <div className="wdc-title">
          <h6>Ağırlık Fark Özeti</h6>
          <span className={`badge bg-${statusInfo.color}`}>
            <i className={`fas fa-${statusInfo.icon} me-1`}></i>
            {statusInfo.label}
          </span>
        </div>
      </div>

      <div className="wdc-summary">
        <div className="summary-row">
          <span className="label">Tahmini Tutar:</span>
          <span className="value">₺{Number(estimatedTotal).toFixed(2)}</span>
        </div>
        <div className="summary-row">
          <span className="label">Gerçek Tutar:</span>
          <span className="value highlight">
            ₺{Number(actualTotal).toFixed(2)}
          </span>
        </div>
        <div className={`summary-row total ${isRefund ? "refund" : "charge"}`}>
          <span className="label">Fark:</span>
          <span className="value">
            {isRefund ? "" : "+"}₺{Math.abs(Number(difference)).toFixed(2)}
          </span>
        </div>
      </div>

      {weightAdjustment.items && weightAdjustment.items.length > 0 && (
        <>
          <button
            className="wdc-expand-btn"
            onClick={() => setIsExpanded(!isExpanded)}
          >
            <span>
              {isExpanded ? "Detayları Gizle" : "Ürün Bazlı Detaylar"}
            </span>
            <i className={`fas fa-chevron-${isExpanded ? "up" : "down"}`}></i>
          </button>

          {isExpanded && (
            <div className="wdc-details">
              {weightAdjustment.items.map((item, index) => {
                const itemDiff =
                  (item.actualPrice || 0) - (item.estimatedPrice || 0);
                return (
                  <div key={index} className="wdc-item">
                    <div className="item-info">
                      <span className="item-name">{item.productName}</span>
                      <span className="item-weights">
                        {item.estimatedQuantity} → {item.actualQuantity}{" "}
                        {item.weightUnit || "g"}
                      </span>
                    </div>
                    <div className="item-prices">
                      <span
                        className={`diff ${itemDiff >= 0 ? "positive" : "negative"}`}
                      >
                        {itemDiff >= 0 ? "+" : ""}₺{itemDiff.toFixed(2)}
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </>
      )}

      <div className="wdc-message">
        {isRefund ? (
          <>
            <i className="fas fa-info-circle text-primary"></i>
            <span>Fark tutarı iade edilecektir.</span>
          </>
        ) : difference > 0 ? (
          <>
            <i className="fas fa-info-circle text-warning"></i>
            <span>Ek tutar ödemenize eklenmiştir.</span>
          </>
        ) : (
          <>
            <i className="fas fa-check-circle text-success"></i>
            <span>Tahmin ve gerçek tutar eşleşiyor.</span>
          </>
        )}
      </div>
    </div>
  );
};

export default function OrderDetailModal({ show, onHide, order, onOrderUpdate }) {
  // Real-time order updates state
  const [localOrder, setLocalOrder] = useState(order);
  const signalRUnsubscribesRef = useRef([]);
  const DEBUG = process.env.NODE_ENV === "development";

  // Order prop değiştiğinde local state'i güncelle
  useEffect(() => {
    setLocalOrder(order);
  }, [order]);

  // SignalR event listener'ları
  useEffect(() => {
    if (!show || !localOrder?.id) return;

    // Önceki listener'ları temizle
    signalRUnsubscribesRef.current.forEach(unsub => {
      if (typeof unsub === 'function') unsub();
    });
    signalRUnsubscribesRef.current = [];

    // Sipariş durumu değişikliği
    const handleOrderStatusChanged = (data) => {
      const matchesId = String(localOrder.id) === String(data.orderId) ||
                       localOrder.orderNumber === data.orderNumber;
      if (matchesId) {
        DEBUG && console.log("[OrderDetailModal] SignalR: Sipariş güncellendi", data);
        setLocalOrder(prev => ({
          ...prev,
          status: data.status || data.newStatus,
          statusText: data.statusText,
          updatedAt: data.timestamp || new Date().toISOString(),
        }));
        // Parent component'e bildir
        if (onOrderUpdate) {
          onOrderUpdate({
            ...localOrder,
            status: data.status || data.newStatus,
            statusText: data.statusText,
          });
        }
      }
    };

    // Ağırlık farkı bildirimi
    const handleWeightChargeApplied = (data) => {
      const matchesId = String(localOrder.id) === String(data.orderId) ||
                       localOrder.orderNumber === data.orderNumber;
      if (matchesId) {
        DEBUG && console.log("[OrderDetailModal] SignalR: Ağırlık farkı uygulandı", data);
        setLocalOrder(prev => ({
          ...prev,
          finalAmount: data.finalAmount,
          weightDifferenceAmount: data.weightDifferenceAmount,
          weightAdjustmentStatus: data.status || 'completed',
        }));
      }
    };

    // Teslimat tamamlandı
    const handleDeliveryCompleted = (data) => {
      const matchesId = String(localOrder.id) === String(data.orderId) ||
                       localOrder.orderNumber === data.orderNumber;
      if (matchesId) {
        DEBUG && console.log("[OrderDetailModal] SignalR: Teslimat tamamlandı", data);
        setLocalOrder(prev => ({
          ...prev,
          status: 'delivered',
          deliveredAt: data.deliveredAt || new Date().toISOString(),
        }));
      }
    };

    // Event listener'ları kaydet
    const unsub1 = signalRService.onOrderStatusChanged(handleOrderStatusChanged);
    const unsub2 = signalRService.onWeightChargeApplied(handleWeightChargeApplied);
    const unsub3 = signalRService.onDeliveryStatusChanged(handleDeliveryCompleted);

    signalRUnsubscribesRef.current = [unsub1, unsub2, unsub3];

    // Cleanup
    return () => {
      signalRUnsubscribesRef.current.forEach(unsub => {
        if (typeof unsub === 'function') unsub();
      });
      signalRUnsubscribesRef.current = [];
    };
  }, [show, localOrder?.id, localOrder?.orderNumber, onOrderUpdate, DEBUG]);

  if (!show || !localOrder) return null;

  const weightAdjustment = localOrder.weightAdjustment || localOrder.weightReport;
  const hasWeightAdjustment = !!weightAdjustment;
  const hasWeightBasedItems = (localOrder.orderItems || []).some(
    (item) => item.isWeightBased || item.weightUnit,
  );

  return (
    <div
      className="order-detail-modal-overlay"
      onClick={(e) => e.target === e.currentTarget && onHide()}
    >
      <div className="order-detail-modal">
        {/* Header */}
        <div className="modal-header-custom">
          <div className="header-content">
            <div className="order-badge">
              <i className="fas fa-receipt"></i>
              <span>#{localOrder.id || localOrder.orderNumber}</span>
            </div>
            <h5>Sipariş Detayları</h5>
            <StatusBadge status={localOrder.status} />
          </div>
          <button className="close-btn" onClick={onHide}>
            <i className="fas fa-times"></i>
          </button>
        </div>

        {/* Body */}
        <div className="modal-body-custom">
          {hasWeightAdjustment && (
            <WeightDifferenceCard weightAdjustment={weightAdjustment} />
          )}

          {hasWeightBasedItems &&
            !hasWeightAdjustment &&
            localOrder.status !== "Delivered" && (
              <div className="weight-pending-notice">
                <i className="fas fa-balance-scale"></i>
                <div>
                  <strong>Tartılı Ürün İçeriyor</strong>
                  <p>Bu siparişte ağırlık bazlı ürünler bulunmaktadır.</p>
                </div>
              </div>
            )}

          {/* Sipariş Bilgileri */}
          <div className="order-info-grid">
            <div className="info-section">
              <h6>
                <i className="fas fa-info-circle"></i> Sipariş Bilgileri
              </h6>
              <div className="info-row">
                <span className="label">Tarih</span>
                <span className="value">
                  {localOrder.orderDate
                    ? new Date(localOrder.orderDate).toLocaleString("tr-TR")
                    : "-"}
                </span>
              </div>
              <div className="info-row">
                <span className="label">Durum</span>
                <span className="value">{localOrder.status}</span>
              </div>
              <div className="info-row">
                <span className="label">Kargo</span>
                <span className="value">
                  {localOrder.shippingMethod}
                  {localOrder.shippingCost ? ` (₺${localOrder.shippingCost})` : ""}
                </span>
              </div>
            </div>

            <div className="info-section">
              <h6>
                <i className="fas fa-map-marker-alt"></i> Teslimat
              </h6>
              <div className="info-row">
                <span className="label">Alıcı</span>
                <span className="value">{localOrder.customerName}</span>
              </div>
              {localOrder.customerPhone && (
                <div className="info-row">
                  <span className="label">Telefon</span>
                  <span className="value">{localOrder.customerPhone}</span>
                </div>
              )}
              <div className="info-row">
                <span className="label">Adres</span>
                <span className="value address">
                  {localOrder.deliveryAddress ||
                    localOrder.shippingAddress ||
                    localOrder.address ||
                    localOrder.fullAddress ||
                    localOrder.addressSummary ||
                    "-"}
                </span>
              </div>
            </div>
          </div>

          {/* Ürünler */}
          <div className="products-section">
            <h6>
              <i className="fas fa-box"></i> Ürünler
            </h6>
            <div className="products-list">
              {(localOrder.orderItems || []).map((item, idx) => (
                <div
                  key={idx}
                  className={`product-item ${item.isWeightBased ? "weight-based" : ""}`}
                >
                  <div className="product-info">
                    <span className="product-name">{item.productName}</span>
                    <div className="product-meta">
                      <span className="quantity">x{item.quantity}</span>
                      {item.isWeightBased && (
                        <span className="weight-badge-sm">
                          <i className="fas fa-balance-scale"></i>
                          Tartılı
                        </span>
                      )}
                    </div>
                  </div>
                  <span className="product-price">₺{item.unitPrice}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Özet */}
          <div className="order-summary">
            <div className="summary-row">
              <span>Toplam</span>
              <span className="total-price">₺{localOrder.totalAmount}</span>
            </div>
          </div>

          {localOrder.deliveryNotes && (
            <div className="delivery-notes">
              <h6>
                <i className="fas fa-sticky-note"></i> Not
              </h6>
              <p>{localOrder.deliveryNotes}</p>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="modal-footer-custom">
          <button className="btn-close-modal" onClick={onHide}>
            Kapat
          </button>
        </div>
      </div>
    </div>
  );
}
