// ============================================================
// SESSION WARNING MODAL - Oturum Uyarı Modalı
// ============================================================
// Oturum süresi dolmak üzereyken kullanıcıyı uyarır.
// Kullanıcı oturumu uzatabilir veya çıkış yapabilir.
// ============================================================

import React from "react";
import "./SessionWarningModal.css";

/**
 * Session Warning Modal
 *
 * @param {boolean} isOpen - Modal açık mı?
 * @param {string} remainingTime - Kalan süre (formatlanmış)
 * @param {Function} onExtend - Oturumu uzat butonu
 * @param {Function} onLogout - Çıkış yap butonu
 */
const SessionWarningModal = ({ isOpen, remainingTime, onExtend, onLogout }) => {
  if (!isOpen) return null;

  return (
    <div className="session-warning-overlay">
      <div className="session-warning-modal">
        {/* İkon */}
        <div className="session-warning-icon">
          <i className="fas fa-clock"></i>
        </div>

        {/* Başlık */}
        <h3 className="session-warning-title">Oturum Süresi Dolmak Üzere</h3>

        {/* Mesaj */}
        <p className="session-warning-message">
          Güvenlik nedeniyle oturumunuz{" "}
          <strong className="session-warning-time">{remainingTime}</strong>{" "}
          sonra sonlanacak.
        </p>

        {/* Alt bilgi */}
        <p className="session-warning-submessage">
          Çalışmaya devam etmek için oturumu uzatın veya güvenli çıkış yapın.
        </p>

        {/* Butonlar */}
        <div className="session-warning-actions">
          <button className="session-warning-btn extend" onClick={onExtend}>
            <i className="fas fa-sync-alt me-2"></i>
            Oturumu Uzat
          </button>
          <button className="session-warning-btn logout" onClick={onLogout}>
            <i className="fas fa-sign-out-alt me-2"></i>
            Çıkış Yap
          </button>
        </div>
      </div>
    </div>
  );
};

export default SessionWarningModal;
