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

    // ProductService subscription - Admin panelinde yapılan CRUD değişikliklerinde
    // ana sayfa otomatik olarak güncellenir (real-time senkronizasyon)
    const unsubscribe = ProductService.subscribe((event) => {
      console.log("[Home] 📦 Ürün değişikliği algılandı:", event.action);
      // Ürün eklendiğinde, güncellendiğinde, silindiğinde veya import edildiğinde
      // ana sayfa ürünlerini yenile
      if (["create", "update", "delete", "import"].includes(event.action)) {
        loadData();
      }
    });

    return () => {
      window.removeEventListener("focus", handleFocus);
      // Subscription'ı temizle (memory leak önleme)
      unsubscribe();
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

      {/* ========== ŞEF TAVSİYESİ SECTION ========== */}
      <section
        className="mb-4"
        style={{ maxWidth: "900px", margin: "0 auto 24px" }}
      >
        <h2
          style={{
            fontSize: "1.25rem",
            fontWeight: "700",
            marginBottom: "16px",
            display: "flex",
            alignItems: "center",
            gap: "8px",
            color: "#ff6b35",
          }}
        >
          <i className="fas fa-hat-chef" style={{ color: "#ff6b35" }}></i>
          Şef Tavsiyesi
        </h2>

        <div
          className="chef-recommendations"
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(300px, 1fr))",
            gap: "16px",
          }}
        >
          {/* Gurme Lezzetler */}
          <div
            className="chef-card"
            style={{
              position: "relative",
              borderRadius: "16px",
              overflow: "hidden",
              boxShadow: "0 4px 12px rgba(0,0,0,0.1)",
              transition: "transform 0.3s ease, box-shadow 0.3s ease",
              cursor: "pointer",
              backgroundColor: "#fff",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = "translateY(-4px)";
              e.currentTarget.style.boxShadow =
                "0 8px 24px rgba(255,107,53,0.2)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = "translateY(0)";
              e.currentTarget.style.boxShadow = "0 4px 12px rgba(0,0,0,0.1)";
            }}
          >
            <img
              src="https://images.migrosone.com/sanalmarket/banner/main_page_slider/144575/146609-banner-20260113145210-29b3cc.jpg"
              alt="Gurme Lezzetler"
              style={{
                width: "100%",
                height: "auto",
                display: "block",
                objectFit: "cover",
              }}
              loading="lazy"
            />
          </div>

          {/* Şef Tavsiyesi Alayım */}
          <div
            className="chef-card"
            style={{
              position: "relative",
              borderRadius: "16px",
              overflow: "hidden",
              boxShadow: "0 4px 12px rgba(0,0,0,0.1)",
              transition: "transform 0.3s ease, box-shadow 0.3s ease",
              cursor: "pointer",
              backgroundColor: "#fff",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = "translateY(-4px)";
              e.currentTarget.style.boxShadow =
                "0 8px 24px rgba(255,107,53,0.2)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = "translateY(0)";
              e.currentTarget.style.boxShadow = "0 4px 12px rgba(0,0,0,0.1)";
            }}
          >
            <img
              src="https://images.migrosone.com/sanalmarket/banner/dogal-indirim-banner.png"
              alt="Şef Tavsiyesi Alayım"
              style={{
                width: "100%",
                height: "auto",
                display: "block",
                objectFit: "cover",
              }}
              loading="lazy"
            />
          </div>
        </div>
      </section>

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

      {/* ========== ÖZELLİKLER SECTION ========== */}
      <section className="mb-5" style={{ marginTop: "40px" }}>
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
            gap: "20px",
            maxWidth: "1200px",
            margin: "0 auto",
          }}
        >
          {/* Esnek Ödeme */}
          <div
            className="feature-card"
            style={{
              textAlign: "center",
              padding: "24px",
              backgroundColor: "#fff",
              borderRadius: "16px",
              boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
              transition: "transform 0.3s ease, box-shadow 0.3s ease",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = "translateY(-4px)";
              e.currentTarget.style.boxShadow =
                "0 8px 20px rgba(255,149,0,0.15)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = "translateY(0)";
              e.currentTarget.style.boxShadow = "0 2px 8px rgba(0,0,0,0.08)";
            }}
          >
            <div className="feature-icon" style={{ marginBottom: "16px" }}>
              <i
                className="fas fa-credit-card"
                style={{ fontSize: "2.5rem", color: "#ff9500" }}
              ></i>
            </div>
            <h5
              className="feature-title"
              style={{
                fontWeight: "700",
                marginBottom: "8px",
                fontSize: "1rem",
              }}
            >
              Esnek Ödeme İmkanları
            </h5>
            <p
              className="feature-desc"
              style={{ color: "#6b7280", fontSize: "0.875rem", margin: 0 }}
            >
              Kapıda veya Kredi Kartı ile Online Ödeme Yapın
            </p>
          </div>

          {/* İstediğin Saatte Teslimat */}
          <div
            className="feature-card"
            style={{
              textAlign: "center",
              padding: "24px",
              backgroundColor: "#fff",
              borderRadius: "16px",
              boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
              transition: "transform 0.3s ease, box-shadow 0.3s ease",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = "translateY(-4px)";
              e.currentTarget.style.boxShadow =
                "0 8px 20px rgba(255,149,0,0.15)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = "translateY(0)";
              e.currentTarget.style.boxShadow = "0 2px 8px rgba(0,0,0,0.08)";
            }}
          >
            <div className="feature-icon" style={{ marginBottom: "16px" }}>
              <i
                className="fas fa-truck"
                style={{ fontSize: "2.5rem", color: "#ff9500" }}
              ></i>
            </div>
            <h5
              className="feature-title"
              style={{
                fontWeight: "700",
                marginBottom: "8px",
                fontSize: "1rem",
              }}
            >
              İstediğin Saatte Teslimat
            </h5>
            <p
              className="feature-desc"
              style={{ color: "#6b7280", fontSize: "0.875rem", margin: 0 }}
            >
              Haftanın 7 günü İstediğin Saatte Teslim Edelim
            </p>
          </div>

          {/* Özenle Seçilmiş Ürünler */}
          <div
            className="feature-card"
            style={{
              textAlign: "center",
              padding: "24px",
              backgroundColor: "#fff",
              borderRadius: "16px",
              boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
              transition: "transform 0.3s ease, box-shadow 0.3s ease",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = "translateY(-4px)";
              e.currentTarget.style.boxShadow =
                "0 8px 20px rgba(255,149,0,0.15)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = "translateY(0)";
              e.currentTarget.style.boxShadow = "0 2px 8px rgba(0,0,0,0.08)";
            }}
          >
            <div className="feature-icon" style={{ marginBottom: "16px" }}>
              <i
                className="fas fa-box-open"
                style={{ fontSize: "2.5rem", color: "#ff9500" }}
              ></i>
            </div>
            <h5
              className="feature-title"
              style={{
                fontWeight: "700",
                marginBottom: "8px",
                fontSize: "1rem",
              }}
            >
              Özenle Seçilmiş, Paketlenmiş Ürünler
            </h5>
            <p
              className="feature-desc"
              style={{ color: "#6b7280", fontSize: "0.875rem", margin: 0 }}
            >
              Senin İçin Tüm Siparişlerini Özenle Hazırlıyoruz
            </p>
          </div>

          {/* Doğal Ürün Garantisi */}
          <div
            className="feature-card"
            style={{
              textAlign: "center",
              padding: "24px",
              backgroundColor: "#fff",
              borderRadius: "16px",
              boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
              transition: "transform 0.3s ease, box-shadow 0.3s ease",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = "translateY(-4px)";
              e.currentTarget.style.boxShadow =
                "0 8px 20px rgba(39,174,96,0.15)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = "translateY(0)";
              e.currentTarget.style.boxShadow = "0 2px 8px rgba(0,0,0,0.08)";
            }}
          >
            <div className="feature-icon" style={{ marginBottom: "16px" }}>
              <i
                className="fas fa-leaf"
                style={{ fontSize: "2.5rem", color: "#27ae60" }}
              ></i>
            </div>
            <h5
              className="feature-title"
              style={{
                fontWeight: "700",
                marginBottom: "8px",
                fontSize: "1rem",
              }}
            >
              Doğal Ürün Garantisi
            </h5>
            <p
              className="feature-desc"
              style={{ color: "#6b7280", fontSize: "0.875rem", margin: 0 }}
            >
              Tüm Ürünlerimiz %100 Doğal ve Taze Garantilidir
            </p>
          </div>
        </div>
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
              gridTemplateColumns: "repeat(auto-fill, minmax(240px, 280px))",
              gap: "20px",
              justifyContent: "center",
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
