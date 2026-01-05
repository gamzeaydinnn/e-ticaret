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
    [user?.id]
  );

  // Sepete ürün ekle - HEM MİSAFİR HEM KULLANICI İÇİN ÇALIŞIR
  const addToCart = useCallback(
    (product, quantity = 1) => {
      const productId = product.id || product.productId;
      const existingIndex = cartItems.findIndex(
        (item) => (item.productId || item.id) === productId
      );

      let updatedCart;
      if (existingIndex >= 0) {
        updatedCart = cartItems.map((item, index) =>
          index === existingIndex
            ? { ...item, quantity: item.quantity + quantity }
            : item
        );
      } else {
        const newItem = {
          id: Date.now(),
          productId: productId,
          product: product,
          quantity: quantity,
          unitPrice: product.specialPrice || product.price,
          addedAt: new Date().toISOString(),
        };
        updatedCart = [...cartItems, newItem];
      }

      saveCart(updatedCart);

      // Backend sync (sadece giriş yapmış kullanıcılar için)
      if (user?.id) {
        CartService.addItem(productId, quantity).catch(() => {});
      }

      return { success: true };
    },
    [cartItems, user?.id, saveCart]
  );

  // Sepetten ürün çıkar
  const removeFromCart = useCallback(
    (productId) => {
      const updatedCart = cartItems.filter(
        (item) => (item.productId || item.id) !== productId
      );
      saveCart(updatedCart);

      if (user?.id) {
        const item = cartItems.find((i) => (i.productId || i.id) === productId);
        if (item?.id) {
          CartService.removeItem(item.id, productId).catch(() => {});
        }
      }

      return { success: true };
    },
    [cartItems, user?.id, saveCart]
  );

  // Ürün miktarını güncelle
  const updateQuantity = useCallback(
    (productId, quantity) => {
      if (quantity <= 0) return removeFromCart(productId);

      const updatedCart = cartItems.map((item) =>
        (item.productId || item.id) === productId ? { ...item, quantity } : item
      );
      saveCart(updatedCart);

      if (user?.id) {
        const item = cartItems.find((i) => (i.productId || i.id) === productId);
        if (item?.id) {
          CartService.updateItem(item.id, productId, quantity).catch(() => {});
        }
      }

      return { success: true };
    },
    [cartItems, user?.id, saveCart, removeFromCart]
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

  // Sepette ürün var mı
  const isInCart = useCallback(
    (productId) => {
      return cartItems.some(
        (item) => (item.productId || item.id) === productId
      );
    },
    [cartItems]
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
    loadCart,
  };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
};
