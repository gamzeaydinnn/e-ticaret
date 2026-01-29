// ==========================================================================
// NotificationBell.jsx - Bildirim Zili Bileşeni
// ==========================================================================
// Header'da gösterilen bildirim zili bileşeni. Real-time bildirimleri
// SignalR üzerinden alır ve kullanıcıya gösterir. Mobil uyumlu dropdown
// tasarımına sahiptir. Ses açma/kapama kontrolü içerir.
// ==========================================================================

import React, { useState, useEffect, useRef, useCallback } from "react";
import { Link } from "react-router-dom";
import "./NotificationBell.css";

// ============================================================================
// SES KONTROLÜ
// localStorage'dan ses ayarını oku/yaz
// ============================================================================
const SOUND_ENABLED_KEY = "notificationSoundEnabled";

const isSoundEnabled = () => {
  const storedValue = localStorage.getItem(SOUND_ENABLED_KEY);
  return storedValue === null || storedValue === "true";
};

const setSoundEnabled = (enabled) => {
  localStorage.setItem(SOUND_ENABLED_KEY, enabled ? "true" : "false");
};

/**
 * NotificationBell - Header bildirim zili bileşeni
 *
 * Props:
 * - notifications: Bildirim listesi
 * - unreadCount: Okunmamış bildirim sayısı
 * - onNotificationClick: Bildirim tıklama callback
 * - onMarkAsRead: Okundu işaretleme callback
 * - onMarkAllAsRead: Tümünü okundu işaretleme callback
 * - onClearAll: Tümünü temizle callback
 * - isLoading: Yükleniyor durumu
 * - maxVisible: Gösterilecek maksimum bildirim sayısı
 * - showSoundToggle: Ses toggle butonu gösterilsin mi (varsayılan: true)
 */
const NotificationBell = ({
  notifications = [],
  unreadCount = 0,
  onNotificationClick,
  onMarkAsRead,
  onMarkAllAsRead,
  onClearAll,
  isLoading = false,
  maxVisible = 5,
  showSoundToggle = true,
}) => {
  // State tanımları
  const [isOpen, setIsOpen] = useState(false);
  const [animatingBell, setAnimatingBell] = useState(false);
  const [soundEnabled, setSoundEnabledState] = useState(isSoundEnabled());
  const dropdownRef = useRef(null);
  const bellRef = useRef(null);
  const prevUnreadCount = useRef(unreadCount);

  // Ses toggle handler
  const handleSoundToggle = useCallback(
    (e) => {
      e.stopPropagation();
      const newValue = !soundEnabled;
      setSoundEnabled(newValue);
      setSoundEnabledState(newValue);
    },
    [soundEnabled],
  );

  // Dışarı tıklandığında dropdown'ı kapat
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("touchstart", handleClickOutside);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("touchstart", handleClickOutside);
    };
  }, []);

  // Yeni bildirim geldiğinde zili animasyon yap
  useEffect(() => {
    if (unreadCount > prevUnreadCount.current) {
      setAnimatingBell(true);

      // Titreşim desteği varsa titret (mobil)
      if ("vibrate" in navigator) {
        navigator.vibrate([100, 50, 100]);
      }

      // 1 saniye sonra animasyonu durdur
      const timer = setTimeout(() => {
        setAnimatingBell(false);
      }, 1000);

      return () => clearTimeout(timer);
    }
    prevUnreadCount.current = unreadCount;
  }, [unreadCount]);

  // ESC tuşu ile kapat
  useEffect(() => {
    const handleEscape = (event) => {
      if (event.key === "Escape" && isOpen) {
        setIsOpen(false);
      }
    };

    document.addEventListener("keydown", handleEscape);
    return () => document.removeEventListener("keydown", handleEscape);
  }, [isOpen]);

  // Zile tıklandığında
  const handleBellClick = useCallback(() => {
    setIsOpen((prev) => !prev);
  }, []);

  // Bildirime tıklandığında
  const handleNotificationClick = useCallback(
    (notification) => {
      if (onNotificationClick) {
        onNotificationClick(notification);
      }
      if (!notification.isRead && onMarkAsRead) {
        onMarkAsRead(notification.id);
      }
      setIsOpen(false);
    },
    [onNotificationClick, onMarkAsRead],
  );

  // Tümünü okundu işaretle
  const handleMarkAllAsRead = useCallback(
    (e) => {
      e.stopPropagation();
      if (onMarkAllAsRead) {
        onMarkAllAsRead();
      }
    },
    [onMarkAllAsRead],
  );

  // Tümünü temizle
  const handleClearAll = useCallback(
    (e) => {
      e.stopPropagation();
      if (onClearAll) {
        onClearAll();
      }
    },
    [onClearAll],
  );

  // Bildirim tipine göre ikon
  const getNotificationIcon = (type) => {
    switch (type) {
      case "delivery":
      case "delivery_completed":
        return "📦";
      case "courier_assigned":
        return "🚚";
      case "courier_enroute":
        return "🛵";
      case "delivery_failed":
        return "❌";
      case "new_order":
        return "🛒";
      case "payment":
        return "💳";
      case "alert":
      case "warning":
        return "⚠️";
      case "success":
        return "✅";
      case "info":
        return "ℹ️";
      case "message":
        return "💬";
      default:
        return "🔔";
    }
  };

  // Zaman formatla
  const formatTime = (dateString) => {
    if (!dateString) return "";

    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Şimdi";
    if (diffMins < 60) return `${diffMins} dk önce`;
    if (diffHours < 24) return `${diffHours} saat önce`;
    if (diffDays < 7) return `${diffDays} gün önce`;

    return date.toLocaleDateString("tr-TR", {
      day: "numeric",
      month: "short",
    });
  };

  // Görünür bildirimleri al
  const visibleNotifications = notifications.slice(0, maxVisible);
  const hasMoreNotifications = notifications.length > maxVisible;

  return (
    <div className="notification-bell-container" ref={dropdownRef}>
      {/* Zil Butonu */}
      <button
        ref={bellRef}
        className={`notification-bell-button ${animatingBell ? "animating" : ""}`}
        onClick={handleBellClick}
        aria-label={`Bildirimler ${unreadCount > 0 ? `(${unreadCount} yeni)` : ""}`}
        aria-expanded={isOpen}
        aria-haspopup="true"
      >
        <span className="bell-icon">🔔</span>

        {/* Okunmamış sayısı badge */}
        {unreadCount > 0 && (
          <span className="notification-badge">
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="notification-dropdown">
          {/* Dropdown Header */}
          <div className="notification-dropdown-header">
            <h3 className="notification-title">
              Bildirimler
              {unreadCount > 0 && (
                <span className="unread-count">({unreadCount} yeni)</span>
              )}
            </h3>

            <div className="notification-actions">
              {/* Ses Toggle Butonu */}
              {showSoundToggle && (
                <button
                  className={`notification-action-btn sound-toggle ${soundEnabled ? "active" : ""}`}
                  onClick={handleSoundToggle}
                  title={
                    soundEnabled
                      ? "Bildirim sesini kapat"
                      : "Bildirim sesini aç"
                  }
                >
                  {soundEnabled ? "🔊" : "🔇"}
                </button>
              )}
              {unreadCount > 0 && (
                <button
                  className="notification-action-btn"
                  onClick={handleMarkAllAsRead}
                  title="Tümünü okundu işaretle"
                >
                  ✓ Okundu
                </button>
              )}
            </div>
          </div>

          {/* Bildirim Listesi */}
          <div className="notification-list">
            {isLoading ? (
              <div className="notification-loading">
                <div className="loading-spinner"></div>
                <span>Yükleniyor...</span>
              </div>
            ) : visibleNotifications.length === 0 ? (
              <div className="notification-empty">
                <span className="empty-icon">📭</span>
                <p>Henüz bildiriminiz yok</p>
              </div>
            ) : (
              visibleNotifications.map((notification) => (
                <div
                  key={notification.id}
                  className={`notification-item ${!notification.isRead ? "unread" : ""}`}
                  onClick={() => handleNotificationClick(notification)}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" || e.key === " ") {
                      handleNotificationClick(notification);
                    }
                  }}
                >
                  <div className="notification-icon">
                    {getNotificationIcon(notification.type)}
                  </div>

                  <div className="notification-content">
                    <p className="notification-message">
                      {notification.title || notification.message}
                    </p>
                    {notification.body && (
                      <p className="notification-body">{notification.body}</p>
                    )}
                    <span className="notification-time">
                      {formatTime(
                        notification.createdAt || notification.timestamp,
                      )}
                    </span>
                  </div>

                  {!notification.isRead && (
                    <div className="unread-indicator"></div>
                  )}
                </div>
              ))
            )}
          </div>

          {/* Dropdown Footer */}
          {notifications.length > 0 && (
            <div className="notification-dropdown-footer">
              {hasMoreNotifications && (
                <Link
                  to="/notifications"
                  className="view-all-link"
                  onClick={() => setIsOpen(false)}
                >
                  Tüm bildirimler ({notifications.length})
                </Link>
              )}

              {notifications.length > 0 && (
                <button className="clear-all-btn" onClick={handleClearAll}>
                  Temizle
                </button>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default NotificationBell;
