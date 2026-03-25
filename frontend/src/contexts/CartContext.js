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
const WEIGHT_QTY_OVERRIDES_KEY = "cart_weight_qty_overrides_v1";

const buildWeightOverrideKey = (productId, variantId = null) =>
  `${productId}:${variantId ?? "base"}`;

const readWeightOverrides = () => {
  try {
    const raw = localStorage.getItem(WEIGHT_QTY_OVERRIDES_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
};

const writeWeightOverrides = (overrides) => {
  try {
    localStorage.setItem(WEIGHT_QTY_OVERRIDES_KEY, JSON.stringify(overrides));
  } catch {
    // Storage erişim hatası sepet akışını bloklamamalı.
  }
};

const setWeightOverride = (productId, variantId, quantity) => {
  if (
    !productId ||
    !Number.isFinite(Number(quantity)) ||
    Number(quantity) <= 0
  ) {
    return;
  }
  const overrides = readWeightOverrides();
  overrides[buildWeightOverrideKey(productId, variantId)] =
    Math.round(Number(quantity) * 100) / 100;
  writeWeightOverrides(overrides);
};

const removeWeightOverride = (productId, variantId = null) => {
  if (!productId) return;
  const overrides = readWeightOverrides();
  delete overrides[buildWeightOverrideKey(productId, variantId)];
  writeWeightOverrides(overrides);
};

const applyWeightOverrides = (items = []) => {
  const overrides = readWeightOverrides();
  return items.map((item) => {
    const key = buildWeightOverrideKey(
      item.productId || item.id,
      item.variantId,
    );
    const overrideQty = overrides[key];
    if (!Number.isFinite(Number(overrideQty))) {
      return item;
    }

    return {
      ...item,
      quantity: Number(overrideQty),
    };
  });
};

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

  const isWeightBasedProductLike = (product) => {
    if (!product) return false;
    return (
      product.isWeightBased === true ||
      product.soldByWeight === true ||
      product.weightUnit === "Kilogram" ||
      product.weightUnit === "Gram" ||
      product.weightUnit === 2 ||
      product.weightUnit === 1
    );
  };

  /**
   * Backend şu an quantity için integer beklediğinden,
   * kesirli miktarlarda (örn: 1.5) güvenli fallback üretir.
   */
  const getIntegerQuantityFallback = (quantity) => {
    const numeric = Number(quantity);
    if (!Number.isFinite(numeric) || numeric <= 0) return 1;
    return Math.max(1, Math.ceil(numeric));
  };

  const isFractionalQuantity = (quantity) => {
    const numeric = Number(quantity);
    if (!Number.isFinite(numeric)) return false;
    return Math.abs(numeric % 1) > 0;
  };

  const isBackendIntQuantityError = (err) => {
    const status =
      err?.status || err?.response?.status || err?.raw?.response?.status;
    const backendMessage =
      err?.response?.data?.message ||
      err?.raw?.response?.data?.message ||
      err?.raw?.response?.data?.errors ||
      err?.response?.data?.title ||
      err?.raw?.response?.data?.title ||
      err?.message ||
      "";
    const normalized = String(backendMessage).toLowerCase();

    if (status !== 400) return false;

    return (
      normalized.includes("int32") ||
      normalized.includes("could not be converted") ||
      normalized.includes("json value") ||
      normalized.includes("geçersiz")
    );
  };

  // ============================================================
  // SEPETİ YÜKLE - Backend'den
  // ============================================================
  const loadCart = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      let cartData;

      if (isAuthenticated && user?.id) {
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

      setCartItems(applyWeightOverrides(cartData.items));
      console.log("🛒 Sepet yüklendi:", cartData.items.length, "ürün");
    } catch (err) {
      console.error("❌ Sepet yüklenirken hata:", err);
      setError("Sepet yüklenemedi");
      setCartItems([]);
    } finally {
      setLoading(false);
    }
  }, [isAuthenticated, user?.id]);

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
        console.log("🔄 Login algılandı, misafir sepeti merge ediliyor...");
        try {
          const result = await CartService.mergeGuestCart();
          if (result.mergedCount > 0) {
            console.log(
              "✅ Merge başarılı:",
              result.mergedCount,
              "ürün eklendi",
            );
          }
        } catch (err) {
          console.error("❌ Merge hatası (sessizce devam):", err);
        }
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
      const productId = product.id || product.productId;
      const variantId = variantInfo?.variantId || null;
      const fallbackQuantity = getIntegerQuantityFallback(quantity);
      const isWeightBased = isWeightBasedProductLike(product);
      const hasFractionalQuantity = isFractionalQuantity(quantity);

      try {
        if (isAuthenticated && user?.id) {
          // Kayıtlı kullanıcı
          await CartService.addItem(productId, quantity, variantId);
        } else {
          // Misafir kullanıcı
          const result = await CartService.addToGuestCart(
            productId,
            quantity,
            variantId,
          );
          if (!result.success) {
            // Guest senaryosunda servis throw etmediği için fallback burada denenir.
            if (
              isFractionalQuantity(quantity) &&
              fallbackQuantity !== quantity
            ) {
              const retryResult = await CartService.addToGuestCart(
                productId,
                fallbackQuantity,
                variantId,
              );
              if (!retryResult.success) {
                return {
                  success: false,
                  error: retryResult.error || result.error,
                };
              }
              await loadCart();
              window.dispatchEvent(new Event("cart:updated"));
              if (hasFractionalQuantity) {
                setWeightOverride(productId, variantId, quantity);
              }
              return {
                success: true,
                warning: `Kesirli miktar backend uyumluluğu nedeniyle ${fallbackQuantity} olarak kaydedildi.`,
              };
            }

            return { success: false, error: result.error };
          }
        }

        // Sepeti yeniden yükle
        await loadCart();

        // Cart updated event - diğer componentler dinleyebilir
        window.dispatchEvent(new Event("cart:updated"));

        if (hasFractionalQuantity) {
          setWeightOverride(productId, variantId, quantity);
        } else {
          removeWeightOverride(productId, variantId);
        }

        return { success: true };
      } catch (err) {
        // Backend int quantity beklediğinde kesirli miktar için güvenli retry
        if (
          isFractionalQuantity(quantity) &&
          fallbackQuantity !== quantity &&
          isBackendIntQuantityError(err)
        ) {
          try {
            if (isAuthenticated && user?.id) {
              await CartService.addItem(productId, fallbackQuantity, variantId);
            } else {
              const retryResult = await CartService.addToGuestCart(
                productId,
                fallbackQuantity,
                variantId,
              );
              if (!retryResult.success) {
                return { success: false, error: retryResult.error };
              }
            }

            await loadCart();
            window.dispatchEvent(new Event("cart:updated"));
            if (hasFractionalQuantity) {
              setWeightOverride(productId, variantId, quantity);
            }

            return {
              success: true,
              warning: `Kesirli miktar backend uyumluluğu nedeniyle ${fallbackQuantity} olarak kaydedildi.`,
            };
          } catch (retryErr) {
            console.error("❌ Retry sepete ekleme hatası:", retryErr);
          }
        }

        console.error("❌ Sepete ekleme hatası:", err);
        const errorMsg = err?.response?.data?.message || "Sepete eklenemedi";
        return { success: false, error: errorMsg };
      }
    },
    [isAuthenticated, user?.id, loadCart],
  );

  // ============================================================
  // SEPETTEN ÜRÜN ÇIKAR - Varyant destekli
  // ============================================================
  const removeFromCart = useCallback(
    async (productId, variantId = null) => {
      try {
        if (isAuthenticated && user?.id) {
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
        removeWeightOverride(productId, variantId);

        return { success: true };
      } catch (err) {
        console.error("❌ Sepetten silme hatası:", err);
        return { success: false, error: err?.message };
      }
    },
    [isAuthenticated, user?.id, cartItems, loadCart],
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

      const fallbackQuantity = getIntegerQuantityFallback(quantity);
      const existingItem = cartItems.find(
        (i) =>
          (i.productId || i.id) === productId &&
          (variantId ? i.variantId === variantId : !i.variantId),
      );
      const isWeightBased =
        existingItem?.isWeightBased ||
        existingItem?.weightUnit === "Kilogram" ||
        existingItem?.weightUnit === "Gram" ||
        existingItem?.weightUnit === 2 ||
        existingItem?.weightUnit === 1;
      const hasFractionalQuantity = isFractionalQuantity(quantity);

      try {
        if (isAuthenticated && user?.id) {
          // Kayıtlı kullanıcı - cart item ID'sini bul
          const item = cartItems.find(
            (i) =>
              (i.productId || i.id) === productId &&
              (variantId ? i.variantId === variantId : !i.variantId),
          );
          if (item?.id) {
            await CartService.updateItem(item.id, productId, quantity);
          }
        } else {
          // Misafir kullanıcı
          await CartService.updateGuestCartItem(productId, quantity, variantId);
        }

        // Sepeti yeniden yükle
        await loadCart();
        window.dispatchEvent(new Event("cart:updated"));

        if (hasFractionalQuantity) {
          setWeightOverride(productId, variantId, quantity);
        } else {
          removeWeightOverride(productId, variantId);
        }

        return { success: true };
      } catch (err) {
        // Backend int quantity beklediğinde kesirli miktar için güvenli retry
        if (
          isFractionalQuantity(quantity) &&
          fallbackQuantity !== quantity &&
          isBackendIntQuantityError(err)
        ) {
          try {
            if (isAuthenticated && user?.id) {
              const item = cartItems.find(
                (i) =>
                  (i.productId || i.id) === productId &&
                  (variantId ? i.variantId === variantId : !i.variantId),
              );
              if (item?.id) {
                await CartService.updateItem(
                  item.id,
                  productId,
                  fallbackQuantity,
                );
              }
            } else {
              await CartService.updateGuestCartItem(
                productId,
                fallbackQuantity,
                variantId,
              );
            }

            await loadCart();
            window.dispatchEvent(new Event("cart:updated"));
            if (hasFractionalQuantity) {
              setWeightOverride(productId, variantId, quantity);
            } else {
              removeWeightOverride(productId, variantId);
            }

            return {
              success: true,
              warning: `Kesirli miktar backend uyumluluğu nedeniyle ${fallbackQuantity} olarak güncellendi.`,
            };
          } catch (retryErr) {
            console.error("❌ Retry miktar güncelleme hatası:", retryErr);
          }
        }

        console.error("❌ Miktar güncelleme hatası:", err);
        return { success: false, error: err?.message };
      }
    },
    [isAuthenticated, user?.id, cartItems, loadCart, removeFromCart],
  );

  // ============================================================
  // SEPETİ TEMİZLE
  // ============================================================
  const clearCart = useCallback(async () => {
    try {
      if (isAuthenticated && user?.id) {
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
  }, [isAuthenticated, user?.id, cartItems]);

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
  }, [isAuthenticated, user?.id, loadCart]);

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
      isWeightBased: item.isWeightBased === true,
      weightUnit: item.weightUnit || null,
    },
  };
}

export default CartContext;
