// src/services/apiBackend.js
// Gerçek Backend API Client - Posterler ve Kategoriler için
import axios from "axios";
import { API_CONFIG } from "../config/apiConfig";

const apiBackend = axios.create({
  baseURL: API_CONFIG.BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 15000,
});

// Request interceptor - Auth token ekle
apiBackend.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor - Hata yönetimi
apiBackend.interceptors.response.use(
  (response) => {
    return response.data;
  },
  (error) => {
    const status = error?.response?.status ?? 0;
    const data = error?.response?.data;

    const message =
      data?.message ||
      data?.error ||
      error?.message ||
      "Beklenmeyen bir hata oluştu";

    const normalizedError = new Error(message);
    normalizedError.status = status;
    normalizedError.raw = error;

    console.error("[Backend API Error]:", {
      status,
      message,
      url: error?.config?.url,
      method: error?.config?.method,
    });

    return Promise.reject(normalizedError);
  }
);

export default apiBackend;
