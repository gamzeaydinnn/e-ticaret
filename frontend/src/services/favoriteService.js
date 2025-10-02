// src/services/favoriteService.js
import api from "./api";

export const FavoriteService = {
  getFavorites: () => api.get("/favorites"),
  toggleFavorite: (productId) => api.post(`/favorites/${productId}`),
  removeFavorite: (productId) => api.delete(`/favorites/${productId}`),
};
