// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET KREDİ KARTI FORMU
// Yapı Kredi POSNET entegrasyonu için kredi kartı giriş bileşeni
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// ÖZELLİKLER:
// 1. PCI DSS uyumlu maskeleme - Kart numarası görsel olarak gruplandırılır
// 2. Luhn algoritması ile kart doğrulama
// 3. Taksit seçimi - Dinamik taksit seçenekleri
// 4. World Puan entegrasyonu - Puan sorgulama ve kullanma
// 5. 3D Secure desteği - Otomatik yönlendirme
// 6. Kart tipi tespiti - VISA, Mastercard, Amex
// ═══════════════════════════════════════════════════════════════════════════════════════════════

import React, { useState, useCallback, useEffect, useMemo } from "react";
import PropTypes from "prop-types";
import { PaymentService } from "../../services/paymentService";
import CreditCardPreview from "./CreditCardPreview";
import "./PosnetCreditCardForm.css";

// ═══════════════════════════════════════════════════════════════════════════
// KART TİPİ TESPİTİ
// BIN numarasına göre kart markasını belirler
// ═══════════════════════════════════════════════════════════════════════════
const detectCardType = (cardNumber) => {
  const cleanNumber = cardNumber?.replace(/\s/g, "") || "";

  // VISA: 4 ile başlar
  if (/^4/.test(cleanNumber)) {
    return { type: "visa", name: "VISA", icon: "💳", color: "#1A1F71" };
  }

  // Mastercard: 51-55 veya 2221-2720 ile başlar
  if (/^5[1-5]/.test(cleanNumber) || /^2[2-7]/.test(cleanNumber)) {
    return {
      type: "mastercard",
      name: "Mastercard",
      icon: "💳",
      color: "#EB001B",
    };
  }

  // American Express: 34 veya 37 ile başlar
  if (/^3[47]/.test(cleanNumber)) {
    return {
      type: "amex",
      name: "American Express",
      icon: "💳",
      color: "#006FCF",
    };
  }

  // Troy (Türkiye): 9792 ile başlar
  if (/^9792/.test(cleanNumber)) {
    return { type: "troy", name: "TROY", icon: "🇹🇷", color: "#00A651" };
  }

  // Bilinmeyen
  return { type: "unknown", name: "", icon: "💳", color: "#666" };
};

// ═══════════════════════════════════════════════════════════════════════════
// LUHN ALGORİTMASI
// Kart numarası doğrulama
// ═══════════════════════════════════════════════════════════════════════════
const luhnCheck = (cardNumber) => {
  const cleanNumber = cardNumber?.replace(/\s/g, "") || "";
  if (!/^\d+$/.test(cleanNumber) || cleanNumber.length < 13) {
    return false;
  }

  let sum = 0;
  let isEven = false;

  for (let i = cleanNumber.length - 1; i >= 0; i--) {
    let digit = parseInt(cleanNumber[i], 10);

    if (isEven) {
      digit *= 2;
      if (digit > 9) {
        digit -= 9;
      }
    }

    sum += digit;
    isEven = !isEven;
  }

  return sum % 10 === 0;
};

// ═══════════════════════════════════════════════════════════════════════════
// KART NUMARASI FORMATLAMA
// 4'lü gruplar halinde gösterim
// ═══════════════════════════════════════════════════════════════════════════
const formatCardNumber = (value) => {
  const cleanValue = value?.replace(/\s/g, "").replace(/\D/g, "") || "";
  const groups = cleanValue.match(/.{1,4}/g) || [];
  return groups.join(" ").substring(0, 19); // Max 16 hane + 3 boşluk
};

// ═══════════════════════════════════════════════════════════════════════════
// SON KULLANMA TARİHİ FORMATLAMA
// MM/YY formatı
// ═══════════════════════════════════════════════════════════════════════════
const formatExpiryDate = (value) => {
  const cleanValue = value?.replace(/\D/g, "") || "";

  if (cleanValue.length >= 2) {
    let month = cleanValue.substring(0, 2);
    const monthNum = parseInt(month, 10);

    // Ay validasyonu
    if (monthNum > 12) month = "12";
    if (monthNum < 1 && cleanValue.length >= 2) month = "01";

    const year = cleanValue.substring(2, 4);
    return month + (year ? "/" + year : "");
  }

  return cleanValue;
};

// ═══════════════════════════════════════════════════════════════════════════
// ANA COMPONENT
// ═══════════════════════════════════════════════════════════════════════════
const PosnetCreditCardForm = ({
  amount,
  orderId,
  onSuccess,
  onError,
  onCancel,
  customerEmail,
  customerPhone,
  userId,
  successUrl,
  failUrl,
  showWorldPoints = true,
  disabled = false,
}) => {
  // ─────────────────────────────────────────────────────────────────────────
  // STATE
  // ─────────────────────────────────────────────────────────────────────────
  const [cardNumber, setCardNumber] = useState("");
  const [cardHolderName, setCardHolderName] = useState("");
  const [expiryDate, setExpiryDate] = useState("");
  const [cvv, setCvv] = useState("");
  const [installmentCount, setInstallmentCount] = useState(0);
  const [use3DSecure, setUse3DSecure] = useState(true);
  const [useWorldPoints, setUseWorldPoints] = useState(false);
  const [worldPointsToUse, setWorldPointsToUse] = useState(0);
  const [availableWorldPoints, setAvailableWorldPoints] = useState(0);
  const [pointsAsTL, setPointsAsTL] = useState(0);
  const [isCardFlipped, setIsCardFlipped] = useState(false); // CVV için kart çevirme

  const [installmentOptions, setInstallmentOptions] = useState([]);
  const [loading, setLoading] = useState(false);
  const [pointsLoading, setPointsLoading] = useState(false);
  const [errors, setErrors] = useState({});
  const [touched, setTouched] = useState({});

  // ─────────────────────────────────────────────────────────────────────────
  // KART TİPİ
  // ─────────────────────────────────────────────────────────────────────────
  const cardType = useMemo(() => detectCardType(cardNumber), [cardNumber]);

  // ─────────────────────────────────────────────────────────────────────────
  // TAKSİT SEÇENEKLERİNİ YÜKLE
  // ─────────────────────────────────────────────────────────────────────────
  useEffect(() => {
    const loadInstallments = async () => {
      if (cardNumber.replace(/\s/g, "").length >= 6 && amount > 0) {
        const cardBin = cardNumber.replace(/\s/g, "").substring(0, 6);
        try {
          const options = await PaymentService.getInstallmentOptions(
            cardBin,
            amount,
          );
          setInstallmentOptions(options);
        } catch (error) {
          console.error("Taksit seçenekleri yüklenemedi:", error);
          // Varsayılan taksit seçenekleri
          setInstallmentOptions([
            {
              count: 0,
              label: "Tek Çekim",
              monthlyAmount: amount,
              totalAmount: amount,
            },
          ]);
        }
      }
    };

    loadInstallments();
  }, [cardNumber, amount]);

  // ─────────────────────────────────────────────────────────────────────────
  // WORLD PUAN SORGULAMA
  // ─────────────────────────────────────────────────────────────────────────
  const queryWorldPoints = useCallback(async () => {
    const cleanCardNumber = cardNumber.replace(/\s/g, "");
    const cleanExpiry = expiryDate.replace("/", "");

    if (
      cleanCardNumber.length < 16 ||
      cleanExpiry.length < 4 ||
      cvv.length < 3
    ) {
      return;
    }

    setPointsLoading(true);
    try {
      const result = await PaymentService.queryWorldPoints(
        cleanCardNumber,
        cleanExpiry,
        cvv,
      );
      if (result.success) {
        setAvailableWorldPoints(result.availablePoints || 0);
        setPointsAsTL(result.pointsAsTL || 0);
      }
    } catch (error) {
      console.error("Puan sorgulama hatası:", error);
    } finally {
      setPointsLoading(false);
    }
  }, [cardNumber, expiryDate, cvv]);

  // ─────────────────────────────────────────────────────────────────────────
  // FORM VALİDASYONU
  // ─────────────────────────────────────────────────────────────────────────
  const validateForm = useCallback(() => {
    const newErrors = {};
    const cleanCardNumber = cardNumber.replace(/\s/g, "");
    const cleanExpiry = expiryDate.replace("/", "");

    // Kart numarası
    if (!cleanCardNumber) {
      newErrors.cardNumber = "Kart numarası gerekli";
    } else if (cleanCardNumber.length < 15 || cleanCardNumber.length > 16) {
      newErrors.cardNumber = "Kart numarası 15-16 hane olmalı";
    } else if (!luhnCheck(cleanCardNumber)) {
      newErrors.cardNumber = "Geçersiz kart numarası";
    }

    // Kart sahibi
    if (!cardHolderName || cardHolderName.trim().length < 3) {
      newErrors.cardHolderName = "Kart sahibi adı gerekli";
    }

    // Son kullanma tarihi
    if (!cleanExpiry || cleanExpiry.length !== 4) {
      newErrors.expiryDate = "Geçerli bir son kullanma tarihi girin";
    } else {
      const month = parseInt(cleanExpiry.substring(0, 2), 10);
      const year = parseInt("20" + cleanExpiry.substring(2, 4), 10);
      const now = new Date();
      const currentYear = now.getFullYear();
      const currentMonth = now.getMonth() + 1;

      if (month < 1 || month > 12) {
        newErrors.expiryDate = "Geçersiz ay";
      } else if (
        year < currentYear ||
        (year === currentYear && month < currentMonth)
      ) {
        newErrors.expiryDate = "Kartın süresi dolmuş";
      }
    }

    // CVV
    const cvvLength = cardType.type === "amex" ? 4 : 3;
    if (!cvv || cvv.length !== cvvLength) {
      newErrors.cvv = `CVV ${cvvLength} hane olmalı`;
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }, [cardNumber, cardHolderName, expiryDate, cvv, cardType]);

  // ─────────────────────────────────────────────────────────────────────────
  // ÖDEME BAŞLAT
  // ─────────────────────────────────────────────────────────────────────────
  const handleSubmit = async (e) => {
    e.preventDefault();

    // Tüm alanları dokunulmuş olarak işaretle
    setTouched({
      cardNumber: true,
      cardHolderName: true,
      expiryDate: true,
      cvv: true,
    });

    if (!validateForm()) {
      return;
    }

    setLoading(true);

    try {
      const paymentData = {
        orderId,
        amount,
        cardNumber: cardNumber.replace(/\s/g, ""),
        expireDate: expiryDate.replace("/", ""),
        cvv,
        cardHolderName: cardHolderName.trim(),
        installmentCount,
        use3DSecure,
        useWorldPoints,
        worldPointsToUse: useWorldPoints ? worldPointsToUse : 0,
        customerEmail,
        customerPhone,
        userId,
        successUrl: successUrl || `${process.env.REACT_APP_SITE_URL || (window.location.hostname === 'localhost' ? window.location.origin : 'https://golkoygurme.com.tr')}/checkout/success`,
        failUrl: failUrl || `${process.env.REACT_APP_SITE_URL || (window.location.hostname === 'localhost' ? window.location.origin : 'https://golkoygurme.com.tr')}/checkout/fail`,
      };

      const result = await PaymentService.initiatePosnet3DSecure(paymentData);

      if (result.success) {
        // 3D Secure yönlendirmesi
        if (result.redirectUrl) {
          // Banka sayfasına yönlendir
          window.location.href = result.redirectUrl;
        } else if (result.threeDSecureHtml) {
          // Form submit ile yönlendir - CSP uyumlu
          const container = document.createElement("div");
          container.style.display = "none";
          container.innerHTML = result.threeDSecureHtml;
          document.body.appendChild(container);

          // Form'u bul ve hemen submit et
          const form = container.querySelector("form");
          if (form) {
            // Küçük gecikme ile submit (DOM'un hazır olması için)
            setTimeout(() => {
              form.submit();
            }, 100);
          } else {
            console.error("3D Secure formu bulunamadı");
            setErrors({ submit: "3D Secure formu oluşturulamadı" });
          }
        } else {
          // Direkt başarılı (2D)
          onSuccess && onSuccess(result);
        }
      } else {
        const errorMessage =
          result.error || result.errorMessage || "Ödeme başlatılamadı";
        setErrors({ submit: errorMessage });
        onError && onError(errorMessage);
      }
    } catch (error) {
      console.error("Ödeme hatası:", error);
      const errorMessage =
        error.response?.data?.message ||
        error.message ||
        "Ödeme işlemi başarısız";
      setErrors({ submit: errorMessage });
      onError && onError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  // ─────────────────────────────────────────────────────────────────────────
  // INPUT HANDLERS
  // ─────────────────────────────────────────────────────────────────────────
  const handleCardNumberChange = (e) => {
    const formatted = formatCardNumber(e.target.value);
    setCardNumber(formatted);
  };

  const handleExpiryChange = (e) => {
    const formatted = formatExpiryDate(e.target.value);
    setExpiryDate(formatted);
  };

  const handleCvvChange = (e) => {
    const value = e.target.value.replace(/\D/g, "");
    const maxLength = cardType.type === "amex" ? 4 : 3;
    setCvv(value.substring(0, maxLength));
  };

  const handleBlur = (field) => {
    setTouched((prev) => ({ ...prev, [field]: true }));
    validateForm();
  };

  // ─────────────────────────────────────────────────────────────────────────
  // HESAPLANAN DEĞERLER
  // ─────────────────────────────────────────────────────────────────────────
  const selectedInstallment = installmentOptions.find(
    (opt) => opt.count === installmentCount,
  );
  const finalAmount = selectedInstallment?.totalAmount || amount;
  const monthlyAmount = selectedInstallment?.monthlyAmount || amount;

  // World puan indirimi
  const pointDiscount = useWorldPoints
    ? Math.min(worldPointsToUse / 100, finalAmount)
    : 0;
  const amountToPay = finalAmount - pointDiscount;

  // ─────────────────────────────────────────────────────────────────────────
  // RENDER
  // ─────────────────────────────────────────────────────────────────────────
  return (
    <div className="posnet-credit-card-form">
      {/* Kredi Kartı Önizlemesi */}
      <CreditCardPreview
        cardNumber={cardNumber}
        cardHolderName={cardHolderName}
        expiryDate={expiryDate}
        cvv={cvv}
        isFlipped={isCardFlipped}
      />

      <div className="form-header">
        <h3>💳 Kredi Kartı ile Öde</h3>
        <div
          className="card-type-indicator"
          style={{ backgroundColor: cardType.color }}
        >
          {cardType.icon} {cardType.name}
        </div>
      </div>

      <form onSubmit={handleSubmit}>
        {/* Kart Numarası */}
        <div className="form-group">
          <label htmlFor="cardNumber">Kart Numarası</label>
          <div className="input-wrapper">
            <input
              type="text"
              id="cardNumber"
              name="cardNumber"
              value={cardNumber}
              onChange={handleCardNumberChange}
              onBlur={() => handleBlur("cardNumber")}
              placeholder="1234 5678 9012 3456"
              maxLength={19}
              autoComplete="cc-number"
              disabled={disabled || loading}
              className={touched.cardNumber && errors.cardNumber ? "error" : ""}
            />
            <span className="card-icon">{cardType.icon}</span>
          </div>
          {touched.cardNumber && errors.cardNumber && (
            <span className="error-message">{errors.cardNumber}</span>
          )}
        </div>

        {/* Kart Sahibi */}
        <div className="form-group">
          <label htmlFor="cardHolderName">Kart Üzerindeki İsim</label>
          <input
            type="text"
            id="cardHolderName"
            name="cardHolderName"
            value={cardHolderName}
            onChange={(e) => setCardHolderName(e.target.value.toUpperCase())}
            onBlur={() => handleBlur("cardHolderName")}
            placeholder="AD SOYAD"
            autoComplete="cc-name"
            disabled={disabled || loading}
            className={
              touched.cardHolderName && errors.cardHolderName ? "error" : ""
            }
          />
          {touched.cardHolderName && errors.cardHolderName && (
            <span className="error-message">{errors.cardHolderName}</span>
          )}
        </div>

        {/* Son Kullanma & CVV */}
        <div className="form-row">
          <div className="form-group half">
            <label htmlFor="expiryDate">Son Kullanma</label>
            <input
              type="text"
              id="expiryDate"
              name="expiryDate"
              value={expiryDate}
              onChange={handleExpiryChange}
              onBlur={() => handleBlur("expiryDate")}
              placeholder="AA/YY"
              maxLength={5}
              autoComplete="cc-exp"
              disabled={disabled || loading}
              className={touched.expiryDate && errors.expiryDate ? "error" : ""}
            />
            {touched.expiryDate && errors.expiryDate && (
              <span className="error-message">{errors.expiryDate}</span>
            )}
          </div>

          <div className="form-group half">
            <label htmlFor="cvv">CVV</label>
            <input
              type="password"
              id="cvv"
              name="cvv"
              value={cvv}
              onChange={handleCvvChange}
              onFocus={() => setIsCardFlipped(true)}
              onBlur={() => {
                handleBlur("cvv");
                setIsCardFlipped(false);
              }}
              placeholder={cardType.type === "amex" ? "••••" : "•••"}
              maxLength={cardType.type === "amex" ? 4 : 3}
              autoComplete="cc-csc"
              disabled={disabled || loading}
              className={touched.cvv && errors.cvv ? "error" : ""}
            />
            {touched.cvv && errors.cvv && (
              <span className="error-message">{errors.cvv}</span>
            )}
          </div>
        </div>

        {/* Taksit Seçimi */}
        {installmentOptions.length > 1 && (
          <div className="form-group">
            <label>Taksit Seçenekleri</label>
            <div className="installment-options">
              {installmentOptions.map((option) => (
                <div
                  key={option.count}
                  className={`installment-option ${installmentCount === option.count ? "selected" : ""}`}
                  onClick={() => setInstallmentCount(option.count)}
                >
                  <div className="option-label">{option.label}</div>
                  <div className="option-amount">
                    {option.count > 0 ? (
                      <>
                        <span className="monthly">
                          {option.monthlyAmount.toFixed(2)} ₺/ay
                        </span>
                        <span className="total">
                          Toplam: {option.totalAmount.toFixed(2)} ₺
                        </span>
                      </>
                    ) : (
                      <span className="total">
                        {option.totalAmount.toFixed(2)} ₺
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* World Puan */}
        {showWorldPoints && cardType.type !== "unknown" && (
          <div className="form-group world-points">
            <div className="world-points-header">
              <label>
                <input
                  type="checkbox"
                  checked={useWorldPoints}
                  onChange={(e) => setUseWorldPoints(e.target.checked)}
                  disabled={availableWorldPoints === 0 || disabled || loading}
                />
                <span>🌍 World Puan Kullan</span>
              </label>
              {!pointsLoading && availableWorldPoints === 0 && (
                <button
                  type="button"
                  className="query-points-btn"
                  onClick={queryWorldPoints}
                  disabled={disabled || loading}
                >
                  Puanları Sorgula
                </button>
              )}
            </div>

            {pointsLoading && (
              <div className="points-loading">Puanlar sorgulanıyor...</div>
            )}

            {availableWorldPoints > 0 && (
              <div className="points-info">
                <span>
                  Kullanılabilir: {availableWorldPoints} puan (
                  {pointsAsTL.toFixed(2)} ₺)
                </span>
                {useWorldPoints && (
                  <input
                    type="number"
                    min={0}
                    max={availableWorldPoints}
                    value={worldPointsToUse}
                    onChange={(e) =>
                      setWorldPointsToUse(
                        Math.min(
                          parseInt(e.target.value) || 0,
                          availableWorldPoints,
                        ),
                      )
                    }
                    disabled={disabled || loading}
                  />
                )}
              </div>
            )}
          </div>
        )}

        {/* 3D Secure */}
        <div className="form-group security-option">
          <label>
            <input
              type="checkbox"
              checked={use3DSecure}
              onChange={(e) => setUse3DSecure(e.target.checked)}
              disabled={disabled || loading}
            />
            <span>🔒 3D Secure ile Güvenli Ödeme</span>
          </label>
          <small>Banka onayı ile güvenli ödeme yaparsınız</small>
        </div>

        {/* Ödeme Özeti */}
        <div className="payment-summary">
          <div className="summary-row">
            <span>Ara Toplam:</span>
            <span>{amount.toFixed(2)} ₺</span>
          </div>
          {installmentCount > 0 && (
            <div className="summary-row">
              <span>Taksit Farkı:</span>
              <span>{(finalAmount - amount).toFixed(2)} ₺</span>
            </div>
          )}
          {useWorldPoints && pointDiscount > 0 && (
            <div className="summary-row discount">
              <span>World Puan İndirimi:</span>
              <span>-{pointDiscount.toFixed(2)} ₺</span>
            </div>
          )}
          <div className="summary-row total">
            <span>Ödenecek Tutar:</span>
            <span>{amountToPay.toFixed(2)} ₺</span>
          </div>
          {installmentCount > 0 && (
            <div className="summary-row monthly">
              <span>Aylık Ödeme:</span>
              <span>
                {(monthlyAmount - pointDiscount / installmentCount).toFixed(2)}{" "}
                ₺ x {installmentCount}
              </span>
            </div>
          )}
        </div>

        {/* Hata Mesajı */}
        {errors.submit && (
          <div className="submit-error">❌ {errors.submit}</div>
        )}

        {/* Butonlar */}
        <div className="form-actions">
          {onCancel && (
            <button
              type="button"
              className="cancel-btn"
              onClick={onCancel}
              disabled={loading}
            >
              ← Geri
            </button>
          )}
          <button
            type="submit"
            className="submit-btn"
            disabled={disabled || loading}
          >
            {loading ? (
              <>
                <span className="spinner"></span>
                İşleniyor...
              </>
            ) : (
              <>🔒 {amountToPay.toFixed(2)} ₺ Öde</>
            )}
          </button>
        </div>

        {/* Güvenlik Bilgisi */}
        <div className="security-info">
          <div className="security-badges">
            <span>🔒 256-bit SSL</span>
            <span>✓ 3D Secure</span>
            <span>🏦 Yapı Kredi</span>
          </div>
          <p>
            Kart bilgileriniz güvenli şekilde şifrelenerek iletilir ve
            saklanmaz.
          </p>
        </div>
      </form>
    </div>
  );
};

// ═══════════════════════════════════════════════════════════════════════════
// PROP TYPES
// ═══════════════════════════════════════════════════════════════════════════
PosnetCreditCardForm.propTypes = {
  amount: PropTypes.number.isRequired,
  orderId: PropTypes.number.isRequired,
  onSuccess: PropTypes.func,
  onError: PropTypes.func,
  onCancel: PropTypes.func,
  customerEmail: PropTypes.string,
  customerPhone: PropTypes.string,
  userId: PropTypes.number,
  successUrl: PropTypes.string,
  failUrl: PropTypes.string,
  showWorldPoints: PropTypes.bool,
  disabled: PropTypes.bool,
};

export default PosnetCreditCardForm;
