import axios from "axios";

const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL,
  headers: { "Content-Type": "application/json" },
});

api.interceptors.response.use(
  (res) => res.data,
  (err) => {
    console.error("API error:", err);
    throw err;
  }
);

export default api;
