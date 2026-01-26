// ==========================================================================
// DispatcherGuard.jsx - Sevkiyat Görevlisi Route Guard
// ==========================================================================
// Dispatcher rotalarını korur ve yetki kontrolü yapar.
// NEDEN: Yetkisiz erişimi engellemek ve güvenlik sağlamak için.
// ==========================================================================

import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useDispatcherAuth } from "../contexts/DispatcherAuthContext";

// Dispatcher için geçerli roller
const VALID_DISPATCHER_ROLES = [
  "Dispatcher",
  "StoreManager",
  "Admin",
  "SuperAdmin",
];

/**
 * Dispatcher yetkisi kontrolü yapan guard component
 *
 * @param {Object} props
 * @param {React.ReactNode} props.children - Korunan içerik
 * @param {string|string[]} props.requiredPermission - Gerekli izin(ler) (opsiyonel)
 * @param {string} props.fallbackPath - Yetkisiz durumda yönlendirilecek yol
 */
export const DispatcherGuard = ({
  children,
  requiredPermission = null,
  fallbackPath = "/dispatch/login",
}) => {
  const { dispatcher, loading, isAuthenticated } = useDispatcherAuth();
  const location = useLocation();

  // ============================================================================
  // YÜKLEME DURUMU
  // NEDEN: Auth kontrolü tamamlanmadan render yapmamak için
  // ============================================================================
  if (loading) {
    return (
      <div
        className="min-vh-100 d-flex justify-content-center align-items-center"
        style={{
          background: "linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)",
        }}
      >
        <div className="text-center">
          <div
            className="spinner-border text-primary mb-3"
            role="status"
            style={{ width: "3rem", height: "3rem" }}
          >
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="text-white-50 mb-0">Oturum kontrol ediliyor...</p>
        </div>
      </div>
    );
  }

  // ============================================================================
  // AUTHENTICATION KONTROLÜ
  // NEDEN: Giriş yapmamış kullanıcıları login sayfasına yönlendirir
  // ============================================================================
  if (!isAuthenticated || !dispatcher) {
    // Gitmek istediği sayfayı state olarak geçir (login sonrası yönlendirme için)
    return <Navigate to={fallbackPath} state={{ from: location }} replace />;
  }

  // ============================================================================
  // ROL KONTROLÜ
  // NEDEN: Sadece yetkili rollerin erişimine izin verir
  // ============================================================================
  const userRole = dispatcher.role;
  if (!VALID_DISPATCHER_ROLES.includes(userRole)) {
    console.warn("[DispatcherGuard] Yetkisiz rol:", userRole);
    return (
      <Navigate
        to={fallbackPath}
        state={{ error: "Bu sayfaya erişim yetkiniz yok" }}
        replace
      />
    );
  }

  // ============================================================================
  // İZİN KONTROLÜ (Opsiyonel)
  // NEDEN: Belirli sayfalar için spesifik izin gerekebilir
  // ============================================================================
  if (requiredPermission) {
    const permissions = dispatcher.permissions || [];
    const requiredPermissions = Array.isArray(requiredPermission)
      ? requiredPermission
      : [requiredPermission];

    const hasPermission = requiredPermissions.some((perm) =>
      permissions.includes(perm),
    );

    if (!hasPermission) {
      console.warn("[DispatcherGuard] Eksik izin:", requiredPermissions);
      return (
        <Navigate
          to="/dispatch/access-denied"
          state={{
            requiredPermission: requiredPermissions,
            from: location,
          }}
          replace
        />
      );
    }
  }

  // ============================================================================
  // YETKİLİ ERİŞİM - İçeriği render et
  // ============================================================================
  return children;
};

/**
 * Login sayfası için guard - Zaten giriş yapmışsa dashboard'a yönlendirir
 * NEDEN: Gereksiz login sayfası gösterimini engeller
 */
export const DispatcherLoginGuard = ({ children }) => {
  const { isAuthenticated, loading } = useDispatcherAuth();
  const location = useLocation();

  // Yüklenirken bekle
  if (loading) {
    return (
      <div
        className="min-vh-100 d-flex justify-content-center align-items-center"
        style={{
          background: "linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)",
        }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  // Zaten giriş yapmışsa dashboard'a yönlendir
  if (isAuthenticated) {
    // Eğer önceki sayfa bilgisi varsa oraya, yoksa dashboard'a git
    const from = location.state?.from?.pathname || "/dispatch/dashboard";
    return <Navigate to={from} replace />;
  }

  return children;
};

export default DispatcherGuard;
