// ==========================================================================
// WeightBasedProductAlert.jsx - Ağırlık Bazlı Ürün Uyarı Komponenti
// ==========================================================================
// Sepette ağırlık bazlı ürün olduğunda müşteriye gösterilen uyarı komponenti.
// Tahmini fiyat ve gerçek fiyat arasındaki farkı açıklar.
// Profesyonel ve mobil uyumlu tasarım.
// ==========================================================================

import React, { useState } from "react";
import "./WeightBasedProductAlert.css";

/**
 * Ağırlık birimini formatlı göster
 */
const formatWeightUnit = (unit) => {
  const units = {
    Piece: "Adet",
    Gram: "gr",
    Kilogram: "kg",
    Liter: "lt",
    Milliliter: "ml",
    0: "Adet",
    1: "gr",
    2: "kg",
    3: "lt",
    4: "ml",
  };
  return units[unit] || unit;
};

/**
 * WeightBasedProductAlert - Ana Uyarı Komponenti
 * @param {Array} weightBasedItems - Ağırlık bazlı ürünlerin listesi
 * @param {boolean} showDetails - Detayları göster/gizle
 * @param {string} variant - 'cart' | 'checkout' | 'compact'
 */
export default function WeightBasedProductAlert({
  weightBasedItems = [],
  showDetails = false,
  variant = "cart",
  onClose,
}) {
  const [isExpanded, setIsExpanded] = useState(showDetails);
  const [isDismissed, setIsDismissed] = useState(false);

  // Ağırlık bazlı ürün yoksa gösterme
  if (!weightBasedItems || weightBasedItems.length === 0 || isDismissed) {
    return null;
  }

  // Toplam tahmini tutarı hesapla
  const totalEstimatedAmount = weightBasedItems.reduce((sum, item) => {
    return (
      sum + (item.estimatedPrice || item.unitPrice || 0) * (item.quantity || 1)
    );
  }, 0);

  // Dismiss handler
  const handleDismiss = () => {
    setIsDismissed(true);
    onClose?.();
  };

  // Compact variant (küçük banner)
  if (variant === "compact") {
    return (
      <div className="weight-alert weight-alert-compact">
        <i className="fas fa-balance-scale alert-icon"></i>
        <span className="alert-text">
          Sepetinizde {weightBasedItems.length} adet tartılı ürün var
        </span>
        <button
          className="alert-expand-btn"
          onClick={() => setIsExpanded(!isExpanded)}
        >
          <i className={`fas fa-chevron-${isExpanded ? "up" : "down"}`}></i>
        </button>
      </div>
    );
  }

  return (
    <div className={`weight-based-alert ${variant}`}>
      {/* Header */}
      <div className="alert-header">
        <div className="alert-icon-wrapper">
          <i className="fas fa-balance-scale"></i>
        </div>
        <div className="alert-title">
          <h6>Tartılı Ürün Bilgilendirmesi</h6>
          <span className="alert-subtitle">
            {weightBasedItems.length} ürün ağırlığa göre fiyatlandırılacak
          </span>
        </div>
        {onClose && (
          <button className="alert-close-btn" onClick={handleDismiss}>
            <i className="fas fa-times"></i>
          </button>
        )}
      </div>

      {/* Info Content */}
      <div className="alert-content">
        <div className="alert-message">
          <i className="fas fa-info-circle"></i>
          <p>
            Sepetinizdeki bazı ürünler <strong>kg/gram</strong> bazında
            satılmaktadır. Gösterilen fiyatlar <strong>tahmini</strong> olup,
            kurye teslimat sırasında ürünü tartarak{" "}
            <strong>gerçek fiyatı</strong> belirleyecektir.
          </p>
        </div>

        {/* Expand/Collapse Button */}
        <button
          className="expand-toggle"
          onClick={() => setIsExpanded(!isExpanded)}
        >
          <span>{isExpanded ? "Detayları Gizle" : "Detayları Göster"}</span>
          <i className={`fas fa-chevron-${isExpanded ? "up" : "down"}`}></i>
        </button>

        {/* Expanded Details */}
        {isExpanded && (
          <div className="alert-details">
            {/* Ürün Listesi */}
            <div className="weight-items-list">
              {weightBasedItems.map((item, index) => (
                <div key={index} className="weight-item">
                  <div className="item-info">
                    <span className="item-name">
                      {item.name || item.productName}
                    </span>
                    <span className="item-unit">
                      {item.quantity}{" "}
                      {formatWeightUnit(item.weightUnit || item.unit)}
                    </span>
                  </div>
                  <div className="item-price">
                    <span className="estimated-label">Tahmini:</span>
                    <span className="estimated-price">
                      ₺
                      {(
                        (item.estimatedPrice || item.unitPrice || 0) *
                        (item.quantity || 1)
                      ).toFixed(2)}
                    </span>
                  </div>
                </div>
              ))}
            </div>

            {/* Toplam */}
            <div className="alert-total">
              <span>Toplam Tahmini Tutar:</span>
              <span className="total-amount">
                ₺{totalEstimatedAmount.toFixed(2)}
              </span>
            </div>

            {/* Açıklama Kutuları */}
            <div className="info-boxes">
              <div className="info-box positive">
                <i className="fas fa-arrow-down"></i>
                <div>
                  <strong>Daha Az Gelirse</strong>
                  <span>Fark kartınıza/hesabınıza iade edilir</span>
                </div>
              </div>
              <div className="info-box negative">
                <i className="fas fa-arrow-up"></i>
                <div>
                  <strong>Daha Fazla Gelirse</strong>
                  <span>Fark ödemenize eklenir</span>
                </div>
              </div>
            </div>

            {/* Güvence Mesajı */}
            <div className="guarantee-message">
              <i className="fas fa-shield-alt"></i>
              <span>
                %20'den fazla fark olursa admin onayı gerekir ve sizinle
                iletişime geçilir.
              </span>
            </div>
          </div>
        )}
      </div>

      {/* Footer - Checkout variant için */}
      {variant === "checkout" && (
        <div className="alert-footer">
          <div className="checkout-note">
            <i className="fas fa-credit-card"></i>
            <span>
              Kredi kartı ödemelerinde, tahmini tutar üzerinden{" "}
              <strong>ön provizyon</strong> alınır. Teslimat sonrası kesin tutar
              çekilir.
            </span>
          </div>
        </div>
      )}
    </div>
  );
}

/**
 * WeightBasedItemBadge - Ürün kartlarında gösterilecek küçük badge
 */
export function WeightBasedItemBadge({ weightUnit, pricePerUnit }) {
  return (
    <div className="weight-badge">
      <i className="fas fa-balance-scale"></i>
      <span>Tartılı</span>
      {pricePerUnit && (
        <span className="price-per-unit">
          ₺{pricePerUnit.toFixed(2)}/{formatWeightUnit(weightUnit)}
        </span>
      )}
    </div>
  );
}

/**
 * WeightEstimateIndicator - Sepet satırında tahmini göstergesi
 */
export function WeightEstimateIndicator({ isWeightBased }) {
  if (!isWeightBased) return null;

  return (
    <span
      className="estimate-indicator"
      title="Bu fiyat tahminidir. Gerçek fiyat tartım sonrası belirlenir."
    >
      <i className="fas fa-info-circle"></i>
      Tahmini
    </span>
  );
}
