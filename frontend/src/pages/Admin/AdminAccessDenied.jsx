// =============================================================================
// AdminAccessDenied.jsx - Erişim Engellendi Sayfası
// =============================================================================
// Bu sayfa, yetkisiz erişim denemelerinde gösterilir.
// Kullanıcıya neden erişemediği hakkında bilgi verir.
// =============================================================================

import React from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";

const AdminAccessDenied = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();

  // Location state'den gelen bilgiler
  const { message, requiredPermission, from } = location.state || {};

  const handleGoBack = () => {
    if (from && from.pathname !== location.pathname) {
      navigate(-1);
    } else {
      navigate("/admin/dashboard");
    }
  };

  const handleGoHome = () => {
    navigate("/admin/dashboard");
  };

  const handleLogout = () => {
    navigate("/admin/login");
  };

  // ============================================================================
  // MOBİL OPTİMİZASYON - Responsive tasarım, küçük ekranlara uygun boyutlar
  // ============================================================================
  return (
    <div className="container-fluid px-2 px-sm-3">
      <div
        className="d-flex align-items-center justify-content-center"
        style={{ minHeight: "calc(100vh - 120px)" }}
      >
        <div className="text-center w-100" style={{ maxWidth: "450px" }}>
          {/* Icon - Mobilde daha küçük */}
          <div className="mb-3 mb-sm-4">
            <div
              className="d-inline-flex align-items-center justify-content-center rounded-circle bg-danger bg-opacity-10"
              style={{ width: "80px", height: "80px" }}
            >
              <i
                className="bi bi-shield-lock-fill text-danger"
                style={{ fontSize: "2.5rem" }}
              ></i>
            </div>
          </div>

          {/* Başlık - Mobilde daha kompakt */}
          <h1
            className="fw-bold text-danger mb-2"
            style={{ fontSize: "clamp(2rem, 8vw, 3.5rem)" }}
          >
            403
          </h1>
          <h2
            className="mb-2"
            style={{ fontSize: "clamp(1rem, 4vw, 1.25rem)" }}
          >
            Erişim Engellendi
          </h2>

          {/* Mesaj - Mobilde daha kısa padding */}
          <p
            className="text-muted mb-3 px-2"
            style={{ fontSize: "clamp(0.8rem, 3vw, 0.95rem)" }}
          >
            {message || "Bu sayfaya erişim yetkiniz bulunmamaktadır."}
          </p>

          {/* Gerekli İzinler - Kompakt versiyon */}
          {requiredPermission && requiredPermission.length > 0 && (
            <div className="mb-3">
              <div
                className="alert alert-warning py-2 px-3 mx-auto"
                style={{ maxWidth: "100%" }}
              >
                <small className="d-block fw-medium mb-1">
                  <i className="bi bi-info-circle me-1"></i>
                  Gerekli İzinler
                </small>
                <div className="d-flex flex-wrap gap-1 justify-content-center">
                  {requiredPermission.map((perm, index) => (
                    <span
                      key={index}
                      className="badge bg-warning text-dark"
                      style={{ fontSize: "0.7rem" }}
                    >
                      {perm}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* Kullanıcı Bilgisi - Kompakt kart */}
          {user && (
            <div className="mb-3">
              <div className="card mx-auto" style={{ maxWidth: "100%" }}>
                <div className="card-body py-2 px-3">
                  <small
                    className="text-muted d-block"
                    style={{ fontSize: "0.75rem" }}
                  >
                    Oturum Açan Kullanıcı
                  </small>
                  <span
                    className="fw-medium"
                    style={{ fontSize: "0.85rem", wordBreak: "break-all" }}
                  >
                    {user.email}
                  </span>
                  {user.role && (
                    <span
                      className="badge bg-secondary ms-2"
                      style={{ fontSize: "0.65rem" }}
                    >
                      {user.role}
                    </span>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* Butonlar - Mobilde dikey, daha küçük */}
          <div className="d-flex flex-column gap-2 px-3">
            <button
              onClick={handleGoBack}
              className="btn btn-outline-secondary btn-sm"
              style={{ fontSize: "0.85rem" }}
            >
              <i className="bi bi-arrow-left me-1"></i>
              GERİ DÖN
            </button>
            <button
              onClick={handleGoHome}
              className="btn btn-primary btn-sm"
              style={{ fontSize: "0.85rem" }}
            >
              <i className="bi bi-house me-1"></i>
              ANA SAYFA
            </button>
            <button
              onClick={handleLogout}
              className="btn btn-outline-danger btn-sm"
              style={{ fontSize: "0.85rem" }}
            >
              <i className="bi bi-box-arrow-right me-1"></i>
              ÇIKIŞ YAP
            </button>
          </div>

          {/* Yardım Metni - Daha küçük */}
          <div className="mt-3 px-2">
            <p className="text-muted mb-0" style={{ fontSize: "0.7rem" }}>
              <i className="bi bi-question-circle me-1"></i>
              Yetki sorunu için sistem yöneticinize başvurun.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminAccessDenied;
