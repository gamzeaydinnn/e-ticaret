// ==========================================================================
// CourierWeightEntry.jsx - Kurye Ağırlık Giriş Sayfası/Sekmesi
// ==========================================================================
// Kuryelerin siparişteki ağırlık bazlı ürünleri tartıp girmeleri için
// tam sayfa/sekme komponenti. WeightEntryCard ve WeightDifferenceSummary
// komponentlerini kullanır.
//
// Özellikler:
// - Ağırlık bazlı ürünlerin listesi
// - Her ürün için tartı girişi
// - Anlık fark özeti
// - Toplu tartı kaydetme
// - Teslimat tamamlama butonu
// ==========================================================================

import React, { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import PropTypes from "prop-types";
import WeightEntryCard from "./components/WeightEntryCard";
import WeightDifferenceSummary from "./components/WeightDifferenceSummary";
import {
  WeightAdjustmentService,
  WeightPaymentService,
} from "../../services/weightAdjustmentService";
import { useCourierAuth } from "../../contexts/CourierAuthContext";

/**
 * CourierWeightEntry - Kurye Ağırlık Giriş Ana Komponenti
 *
 * Bu komponent iki modda kullanılabilir:
 * 1. Standalone sayfa olarak (/courier/orders/:orderId/weight)
 * 2. CourierDeliveryDetail içinde sekme olarak (embedded mode)
 *
 * @param {Object} order - Sipariş bilgisi (embedded modda)
 * @param {Function} onComplete - Tamamlandığında callback (embedded modda)
 * @param {boolean} embedded - Gömülü mod mu?
 */
export default function CourierWeightEntry({
  order: propOrder,
  onComplete,
  embedded = false,
}) {
  const { orderId: paramOrderId } = useParams();
  const navigate = useNavigate();
  const { courier } = useCourierAuth();

  // =========================================================================
  // STATE
  // =========================================================================
  const [order, setOrder] = useState(propOrder || null);
  const [weightItems, setWeightItems] = useState([]);
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState(null);
  const [successMessage, setSuccessMessage] = useState("");

  // Sipariş ID'sini al (prop veya URL'den)
  const orderId = propOrder?.orderId || propOrder?.id || paramOrderId;

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================

  /**
   * Sipariş ağırlık verilerini yükle
   */
  const loadWeightData = useCallback(async () => {
    if (!orderId) return;

    try {
      setLoading(true);
      setError(null);

      // Ağırlık özetini getir
      const summaryData =
        await WeightAdjustmentService.getOrderWeightSummary(orderId);

      if (summaryData) {
        setSummary(summaryData);

        // Ağırlık bazlı ürünleri filtrele
        // Backend'den adjustments içinde gelebilir veya ayrı endpoint'ten
        if (summaryData.adjustments) {
          setWeightItems(
            summaryData.adjustments.map((adj) => ({
              ...adj,
              // Frontend için ek alanlar
              id: adj.orderItemId,
              isWeightBased: true,
            })),
          );
        } else if (propOrder?.items) {
          // Prop'tan gelen siparişten weight-based ürünleri al
          const weightBasedItems = propOrder.items.filter(
            (item) =>
              item.isWeightBased ||
              item.weightUnit === "Gram" ||
              item.weightUnit === "Kilogram",
          );
          setWeightItems(weightBasedItems);
        }
      }
    } catch (err) {
      console.error("[CourierWeightEntry] loadWeightData error:", err);
      setError("Ağırlık verileri yüklenirken bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }, [orderId, propOrder]);

  useEffect(() => {
    loadWeightData();
  }, [loadWeightData]);

  // =========================================================================
  // EVENT HANDLERS
  // =========================================================================

  /**
   * Tek ürün için ağırlık kaydet
   */
  const handleWeightSubmit = async (weightData) => {
    try {
      setSubmitting(true);

      await WeightAdjustmentService.recordWeight(
        orderId,
        weightData.orderItemId,
        {
          actualWeightGrams: weightData.actualWeightGrams,
          notes: weightData.notes,
        },
      );

      // Başarı mesajı
      setSuccessMessage("Ağırlık kaydedildi!");
      setTimeout(() => setSuccessMessage(""), 2000);

      // Verileri yenile
      await loadWeightData();
    } catch (err) {
      console.error("[CourierWeightEntry] handleWeightSubmit error:", err);
      throw new Error(err.response?.data?.message || "Ağırlık kaydedilemedi");
    } finally {
      setSubmitting(false);
    }
  };

  /**
   * Teslimatı tamamla ve ödemeyi kesinleştir
   */
  const handleFinalizeDelivery = async () => {
    // Tüm ürünler tartıldı mı kontrol et
    if (summary && !summary.allItemsWeighed) {
      if (
        !window.confirm(
          "Bazı ürünler henüz tartılmadı. Yine de devam etmek istiyor musunuz?",
        )
      ) {
        return;
      }
    }

    // Yüksek fark uyarısı
    if (summary && Math.abs(summary.differencePercent) > 20) {
      const confirmMsg =
        summary.differencePercent > 0
          ? `Toplam fark +%${summary.differencePercent.toFixed(1)} (${summary.totalDifference.toFixed(2)} ₺ ek ödeme). Devam edilsin mi?`
          : `Toplam fark ${summary.differencePercent.toFixed(1)}% (${Math.abs(summary.totalDifference).toFixed(2)} ₺ iade). Devam edilsin mi?`;

      if (!window.confirm(confirmMsg)) {
        return;
      }
    }

    try {
      setSubmitting(true);
      setError(null);

      // Teslimatı tamamla
      const result = await WeightPaymentService.finalizeDelivery(orderId, {
        courierNotes: `Ağırlık tartımı tamamlandı. Kurye: ${courier?.name || "Bilinmiyor"}`,
      });

      if (result.success) {
        setSuccessMessage("Teslimat başarıyla tamamlandı!");

        // Callback veya yönlendirme
        if (onComplete) {
          onComplete(result);
        } else if (!embedded) {
          setTimeout(() => {
            navigate("/courier/orders");
          }, 1500);
        }
      } else {
        // Admin onayı gerekiyorsa
        if (result.requiresAdminApproval) {
          setSuccessMessage(
            "Yüksek fark tespit edildi. Admin onayı bekleniyor.",
          );
          if (onComplete) {
            onComplete(result);
          }
        } else {
          setError(result.message || "Teslimat tamamlanamadı");
        }
      }
    } catch (err) {
      console.error("[CourierWeightEntry] handleFinalizeDelivery error:", err);
      setError(
        err.response?.data?.message || "Teslimat tamamlanırken bir hata oluştu",
      );
    } finally {
      setSubmitting(false);
    }
  };

  // =========================================================================
  // RENDER HELPERS
  // =========================================================================

  /**
   * Ödeme metodunu Türkçe'ye çevir
   */
  const getPaymentMethodDisplay = () => {
    const method = order?.paymentMethod || propOrder?.paymentMethod;
    switch (method) {
      case "Cash":
        return "Nakit";
      case "Card":
        return "Kredi Kartı";
      case "Online":
        return "Online Ödeme";
      default:
        return method || "Bilinmiyor";
    }
  };

  // =========================================================================
  // LOADING STATE
  // =========================================================================
  if (loading) {
    return (
      <div
        className={`${embedded ? "" : "min-vh-100"} d-flex align-items-center justify-content-center`}
      >
        <div className="text-center p-4">
          <div
            className="spinner-border text-primary mb-3"
            style={{ width: "3rem", height: "3rem" }}
          ></div>
          <p className="text-muted mb-0">Ağırlık bilgileri yükleniyor...</p>
        </div>
      </div>
    );
  }

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <div className={embedded ? "" : "min-vh-100 bg-light"}>
      {/* Header (standalone modda) */}
      {!embedded && (
        <nav
          className="navbar navbar-dark sticky-top"
          style={{ background: "linear-gradient(135deg, #ff6b35, #ff8c00)" }}
        >
          <div className="container-fluid">
            <div className="d-flex align-items-center">
              <button
                className="btn btn-link text-white p-0 me-3"
                onClick={() => navigate(-1)}
              >
                <i className="fas fa-arrow-left fs-5"></i>
              </button>
              <div>
                <span className="navbar-brand mb-0 fw-bold">
                  Ağırlık Girişi
                </span>
                <div>
                  <span className="text-white-50" style={{ fontSize: "12px" }}>
                    Sipariş #{orderId}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </nav>
      )}

      {/* Content */}
      <div
        className={embedded ? "" : "container-fluid p-3"}
        style={{ paddingBottom: embedded ? "0" : "100px" }}
      >
        {/* Error Alert */}
        {error && (
          <div
            className="alert alert-danger d-flex align-items-center"
            role="alert"
          >
            <i className="fas fa-exclamation-circle me-2"></i>
            {error}
            <button
              type="button"
              className="btn-close ms-auto"
              onClick={() => setError(null)}
            ></button>
          </div>
        )}

        {/* Success Alert */}
        {successMessage && (
          <div
            className="alert alert-success d-flex align-items-center"
            role="alert"
          >
            <i className="fas fa-check-circle me-2"></i>
            {successMessage}
          </div>
        )}

        {/* Bilgi Kartı */}
        <div
          className="bg-white rounded-3 p-3 mb-3 shadow-sm"
          style={{ border: "1px solid #e9ecef" }}
        >
          <div className="d-flex justify-content-between align-items-center">
            <div>
              <h6 className="mb-1 fw-bold">
                <i
                  className="fas fa-balance-scale me-2"
                  style={{ color: "#ff6b35" }}
                ></i>
                Ağırlık Bazlı Ürünler
              </h6>
              <small className="text-muted">
                {weightItems.length} ürün tartılacak • Ödeme:{" "}
                {getPaymentMethodDisplay()}
              </small>
            </div>
            <span className="badge bg-primary">
              {summary?.weighedItemCount || 0}/
              {summary?.weightBasedItemCount || weightItems.length}
            </span>
          </div>
        </div>

        {/* Ağırlık Fark Özeti */}
        {summary && (
          <WeightDifferenceSummary
            summary={summary}
            paymentMethod={
              order?.paymentMethod || propOrder?.paymentMethod || "Cash"
            }
            loading={submitting}
          />
        )}

        {/* Ürün Kartları - Tartılacak Ürünler */}
        {weightItems.length > 0 ? (
          <>
            <h6 className="text-muted mb-3 mt-4">
              <i className="fas fa-list me-2"></i>
              Ürünleri Tartın
            </h6>

            {weightItems.map((item, index) => (
              <WeightEntryCard
                key={item.id || item.orderItemId || index}
                item={item}
                onWeightSubmit={handleWeightSubmit}
                disabled={submitting}
                loading={submitting}
              />
            ))}
          </>
        ) : (
          <div className="text-center py-5">
            <i
              className="fas fa-box-open text-muted mb-3"
              style={{ fontSize: "48px" }}
            ></i>
            <p className="text-muted">
              Bu siparişte ağırlık bazlı ürün bulunmuyor.
            </p>
          </div>
        )}

        {/* Teslimatı Tamamla Butonu (embedded modda parent'a bırakılabilir) */}
        {!embedded && weightItems.length > 0 && (
          <div
            className="fixed-bottom bg-white p-3 shadow-lg"
            style={{
              borderTop: "1px solid #e9ecef",
              paddingBottom: "calc(16px + env(safe-area-inset-bottom))",
            }}
          >
            <button
              className="btn btn-success w-100 py-3"
              onClick={handleFinalizeDelivery}
              disabled={
                submitting || (summary && summary.weighedItemCount === 0)
              }
              style={{
                borderRadius: "12px",
                fontSize: "16px",
                fontWeight: "600",
              }}
            >
              {submitting ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2"></span>
                  İşleniyor...
                </>
              ) : (
                <>
                  <i className="fas fa-check-circle me-2"></i>
                  Teslimatı Tamamla
                  {summary?.totalDifference !== 0 &&
                    summary?.totalDifference !== undefined && (
                      <span className="ms-2 badge bg-light text-dark">
                        {summary.totalDifference >= 0 ? "+" : ""}
                        {summary.totalDifference.toFixed(2)} ₺
                      </span>
                    )}
                </>
              )}
            </button>

            {/* Uyarı */}
            {summary && !summary.allItemsWeighed && (
              <div className="text-center mt-2">
                <small className="text-warning">
                  <i className="fas fa-exclamation-triangle me-1"></i>
                  Bazı ürünler henüz tartılmadı
                </small>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

CourierWeightEntry.propTypes = {
  order: PropTypes.object,
  onComplete: PropTypes.func,
  embedded: PropTypes.bool,
};
