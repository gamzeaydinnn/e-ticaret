// src/services/favoriteService.js
import api from "./api";

export const FavoriteService = {
  getFavorites: () => api.get("/api/favorites").then((r) => r.data),
  toggleFavorite: (productId) =>
    api.post(`/api/favorites/${productId}`).then((r) => r.data),
  removeFavorite: (productId) =>
    api.delete(`/api/favorites/${productId}`).then((r) => r.data),

  // LocalStorage için guest favori yönetimi
  getGuestFavorites: () => {
    const favorites = localStorage.getItem("guestFavorites");
    return favorites ? JSON.parse(favorites) : [];
  },

  addToGuestFavorites: (productId) => {
    const favorites = FavoriteService.getGuestFavorites();
    if (!favorites.includes(productId)) {
      favorites.push(productId);
      localStorage.setItem("guestFavorites", JSON.stringify(favorites));
    }
    return favorites;
  },

  removeFromGuestFavorites: (productId) => {
    const favorites = FavoriteService.getGuestFavorites();
    const filteredFavorites = favorites.filter((id) => id !== productId);
    localStorage.setItem("guestFavorites", JSON.stringify(filteredFavorites));
    return filteredFavorites;
  },

  clearGuestFavorites: () => {
    localStorage.removeItem("guestFavorites");
  },

  getGuestFavoriteCount: () => {
    const favorites = FavoriteService.getGuestFavorites();
    return favorites.length;
  },
};
