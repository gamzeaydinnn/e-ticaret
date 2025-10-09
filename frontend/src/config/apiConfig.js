// src/config/apiConfig.js

/**
 * API Configuration
 * Backend hazır olduğunda bu değerleri değiştirin
 */

export const API_CONFIG = {
  // Backend API durumu
  BACKEND_ENABLED: false, // Backend hazır olduğunda true yapın

  // API Base URL
  BASE_URL: process.env.REACT_APP_API_URL || "http://localhost:5000",

  // Auth durumu
  AUTH_ENABLED: false, // Auth sistemi hazır olduğunda true yapın

  // Mock data kullanımı
  USE_MOCK_DATA: true, // Backend hazır olduğunda false yapın

  // Debug modu
  DEBUG_MODE: process.env.NODE_ENV === "development",
};

/**
 * API durumunu kontrol et
 */
export const isBackendAvailable = () => {
  return API_CONFIG.BACKEND_ENABLED;
};

/**
 * Auth sisteminin aktif olup olmadığını kontrol et
 */
export const isAuthEnabled = () => {
  return API_CONFIG.AUTH_ENABLED;
};

/**
 * Mock data kullanılıp kullanılmayacağını kontrol et
 */
export const shouldUseMockData = () => {
  return API_CONFIG.USE_MOCK_DATA || !API_CONFIG.BACKEND_ENABLED;
};

/**
 * Debug log
 */
export const debugLog = (message, data) => {
  if (API_CONFIG.DEBUG_MODE) {
    console.log(`[API Debug] ${message}`, data);
  }
};
