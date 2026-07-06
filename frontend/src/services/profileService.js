import api from "./api";

const BASE = "/api/profile";

/**
 * Müşteri profil API'si — backend ProfileController (/api/profile)
 */
export const profileService = {
  getProfile: () => api.get(BASE),

  updateProfile: ({ firstName, lastName, phoneNumber }) =>
    api.put(BASE, { firstName, lastName, phoneNumber }),

  changePassword: ({ oldPassword, newPassword, confirmPassword }) =>
    api.post(`${BASE}/change-password`, {
      oldPassword,
      newPassword,
      confirmPassword,
    }),
};

export default profileService;
