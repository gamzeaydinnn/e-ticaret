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
    const token = localStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor: Başarıda data unwrap, hatada normalize
api.interceptors.response.use(
  (res) => res.data,
  (error) => {
    const status = error?.response?.status ?? 0;
    const data = error?.response?.data;

    const message =
      data?.message ||
      data?.error ||
      error?.message ||
      "Beklenmeyen bir hata oluştu.";

    const normalizedError = new Error(message);
    normalizedError.status = status;
    normalizedError.raw = error;

    // Sadece development'ta detaylı log (production'da temiz)
    if (process.env.NODE_ENV === "development") {
      console.error("[API] Hata:", status, message);
    }

    throw normalizedError;
  }
);

export default api;
