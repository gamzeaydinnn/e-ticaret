// ==========================================================================
// CourierAuthContext.js - Kurye Kimlik Doğrulama Context
// ==========================================================================
// JWT token yönetimi, otomatik refresh, logout ve oturum durumu.
// Tüm kurye paneli bu context'i kullanarak auth durumunu yönetir.
// ==========================================================================

import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
} from "react";
import { CourierService } from "../services/courierService";

// Context oluştur
const CourierAuthContext = createContext(null);

// Token decode helper (JWT payload'ını parse eder)
const decodeToken = (token) => {
  try {
    if (!token) return null;
    const payload = token.split(".")[1];
    const decoded = JSON.parse(atob(payload));
    return decoded;
  } catch {
    return null;
  }
};

// Token'ın expire olup olmadığını kontrol et
const isTokenExpired = (token) => {
  const decoded = decodeToken(token);
  if (!decoded || !decoded.exp) return true;
  // 30 saniye önce expire sayıyoruz (buffer)
  return decoded.exp * 1000 < Date.now() + 30000;
};

// Storage keys
const TOKEN_KEY = "courierToken";
const REFRESH_TOKEN_KEY = "courierRefreshToken";
const COURIER_DATA_KEY = "courierData";
const REMEMBER_ME_KEY = "courierRememberMe";

export function CourierAuthProvider({ children }) {
  // State tanımları
  const [courier, setCourier] = useState(null);
  const [token, setToken] = useState(null);
  const [refreshToken, setRefreshToken] = useState(null);
  const [loading, setLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isOnline, setIsOnline] = useState(true);

  // =========================================================================
  // LOGOUT FONKSİYONU
  // =========================================================================
  const logout = useCallback(() => {
    // Tüm storage'ları temizle
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(COURIER_DATA_KEY);
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
    sessionStorage.removeItem(COURIER_DATA_KEY);

    // State'leri sıfırla
    setCourier(null);
    setToken(null);
    setRefreshToken(null);
    setIsAuthenticated(false);

    // Event dispatch (SignalR bağlantısını kesmek için)
    window.dispatchEvent(new CustomEvent("courierLogout"));
  }, []);

  // =========================================================================
  // TOKEN YENİLEME
  // =========================================================================
  const refreshAccessToken = useCallback(async () => {
    try {
      const currentRefreshToken =
        localStorage.getItem(REFRESH_TOKEN_KEY) ||
        sessionStorage.getItem(REFRESH_TOKEN_KEY);
      const currentToken =
        localStorage.getItem(TOKEN_KEY) || sessionStorage.getItem(TOKEN_KEY);

      if (!currentRefreshToken || !currentToken) {
        logout();
        return null;
      }

      const result = await CourierService.refreshToken(
        currentToken,
        currentRefreshToken,
      );

      if (result.success && result.token) {
        const rememberMe = localStorage.getItem(REMEMBER_ME_KEY) === "true";
        const storage = rememberMe ? localStorage : sessionStorage;

        storage.setItem(TOKEN_KEY, result.token);
        if (result.refreshToken) {
          storage.setItem(REFRESH_TOKEN_KEY, result.refreshToken);
          setRefreshToken(result.refreshToken);
        }

        setToken(result.token);
        return result.token;
      } else {
        logout();
        return null;
      }
    } catch (error) {
      console.error("Token yenileme hatası:", error);
      logout();
      return null;
    }
  }, [logout]);

  // =========================================================================
  // GİRİŞ FONKSİYONU
  // =========================================================================
  const login = async (email, password, rememberMe = false) => {
    try {
      const result = await CourierService.login(email, password);

      if (result.success) {
        const storage = rememberMe ? localStorage : sessionStorage;

        // Token'ları kaydet
        storage.setItem(TOKEN_KEY, result.token);
        if (result.refreshToken) {
          storage.setItem(REFRESH_TOKEN_KEY, result.refreshToken);
          setRefreshToken(result.refreshToken);
        }
        storage.setItem(COURIER_DATA_KEY, JSON.stringify(result.courier));

        // Remember me tercihini kaydet
        localStorage.setItem(REMEMBER_ME_KEY, rememberMe.toString());

        // State'leri güncelle
        setToken(result.token);
        setCourier(result.courier);
        setIsAuthenticated(true);

        // Event dispatch (SignalR bağlantısı için)
        window.dispatchEvent(
          new CustomEvent("courierLogin", {
            detail: { courierId: result.courier.id },
          }),
        );

        return { success: true, courier: result.courier };
      }

      return { success: false, error: result.message || "Giriş başarısız" };
    } catch (error) {
      console.error("Login hatası:", error);
      return {
        success: false,
        error: error.message || "Giriş yapılırken bir hata oluştu",
      };
    }
  };

  // =========================================================================
  // ŞİFRE SIFIRLAMA İSTEĞİ
  // =========================================================================
  const requestPasswordReset = async (email) => {
    try {
      const result = await CourierService.requestPasswordReset(email);
      return result;
    } catch (error) {
      console.error("Şifre sıfırlama isteği hatası:", error);
      return {
        success: false,
        error: error.message || "Şifre sıfırlama isteği gönderilemedi",
      };
    }
  };

  // =========================================================================
  // ŞİFRE DEĞİŞTİRME
  // =========================================================================
  const changePassword = async (currentPassword, newPassword) => {
    try {
      const result = await CourierService.changePassword(
        currentPassword,
        newPassword,
      );
      return result;
    } catch (error) {
      console.error("Şifre değiştirme hatası:", error);
      return {
        success: false,
        error: error.message || "Şifre değiştirilemedi",
      };
    }
  };

  // =========================================================================
  // ONLİNE/OFFLİNE DURUM DEĞİŞTİRME
  // =========================================================================
  const toggleOnlineStatus = async (online) => {
    try {
      const result = await CourierService.updateOnlineStatus(
        courier?.id,
        online,
      );
      if (result.success) {
        setIsOnline(online);
        setCourier((prev) => (prev ? { ...prev, isOnline: online } : null));
      }
      return result;
    } catch (error) {
      console.error("Online durum güncelleme hatası:", error);
      return { success: false, error: error.message };
    }
  };

  // =========================================================================
  // KURYE BİLGİLERİNİ GÜNCELLE
  // =========================================================================
  const updateCourierData = (data) => {
    setCourier((prev) => {
      const updated = { ...prev, ...data };
      // Storage'ı güncelle
      const rememberMe = localStorage.getItem(REMEMBER_ME_KEY) === "true";
      const storage = rememberMe ? localStorage : sessionStorage;
      storage.setItem(COURIER_DATA_KEY, JSON.stringify(updated));
      return updated;
    });
  };

  // =========================================================================
  // İLK YÜKLEME - OTURUM KONTROLÜ
  // =========================================================================
  useEffect(() => {
    const initializeAuth = async () => {
      try {
        // Önce localStorage, sonra sessionStorage kontrol et
        let savedToken =
          localStorage.getItem(TOKEN_KEY) || sessionStorage.getItem(TOKEN_KEY);
        let savedRefreshToken =
          localStorage.getItem(REFRESH_TOKEN_KEY) ||
          sessionStorage.getItem(REFRESH_TOKEN_KEY);
        let savedCourier =
          localStorage.getItem(COURIER_DATA_KEY) ||
          sessionStorage.getItem(COURIER_DATA_KEY);

        if (!savedToken || !savedCourier) {
          setLoading(false);
          return;
        }

        // Token expire olmuş mu kontrol et
        if (isTokenExpired(savedToken)) {
          // Refresh token ile yenilemeyi dene
          if (savedRefreshToken) {
            const newToken = await refreshAccessToken();
            if (!newToken) {
              setLoading(false);
              return;
            }
            savedToken = newToken;
          } else {
            logout();
            setLoading(false);
            return;
          }
        }

        // Kurye verilerini parse et
        const courierData = JSON.parse(savedCourier);

        setToken(savedToken);
        setRefreshToken(savedRefreshToken);
        setCourier(courierData);
        setIsOnline(courierData.isOnline ?? true);
        setIsAuthenticated(true);
      } catch (error) {
        console.error("Auth başlatma hatası:", error);
        logout();
      } finally {
        setLoading(false);
      }
    };

    initializeAuth();
  }, [logout, refreshAccessToken]);

  // =========================================================================
  // TOKEN OTOMATİK YENİLEME TIMER
  // =========================================================================
  useEffect(() => {
    if (!token || !isAuthenticated) return;

    // Token'ın expire süresini hesapla
    const decoded = decodeToken(token);
    if (!decoded?.exp) return;

    const expiresIn = decoded.exp * 1000 - Date.now();
    // Expire'dan 2 dakika önce yenile
    const refreshTime = Math.max(expiresIn - 120000, 30000);

    const timer = setTimeout(() => {
      refreshAccessToken();
    }, refreshTime);

    return () => clearTimeout(timer);
  }, [token, isAuthenticated, refreshAccessToken]);

  // =========================================================================
  // AUTH HEADER GETTER
  // =========================================================================
  const getAuthHeader = useCallback(() => {
    return token ? { Authorization: `Bearer ${token}` } : {};
  }, [token]);

  // =========================================================================
  // CONTEXT VALUE
  // =========================================================================
  const value = {
    // State
    courier,
    token,
    loading,
    isAuthenticated,
    isOnline,

    // Actions
    login,
    logout,
    refreshAccessToken,
    requestPasswordReset,
    changePassword,
    toggleOnlineStatus,
    updateCourierData,
    getAuthHeader,
  };

  return (
    <CourierAuthContext.Provider value={value}>
      {children}
    </CourierAuthContext.Provider>
  );
}

// =========================================================================
// CUSTOM HOOK
// =========================================================================
export function useCourierAuth() {
  const context = useContext(CourierAuthContext);
  if (!context) {
    throw new Error("useCourierAuth must be used within a CourierAuthProvider");
  }
  return context;
}

export default CourierAuthContext;
