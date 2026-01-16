/**
 * MobileBottomNav - Mobil Alt Navigasyon Bileşeni
 *
 * Mobil cihazlarda (768px ve altı) ekranın altında sabit konumda görünen
 * navigasyon çubuğu. 5 ana sayfa için hızlı erişim sağlar.
 *
 * Özellikler:
 * - Anasayfa, Kategoriler, Sepetim, Kampanyalar, Hesabım navigasyonu
 * - Aktif sayfa vurgulaması
 * - Sepet badge ile ürün sayısı gösterimi
 * - Kategoriler açılır panel (API'den dinamik)
 * - Touch-friendly buton boyutları (min 44x44px)
 * - CSS-only responsive görünürlük (performans için)
 *
 * Requirements: 1.1, 1.3, 1.4, 1.6, 1.8
 */

import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useCartCount } from "../hooks/useCartCount";
import categoryServiceReal from "../services/categoryServiceReal";
import "../styles/mobileNav.css";

// Slug oluşturma fonksiyonu (App.js ile aynı)
const createSlug = (name) => {
  if (!name) return "";
  return name
    .toLowerCase()
    .replace(/ç/g, "c")
    .replace(/ğ/g, "g")
    .replace(/ı/g, "i")
    .replace(/ö/g, "o")
    .replace(/ş/g, "s")
    .replace(/ü/g, "u")
    .replace(/[^a-z0-9\s-]/g, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .trim();
};

// Kategori ikonları mapping
const getCategoryIcon = (name) => {
  const iconMap = {
    meyve: "fa-apple-alt",
    sebze: "fa-carrot",
    et: "fa-drumstick-bite",
    tavuk: "fa-drumstick-bite",
    balık: "fa-fish",
    süt: "fa-cheese",
    temel: "fa-bread-slice",
    içecek: "fa-glass-water",
    atıştırmalık: "fa-cookie-bite",
    temizlik: "fa-spray-can-sparkles",
  };

  const lowerName = name.toLowerCase();
  for (const [key, icon] of Object.entries(iconMap)) {
    if (lowerName.includes(key)) return icon;
  }
  return "fa-box";
};

// Navigasyon öğeleri tanımı
const NAV_ITEMS = [
  {
    id: "home",
    label: "Anasayfa",
    icon: "fa-home",
    path: "/",
    exactMatch: true,
  },
  {
    id: "categories",
    label: "Kategoriler",
    icon: "fa-th-large",
    path: "/category",
    exactMatch: false,
    isCategories: true,
  },
  {
    id: "cart",
    label: "Sepetim",
    icon: "fa-shopping-cart",
    path: "/cart",
    showBadge: true,
    exactMatch: true,
  },
  {
    id: "campaigns",
    label: "Kampanyalar",
    icon: "fa-tags",
    path: "/campaigns",
    exactMatch: false,
  },
  {
    id: "account",
    label: "Hesabım",
    icon: "fa-user",
    path: "/profile",
    exactMatch: false,
  },
];

/**
 * Aktif route kontrolü
 */
const isActiveRoute = (currentPath, navItem) => {
  if (navItem.exactMatch) {
    return currentPath === navItem.path;
  }
  return currentPath.startsWith(navItem.path);
};

const MobileBottomNav = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { count: cartCount } = useCartCount();
  const [showCategories, setShowCategories] = useState(false);
  const [categories, setCategories] = useState([]);

  // Kategorileri API'den yükle
  useEffect(() => {
    const loadCategories = async () => {
      try {
        const cats = await categoryServiceReal.getActive();
        setCategories(cats || []);
      } catch (error) {
        console.error("Kategoriler yüklenemedi:", error);
      }
    };
    loadCategories();
  }, []);

  /**
   * Navigasyon öğesine tıklama handler'ı
   */
  const handleNavClick = (item) => {
    if (item.isCategories) {
      setShowCategories(!showCategories);
    } else {
      setShowCategories(false);
      navigate(item.path);
    }
  };

  /**
   * Kategori seçme handler'ı
   */
  const handleCategoryClick = (cat) => {
    const slug = cat.slug || createSlug(cat.name);
    setShowCategories(false);
    navigate(`/category/${slug}`);
  };

  return (
    <>
      {/* Kategoriler Açılır Panel */}
      {showCategories && (
        <>
          <div
            className="category-overlay"
            onClick={() => setShowCategories(false)}
          />
          <div className="mobile-category-panel">
            <div className="category-panel-header">
              <h5 className="mb-0 fw-bold">Kategoriler</h5>
              <button
                className="btn-close"
                onClick={() => setShowCategories(false)}
                aria-label="Kapat"
              />
            </div>
            <div className="category-panel-body">
              {categories.length === 0 ? (
                <div className="text-center py-4 text-muted">
                  <i className="fas fa-spinner fa-spin me-2"></i>
                  Yükleniyor...
                </div>
              ) : (
                categories.map((cat) => (
                  <button
                    key={cat.id}
                    className="category-panel-item"
                    onClick={() => handleCategoryClick(cat)}
                  >
                    <i className={`fas ${getCategoryIcon(cat.name)} me-3`}></i>
                    <span>{cat.name}</span>
                    <i className="fas fa-chevron-right ms-auto"></i>
                  </button>
                ))
              )}
            </div>
          </div>
        </>
      )}

      <nav
        className="mobile-bottom-nav"
        role="navigation"
        aria-label="Mobil navigasyon"
      >
        <div className="mobile-nav-container">
          {NAV_ITEMS.map((item) => {
            const isActive = item.isCategories
              ? showCategories || isActiveRoute(location.pathname, item)
              : isActiveRoute(location.pathname, item);

            return (
              <button
                key={item.id}
                className={`mobile-nav-item ${isActive ? "active" : ""}`}
                onClick={() => handleNavClick(item)}
                aria-label={item.label}
                aria-current={isActive ? "page" : undefined}
              >
                {/* İkon Container */}
                <div className="mobile-nav-icon">
                  <i className={`fas ${item.icon}`}></i>

                  {/* Sepet Badge */}
                  {item.showBadge && cartCount > 0 && (
                    <span
                      className="mobile-nav-badge"
                      aria-label={`${cartCount} ürün`}
                    >
                      {cartCount > 99 ? "99+" : cartCount}
                    </span>
                  )}
                </div>

                {/* Label */}
                <span className="mobile-nav-label">{item.label}</span>
              </button>
            );
          })}
        </div>
      </nav>
    </>
  );
};

export default MobileBottomNav;
