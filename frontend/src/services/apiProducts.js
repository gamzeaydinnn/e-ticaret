// src/services/apiProducts.js
// JSON Server Client - Şimdilik SADECE ÜRÜNLER için (Mikro API gelene kadar)
import axios from "axios";

const apiProducts = axios.create({
  baseURL: "http://localhost:3005",
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 10000,
});

// Request interceptor
apiProducts.interceptors.request.use(
  (config) => {
    console.log(`[Products Mock API] ${config.method?.toUpperCase()} ${config.url}`);
    return config;
  },
  (error) => {
    console.error("[Products Mock API] Request error:", error);
    return Promise.reject(error);
  }
);

// Response interceptor
apiProducts.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    console.error(
      "[Products Mock API] Response error:",
      error.response?.status,
      error.message
    );
    return Promise.reject(error);
  }
);

export default apiProducts;
