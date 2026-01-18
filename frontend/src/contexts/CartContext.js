// src/contexts/CartContext.js
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

// Storage key - kullanıcı veya misafir
const getCartKey = (userId) => (userId ? `cart_user_${userId}` : "cart_guest");

export const CartProvider = ({ children }) => {
  const [cartItems, setCartItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const { user } = useAuth();

  // Kullanıcı değiştiğinde sepeti yükle
  useEffect(() => {
    loadCart();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.id]);

  // Sepeti yükle
  const loadCart = useCallback(() => {
    setLoading(true);
    try {
      const key = getCartKey(user?.id);
      const stored = localStorage.getItem(key);
      setCartItems(stored ? JSON.parse(stored) : []);
    } catch (error) {
      console.error("Sepet yüklenirken hata:", error);
      setCartItems([]);
    } finally {
      setLoading(false);
    }
  }, [user?.id]);

  // Sepeti kaydet
  const saveCart = useCallback(
    (items) => {
      const key = getCartKey(user?.id);
      localStorage.setItem(key, JSON.stringify(items));
      setCartItems(items);
      window.dispatchEvent(new Event("cart:updated"));
    },
    [user?.id],
  );

  // ============================================================================
  // Sepete ürün ekle - HEM MİSAFİR HEM KULLANICI İÇİN ÇALIŞIR
  // VARYANT DESTEĞİ: variantInfo parametresi ile varyant bilgisi alınır
  // variantInfo = { variantId, sku, variantTitle, price, stock }
  // ============================================================================
  const addToCart = useCallback(
    (product, quantity = 1, variantInfo = null) => {
      const productId = product.id || product.productId;

      // Varyantlı ürünlerde benzersiz key: productId + variantId
      // Böylece aynı ürünün farklı varyantları sepette ayrı satır olur
      const cartKey = variantInfo?.variantId
        ? `${productId}_${variantInfo.variantId}`
        : String(productId);

      const existingIndex = cartItems.findIndex((item) => {
        const itemKey = item.variantId
          ? `${item.productId || item.id}_${item.variantId}`
          : String(item.productId || item.id);
        return itemKey === cartKey;
      });

      // Varyant bilgisi varsa fiyatı varyanttan al
      const unitPrice =
        variantInfo?.price || product.specialPrice || product.price;

      let updatedCart;
      if (existingIndex >= 0) {
        // Mevcut öğeyi güncelle - miktarı artır
        updatedCart = cartItems.map((item, index) =>
          index === existingIndex
            ? { ...item, quantity: item.quantity + quantity }
            : item,
        );
      } else {
        // Yeni öğe ekle - varyant bilgisi dahil
        const newItem = {
          id: Date.now(),
          productId: productId,
          product: product,
          quantity: quantity,
          unitPrice: unitPrice,
          addedAt: new Date().toISOString(),
          // === VARYANT BİLGİLERİ ===
          variantId: variantInfo?.variantId || null,
          sku: variantInfo?.sku || null,
          variantTitle: variantInfo?.variantTitle || null,
          barcode: variantInfo?.barcode || null,
        };
        updatedCart = [...cartItems, newItem];
      }

      saveCart(updatedCart);

      // Backend sync (sadece giriş yapmış kullanıcılar için)
      if (user?.id) {
        CartService.addItem(productId, quantity, variantInfo?.variantId).catch(
          () => {},
        );
      }

      return { success: true };
    },
    [cartItems, user?.id, saveCart],
  );

  // ============================================================================
  // Sepetten ürün çıkar - VARYANT DESTEKLİ
  // productId ve opsiyonel variantId ile benzersiz item bulunur
  // ============================================================================
  const removeFromCart = useCallback(
    (productId, variantId = null) => {
      const updatedCart = cartItems.filter((item) => {
        // Varyantlı karşılaştırma
        if (variantId) {
          return !(
            (item.productId || item.id) === productId &&
            item.variantId === variantId
          );
        }
        // Varyantlı item'ı silmek için hem productId hem variantId eşleşmeli
        if (item.variantId) {
          return true; // Bu item varyantlı ama biz varyant belirtmedik, silme
        }
        return (item.productId || item.id) !== productId;
      });
      saveCart(updatedCart);

      if (user?.id) {
        const item = cartItems.find((i) => {
          if (variantId) {
            return (
              (i.productId || i.id) === productId && i.variantId === variantId
            );
          }
          return (i.productId || i.id) === productId && !i.variantId;
        });
        if (item?.id) {
          CartService.removeItem(item.id, productId).catch(() => {});
        }
      }

      return { success: true };
    },
    [cartItems, user?.id, saveCart],
  );

  // ============================================================================
  // Ürün miktarını güncelle - VARYANT DESTEKLİ
  // ============================================================================
  const updateQuantity = useCallback(
    (productId, quantity, variantId = null) => {
      if (quantity <= 0) return removeFromCart(productId, variantId);

      const updatedCart = cartItems.map((item) => {
        // Varyantlı karşılaştırma
        if (variantId) {
          if (
            (item.productId || item.id) === productId &&
            item.variantId === variantId
          ) {
            return { ...item, quantity };
          }
          return item;
        }
        // Varyantsız karşılaştırma
        if ((item.productId || item.id) === productId && !item.variantId) {
          return { ...item, quantity };
        }
        return item;
      });
      saveCart(updatedCart);

      if (user?.id) {
        const item = cartItems.find((i) => {
          if (variantId) {
            return (
              (i.productId || i.id) === productId && i.variantId === variantId
            );
          }
          return (i.productId || i.id) === productId && !i.variantId;
        });
        if (item?.id) {
          CartService.updateItem(item.id, productId, quantity).catch(() => {});
        }
      }

      return { success: true };
    },
    [cartItems, user?.id, saveCart, removeFromCart],
  );

  // Sepeti temizle
  const clearCart = useCallback(() => {
    const key = getCartKey(user?.id);
    localStorage.removeItem(key);
    setCartItems([]);
    window.dispatchEvent(new Event("cart:updated"));
  }, [user?.id]);

  // Sepet toplamı
  const getCartTotal = useCallback(() => {
    return cartItems.reduce((total, item) => {
      const price =
        item.unitPrice ||
        item.product?.specialPrice ||
        item.product?.price ||
        0;
      return total + price * item.quantity;
    }, 0);
  }, [cartItems]);

  // Sepet ürün sayısı
  const getCartCount = useCallback(() => {
    return cartItems.reduce((count, item) => count + item.quantity, 0);
  }, [cartItems]);

  // ============================================================================
  // Sepette ürün var mı kontrol - VARYANT DESTEKLİ
  // ============================================================================
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

  // ============================================================================
  // Sepetteki belirli bir ürünün/varyantın miktarını getir
  // ============================================================================
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

  const value = {
    cartItems,
    loading,
    addToCart,
    removeFromCart,
    updateQuantity,
    clearCart,
    getCartTotal,
    getCartCount,
    isInCart,
    getItemQuantity, // Yeni eklenen
    loadCart,
  };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
};
