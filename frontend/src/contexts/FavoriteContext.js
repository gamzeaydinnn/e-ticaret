// src/contexts/FavoriteContext.js
import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
} from "react";
import { FavoriteService } from "../services/favoriteService";
import { useAuth } from "./AuthContext";

const FavoriteContext = createContext();

export const useFavorites = () => {
  const context = useContext(FavoriteContext);
  if (!context) {
    throw new Error("useFavorites must be used within a FavoriteProvider");
  }
  return context;
};

// Storage key - kullanıcı veya misafir
const getFavoriteKey = (userId) =>
  userId ? `favorites_user_${userId}` : "favorites_guest";

export const FavoriteProvider = ({ children }) => {
  const [favorites, setFavorites] = useState([]);
  const [loading, setLoading] = useState(true);
  const { user } = useAuth();

  // Kullanıcı değiştiğinde favorileri yükle
  useEffect(() => {
    loadFavorites();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.id]);

  // Favorileri yükle
  const loadFavorites = useCallback(() => {
    setLoading(true);
    try {
      const key = getFavoriteKey(user?.id);
      const stored = localStorage.getItem(key);
      setFavorites(stored ? JSON.parse(stored) : []);
    } catch (error) {
      console.error("Favoriler yüklenirken hata:", error);
      setFavorites([]);
    } finally {
      setLoading(false);
    }
  }, [user?.id]);

  // Favorileri kaydet
  const saveFavorites = useCallback(
    (items) => {
      const key = getFavoriteKey(user?.id);
      localStorage.setItem(key, JSON.stringify(items));
      setFavorites(items);
      window.dispatchEvent(new Event("favorites:updated"));
    },
    [user?.id]
  );

  // Favoriye ekle/çıkar (toggle) - HEM MİSAFİR HEM KULLANICI İÇİN ÇALIŞIR
  const toggleFavorite = useCallback(
    (productId) => {
      const isFav = favorites.includes(productId);
      let updatedFavorites;

      if (isFav) {
        updatedFavorites = favorites.filter((id) => id !== productId);
      } else {
        updatedFavorites = [...favorites, productId];
      }

      saveFavorites(updatedFavorites);

      // Backend sync (sadece giriş yapmış kullanıcılar için)
      if (user?.id) {
        FavoriteService.toggleFavorite(productId).catch(() => {});
      }

      return { success: true, action: isFav ? "removed" : "added" };
    },
    [favorites, user?.id, saveFavorites]
  );

  // Favoriye ekle
  const addToFavorites = useCallback(
    (productId) => {
      if (favorites.includes(productId)) {
        return { success: true, message: "Ürün zaten favorilerde" };
      }

      const updatedFavorites = [...favorites, productId];
      saveFavorites(updatedFavorites);

      if (user?.id) {
        FavoriteService.toggleFavorite(productId).catch(() => {});
      }

      return { success: true };
    },
    [favorites, user?.id, saveFavorites]
  );

  // Favoriden çıkar
  const removeFromFavorites = useCallback(
    (productId) => {
      const updatedFavorites = favorites.filter((id) => id !== productId);
      saveFavorites(updatedFavorites);

      if (user?.id) {
        FavoriteService.removeFavorite(productId).catch(() => {});
      }

      return { success: true };
    },
    [favorites, user?.id, saveFavorites]
  );

  // Favorileri temizle
  const clearFavorites = useCallback(() => {
    const key = getFavoriteKey(user?.id);
    localStorage.removeItem(key);
    setFavorites([]);
    window.dispatchEvent(new Event("favorites:updated"));
  }, [user?.id]);

  // Favori mi kontrol et
  const isFavorite = useCallback(
    (productId) => {
      return favorites.includes(productId);
    },
    [favorites]
  );

  // Favori sayısı
  const getFavoriteCount = useCallback(() => {
    return favorites.length;
  }, [favorites]);

  const value = {
    favorites,
    loading,
    toggleFavorite,
    addToFavorites,
    removeFromFavorites,
    clearFavorites,
    isFavorite,
    getFavoriteCount,
    loadFavorites,
  };

  return (
    <FavoriteContext.Provider value={value}>
      {children}
    </FavoriteContext.Provider>
  );
};
