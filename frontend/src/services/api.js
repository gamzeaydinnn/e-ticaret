import axios from "axios";

const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL || "",
  headers: { "Content-Type": "application/json" },
});

// Global response interceptor: başarıda data'yı unwrap et, hatalarda tutarlı bir Error nesnesi fırlat
api.interceptors.response.use(
  (res) => res.data,
  (error) => {
    const status = error?.response?.status ?? 0;
    const data = error?.response?.data;

    const message =
      data?.message ||
      data?.error ||
      error?.message ||
      "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";

    const normalizedError = new Error(message);
    normalizedError.status = status;
    normalizedError.raw = error;

    console.error("API error:", {
      status,
      message,
      url: error?.config?.url,
      method: error?.config?.method,
    });

    throw normalizedError;
  }
);

export default api;
