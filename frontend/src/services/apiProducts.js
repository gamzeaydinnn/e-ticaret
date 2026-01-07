// src/services/apiProducts.js
// SADECE ÜRÜNLER IÇIN Mock API - Production'da nginx proxy (/products)
// DİĞER API CALLS gerçek .NET API'ye gider (/api/...)
import axios from "axios";

const getBaseURL = () => {
  // Production'da nginx proxy üzerinden /products'a git
  if (process.env.NODE_ENV === 'production') {
    return "";  // baseURL boş = localhost:3005 yerine nginx proxy (/products)
  }
  // Development'ta direkt localhost:3005'e git
  return "http://localhost:3005";
};

const apiProducts = axios.create({
  baseURL: getBaseURL(),
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 10000,
});

// Request interceptor
apiProducts.interceptors.request.use(
  (config) => {
    console.log(
      `[Products Mock API] ${config.method?.toUpperCase()} ${config.url}`
    );
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
