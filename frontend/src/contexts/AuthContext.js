// src/contexts/AuthContext.js
import React, { createContext, useContext, useState, useEffect } from "react";
import { AuthService } from "../services/authService";

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
      { id: 1, email: "demo@example.com", password: "123456", firstName: "Demo", lastName: "User" },
      { id: 2, email: "test@example.com", password: "123456", firstName: "Test", lastName: "User" },
      { id: 3, email: "user@example.com", password: "123456", firstName: "Example", lastName: "User" },
    ];
  };

  const [demoUsers, setDemoUsers] = useState(getStoredDemoUsers());

  useEffect(() => {
    // Token interceptor'ını kur
    AuthService.setupTokenInterceptor();

    // Sayfa yüklendiğinde token kontrolü yap
    const token = AuthService.getToken();
    const userData = localStorage.getItem("user");

    if (token && userData) {
      try {
        setUser(JSON.parse(userData));
      } catch (error) {
        console.error("User data parsing error:", error);
        logout();
      }
    }

    setLoading(false);
  }, []);

  const login = async (email, password) => {
    try {
      const result = await AuthService.login({ email, password });
      // api.js returns response.data directly
      if (result?.Token) {
        // Save token and fetch user profile
        AuthService.saveToken(result.Token);
        try {
          const me = await AuthService.me();
          if (me) {
            localStorage.setItem("user", JSON.stringify(me));
            localStorage.setItem("userId", String(me.id || me.Id || ""));
            setUser(me);
          }
        } catch {}
        return { success: true, user: user || null };
      }
      return { success: false, error: result?.Message || "Giriş başarısız!" };
    } catch (error) {
      console.error("Login error:", error);

      // Backend bağlantısı yoksa demo login'e geç
      const demoUser = demoUsers.find(
        (u) => u.email === email && u.password === password
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

    // Sepet verileri
    localStorage.removeItem("guestCart");

    // Favori verileri
    localStorage.removeItem("guestFavorites");

    // Diğer kullanıcı spesifik veriler
    localStorage.removeItem("tempProductId");
    localStorage.removeItem("tempFavoriteProductId");

    setUser(null);
  };

  const register = async (email, password, firstName, lastName) => {
    try {
      const result = await AuthService.register({ email, password, firstName, lastName });
      // Expecting { RequireEmailConfirmation, Message }
      if (result?.RequireEmailConfirmation) {
        return { success: true, requiresEmailConfirmation: true, message: result.Message };
      }
      // Legacy path: if Token returned, treat as logged-in
      if (result?.Token) {
        AuthService.saveToken(result.Token);
        try {
          const me = await AuthService.me();
          if (me) {
            localStorage.setItem("user", JSON.stringify(me));
            localStorage.setItem("userId", String(me.id || me.Id || ""));
            setUser(me);
          }
        } catch {}
        return { success: true, user: user || null };
      }
      return { success: false, error: result?.Message || "Kayıt başarısız!" };
    } catch (error) {
      console.error("Register error:", error);

      // Backend bağlantısı yoksa demo register'a geç
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
        password // Demo için şifreyi de saklayalım
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
  };

  const value = {
    user,
    setUser,
    login,
    logout,
    register,
    loading,
    isAuthenticated: !!user,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
