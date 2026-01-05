import { useFavorites } from "../contexts/FavoriteContext";

/**
 * useFavorite Hook - FavoriteContext'i sarmalayan hook
 * Geriye dönük uyumluluk için mevcut API'yi korur
 */
export const useFavorite = () => {
  const {
    favorites,
    loading,
    toggleFavorite,
    removeFromFavorites,
    isFavorite,
    getFavoriteCount,
    loadFavorites,
  } = useFavorites();

  return {
    favorites,
    loading,
    isFavorite,
    toggleFavorite,
    removeFavorite: removeFromFavorites,
    loadFavorites,
    getFavoriteCount,
  };
};
