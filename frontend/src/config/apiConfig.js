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

// Default to .NET dev URLs from launchSettings.json
const envApiUrl =
  process.env.REACT_APP_API_URL ||
  process.env.REACT_APP_API_BASE_URL ||
  process.env.REACT_APP_API_BASE ||
  process.env.REACT_APP_BASE_URL;

const defaultDevUrl = (() => {
  // Prefer HTTPS dev port when available
  if (typeof window !== "undefined" && window.location && window.location.protocol === "https:") {
    return "https://localhost:7221";
  }
  // If running over http (CRA default), API still serves HTTPS by default
  return "https://localhost:7221";
})();

const baseUrl = (envApiUrl || defaultDevUrl).replace(/\/+$/, "");

const backendEnabled = parseBoolean(
  process.env.REACT_APP_BACKEND_ENABLED,
  Boolean(envApiUrl) || process.env.NODE_ENV !== "test"
);

export const API_CONFIG = {
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
