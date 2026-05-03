// ==========================================================================
// AdminNotificationBell.jsx - Admin Bildirim Çanı Bileşeni
// ==========================================================================
// AdminSignalRContext'ten bildirim verisini alarak gösterir.
// Kendi SignalR bağlantısı KURMAZ — merkezi context'e bağlıdır.
//
// NEDEN: NotificationBell.jsx kendi SignalR bağlantısını yönetiyordu,
// bu da çoklu bağlantı ve paylaşılamayan state sorunlarına yol açıyordu.
// Bu bileşen sadece UI'dan sorumludur (Single Responsibility).
// ==========================================================================

import React, { useState, useEffect, useCallback, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useAdminSignalR } from "../contexts/AdminSignalRContext";
import {
  isSoundEnabled,
  setSoundEnabled,
} from "../contexts/NotificationContext";

// ============================================================================
// BİLDİRİM TÜRLERİ — İkon ve renk eşleşmeleri
// ============================================================================
const NotificationConfig = {
  new_order: {
    icon: "fas fa-shopping-cart",
    color: "primary",
    bgColor: "rgba(13, 110, 253, 0.1)",
  },
  order_status: {
    icon: "fas fa-info-circle",
    color: "info",
    bgColor: "rgba(13, 202, 240, 0.1)",
  },
  payment_success: {
    icon: "fas fa-credit-card",
    color: "success",
    bgColor: "rgba(25, 135, 84, 0.1)",
  },
  payment_failed: {
    icon: "fas fa-exclamation-circle",
    color: "danger",
    bgColor: "rgba(220, 53, 69, 0.1)",
  },
  delivery_assigned: {
    icon: "fas fa-truck",
    color: "success",
    bgColor: "rgba(25, 135, 84, 0.1)",
  },
  delivery_completed: {
    icon: "fas fa-check-circle",
    color: "success",
    bgColor: "rgba(25, 135, 84, 0.1)",
  },
  delivery_failed: {
    icon: "fas fa-times-circle",
    color: "danger",
    bgColor: "rgba(220, 53, 69, 0.1)",
  },
  delivery_problem: {
    icon: "fas fa-exclamation-triangle",
    color: "warning",
    bgColor: "rgba(255, 193, 7, 0.1)",
  },
  delivery_stuck: {
    icon: "fas fa-pause-circle",
    color: "warning",
    bgColor: "rgba(255, 193, 7, 0.1)",
  },
  order_cancelled: {
    icon: "fas fa-ban",
    color: "danger",
    bgColor: "rgba(220, 53, 69, 0.1)",
  },
  refund_requested: {
    icon: "fas fa-undo",
    color: "warning",
    bgColor: "rgba(255, 193, 7, 0.1)",
  },
  low_stock: {
    icon: "fas fa-box-open",
    color: "danger",
    bgColor: "rgba(220, 53, 69, 0.1)",
  },
  admin_alert: {
    icon: "fas fa-bell",
    color: "danger",
    bgColor: "rgba(220, 53, 69, 0.1)",
  },
  sla_violation: {
    icon: "fas fa-clock",
    color: "danger",
    bgColor: "rgba(220, 53, 69, 0.1)",
  },
  courier_offline: {
    icon: "fas fa-user-slash",
    color: "secondary",
    bgColor: "rgba(108, 117, 125, 0.1)",
  },
  weight_charge: {
    icon: "fas fa-weight",
    color: "info",
    bgColor: "rgba(13, 202, 240, 0.1)",
  },
  default: {
    icon: "fas fa-cog",
    color: "secondary",
    bgColor: "rgba(108, 117, 125, 0.1)",
  },
};

// ============================================================================
// YARDIMCI FONKSİYONLAR
// ============================================================================

/**
 * Relative zaman formatla (ör. "3 dk önce")
 */
const formatTimeAgo = (timestamp) => {
  const now = new Date();
  const time = new Date(timestamp);
  const diffMs = now - time;
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return "Şimdi";
  if (diffMins < 60) return `${diffMins} dk önce`;
  if (diffHours < 24) return `${diffHours} saat önce`;
  return `${diffDays} gün önce`;
};

/**
 * Bildirim konfigürasyonu al
 */
const getConfig = (type) =>
  NotificationConfig[type] || NotificationConfig.default;

// ============================================================================
// ANA KOMPONENT
// ============================================================================
export default function AdminNotificationBell() {
  // Merkezi context'ten bildirim verisini al
  const {
    notifications,
    unreadCount,
    isConnected,
    markAsRead,
    markAllAsRead,
    clearNotifications,
  } = useAdminSignalR();

  const [isOpen, setIsOpen] = useState(false);
  const [soundEnabled, setSoundEnabledLocal] = useState(isSoundEnabled());
  const dropdownRef = useRef(null);
  const navigate = useNavigate();

  // Ses toggle
  const handleSoundToggle = useCallback(
    (e) => {
      e.stopPropagation();
      const newValue = !soundEnabled;
      setSoundEnabled(newValue);
      setSoundEnabledLocal(newValue);
    },
    [soundEnabled]
  );

  // Dropdown dış tıklama
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Bildirime tıklama — türe göre yönlendirme
  const handleNotificationClick = useCallback(
    (notification) => {
      markAsRead(notification.id);
      setIsOpen(false);

      const routeMap = {
        new_order: "/admin/orders",
        order_status: "/admin/orders",
        order_cancelled: "/admin/orders",
        payment_success: "/admin/orders",
        payment_failed: "/admin/orders",
        delivery_assigned: "/admin/delivery-tasks",
        delivery_completed: "/admin/delivery-tasks",
        delivery_failed: "/admin/delivery-tasks",
        delivery_problem: "/admin/delivery-tasks",
        delivery_stuck: "/admin/delivery-tasks",
        courier_offline: "/admin/couriers",
        low_stock: "/admin/products",
        refund_requested: "/admin/orders",
        weight_charge: "/admin/weight-management",
        sla_violation: "/admin/orders",
      };

      const route = routeMap[notification.type];
      if (route) navigate(route);
    },
    [markAsRead, navigate]
  );

  return (
    <div className="position-relative" ref={dropdownRef}>
      {/* Bildirim Çanı Butonu */}
      <button
        className="btn btn-link position-relative p-2 admin-notification-trigger"
        onClick={() => setIsOpen(!isOpen)}
        style={{ color: "#6c757d" }}
        title={
          isConnected ? "Bildirimler (Canlı)" : "Bildirimler (Bağlanıyor...)"
        }
      >
        <i className="fas fa-bell" style={{ fontSize: "1.1rem" }}></i>

        {/* Okunmamış Sayısı Badge */}
        {unreadCount > 0 && (
          <span
            className="position-absolute badge rounded-pill bg-danger"
            style={{
              top: "2px",
              right: "2px",
              fontSize: "0.6rem",
              padding: "0.25em 0.45em",
              minWidth: "18px",
            }}
          >
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}

        {/* Bağlantı Göstergesi */}
        <span
          className={`position-absolute rounded-circle admin-notification-connection-dot ${isConnected ? "bg-success" : "bg-warning"}`}
          style={{
            width: "8px",
            height: "8px",
            bottom: "5px",
            right: "5px",
            border: "1px solid white",
          }}
        ></span>
      </button>

      {/* Dropdown Menü */}
      {isOpen && (
        <div
          className="position-absolute bg-white shadow-lg rounded-3 border-0"
          style={{
            right: 0,
            top: "100%",
            width: "340px",
            maxWidth: "calc(100vw - 20px)",
            zIndex: 1050,
            animation: "fadeIn 0.2s ease",
          }}
        >
          {/* Header */}
          <div className="d-flex justify-content-between align-items-center p-3 border-bottom">
            <h6 className="mb-0 fw-bold" style={{ fontSize: "0.9rem" }}>
              <i className="fas fa-bell me-2 text-primary"></i>
              Bildirimler
              {unreadCount > 0 && (
                <span
                  className="badge bg-primary ms-2"
                  style={{ fontSize: "0.7rem" }}
                >
                  {unreadCount} yeni
                </span>
              )}
            </h6>
            <div className="d-flex gap-2 align-items-center">
              {/* Ses Toggle */}
              <button
                className={`btn btn-sm btn-link p-0 ${soundEnabled ? "text-success" : "text-muted"}`}
                onClick={handleSoundToggle}
                title={soundEnabled ? "Sesi kapat" : "Sesi aç"}
                style={{ fontSize: "0.85rem" }}
              >
                <i
                  className={`fas ${soundEnabled ? "fa-volume-up" : "fa-volume-mute"}`}
                ></i>
              </button>
              {notifications.length > 0 && (
                <>
                  <button
                    className="btn btn-sm btn-link text-muted p-0"
                    onClick={markAllAsRead}
                    title="Tümünü okundu işaretle"
                    style={{ fontSize: "0.75rem" }}
                  >
                    <i className="fas fa-check-double"></i>
                  </button>
                  <button
                    className="btn btn-sm btn-link text-danger p-0"
                    onClick={() => {
                      clearNotifications();
                      setIsOpen(false);
                    }}
                    title="Tümünü temizle"
                    style={{ fontSize: "0.75rem" }}
                  >
                    <i className="fas fa-trash"></i>
                  </button>
                </>
              )}
            </div>
          </div>

          {/* Bildirim Listesi */}
          <div style={{ maxHeight: "380px", overflowY: "auto" }}>
            {notifications.length === 0 ? (
              <div className="text-center py-4 text-muted">
                <i className="fas fa-bell-slash fa-2x mb-2 opacity-50"></i>
                <p className="mb-0" style={{ fontSize: "0.85rem" }}>
                  Henüz bildirim yok
                </p>
                <small className="text-muted">
                  Yeni bildirimler burada görünecek
                </small>
              </div>
            ) : (
              notifications.map((notification) => {
                const config = getConfig(notification.type);
                return (
                  <div
                    key={notification.id}
                    className={`p-3 border-bottom ${!notification.read ? "bg-light" : ""}`}
                    onClick={() => handleNotificationClick(notification)}
                    style={{
                      cursor: "pointer",
                      transition: "background-color 0.2s",
                      borderLeft: !notification.read
                        ? `3px solid var(--bs-${config.color})`
                        : "3px solid transparent",
                    }}
                    onMouseEnter={(e) =>
                      (e.currentTarget.style.backgroundColor = "#f8f9fa")
                    }
                    onMouseLeave={(e) =>
                      (e.currentTarget.style.backgroundColor =
                        notification.read ? "" : "#f8f9fa")
                    }
                  >
                    <div className="d-flex align-items-start gap-2">
                      {/* ikon */}
                      <div
                        className="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0"
                        style={{
                          width: "36px",
                          height: "36px",
                          backgroundColor: config.bgColor,
                        }}
                      >
                        <i
                          className={`${config.icon} text-${config.color}`}
                          style={{ fontSize: "0.85rem" }}
                        ></i>
                      </div>

                      {/* İçerik */}
                      <div className="flex-grow-1 overflow-hidden">
                        <div className="d-flex justify-content-between align-items-start">
                          <span
                            className={`fw-semibold ${!notification.read ? "text-dark" : "text-muted"}`}
                            style={{ fontSize: "0.8rem" }}
                          >
                            {notification.title}
                          </span>
                          <small
                            className="text-muted flex-shrink-0 ms-2"
                            style={{ fontSize: "0.65rem" }}
                          >
                            {formatTimeAgo(notification.timestamp)}
                          </small>
                        </div>
                        <p
                          className="mb-0 text-muted text-truncate"
                          style={{ fontSize: "0.75rem" }}
                        >
                          {notification.message}
                        </p>
                      </div>

                      {/* Okunmadı işareti */}
                      {!notification.read && (
                        <span
                          className={`rounded-circle bg-${config.color} flex-shrink-0`}
                          style={{
                            width: "8px",
                            height: "8px",
                            marginTop: "6px",
                          }}
                        ></span>
                      )}
                    </div>
                  </div>
                );
              })
            )}
          </div>

          {/* Footer */}
          {notifications.length > 5 && (
            <div className="p-2 border-top text-center">
              <small className="text-muted" style={{ fontSize: "0.75rem" }}>
                {notifications.length} bildirim gösteriliyor
              </small>
            </div>
          )}
        </div>
      )}

      {/* CSS Animation */}
      <style>{`
        @keyframes fadeIn {
          from { opacity: 0; transform: translateY(-10px); }
          to { opacity: 1; transform: translateY(0); }
        }
      `}</style>
    </div>
  );
}
