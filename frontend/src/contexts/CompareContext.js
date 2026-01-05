// src/contexts/CompareContext.js
import { createContext, useContext, useState, useCallback } from "react";

const CompareContext = createContext();

export const useCompare = () => {
  const context = useContext(CompareContext);
  if (!context) {
    throw new Error("useCompare must be used within a CompareProvider");
  }
  return context;
};

const MAX_COMPARE_ITEMS = 4;

export const CompareProvider = ({ children }) => {
  const [compareItems, setCompareItems] = useState(() => {
    try {
      const stored = localStorage.getItem("compare_items");
      return stored ? JSON.parse(stored) : [];
    } catch {
      return [];
    }
  });

  // Karşılaştırmaya ürün ekle
  const addToCompare = useCallback((product) => {
    setCompareItems((prev) => {
      if (prev.length >= MAX_COMPARE_ITEMS) {
        return prev; // Maksimum limite ulaşıldı
      }
      if (prev.some((p) => p.id === product.id)) {
        return prev; // Zaten ekli
      }
      const updated = [...prev, product];
      localStorage.setItem("compare_items", JSON.stringify(updated));
      return updated;
    });
  }, []);

  // Karşılaştırmadan ürün çıkar
  const removeFromCompare = useCallback((productId) => {
    setCompareItems((prev) => {
      const updated = prev.filter((p) => p.id !== productId);
      localStorage.setItem("compare_items", JSON.stringify(updated));
      return updated;
    });
  }, []);

  // Karşılaştırmayı temizle
  const clearCompare = useCallback(() => {
    setCompareItems([]);
    localStorage.removeItem("compare_items");
  }, []);

  // Ürün karşılaştırmada mı?
  const isInCompare = useCallback(
    (productId) => {
      return compareItems.some((p) => p.id === productId);
    },
    [compareItems]
  );

  // Toggle karşılaştırma
  const toggleCompare = useCallback(
    (product) => {
      if (isInCompare(product.id)) {
        removeFromCompare(product.id);
        return { action: "removed" };
      } else {
        if (compareItems.length >= MAX_COMPARE_ITEMS) {
          return { action: "limit", message: `En fazla ${MAX_COMPARE_ITEMS} ürün karşılaştırabilirsiniz` };
        }
        addToCompare(product);
        return { action: "added" };
      }
    },
    [isInCompare, compareItems.length, addToCompare, removeFromCompare]
  );

  const value = {
    compareItems,
    addToCompare,
    removeFromCompare,
    clearCompare,
    isInCompare,
    toggleCompare,
    maxItems: MAX_COMPARE_ITEMS,
  };

  return <CompareContext.Provider value={value}>{children}</CompareContext.Provider>;
};
