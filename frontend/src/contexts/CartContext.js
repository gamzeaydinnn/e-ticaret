/**
 * Sepet Context - Backend API Entegrasyonlu
 *
 * Tüm sepet verileri BACKEND'de tutulur - localStorage KULLANILMAZ (sadece token için)
 *
 * Mimari:
 * - Misafir: CartToken (UUID) ile backend'e istek atılır
 * - Kayıtlı: JWT token ile backend'e istek atılır
 * - Login sonrası: Misafir sepet → Kullanıcı sepetine merge edilir
 */
import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
} from "react";
import { CartService } from "../services/cartService";
import { useAuth } from "./AuthContext";

const CartContext = createContext();

export const useCart = () => {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error("useCart must be used within a CartProvider");
  }
  return context;
};

export const CartProvider = ({ children }) => {
  // State
  const [cartItems, setCartItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Auth context - kullanıcı durumunu takip et
  const { user, isAuthenticated } = useAuth();

  // ============================================================
  // SEPETİ YÜKLE - Backend'den
  // ============================================================
  const loadCart = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      let cartData;

      if (isAuthenticated) {
        // Kayıtlı kullanıcı - JWT ile sepet al
        console.log("🔐 Kayıtlı kullanıcı sepeti yükleniyor...");
        const response = await CartService.getCartItems();
        // Backend CartItem[] döner, CartSummaryDto'ya dönüştür
        cartData = {
          items: Array.isArray(response) ? response.map(mapBackendItem) : [],
          total: 0,
        };
        cartData.total = cartData.items.reduce(
          (sum, item) => sum + (item.unitPrice || 0) * (item.quantity || 0),
          0,
        );
      } else {
        // Misafir kullanıcı - CartToken ile sepet al
        console.log("👤 Misafir sepeti yükleniyor...");
        cartData = await CartService.getGuestCart();
        // Backend CartSummaryDto döner
        cartData = {
          items: Array.isArray(cartData?.items)
            ? cartData.items.map(mapBackendItem)
            : [],
          total: cartData?.total || 0,
        };
      }

      setCartItems(cartData.items);
      console.log("🛒 Sepet yüklendi:", cartData.items.length, "ürün");
      return cartData.items;
    } catch (err) {
      console.error("❌ Sepet yüklenirken hata:", err);
      setError("Sepet yüklenemedi");
      setCartItems([]);
      return [];
    } finally {
      setLoading(false);
    }
  }, [isAuthenticated]);

  // Kullanıcı değiştiğinde sepeti yükle
  // Login sonrası misafir sepetini merge et
  // ÖNEMLI: Farklı kullanıcıya geçişte sepeti sıfırla
  const [prevUserId, setPrevUserId] = useState(null);

  useEffect(() => {
    const handleUserChange = async () => {
      const currentUserId = user?.id || null;

      // Kullanıcı değiştiyse (farklı hesaba geçiş veya logout)
      if (prevUserId !== null && currentUserId !== prevUserId) {
        console.log("🔄 Kullanıcı değişti:", prevUserId, "→", currentUserId);
        // Önceki sepeti temizle (UI'da)
        setCartItems([]);
      }

      // Kullanıcı login olduysa (misafir → kayıtlı)
      if (currentUserId && !prevUserId) {
        console.log(
          "🔄 Login algılandı, misafir sepet token'ı temizleniyor (merge kapalı)",
        );
        CartService.clearGuestToken();
      }

      // Sepeti yükle (kullanıcıya özel)
      await loadCart();
      setPrevUserId(currentUserId);
    };

    handleUserChange();
  }, [user?.id, prevUserId, loadCart]);

  // ============================================================
  // SEPETE ÜRÜN EKLE - Varyant destekli
  // ============================================================
  const addToCart = useCallback(
    async (product, quantity = 1, variantInfo = null) => {
      // NEDEN: product.id=0 falsy — || operatörü 0'ı undefined'a düşürürdü.
      // Mikro-only ürünler (id=0) backend'de kayıtlı değil — ensure-local ile anında ekle.
      let productId =
        product.id > 0
          ? product.id
          : product.productId > 0
            ? product.productId
            : null;

      // id=0 ama SKU var: backend'e ensure-local gönder, gerçek ID al
      if (!productId && product.sku) {
        try {
          // api imi dinamik import ile alınabilir, ama biz services/api'yi kullanıyoruz
          // CartService'in api objesini kullanmak için dolaylı import yapmak yerine
          // fetch kullan (circular dep'ten kaçın)
          const base = process.env.REACT_APP_API_URL || "";
          const response = await fetch(
            `${base}/api/products/ensure-local/${encodeURIComponent(product.sku)}`,
            {
              method: "POST",
              credentials: "include",
              headers: { "Content-Type": "application/json" },
            },
          );
          if (response.ok) {
            const data = await response.json();
            productId = data?.id || null;
            // Product objesini de güncelle (aynı oturum içinde)
            if (productId) product = { ...product, id: productId };
          }
        } catch (e) {
          console.warn("[CartContext] ensure-local hatası:", e);
        }
      }

      if (!productId) {
        console.warn(
          "[CartContext] Geçersiz productId — sepete eklenemiyor:",
          product.name || product.sku,
        );
        return { success: false, error: "Bu ürün şu an sepete eklenemiyor." };
      }
      const variantId = variantInfo?.variantId || null;
      try {
        if (isAuthenticated) {
          await CartService.addItem(productId, quantity, variantId);
        } else {
          const result = await CartService.addToGuestCart(
            productId,
            quantity,
            variantId,
          );
          if (!result.success) {
            return { success: false, error: result.error };
          }
        }

        await loadCart();
        window.dispatchEvent(new Event("cart:updated"));

        return { success: true };
      } catch (err) {
        console.error("❌ Sepete ekleme hatası:", err);
        const rawMsg = err?.response?.data?.message || "Sepete eklenemedi";
        // NEDEN: Backend "Yetersiz stok. Maksimum 0 adet." gibi teknik mesaj döner —
        // kullanıcıya anlamlı, net bir stok mesajı gösteriyoruz.
        const errorMsg = /stok|yetersiz/i.test(rawMsg)
          ? "Bu ürün şu an stokta bulunmamaktadır."
          : rawMsg;
        return { success: false, error: errorMsg };
      }
    },
    [isAuthenticated, loadCart],
  );

  // ============================================================
  // SEPETTEN ÜRÜN ÇIKAR - Varyant destekli
  // ============================================================
  const removeFromCart = useCallback(
    async (productId, variantId = null) => {
      try {
        if (isAuthenticated) {
          // Kayıtlı kullanıcı - cart item ID'sini bul
          const item = cartItems.find(
            (i) =>
              (i.productId || i.id) === productId &&
              (variantId ? i.variantId === variantId : !i.variantId),
          );
          if (item?.id) {
            await CartService.removeItem(item.id);
          }
        } else {
          // Misafir kullanıcı
          await CartService.removeFromGuestCart(productId, variantId);
        }

        // Sepeti yeniden yükle
        await loadCart();
        window.dispatchEvent(new Event("cart:updated"));

        return { success: true };
      } catch (err) {
        console.error("❌ Sepetten silme hatası:", err);
        return { success: false, error: err?.message };
      }
    },
    [isAuthenticated, cartItems, loadCart],
  );

  // ============================================================
  // ÜRÜN MİKTARINI GÜNCELLE
  // ============================================================
  const updateQuantity = useCallback(
    async (productId, quantity, variantId = null) => {
      // Miktar 0 veya altı = sil
      if (quantity <= 0) {
        return removeFromCart(productId, variantId);
      }

      const matchesCartItem = (item) => {
        if ((item.productId || item.id) !== productId) {
          return false;
        }

        if (variantId) {
          return item.variantId === variantId;
        }

        return !item.variantId;
      };

      const previousCartItems = cartItems;

      const applyOptimisticQuantity = (nextQuantity) => {
        // NEDEN: Kullanıcı ilk tıklamada yeni miktarı görmeli; backend dönüşünü
        // beklerken eski değer kalırsa buton akışı ölü hissediliyor.
        setCartItems((prevItems) =>
          prevItems.map((item) =>
            matchesCartItem(item)
              ? {
                  ...item,
                  quantity: nextQuantity,
                }
              : item,
          ),
        );
      };

      try {
        applyOptimisticQuantity(quantity);

        if (isAuthenticated) {
          const item = cartItems.find(matchesCartItem);
          if (item?.id) {
            await CartService.updateItem(
              item.id,
              productId,
              quantity,
              variantId,
            );
          }
        } else {
          // Misafir kullanıcı
          await CartService.updateGuestCartItem(productId, quantity, variantId);
        }

        await loadCart();
        window.dispatchEvent(new Event("cart:updated"));

        return { success: true };
      } catch (err) {
        setCartItems(previousCartItems);
        console.error("❌ Miktar güncelleme hatası:", err);
        return { success: false, error: err?.message };
      }
    },
    [isAuthenticated, cartItems, loadCart, removeFromCart],
  );

  // ============================================================
  // SEPETİ TEMİZLE
  // ============================================================
  const clearCart = useCallback(async () => {
    try {
      if (isAuthenticated) {
        // Kayıtlı kullanıcı - tüm öğeleri sil
        for (const item of cartItems) {
          if (item.id) {
            await CartService.removeItem(item.id);
          }
        }
      } else {
        // Misafir kullanıcı
        await CartService.clearGuestCart();
      }

      setCartItems([]);
      window.dispatchEvent(new Event("cart:updated"));
    } catch (err) {
      console.error("❌ Sepet temizleme hatası:", err);
    }
  }, [isAuthenticated, cartItems]);

  // ============================================================
  // SEPET TOPLAMI
  // ============================================================
  const getCartTotal = useCallback(() => {
    return cartItems.reduce((total, item) => {
      const price =
        item.unitPrice ||
        item.product?.specialPrice ||
        item.product?.price ||
        0;
      return total + price * (item.quantity || 0);
    }, 0);
  }, [cartItems]);

  // ============================================================
  // SEPET ÜRÜN SAYISI
  // ============================================================
  const getCartCount = useCallback(() => {
    return cartItems.reduce((count, item) => count + (item.quantity || 0), 0);
  }, [cartItems]);

  // ============================================================
  // SEPETTE ÜRÜN VAR MI? - Varyant destekli
  // ============================================================
  const isInCart = useCallback(
    (productId, variantId = null) => {
      return cartItems.some((item) => {
        if (variantId) {
          return (
            (item.productId || item.id) === productId &&
            item.variantId === variantId
          );
        }
        return (item.productId || item.id) === productId;
      });
    },
    [cartItems],
  );

  // ============================================================
  // SEPETTEKİ ÜRÜN MİKTARINI GETİR
  // ============================================================
  const getItemQuantity = useCallback(
    (productId, variantId = null) => {
      const item = cartItems.find((i) => {
        if (variantId) {
          return (
            (i.productId || i.id) === productId && i.variantId === variantId
          );
        }
        return (i.productId || i.id) === productId && !i.variantId;
      });
      return item?.quantity || 0;
    },
    [cartItems],
  );

  // ============================================================
  // MERGE CART (Login sonrası çağrılır)
  // ============================================================
  const mergeGuestCart = useCallback(async () => {
    if (!isAuthenticated || !user?.id) {
      return { mergedCount: 0 };
    }

    try {
      const result = await CartService.mergeGuestCart();
      if (result.mergedCount > 0) {
        // Merge başarılı - sepeti yeniden yükle
        await loadCart();
        console.log("✅ Misafir sepet aktarıldı:", result.mergedCount, "ürün");
      }
      return result;
    } catch (err) {
      console.error("❌ Sepet merge hatası:", err);
      return { mergedCount: 0 };
    }
  }, [isAuthenticated, loadCart, user?.id]);

  // ============================================================
  // CONTEXT VALUE
  // ============================================================
  const value = {
    // State
    cartItems,
    loading,
    error,

    // Actions
    addToCart,
    removeFromCart,
    updateQuantity,
    clearCart,
    loadCart,
    mergeGuestCart,

    // Computed
    getCartTotal,
    getCartCount,
    isInCart,
    getItemQuantity,
  };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
};

// ============================================================
// HELPER: Backend item'ı frontend formatına dönüştür
// ============================================================
function mapBackendItem(item) {
  const product = item.product || {};
  const variantId = item.productVariantId || item.variantId || null;
  const hasWeightUnit =
    product.weightUnit === "Kilogram" ||
    product.weightUnit === "Gram" ||
    product.weightUnit === 2 ||
    product.weightUnit === 1;

  return {
    id: item.id,
    productId: item.productId,
    variantId,
    quantity: Number(item.quantity) || 0,
    isActive: item.isActive ?? item.product?.isActive ?? true,
    unitPrice:
      item.unitPrice || item.product?.specialPrice || item.product?.price || 0,
    // Kg bazlı ürünler için UI'nin tek kaynaktan karar verebilmesi için normalize edilir.
    isWeightBased:
      item.isWeightBased === true ||
      product.isWeightBased === true ||
      product.soldByWeight === true ||
      hasWeightUnit,
    weightUnit: item.weightUnit || product.weightUnit || null,
    // Ürün bilgileri (backend'den gelirse)
    productName: item.productName || item.product?.name,
    productImage:
      item.productImageUrl || item.productImage || item.product?.imageUrl,
    variantTitle: item.variantTitle,
    sku: item.sku || item.variantSku,
    // Backward compat
    product: item.product || {
      id: item.productId,
      name: item.productName,
      imageUrl: item.productImageUrl || item.productImage,
      price: item.unitPrice,
      specialPrice: item.unitPrice,
      isActive: item.isActive ?? true,
      isWeightBased: item.isWeightBased === true,
      weightUnit: item.weightUnit || null,
    },
  };
}

export default CartContext;
