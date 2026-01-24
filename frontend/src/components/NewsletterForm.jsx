/**
 * NewsletterForm - Bülten Abonelik Formu Bileşeni
 *
 * Turuncu gradient arka planlı, modern tasarımlı newsletter abonelik formu.
 * Hem mobil hem web görünümünde responsive çalışır.
 *
 * Özellikler:
 * - Turuncu gradient arka plan
 * - Dekoratif blur efektleri
 * - E-posta validasyonu (regex + backend)
 * - Form state yönetimi (idle, loading, success, error)
 * - Backend API entegrasyonu (newsletterService)
 * - LocalStorage ile abonelik durumu önbelleği
 *
 * API ENTEGRASYONU:
 * - POST /api/newsletter/subscribe endpoint'ine bağlı
 * - IP adresi backend'de otomatik kaydedilir (KVKK)
 * - Resubscribe desteği (pasif aboneler yeniden aktif edilebilir)
 *
 * Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6
 */

import React, { useState, useEffect } from "react";
import { subscribe, SUBSCRIPTION_SOURCES } from "../services/newsletterService";
import "../styles/newsletterForm.css";

// E-posta validasyon regex
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

// LocalStorage key - abonelik önbelleği için
const NEWSLETTER_STORAGE_KEY = "newsletter_subscribed";

const NewsletterForm = ({ className = "" }) => {
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState("idle"); // idle, loading, success, error
  const [message, setMessage] = useState("");
  const [isAlreadySubscribed, setIsAlreadySubscribed] = useState(false);

  // Daha önce abone olunmuş mu kontrol et
  useEffect(() => {
    const subscribed = localStorage.getItem(NEWSLETTER_STORAGE_KEY);
    if (subscribed) {
      setIsAlreadySubscribed(true);
    }
  }, []);

  /**
   * E-posta validasyonu
   * @param {string} emailValue - Kontrol edilecek e-posta
   * @returns {boolean} - Geçerli mi?
   */
  const validateEmail = (emailValue) => {
    return EMAIL_REGEX.test(emailValue);
  };

  /**
   * Form submit handler
   * @param {Event} e - Form submit event
   */
  const handleSubmit = async (e) => {
    e.preventDefault();

    // Boş e-posta kontrolü
    if (!email.trim()) {
      setStatus("error");
      setMessage("Lütfen e-posta adresinizi girin");
      return;
    }

    // E-posta format kontrolü
    if (!validateEmail(email)) {
      setStatus("error");
      setMessage("Geçerli bir e-posta adresi girin");
      return;
    }

    // Loading state
    setStatus("loading");
    setMessage("");

    try {
      // ══════════════════════════════════════════════════════════════════════
      // BACKEND API ÇAĞRISI
      // newsletterService.subscribe() fonksiyonu çağrılır
      // Backend'de email normalizasyonu, IP kaydı ve duplicate kontrolü yapılır
      // ══════════════════════════════════════════════════════════════════════

      // Abonelik kaynağını belirle (mobil/web)
      const source =
        window.innerWidth <= 768
          ? SUBSCRIPTION_SOURCES.WEB_FOOTER // Mobil de web_footer olarak sayılır
          : SUBSCRIPTION_SOURCES.WEB_FOOTER;

      const result = await subscribe({
        email: email.trim(),
        fullName: null, // Footer formunda isim alanı yok
        source: source,
      });

      if (result.success) {
        // Başarılı abonelik - LocalStorage'a önbellek olarak kaydet
        localStorage.setItem(
          NEWSLETTER_STORAGE_KEY,
          JSON.stringify({
            email: email.trim().toLowerCase(),
            subscribedAt: new Date().toISOString(),
            source: source,
            subscriberId: result.subscriberId,
          }),
        );

        setStatus("success");
        // Backend'den gelen mesajı kullan
        setMessage(result.message || "Bültenimize başarıyla abone oldunuz!");
        setIsAlreadySubscribed(true);
        setEmail("");
      } else {
        // Backend'den hata döndü
        setStatus("error");
        setMessage(result.message || "Abonelik işlemi başarısız oldu.");
      }
    } catch (error) {
      // Ağ hatası veya beklenmeyen hata
      console.error("[NewsletterForm] Subscription error:", error);
      setStatus("error");
      setMessage("Bağlantı hatası, lütfen tekrar deneyin.");
    }
  };

  // Zaten abone ise farklı görünüm
  if (isAlreadySubscribed && status !== "success") {
    return (
      <section className={`newsletter-section ${className}`}>
        <div className="newsletter-container">
          {/* Dekoratif blur efektleri */}
          <div className="newsletter-blur newsletter-blur-1"></div>
          <div className="newsletter-blur newsletter-blur-2"></div>

          <div className="newsletter-content">
            <div className="newsletter-icon">
              <i className="fas fa-check-circle"></i>
            </div>
            <h3 className="newsletter-title">Zaten Abonesiniz!</h3>
            <p className="newsletter-subtitle">
              Kampanya ve güncellemelerden haberdar olacaksınız.
            </p>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className={`newsletter-section ${className}`}>
      <div className="newsletter-container">
        {/* Dekoratif blur efektleri */}
        <div className="newsletter-blur newsletter-blur-1"></div>
        <div className="newsletter-blur newsletter-blur-2"></div>

        <div className="newsletter-content">
          {/* İkon */}
          <div className="newsletter-icon">
            <i className="fas fa-envelope-open-text"></i>
          </div>

          {/* Başlık */}
          <h3 className="newsletter-title">Bültenimize Abone Ol</h3>
          <p className="newsletter-subtitle">
            Kampanya ve güncellemelerden ilk siz haberdar olun!
          </p>

          {/* Form */}
          <form onSubmit={handleSubmit} className="newsletter-form">
            <div className="newsletter-input-group">
              <input
                type="email"
                value={email}
                onChange={(e) => {
                  setEmail(e.target.value);
                  if (status === "error") {
                    setStatus("idle");
                    setMessage("");
                  }
                }}
                placeholder="E-posta adresiniz"
                className={`newsletter-input ${
                  status === "error" ? "error" : ""
                }`}
                disabled={status === "loading"}
                aria-label="E-posta adresi"
                aria-invalid={status === "error"}
              />
              <button
                type="submit"
                className="newsletter-button"
                disabled={status === "loading"}
                aria-label="Abone ol"
              >
                {status === "loading" ? (
                  <i className="fas fa-spinner fa-spin"></i>
                ) : (
                  <>
                    <span className="btn-text">Abone Ol</span>
                    <i className="fas fa-paper-plane btn-icon"></i>
                  </>
                )}
              </button>
            </div>

            {/* Mesaj alanı */}
            {message && (
              <div
                className={`newsletter-message ${status}`}
                role={status === "error" ? "alert" : "status"}
                aria-live="polite"
              >
                <i
                  className={`fas ${
                    status === "success"
                      ? "fa-check-circle"
                      : "fa-exclamation-circle"
                  }`}
                ></i>
                {message}
              </div>
            )}
          </form>

          {/* Gizlilik notu */}
          <p className="newsletter-privacy">
            <i className="fas fa-lock"></i>
            E-posta adresiniz güvende, spam göndermiyoruz.
          </p>
        </div>
      </div>
    </section>
  );
};

export default NewsletterForm;
