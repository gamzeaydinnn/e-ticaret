/**
 * MobileBottomNav - Mobil Alt Navigasyon Bileşeni
 *
 * Mobil cihazlarda ve tabletlerde (1024px ve altı) ekranın altında sabit konumda görünen
 * navigasyon çubuğu. 5 ana sayfa için hızlı erişim sağlar.
 *
 * Özellikler:
 * - Anasayfa, Kategoriler, Sepetim, Kampanyalar, Hesabım navigasyonu
 * - Aktif sayfa vurgulaması
 * - Sepet badge ile ürün sayısı gösterimi
 * - Kategoriler açılır panel (API'den dinamik, alt kategori destekli)
 * - Touch-friendly buton boyutları (min 44x44px)
 * - CSS-only responsive görünürlük (performans için)
 *
 * Requirements: 1.1, 1.3, 1.4, 1.6, 1.8
 */

import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useCartCount } from "../hooks/useCartCount";
import { useFavorites } from "../contexts/FavoriteContext";
import categoryServiceReal, {
  normalizeCategorySlug,
} from "../services/categoryServiceReal";
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

  const lowerName = (name || "").toLowerCase();
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
    id: "favorites",
    label: "Favoriler",
    icon: "fa-heart",
    path: "/favorites",
    showBadge: true,
    badgeSource: "favorites",
    exactMatch: true,
  },
  {
    id: "account",
    label: "Hesabım",
    icon: "fa-user",
    path: "/account",
    matchPaths: ["/account", "/profile", "/addresses"],
    exactMatch: false,
  },
];

/**
 * Aktif route kontrolü
 */
const isActiveRoute = (currentPath, navItem) => {
  if (navItem.matchPaths?.length) {
    return navItem.matchPaths.some(
      (path) => currentPath === path || currentPath.startsWith(`${path}/`),
    );
  }
  if (navItem.exactMatch) {
    return currentPath === navItem.path;
  }
  return currentPath.startsWith(navItem.path);
};

const MobileBottomNav = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { count: cartCount } = useCartCount();
  const { favorites } = useFavorites();
  const favoriteCount = favorites?.length ?? 0;
  const [showCategories, setShowCategories] = useState(false);
  const [categories, setCategories] = useState([]);
  const [expandedCatId, setExpandedCatId] = useState(null);

  // Kategorileri API'den yükle (ana kategoriler + alt kategoriler)
  useEffect(() => {
    const loadCategories = async () => {
      try {
        const tree = await categoryServiceReal.getCategoryTree();
        const rootCats = Array.isArray(tree)
          ? tree.filter((cat) => cat.isActive !== false)
          : [];

        if (rootCats.length > 0) {
          setCategories(rootCats);
          return;
        }

        const cats = await categoryServiceReal.getActive();
        const rootsOnly = (cats || []).filter((cat) => !cat.parentId);
        setCategories(rootsOnly.length > 0 ? rootsOnly : cats || []);
      } catch (error) {
        console.error("Kategoriler yüklenemedi:", error);
        try {
          const cats = await categoryServiceReal.getActive();
          setCategories((cats || []).filter((cat) => !cat.parentId));
        } catch {
          setCategories([]);
        }
      }
    };
    loadCategories();
  }, []);

  /**
   * Navigasyon öğesine tıklama handler'ı
   */
  const handleNavClick = (item) => {
    if (item.isCategories) {
      setShowCategories((prev) => !prev);
      return;
    }
    setShowCategories(false);
    navigate(item.path);
  };

  /**
   * Kategori seçme handler'ı
   */
  const handleCategoryClick = (cat) => {
    const slug = normalizeCategorySlug(cat.slug || createSlug(cat.name));
    setShowCategories(false);
    setExpandedCatId(null);
    navigate(`/category/${slug}`);
  };

  /**
   * Alt kategori seçme handler'ı
   */
  const handleSubCategoryClick = (cat, sub) => {
    const slug = normalizeCategorySlug(cat.slug || createSlug(cat.name));
    const subSlug = normalizeCategorySlug(sub.slug || createSlug(sub.name));
    setShowCategories(false);
    setExpandedCatId(null);
    navigate(`/category/${slug}#${subSlug}`);
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
                categories.map((cat) => {
                  const subs = (
                    cat.subCategories ||
                    cat.children ||
                    cat.SubCategories ||
                    cat.Children ||
                    []
                  ).filter((sc) => (sc?.isActive ?? sc?.IsActive) !== false);
                  const hasSubs = subs.length > 0;
                  const isExpanded = expandedCatId === cat.id;

                  return (
                    <div key={cat.id}>
                      <div className="d-flex align-items-center">
                        <button
                          type="button"
                          className="category-panel-item flex-grow-1"
                          onClick={() => handleCategoryClick(cat)}
                        >
                          <i
                            className={`fas ${getCategoryIcon(cat.name)} me-3`}
                          ></i>
                          <span>{cat.name}</span>
                          {!hasSubs && (
                            <i className="fas fa-chevron-right ms-auto"></i>
                          )}
                        </button>
                        {hasSubs && (
                          <button
                            type="button"
                            onClick={(e) => {
                              e.stopPropagation();
                              setExpandedCatId((prev) =>
                                prev === cat.id ? null : cat.id,
                              );
                            }}
                            aria-label={`${cat.name} alt kategorileri`}
                            style={{
                              background: "none",
                              border: "none",
                              padding: "14px 18px",
                              color: isExpanded ? "#ff6b35" : "#64748b",
                              cursor: "pointer",
                            }}
                          >
                            <i
                              className={`fas fa-chevron-${
                                isExpanded ? "up" : "down"
                              }`}
                            ></i>
                          </button>
                        )}
                      </div>

                      {hasSubs && isExpanded && (
                        <div
                          style={{
                            background: "#fffaf5",
                            borderTop: "1px solid #fff0e5",
                            borderBottom: "1px solid #fff0e5",
                            padding: "6px 16px 8px 44px",
                          }}
                        >
                          {subs.map((sub) => (
                            <button
                              key={sub.id ?? sub.Id ?? sub.slug}
                              type="button"
                              onClick={() => handleSubCategoryClick(cat, sub)}
                              style={{
                                display: "flex",
                                alignItems: "center",
                                width: "100%",
                                padding: "9px 10px",
                                background: "none",
                                border: "none",
                                textAlign: "left",
                                fontSize: "0.88rem",
                                fontWeight: 500,
                                color: "#334155",
                                borderRadius: "8px",
                                cursor: "pointer",
                              }}
                            >
                              <i
                                className="fas fa-angle-right me-2"
                                style={{ color: "#ff6b35", fontSize: "0.75rem" }}
                              ></i>
                              <span>{sub.name || sub.Name}</span>
                            </button>
                          ))}
                        </div>
                      )}
                    </div>
                  );
                })
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

            const badgeCount =
              item.badgeSource === "favorites" ? favoriteCount : cartCount;

            return (
              <button
                key={item.id}
                type="button"
                className={`mobile-nav-item ${isActive ? "active" : ""}`}
                onClick={() => handleNavClick(item)}
                aria-label={item.label}
                aria-current={isActive ? "page" : undefined}
              >
                {/* İkon Container */}
                <div className="mobile-nav-icon">
                  <i className={`fas ${item.icon}`}></i>

                  {/* Badge */}
                  {item.showBadge && badgeCount > 0 && (
                    <span
                      className="mobile-nav-badge"
                      aria-label={`${badgeCount} ürün`}
                    >
                      {badgeCount > 99 ? "99+" : badgeCount}
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
