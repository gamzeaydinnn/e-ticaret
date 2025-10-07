import { useState, useEffect } from "react";
import { CartService } from "../services/cartService";

export const useCartCount = () => {
  const [count, setCount] = useState(0);

  const updateCount = async () => {
    try {
      const userId = localStorage.getItem("userId");
      let items = [];

      if (userId) {
        // Giriş yapmış kullanıcı için backend'den sepeti getir
        try {
          items = await CartService.getCartItems();
        } catch (error) {
          // Backend bağlantısı yoksa localStorage kullan
          items = CartService.getGuestCart();
        }
      } else {
        // Misafir kullanıcı için localStorage'dan sepeti getir
        items = CartService.getGuestCart();
      }

      setCount(items.reduce((sum, item) => sum + (item.quantity || 1), 0));
    } catch {
      setCount(0);
    }
  };

  useEffect(() => {
    updateCount();
  }, []);

  return { count, refresh: updateCount };
};
