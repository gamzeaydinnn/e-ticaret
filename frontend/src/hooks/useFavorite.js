import { useCallback, useEffect, useState } from "react";
import { FavoriteService } from "../services/favoriteService";

export const useFavorite = () => {
  const [favorites, setFavorites] = useState([]);
  const [loading, setLoading] = useState(false);

  // Favorileri yükle
  const loadFavorites = useCallback(async () => {
    setLoading(true);
    try {
      // FavoriteService otomatik olarak user durumunu ve backend availability'yi kontrol eder
      const favoritesData = await FavoriteService.getFavorites();

      // Gelen veriyi normalize et
      if (Array.isArray(favoritesData)) {
        if (favoritesData.length === 0) {
          setFavorites([]);
        } else if (
          typeof favoritesData[0] === "object" &&
          favoritesData[0].id
        ) {
          // Backend'den ürün objeleri geliyorsa ID'leri çıkar
          setFavorites(favoritesData.map((f) => f.id));
        } else {
          // localStorage'dan ID dizisi geliyorsa direkt kullan
          setFavorites(favoritesData);
        }
      } else {
        setFavorites([]);
      }
    } catch (error) {
      console.error("Favoriler yüklenirken hata:", error);
      // Kritik hata durumunda boş liste
      setFavorites([]);
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Favori durumunu kontrol et
  const isFavorite = useCallback(
    (productId) => {
      return favorites.includes(productId);
    },
    [favorites]
  );

  // Favori toggle işlemi
  const toggleFavorite = useCallback(
    async (productId) => {
      try {
        // FavoriteService otomatik olarak user durumunu ve backend availability'yi kontrol eder
        const result = await FavoriteService.toggleFavorite(productId);

        // İşlem başarılıysa favorileri yeniden yükle
        if (result.success) {
          await loadFavorites();
        }

        return result;
      } catch (error) {
        console.error("Favori toggle edilirken hata:", error);
        return { success: false, error: error.message };
      }
    },
    [loadFavorites]
  );

  // Favoriden kaldır
  const removeFavorite = useCallback(
    async (productId) => {
      try {
        // FavoriteService otomatik olarak user durumunu ve backend availability'yi kontrol eder
        const result = await FavoriteService.removeFavorite(productId);

        // İşlem başarılıysa favorileri yeniden yükle
        if (result.success) {
          await loadFavorites();
        }

        return result;
      } catch (error) {
        console.error("Favori silinirken hata:", error);
        return { success: false, error: error.message };
      }
    },
    [loadFavorites]
  );

  // Favori sayısını al
  const getFavoriteCount = useCallback(() => {
    return favorites.length;
  }, [favorites]);

  // Component mount olduğunda favorileri yükle
  useEffect(() => {
    loadFavorites();
  }, [loadFavorites]);

  return {
    favorites,
    loading,
    isFavorite,
    toggleFavorite,
    removeFavorite,
    loadFavorites,
    getFavoriteCount,
  };
};
