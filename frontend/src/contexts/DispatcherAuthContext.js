// ==========================================================================
// DispatcherAuthContext.js - Sevkiyat Görevlisi Kimlik Doğrulama Context
// ==========================================================================
// JWT token yönetimi, otomatik refresh, logout ve oturum durumu.
// Tüm dispatcher paneli bu context'i kullanarak auth durumunu yönetir.
// NEDEN: Dispatcher rolü için ayrı auth context, güvenlik ve modülerlik sağlar.
// ==========================================================================

import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
} from "react";
import api from "../services/api";

// Context oluştur
const DispatcherAuthContext = createContext(null);

// ============================================================================
// YARDIMCI FONKSİYONLAR
// ============================================================================

/**
 * Token decode helper (JWT payload'ını parse eder)
 * NEDEN: JWT içinden kullanıcı bilgilerini ve expiry süresini almak için
 */
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

/**
 * Token'ın expire olup olmadığını kontrol et
 * NEDEN: Kullanıcı deneyimini bozmadan otomatik refresh yapabilmek için
 */
const isTokenExpired = (token) => {
  const decoded = decodeToken(token);
  if (!decoded || !decoded.exp) return true;
  // 30 saniye önce expire sayıyoruz (buffer)
  return decoded.exp * 1000 < Date.now() + 30000;
};

/**
 * Token'dan rol bilgisini çıkarır
 * NEDEN: Dispatcher yetkisi kontrolü için
 */
const getRoleFromToken = (token) => {
  const decoded = decodeToken(token);
  if (!decoded) return null;
  // ASP.NET Core Identity rol claim'leri
  return (
    decoded.role ||
    decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
    null
  );
};

// Storage keys - Dispatcher'a özel
const TOKEN_KEY = "dispatcherToken";
const REFRESH_TOKEN_KEY = "dispatcherRefreshToken";
const DISPATCHER_DATA_KEY = "dispatcherData";
const REMEMBER_ME_KEY = "dispatcherRememberMe";

// Dispatcher için geçerli roller
const VALID_DISPATCHER_ROLES = [
  "Dispatcher",
  "StoreManager",
  "Admin",
  "SuperAdmin",
];

// ============================================================================
// PROVIDER COMPONENT
// ============================================================================

export function DispatcherAuthProvider({ children }) {
  // State tanımları
  const [dispatcher, setDispatcher] = useState(null);
  const [token, setToken] = useState(null);
  const [refreshToken, setRefreshToken] = useState(null);
  const [loading, setLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  // =========================================================================
  // LOGOUT FONKSİYONU
  // NEDEN: Tüm oturum verilerini temizleyerek güvenli çıkış sağlar
  // =========================================================================
  const logout = useCallback(() => {
    // Tüm storage'ları temizle
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(DISPATCHER_DATA_KEY);
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
    sessionStorage.removeItem(DISPATCHER_DATA_KEY);

    // State'leri sıfırla
    setDispatcher(null);
    setToken(null);
    setRefreshToken(null);
    setIsAuthenticated(false);

    // Event dispatch (SignalR bağlantısını kesmek için)
    window.dispatchEvent(new CustomEvent("dispatcherLogout"));
  }, []);

  // =========================================================================
  // TOKEN YENİLEME
  // NEDEN: Kullanıcı oturumu sürerken token'ı otomatik yenileyerek kesintisiz deneyim sağlar
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

      // Backend'e refresh token isteği gönder
      const response = await api.post("/api/auth/refresh-token", {
        token: currentToken,
        refreshToken: currentRefreshToken,
      });

      if (response.data && response.data.token) {
        const rememberMe = localStorage.getItem(REMEMBER_ME_KEY) === "true";
        const storage = rememberMe ? localStorage : sessionStorage;

        storage.setItem(TOKEN_KEY, response.data.token);
        if (response.data.refreshToken) {
          storage.setItem(REFRESH_TOKEN_KEY, response.data.refreshToken);
          setRefreshToken(response.data.refreshToken);
        }

        setToken(response.data.token);
        return response.data.token;
      } else {
        logout();
        return null;
      }
    } catch (error) {
      console.error("[DispatcherAuth] Token yenileme hatası:", error);
      logout();
      return null;
    }
  }, [logout]);

  // =========================================================================
  // GİRİŞ FONKSİYONU
  // NEDEN: Dispatcher rolü ile güvenli giriş ve rol doğrulaması sağlar
  // =========================================================================
  const login = async (email, password, rememberMe = false) => {
    try {
      // Backend'e login isteği gönder
      const response = await api.post("/api/auth/login", {
        email,
        password,
      });

      if (response.data && response.data.token) {
        const receivedToken = response.data.token;

        // Token'dan rol bilgisini al
        const userRole = getRoleFromToken(receivedToken);

        // Dispatcher yetkisi kontrolü
        if (!VALID_DISPATCHER_ROLES.includes(userRole)) {
          return {
            success: false,
            error: "Bu hesabın Sevkiyat Paneline erişim yetkisi yok",
          };
        }

        const storage = rememberMe ? localStorage : sessionStorage;

        // Token'ları kaydet
        storage.setItem(TOKEN_KEY, receivedToken);
        if (response.data.refreshToken) {
          storage.setItem(REFRESH_TOKEN_KEY, response.data.refreshToken);
          setRefreshToken(response.data.refreshToken);
        }

        // Kullanıcı bilgilerini hazırla
        const dispatcherData = {
          id: response.data.userId || response.data.id,
          name:
            response.data.fullName ||
            response.data.name ||
            `${response.data.firstName || ""} ${response.data.lastName || ""}`.trim(),
          email: response.data.email || email,
          role: userRole,
          permissions: response.data.permissions || [],
        };

        storage.setItem(DISPATCHER_DATA_KEY, JSON.stringify(dispatcherData));

        // Remember me tercihini kaydet
        localStorage.setItem(REMEMBER_ME_KEY, rememberMe.toString());

        // State'leri güncelle
        setToken(receivedToken);
        setDispatcher(dispatcherData);
        setIsAuthenticated(true);

        // Event dispatch (SignalR bağlantısı için)
        window.dispatchEvent(
          new CustomEvent("dispatcherLogin", {
            detail: { dispatcherId: dispatcherData.id },
          }),
        );

        return { success: true, dispatcher: dispatcherData };
      } else {
        return {
          success: false,
          error: response.data?.message || "Giriş başarısız",
        };
      }
    } catch (error) {
      console.error("[DispatcherAuth] Giriş hatası:", error);

      // HTTP hata kodlarına göre mesaj
      if (error.response) {
        const status = error.response.status;
        const message = error.response.data?.message;

        if (status === 401) {
          return { success: false, error: "Geçersiz e-posta veya şifre" };
        } else if (status === 403) {
          return { success: false, error: "Bu hesabın erişim yetkisi yok" };
        } else if (status === 429) {
          return { success: false, error: "Çok fazla deneme. Lütfen bekleyin" };
        } else if (status >= 500) {
          return {
            success: false,
            error: "Sunucu hatası. Lütfen daha sonra deneyin",
          };
        }
        return { success: false, error: message || "Giriş başarısız" };
      }

      return { success: false, error: "Bağlantı hatası" };
    }
  };

  // =========================================================================
  // OTURUMU KONTROL ET (Sayfa yüklendiğinde)
  // NEDEN: Sayfa yenilendiğinde veya uygulama açıldığında mevcut oturumu restore eder
  // =========================================================================
  useEffect(() => {
    const restoreSession = async () => {
      try {
        // Storage'dan token'ı al
        const storedToken =
          localStorage.getItem(TOKEN_KEY) || sessionStorage.getItem(TOKEN_KEY);
        const storedRefreshToken =
          localStorage.getItem(REFRESH_TOKEN_KEY) ||
          sessionStorage.getItem(REFRESH_TOKEN_KEY);
        const storedDispatcher =
          localStorage.getItem(DISPATCHER_DATA_KEY) ||
          sessionStorage.getItem(DISPATCHER_DATA_KEY);

        if (!storedToken) {
          setLoading(false);
          return;
        }

        // Token expire olmuş mu kontrol et
        if (isTokenExpired(storedToken)) {
          if (storedRefreshToken) {
            // Token'ı yenilemeyi dene
            const newToken = await refreshAccessToken();
            if (!newToken) {
              setLoading(false);
              return;
            }
          } else {
            // Refresh token yoksa çıkış yap
            logout();
            setLoading(false);
            return;
          }
        }

        // Rol kontrolü yap
        const userRole = getRoleFromToken(storedToken);
        if (!VALID_DISPATCHER_ROLES.includes(userRole)) {
          logout();
          setLoading(false);
          return;
        }

        // Dispatcher verilerini restore et
        if (storedDispatcher) {
          const dispatcherData = JSON.parse(storedDispatcher);
          setDispatcher(dispatcherData);
          setToken(storedToken);
          setRefreshToken(storedRefreshToken);
          setIsAuthenticated(true);
        }
      } catch (error) {
        console.error("[DispatcherAuth] Oturum restore hatası:", error);
        logout();
      } finally {
        setLoading(false);
      }
    };

    restoreSession();
  }, [logout, refreshAccessToken]);

  // =========================================================================
  // TOKEN OTOMATİK YENİLEME (Arka planda)
  // NEDEN: Token expire olmadan önce otomatik yenileme ile kesintisiz oturum sağlar
  // =========================================================================
  useEffect(() => {
    if (!token || !isAuthenticated) return;

    // Token'ın kalan süresini hesapla
    const decoded = decodeToken(token);
    if (!decoded || !decoded.exp) return;

    const expiresIn = decoded.exp * 1000 - Date.now();
    // Token expire olmadan 2 dakika önce yenile
    const refreshIn = Math.max(expiresIn - 120000, 10000);

    const timerId = setTimeout(async () => {
      await refreshAccessToken();
    }, refreshIn);

    return () => clearTimeout(timerId);
  }, [token, isAuthenticated, refreshAccessToken]);

  // =========================================================================
  // CONTEXT VALUE
  // =========================================================================
  const contextValue = {
    dispatcher,
    token,
    loading,
    isAuthenticated,
    login,
    logout,
    refreshAccessToken,
  };

  return (
    <DispatcherAuthContext.Provider value={contextValue}>
      {children}
    </DispatcherAuthContext.Provider>
  );
}

// ============================================================================
// CUSTOM HOOK
// ============================================================================

/**
 * Dispatcher auth context'ini kullanmak için hook
 * NEDEN: Context'i direkt useContext ile kullanmak yerine,
 *        bu hook ile hata kontrolü ve tip güvenliği sağlanır
 */
export const useDispatcherAuth = () => {
  const context = useContext(DispatcherAuthContext);
  if (!context) {
    throw new Error(
      "useDispatcherAuth must be used within a DispatcherAuthProvider",
    );
  }
  return context;
};

export default DispatcherAuthContext;
