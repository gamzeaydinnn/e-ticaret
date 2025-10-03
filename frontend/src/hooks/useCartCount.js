import { useState, useEffect } from "react";
import { CartService } from "../services/cartService";

export const useCartCount = () => {
  const [count, setCount] = useState(0);

  const updateCount = async () => {
    try {
      const userId = localStorage.getItem("userId");
      const items = await CartService.getCart(userId);
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
