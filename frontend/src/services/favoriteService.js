// src/services/favoriteService.js
import api from "./api";
import {
  isBackendAvailable,
  isAuthEnabled,
  debugLog,
} from "../config/apiConfig";

// Giriş yapan kullanıcı kontrolü
const getAuthenticatedUser = () => {
  if (!isAuthEnabled()) {
    return null; // Auth sistemi aktif değil
  }

  // Auth aktifse localStorage'dan kullanıcı bilgisini al
  const authUser = localStorage.getItem("authUser");
  return authUser ? JSON.parse(authUser) : null;
};

export const FavoriteService = {
  getFavorites: async () => {
    const user = getAuthenticatedUser();

    debugLog("getFavorites çağrıldı", {
      backendAvailable: isBackendAvailable(),
      authEnabled: isAuthEnabled(),
      hasUser: !!user,
    });

    if (isBackendAvailable() && user) {
      // Backend API mevcut ve kullanıcı giriş yapmış
      try {
        debugLog("Backend'den favoriler çekiliyor", { userId: user.id });
        const result = await api.get(`/favorites?userId=${user.id}`);
        return result?.success ? result.data : [];
      } catch (error) {
        debugLog("Backend API hatası, localStorage fallback", error);
        return FavoriteService.getGuestFavorites();
      }
    } else {
      // Backend yok veya kullanıcı giriş yapmamış - localStorage kullan
      debugLog("localStorage'dan favoriler çekiliyor");
      return FavoriteService.getGuestFavorites();
    }
  },

  toggleFavorite: async (productId) => {
    const user = getAuthenticatedUser();

    if (isBackendAvailable() && user) {
      // Backend API mevcut ve kullanıcı giriş yapmış
      try {
        const result = await api.post(
          `/api/favorites/${productId}?userId=${user.id}`
        );
        return result;
      } catch (error) {
        console.warn("Backend API hatası, localStorage fallback:", error);
        // API hatası durumunda localStorage'a fallback
        const favorites = FavoriteService.getGuestFavorites();
        if (favorites.includes(productId)) {
          return {
            success: true,
            data: FavoriteService.removeFromGuestFavorites(productId),
            action: "removed",
          };
        } else {
          return {
            success: true,
            data: FavoriteService.addToGuestFavorites(productId),
            action: "added",
          };
        }
      }
    } else {
      // Backend yok veya kullanıcı giriş yapmamış - localStorage kullan
      const favorites = FavoriteService.getGuestFavorites();
      if (favorites.includes(productId)) {
        return {
          success: true,
          data: FavoriteService.removeFromGuestFavorites(productId),
          action: "removed",
        };
      } else {
        return {
          success: true,
          data: FavoriteService.addToGuestFavorites(productId),
          action: "added",
        };
      }
    }
  },

  removeFavorite: async (productId) => {
    const user = getAuthenticatedUser();

    if (isBackendAvailable() && user) {
      // Backend API mevcut ve kullanıcı giriş yapmış
      try {
        const result = await api.delete(
          `/api/favorites/${productId}?userId=${user.id}`
        );
        return result;
      } catch (error) {
        console.warn("Backend API hatası, localStorage fallback:", error);
        return {
          success: true,
          data: FavoriteService.removeFromGuestFavorites(productId),
        };
      }
    } else {
      // Backend yok veya kullanıcı giriş yapmamış - localStorage kullan
      return {
        success: true,
        data: FavoriteService.removeFromGuestFavorites(productId),
      };
    }
  },

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
