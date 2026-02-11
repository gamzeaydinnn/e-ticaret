import React, { useState, useEffect } from "react";
import "./CookieConsent.css";

/**
 * KVKK uyumlu Cookie Consent Banner - Minimal & Kibar versiyon
 * Kullanıcı deneyimini bozmadan yasal uyumluluğu sağlar
 */
const CookieConsent = () => {
  const [showBanner, setShowBanner] = useState(false);
  const [kvkkAccepted, setKvkkAccepted] = useState(false);

  useEffect(() => {
    const consent = localStorage.getItem("cookieConsent");
    if (!consent) {
      setTimeout(() => setShowBanner(true), 500);
    }
  }, []);

  const handleAccept = () => {
    if (!kvkkAccepted) {
      alert("Lütfen kişisel veri işleme onayını işaretleyin.");
      return;
    }
    localStorage.setItem("cookieConsent", "accepted");
    localStorage.setItem("cookieConsentDate", new Date().toISOString());
    localStorage.setItem("kvkkConsent", "accepted");
    setShowBanner(false);
  };

  const handleReject = () => {
    localStorage.setItem("cookieConsent", "rejected");
    localStorage.setItem("cookieConsentDate", new Date().toISOString());
    setShowBanner(false);
  };

  if (!showBanner) return null;

  return (
    <div className="cookie-consent-overlay">
      <div className="cookie-consent-banner">
        <div className="cookie-consent-content">
          <p className="cookie-consent-text">
            🍪 Daha iyi bir deneyim sunmak için çerezler kullanıyoruz.{" "}
            <a
              href="/privacy-policy"
              target="_blank"
              rel="noopener noreferrer"
              className="cookie-consent-link"
            >
              Detaylı bilgi
            </a>
          </p>

          <label className="cookie-consent-checkbox">
            <input
              type="checkbox"
              checked={kvkkAccepted}
              onChange={(e) => setKvkkAccepted(e.target.checked)}
            />
            <span className="cookie-consent-checkbox-text">
              Kişisel verilerimin işlenmesini kabul ediyorum (KVKK)
            </span>
          </label>
        </div>

        <div className="cookie-consent-actions">
          <button
            onClick={handleReject}
            className="cookie-consent-btn cookie-consent-btn-secondary"
          >
            Hayır
          </button>
          <button
            onClick={handleAccept}
            className="cookie-consent-btn cookie-consent-btn-primary"
          >
            Kabul Et
          </button>
        </div>
      </div>
    </div>
  );
};

export default CookieConsent;
