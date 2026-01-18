// ==========================================================================
// ToastContainer.js - Toast Bildirim Container Bileşeni
// ==========================================================================
// Ekranda anlık bildirimleri gösteren toast container.
// Mobil uyumlu, swipe-to-dismiss destekli.
// ==========================================================================

import React, { useState, useEffect, useRef, useCallback } from "react";
import { useNotifications } from "../../contexts/NotificationContext";
import "./ToastContainer.css";

const ToastContainer = () => {
  const { toasts, removeToast } = useNotifications();

  return (
    <div className="toast-container" aria-live="polite" aria-atomic="true">
      {toasts.map((toast) => (
        <Toast
          key={toast.id}
          toast={toast}
          onRemove={() => removeToast(toast.id)}
        />
      ))}
    </div>
  );
};

// Toast Item Component
const Toast = ({ toast, onRemove }) => {
  const [isExiting, setIsExiting] = useState(false);
  const [progress, setProgress] = useState(100);
  const [isPaused, setIsPaused] = useState(false);
  const [swipeX, setSwipeX] = useState(0);
  const [isSwiping, setIsSwiping] = useState(false);

  const toastRef = useRef(null);
  const touchStartX = useRef(0);
  const timerRef = useRef(null);
  const startTimeRef = useRef(null);
  const remainingTimeRef = useRef(toast.duration || 5000);

  // Toast kaldırma
  const handleRemove = useCallback(() => {
    setIsExiting(true);
    setTimeout(onRemove, 300);
  }, [onRemove]);

  // Auto-dismiss timer
  useEffect(() => {
    if (!toast.persistent && !isPaused) {
      startTimeRef.current = Date.now();

      const updateProgress = () => {
        const elapsed = Date.now() - startTimeRef.current;
        const remaining = remainingTimeRef.current - elapsed;
        const progressPercent = (remaining / (toast.duration || 5000)) * 100;

        if (progressPercent <= 0) {
          handleRemove();
        } else {
          setProgress(progressPercent);
          timerRef.current = requestAnimationFrame(updateProgress);
        }
      };

      timerRef.current = requestAnimationFrame(updateProgress);

      return () => {
        if (timerRef.current) {
          cancelAnimationFrame(timerRef.current);
        }
        remainingTimeRef.current =
          remainingTimeRef.current - (Date.now() - startTimeRef.current);
      };
    }
  }, [isPaused, toast.persistent, toast.duration, handleRemove]);

  // Mouse/Touch pause
  const handleMouseEnter = () => {
    setIsPaused(true);
    if (timerRef.current) {
      cancelAnimationFrame(timerRef.current);
    }
  };

  const handleMouseLeave = () => {
    if (!isSwiping) {
      setIsPaused(false);
    }
  };

  // Touch swipe handlers
  const handleTouchStart = (e) => {
    touchStartX.current = e.touches[0].clientX;
    setIsSwiping(true);
    setIsPaused(true);
  };

  const handleTouchMove = (e) => {
    if (!isSwiping) return;
    const currentX = e.touches[0].clientX;
    const diff = currentX - touchStartX.current;
    setSwipeX(diff);
  };

  const handleTouchEnd = () => {
    setIsSwiping(false);

    // Eğer yeterince sürüklendiyse kaldır
    if (Math.abs(swipeX) > 100) {
      handleRemove();
    } else {
      setSwipeX(0);
      setIsPaused(false);
    }
  };

  // Toast ikonlarını al
  const getToastIcon = (type) => {
    const icons = {
      success: "✅",
      error: "❌",
      warning: "⚠️",
      info: "ℹ️",
      order: "📦",
      delivery: "🚚",
      courier: "🏍️",
      payment: "💳",
    };
    return icons[type] || "🔔";
  };

  // Toast renklerini al
  const getToastClass = (type) => {
    const classes = {
      success: "toast-success",
      error: "toast-error",
      warning: "toast-warning",
      info: "toast-info",
    };
    return classes[type] || "toast-info";
  };

  const swipeStyle = {
    transform: `translateX(${swipeX}px)`,
    opacity: 1 - Math.abs(swipeX) / 200,
  };

  return (
    <div
      ref={toastRef}
      className={`toast ${getToastClass(toast.type)} ${isExiting ? "exiting" : ""}`}
      style={isSwiping ? swipeStyle : undefined}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
      role="alert"
      aria-live="assertive"
    >
      {/* İkon */}
      <div className="toast-icon">{getToastIcon(toast.type)}</div>

      {/* İçerik */}
      <div className="toast-content">
        {toast.title && <p className="toast-title">{toast.title}</p>}
        <p className="toast-message">{toast.message}</p>
      </div>

      {/* Kapat Butonu */}
      <button
        className="toast-close"
        onClick={handleRemove}
        aria-label="Bildirimi kapat"
      >
        ×
      </button>

      {/* Progress Bar */}
      {!toast.persistent && (
        <div className="toast-progress-container">
          <div className="toast-progress" style={{ width: `${progress}%` }} />
        </div>
      )}

      {/* Action Button */}
      {toast.action && (
        <button
          className="toast-action-btn"
          onClick={() => {
            toast.action.onClick();
            handleRemove();
          }}
        >
          {toast.action.label}
        </button>
      )}
    </div>
  );
};

export default ToastContainer;
