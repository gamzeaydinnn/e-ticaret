import axios from "axios";

// Production'da relative path kullan (nginx proxy), development'ta env var
const getBaseURL = () => {
  // Production'da boş baseURL = relative path = nginx proxy'ye git
  if (process.env.NODE_ENV === 'production') {
    return "";
  }
  return process.env.REACT_APP_API_URL || "http://localhost:5000";
};

const api = axios.create({
  baseURL: getBaseURL(),
  headers: { "Content-Type": "application/json" },
});

// Request interceptor: Token'ı header'a ekle
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("authToken");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

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
