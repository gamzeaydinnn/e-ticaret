import axios from "axios";

// Base URL fallback: CRA veya Vite uyumlu
const baseURL = process.env.REACT_APP_API_BASE_URL || "http://localhost:5000/api";

const api = axios.create({
  baseURL,
  // FormData veya JSON fark etmez, axios otomatik ayarlar
});

// Request interceptor: token ekle
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("token");
    if (token) {
      config.headers = config.headers || {};
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor: response.data'yı döndür
api.interceptors.response.use(
  (response) => response.data,
  (error) => {
    const serverData = error?.response?.data;
    const message =
      typeof serverData === "string"
        ? serverData
        : serverData?.message || error.message || "Unknown API error";
    return Promise.reject(new Error(message));
  }
);

export default api;
