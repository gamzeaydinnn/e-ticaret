// ==========================================================================
// StoreAttendantAuthContext.jsx - Market Görevlisi Auth Context
// ==========================================================================
// Store Attendant kullanıcıları için authentication ve session yönetimi.
// Token tabanlı authentication ile güvenli oturum yönetimi sağlar.
// ==========================================================================

import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
} from "react";

// Context oluştur
const StoreAttendantAuthContext = createContext(null);

// Storage key'leri
const STORAGE_KEYS = {
  TOKEN: "storeAttendantToken",
  USER: "storeAttendantUser",
};

// API base URL
const API_BASE = process.env.REACT_APP_API_URL || "http://localhost:5002/api";

// ============================================================================
// PROVIDER COMPONENT
// ============================================================================
export function StoreAttendantAuthProvider({ children }) {
  const [attendant, setAttendant] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // =========================================================================
  // TOKEN YARDIMCI FONKSİYONLARI
  // =========================================================================
  const getStoredToken = useCallback(() => {
    return (
      localStorage.getItem(STORAGE_KEYS.TOKEN) ||
      sessionStorage.getItem(STORAGE_KEYS.TOKEN)
    );
  }, []);

  const getStoredUser = useCallback(() => {
    const stored =
      localStorage.getItem(STORAGE_KEYS.USER) ||
      sessionStorage.getItem(STORAGE_KEYS.USER);
    if (stored) {
      try {
        return JSON.parse(stored);
      } catch {
        return null;
      }
    }
    return null;
  }, []);

  const saveAuth = useCallback((token, user, remember = true) => {
    const storage = remember ? localStorage : sessionStorage;
    storage.setItem(STORAGE_KEYS.TOKEN, token);
    storage.setItem(STORAGE_KEYS.USER, JSON.stringify(user));
    setAttendant(user);
  }, []);

  const clearAuth = useCallback(() => {
    localStorage.removeItem(STORAGE_KEYS.TOKEN);
    localStorage.removeItem(STORAGE_KEYS.USER);
    sessionStorage.removeItem(STORAGE_KEYS.TOKEN);
    sessionStorage.removeItem(STORAGE_KEYS.USER);
    setAttendant(null);
  }, []);

  // =========================================================================
  // BAŞLANGIÇ KONTROLÜ
  // =========================================================================
  useEffect(() => {
    const initAuth = async () => {
      const token = getStoredToken();
      const user = getStoredUser();

      if (token && user) {
        // Token'ı validate et
        try {
          const response = await fetch(`${API_BASE}/Auth/validate`, {
            method: "GET",
            headers: {
              Authorization: `Bearer ${token}`,
              "Content-Type": "application/json",
            },
          });

          if (response.ok) {
            setAttendant(user);
          } else {
            // Token geçersiz
            clearAuth();
          }
        } catch (err) {
          console.warn("[StoreAttendantAuth] Token validation hatası:", err);
          // Offline durumda mevcut session'ı koru
          setAttendant(user);
        }
      }

      setLoading(false);
    };

    initAuth();
  }, [getStoredToken, getStoredUser, clearAuth]);

  // =========================================================================
  // LOGIN
  // =========================================================================
  const login = useCallback(
    async (email, password, remember = true) => {
      setError(null);

      try {
        const response = await fetch(`${API_BASE}/Auth/login`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ email, password }),
        });

        const data = await response.json();

        if (response.ok && data.token) {
          // Rol kontrolü - StoreAttendant veya Admin olmalı
          const userRole = data.role || data.roles?.[0] || "";
          const allowedRoles = [
            "StoreAttendant",
            "Admin",
            "SuperAdmin",
            "StoreManager",
          ];

          if (
            !allowedRoles.some((r) =>
              userRole.toLowerCase().includes(r.toLowerCase()),
            )
          ) {
            return {
              success: false,
              error:
                "Bu panel için yetkiniz bulunmuyor. StoreAttendant veya Admin rolü gerekli.",
            };
          }

          // Kullanıcı bilgilerini hazırla
          const user = {
            id: data.userId || data.id,
            email: data.email || email,
            name:
              data.name ||
              data.fullName ||
              data.userName ||
              email.split("@")[0],
            role: userRole,
          };

          saveAuth(data.token, user, remember);
          return { success: true };
        } else {
          const errorMsg = data.message || data.error || "Giriş başarısız";
          setError(errorMsg);
          return { success: false, error: errorMsg };
        }
      } catch (err) {
        console.error("[StoreAttendantAuth] Login hatası:", err);
        const errorMsg = "Bağlantı hatası. Lütfen tekrar deneyin.";
        setError(errorMsg);
        return { success: false, error: errorMsg };
      }
    },
    [saveAuth],
  );

  // =========================================================================
  // LOGOUT
  // =========================================================================
  const logout = useCallback(async () => {
    const token = getStoredToken();

    // Backend'e logout bildir (opsiyonel)
    if (token) {
      try {
        await fetch(`${API_BASE}/Auth/logout`, {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        });
      } catch (err) {
        console.warn("[StoreAttendantAuth] Logout API hatası:", err);
      }
    }

    clearAuth();
  }, [getStoredToken, clearAuth]);

  // =========================================================================
  // CONTEXT VALUE
  // =========================================================================
  const value = {
    attendant,
    isAuthenticated: !!attendant,
    loading,
    error,
    login,
    logout,
    getToken: getStoredToken,
  };

  return (
    <StoreAttendantAuthContext.Provider value={value}>
      {children}
    </StoreAttendantAuthContext.Provider>
  );
}

// ============================================================================
// HOOK
// ============================================================================
export function useStoreAttendantAuth() {
  const context = useContext(StoreAttendantAuthContext);

  if (!context) {
    throw new Error(
      "useStoreAttendantAuth must be used within StoreAttendantAuthProvider",
    );
  }

  return context;
}

export default StoreAttendantAuthContext;
