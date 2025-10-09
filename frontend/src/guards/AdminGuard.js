// src/guards/AdminGuard.js
import React from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";

/**
 * Admin yetkisi kontrolü yapan guard component
 */
export const AdminGuard = ({ children }) => {
  const { user, loading } = useAuth();

  // Yükleniyor durumu
  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ minHeight: "100vh" }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  // Kullanıcı yoksa login sayfasına yönlendir
  if (!user) {
    return <Navigate to="/admin/login" replace />;
  }

  // Admin yetkisi yoksa ana sayfaya yönlendir
  if (!user.isAdmin && user.role !== "Admin") {
    return <Navigate to="/" replace />;
  }

  // Admin yetkisi varsa içeriği göster
  return children;
};

/**
 * Admin login kontrolü - giriş yapmışsa admin paneline yönlendir
 */
export const AdminLoginGuard = ({ children }) => {
  const { user, loading } = useAuth();

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ minHeight: "100vh" }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  // Admin girişi yapmışsa dashboard'a yönlendir
  if (user && (user.isAdmin || user.role === "Admin")) {
    return <Navigate to="/admin/dashboard" replace />;
  }

  return children;
};
