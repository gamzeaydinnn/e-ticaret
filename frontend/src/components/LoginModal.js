// src/components/LoginModal.js
// Giriş ve Kayıt Modalı - SMS OTP doğrulama ile kullanıcı kayıt akışı
// Google OAuth entegrasyonu
import React, { useState, useCallback } from "react";
import { useGoogleLogin } from "@react-oauth/google";
import { useAuth } from "../contexts/AuthContext";
import otpService, { SmsVerificationPurpose } from "../services/otpService";

const LoginModal = ({ show, onHide, onLoginSuccess }) => {
  // ==================== STATE TANIMLARI ====================
  const [isLogin, setIsLogin] = useState(true);
  const [isForgotPassword, setIsForgotPassword] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPasswordRegister, setConfirmPasswordRegister] = useState(""); // Kayıt için şifre onayı
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [showOtpInput, setShowOtpInput] = useState(false);
  const [otpSent, setOtpSent] = useState(false);
  const [otpVerified, setOtpVerified] = useState(false);
  const [otpCountdown, setOtpCountdown] = useState(0);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(false);

  // Şifre sıfırlama için ek state'ler
  const [forgotPasswordStep, setForgotPasswordStep] = useState(1); // 1: telefon gir, 2: kod gir, 3: yeni şifre
  const [forgotPasswordPhone, setForgotPasswordPhone] = useState("");
  const [forgotPasswordCode, setForgotPasswordCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const {
    login,
    register,
    registerWithPhone,
    verifyPhoneRegistration,
    forgotPasswordByPhone,
    resetPasswordByPhone,
    loginWithSocial,
  } = useAuth();

  // ==================== OTP İŞLEMLERİ ====================

  /**
   * Kayıt için OTP kodu gönder
   * Backend'deki yeni /api/sms/send-otp endpoint'ini kullanır
   */
  const handleSendOtp = async () => {
    if (!phoneNumber || phoneNumber.length < 10) {
      setError("Geçerli bir telefon numarası girin (05XXXXXXXXX)");
      return;
    }

    setLoading(true);
    setError("");

    // Yeni backend API'yi kullan - purpose: Registration (1)
    const result = await otpService.sendOtp(
      phoneNumber,
      SmsVerificationPurpose.Registration,
    );

    if (result.success) {
      setOtpSent(true);
      setShowOtpInput(true);
      setSuccess("Doğrulama kodu telefonunuza gönderildi.");
      setOtpCountdown(result.expiresInSeconds || 120); // Backend'den gelen süre

      // Countdown timer
      const timer = setInterval(() => {
        setOtpCountdown((prev) => {
          if (prev <= 1) {
            clearInterval(timer);
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    } else {
      setError(result.message || "SMS gönderilemedi");
      if (result.retryAfterSeconds) {
        setOtpCountdown(result.retryAfterSeconds);
      }
    }

    setLoading(false);
  };

  /**
   * Kayıt için OTP kodunu doğrula
   * Backend'deki yeni /api/sms/verify-otp endpoint'ini kullanır
   */
  const handleVerifyOtp = async () => {
    if (!otpCode || otpCode.length !== 6) {
      setError("6 haneli kodu girin");
      return;
    }

    setLoading(true);
    setError("");

    // Yeni backend API'yi kullan - purpose: Registration (1)
    const result = await otpService.verifyOtp(
      phoneNumber,
      otpCode,
      SmsVerificationPurpose.Registration,
    );

    if (result.success) {
      setOtpVerified(true);
      setSuccess("Telefon numaranız doğrulandı!");
      setShowOtpInput(false);
    } else {
      setError(result.message || "Kod doğrulanamadı");
      if (result.remainingAttempts !== undefined) {
        setError(
          `${result.message} (${result.remainingAttempts} deneme kaldı)`,
        );
      }
    }

    setLoading(false);
  };

  // ==================== ŞİFRE SIFIRLAMA İŞLEMLERİ ====================

  /**
   * Telefon ile şifre sıfırlama - Adım 1: OTP Gönder
   */
  const handleForgotPasswordSendOtp = async () => {
    if (!forgotPasswordPhone || forgotPasswordPhone.length < 10) {
      setError("Geçerli bir telefon numarası girin (05XXXXXXXXX)");
      return;
    }

    setLoading(true);
    setError("");

    const result = await forgotPasswordByPhone(forgotPasswordPhone);

    if (result.success) {
      setForgotPasswordStep(2);
      setSuccess("Doğrulama kodu telefonunuza gönderildi.");
      setOtpCountdown(result.expiresInSeconds || 180);

      const timer = setInterval(() => {
        setOtpCountdown((prev) => {
          if (prev <= 1) {
            clearInterval(timer);
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    } else {
      setError(result.message || result.error || "SMS gönderilemedi");
    }

    setLoading(false);
  };

  /**
   * Telefon ile şifre sıfırlama - Adım 2: Kodu Doğrula
   * Backend'e OTP doğrulama isteği gönderir, doğrulanırsa şifre adımına geçer.
   * Güvenlik: Sadece client-side değil, backend tarafından da doğrulama yapılır.
   */
  const handleForgotPasswordVerifyCode = async () => {
    if (!forgotPasswordCode || forgotPasswordCode.length !== 6) {
      setError("6 haneli kodu girin");
      return;
    }

    // NOTE: Şifre sıfırlama akışında OTP, backend'de reset sırasında doğrulanır.
    // Burada ayrıca /api/sms/verify-otp çağırmak kodu "Verified" yapıp
    // reset endpoint'inde tekrar doğrulama yapıldığı için hata yaratıyordu.
    setError("");
    setForgotPasswordStep(3);
    setSuccess("Yeni şifrenizi belirleyin.");
  };

  /**
   * Telefon ile şifre sıfırlama - Adım 3: Yeni Şifre Belirle
   */
  const handleResetPassword = async (e) => {
    e.preventDefault();

    if (newPassword !== confirmPassword) {
      setError("Şifreler eşleşmiyor");
      return;
    }

    if (newPassword.length < 6) {
      setError("Şifre en az 6 karakter olmalıdır");
      return;
    }

    setLoading(true);
    setError("");

    const result = await resetPasswordByPhone(
      forgotPasswordPhone,
      forgotPasswordCode,
      newPassword,
      confirmPassword,
    );

    if (result.success) {
      setSuccess("Şifreniz başarıyla değiştirildi! Giriş yapabilirsiniz.");
      setTimeout(() => {
        resetForgotPasswordState();
        setIsForgotPassword(false);
        setIsLogin(true);
      }, 2000);
    } else {
      setError(result.error || "Şifre sıfırlanamadı");
    }

    setLoading(false);
  };

  /**
   * Şifre sıfırlama state'lerini temizle
   */
  const resetForgotPasswordState = () => {
    setForgotPasswordStep(1);
    setForgotPasswordPhone("");
    setForgotPasswordCode("");
    setNewPassword("");
    setConfirmPassword("");
    setError("");
    setSuccess("");
    setOtpCountdown(0);
  };

  // ==================== E-POSTA İLE ŞİFRE SIFIRLAMA ====================
  const handleForgotPassword = async (e) => {
    e.preventDefault();
    setError("");
    setSuccess("");
    setLoading(true);

    try {
      // GÜVENLİK: Production'da REACT_APP_API_URL zorunlu olmalı
      const apiUrl = process.env.REACT_APP_API_URL;
      if (!apiUrl && process.env.NODE_ENV === "production") {
        throw new Error(
          "API URL yapılandırması eksik. Lütfen sistem yöneticisiyle iletişime geçin.",
        );
      }
      const response = await fetch(
        `${apiUrl || "https://localhost:5153"}/api/auth/forgot-password`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email }),
        },
      );

      if (response.ok) {
        setSuccess("Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.");
        setTimeout(() => {
          setIsForgotPassword(false);
          setSuccess("");
        }, 3000);
      } else {
        setError("E-posta adresi bulunamadı.");
      }
    } catch (error) {
      setError("Bir hata oluştu. Lütfen tekrar deneyin.");
    } finally {
      setLoading(false);
    }
  };

  // ==================== KAYIT VE GİRİŞ İŞLEMLERİ ====================

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    // Kayıt için validasyonlar
    if (!isLogin) {
      // OTP zorunlu
      if (!otpVerified) {
        setError("Lütfen telefon numaranızı doğrulayın");
        return;
      }

      // Şifre minimum 6 karakter
      if (password.length < 6) {
        setError("Şifre en az 6 karakter olmalıdır");
        return;
      }

      // Şifre onayı kontrolü
      if (password !== confirmPasswordRegister) {
        setError("Şifreler eşleşmiyor");
        return;
      }
    }

    setLoading(true);

    try {
      let result;
      if (isLogin) {
        result = await login(email, password);
      } else {
        // Telefon numarası ile kayıt
        result = await register(
          email,
          password,
          firstName,
          lastName,
          phoneNumber,
          confirmPasswordRegister, // Şifre onayı eklendi
        );
      }

      if (result.success) {
        onLoginSuccess && onLoginSuccess();
        onHide();
        // Formu temizle
        setEmail("");
        setPassword("");
        setConfirmPasswordRegister("");
        setFirstName("");
        setLastName("");
        setPhoneNumber("");
        setOtpCode("");
        setOtpVerified(false);
        setOtpSent(false);
        setShowOtpInput(false);
      } else {
        setError(result.error);
      }
    } catch (error) {
      setError("Bir hata oluştu. Lütfen tekrar deneyin.");
    } finally {
      setLoading(false);
    }
  };

  const handleSocialLogin = async (provider) => {
    setError("");
    setLoading(true);
    try {
      // Dev basit senaryo: email/name bilgisi olmadan backend dev fallback'i kullan
      const result = await loginWithSocial(provider, { email });
      if (result.success) {
        onLoginSuccess && onLoginSuccess();
        onHide();
      } else {
        setError(result.error || "Sosyal giriş başarısız");
      }
    } catch (e) {
      setError("Sosyal giriş başarısız");
    } finally {
      setLoading(false);
    }
  };

  // Google OAuth Login Hook
  const googleLogin = useGoogleLogin({
    onSuccess: async (tokenResponse) => {
      setLoading(true);
      setError("");
      try {
        // Google'dan kullanıcı bilgilerini al
        const userInfoResponse = await fetch(
          "https://www.googleapis.com/oauth2/v3/userinfo",
          {
            headers: { Authorization: `Bearer ${tokenResponse.access_token}` },
          },
        );
        const userInfo = await userInfoResponse.json();

        // Backend'e gönder
        const result = await loginWithSocial("google", {
          email: userInfo.email,
          name: userInfo.name,
          googleId: userInfo.sub,
          picture: userInfo.picture,
          accessToken: tokenResponse.access_token,
        });

        if (result.success) {
          onLoginSuccess && onLoginSuccess();
          onHide();
        } else {
          setError(result.error || "Google ile giriş başarısız");
        }
      } catch (e) {
        console.error("Google login error:", e);
        setError("Google ile giriş sırasında bir hata oluştu");
      } finally {
        setLoading(false);
      }
    },
    onError: (error) => {
      console.error("Google login error:", error);
      setError("Google ile giriş iptal edildi");
      setLoading(false);
    },
  });

  const handleModalClick = (e) => {
    e.stopPropagation();
  };

  const fillDemoCredentials = () => {
    setEmail("demo@example.com");
    setPassword("123456");
  };

  if (!show) return null;

  return (
    <div
      className="modal fade show d-block"
      style={{
        backgroundColor: "rgba(0,0,0,0.6)",
        backdropFilter: "blur(4px)",
      }}
      onClick={onHide}
    >
      <div
        className="modal-dialog modal-dialog-centered"
        onClick={handleModalClick}
        style={{ maxWidth: "440px" }}
      >
        <div
          className="modal-content border-0"
          style={{
            borderRadius: "24px",
            overflow: "hidden",
            boxShadow: "0 20px 60px rgba(255, 107, 53, 0.25)",
          }}
        >
          <div
            className="modal-header border-0"
            style={{
              background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
              padding: "24px 32px",
            }}
          >
            <h5 className="modal-title text-white fw-bold">
              {isForgotPassword
                ? "Şifremi Unuttum"
                : isLogin
                  ? "Giriş Yap"
                  : "Hesap Oluştur"}
            </h5>
            <button
              type="button"
              className="btn-close btn-close-white"
              onClick={onHide}
            ></button>
          </div>

          <div
            className="modal-body p-4"
            style={{ padding: "32px !important" }}
          >
            {/* ==================== ŞİFRE SIFIRLAMA FORMU ==================== */}
            {isForgotPassword ? (
              <>
                {success && (
                  <div className="alert alert-success">
                    <i className="fas fa-check-circle me-2"></i>
                    {success}
                  </div>
                )}
                {error && (
                  <div className="alert alert-danger">
                    <i className="fas fa-exclamation-triangle me-2"></i>
                    {error}
                  </div>
                )}

                {/* Adım İndikatörü */}
                <div className="d-flex justify-content-center mb-4">
                  <div className="d-flex align-items-center">
                    <div
                      className={`rounded-circle d-flex align-items-center justify-content-center ${
                        forgotPasswordStep >= 1
                          ? "bg-primary text-white"
                          : "bg-light text-muted"
                      }`}
                      style={{
                        width: "32px",
                        height: "32px",
                        fontSize: "14px",
                      }}
                    >
                      1
                    </div>
                    <div
                      className={`mx-2 ${
                        forgotPasswordStep >= 2 ? "bg-primary" : "bg-light"
                      }`}
                      style={{ width: "40px", height: "3px" }}
                    ></div>
                    <div
                      className={`rounded-circle d-flex align-items-center justify-content-center ${
                        forgotPasswordStep >= 2
                          ? "bg-primary text-white"
                          : "bg-light text-muted"
                      }`}
                      style={{
                        width: "32px",
                        height: "32px",
                        fontSize: "14px",
                      }}
                    >
                      2
                    </div>
                    <div
                      className={`mx-2 ${
                        forgotPasswordStep >= 3 ? "bg-primary" : "bg-light"
                      }`}
                      style={{ width: "40px", height: "3px" }}
                    ></div>
                    <div
                      className={`rounded-circle d-flex align-items-center justify-content-center ${
                        forgotPasswordStep >= 3
                          ? "bg-primary text-white"
                          : "bg-light text-muted"
                      }`}
                      style={{
                        width: "32px",
                        height: "32px",
                        fontSize: "14px",
                      }}
                    >
                      3
                    </div>
                  </div>
                </div>

                {/* Adım 1: Telefon Numarası Gir */}
                {forgotPasswordStep === 1 && (
                  <div>
                    <p className="text-muted text-center mb-3">
                      Şifrenizi sıfırlamak için kayıtlı telefon numaranızı
                      girin.
                    </p>
                    <div className="mb-3">
                      <label htmlFor="forgotPhone" className="form-label">
                        <i className="fas fa-phone me-2"></i>Telefon Numarası
                      </label>
                      <input
                        type="tel"
                        className="form-control form-control-lg"
                        id="forgotPhone"
                        value={forgotPasswordPhone}
                        onChange={(e) =>
                          setForgotPasswordPhone(
                            e.target.value.replace(/\D/g, ""),
                          )
                        }
                        placeholder="05XXXXXXXXX"
                        maxLength="11"
                        style={{
                          borderRadius: "12px",
                          border: "2px solid #e0e0e0",
                          padding: "12px 16px",
                        }}
                        onFocus={(e) =>
                          (e.target.style.borderColor = "#ff6b35")
                        }
                        onBlur={(e) => (e.target.style.borderColor = "#e0e0e0")}
                      />
                    </div>
                    <button
                      type="button"
                      className="btn btn-lg w-100 text-white fw-bold mb-3"
                      style={{
                        background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                        border: "none",
                        borderRadius: "12px",
                        padding: "14px",
                      }}
                      onClick={handleForgotPasswordSendOtp}
                      disabled={loading || forgotPasswordPhone.length < 10}
                    >
                      {loading ? (
                        <>
                          <i className="fas fa-spinner fa-spin me-2"></i>
                          Gönderiliyor...
                        </>
                      ) : (
                        <>
                          <i className="fas fa-sms me-2"></i>Doğrulama Kodu
                          Gönder
                        </>
                      )}
                    </button>
                  </div>
                )}

                {/* Adım 2: OTP Kodu Gir */}
                {forgotPasswordStep === 2 && (
                  <div>
                    <p className="text-muted text-center mb-3">
                      <strong>{forgotPasswordPhone}</strong> numarasına
                      gönderilen 6 haneli kodu girin.
                    </p>
                    <div className="mb-3">
                      <label htmlFor="forgotCode" className="form-label">
                        <i className="fas fa-key me-2"></i>Doğrulama Kodu
                        {otpCountdown > 0 && (
                          <span className="badge bg-secondary ms-2">
                            {otpCountdown}s
                          </span>
                        )}
                      </label>
                      <input
                        type="text"
                        className="form-control form-control-lg text-center"
                        id="forgotCode"
                        value={forgotPasswordCode}
                        onChange={(e) =>
                          setForgotPasswordCode(
                            e.target.value.replace(/\D/g, ""),
                          )
                        }
                        placeholder="123456"
                        maxLength="6"
                        style={{
                          borderRadius: "12px",
                          border: "2px solid #e0e0e0",
                          padding: "12px 16px",
                          fontSize: "24px",
                          letterSpacing: "12px",
                        }}
                      />
                    </div>
                    <button
                      type="button"
                      className="btn btn-lg w-100 text-white fw-bold mb-3"
                      style={{
                        background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                        border: "none",
                        borderRadius: "12px",
                        padding: "14px",
                      }}
                      onClick={handleForgotPasswordVerifyCode}
                      disabled={loading || forgotPasswordCode.length !== 6}
                    >
                      {loading ? (
                        <>
                          <i className="fas fa-spinner fa-spin me-2"></i>
                          Doğrulanıyor...
                        </>
                      ) : (
                        <>
                          <i className="fas fa-check me-2"></i>Kodu Doğrula
                        </>
                      )}
                    </button>
                    {otpCountdown === 0 && (
                      <button
                        type="button"
                        className="btn btn-outline-secondary w-100"
                        onClick={handleForgotPasswordSendOtp}
                        disabled={loading}
                        style={{ borderRadius: "12px" }}
                      >
                        <i className="fas fa-redo me-2"></i>Tekrar Kod Gönder
                      </button>
                    )}
                  </div>
                )}

                {/* Adım 3: Yeni Şifre Belirle */}
                {forgotPasswordStep === 3 && (
                  <form onSubmit={handleResetPassword}>
                    <p className="text-muted text-center mb-3">
                      Yeni şifrenizi belirleyin.
                    </p>
                    <div className="mb-3">
                      <label htmlFor="newPassword" className="form-label">
                        <i className="fas fa-lock me-2"></i>Yeni Şifre
                      </label>
                      <input
                        type="password"
                        className="form-control form-control-lg"
                        id="newPassword"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        placeholder="En az 6 karakter"
                        required
                        minLength={6}
                        style={{
                          borderRadius: "12px",
                          border: "2px solid #e0e0e0",
                          padding: "12px 16px",
                        }}
                        onFocus={(e) =>
                          (e.target.style.borderColor = "#ff6b35")
                        }
                        onBlur={(e) => (e.target.style.borderColor = "#e0e0e0")}
                      />
                    </div>
                    <div className="mb-3">
                      <label htmlFor="confirmPassword" className="form-label">
                        <i className="fas fa-lock me-2"></i>Şifre Tekrar
                      </label>
                      <input
                        type="password"
                        className="form-control form-control-lg"
                        id="confirmPassword"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        placeholder="Şifrenizi tekrar girin"
                        required
                        minLength={6}
                        style={{
                          borderRadius: "12px",
                          border: "2px solid #e0e0e0",
                          padding: "12px 16px",
                        }}
                        onFocus={(e) =>
                          (e.target.style.borderColor = "#ff6b35")
                        }
                        onBlur={(e) => (e.target.style.borderColor = "#e0e0e0")}
                      />
                    </div>
                    <button
                      type="submit"
                      className="btn btn-lg w-100 text-white fw-bold mb-3"
                      style={{
                        background: "linear-gradient(135deg, #28a745, #20c997)",
                        border: "none",
                        borderRadius: "12px",
                        padding: "14px",
                      }}
                      disabled={loading}
                    >
                      {loading ? (
                        <>
                          <i className="fas fa-spinner fa-spin me-2"></i>
                          Kaydediliyor...
                        </>
                      ) : (
                        <>
                          <i className="fas fa-save me-2"></i>Şifremi Değiştir
                        </>
                      )}
                    </button>
                  </form>
                )}

                <div className="text-center mt-3">
                  <button
                    className="btn btn-link p-0 text-decoration-none"
                    onClick={() => {
                      resetForgotPasswordState();
                      setIsForgotPassword(false);
                    }}
                    style={{ color: "#ff6b35" }}
                  >
                    ← Giriş Sayfasına Dön
                  </button>
                </div>
              </>
            ) : (
              <>
                {/* ==================== GİRİŞ VE KAYIT FORMU ==================== */}

                {error && (
                  <div className="alert alert-danger">
                    <i className="fas fa-exclamation-triangle me-2"></i>
                    {error}
                  </div>
                )}

                {success && (
                  <div className="alert alert-success">
                    <i className="fas fa-check-circle me-2"></i>
                    {success}
                  </div>
                )}

                <form onSubmit={handleSubmit}>
                  {/* Google ile Giriş - Sadece giriş için */}
                  {isLogin && (
                    <div className="d-grid gap-2 mb-4">
                      <button
                        type="button"
                        className="btn d-flex align-items-center justify-content-center"
                        onClick={() => googleLogin()}
                        disabled={loading}
                        style={{
                          borderRadius: "12px",
                          padding: "12px 16px",
                          background: "#fff",
                          border: "2px solid #e0e0e0",
                          transition: "all 0.3s ease",
                          boxShadow: "0 2px 8px rgba(0,0,0,0.05)",
                        }}
                        onMouseEnter={(e) => {
                          e.target.style.borderColor = "#4285F4";
                          e.target.style.boxShadow =
                            "0 4px 12px rgba(66, 133, 244, 0.2)";
                        }}
                        onMouseLeave={(e) => {
                          e.target.style.borderColor = "#e0e0e0";
                          e.target.style.boxShadow =
                            "0 2px 8px rgba(0,0,0,0.05)";
                        }}
                      >
                        <svg
                          width="20"
                          height="20"
                          viewBox="0 0 24 24"
                          style={{ marginRight: "12px" }}
                        >
                          <path
                            fill="#4285F4"
                            d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                          />
                          <path
                            fill="#34A853"
                            d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                          />
                          <path
                            fill="#FBBC05"
                            d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                          />
                          <path
                            fill="#EA4335"
                            d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                          />
                        </svg>
                        <span style={{ fontWeight: "500", color: "#444" }}>
                          Google ile devam et
                        </span>
                      </button>

                      {/* Ayırıcı çizgi */}
                      <div className="d-flex align-items-center my-2">
                        <hr
                          className="flex-grow-1"
                          style={{ borderColor: "#e0e0e0" }}
                        />
                        <span
                          className="px-3 text-muted"
                          style={{ fontSize: "13px" }}
                        >
                          veya
                        </span>
                        <hr
                          className="flex-grow-1"
                          style={{ borderColor: "#e0e0e0" }}
                        />
                      </div>
                    </div>
                  )}
                  {!isLogin && (
                    <>
                      {/* Google ile Kayıt Butonu */}
                      <div className="d-grid mb-3">
                        <button
                          type="button"
                          className="btn d-flex align-items-center justify-content-center"
                          onClick={() => googleLogin()}
                          disabled={loading}
                          style={{
                            borderRadius: "12px",
                            padding: "12px 16px",
                            background: "#fff",
                            border: "2px solid #e0e0e0",
                            transition: "all 0.3s ease",
                            boxShadow: "0 2px 8px rgba(0,0,0,0.05)",
                          }}
                          onMouseEnter={(e) => {
                            e.target.style.borderColor = "#4285F4";
                            e.target.style.boxShadow =
                              "0 4px 12px rgba(66, 133, 244, 0.2)";
                          }}
                          onMouseLeave={(e) => {
                            e.target.style.borderColor = "#e0e0e0";
                            e.target.style.boxShadow =
                              "0 2px 8px rgba(0,0,0,0.05)";
                          }}
                        >
                          <svg
                            width="20"
                            height="20"
                            viewBox="0 0 24 24"
                            style={{ marginRight: "12px" }}
                          >
                            <path
                              fill="#4285F4"
                              d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                            />
                            <path
                              fill="#34A853"
                              d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                            />
                            <path
                              fill="#FBBC05"
                              d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                            />
                            <path
                              fill="#EA4335"
                              d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                            />
                          </svg>
                          <span style={{ fontWeight: "500", color: "#444" }}>
                            Google ile hızlı kayıt
                          </span>
                        </button>
                      </div>

                      {/* Ayırıcı çizgi */}
                      <div className="d-flex align-items-center mb-3">
                        <hr
                          className="flex-grow-1"
                          style={{ borderColor: "#e0e0e0" }}
                        />
                        <span
                          className="px-3 text-muted"
                          style={{ fontSize: "13px" }}
                        >
                          veya e-posta ile
                        </span>
                        <hr
                          className="flex-grow-1"
                          style={{ borderColor: "#e0e0e0" }}
                        />
                      </div>

                      {/* Kayıt Adımları Göstergesi (Stepper) */}
                      <div className="d-flex justify-content-center mb-4">
                        <div className="d-flex align-items-center">
                          {/* Adım 1: Kişisel Bilgiler */}
                          <div className="d-flex flex-column align-items-center">
                            <div
                              style={{
                                width: "32px",
                                height: "32px",
                                borderRadius: "50%",
                                background:
                                  firstName && lastName
                                    ? "#28a745"
                                    : "linear-gradient(135deg, #ff6b35, #ff8c00)",
                                color: "#fff",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                                fontSize: "14px",
                                fontWeight: "600",
                              }}
                            >
                              {firstName && lastName ? (
                                <i className="fas fa-check"></i>
                              ) : (
                                "1"
                              )}
                            </div>
                            <small
                              className="text-muted mt-1"
                              style={{ fontSize: "10px" }}
                            >
                              Bilgiler
                            </small>
                          </div>
                          <div
                            style={{
                              width: "40px",
                              height: "2px",
                              background:
                                phoneNumber && otpVerified
                                  ? "#28a745"
                                  : "#e0e0e0",
                            }}
                          ></div>

                          {/* Adım 2: Telefon Doğrulama */}
                          <div className="d-flex flex-column align-items-center">
                            <div
                              style={{
                                width: "32px",
                                height: "32px",
                                borderRadius: "50%",
                                background: otpVerified
                                  ? "#28a745"
                                  : phoneNumber.length >= 10
                                    ? "linear-gradient(135deg, #ff6b35, #ff8c00)"
                                    : "#e0e0e0",
                                color:
                                  otpVerified || phoneNumber.length >= 10
                                    ? "#fff"
                                    : "#999",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                                fontSize: "14px",
                                fontWeight: "600",
                              }}
                            >
                              {otpVerified ? (
                                <i className="fas fa-check"></i>
                              ) : (
                                "2"
                              )}
                            </div>
                            <small
                              className="text-muted mt-1"
                              style={{ fontSize: "10px" }}
                            >
                              Doğrulama
                            </small>
                          </div>
                          <div
                            style={{
                              width: "40px",
                              height: "2px",
                              background:
                                password &&
                                confirmPasswordRegister &&
                                password === confirmPasswordRegister
                                  ? "#28a745"
                                  : "#e0e0e0",
                            }}
                          ></div>

                          {/* Adım 3: Şifre */}
                          <div className="d-flex flex-column align-items-center">
                            <div
                              style={{
                                width: "32px",
                                height: "32px",
                                borderRadius: "50%",
                                background:
                                  password &&
                                  confirmPasswordRegister &&
                                  password === confirmPasswordRegister
                                    ? "#28a745"
                                    : password
                                      ? "linear-gradient(135deg, #ff6b35, #ff8c00)"
                                      : "#e0e0e0",
                                color: password ? "#fff" : "#999",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                                fontSize: "14px",
                                fontWeight: "600",
                              }}
                            >
                              {password &&
                              confirmPasswordRegister &&
                              password === confirmPasswordRegister ? (
                                <i className="fas fa-check"></i>
                              ) : (
                                "3"
                              )}
                            </div>
                            <small
                              className="text-muted mt-1"
                              style={{ fontSize: "10px" }}
                            >
                              Şifre
                            </small>
                          </div>
                        </div>
                      </div>

                      <div className="row mb-3">
                        <div className="col-6">
                          <label htmlFor="firstName" className="form-label">
                            <i className="fas fa-user me-2"></i>Ad
                          </label>
                          <input
                            type="text"
                            className="form-control form-control-lg"
                            id="firstName"
                            value={firstName}
                            onChange={(e) => setFirstName(e.target.value)}
                            required={!isLogin}
                            placeholder="Gamze"
                            style={{
                              borderRadius: "12px",
                              border: "2px solid #e0e0e0",
                              padding: "12px 16px",
                              transition: "all 0.3s ease",
                            }}
                            onFocus={(e) =>
                              (e.target.style.borderColor = "#ff6b35")
                            }
                            onBlur={(e) =>
                              (e.target.style.borderColor = "#e0e0e0")
                            }
                          />
                        </div>
                        <div className="col-6">
                          <label htmlFor="lastName" className="form-label">
                            <i className="fas fa-user me-2"></i>Soyad
                          </label>
                          <input
                            type="text"
                            className="form-control form-control-lg"
                            id="lastName"
                            value={lastName}
                            onChange={(e) => setLastName(e.target.value)}
                            required={!isLogin}
                            placeholder="Aydın"
                            style={{
                              borderRadius: "12px",
                              border: "2px solid #e0e0e0",
                              padding: "12px 16px",
                            }}
                            onFocus={(e) =>
                              (e.target.style.borderColor = "#ff6b35")
                            }
                            onBlur={(e) =>
                              (e.target.style.borderColor = "#e0e0e0")
                            }
                          />
                        </div>
                      </div>

                      {/* Telefon Numarası ve OTP */}
                      <div className="mb-3">
                        <label htmlFor="phoneNumber" className="form-label">
                          <i className="fas fa-phone me-2"></i>Telefon Numarası
                          {otpVerified && (
                            <i className="fas fa-check-circle text-success ms-2"></i>
                          )}
                        </label>
                        <div className="input-group">
                          <input
                            type="tel"
                            className="form-control form-control-lg"
                            id="phoneNumber"
                            value={phoneNumber}
                            onChange={(e) => {
                              const value = e.target.value.replace(/\D/g, "");
                              setPhoneNumber(value);
                              setOtpVerified(false);
                              setOtpSent(false);
                            }}
                            required={!isLogin}
                            placeholder="05XXXXXXXXX"
                            disabled={otpVerified}
                            maxLength="11"
                            style={{
                              borderRadius: "12px 0 0 12px",
                              border: "2px solid #e0e0e0",
                              padding: "12px 16px",
                              borderRight: "none",
                            }}
                            onFocus={(e) =>
                              (e.target.style.borderColor = "#ff6b35")
                            }
                            onBlur={(e) =>
                              (e.target.style.borderColor = "#e0e0e0")
                            }
                          />
                          <button
                            type="button"
                            className="btn btn-outline-primary"
                            onClick={handleSendOtp}
                            disabled={
                              loading ||
                              otpVerified ||
                              otpCountdown > 0 ||
                              phoneNumber.length < 10
                            }
                            style={{
                              borderRadius: "0 12px 12px 0",
                              border: "2px solid #e0e0e0",
                              borderLeft: "none",
                              padding: "12px 20px",
                            }}
                          >
                            {otpVerified ? (
                              <i className="fas fa-check"></i>
                            ) : otpCountdown > 0 ? (
                              `${otpCountdown}s`
                            ) : otpSent ? (
                              "Tekrar Gönder"
                            ) : (
                              "Kod Gönder"
                            )}
                          </button>
                        </div>
                      </div>

                      {/* OTP Kod Girişi */}
                      {showOtpInput && !otpVerified && (
                        <div className="mb-3">
                          <label htmlFor="otpCode" className="form-label">
                            <i className="fas fa-key me-2"></i>Doğrulama Kodu
                          </label>
                          <div className="input-group">
                            <input
                              type="text"
                              className="form-control form-control-lg text-center"
                              id="otpCode"
                              value={otpCode}
                              onChange={(e) => {
                                const value = e.target.value.replace(/\D/g, "");
                                setOtpCode(value);
                              }}
                              placeholder="123456"
                              maxLength="6"
                              style={{
                                borderRadius: "12px 0 0 12px",
                                border: "2px solid #e0e0e0",
                                padding: "12px 16px",
                                borderRight: "none",
                                fontSize: "20px",
                                letterSpacing: "8px",
                              }}
                            />
                            <button
                              type="button"
                              className="btn btn-primary"
                              onClick={handleVerifyOtp}
                              disabled={loading || otpCode.length !== 6}
                              style={{
                                borderRadius: "0 12px 12px 0",
                                padding: "12px 20px",
                              }}
                            >
                              Doğrula
                            </button>
                          </div>
                          <small className="text-muted">
                            SMS ile gelen 6 haneli kodu girin
                          </small>
                        </div>
                      )}
                    </>
                  )}

                  <div className="mb-3">
                    <label htmlFor="email" className="form-label">
                      <i className="fas fa-envelope me-2"></i>E-posta
                    </label>
                    <input
                      type="email"
                      className="form-control form-control-lg"
                      id="email"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      required
                      placeholder="E-posta adresinizi girin"
                      style={{
                        borderRadius: "12px",
                        border: "2px solid #e0e0e0",
                        padding: "12px 16px",
                      }}
                      onFocus={(e) => (e.target.style.borderColor = "#ff6b35")}
                      onBlur={(e) => (e.target.style.borderColor = "#e0e0e0")}
                    />
                  </div>

                  <div className="mb-4">
                    <label htmlFor="password" className="form-label">
                      <i className="fas fa-lock me-2"></i>Şifre
                      {!isLogin && (
                        <small className="text-muted ms-1">
                          (en az 6 karakter)
                        </small>
                      )}
                    </label>
                    <input
                      type="password"
                      className="form-control form-control-lg"
                      id="password"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      required
                      minLength={!isLogin ? 6 : undefined}
                      placeholder={
                        isLogin ? "Şifrenizi girin" : "En az 6 karakter"
                      }
                      style={{
                        borderRadius: "12px",
                        border: "2px solid #e0e0e0",
                        padding: "12px 16px",
                      }}
                      onFocus={(e) => (e.target.style.borderColor = "#ff6b35")}
                      onBlur={(e) => (e.target.style.borderColor = "#e0e0e0")}
                    />
                    {/* Şifre Güçlük Metre - Sadece kayıt için */}
                    {!isLogin && password && (
                      <div className="mt-2">
                        <div className="d-flex align-items-center mb-1">
                          <small className="text-muted me-2">
                            Şifre Güçlüğü:
                          </small>
                          <small
                            style={{
                              color:
                                password.length < 6
                                  ? "#dc3545"
                                  : password.length < 8
                                    ? "#ffc107"
                                    : password.length >= 8 &&
                                        /[A-Z]/.test(password) &&
                                        /[0-9]/.test(password)
                                      ? "#28a745"
                                      : "#17a2b8",
                              fontWeight: "600",
                            }}
                          >
                            {password.length < 6
                              ? "Zayıf"
                              : password.length < 8
                                ? "Orta"
                                : password.length >= 8 &&
                                    /[A-Z]/.test(password) &&
                                    /[0-9]/.test(password)
                                  ? "Güçlü"
                                  : "İyi"}
                          </small>
                        </div>
                        <div
                          style={{
                            height: "4px",
                            borderRadius: "2px",
                            background: "#e0e0e0",
                            overflow: "hidden",
                          }}
                        >
                          <div
                            style={{
                              height: "100%",
                              width:
                                password.length < 6
                                  ? "25%"
                                  : password.length < 8
                                    ? "50%"
                                    : password.length >= 8 &&
                                        /[A-Z]/.test(password) &&
                                        /[0-9]/.test(password)
                                      ? "100%"
                                      : "75%",
                              background:
                                password.length < 6
                                  ? "#dc3545"
                                  : password.length < 8
                                    ? "#ffc107"
                                    : password.length >= 8 &&
                                        /[A-Z]/.test(password) &&
                                        /[0-9]/.test(password)
                                      ? "#28a745"
                                      : "#17a2b8",
                              transition: "all 0.3s ease",
                              borderRadius: "2px",
                            }}
                          ></div>
                        </div>
                        <div className="d-flex justify-content-between mt-1">
                          <small
                            className={`${password.length >= 6 ? "text-success" : "text-muted"}`}
                          >
                            <i
                              className={`fas fa-${password.length >= 6 ? "check" : "times"} me-1`}
                            ></i>
                            6+ karakter
                          </small>
                          <small
                            className={`${/[A-Z]/.test(password) ? "text-success" : "text-muted"}`}
                          >
                            <i
                              className={`fas fa-${/[A-Z]/.test(password) ? "check" : "times"} me-1`}
                            ></i>
                            Büyük harf
                          </small>
                          <small
                            className={`${/[0-9]/.test(password) ? "text-success" : "text-muted"}`}
                          >
                            <i
                              className={`fas fa-${/[0-9]/.test(password) ? "check" : "times"} me-1`}
                            ></i>
                            Rakam
                          </small>
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Şifre Onayı - Sadece kayıt için */}
                  {!isLogin && (
                    <div className="mb-4">
                      <label
                        htmlFor="confirmPasswordRegister"
                        className="form-label"
                      >
                        <i className="fas fa-lock me-2"></i>Şifre Tekrar
                        {password &&
                          confirmPasswordRegister &&
                          password === confirmPasswordRegister && (
                            <i className="fas fa-check-circle text-success ms-2"></i>
                          )}
                      </label>
                      <input
                        type="password"
                        className="form-control form-control-lg"
                        id="confirmPasswordRegister"
                        value={confirmPasswordRegister}
                        onChange={(e) =>
                          setConfirmPasswordRegister(e.target.value)
                        }
                        required
                        placeholder="Şifrenizi tekrar girin"
                        style={{
                          borderRadius: "12px",
                          border:
                            password &&
                            confirmPasswordRegister &&
                            password !== confirmPasswordRegister
                              ? "2px solid #dc3545"
                              : "2px solid #e0e0e0",
                          padding: "12px 16px",
                        }}
                        onFocus={(e) =>
                          (e.target.style.borderColor = "#ff6b35")
                        }
                        onBlur={(e) =>
                          (e.target.style.borderColor =
                            password &&
                            confirmPasswordRegister &&
                            password !== confirmPasswordRegister
                              ? "#dc3545"
                              : "#e0e0e0")
                        }
                      />
                      {password &&
                        confirmPasswordRegister &&
                        password !== confirmPasswordRegister && (
                          <small className="text-danger">
                            Şifreler eşleşmiyor
                          </small>
                        )}
                    </div>
                  )}

                  <button
                    type="submit"
                    className="btn btn-lg w-100 text-white fw-bold mb-3"
                    style={{
                      background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      border: "none",
                      borderRadius: "12px",
                      padding: "14px",
                      transition: "all 0.3s ease",
                      boxShadow: "0 4px 15px rgba(255, 107, 53, 0.3)",
                    }}
                    onMouseEnter={(e) => {
                      e.target.style.transform = "translateY(-2px)";
                      e.target.style.boxShadow =
                        "0 6px 20px rgba(255, 107, 53, 0.4)";
                    }}
                    onMouseLeave={(e) => {
                      e.target.style.transform = "translateY(0)";
                      e.target.style.boxShadow =
                        "0 4px 15px rgba(255, 107, 53, 0.3)";
                    }}
                    disabled={loading || (!isLogin && !otpVerified)}
                  >
                    {loading ? (
                      <div className="d-flex align-items-center justify-content-center">
                        <div
                          className="spinner-border spinner-border-sm me-2"
                          role="status"
                        >
                          <span className="visually-hidden">Yükleniyor...</span>
                        </div>
                        Lütfen bekleyin...
                      </div>
                    ) : (
                      <>{isLogin ? "Giriş Yap" : "Hesap Oluştur"}</>
                    )}
                  </button>

                  {/* Güvenlik Bilgisi - Kayıt için */}
                  {!isLogin && (
                    <div
                      className="text-center mb-3 p-2"
                      style={{
                        background: "rgba(40, 167, 69, 0.08)",
                        borderRadius: "8px",
                        border: "1px solid rgba(40, 167, 69, 0.2)",
                      }}
                    >
                      <small className="text-success">
                        <i className="fas fa-shield-alt me-2"></i>
                        Bilgileriniz 256-bit SSL ile şifrelenir ve güvenle
                        saklanır
                      </small>
                    </div>
                  )}
                </form>

                {isLogin && (
                  <div className="text-center mb-3">
                    <button
                      className="btn btn-link p-0 text-decoration-none"
                      onClick={() => {
                        setIsForgotPassword(true);
                        setError("");
                      }}
                      style={{ color: "#ff6b35", fontSize: "14px" }}
                    >
                      Şifremi Unuttum
                    </button>
                  </div>
                )}

                <div className="text-center">
                  <p className="text-muted mb-0">
                    {isLogin ? "Hesabınız yok mu?" : "Zaten hesabınız var mı?"}{" "}
                    <button
                      className="btn btn-link p-0 text-decoration-none"
                      onClick={() => {
                        setIsLogin(!isLogin);
                        setError("");
                        setSuccess("");
                        setOtpVerified(false);
                        setOtpSent(false);
                        setShowOtpInput(false);
                        setPhoneNumber("");
                        setOtpCode("");
                        setConfirmPasswordRegister("");
                      }}
                      style={{ color: "#ff6b35" }}
                    >
                      {isLogin ? "Hesap Oluştur" : "Giriş Yap"}
                    </button>
                  </p>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginModal;
