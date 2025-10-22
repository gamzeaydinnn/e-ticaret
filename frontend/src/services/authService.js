// src/services/authService.js
import api from "./api";

export const AuthService = {
  login: (credentials) => api.post("/api/auth/login", credentials),
  register: (data) => api.post("/api/auth/register", data),
  logout: () => api.post("/api/auth/logout"),
  me: () => api.get("/api/auth/me"),
  refresh: (data) => api.post("/api/auth/refresh", data),

  // helper client-side
  saveToken: (token) => {
    localStorage.setItem("token", token);
    // Token'ı API header'ına da ekle
    api.defaults.headers.common["Authorization"] = `Bearer ${token}`;
  },

  removeToken: () => {
    localStorage.removeItem("token");
    // API header'ından da kaldır
    delete api.defaults.headers.common["Authorization"];
  },

  getToken: () => localStorage.getItem("token"),

  // Token'ı API'ye otomatik eklemek için
  setupTokenInterceptor: () => {
    const token = AuthService.getToken();
    if (token) {
      api.defaults.headers.common["Authorization"] = `Bearer ${token}`;
    }
  },
};
