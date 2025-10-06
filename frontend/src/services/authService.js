// src/services/authService.js
import api from "./api";

export const AuthService = {
  login: (credentials) => api.post("/auth/login", credentials),
  register: (data) => api.post("/auth/register", data),
  me: () => api.get("/auth/me"),
  refresh: (data) => api.post("/auth/refresh", data),
  // helper client-side
  saveToken: (token) => localStorage.setItem("token", token),
  removeToken: () => localStorage.removeItem("token"),
  getToken: () => localStorage.getItem("token"),
};
