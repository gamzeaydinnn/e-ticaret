// ==========================================================================
// StoreAttendantGuard.jsx - Market Görevlisi Route Guard
// ==========================================================================
// Store Attendant paneli için route koruma bileşenleri.
// Authenticated kullanıcıları yönlendirir, olmayanlara login gösterir.
// ==========================================================================

import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useStoreAttendantAuth } from "../contexts/StoreAttendantAuthContext";

// ============================================================================
// STORE ATTENDANT GUARD
// Dashboard ve diğer korumalı sayfalar için
// ============================================================================
export function StoreAttendantGuard({ children }) {
  const { isAuthenticated, loading } = useStoreAttendantAuth();
  const location = useLocation();

  // Yükleme durumunda spinner göster
  if (loading) {
    return (
      <div
        className="min-vh-100 d-flex align-items-center justify-content-center"
        style={{
          background: "linear-gradient(135deg, #2E7D32 0%, #66BB6A 100%)",
        }}
      >
        <div className="text-center text-white">
          <div
            className="spinner-border mb-3"
            style={{ width: "3rem", height: "3rem" }}
          ></div>
          <p>Yükleniyor...</p>
        </div>
      </div>
    );
  }

  // Authenticated değilse login'e yönlendir
  if (!isAuthenticated) {
    return <Navigate to="/store/login" state={{ from: location }} replace />;
  }

  // Authenticated ise children'ı göster
  return children;
}

// ============================================================================
// STORE ATTENDANT LOGIN GUARD
// Zaten giriş yapmışsa dashboard'a yönlendir
// ============================================================================
export function StoreAttendantLoginGuard({ children }) {
  const { isAuthenticated, loading } = useStoreAttendantAuth();
  const location = useLocation();

  // Yükleme durumunda spinner göster
  if (loading) {
    return (
      <div
        className="min-vh-100 d-flex align-items-center justify-content-center"
        style={{
          background: "linear-gradient(135deg, #2E7D32 0%, #66BB6A 100%)",
        }}
      >
        <div className="text-center text-white">
          <div
            className="spinner-border mb-3"
            style={{ width: "3rem", height: "3rem" }}
          ></div>
          <p>Yükleniyor...</p>
        </div>
      </div>
    );
  }

  // Zaten authenticated ise dashboard'a yönlendir
  if (isAuthenticated) {
    const from = location.state?.from?.pathname || "/store/dashboard";
    return <Navigate to={from} replace />;
  }

  // Authenticated değilse login formunu göster
  return children;
}

export default StoreAttendantGuard;
