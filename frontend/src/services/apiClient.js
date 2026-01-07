// src/services/apiClient.js
// JSON Server ile iletişim için merkezi axios client
import axios from "axios";

// JSON Server portu - package.json'daki mock-api scriptine bakın
const MOCK_API_BASE_URL = "http://localhost:3005";

const apiClient = axios.create({
  baseURL: MOCK_API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor - logging ve debugging için
apiClient.interceptors.request.use(
  (config) => {
    console.log(`[API] ${config.method?.toUpperCase()} ${config.url}`);
    return config;
  },
  (error) => {
    console.error("[API] Request error:", error);
    return Promise.reject(error);
  }
);

// Response interceptor - error handling
apiClient.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    console.error("[API] Response error:", error.response?.status, error.message);
    return Promise.reject(error);
  }
);

export default apiClient;
