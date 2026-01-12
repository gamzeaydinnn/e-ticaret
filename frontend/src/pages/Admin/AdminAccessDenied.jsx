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

  return (
    <div className="container-fluid">
      <div
        className="d-flex align-items-center justify-content-center"
        style={{ minHeight: "calc(100vh - 200px)" }}
      >
        <div className="text-center">
          {/* Icon */}
          <div className="mb-4">
            <div
              className="d-inline-flex align-items-center justify-content-center rounded-circle bg-danger bg-opacity-10"
              style={{ width: "120px", height: "120px" }}
            >
              <i
                className="bi bi-shield-lock-fill text-danger"
                style={{ fontSize: "3.5rem" }}
              ></i>
            </div>
          </div>

          {/* Başlık */}
          <h1 className="display-4 fw-bold text-danger mb-3">403</h1>
          <h2 className="h3 mb-3">Erişim Engellendi</h2>

          {/* Mesaj */}
          <p
            className="text-muted mb-4"
            style={{ maxWidth: "500px", margin: "0 auto" }}
          >
            {message ||
              "Bu sayfaya erişim yetkiniz bulunmamaktadır. Yetki almak için sistem yöneticinize başvurun."}
          </p>

          {/* Gerekli İzinler */}
          {requiredPermission && requiredPermission.length > 0 && (
            <div className="mb-4">
              <div
                className="alert alert-warning d-inline-block"
                style={{ maxWidth: "500px" }}
              >
                <h6 className="alert-heading mb-2">
                  <i className="bi bi-info-circle me-2"></i>
                  Gerekli İzinler
                </h6>
                <div className="d-flex flex-wrap gap-2 justify-content-center">
                  {requiredPermission.map((perm, index) => (
                    <span key={index} className="badge bg-warning text-dark">
                      {perm}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* Kullanıcı Bilgisi */}
          {user && (
            <div className="mb-4">
              <div
                className="card d-inline-block"
                style={{ maxWidth: "400px" }}
              >
                <div className="card-body py-2 px-4">
                  <small className="text-muted d-block">
                    Oturum Açan Kullanıcı
                  </small>
                  <span className="fw-medium">{user.email}</span>
                  {user.role && (
                    <span className="badge bg-secondary ms-2">{user.role}</span>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* Butonlar */}
          <div className="d-flex flex-column flex-sm-row gap-3 justify-content-center">
            <button
              onClick={handleGoBack}
              className="btn btn-outline-secondary"
            >
              <i className="bi bi-arrow-left me-2"></i>
              Geri Dön
            </button>
            <button onClick={handleGoHome} className="btn btn-primary">
              <i className="bi bi-house me-2"></i>
              Ana Sayfa
            </button>
            <button onClick={handleLogout} className="btn btn-outline-danger">
              <i className="bi bi-box-arrow-right me-2"></i>
              Çıkış Yap
            </button>
          </div>

          {/* Yardım Metni */}
          <div className="mt-5">
            <p className="text-muted small mb-0">
              <i className="bi bi-question-circle me-1"></i>
              Yetki sorunu yaşıyorsanız, lütfen sistem yöneticinizle iletişime
              geçin.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminAccessDenied;
