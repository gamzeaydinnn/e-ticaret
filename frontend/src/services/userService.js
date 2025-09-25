import api from "../api/client";

const base = "/api/Users";

// Kayıt (register)
export const register = (payload) =>
  api.post(base, payload).then((r) => r.data);

// Profil (me): Basit yaklaşım — localStorage.userId varsa backend'den çeker
export const me = async () => {
  const id = localStorage.getItem("userId");
  if (!id) return null;
  try {
    return await getById(id);
  } catch {
    return null;
  }
};

// Login: Backend'de endpoint olmadığı için placeholder; eklenince güncellenir
export const login = async (email, password) => {
  throw new Error(
    "Login endpoint backendde tanımlı değil. Eklenince güncellenecek."
  );
};

// CRUD yardımcıları
export const getAll = () => api.get(base).then((r) => r.data);
export const getById = (id) => api.get(`${base}/${id}`).then((r) => r.data);
export const update = (id, payload) =>
  api.put(`${base}/${id}`, payload).then((r) => r.data);
export const remove = (id) => api.delete(`${base}/${id}`).then((r) => r.data);
