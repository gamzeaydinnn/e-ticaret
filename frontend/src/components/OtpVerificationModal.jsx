// src/components/OtpVerificationModal.jsx
// SMS doğrulama modal komponenti
// Kayıt, şifre sıfırlama ve telefon değişikliği için kullanılır

import React, { useState, useEffect, useRef, useCallback } from "react";
import { smsService, SmsVerificationPurpose } from "../services/otpService";

/**
 * OTP Doğrulama Modal Komponenti
 * 
 * @param {boolean} show - Modal görünür mü?
 * @param {function} onHide - Modal kapatıldığında çağrılır
 * @param {function} onVerified - Doğrulama başarılı olduğunda çağrılır
 * @param {string} phoneNumber - Doğrulanacak telefon numarası
 * @param {string} email - Email adresi (kayıt için gerekli)
 * @param {number} purpose - Doğrulama amacı (SmsVerificationPurpose)
 * @param {string} title - Modal başlığı
 * @param {boolean} autoSendOnShow - Modal açıldığında otomatik SMS gönder
 */
const OtpVerificationModal = ({
  show,
  onHide,
  onVerified,
  phoneNumber,
  email = "",
  purpose = SmsVerificationPurpose.Registration,
  title = "Telefon Doğrulama",
  autoSendOnShow = true,
}) => {
  // State'ler
  const [otpDigits, setOtpDigits] = useState(["", "", "", "", "", ""]);
  const [loading, setLoading] = useState(false);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [countdown, setCountdown] = useState(0);
  const [expiryCountdown, setExpiryCountdown] = useState(0);
  const [remainingAttempts, setRemainingAttempts] = useState(3);
  const [otpSent, setOtpSent] = useState(false);

  // Input ref'leri (6 haneli kod için)
  const inputRefs = useRef([]);

  // Geri sayım timer'ları
  const resendTimerRef = useRef(null);
  const expiryTimerRef = useRef(null);

  // Timer'ları temizle
  const clearTimers = useCallback(() => {
    if (resendTimerRef.current) {
      clearInterval(resendTimerRef.current);
      resendTimerRef.current = null;
    }
    if (expiryTimerRef.current) {
      clearInterval(expiryTimerRef.current);
      expiryTimerRef.current = null;
    }
  }, []);

  // Resend countdown başlat
  const startResendCountdown = useCallback((seconds) => {
    setCountdown(seconds);
    resendTimerRef.current = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          clearInterval(resendTimerRef.current);
          resendTimerRef.current = null;
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  }, []);

  // Expiry countdown başlat (3 dakika)
  const startExpiryCountdown = useCallback((seconds) => {
    setExpiryCountdown(seconds);
    expiryTimerRef.current = setInterval(() => {
      setExpiryCountdown((prev) => {
        if (prev <= 1) {
          clearInterval(expiryTimerRef.current);
          expiryTimerRef.current = null;
          setError("Kodun süresi doldu. Lütfen yeni kod isteyin.");
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  }, []);

  // OTP Gönder
  const handleSendOtp = useCallback(async () => {
    if (!phoneNumber) {
      setError("Telefon numarası gerekli");
      return;
    }

    setSending(true);
    setError("");
    setSuccess("");

    try {
      const result = await smsService.sendOtp(phoneNumber, purpose);

      if (result.success) {
        setOtpSent(true);
        setSuccess("Doğrulama kodu telefonunuza gönderildi.");
        setRemainingAttempts(3);
        
        // Resend countdown (60 saniye)
        startResendCountdown(60);
        
        // Expiry countdown (varsayılan 180 saniye = 3 dakika)
        startExpiryCountdown(result.expiresInSeconds || 180);
        
        // İlk input'a focus
        setTimeout(() => {
          inputRefs.current[0]?.focus();
        }, 100);
      } else {
        setError(result.message);
        if (result.retryAfterSeconds) {
          startResendCountdown(result.retryAfterSeconds);
        }
      }
    } catch (err) {
      setError("SMS gönderilirken bir hata oluştu.");
    } finally {
      setSending(false);
    }
  }, [phoneNumber, purpose, startResendCountdown, startExpiryCountdown]);

  // Modal açıldığında otomatik OTP gönder
  useEffect(() => {
    if (show && autoSendOnShow && phoneNumber && !otpSent) {
      handleSendOtp();
    }
    
    // Modal kapandığında temizle
    if (!show) {
      clearTimers();
      setOtpDigits(["", "", "", "", "", ""]);
      setError("");
      setSuccess("");
      setOtpSent(false);
    }
  }, [show, autoSendOnShow, phoneNumber, otpSent, handleSendOtp, clearTimers]);

  // Component unmount olduğunda temizle
  useEffect(() => {
    return () => clearTimers();
  }, [clearTimers]);

  // OTP Doğrula
  const handleVerifyOtp = async () => {
    const code = otpDigits.join("");
    
    if (code.length !== 6) {
      setError("6 haneli kodu eksiksiz girin");
      return;
    }

    setLoading(true);
    setError("");

    try {
      let result;

      // Purpose'a göre farklı endpoint kullan
      if (purpose === SmsVerificationPurpose.Registration && email) {
        // Kayıt doğrulama
        result = await smsService.verifyPhoneRegistration(phoneNumber, code, email);
      } else {
        // Genel OTP doğrulama
        result = await smsService.verifyOtp(phoneNumber, code, purpose);
      }

      if (result.success) {
        setSuccess(result.message || "Doğrulama başarılı!");
        clearTimers();
        
        // Parent'a bildir
        setTimeout(() => {
          onVerified && onVerified({
            phoneNumber,
            token: result.token,
            refreshToken: result.refreshToken,
          });
        }, 500);
      } else {
        setError(result.message);
        
        if (result.remainingAttempts !== undefined) {
          setRemainingAttempts(result.remainingAttempts);
        }
        
        // Input'ları temizle
        setOtpDigits(["", "", "", "", "", ""]);
        inputRefs.current[0]?.focus();
      }
    } catch (err) {
      setError("Doğrulama sırasında bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  };

  // Tek bir digit değiştiğinde
  const handleDigitChange = (index, value) => {
    // Sadece rakam kabul et
    if (!/^\d*$/.test(value)) return;

    const newDigits = [...otpDigits];
    
    // Paste durumu (6 haneli kod yapıştırıldı)
    if (value.length > 1) {
      const pastedDigits = value.slice(0, 6).split("");
      for (let i = 0; i < 6; i++) {
        newDigits[i] = pastedDigits[i] || "";
      }
      setOtpDigits(newDigits);
      
      // Son dolu input'a focus
      const lastIndex = Math.min(value.length - 1, 5);
      inputRefs.current[lastIndex]?.focus();
      return;
    }

    // Tek karakter girişi
    newDigits[index] = value;
    setOtpDigits(newDigits);

    // Otomatik sonraki input'a geç
    if (value && index < 5) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  // Backspace ile önceki input'a geç
  const handleKeyDown = (index, e) => {
    if (e.key === "Backspace" && !otpDigits[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
    
    // Enter ile doğrula
    if (e.key === "Enter" && otpDigits.join("").length === 6) {
      handleVerifyOtp();
    }
  };

  // Süreyi formatla (mm:ss)
  const formatTime = (seconds) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, "0")}`;
  };

  // Modal görünmüyorsa render etme
  if (!show) return null;

  return (
    <div className="modal-overlay" style={styles.overlay}>
      <div className="modal-content" style={styles.modal}>
        {/* Başlık */}
        <div style={styles.header}>
          <h2 style={styles.title}>{title}</h2>
          <button onClick={onHide} style={styles.closeBtn}>×</button>
        </div>

        {/* İçerik */}
        <div style={styles.body}>
          {/* Telefon numarası göster */}
          <p style={styles.phoneInfo}>
            <span style={styles.phoneIcon}>📱</span>
            <strong>{phoneNumber}</strong> numarasına doğrulama kodu gönderildi.
          </p>

          {/* Süre bilgisi */}
          {expiryCountdown > 0 && (
            <p style={styles.expiryInfo}>
              Kod geçerlilik süresi: <strong>{formatTime(expiryCountdown)}</strong>
            </p>
          )}

          {/* 6 Haneli OTP Input'ları */}
          <div style={styles.otpContainer}>
            {otpDigits.map((digit, index) => (
              <input
                key={index}
                ref={(el) => (inputRefs.current[index] = el)}
                type="text"
                inputMode="numeric"
                maxLength={6} // Paste için
                value={digit}
                onChange={(e) => handleDigitChange(index, e.target.value)}
                onKeyDown={(e) => handleKeyDown(index, e)}
                style={{
                  ...styles.otpInput,
                  borderColor: error ? "#dc3545" : digit ? "#28a745" : "#ced4da",
                }}
                disabled={loading}
                autoFocus={index === 0}
              />
            ))}
          </div>

          {/* Kalan deneme hakkı */}
          {remainingAttempts < 3 && remainingAttempts > 0 && (
            <p style={styles.attemptsInfo}>
              Kalan deneme hakkı: <strong>{remainingAttempts}</strong>
            </p>
          )}

          {/* Hata mesajı */}
          {error && (
            <div style={styles.errorBox}>
              <span style={styles.errorIcon}>⚠️</span> {error}
            </div>
          )}

          {/* Başarı mesajı */}
          {success && !error && (
            <div style={styles.successBox}>
              <span style={styles.successIcon}>✓</span> {success}
            </div>
          )}

          {/* Doğrula butonu */}
          <button
            onClick={handleVerifyOtp}
            disabled={loading || otpDigits.join("").length !== 6}
            style={{
              ...styles.verifyBtn,
              opacity: loading || otpDigits.join("").length !== 6 ? 0.6 : 1,
            }}
          >
            {loading ? "Doğrulanıyor..." : "Doğrula"}
          </button>

          {/* Tekrar gönder */}
          <div style={styles.resendContainer}>
            {countdown > 0 ? (
              <p style={styles.resendWait}>
                Tekrar göndermek için <strong>{countdown}</strong> saniye bekleyin
              </p>
            ) : (
              <button
                onClick={handleSendOtp}
                disabled={sending}
                style={styles.resendBtn}
              >
                {sending ? "Gönderiliyor..." : "Kodu Tekrar Gönder"}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

// Stiller
const styles = {
  overlay: {
    position: "fixed",
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: "rgba(0, 0, 0, 0.5)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    zIndex: 9999,
  },
  modal: {
    backgroundColor: "#fff",
    borderRadius: "12px",
    width: "90%",
    maxWidth: "400px",
    boxShadow: "0 4px 20px rgba(0, 0, 0, 0.15)",
    overflow: "hidden",
  },
  header: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    padding: "16px 20px",
    borderBottom: "1px solid #eee",
    backgroundColor: "#f8f9fa",
  },
  title: {
    margin: 0,
    fontSize: "18px",
    fontWeight: "600",
    color: "#333",
  },
  closeBtn: {
    background: "none",
    border: "none",
    fontSize: "24px",
    cursor: "pointer",
    color: "#666",
    padding: "0 8px",
  },
  body: {
    padding: "24px 20px",
    textAlign: "center",
  },
  phoneInfo: {
    fontSize: "14px",
    color: "#555",
    marginBottom: "8px",
  },
  phoneIcon: {
    marginRight: "8px",
  },
  expiryInfo: {
    fontSize: "13px",
    color: "#666",
    marginBottom: "20px",
  },
  otpContainer: {
    display: "flex",
    justifyContent: "center",
    gap: "8px",
    marginBottom: "20px",
  },
  otpInput: {
    width: "45px",
    height: "55px",
    textAlign: "center",
    fontSize: "24px",
    fontWeight: "600",
    border: "2px solid #ced4da",
    borderRadius: "8px",
    outline: "none",
    transition: "border-color 0.2s",
  },
  attemptsInfo: {
    fontSize: "13px",
    color: "#856404",
    backgroundColor: "#fff3cd",
    padding: "8px 12px",
    borderRadius: "6px",
    marginBottom: "16px",
  },
  errorBox: {
    backgroundColor: "#f8d7da",
    color: "#721c24",
    padding: "12px 16px",
    borderRadius: "8px",
    marginBottom: "16px",
    fontSize: "14px",
    textAlign: "left",
  },
  errorIcon: {
    marginRight: "8px",
  },
  successBox: {
    backgroundColor: "#d4edda",
    color: "#155724",
    padding: "12px 16px",
    borderRadius: "8px",
    marginBottom: "16px",
    fontSize: "14px",
  },
  successIcon: {
    marginRight: "8px",
    fontWeight: "bold",
  },
  verifyBtn: {
    width: "100%",
    padding: "14px",
    fontSize: "16px",
    fontWeight: "600",
    backgroundColor: "#28a745",
    color: "#fff",
    border: "none",
    borderRadius: "8px",
    cursor: "pointer",
    marginBottom: "16px",
    transition: "background-color 0.2s",
  },
  resendContainer: {
    marginTop: "8px",
  },
  resendWait: {
    fontSize: "13px",
    color: "#666",
    margin: 0,
  },
  resendBtn: {
    background: "none",
    border: "none",
    color: "#007bff",
    fontSize: "14px",
    cursor: "pointer",
    textDecoration: "underline",
  },
};

export default OtpVerificationModal;
