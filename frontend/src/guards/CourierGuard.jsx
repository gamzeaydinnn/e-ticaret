// ==========================================================================
// CourierGuard.jsx - Kurye Route Guard
// ==========================================================================

import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useCourierAuth } from "../contexts/CourierAuthContext";

export const CourierGuard = ({ children, fallbackPath = "/courier/login" }) => {
  const { courier, loading, isAuthenticated } = useCourierAuth();
  const location = useLocation();

  if (loading) {
    return (
      <div className="min-vh-100 d-flex justify-content-center align-items-center bg-light">
        <div className="text-center">
          <div
            className="spinner-border text-warning mb-3"
            role="status"
            style={{ width: "3rem", height: "3rem" }}
          >
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="text-muted mb-0">Oturum kontrol ediliyor...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated || !courier?.id) {
    return (
      <Navigate to={fallbackPath} state={{ from: location }} replace />
    );
  }

  return children;
};

export const CourierLoginGuard = ({ children }) => {
  const { isAuthenticated, loading } = useCourierAuth();
  const location = useLocation();

  if (loading) {
    return (
      <div className="min-vh-100 d-flex justify-content-center align-items-center bg-light">
        <div className="spinner-border text-warning" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  if (isAuthenticated) {
    const from = location.state?.from?.pathname || "/courier/dashboard";
    return <Navigate to={from} replace />;
  }

  return children;
};

export default CourierGuard;
