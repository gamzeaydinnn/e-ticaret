// ==========================================================================
// ReadyOrderCard.jsx - Hazır Sipariş Kartı Komponenti
// ==========================================================================
// Dispatcher panelinde her bir siparişi gösteren kart.
// Kurye atama ve değiştirme işlemleri bu kart üzerinden yapılır.
// NEDEN: Sipariş bilgilerini kompakt ve anlaşılır şekilde göstermek için.
// ==========================================================================

import React, { useState } from "react";
import CourierSelect from "./CourierSelect";

export default function ReadyOrderCard({
  order,
  couriers = [],
  onAssignCourier,
  onReassignCourier,
  showAssignButton = true,
  showReassignButton = false,
}) {
  // =========================================================================
  // STATE TANIMLARI
  // =========================================================================
  const [showCourierSelect, setShowCourierSelect] = useState(false);
  const [selectedCourierId, setSelectedCourierId] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [showReassignModal, setShowReassignModal] = useState(false);
  const [reassignReason, setReassignReason] = useState("");

  // =========================================================================
  // YARDIMCI FONKSİYONLAR
  // =========================================================================

  // Bekleme süresini hesapla ve formatla
  const formatWaitingTime = (readyAt) => {
    if (!readyAt) return "-";

    const readyDate = new Date(readyAt);
    const now = new Date();
    const diffMs = now - readyDate;
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return "Az önce";
    if (diffMins < 60) return `${diffMins} dk`;

    const hours = Math.floor(diffMins / 60);
    const mins = diffMins % 60;
    return `${hours} sa ${mins} dk`;
  };

  // Ödeme yöntemi badge rengi
  const getPaymentMethodBadge = (method) => {
    const badges = {
      CreditCard: {
        bg: "rgba(0,123,255,0.2)",
        color: "#0d6efd",
        text: "Kredi Kartı",
        icon: "fa-credit-card",
      },
      CashOnDelivery: {
        bg: "rgba(40,167,69,0.2)",
        color: "#28a745",
        text: "Kapıda Nakit",
        icon: "fa-money-bill-wave",
      },
      CardOnDelivery: {
        bg: "rgba(102,126,234,0.2)",
        color: "#667eea",
        text: "Kapıda Kart",
        icon: "fa-credit-card",
      },
    };
    return (
      badges[method] || {
        bg: "rgba(108,117,125,0.2)",
        color: "#6c757d",
        text: method,
        icon: "fa-wallet",
      }
    );
  };

  // Acil sipariş kontrolü (30 dakikadan fazla bekleyen)
  const isUrgent = () => {
    if (!order.readyAt) return false;
    const readyDate = new Date(order.readyAt);
    const now = new Date();
    const diffMins = Math.floor((now - readyDate) / 60000);
    return diffMins > 30;
  };

  // =========================================================================
  // KURYE ATAMA
  // =========================================================================
  const handleAssign = async () => {
    if (!selectedCourierId) {
      setError("Lütfen bir kurye seçin");
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const result = await onAssignCourier(
        order.orderId || order.id,
        selectedCourierId,
      );
      if (!result.success) {
        setError(result.error || "Kurye atanamadı");
      } else {
        setShowCourierSelect(false);
        setSelectedCourierId(null);
      }
    } catch (err) {
      setError("Bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  // =========================================================================
  // KURYE DEĞİŞTİRME
  // =========================================================================
  const handleReassign = async () => {
    if (!selectedCourierId) {
      setError("Lütfen yeni bir kurye seçin");
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const result = await onReassignCourier(
        order.orderId || order.id,
        selectedCourierId,
        reassignReason,
      );
      if (!result.success) {
        setError(result.error || "Kurye değiştirilemedi");
      } else {
        setShowReassignModal(false);
        setSelectedCourierId(null);
        setReassignReason("");
      }
    } catch (err) {
      setError("Bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  // =========================================================================
  // RENDER
  // =========================================================================
  const paymentBadge = getPaymentMethodBadge(order.paymentMethod);
  const urgent = isUrgent();

  return (
    <div
      className={`card border-0 h-100 ${urgent ? "border-warning" : ""}`}
      style={{
        background: "rgba(255,255,255,0.08)",
        borderRadius: "16px",
        transition: "all 0.3s ease",
        borderLeft: urgent ? "4px solid #ffc107" : "none",
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = "translateY(-4px)";
        e.currentTarget.style.background = "rgba(255,255,255,0.12)";
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = "translateY(0)";
        e.currentTarget.style.background = "rgba(255,255,255,0.08)";
      }}
    >
      <div className="card-body p-3">
        {/* Üst Kısım - Sipariş No ve Durum */}
        <div className="d-flex justify-content-between align-items-start mb-3">
          <div>
            <h6 className="mb-1 fw-bold text-white">
              #{order.orderNumber}
              {urgent && (
                <span
                  className="badge ms-2"
                  style={{
                    background: "rgba(255,193,7,0.2)",
                    color: "#ffc107",
                    fontSize: "0.65rem",
                  }}
                >
                  <i className="fas fa-exclamation-triangle me-1"></i>
                  ACİL
                </span>
              )}
            </h6>
            <small className="text-white-50">
              <i className="fas fa-clock me-1"></i>
              {formatWaitingTime(order.readyAt)} bekledi
            </small>
          </div>

          {/* Ödeme Yöntemi Badge */}
          <span
            className="badge"
            style={{
              background: paymentBadge.bg,
              color: paymentBadge.color,
              fontSize: "0.7rem",
            }}
          >
            <i className={`fas ${paymentBadge.icon} me-1`}></i>
            {paymentBadge.text}
          </span>
        </div>

        {/* Müşteri Bilgileri */}
        <div className="mb-3">
          <div className="d-flex align-items-center mb-2">
            <div
              className="d-flex align-items-center justify-content-center me-2"
              style={{
                width: "32px",
                height: "32px",
                borderRadius: "50%",
                background: "rgba(102,126,234,0.2)",
              }}
            >
              <i
                className="fas fa-user text-primary"
                style={{ fontSize: "0.8rem" }}
              ></i>
            </div>
            <div>
              <span className="text-white fw-semibold">
                {order.customerName}
              </span>
              {order.customerPhone && (
                <a
                  href={`tel:${order.customerPhone}`}
                  className="ms-2 text-white-50 text-decoration-none"
                  style={{ fontSize: "0.85rem" }}
                >
                  <i className="fas fa-phone"></i>
                </a>
              )}
            </div>
          </div>

          {/* Adres */}
          <div className="d-flex align-items-start">
            <i
              className="fas fa-map-marker-alt text-danger me-2 mt-1"
              style={{ fontSize: "0.8rem" }}
            ></i>
            <small className="text-white-50" style={{ lineHeight: "1.4" }}>
              {order.deliveryAddress || order.address || "Adres bilgisi yok"}
            </small>
          </div>
        </div>

        {/* Sipariş Detayları */}
        <div
          className="d-flex justify-content-between align-items-center p-2 mb-3"
          style={{
            background: "rgba(0,0,0,0.2)",
            borderRadius: "10px",
          }}
        >
          <div className="text-center">
            <small className="text-white-50 d-block">Tutar</small>
            <span className="text-white fw-bold">
              ₺{order.totalAmount?.toFixed(2) || "0.00"}
            </span>
          </div>
          <div className="text-center">
            <small className="text-white-50 d-block">Ürün</small>
            <span className="text-white fw-bold">{order.itemCount || "-"}</span>
          </div>
          {order.weightInGrams && (
            <div className="text-center">
              <small className="text-white-50 d-block">Ağırlık</small>
              <span className="text-white fw-bold">
                {order.weightInGrams >= 1000
                  ? `${(order.weightInGrams / 1000).toFixed(1)} kg`
                  : `${order.weightInGrams} g`}
              </span>
            </div>
          )}
        </div>

        {/* Atanmış Kurye Bilgisi (varsa) */}
        {order.courierName && (
          <div
            className="d-flex align-items-center p-2 mb-3"
            style={{
              background: "rgba(40,167,69,0.15)",
              borderRadius: "10px",
            }}
          >
            <div
              className="d-flex align-items-center justify-content-center me-2"
              style={{
                width: "28px",
                height: "28px",
                borderRadius: "50%",
                background: "rgba(40,167,69,0.3)",
              }}
            >
              <i
                className="fas fa-motorcycle text-success"
                style={{ fontSize: "0.7rem" }}
              ></i>
            </div>
            <div>
              <small
                className="text-white-50 d-block"
                style={{ fontSize: "0.7rem" }}
              >
                Kurye
              </small>
              <span
                className="text-success fw-semibold"
                style={{ fontSize: "0.85rem" }}
              >
                {order.courierName}
              </span>
            </div>
          </div>
        )}

        {/* Hata Mesajı */}
        {error && (
          <div
            className="alert alert-danger py-2 mb-3"
            style={{ fontSize: "0.8rem", borderRadius: "8px" }}
          >
            <i className="fas fa-exclamation-circle me-1"></i>
            {error}
          </div>
        )}

        {/* Kurye Seçimi (açıksa) */}
        {showCourierSelect && (
          <div className="mb-3">
            <CourierSelect
              couriers={couriers}
              selectedId={selectedCourierId}
              onSelect={setSelectedCourierId}
              compact={true}
            />
            <div className="d-flex gap-2 mt-2">
              <button
                className="btn btn-sm flex-fill"
                onClick={handleAssign}
                disabled={loading || !selectedCourierId}
                style={{
                  background:
                    "linear-gradient(135deg, #28a745 0%, #20c997 100%)",
                  border: "none",
                  color: "#fff",
                }}
              >
                {loading ? (
                  <span className="spinner-border spinner-border-sm"></span>
                ) : (
                  <>
                    <i className="fas fa-check me-1"></i>
                    Ata
                  </>
                )}
              </button>
              <button
                className="btn btn-sm btn-outline-light"
                onClick={() => {
                  setShowCourierSelect(false);
                  setSelectedCourierId(null);
                  setError(null);
                }}
                disabled={loading}
              >
                İptal
              </button>
            </div>
          </div>
        )}

        {/* Aksiyon Butonları */}
        {!showCourierSelect && (
          <div className="d-flex gap-2">
            {showAssignButton && (
              <button
                className="btn btn-sm flex-fill"
                onClick={() => setShowCourierSelect(true)}
                style={{
                  background:
                    "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                  border: "none",
                  color: "#fff",
                }}
              >
                <i className="fas fa-user-plus me-1"></i>
                Kurye Ata
              </button>
            )}

            {showReassignButton && (
              <button
                className="btn btn-sm flex-fill btn-outline-warning"
                onClick={() => setShowReassignModal(true)}
              >
                <i className="fas fa-exchange-alt me-1"></i>
                Kurye Değiştir
              </button>
            )}
          </div>
        )}
      </div>

      {/* Kurye Değiştirme Modal */}
      {showReassignModal && (
        <div
          className="modal fade show d-block"
          style={{ background: "rgba(0,0,0,0.7)" }}
          onClick={(e) => {
            if (e.target === e.currentTarget) {
              setShowReassignModal(false);
            }
          }}
        >
          <div className="modal-dialog modal-dialog-centered">
            <div
              className="modal-content border-0"
              style={{
                background: "#2d2d44",
                borderRadius: "16px",
              }}
            >
              <div className="modal-header border-0">
                <h6 className="modal-title text-white">
                  Kurye Değiştir - #{order.orderNumber}
                </h6>
                <button
                  className="btn-close btn-close-white"
                  onClick={() => setShowReassignModal(false)}
                ></button>
              </div>
              <div className="modal-body">
                {/* Mevcut Kurye */}
                <div
                  className="p-2 mb-3"
                  style={{
                    background: "rgba(255,193,7,0.1)",
                    borderRadius: "8px",
                  }}
                >
                  <small className="text-warning d-block mb-1">
                    Mevcut Kurye
                  </small>
                  <span className="text-white">{order.courierName}</span>
                </div>

                {/* Yeni Kurye Seçimi */}
                <label className="text-white-50 small mb-2">
                  Yeni Kurye Seç
                </label>
                <CourierSelect
                  couriers={couriers.filter((c) => c.id !== order.courierId)}
                  selectedId={selectedCourierId}
                  onSelect={setSelectedCourierId}
                />

                {/* Değişiklik Nedeni */}
                <label className="text-white-50 small mt-3 mb-2">
                  Değişiklik Nedeni (opsiyonel)
                </label>
                <textarea
                  className="form-control border-0"
                  rows="2"
                  placeholder="Örn: Kurye müsait değil"
                  value={reassignReason}
                  onChange={(e) => setReassignReason(e.target.value)}
                  style={{
                    background: "rgba(255,255,255,0.1)",
                    color: "#fff",
                    resize: "none",
                  }}
                ></textarea>

                {error && (
                  <div className="alert alert-danger py-2 mt-3 mb-0">
                    {error}
                  </div>
                )}
              </div>
              <div className="modal-footer border-0">
                <button
                  className="btn btn-outline-light"
                  onClick={() => {
                    setShowReassignModal(false);
                    setSelectedCourierId(null);
                    setReassignReason("");
                    setError(null);
                  }}
                >
                  İptal
                </button>
                <button
                  className="btn"
                  onClick={handleReassign}
                  disabled={loading || !selectedCourierId}
                  style={{
                    background:
                      "linear-gradient(135deg, #ffc107 0%, #ff9800 100%)",
                    border: "none",
                    color: "#000",
                  }}
                >
                  {loading ? (
                    <span className="spinner-border spinner-border-sm"></span>
                  ) : (
                    <>
                      <i className="fas fa-exchange-alt me-1"></i>
                      Değiştir
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
