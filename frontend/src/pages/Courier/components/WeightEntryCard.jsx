// ==========================================================================
// WeightEntryCard.jsx - Ağırlık Giriş Kartı Komponenti
// ==========================================================================
// Kuryelerin ağırlık bazlı ürünleri tartıp gram cinsinden girmeleri için
// mobil uyumlu, dokunmatik ekran dostu bir kart komponenti.
//
// Özellikler:
// - Gram/kg seçimi ile kolay giriş
// - Anlık fark hesaplama ve gösterimi
// - Hızlı numpad ile gram girişi
// - Fiyat farkını otomatik hesaplama
// - Üç aşamalı durum: Tartılmadı → Tartıldı → Onaylandı
// ==========================================================================

import React, { useState, useCallback, useMemo } from "react";
import PropTypes from "prop-types";

// Birim enum değerlerini Türkçe'ye çevir
const UNIT_LABELS = {
  Gram: "g",
  Kilogram: "kg",
  Piece: "adet",
  Liter: "lt",
  Milliliter: "ml",
};

// Birim için placeholder
const UNIT_PLACEHOLDERS = {
  Gram: "Gram girin",
  Kilogram: "Kg girin",
  Piece: "Adet girin",
  Liter: "Litre girin",
  Milliliter: "Ml girin",
};

/**
 * WeightEntryCard - Tek bir ürün için ağırlık giriş kartı
 *
 * @param {Object} item - Sipariş kalemi bilgisi
 * @param {Function} onWeightSubmit - Ağırlık gönderildiğinde çağrılacak callback
 * @param {boolean} disabled - Kart devre dışı mı?
 * @param {boolean} loading - Yükleniyor durumu
 */
export default function WeightEntryCard({
  item,
  onWeightSubmit,
  disabled = false,
  loading = false,
}) {
  // =========================================================================
  // STATE
  // =========================================================================
  const [inputValue, setInputValue] = useState("");
  const [inputUnit, setInputUnit] = useState(item?.weightUnit || "Gram");
  const [isEditing, setIsEditing] = useState(false);
  const [showNumpad, setShowNumpad] = useState(false);
  const [localError, setLocalError] = useState("");

  // =========================================================================
  // HESAPLAMALAR
  // =========================================================================

  /**
   * Girilen değeri gram cinsine çevir
   * Tüm hesaplamalar gram üzerinden yapılır
   */
  const convertToGrams = useCallback((value, unit) => {
    const numValue = parseFloat(value) || 0;
    switch (unit) {
      case "Kilogram":
        return numValue * 1000;
      case "Gram":
      default:
        return numValue;
    }
  }, []);

  /**
   * Gram değerini belirtilen birime çevir
   */
  const convertFromGrams = useCallback((grams, unit) => {
    switch (unit) {
      case "Kilogram":
        return grams / 1000;
      case "Gram":
      default:
        return grams;
    }
  }, []);

  /**
   * Tahmini miktar (gram cinsinden)
   */
  const estimatedGrams = useMemo(() => {
    return item?.estimatedQuantity || item?.estimatedWeightGrams || 0;
  }, [item]);

  /**
   * Gerçek miktar (gram cinsinden) - daha önce tartılmışsa
   */
  const actualGrams = useMemo(() => {
    return item?.actualQuantity || item?.actualWeightGrams || 0;
  }, [item]);

  /**
   * Şu anki giriş değeri (gram cinsinden)
   */
  const currentInputGrams = useMemo(() => {
    return convertToGrams(inputValue, inputUnit);
  }, [inputValue, inputUnit, convertToGrams]);

  /**
   * Fark hesaplama (gram cinsinden)
   * Pozitif: Fazla geldi
   * Negatif: Az geldi
   */
  const weightDifferenceGrams = useMemo(() => {
    if (!inputValue || currentInputGrams === 0) return 0;
    return currentInputGrams - estimatedGrams;
  }, [currentInputGrams, estimatedGrams, inputValue]);

  /**
   * Fark yüzdesi
   */
  const differencePercent = useMemo(() => {
    if (estimatedGrams === 0) return 0;
    return (weightDifferenceGrams / estimatedGrams) * 100;
  }, [weightDifferenceGrams, estimatedGrams]);

  /**
   * Birim fiyat (TL/gram)
   */
  const pricePerGram = useMemo(() => {
    if (!item?.estimatedPrice || estimatedGrams === 0) return 0;
    return item.estimatedPrice / estimatedGrams;
  }, [item, estimatedGrams]);

  /**
   * Fark tutarı (TL)
   * Pozitif: Ek ödeme alınacak
   * Negatif: İade yapılacak
   */
  const priceDifference = useMemo(() => {
    return weightDifferenceGrams * pricePerGram;
  }, [weightDifferenceGrams, pricePerGram]);

  /**
   * Ürün daha önce tartılmış mı?
   */
  const isWeighed = useMemo(() => {
    return item?.isWeighed || actualGrams > 0;
  }, [item, actualGrams]);

  // =========================================================================
  // EVENT HANDLERS
  // =========================================================================

  /**
   * Input değişikliği - sadece sayı ve nokta kabul et
   */
  const handleInputChange = (e) => {
    const value = e.target.value;
    // Sadece sayı ve nokta kabul et, maksimum bir nokta
    if (/^\d*\.?\d*$/.test(value)) {
      setInputValue(value);
      setLocalError("");
    }
  };

  /**
   * Numpad tuş basımı
   */
  const handleNumpadPress = (key) => {
    if (key === "C") {
      setInputValue("");
    } else if (key === "⌫") {
      setInputValue((prev) => prev.slice(0, -1));
    } else if (key === ".") {
      if (!inputValue.includes(".")) {
        setInputValue((prev) => prev + ".");
      }
    } else {
      setInputValue((prev) => prev + key);
    }
  };

  /**
   * Hızlı değer butonları (±50g, ±100g vb.)
   */
  const handleQuickAdjust = (adjustmentGrams) => {
    const currentGrams = currentInputGrams || estimatedGrams;
    const newGrams = Math.max(0, currentGrams + adjustmentGrams);
    setInputValue(convertFromGrams(newGrams, inputUnit).toString());
  };

  /**
   * Tahmini değeri kopyala
   */
  const handleCopyEstimated = () => {
    const valueInUnit = convertFromGrams(estimatedGrams, inputUnit);
    setInputValue(valueInUnit.toString());
  };

  /**
   * Ağırlık gönder
   */
  const handleSubmit = async () => {
    // Validasyon
    if (!inputValue || currentInputGrams <= 0) {
      setLocalError("Lütfen geçerli bir değer girin");
      return;
    }

    // Min/Max kontrolü
    if (item?.minWeight && currentInputGrams < item.minWeight) {
      setLocalError(`Minimum ağırlık: ${item.minWeight}g`);
      return;
    }
    if (item?.maxWeight && currentInputGrams > item.maxWeight) {
      setLocalError(`Maksimum ağırlık: ${item.maxWeight}g`);
      return;
    }

    // Aşırı fark uyarısı (±%30 üzeri)
    if (Math.abs(differencePercent) > 30) {
      const confirmMessage =
        differencePercent > 0
          ? `Ürün tahminden %${differencePercent.toFixed(0)} FAZLA. Devam etmek istiyor musunuz?`
          : `Ürün tahminden %${Math.abs(differencePercent).toFixed(0)} AZ. Devam etmek istiyor musunuz?`;

      if (!window.confirm(confirmMessage)) {
        return;
      }
    }

    // Callback'i çağır
    try {
      await onWeightSubmit({
        orderItemId: item.id || item.orderItemId,
        actualWeightGrams: currentInputGrams,
        notes: `Kurye tartısı: ${inputValue} ${UNIT_LABELS[inputUnit]}`,
      });
      setIsEditing(false);
      setShowNumpad(false);
    } catch (error) {
      setLocalError(error.message || "Kaydetme hatası");
    }
  };

  // =========================================================================
  // RENDER HELPERS
  // =========================================================================

  /**
   * Fark badge'i rengi
   */
  const getDifferenceBadgeClass = () => {
    if (weightDifferenceGrams === 0) return "bg-secondary";
    if (weightDifferenceGrams > 0) return "bg-success"; // Fazla = müşteriden ek ödeme
    return "bg-warning text-dark"; // Az = iade
  };

  /**
   * Durum badge'i
   */
  const getStatusBadge = () => {
    if (isWeighed) {
      return (
        <span className="badge bg-success">
          <i className="fas fa-check me-1"></i>Tartıldı
        </span>
      );
    }
    return (
      <span className="badge bg-warning text-dark">
        <i className="fas fa-clock me-1"></i>Bekliyor
      </span>
    );
  };

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <div
      className={`weight-entry-card ${isWeighed ? "weighed" : ""} ${disabled ? "disabled" : ""}`}
      style={{
        background: "white",
        borderRadius: "16px",
        padding: "16px",
        marginBottom: "12px",
        boxShadow: isEditing
          ? "0 4px 20px rgba(255, 107, 53, 0.3)"
          : "0 2px 8px rgba(0,0,0,0.08)",
        border: isEditing ? "2px solid #ff6b35" : "1px solid #e9ecef",
        transition: "all 0.3s ease",
        opacity: disabled ? 0.6 : 1,
        pointerEvents: disabled ? "none" : "auto",
      }}
    >
      {/* Header: Ürün bilgisi ve durum */}
      <div className="d-flex justify-content-between align-items-start mb-3">
        <div className="d-flex align-items-center flex-grow-1">
          {item.imageUrl && (
            <img
              src={item.imageUrl}
              alt={item.productName}
              className="rounded me-3"
              style={{ width: "56px", height: "56px", objectFit: "cover" }}
            />
          )}
          <div>
            <h6 className="mb-1 fw-bold" style={{ fontSize: "15px" }}>
              {item.productName || item.name}
            </h6>
            <div className="d-flex align-items-center gap-2">
              <span className="text-muted" style={{ fontSize: "13px" }}>
                Tahmini:{" "}
                <strong>
                  {convertFromGrams(estimatedGrams, inputUnit).toFixed(
                    inputUnit === "Kilogram" ? 2 : 0,
                  )}{" "}
                  {UNIT_LABELS[inputUnit]}
                </strong>
              </span>
              {getStatusBadge()}
            </div>
          </div>
        </div>

        {/* Birim fiyatı */}
        <div className="text-end">
          <div className="text-muted" style={{ fontSize: "11px" }}>
            Birim Fiyat
          </div>
          <div
            className="fw-bold"
            style={{ color: "#ff6b35", fontSize: "14px" }}
          >
            {(pricePerGram * 1000).toFixed(2)} ₺/kg
          </div>
        </div>
      </div>

      {/* Daha önce tartılmışsa sonucu göster */}
      {isWeighed && !isEditing && (
        <div
          className="d-flex justify-content-between align-items-center p-3 rounded mb-3"
          style={{ backgroundColor: "#e8f5e9" }}
        >
          <div>
            <div className="text-muted" style={{ fontSize: "12px" }}>
              Tartılan Ağırlık
            </div>
            <div
              className="fw-bold"
              style={{ fontSize: "18px", color: "#2e7d32" }}
            >
              {convertFromGrams(actualGrams, inputUnit).toFixed(
                inputUnit === "Kilogram" ? 2 : 0,
              )}{" "}
              {UNIT_LABELS[inputUnit]}
            </div>
          </div>
          <div className="text-end">
            <div className="text-muted" style={{ fontSize: "12px" }}>
              Fark
            </div>
            <span
              className={`badge ${getDifferenceBadgeClass()}`}
              style={{ fontSize: "14px" }}
            >
              {actualGrams - estimatedGrams >= 0 ? "+" : ""}
              {convertFromGrams(
                actualGrams - estimatedGrams,
                inputUnit,
              ).toFixed(inputUnit === "Kilogram" ? 2 : 0)}{" "}
              {UNIT_LABELS[inputUnit]}
            </span>
          </div>
          <button
            className="btn btn-outline-primary btn-sm ms-2"
            onClick={() => {
              setIsEditing(true);
              setInputValue(
                convertFromGrams(actualGrams, inputUnit).toString(),
              );
            }}
          >
            <i className="fas fa-edit"></i>
          </button>
        </div>
      )}

      {/* Ağırlık Giriş Alanı */}
      {(!isWeighed || isEditing) && (
        <>
          {/* Birim Seçimi */}
          <div className="d-flex gap-2 mb-3">
            <button
              className={`btn btn-sm flex-grow-1 ${inputUnit === "Gram" ? "btn-primary" : "btn-outline-secondary"}`}
              onClick={() => setInputUnit("Gram")}
              style={{ borderRadius: "8px" }}
            >
              Gram (g)
            </button>
            <button
              className={`btn btn-sm flex-grow-1 ${inputUnit === "Kilogram" ? "btn-primary" : "btn-outline-secondary"}`}
              onClick={() => setInputUnit("Kilogram")}
              style={{ borderRadius: "8px" }}
            >
              Kilogram (kg)
            </button>
          </div>

          {/* Input Alanı */}
          <div className="position-relative mb-3">
            <input
              type="text"
              inputMode="decimal"
              className={`form-control form-control-lg text-center ${localError ? "is-invalid" : ""}`}
              placeholder={UNIT_PLACEHOLDERS[inputUnit]}
              value={inputValue}
              onChange={handleInputChange}
              onFocus={() => setShowNumpad(true)}
              style={{
                fontSize: "24px",
                fontWeight: "bold",
                borderRadius: "12px",
                padding: "16px",
                backgroundColor: "#f8f9fa",
              }}
              disabled={loading}
            />
            <span
              className="position-absolute"
              style={{
                right: "16px",
                top: "50%",
                transform: "translateY(-50%)",
                fontSize: "18px",
                color: "#6c757d",
              }}
            >
              {UNIT_LABELS[inputUnit]}
            </span>
            {localError && <div className="invalid-feedback">{localError}</div>}
          </div>

          {/* Hızlı Ayar Butonları */}
          <div className="d-flex gap-2 mb-3 flex-wrap">
            <button
              className="btn btn-outline-secondary btn-sm"
              onClick={handleCopyEstimated}
              style={{ borderRadius: "20px" }}
            >
              <i className="fas fa-copy me-1"></i>Tahminiyi Kopyala
            </button>
            <button
              className="btn btn-outline-danger btn-sm"
              onClick={() => handleQuickAdjust(-50)}
              style={{ borderRadius: "20px" }}
            >
              -50g
            </button>
            <button
              className="btn btn-outline-success btn-sm"
              onClick={() => handleQuickAdjust(50)}
              style={{ borderRadius: "20px" }}
            >
              +50g
            </button>
            <button
              className="btn btn-outline-danger btn-sm"
              onClick={() => handleQuickAdjust(-100)}
              style={{ borderRadius: "20px" }}
            >
              -100g
            </button>
            <button
              className="btn btn-outline-success btn-sm"
              onClick={() => handleQuickAdjust(100)}
              style={{ borderRadius: "20px" }}
            >
              +100g
            </button>
          </div>

          {/* Anlık Fark Hesabı */}
          {inputValue && currentInputGrams > 0 && (
            <div
              className="p-3 rounded mb-3"
              style={{
                backgroundColor:
                  weightDifferenceGrams >= 0 ? "#e8f5e9" : "#fff3e0",
                border: `1px solid ${weightDifferenceGrams >= 0 ? "#a5d6a7" : "#ffcc80"}`,
              }}
            >
              <div className="row g-2 text-center">
                <div className="col-4">
                  <div className="text-muted" style={{ fontSize: "11px" }}>
                    Tahmini
                  </div>
                  <div className="fw-bold">
                    {convertFromGrams(estimatedGrams, inputUnit).toFixed(
                      inputUnit === "Kilogram" ? 2 : 0,
                    )}{" "}
                    {UNIT_LABELS[inputUnit]}
                  </div>
                </div>
                <div className="col-4">
                  <div className="text-muted" style={{ fontSize: "11px" }}>
                    Gerçek
                  </div>
                  <div className="fw-bold" style={{ color: "#ff6b35" }}>
                    {convertFromGrams(currentInputGrams, inputUnit).toFixed(
                      inputUnit === "Kilogram" ? 2 : 0,
                    )}{" "}
                    {UNIT_LABELS[inputUnit]}
                  </div>
                </div>
                <div className="col-4">
                  <div className="text-muted" style={{ fontSize: "11px" }}>
                    Fark
                  </div>
                  <div
                    className={`fw-bold ${weightDifferenceGrams >= 0 ? "text-success" : "text-warning"}`}
                  >
                    {weightDifferenceGrams >= 0 ? "+" : ""}
                    {convertFromGrams(weightDifferenceGrams, inputUnit).toFixed(
                      inputUnit === "Kilogram" ? 2 : 0,
                    )}{" "}
                    {UNIT_LABELS[inputUnit]}
                    <small className="d-block">
                      (%{differencePercent.toFixed(1)})
                    </small>
                  </div>
                </div>
              </div>

              {/* Fiyat Farkı */}
              <hr className="my-2" />
              <div className="text-center">
                <span className="text-muted me-2">Fiyat Farkı:</span>
                <span
                  className={`badge ${priceDifference >= 0 ? "bg-success" : "bg-warning text-dark"}`}
                  style={{ fontSize: "16px" }}
                >
                  {priceDifference >= 0 ? "+" : ""}
                  {priceDifference.toFixed(2)} ₺
                </span>
                <div className="text-muted mt-1" style={{ fontSize: "11px" }}>
                  {priceDifference >= 0
                    ? "Müşteriden ek ödeme alınacak"
                    : "Müşteriye iade yapılacak"}
                </div>
              </div>
            </div>
          )}

          {/* Numpad */}
          {showNumpad && (
            <div className="numpad-container mb-3">
              <div className="row g-2">
                {[
                  "1",
                  "2",
                  "3",
                  "4",
                  "5",
                  "6",
                  "7",
                  "8",
                  "9",
                  ".",
                  "0",
                  "⌫",
                ].map((key) => (
                  <div key={key} className="col-4">
                    <button
                      className={`btn w-100 ${key === "⌫" ? "btn-outline-danger" : "btn-outline-secondary"}`}
                      onClick={() => handleNumpadPress(key)}
                      style={{
                        height: "48px",
                        fontSize: "18px",
                        borderRadius: "10px",
                      }}
                    >
                      {key}
                    </button>
                  </div>
                ))}
              </div>
              <button
                className="btn btn-outline-secondary w-100 mt-2"
                onClick={() => handleNumpadPress("C")}
                style={{ borderRadius: "10px" }}
              >
                <i className="fas fa-times me-2"></i>Temizle
              </button>
            </div>
          )}

          {/* Kaydet Butonu */}
          <button
            className="btn btn-primary w-100"
            onClick={handleSubmit}
            disabled={loading || !inputValue}
            style={{
              borderRadius: "12px",
              padding: "14px",
              fontSize: "16px",
              fontWeight: "600",
              background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
            }}
          >
            {loading ? (
              <>
                <span className="spinner-border spinner-border-sm me-2"></span>
                Kaydediliyor...
              </>
            ) : (
              <>
                <i className="fas fa-balance-scale me-2"></i>
                Tartıyı Kaydet
              </>
            )}
          </button>

          {/* İptal butonu (düzenleme modundaysa) */}
          {isEditing && (
            <button
              className="btn btn-outline-secondary w-100 mt-2"
              onClick={() => {
                setIsEditing(false);
                setInputValue("");
                setShowNumpad(false);
              }}
              style={{ borderRadius: "12px" }}
            >
              <i className="fas fa-times me-2"></i>İptal
            </button>
          )}
        </>
      )}
    </div>
  );
}

WeightEntryCard.propTypes = {
  item: PropTypes.shape({
    id: PropTypes.number,
    orderItemId: PropTypes.number,
    productName: PropTypes.string,
    name: PropTypes.string,
    imageUrl: PropTypes.string,
    weightUnit: PropTypes.string,
    estimatedQuantity: PropTypes.number,
    estimatedWeightGrams: PropTypes.number,
    estimatedPrice: PropTypes.number,
    actualQuantity: PropTypes.number,
    actualWeightGrams: PropTypes.number,
    isWeighed: PropTypes.bool,
    minWeight: PropTypes.number,
    maxWeight: PropTypes.number,
  }).isRequired,
  onWeightSubmit: PropTypes.func.isRequired,
  disabled: PropTypes.bool,
  loading: PropTypes.bool,
};
