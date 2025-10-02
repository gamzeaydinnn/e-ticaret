import api from "./api";

const base = "/api/Users";

export const UserService = {
  register: (payload) => api.post(base, payload).then(r => r.data),
  login: (payload) => api.post("/api/Auth/login", payload).then(r => r.data), // login endpoint var
  me: async () => {
    const id = localStorage.getItem("userId");
    if (!id) return null;
    try {
      return await UserService.getById(id);
    } catch {
      return null;
    }
  },
  getAll: () => api.get(base).then(r => r.data),
  getById: (id) => api.get(`${base}/${id}`).then(r => r.data),
  update: (id, payload) => api.put(`${base}/${id}`, payload).then(r => r.data),
  remove: (id) => api.delete(`${base}/${id}`).then(r => r.data),
};
