import axios from "axios";
import {
  API_CONFIG,
  debugLog,
  isBackendAvailable,
  shouldUseMockData,
} from "../config/apiConfig";

const api = axios.create({
  baseURL: API_CONFIG.BASE_URL,
});

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

const buildErrorMessage = (error) => {
  const payload = error?.response?.data;

  if (payload) {
    if (typeof payload === "string") {
      return payload;
    }

    if (typeof payload.message === "string") {
      return payload.message;
    }

    if (Array.isArray(payload.errors)) {
      return payload.errors.join(", ");
    }
  }

  if (error?.message) {
    return error.message;
  }

  return "Sunucuya erişilemiyor. Lütfen tekrar deneyin.";
};

api.interceptors.response.use(
  (response) => response.data,
  (error) => {
    const message = buildErrorMessage(error);

    debugLog("API isteği başarısız", {
      url: error?.config?.url,
      status: error?.response?.status,
      message,
    });

    if (!isBackendAvailable() && shouldUseMockData()) {
      debugLog("Backend devre dışı. Mock veriye düşülüyor.");
    }

    return Promise.reject(new Error(message));
  }
);

export default api;
