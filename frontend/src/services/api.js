/**
 * api.js - Axios HTTP Client
 *
 * baseURL: REACT_APP_API_URL environment variable'ından gelir
 * Docker'da nginx proxy ile /api → ecommerce-api:5000 yönlendirilir
 */
import axios from "axios";

const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL || "",
  headers: { "Content-Type": "application/json" },
});

// Request interceptor: Token varsa ekle
api.interceptors.request.use(
  (config) => {
    // Checkout endpoint'i Authorization header'ı gerektirmez (guest checkout)
    if (config.url?.includes("/orders/checkout") || config.url?.includes("/payments/posnet")) {
      delete config.headers.Authorization;
      return config;
    }

    const url = config.url || "";
    const isCourierRequest =
      url.includes("/api/courier") ||
      url.includes("/weight-adjustment") ||
      url.includes("/weight-payment");

    const courierToken =
      localStorage.getItem("courierToken") ||
      sessionStorage.getItem("courierToken");

    // Tüm olası token key'lerini kontrol et (uyumluluk için)
    const token =
      (isCourierRequest ? courierToken : null) ||
      localStorage.getItem("token") ||
      localStorage.getItem("authToken") ||
      localStorage.getItem("adminToken");

    // Token sadece geçerliyse ekle (boş string veya null değil)
    if (token && token.trim().length > 0) {
      config.headers.Authorization = `Bearer ${token}`;
      // Debug: Token gönderildiğini log'la
      if (process.env.NODE_ENV === "development") {
        console.log(`[API] 📤 ${config.method?.toUpperCase()} ${config.url}`, {
          hasToken: !!token,
          tokenPrefix: token.substring(0, 20) + "...",
        });
      }
    } else {
      // Debug: Token bulunamadı uyarısı
      if (process.env.NODE_ENV === "development") {
        console.warn(
          `[API] ⚠️  ${config.method?.toUpperCase()} ${
            config.url
          } - Token bulunamadı, Authorization header'ı eklenmedi!`,
        );
      }
      // Token yoksa Authorization header'ını sil (başka bir yerde set edilmiş olabilir)
      delete config.headers.Authorization;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// Response interceptor: Başarıda data unwrap, hatada normalize + token refresh
api.interceptors.response.use(
  (res) => res.data,
  async (error) => {
    const status = error?.response?.status ?? 0;
    const data = error?.response?.data;
    const url = error?.config?.url || "";
    const isCourierRequest =
      url.includes("/api/courier") ||
      url.includes("/weight-adjustment") ||
      url.includes("/weight-payment");

    const message =
      data?.message ||
      data?.error ||
      error?.message ||
      "Beklenmeyen bir hata oluştu.";

    // 401 Unauthorized - Token süresi dolmuş olabilir
    if (status === 401) {
      const originalConfig = error.config;
      const shouldRetry = !!originalConfig && !originalConfig._retry;
      if (shouldRetry) {
        originalConfig._retry = true;
      }

      if (isCourierRequest) {
        const courierRefreshToken =
          localStorage.getItem("courierRefreshToken") ||
          sessionStorage.getItem("courierRefreshToken");
        const courierToken =
          localStorage.getItem("courierToken") ||
          sessionStorage.getItem("courierToken");

        if (courierRefreshToken && shouldRetry) {
          try {
            const response = await axios.post(
              `${process.env.REACT_APP_API_URL || ""}/api/courier/auth/refresh`,
              { accessToken: courierToken, refreshToken: courierRefreshToken },
            );

            if (response.data?.accessToken || response.data?.AccessToken) {
              const newToken =
                response.data.accessToken || response.data.AccessToken;
              const newRefresh =
                response.data.refreshToken || response.data.RefreshToken;
              const storage =
                localStorage.getItem("courierToken") !== null
                  ? localStorage
                  : sessionStorage;

              storage.setItem("courierToken", newToken);
              if (newRefresh) {
                storage.setItem("courierRefreshToken", newRefresh);
              }

              originalConfig.headers.Authorization = `Bearer ${newToken}`;
              return api(originalConfig);
            }
          } catch (refreshError) {
            localStorage.removeItem("courierToken");
            localStorage.removeItem("courierRefreshToken");
            sessionStorage.removeItem("courierToken");
            sessionStorage.removeItem("courierRefreshToken");
            window.location.href = "/courier/login?session_expired=true";
          }
        } else {
          localStorage.removeItem("courierToken");
          localStorage.removeItem("courierRefreshToken");
          sessionStorage.removeItem("courierToken");
          sessionStorage.removeItem("courierRefreshToken");
        }
      } else {
        const refreshToken = localStorage.getItem("refreshToken");

        if (refreshToken && shouldRetry) {
          try {
            // Token refresh isteği yap
            const response = await axios.post(
              `${process.env.REACT_APP_API_URL || ""}/api/auth/refresh`,
              { refreshToken },
            );

            if (response.data?.token || response.data?.Token) {
              const newToken = response.data.token || response.data.Token;
              localStorage.setItem("token", newToken);
              localStorage.setItem("authToken", newToken);

              // Yeni token ile orijinal isteği tekrar dene
              originalConfig.headers.Authorization = `Bearer ${newToken}`;
              return api(originalConfig);
            }
          } catch (refreshError) {
            // Refresh başarısız - logout et
            localStorage.removeItem("token");
            localStorage.removeItem("authToken");
            localStorage.removeItem("adminToken");
            localStorage.removeItem("refreshToken");
            localStorage.removeItem("user");
            localStorage.removeItem("userId");
            // Sayfayı reload et - AuthContext logout yap
            window.location.href = "/login?session_expired=true";
          }
        } else if (!refreshToken) {
          // Refresh token yok - logout et
          localStorage.removeItem("token");
          localStorage.removeItem("authToken");
          localStorage.removeItem("adminToken");
          localStorage.removeItem("user");
          localStorage.removeItem("userId");
        }
      }
    }

    const normalizedError = new Error(message);
    normalizedError.status = status;
    normalizedError.raw = error;

    // Development'ta detaylı log
    if (process.env.NODE_ENV === "development") {
      if (status === 401) {
        console.error(`[API] 🔒 401 Unauthorized:`, {
          url: error.config?.url,
          method: error.config?.method,
          hasAuthHeader: !!error.config?.headers?.Authorization,
          message: message,
        });
      } else {
        console.error(`[API] ❌ ${status}:`, message);
      }
    }

    throw normalizedError;
  },
);

export default api;
