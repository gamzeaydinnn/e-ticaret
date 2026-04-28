/**
 * Favori Context - Backend API Entegrasyonlu
 *
 * Tüm favori verileri BACKEND'de tutulur - localStorage KULLANILMAZ (sadece token için)
 *
 * Mimari:
 * - Misafir: X-Favorites-Token (UUID) ile backend'e istek atılır
 * - Kayıtlı: JWT token ile backend'e istek atılır
 * - Login sonrası: Misafir favoriler → Kullanıcı favorilerine merge edilir
 */
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

export const FavoriteProvider = ({ children }) => {
  // State
  const [favorites, setFavorites] = useState([]); // Favori ürün objeleri (ProductListDto)
  const [favoriteIds, setFavoriteIds] = useState([]); // Sadece ID'ler (hızlı kontrol için)
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Auth context
  const { user, isAuthenticated } = useAuth();

  // ============================================================
  // FAVORİLERİ YÜKLE - Backend'den
  // ============================================================
  const loadFavorites = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      let favoritesData;

      if (isAuthenticated) {
        // Kayıtlı kullanıcı - JWT ile favorileri al
        console.log("🔐 Kayıtlı kullanıcı favorileri yükleniyor...");
        favoritesData = await FavoriteService.getFavorites();
      } else {
        // Misafir kullanıcı - Token ile favorileri al
        console.log("👤 Misafir favorileri yükleniyor...");
        favoritesData = await FavoriteService.getGuestFavorites();
      }

      // Favorileri ve ID'leri ayarla
      setFavorites(favoritesData || []);
      const ids = (favoritesData || []).map((f) => f.id || f.productId);
      setFavoriteIds(ids);

      console.log("⭐ Favoriler yüklendi:", ids.length, "ürün");
    } catch (err) {
      console.error("❌ Favoriler yüklenirken hata:", err);
      setError("Favoriler yüklenemedi");
      setFavorites([]);
      setFavoriteIds([]);
    } finally {
      setLoading(false);
    }
  }, [isAuthenticated]);

  // Kullanıcı değiştiğinde favorileri yükle ve merge et
  const [prevUserId, setPrevUserId] = useState(null);

  useEffect(() => {
    const handleUserChange = async () => {
      const currentUserId = user?.id || null;

      // Kullanıcı login olduysa (misafir → kayıtlı)
      if (currentUserId && !prevUserId) {
        console.log("🔄 Login algılandı, misafir favoriler merge ediliyor...");
        try {
          const result = await FavoriteService.mergeGuestFavorites();
          if (result.mergedCount > 0) {
            console.log(
              "✅ Favori merge başarılı:",
              result.mergedCount,
              "ürün eklendi",
            );
          }
        } catch (err) {
          console.error("❌ Favori merge hatası (sessizce devam):", err);
        }
      }

      // Favorileri yükle
      await loadFavorites();
      setPrevUserId(currentUserId);
    };

    handleUserChange();
  }, [user?.id, prevUserId, loadFavorites]);

  // ============================================================
  // FAVORİYE EKLE/ÇIKAR (TOGGLE)
  // ============================================================
  const toggleFavorite = useCallback(
    async (productId) => {
      try {
        let result;

        if (isAuthenticated) {
          result = await FavoriteService.toggleFavorite(productId);
        } else {
          result = await FavoriteService.toggleGuestFavorite(productId);
        }

        if (result.success) {
          // Favorileri yeniden yükle
          await loadFavorites();

          // Event dispatch et
          window.dispatchEvent(new Event("favorites:updated"));

          return result;
        }

        return { success: false, error: result.error };
      } catch (err) {
        console.error("❌ Favori toggle hatası:", err);
        return { success: false, error: err?.message };
      }
    },
    [isAuthenticated, loadFavorites],
  );

  // ============================================================
  // FAVORİYE EKLE
  // ============================================================
  const addToFavorites = useCallback(
    async (productId) => {
      // Zaten favorideyse işlem yapma
      if (favoriteIds.includes(productId)) {
        return { success: true, message: "Ürün zaten favorilerde" };
      }

      return toggleFavorite(productId);
    },
    [favoriteIds, toggleFavorite],
  );

  // ============================================================
  // FAVORİDEN ÇIKAR
  // ============================================================
  const removeFromFavorites = useCallback(
    async (productId) => {
      try {
        let result;

        if (isAuthenticated) {
          result = await FavoriteService.removeFavorite(productId);
        } else {
          result = await FavoriteService.removeGuestFavorite(productId);
        }

        if (result.success) {
          // Favorileri yeniden yükle
          await loadFavorites();
          window.dispatchEvent(new Event("favorites:updated"));
        }

        return result;
      } catch (err) {
        console.error("❌ Favoriden çıkarma hatası:", err);
        return { success: false, error: err?.message };
      }
    },
    [isAuthenticated, loadFavorites],
  );

  // ============================================================
  // FAVORİLERİ TEMİZLE
  // ============================================================
  const clearFavorites = useCallback(async () => {
    try {
      // Her favoriyi tek tek sil
      for (const id of favoriteIds) {
        if (isAuthenticated) {
          await FavoriteService.removeFavorite(id);
        } else {
          await FavoriteService.removeGuestFavorite(id);
        }
      }

      setFavorites([]);
      setFavoriteIds([]);
      window.dispatchEvent(new Event("favorites:updated"));
    } catch (err) {
      console.error("❌ Favorileri temizleme hatası:", err);
    }
  }, [favoriteIds, isAuthenticated]);

  // ============================================================
  // FAVORİ Mİ KONTROL ET
  // ============================================================
  const isFavorite = useCallback(
    (productId) => {
      return favoriteIds.includes(productId);
    },
    [favoriteIds],
  );

  // ============================================================
  // FAVORİ SAYISI
  // ============================================================
  const getFavoriteCount = useCallback(() => {
    return favoriteIds.length;
  }, [favoriteIds]);

  // ============================================================
  // CONTEXT VALUE
  // ============================================================
  const value = {
    // State
    favorites,
    favoriteIds,
    loading,
    error,

    // Actions
    toggleFavorite,
    addToFavorites,
    removeFromFavorites,
    clearFavorites,
    loadFavorites,

    // Computed
    isFavorite,
    getFavoriteCount,
  };

  return (
    <FavoriteContext.Provider value={value}>
      {children}
    </FavoriteContext.Provider>
  );
};

export default FavoriteContext;
