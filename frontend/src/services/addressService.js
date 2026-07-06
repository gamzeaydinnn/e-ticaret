import api from "./api";

const BASE = "/api/address";

/** Backend entity Street alanını UI'daki address ile eşler */
const normalizeAddress = (item = {}) => ({
  id: item.id,
  title: item.title ?? "",
  fullName: item.fullName ?? "",
  phone: item.phone ?? "",
  city: item.city ?? "",
  district: item.district ?? "",
  address: item.street ?? item.address ?? "",
  postalCode: item.postalCode ?? "",
  isDefault: item.isDefault ?? false,
});

const toApiPayload = (form) => ({
  title: form.title,
  fullName: form.fullName,
  phone: form.phone,
  city: form.city,
  district: form.district,
  street: form.address ?? form.street ?? "",
  postalCode: form.postalCode || null,
  isDefault: form.isDefault ?? false,
});

/**
 * Kullanıcı adres API'si — backend AddressController (/api/address)
 */
export const addressService = {
  getAll: async () => {
    const response = await api.get(BASE);
    const items = Array.isArray(response?.data) ? response.data : [];
    return items.map(normalizeAddress);
  },

  create: async (form) => {
    const response = await api.post(BASE, toApiPayload(form));
    return normalizeAddress(response?.data ?? {});
  },

  update: async (id, form) => {
    const response = await api.put(`${BASE}/${id}`, toApiPayload(form));
    return normalizeAddress(response?.data ?? {});
  },

  remove: (id) => api.delete(`${BASE}/${id}`),
};

export default addressService;
