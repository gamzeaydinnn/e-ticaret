// ==========================================================================
// ToastContainer.jsx - Toast Bildirimleri Container
// ==========================================================================
// Mobil uyumlu, animasyonlu toast bildirimleri. Sağ üstten çıkar ve
// otomatik olarak kaybolur. Swipe ile kapatma desteği vardır.
// ==========================================================================

import React, { useCallback, useRef } from "react";
import { useNotifications } from "../../contexts/NotificationContext";
import "./ToastContainer.css";

/**
 * ToastContainer - Toast bildirimlerini gösteren container
 *
 * Özellikler:
 * - Animasyonlu giriş/çıkış
 * - Swipe ile kapatma (mobil)
 * - Tip bazlı stil (success, error, warning, info)
 * - Action buton desteği
 */
const ToastContainer = () => {
  const { toasts, dismissToast } = useNotifications();

  return (
    <div className="toast-container" role="region" aria-label="Bildirimler">
      {toasts.map((toast) => (
        <ToastItem
          key={toast.id}
          toast={toast}
          onDismiss={() => dismissToast(toast.id)}
        />
      ))}
    </div>
  );
};

/**
 * ToastItem - Tekil toast bildirimi
 */
const ToastItem = ({ toast, onDismiss }) => {
  const touchStartX = useRef(0);
  const touchStartY = useRef(0);
  const toastRef = useRef(null);

  // Toast tipine göre ikon
  const getToastIcon = (type) => {
    switch (type) {
      case "success":
      case "delivery_completed":
        return "✅";
      case "error":
      case "delivery_failed":
        return "❌";
      case "warning":
      case "alert":
        return "⚠️";
      case "delivery":
      case "courier_assigned":
        return "📦";
      case "courier_enroute":
        return "🚚";
      case "new_order":
        return "🛒";
      case "message":
        return "💬";
      default:
        return "ℹ️";
    }
  };

  // Toast tipine göre sınıf
  const getToastClass = (type) => {
    switch (type) {
      case "success":
      case "delivery_completed":
        return "toast-success";
      case "error":
      case "delivery_failed":
        return "toast-error";
      case "warning":
      case "alert":
        return "toast-warning";
      default:
        return "toast-info";
    }
  };

  // Touch başlangıç
  const handleTouchStart = useCallback((e) => {
    touchStartX.current = e.touches[0].clientX;
    touchStartY.current = e.touches[0].clientY;
  }, []);

  // Touch hareket
  const handleTouchMove = useCallback((e) => {
    if (!toastRef.current) return;

    const currentX = e.touches[0].clientX;
    const currentY = e.touches[0].clientY;
    const diffX = currentX - touchStartX.current;
    const diffY = Math.abs(currentY - touchStartY.current);

    // Yatay swipe kontrolü (dikey hareketten fazla olmalı)
    if (Math.abs(diffX) > diffY && diffX > 0) {
      toastRef.current.style.transform = `translateX(${diffX}px)`;
      toastRef.current.style.opacity = 1 - diffX / 200;
    }
  }, []);

  // Touch bitişi
  const handleTouchEnd = useCallback(
    (e) => {
      if (!toastRef.current) return;

      const currentX = e.changedTouches[0].clientX;
      const diffX = currentX - touchStartX.current;

      // Yeterince sağa swipe edilmişse kapat
      if (diffX > 100) {
        toastRef.current.style.transform = "translateX(100%)";
        toastRef.current.style.opacity = "0";
        setTimeout(onDismiss, 200);
      } else {
        // Geri al
        toastRef.current.style.transform = "translateX(0)";
        toastRef.current.style.opacity = "1";
      }
    },
    [onDismiss],
  );

  // Action butona tıklandığında
  const handleActionClick = useCallback(() => {
    if (toast.action && toast.action.onClick) {
      toast.action.onClick();
    }
    onDismiss();
  }, [toast.action, onDismiss]);

  return (
    <div
      ref={toastRef}
      className={`toast-item ${getToastClass(toast.type)}`}
      role="alert"
      aria-live="polite"
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
    >
      {/* İkon */}
      <div className="toast-icon">{getToastIcon(toast.type)}</div>

      {/* İçerik */}
      <div className="toast-content">
        {toast.title && <h4 className="toast-title">{toast.title}</h4>}
        {toast.message && <p className="toast-message">{toast.message}</p>}

        {/* Action Button */}
        {toast.action && (
          <button className="toast-action-btn" onClick={handleActionClick}>
            {toast.action.label}
          </button>
        )}
      </div>

      {/* Kapatma Butonu */}
      <button
        className="toast-close-btn"
        onClick={onDismiss}
        aria-label="Bildirimi kapat"
      >
        ×
      </button>

      {/* Progress Bar (opsiyonel) */}
      <div className="toast-progress"></div>
    </div>
  );
};

export default ToastContainer;
