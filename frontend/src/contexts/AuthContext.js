// src/contexts/AuthContext.js
// =============================================================================
// AuthContext - Kimlik Doğrulama ve Yetki Yönetimi
// =============================================================================
// Bu context, kullanıcı kimlik doğrulama işlemlerini ve izin yönetimini sağlar.
// Permission sistemi entegrasyonu ile birlikte çalışır.
// =============================================================================

import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
} from "react";
import { AuthService } from "../services/authService";
import { smsService } from "../services/otpService";
import permissionService from "../services/permissionService";

const AuthContext = createContext();

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // =========================================================================
  // Permission State - Kullanıcı izinleri
  // =========================================================================
  const [permissions, setPermissions] = useState(() => {
    // ============================================================================
    // İLK YÜKLENİRKEN localStorage'dan permissions'ı oku
    // Bu sayede sayfa refresh edildiğinde izinler hemen hazır olur
    // Race condition önlenir: AdminGuard render olduğunda permissions hazırdır
    // ============================================================================
    try {
      const cached = localStorage.getItem("userPermissions");
      if (cached) {
        const parsed = JSON.parse(cached);
        if (Array.isArray(parsed)) {
          return parsed;
        }
        if (Array.isArray(parsed?.permissions)) {
          return parsed.permissions;
        }
      }
    } catch (error) {
      console.error("Permission cache okuma hatası:", error);
    }
    return [];
  });

  // ============================================================================
  // KRİTİK: permissionsLoading başlangıç değeri
  // Eğer user localStorage'da varsa ve admin rolündeyse, başlangıçta true olmalı
  // Bu sayede ilk render'da izin kontrolü yapılmadan önce yükleme tamamlanır
  // ============================================================================
  const [permissionsLoading, setPermissionsLoading] = useState(() => {
    try {
      const storedUser = localStorage.getItem("user");
      const token =
        localStorage.getItem("authToken") || localStorage.getItem("token");
      if (storedUser && token) {
        const parsed = JSON.parse(storedUser);
        const adminRoles = [
          "Admin",
          "SuperAdmin",
          "StoreManager",
          "CustomerSupport",
          "Logistics",
          "StoreAttendant",
          "Dispatcher",
        ];
        // Eğer admin kullanıcı ve cache'de permission yoksa, loading=true başla
        const cachedPerms = localStorage.getItem("userPermissions");
        if (
          (parsed.isAdmin || adminRoles.includes(parsed.role)) &&
          !cachedPerms
        ) {
          return true;
        }
      }
    } catch (e) {
      // Hata durumunda false devam et
    }
    return false;
  });
  const [permissionsError, setPermissionsError] = useState(null);

  // Demo kullanıcıları - localStorage'dan yükle veya default'ları kullan
  const getStoredDemoUsers = () => {
    try {
      const stored = localStorage.getItem("demoUsers");
      if (stored) {
        return JSON.parse(stored);
      }
    } catch (error) {
      console.error("Demo users parsing error:", error);
    }
    return [
      {
        id: 1,
        email: "demo@example.com",
        password: "123456",
        firstName: "Demo",
        lastName: "User",
      },
      {
        id: 2,
        email: "test@example.com",
        password: "123456",
        firstName: "Test",
        lastName: "User",
      },
      {
        id: 3,
        email: "user@example.com",
        password: "123456",
        firstName: "Example",
        lastName: "User",
      },
    ];
  };

  const [demoUsers, setDemoUsers] = useState(getStoredDemoUsers());

  // =========================================================================
  // Permission Loader - Kullanıcı izinlerini yükle
  // Cache süresi: 5 dakika (rol değişikliklerinin makul sürede yansıması için)
  // KRİTİK: Bu fonksiyon yüklenen izinleri döndürür - caller state güncelini bekleyebilir
  // =========================================================================
  const loadUserPermissions = useCallback(
    async (userData, forceRefresh = false) => {
      // Sadece admin kullanıcılar için izinleri yükle
      if (
        !userData ||
        (!userData.isAdmin &&
          ![
            "Admin",
            "SuperAdmin",
            "StoreManager",
            "CustomerSupport",
            "Logistics",
            "StoreAttendant",
            "Dispatcher",
          ].includes(userData.role))
      ) {
        setPermissions([]);
        localStorage.removeItem("userPermissions");
        localStorage.removeItem("permissionsCacheTime");
        localStorage.removeItem("permissionsCacheRole");
        return [];
      }

      setPermissionsLoading(true);
      setPermissionsError(null);

      try {
        // =========================================================================
        // Cache Kontrolü - Rol değişikliği durumunda cache geçersiz sayılır
        // =========================================================================
        const cachedPermissions = localStorage.getItem("userPermissions");
        const cacheTimestamp = localStorage.getItem("permissionsCacheTime");
        const cachedRole = localStorage.getItem("permissionsCacheRole");
        const cacheMaxAge = 1 * 60 * 1000; // 1 dakika — izin değişiklikleri hızlı yansısın

        // Cache geçerlilik kontrolü:
        // 1. forceRefresh true ise cache'i atla
        // 2. Rol değiştiyse cache'i atla (Madde 5 düzeltmesi)
        // 3. Cache süresi dolduysa cache'i atla
        const roleChanged = cachedRole && cachedRole !== userData.role;

        if (
          !forceRefresh &&
          !roleChanged &&
          cachedPermissions &&
          cacheTimestamp
        ) {
          const cacheAge = Date.now() - parseInt(cacheTimestamp, 10);
          if (cacheAge < cacheMaxAge) {
            const cached = JSON.parse(cachedPermissions);
            setPermissions(cached);
            setPermissionsLoading(false);
            return cached; // Cache'den döndür
          }
        }

        // API'den izinleri al
        const response = await permissionService.getMyPermissions();
        let loadedPermissions = [];

        if (response && Array.isArray(response.permissions)) {
          loadedPermissions = response.permissions;
        } else if (Array.isArray(response)) {
          loadedPermissions = response;
        }

        // State'i güncelle
        setPermissions(loadedPermissions);

        // Cache'e kaydet - rol bilgisi ile birlikte
        localStorage.setItem(
          "userPermissions",
          JSON.stringify(loadedPermissions),
        );
        localStorage.setItem("permissionsCacheTime", Date.now().toString());
        localStorage.setItem("permissionsCacheRole", userData.role);

        return loadedPermissions; // Yüklenen izinleri döndür
      } catch (error) {
        console.error("Permission loading error:", error);
        setPermissionsError(error.message || "İzinler yüklenirken hata oluştu");

        // Hata durumunda cache'den yükle (varsa ve rol değişmediyse)
        const cachedPermissions = localStorage.getItem("userPermissions");
        const cachedRole = localStorage.getItem("permissionsCacheRole");

        if (cachedPermissions && cachedRole === userData.role) {
          try {
            const cached = JSON.parse(cachedPermissions);
            setPermissions(cached);
            return cached;
          } catch (parseError) {
            console.error("Cache parse error:", parseError);
          }
        }
        return [];
      } finally {
        setPermissionsLoading(false);
      }
    },
    [],
  );

  // =========================================================================
  // Permission Helper Functions
  // =========================================================================

  /**
   * Belirli bir izne sahip mi kontrol et
   */
  const hasPermission = useCallback(
    (permission) => {
      if (!user) return false;

      // SuperAdmin her şeye erişebilir
      if (user.role === "SuperAdmin") return true;

      // İzin listesinde var mı?
      return permissions.includes(permission);
    },
    [user, permissions],
  );

  /**
   * Verilen izinlerden herhangi birine sahip mi?
   */
  const hasAnyPermission = useCallback(
    (...permissionList) => {
      if (!user) return false;
      if (user.role === "SuperAdmin") return true;

      return permissionList.some((p) => permissions.includes(p));
    },
    [user, permissions],
  );

  /**
   * Verilen tüm izinlere sahip mi?
   */
  const hasAllPermissions = useCallback(
    (...permissionList) => {
      if (!user) return false;
      if (user.role === "SuperAdmin") return true;

      return permissionList.every((p) => permissions.includes(p));
    },
    [user, permissions],
  );

  /**
   * Admin paneline erişim yetkisi var mı?
   */
  const canAccessAdminPanel = useCallback(() => {
    if (!user) return false;

    // Admin rolleri
    const adminRoles = [
      "Admin",
      "SuperAdmin",
      "StoreManager",
      "CustomerSupport",
      "Logistics",
      "StoreAttendant",
      "Dispatcher",
    ];
    if (adminRoles.includes(user.role)) return true;

    // isAdmin flag
    if (user.isAdmin) return true;

    // En az bir admin izni varsa
    return permissions.some(
      (p) =>
        p.startsWith("dashboard.") ||
        p.startsWith("products.") ||
        p.startsWith("orders."),
    );
  }, [user, permissions]);

  /**
   * İzin cache'ini temizle
   * Rol değişikliği sonrası çağrılmalı (Madde 5 düzeltmesi)
   */
  const clearPermissionsCache = useCallback(() => {
    setPermissions([]);
    localStorage.removeItem("userPermissions");
    localStorage.removeItem("permissionsCacheTime");
    localStorage.removeItem("permissionsCacheRole");
  }, []);

  /**
   * İzinleri yeniden yükle (cache'i atlayarak)
   * Rol değişikliği sonrası çağrılmalı (Madde 5 düzeltmesi)
   */
  const refreshPermissions = useCallback(async () => {
    clearPermissionsCache();
    if (user) {
      await loadUserPermissions(user, true); // forceRefresh = true
    }
  }, [user, loadUserPermissions, clearPermissionsCache]);

  useEffect(() => {
    // Token interceptor'ını kur
    AuthService.setupTokenInterceptor();

    // Sayfa yüklendiğinde token kontrolü yap
    const token = AuthService.getToken();
    const userData = localStorage.getItem("user");

    if (token && userData) {
      try {
        const parsedUser = JSON.parse(userData);
        setUser(parsedUser);

        // Kullanıcı izinlerini yükle
        loadUserPermissions(parsedUser);
      } catch (error) {
        console.error("User data parsing error:", error);
        clearUserData();
      }
    }

    setLoading(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const login = async (email, password) => {
    try {
      // Backend API çağrısı (axios interceptor data döndürür)
      const resp = await AuthService.login({ email, password });
      const data = resp && resp.data === undefined ? resp : resp.data; // her iki şekli destekle

      if (data && (data.success || data.token || data.Token)) {
        const userData = data.user ||
          data.User || {
            id: data.id,
            email,
            firstName: data.firstName,
            lastName: data.lastName,
            name:
              data.name ||
              `${data.firstName ?? ""} ${data.lastName ?? ""}`.trim(),
            role: data.role,
            isAdmin: data.isAdmin,
          };
        const token = data.token || data.Token;
        const refreshToken = data.refreshToken || data.RefreshToken;

        // Token ve kullanıcı bilgilerini kaydet
        AuthService.saveToken(token);
        if (refreshToken) {
          localStorage.setItem("refreshToken", refreshToken);
        }
        localStorage.setItem("user", JSON.stringify(userData));
        if (userData?.id != null) {
          localStorage.setItem("userId", String(userData.id));
        }

        setUser(userData);

        // Kullanıcı izinlerini yükle
        await loadUserPermissions(userData);

        return { success: true, user: userData };
      } else {
        return {
          success: false,
          error: (data && (data.message || data.error)) || "Giriş başarısız!",
        };
      }
    } catch (error) {
      console.error("Login error:", error);

      // Backend bağlantısı yoksa demo login'e geç (SADECE DEVELOPMENT)
      if (process.env.NODE_ENV === "development") {
        const demoUser = demoUsers.find(
          (u) => u.email === email && u.password === password,
        );

        if (demoUser) {
          const token = "demo_token_" + Date.now();
          const userData = {
            id: demoUser.id,
            email: demoUser.email,
            firstName: demoUser.firstName,
            lastName: demoUser.lastName,
            name: demoUser.name || `${demoUser.firstName} ${demoUser.lastName}`,
          };

          // Token ve kullanıcı bilgilerini kaydet
          AuthService.saveToken(token);
          localStorage.setItem("user", JSON.stringify(userData));
          localStorage.setItem("userId", demoUser.id.toString());

          setUser(userData);

          return { success: true, user: userData };
        } else {
          return { success: false, error: "Geçersiz email veya şifre!" };
        }
      }

      // Production'da demo login yok, backend hatası döndür
      return {
        success: false,
        error:
          error?.response?.data?.message ||
          "Giriş yapılamadı. Lütfen tekrar deneyin.",
      };
    }
  };

  const logout = async () => {
    try {
      // Backend'e logout isteği gönder
      await AuthService.logout();
    } catch (error) {
      console.error("Logout error:", error);
      // Hata olsa bile local logout yap
    } finally {
      // Tüm kullanıcı verilerini temizle
      clearUserData();
    }
  };

  const clearUserData = () => {
    // Auth verileri
    AuthService.removeToken();
    localStorage.removeItem("user");
    localStorage.removeItem("userId");
    localStorage.removeItem("refreshToken");

    // Permission verileri (Madde 5 düzeltmesi - rol bilgisi de temizleniyor)
    localStorage.removeItem("userPermissions");
    localStorage.removeItem("permissionsCacheTime");
    localStorage.removeItem("permissionsCacheRole");
    setPermissions([]);

    // Sepet verileri - localStorage ve sessionStorage
    localStorage.removeItem("guestCart");
    sessionStorage.removeItem("cart_guest_token");
    sessionStorage.removeItem("guest_session_id");
    // Session-bazlı tüm cart token'larını temizle
    Object.keys(localStorage).forEach((key) => {
      if (key.startsWith("cart_guest_token_")) {
        localStorage.removeItem(key);
      }
    });

    // Favori verileri
    localStorage.removeItem("guestFavorites");

    // Diğer kullanıcı spesifik veriler
    localStorage.removeItem("tempProductId");
    localStorage.removeItem("tempFavoriteProductId");

    setUser(null);

    // GÜVENLİK: Production'da debug log'ları kapalı
    if (process.env.NODE_ENV === "development") {
      console.log(
        "🧹 Kullanıcı verileri temizlendi (auth + sepet + favoriler)",
      );
    }
  };

  const register = async (
    email,
    password,
    firstName,
    lastName,
    phoneNumber,
    confirmPassword,
  ) => {
    try {
      // Backend API çağrısı
      const resp = await AuthService.register({
        email,
        password,
        confirmPassword: confirmPassword || password, // Backend için şifre onayı
        firstName,
        lastName,
        phoneNumber, // Telefon numarası eklendi
      });

      const data = resp && resp.data === undefined ? resp : resp.data;

      if (data && (data.Token || data.token)) {
        const userData = {
          email,
          firstName,
          lastName,
          name: `${firstName} ${lastName}`,
        };

        // Token ve kullanıcı bilgilerini kaydet
        AuthService.saveToken(data.Token || data.token);
        localStorage.setItem("user", JSON.stringify(userData));
        localStorage.setItem("userId", Date.now().toString());

        setUser(userData);

        return { success: true, user: userData };
      } else {
        return {
          success: false,
          error: (data && (data.Message || data.message)) || "Kayıt başarısız!",
        };
      }
    } catch (error) {
      console.error("Register error:", error);

      // Backend bağlantısı yoksa demo register'a geç (SADECE DEVELOPMENT)
      if (process.env.NODE_ENV === "development") {
        const existingUser = demoUsers.find((u) => u.email === email);

        if (existingUser) {
          return { success: false, error: "Bu email adresi zaten kullanımda!" };
        }

        // Şifre kontrolü
        if (!password || password.length < 6) {
          return { success: false, error: "Şifre en az 6 karakter olmalıdır!" };
        }

        // Yeni kullanıcı oluştur (demo için)
        const newUser = {
          id: Date.now(),
          email,
          firstName,
          lastName,
          name: `${firstName} ${lastName}`,
          password, // Demo için şifreyi de saklayalım
        };

        // Demo kullanıcılar listesine ekle
        const updatedDemoUsers = [...demoUsers, newUser];
        setDemoUsers(updatedDemoUsers);

        const token = "demo_token_" + Date.now();

        AuthService.saveToken(token);
        localStorage.setItem("user", JSON.stringify(newUser));
        localStorage.setItem("userId", newUser.id.toString());

        // Demo kullanıcıları localStorage'a kaydet
        localStorage.setItem("demoUsers", JSON.stringify(updatedDemoUsers));

        setUser(newUser);

        return { success: true, user: newUser };
      }

      // Production'da demo register yok, backend hatası döndür
      return {
        success: false,
        error:
          error?.response?.data?.message ||
          "Kayıt yapılamadı. Lütfen tekrar deneyin.",
      };
    }
  };

  // ======= SMS DOĞRULAMA İLE KAYIT =======

  /**
   * Telefon numarası ile kayıt başlatır.
   * SMS kodu gönderilir, verifyPhoneRegistration ile tamamlanır.
   */
  const registerWithPhone = async (
    email,
    password,
    firstName,
    lastName,
    phoneNumber,
  ) => {
    try {
      const result = await smsService.registerWithPhone({
        email,
        password,
        firstName,
        lastName,
        phoneNumber,
      });

      if (result.success) {
        return {
          success: true,
          message: result.message,
          userId: result.userId,
          phoneVerificationRequired: true,
        };
      } else {
        return {
          success: false,
          error: result.message || "Kayıt başarısız!",
        };
      }
    } catch (error) {
      console.error("RegisterWithPhone error:", error);
      return {
        success: false,
        error: "Kayıt sırasında bir hata oluştu.",
      };
    }
  };

  /**
   * Telefon doğrulama kodunu kontrol eder ve hesabı aktif eder.
   * Başarılı olursa JWT token döner.
   */
  const verifyPhoneRegistration = async (phoneNumber, code, email) => {
    try {
      const result = await smsService.verifyPhoneRegistration(
        phoneNumber,
        code,
        email,
      );

      if (result.success && result.token) {
        // Token ve kullanıcı bilgilerini kaydet
        AuthService.saveToken(result.token);

        // Kullanıcı bilgilerini decode et veya API'den al
        const userData = {
          email,
          phoneNumber,
        };

        localStorage.setItem("user", JSON.stringify(userData));
        setUser(userData);

        return {
          success: true,
          message: result.message,
          token: result.token,
        };
      } else {
        return {
          success: false,
          error: result.message || "Doğrulama başarısız!",
        };
      }
    } catch (error) {
      console.error("VerifyPhoneRegistration error:", error);
      return {
        success: false,
        error: "Doğrulama sırasında bir hata oluştu.",
      };
    }
  };

  // ======= TELEFON İLE ŞİFRE SIFIRLAMA =======

  /**
   * Telefon numarasına şifre sıfırlama kodu gönderir.
   */
  const forgotPasswordByPhone = async (phoneNumber) => {
    try {
      const result = await smsService.forgotPasswordByPhone(phoneNumber);
      return {
        success: result.success,
        message: result.message,
        error: result.message, // Hata durumu için de message'ı error olarak da döndür
        expiresInSeconds: result.expiresInSeconds || 180,
      };
    } catch (error) {
      console.error("ForgotPasswordByPhone error:", error);
      return {
        success: false,
        message: "İşlem sırasında bir hata oluştu.",
        error: "İşlem sırasında bir hata oluştu.",
      };
    }
  };

  /**
   * SMS kodu ile şifre sıfırlar.
   */
  const resetPasswordByPhone = async (
    phoneNumber,
    code,
    newPassword,
    confirmPassword,
  ) => {
    try {
      const result = await smsService.resetPasswordByPhone(
        phoneNumber,
        code,
        newPassword,
        confirmPassword,
      );
      return {
        success: result.success,
        message: result.message,
      };
    } catch (error) {
      console.error("ResetPasswordByPhone error:", error);
      return {
        success: false,
        error: "Şifre sıfırlama sırasında bir hata oluştu.",
      };
    }
  };

  const value = {
    // User state
    user,
    setUser,
    loading,
    isAuthenticated: !!user,

    // Auth functions
    login,
    loginWithSocial: async (provider, profile = {}) => {
      try {
        const resp = await AuthService.socialLogin({ provider, ...profile });
        const data = resp && resp.data === undefined ? resp : resp.data;
        if (data && (data.token || data.Token)) {
          const token = data.token || data.Token;
          const userData = data.user ||
            data.User || {
              id: data.id,
              email: data.email,
              name: data.name,
              firstName: data.firstName,
              lastName: data.lastName,
              role: data.role || "User",
            };
          AuthService.saveToken(token);
          localStorage.setItem("user", JSON.stringify(userData));
          if (userData?.id != null)
            localStorage.setItem("userId", String(userData.id));
          setUser(userData);

          // İzinleri yükle
          await loadUserPermissions(userData);

          return { success: true, user: userData };
        }
        return {
          success: false,
          error: data?.message || "Sosyal giriş başarısız",
        };
      } catch (e) {
        // Backend yoksa demo sosyal login (SADECE DEVELOPMENT)
        if (process.env.NODE_ENV === "development") {
          const fallbackUser = {
            id: Date.now(),
            email: profile.email || `${provider}_demo@local`,
            firstName: profile.firstName || provider,
            lastName: profile.lastName || "User",
            name: profile.name || `${provider} User`,
            role: "User",
          };
          const token = `${provider}_demo_token_${Date.now()}`;
          AuthService.saveToken(token);
          localStorage.setItem("user", JSON.stringify(fallbackUser));
          localStorage.setItem("userId", String(fallbackUser.id));
          setUser(fallbackUser);
          return { success: true, user: fallbackUser, demo: true };
        }

        // Production'da demo social login yok, backend hatası döndür
        return {
          success: false,
          error:
            e?.response?.data?.message ||
            "Sosyal giriş başarısız. Lütfen tekrar deneyin.",
        };
      }
    },
    logout,
    register,
    registerWithPhone,
    verifyPhoneRegistration,
    forgotPasswordByPhone,
    resetPasswordByPhone,

    // Permission state
    permissions,
    permissionsLoading,
    permissionsError,

    // Permission functions
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    canAccessAdminPanel,
    loadUserPermissions,
    refreshPermissions,
    clearPermissionsCache,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
