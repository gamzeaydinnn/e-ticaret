// src/config/apiConfig.js

const parseBoolean = (value, defaultValue) => {
  if (value === undefined || value === null || value === "") {
    return defaultValue;
  }

  if (typeof value === "boolean") {
    return value;
  }

  const normalized = value.toString().trim().toLowerCase();
  return ["1", "true", "yes", "y", "on"].includes(normalized);
};

const baseUrl = (process.env.REACT_APP_API_URL || "http://localhost:5000").replace(/\/+$/, "");

const backendEnabled = parseBoolean(
  process.env.REACT_APP_BACKEND_ENABLED,
  Boolean(process.env.REACT_APP_API_URL) || process.env.NODE_ENV !== "test"
);

export const API_CONFIG = {
<<<<<<< HEAD
  // Backend API durumu
  BACKEND_ENABLED: false, // Backend hazır olduğunda true yapın

  // API Base URL
  BASE_URL: process.env.REACT_APP_API_URL || "http://localhost:5153",

  // Auth durumu
  AUTH_ENABLED: false, // Auth sistemi hazır olduğunda true yapın

  // Mock data kullanımı
  USE_MOCK_DATA: true, // Backend hazır olduğunda false yapın

  // Debug modu
=======
  BASE_URL: baseUrl,
  BACKEND_ENABLED: backendEnabled,
  AUTH_ENABLED: parseBoolean(
    process.env.REACT_APP_AUTH_ENABLED,
    backendEnabled
  ),
  USE_MOCK_DATA: parseBoolean(
    process.env.REACT_APP_USE_MOCK_DATA,
    !backendEnabled && process.env.NODE_ENV !== "production"
  ),
>>>>>>> sare-branch
  DEBUG_MODE: process.env.NODE_ENV === "development",
};

export const getApiBaseUrl = () => API_CONFIG.BASE_URL;

export const isBackendAvailable = () => API_CONFIG.BACKEND_ENABLED;

export const isAuthEnabled = () => API_CONFIG.AUTH_ENABLED;

export const shouldUseMockData = () =>
  API_CONFIG.USE_MOCK_DATA && process.env.NODE_ENV !== "production";

export const debugLog = (message, data) => {
  if (API_CONFIG.DEBUG_MODE) {
    console.log(`[API Debug] ${message}`, data);
  }
};
