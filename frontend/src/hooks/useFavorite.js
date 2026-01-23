import { useFavorites } from "../contexts/FavoriteContext";

/**
 * useFavorite Hook - FavoriteContext'i sarmalayan hook
 * Geriye dönük uyumluluk için mevcut API'yi korur
 */
export const useFavorite = () => {
  const {
    favorites, // ProductListDto array (tam ürün objeleri)
    favoriteIds, // Sadece ID'ler (hızlı kontrol için)
    loading,
    toggleFavorite,
    removeFromFavorites,
    isFavorite,
    getFavoriteCount,
    loadFavorites,
  } = useFavorites();

  return {
    favorites, // Artık ProductListDto array
    favoriteIds, // Sadece ID'ler
    loading,
    isFavorite,
    toggleFavorite,
    removeFavorite: removeFromFavorites,
    loadFavorites,
    getFavoriteCount,
  };
};
