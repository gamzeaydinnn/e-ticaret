/**
 * Home.js - Ana Sayfa Bileşeni
 *
 * Bu bileşen, e-ticaret sitesinin ana sayfasını render eder.
 *
 * Bölümler:
 * - Hero Slider (banner'lar)
 * - Promosyon Kartları
 * - Kategori Grid
 * - Öne Çıkan Ürünler
 *
 * @author Senior Developer
 * @version 2.0.0
 */

import { useEffect, useState, useCallback } from "react";
import { ProductService } from "../services/productService";
import bannerService from "../services/bannerService";
import categoryServiceReal from "../services/categoryServiceReal";
import { Helmet } from "react-helmet-async";
import ProductCard from "./components/ProductCard";
import CategoryTile from "./components/CategoryTile";
import HeroSlider from "../components/HeroSlider";
import PromoCards from "../components/PromoCards";

// ============================================
// ANA BİLEŞEN
// ============================================

export default function Home() {
  // ============================================
  // STATE YÖNETİMİ
  // ============================================

  // Kategori state'leri
  const [categories, setCategories] = useState([]);
  const [categoriesLoading, setCategoriesLoading] = useState(true);

  // Ürün state'leri
  const [featured, setFeatured] = useState([]);
  const [productLoading, setProductLoading] = useState(true);
  const [productError, setProductError] = useState(null);

  // Banner state'leri
  const [sliderBanners, setSliderBanners] = useState([]);
  const [promoBanners, setPromoBanners] = useState([]);
  const [bannersLoading, setBannersLoading] = useState(true);

  // Favori state'i
  const [favorites, setFavorites] = useState(() => {
    // localStorage'dan favori ürünleri yükle
    try {
      const stored = localStorage.getItem("favorites");
      return stored ? JSON.parse(stored) : [];
    } catch {
      return [];
    }
  });

  // ============================================
  // VERİ YÜKLEME FONKSİYONU
  // ============================================

  const loadData = useCallback(async () => {
    // Kategorileri yükle
    setCategoriesLoading(true);
    try {
      const cats = await categoryServiceReal.getActive();
      console.log("[Home] Categories from API:", cats?.length || 0);
      setCategories(cats || []);
    } catch (err) {
      console.error("[Home] Categories error:", err.message);
      setCategories([]);
    } finally {
      setCategoriesLoading(false);
    }

    // Ürünleri yükle
    setProductLoading(true);
    try {
      const items = await ProductService.list();
      setFeatured(items || []);
      setProductError(null);
    } catch (err) {
      console.error("[Home] Products error:", err.message);
      setProductError(err?.message || "Ürünler yüklenemedi");
      setFeatured([]);
    } finally {
      setProductLoading(false);
    }

    // Banner'ları yükle (paralel olarak)
    setBannersLoading(true);
    try {
      const [sliders, promos] = await Promise.all([
        bannerService.getSliderBanners(),
        bannerService.getPromoBanners(),
      ]);

      console.log("[Home] Sliders loaded:", sliders?.length || 0);
      console.log("[Home] Promos loaded:", promos?.length || 0);

      setSliderBanners(sliders || []);
      setPromoBanners(promos || []);
    } catch (err) {
      console.error("[Home] Banners error:", err.message);
      // Hata durumunda boş array kullan (fallback)
      setSliderBanners([]);
      setPromoBanners([]);
    } finally {
      setBannersLoading(false);
    }
  }, []);

  // ============================================
  // LIFECYCLE EFFECTS
  // ============================================

  useEffect(() => {
    loadData();

    // Sayfa odağına geldiğinde verileri yenile (sekmeler arası senkronizasyon)
    const handleFocus = () => {
      console.log("[Home] Sayfa odaklandı, veriler yenileniyor...");
      loadData();
    };
    window.addEventListener("focus", handleFocus);

    return () => {
      window.removeEventListener("focus", handleFocus);
    };
  }, [loadData]);

  // ============================================
  // FAVORİ İŞLEMLERİ
  // ============================================

  /** Favori ürün ekle/çıkar */
  const handleToggleFavorite = useCallback((productId) => {
    setFavorites((prev) => {
      const updated = prev.includes(productId)
        ? prev.filter((id) => id !== productId)
        : [...prev, productId];
      localStorage.setItem("favorites", JSON.stringify(updated));
      return updated;
    });
  }, []);

  // ============================================
  // SEPET İŞLEMLERİ
  // ============================================

  /** Sepete ürün ekle */
  const handleAddToCart = useCallback(
    (productId) => {
      const product = featured.find((p) => p.id === productId);
      if (product) {
        // TODO: Context veya Redux ile sepet yönetimi
        alert(`${product.name} sepete eklendi!`);
      }
    },
    [featured]
  );

  // ============================================
  // SEO METADATA
  // ============================================

  const siteUrl =
    process.env.REACT_APP_SITE_URL ||
    (typeof window !== "undefined" ? window.location.origin : "");

  // ============================================
  // RENDER
  // ============================================

  return (
    <div style={{ maxWidth: "1200px", margin: "0 auto", padding: "16px" }}>
      {/* SEO Helmet */}
      <Helmet>
        <title>Doğadan Sofranza — Taze ve doğal market ürünleri</title>
        <meta
          name="description"
          content="Doğadan Sofranza: Taze meyve, sebze, süt ürünleri ve günlük ihtiyaçlarınızı güvenle sipariş edin."
        />
        <meta
          property="og:title"
          content="Doğadan Sofranza — Taze ve doğal market ürünleri"
        />
        <meta
          property="og:description"
          content="Taze ve doğal ürünleri kapınıza getiren yerel market"
        />
        <meta
          property="og:image"
          content={`${siteUrl}/images/og-default.jpg`}
        />
        <link
          rel="canonical"
          href={`${siteUrl}${
            typeof window !== "undefined" ? window.location.pathname : ""
          }`}
        />
      </Helmet>

      {/* ========== HERO SLIDER SECTION ========== */}
      <section className="mb-4">
        <HeroSlider
          banners={sliderBanners}
          loading={bannersLoading}
          showNavigation={true}
          showDots={true}
          showContent={false}
          autoSlideInterval={5000}
        />
      </section>

      {/* ========== PROMO CARDS SECTION ========== */}
      <PromoCards
        promos={promoBanners}
        loading={bannersLoading}
        title="Kampanyalar"
        icon="fa-tags"
        showTitle={true}
      />

      {/* ========== KATEGORİLER SECTION ========== */}
      <section className="mb-4">
        <h2
          style={{
            fontSize: "1.25rem",
            fontWeight: "600",
            marginBottom: "12px",
            display: "flex",
            alignItems: "center",
            gap: "8px",
          }}
        >
          <i className="fas fa-th-large" style={{ color: "#10b981" }}></i>
          Kategoriler
        </h2>

        {categoriesLoading ? (
          // Loading State
          <div
            style={{
              textAlign: "center",
              padding: "30px 20px",
              color: "#6b7280",
            }}
          >
            <i
              className="fas fa-spinner fa-spin me-2"
              style={{ fontSize: "20px" }}
            ></i>
            Kategoriler yükleniyor…
          </div>
        ) : categories.length === 0 ? (
          // Empty State
          <div
            style={{
              textAlign: "center",
              padding: "30px 20px",
              color: "#9ca3af",
              backgroundColor: "#f9fafb",
              borderRadius: "12px",
            }}
          >
            <i className="fas fa-folder-open fa-2x mb-2 d-block opacity-50"></i>
            Kategori bulunamadı
          </div>
        ) : (
          // Kategori Grid
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(140px, 1fr))",
              gap: "12px",
            }}
          >
            {categories.map((c) => (
              <CategoryTile key={c.id} category={c} />
            ))}
          </div>
        )}
      </section>

      {/* ========== ÖNE ÇIKANLAR SECTION ========== */}
      <section>
        <h2
          style={{
            fontSize: "1.25rem",
            fontWeight: "600",
            marginBottom: "12px",
            display: "flex",
            alignItems: "center",
            gap: "8px",
          }}
        >
          <i className="fas fa-star" style={{ color: "#eab308" }}></i>
          Öne Çıkanlar
        </h2>

        {productLoading ? (
          // Loading State
          <div
            style={{ textAlign: "center", padding: "20px", color: "#6b7280" }}
          >
            <i className="fas fa-spinner fa-spin me-2"></i>
            Ürünler yükleniyor…
          </div>
        ) : productError ? (
          // Error State
          <div
            style={{
              color: "#dc2626",
              padding: "16px",
              background: "#fef2f2",
              borderRadius: "12px",
              display: "flex",
              alignItems: "center",
              gap: "12px",
            }}
          >
            <i className="fas fa-exclamation-circle fa-lg"></i>
            <div>
              <strong>Hata:</strong> {productError}
              <button
                onClick={loadData}
                style={{
                  marginLeft: "12px",
                  padding: "4px 12px",
                  backgroundColor: "#dc2626",
                  color: "white",
                  border: "none",
                  borderRadius: "6px",
                  cursor: "pointer",
                  fontSize: "0.85rem",
                }}
              >
                Tekrar Dene
              </button>
            </div>
          </div>
        ) : featured.length === 0 ? (
          // Empty State
          <div
            style={{
              textAlign: "center",
              padding: "30px 20px",
              color: "#9ca3af",
              backgroundColor: "#f9fafb",
              borderRadius: "12px",
            }}
          >
            <i className="fas fa-box-open fa-2x mb-2 d-block opacity-50"></i>
            Henüz ürün eklenmemiş
          </div>
        ) : (
          // Ürün Grid
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(160px, 1fr))",
              gap: "12px",
            }}
          >
            {featured.map((p) => (
              <ProductCard
                key={p.id}
                product={p}
                onToggleFavorite={handleToggleFavorite}
                isFavorite={favorites.includes(p.id)}
                onAddToCart={handleAddToCart}
              />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
