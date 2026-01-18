// ==========================================================================
// NotificationBell.js - Bildirim Zili Bileşeni
// ==========================================================================
// Header'da gösterilecek bildirim zili. Okunmamış bildirim sayısını gösterir
// ve tıklandığında bildirim listesini açar.
// ==========================================================================

import React, { useState, useEffect, useRef, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useNotifications } from "../../contexts/NotificationContext";
import "./NotificationBell.css";

const NotificationBell = () => {
  const navigate = useNavigate();
  const {
    notifications,
    unreadCount,
    markAsRead,
    markAllAsRead,
    deleteNotification,
    loading,
  } = useNotifications();

  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef(null);

  // Dropdown dışına tıklandığında kapat
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Escape tuşu ile kapat
  useEffect(() => {
    const handleEscape = (event) => {
      if (event.key === "Escape") {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener("keydown", handleEscape);
      return () => document.removeEventListener("keydown", handleEscape);
    }
  }, [isOpen]);

  // Bildirim tıklama
  const handleNotificationClick = useCallback(
    async (notification) => {
      // Okunmadıysa okundu olarak işaretle
      if (!notification.isRead) {
        await markAsRead(notification.id);
      }

      // Aksiyona göre yönlendir
      if (notification.actionUrl) {
        setIsOpen(false);
        navigate(notification.actionUrl);
      }
    },
    [markAsRead, navigate],
  );

  // Tümünü okundu işaretle
  const handleMarkAllRead = async (e) => {
    e.stopPropagation();
    await markAllAsRead();
  };

  // Bildirim sil
  const handleDelete = async (e, notificationId) => {
    e.stopPropagation();
    await deleteNotification(notificationId);
  };

  // Bildirim ikonunu al
  const getNotificationIcon = (type) => {
    const icons = {
      order: "📦",
      delivery: "🚚",
      payment: "💳",
      system: "⚙️",
      promotion: "🎉",
      alert: "⚠️",
      success: "✅",
      info: "ℹ️",
      courier: "🏍️",
      weight: "⚖️",
    };
    return icons[type] || "🔔";
  };

  // Zaman formatla
  const formatTime = (dateString) => {
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

    return date.toLocaleDateString("tr-TR");
  };

  // Son 10 bildirimi göster
  const recentNotifications = notifications.slice(0, 10);

  return (
    <div className="notification-bell-container" ref={dropdownRef}>
      {/* Bildirim Zili Butonu */}
      <button
        className={`notification-bell-button ${isOpen ? "active" : ""}`}
        onClick={() => setIsOpen(!isOpen)}
        aria-label={`Bildirimler${unreadCount > 0 ? ` (${unreadCount} okunmamış)` : ""}`}
        aria-expanded={isOpen}
      >
        <span className="bell-icon">🔔</span>
        {unreadCount > 0 && (
          <span className="notification-badge">
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </button>

      {/* Bildirim Dropdown */}
      {isOpen && (
        <div className="notification-dropdown">
          {/* Header */}
          <div className="notification-dropdown-header">
            <h3>Bildirimler</h3>
            {unreadCount > 0 && (
              <button className="mark-all-read-btn" onClick={handleMarkAllRead}>
                Tümünü Okundu İşaretle
              </button>
            )}
          </div>

          {/* Bildirim Listesi */}
          <div className="notification-list">
            {loading ? (
              <div className="notification-loading">
                <div className="loading-spinner"></div>
                <span>Yükleniyor...</span>
              </div>
            ) : recentNotifications.length === 0 ? (
              <div className="notification-empty">
                <span className="empty-icon">📭</span>
                <p>Henüz bildiriminiz yok</p>
              </div>
            ) : (
              recentNotifications.map((notification) => (
                <div
                  key={notification.id}
                  className={`notification-item ${!notification.isRead ? "unread" : ""}`}
                  onClick={() => handleNotificationClick(notification)}
                  role="button"
                  tabIndex={0}
                  onKeyPress={(e) =>
                    e.key === "Enter" && handleNotificationClick(notification)
                  }
                >
                  {/* İkon */}
                  <div className="notification-icon">
                    {getNotificationIcon(notification.type)}
                  </div>

                  {/* İçerik */}
                  <div className="notification-content">
                    <p className="notification-title">{notification.title}</p>
                    <p className="notification-message">
                      {notification.message}
                    </p>
                    <span className="notification-time">
                      {formatTime(notification.createdAt)}
                    </span>
                  </div>

                  {/* Aksiyonlar */}
                  <div className="notification-actions">
                    {!notification.isRead && (
                      <span className="unread-dot" title="Okunmadı"></span>
                    )}
                    <button
                      className="delete-btn"
                      onClick={(e) => handleDelete(e, notification.id)}
                      title="Sil"
                    >
                      ×
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>

          {/* Footer */}
          {notifications.length > 10 && (
            <div className="notification-dropdown-footer">
              <button
                className="view-all-btn"
                onClick={() => {
                  setIsOpen(false);
                  navigate("/notifications");
                }}
              >
                Tüm Bildirimleri Gör ({notifications.length})
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default NotificationBell;
