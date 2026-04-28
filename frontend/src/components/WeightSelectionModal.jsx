/**
 * WeightSelectionModal.jsx - Kilogram Seçici Modal
 *
 * Meyve, sebze gibi kg bazlı ürünler için sepete ekleme öncesi
 * miktar seçimi yapılmasını sağlayan profesyonel modal komponenti.
 *
 * Özellikler:
 * - Minimum 0.5 kg ile başlar
 * - 0.5 kg artış/azalış adımları
 * - Slider ve buton kontrolü
 * - Mobil uyumlu tasarım
 */
import React, { useState, useCallback, useEffect } from "react";
import "./WeightSelectionModal.css";

// Kg bazlı satılabilecek kategori isimleri/anahtar kelimeleri
const WEIGHT_BASED_CATEGORIES = [
  "meyve",
  "sebze",
  "manav",
  "et",
  "tavuk",
  "balık",
  "balik",
  "kuruyemiş",
  "kuruyemis",
  "şarküteri",
  "sarkuteri",
  "peynir",
  "zeytin",
];

// Kg bazlı satılabilecek ürün isimleri/anahtar kelimeleri
const WEIGHT_BASED_KEYWORDS = [
  "domates",
  "patates",
  "soğan",
  "sogan",
  "biber",
  "patlıcan",
  "patlican",
  "salatalık",
  "salatalik",
  "kabak",
  "havuç",
  "havuc",
  "elma",
  "armut",
  "portakal",
  "mandalina",
  "muz",
  "üzüm",
  "uzum",
  "kiraz",
  "çilek",
  "cilek",
  "karpuz",
  "kavun",
  "şeftali",
  "seftali",
  "erik",
  "kayısı",
  "kayisi",
  "limon",
  "kıyma",
  "kiyma",
  "kuşbaşı",
  "kusbasi",
  "pirzola",
  "bonfile",
  "biftek",
  "somon",
  "levrek",
  "çupra",
  "cupra",
  "hamsi",
  "karides",
  "badem",
  "fındık",
  "findik",
  "ceviz",
  "fıstık",
  "fistik",
  "kaju",
];

/**
 * Ürünün kg bazlı satılıp satılmayacağını kontrol et
 * @param {object} product - Ürün objesi
 * @returns {boolean} - Kg bazlı mı?
 */
export const isWeightBasedProduct = (product) => {
  if (!product) return false;

  const productName = (product.name || product.Name || "").toUpperCase();

  // NEDEN: İsim kontrolü HER şeyden önce gelir — backend isWeightBased=true demiş olsa bile
  // "3 KG", "1 LT", "50 GR" gibi sabit paket ürünler weight modal açmamalı.
  // "DOMATES KG" (sayısız) → regex eşleşmez → devam eder. ✓
  if (/\d+\s*(GR|KG|LT|ML|CL|L)\b/.test(productName)) return false;
  if (/\bADET\b/.test(productName)) return false;

  // Backend'den gelen isWeightBased veya weightUnit kontrolü
  if (product.isWeightBased === true) return true;
  if (product.soldByWeight === true) return true;
  if (product.weightUnit === "Kilogram" || product.weightUnit === 2) {
    return true;
  }

  // NEDEN: Mikro ERP'den gelen birim bilgisi — "KG" ise kg bazlı, değilse değil.
  // unit alanı varsa kesin karar ver, keyword fallback'e düşme.
  // "ANANAS ADET" (unit=ADET) → false, "DOMATES KG" (unit=KG) → true
  const unit = (product.unit || "").toUpperCase().trim();
  if (unit) return unit === "KG";

  // unit alanı yoksa (eski ürünler / local DB) → kategori/keyword fallback
  const categoryName = (
    product.categoryName ||
    product.category?.name ||
    ""
  ).toLowerCase();
  if (WEIGHT_BASED_CATEGORIES.some((cat) => categoryName.includes(cat))) {
    return true;
  }

  const productNameLower = productName.toLowerCase();
  if (
    WEIGHT_BASED_KEYWORDS.some((keyword) => productNameLower.includes(keyword))
  ) {
    return true;
  }

  return false;
};

/**
 * WeightSelectionModal - Ana Komponent
 */
export default function WeightSelectionModal({
  isOpen,
  onClose,
  product,
  onConfirm,
  minWeight = 0.5,
  maxWeight = 10,
  step = 0.5,
}) {
  // State: Seçilen ağırlık
  const [selectedWeight, setSelectedWeight] = useState(minWeight);
  // Animasyon için state
  const [isClosing, setIsClosing] = useState(false);

  // Modal açıldığında varsayılan değeri ayarla
  useEffect(() => {
    if (isOpen) {
      setSelectedWeight(minWeight);
      setIsClosing(false);
    }
  }, [isOpen, minWeight]);

  // Ağırlığı artır
  const increaseWeight = useCallback(() => {
    setSelectedWeight((prev) => {
      const newWeight = Math.round((prev + step) * 100) / 100;
      return newWeight <= maxWeight ? newWeight : prev;
    });
  }, [step, maxWeight]);

  // Ağırlığı azalt
  const decreaseWeight = useCallback(() => {
    setSelectedWeight((prev) => {
      const newWeight = Math.round((prev - step) * 100) / 100;
      return newWeight >= minWeight ? newWeight : prev;
    });
  }, [step, minWeight]);

  // Slider değişimi
  const handleSliderChange = useCallback(
    (e) => {
      const value = parseFloat(e.target.value);
      // Step'e yuvarla
      const rounded = Math.round(value / step) * step;
      setSelectedWeight(Math.round(rounded * 100) / 100);
    },
    [step],
  );

  // Onay butonu
  const handleConfirm = useCallback(() => {
    if (onConfirm && product) {
      onConfirm(product, selectedWeight);
    }
    handleClose();
  }, [onConfirm, product, selectedWeight]);

  // Modal'ı kapat (animasyonlu)
  const handleClose = useCallback(() => {
    setIsClosing(true);
    setTimeout(() => {
      onClose?.();
      setIsClosing(false);
    }, 200);
  }, [onClose]);

  // Backdrop tıklaması
  const handleBackdropClick = useCallback(
    (e) => {
      if (e.target === e.currentTarget) {
        handleClose();
      }
    },
    [handleClose],
  );

  // Gösterilmiyorsa render etme
  if (!isOpen || !product) return null;

  // Fiyat hesaplamaları
  const basePrice = Number(product.price || product.Price || 0);
  const specialPrice = Number(
    product.specialPrice ||
      product.discountedPrice ||
      product.DiscountedPrice ||
      0,
  );
  const hasDiscount = specialPrice > 0 && specialPrice < basePrice;
  const pricePerKg = hasDiscount ? specialPrice : basePrice;
  const totalPrice = pricePerKg * selectedWeight;
  const originalTotal = hasDiscount ? basePrice * selectedWeight : null;

  // Resim URL'i
  const imageUrl =
    product.imageUrl ||
    product.ImageUrl ||
    product.image ||
    "/images/placeholder.png";

  return (
    <div
      className={`weight-modal-overlay ${isClosing ? "closing" : ""}`}
      onClick={handleBackdropClick}
    >
      <div className={`weight-modal ${isClosing ? "closing" : ""}`}>
        {/* Header */}
        <div className="weight-modal-header">
          <div className="header-icon">
            <i className="fas fa-balance-scale"></i>
          </div>
          <h3>Miktar Seçin</h3>
          <button className="close-btn" onClick={handleClose}>
            <i className="fas fa-times"></i>
          </button>
        </div>

        {/* Ürün Bilgisi */}
        <div className="weight-modal-product">
          <img
            src={imageUrl}
            alt={product.name || product.Name}
            onError={(e) => {
              e.target.src = "/images/placeholder.png";
            }}
          />
          <div className="product-info">
            <h4>{product.name || product.Name}</h4>
            <div className="price-per-kg">
              {hasDiscount && (
                <span className="original-price">
                  ₺{basePrice.toFixed(2)}/kg
                </span>
              )}
              <span className="current-price">₺{pricePerKg.toFixed(2)}/kg</span>
            </div>
          </div>
        </div>

        {/* Miktar Seçici */}
        <div className="weight-modal-selector">
          {/* Buton Kontrolleri */}
          <div className="weight-controls">
            <button
              className="weight-btn decrease"
              onClick={decreaseWeight}
              disabled={selectedWeight <= minWeight}
            >
              <i className="fas fa-minus"></i>
            </button>

            <div className="weight-display">
              <span className="weight-value">{selectedWeight.toFixed(1)}</span>
              <span className="weight-unit">kg</span>
            </div>

            <button
              className="weight-btn increase"
              onClick={increaseWeight}
              disabled={selectedWeight >= maxWeight}
            >
              <i className="fas fa-plus"></i>
            </button>
          </div>

          {/* Slider */}
          <div className="weight-slider-container">
            <input
              type="range"
              min={minWeight}
              max={maxWeight}
              step={step}
              value={selectedWeight}
              onChange={handleSliderChange}
              className="weight-slider"
            />
            <div className="slider-labels">
              <span>{minWeight} kg</span>
              <span>{maxWeight} kg</span>
            </div>
          </div>

          {/* Hızlı Seçim Butonları */}
          <div className="quick-select-buttons">
            {[0.5, 1, 1.5, 2, 3, 5].map((weight) => (
              <button
                key={weight}
                className={`quick-btn ${selectedWeight === weight ? "active" : ""}`}
                onClick={() => setSelectedWeight(weight)}
                disabled={weight > maxWeight}
              >
                {weight} kg
              </button>
            ))}
          </div>
        </div>

        {/* Fiyat Özeti */}
        <div className="weight-modal-summary">
          <div className="summary-row">
            <span>Seçilen Miktar:</span>
            <span className="summary-value">
              {selectedWeight.toFixed(1)} kg
            </span>
          </div>
          {originalTotal && (
            <div className="summary-row original">
              <span>Normal Fiyat:</span>
              <span className="summary-value line-through">
                ₺{originalTotal.toFixed(2)}
              </span>
            </div>
          )}
          <div className="summary-row total">
            <span>Toplam Tutar:</span>
            <span className="summary-value">₺{totalPrice.toFixed(2)}</span>
          </div>
          <p className="estimate-note">
            <i className="fas fa-info-circle"></i>
            Bu fiyat tahminidir. Gerçek fiyat tartım sonrası belirlenir.
          </p>
        </div>

        {/* Footer Butonlar */}
        <div className="weight-modal-footer">
          <button className="btn-cancel" onClick={handleClose}>
            İptal
          </button>
          <button className="btn-confirm" onClick={handleConfirm}>
            <i className="fas fa-shopping-cart"></i>
            Sepete Ekle
          </button>
        </div>
      </div>
    </div>
  );
}
