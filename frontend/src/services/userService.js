import api from "./api";

const base = "/Users";

export const UserService = {
  register: (payload) => api.post(base, payload),
  login: (payload) => api.post("/Auth/login", payload), // login endpoint var
  me: async () => {
    const id = localStorage.getItem("userId");
    if (!id) return null;
    try {
      return await UserService.getById(id);
    } catch {
      return null;
    }
  },
  getAll: () => api.get(base),
  getById: (id) => api.get(`${base}/${id}`),
  update: (id, payload) => api.put(`${base}/${id}`, payload),
  remove: (id) => api.delete(`${base}/${id}`),
};
