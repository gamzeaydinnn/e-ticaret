import { useCart } from "../contexts/CartContext";

/**
 * useCartCount Hook - CartContext'i sarmalayan hook
 * Geriye dönük uyumluluk için mevcut API'yi korur
 */
export const useCartCount = () => {
  const { getCartCount, loadCart, cartItems } = useCart();

  return {
    count: getCartCount(),
    lineCount: cartItems?.length ?? 0,
    refresh: loadCart,
  };
};
