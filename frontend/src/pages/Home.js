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
import { Link } from "react-router-dom";
import { ProductService } from "../services/productService";
import { CampaignService } from "../services/campaignService";
import bannerService from "../services/bannerService";
import categoryServiceReal from "../services/categoryServiceReal";
import { Helmet } from "react-helmet-async";
import ProductCard from "./components/ProductCard";
import CategoryTile from "./components/CategoryTile";
import HeroSlider from "../components/HeroSlider";
import PromoCards from "../components/PromoCards";
import { useCart } from "../contexts/CartContext";
import AddToCartModal from "../components/AddToCartModal";

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
  const [activeCampaigns, setActiveCampaigns] = useState([]);

  // Banner state'leri
  const [sliderBanners, setSliderBanners] = useState([]);
  const [promoBanners, setPromoBanners] = useState([]);
  const [recipeBanners, setRecipeBanners] = useState([]); // Şef Tavsiyesi / Tarif posterleri
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

  // Sepet context'i
  const { addToCart } = useCart();

  // Sepete ekleme modal state'i
  const [cartModalOpen, setCartModalOpen] = useState(false);
  const [addedProduct, setAddedProduct] = useState(null);

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

    // Aktif kampanyaları yükle (admin panel ile bağlı endpoint)
    try {
      const campaigns = await CampaignService.getActiveCampaigns();
      setActiveCampaigns(Array.isArray(campaigns) ? campaigns : []);
    } catch (err) {
      console.error("[Home] Campaigns error:", err?.message || err);
      setActiveCampaigns([]);
    }

    // Banner'ları yükle (paralel olarak - slider, promo ve recipe)
    setBannersLoading(true);
    try {
      // Slider ve Promo'yu paralel yükle
      const [sliders, promos] = await Promise.all([
        bannerService.getSliderBanners(),
        bannerService.getPromoBanners(),
      ]);

      console.log("[Home] Sliders loaded:", sliders?.length || 0);
      console.log("[Home] Promos loaded:", promos?.length || 0);

      setSliderBanners(sliders || []);
      setPromoBanners(promos || []);

      // Recipe'leri ayrı yükle (hata durumunda diğerlerini etkilemesin)
      try {
        const recipes = await bannerService.getRecipeBanners();
        console.log("[Home] 🍳 Recipes loaded:", recipes?.length || 0, recipes);
        setRecipeBanners(recipes || []);
      } catch (recipeErr) {
        console.error("[Home] ⚠️ Recipe banners error:", recipeErr);
        setRecipeBanners([]);
      }
    } catch (err) {
      console.error("[Home] Banners error:", err.message);
      // Hata durumunda boş array kullan (fallback)
      setSliderBanners([]);
      setPromoBanners([]);
      setRecipeBanners([]);
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

  // Kampanya seçimi - ürün ve kategori hedeflerine göre eşleştirir
  const getProductCategoryId = (product) =>
    product?.categoryId ?? product?.category?.id ?? null;

  const getCampaignForProduct = (product) => {
    if (!product || activeCampaigns.length === 0) return null;

    const productId = product.id;
    const categoryId = getProductCategoryId(product);

    const applicable = activeCampaigns.filter((c) => {
      const targetType = c?.targetType ?? c?.targetKind ?? c?.target;
      const targetIds = Array.isArray(c?.targetIds) ? c.targetIds : [];
      const targetKinds = Array.isArray(c?.targetKinds) ? c.targetKinds : [];
      const hasTargetIds = targetIds.length > 0;
      const cProductId = c?.productId ?? c?.targetId;
      const cCategoryId = c?.categoryId;

      if (targetType === "All" || targetType === 0 || !targetType) return true;
      if (targetType === "Product" || targetType === 2)
        return hasTargetIds
          ? targetIds.some((id) => String(id) === String(productId))
          : String(cProductId) === String(productId);
      if (targetType === "Category" || targetType === 1)
        return hasTargetIds
          ? targetIds.some((id) => String(id) === String(categoryId))
          : String(cCategoryId) === String(categoryId);
      return false;
    });

    if (applicable.length === 0) return null;

    // UI için en yüksek indirimi seç (backend önceliği yoksa)
    const score = (c) => {
      const type = typeof c.type === "string" ? parseInt(c.type) : c.type;
      const value = Number(c.discountValue || 0);
      if (type === 2) return 10; // BuyXPayY görsel öncelik
      if (type === 3) return 5; // FreeShipping
      return value;
    };

    return applicable.reduce((best, current) =>
      score(current) > score(best) ? current : best,
    );
  };

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
    async (productId) => {
      const product = featured.find((p) => p.id === productId);
      if (product) {
        try {
          await addToCart(product, 1);
          setAddedProduct(product);
          setCartModalOpen(true);
        } catch (error) {
          console.error("Sepete ekleme hatası:", error);
          alert("Ürün sepete eklenirken bir hata oluştu.");
        }
      }
    },
    [featured, addToCart],
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
                campaign={getCampaignForProduct(p)}
                onToggleFavorite={handleToggleFavorite}
                isFavorite={favorites.includes(p.id)}
                onAddToCart={handleAddToCart}
              />
            ))}
          </div>
        )}
      </section>

      {/* ========== NE PİŞİRSEM? - ŞEF TAVSİYESİ SECTION ========== */}
      {/* 
        Bu bölüm Admin Panel > Poster Yönetimi'nden kontrol edilir.
        Banner tipi: "recipe" - Önerilen boyut: 600x300px (2:1 oran)
        Posterler tıklanabilir, /tarif/:id sayfasına yönlendirir.
      */}
      {/* DEBUG: Bu bölüm her zaman görünür olmalı */}
      {console.log("[Home] 🍳 Ne Pişirsem Bölümü Render - recipeBanners:", recipeBanners?.length || 0)}
      <section
        className="chef-recommendation-section"
        style={{ 
          marginTop: "40px", 
          marginBottom: "40px",
          padding: "24px",
          backgroundColor: "#fff8f5",
          borderRadius: "16px",
          border: "2px solid #ff6b35"
        }}
      >
          <h2
            style={{
              fontSize: "1.35rem",
              fontWeight: "700",
              marginBottom: "20px",
              display: "flex",
              alignItems: "center",
              gap: "10px",
              color: "#ff6b35",
            }}
          >
            <i className="fas fa-utensils" style={{ color: "#ff6b35" }}></i>
            Ne Pişirsem?
            <span
              style={{
                backgroundColor: "#10b981",
                color: "white",
                fontSize: "0.65rem",
                fontWeight: "600",
                padding: "2px 8px",
                borderRadius: "12px",
                marginLeft: "8px",
              }}
            >
              YENİ
            </span>
          </h2>

          <div
            className="chef-posters-grid"
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(2, 1fr)",
              gap: "20px",
            }}
          >
            {/* API'den gelen posterler varsa onları göster, yoksa demo posterler */}
            {(recipeBanners.length > 0 ? recipeBanners : [
              {
                id: 'demo1',
                title: 'Gurme Lezzetler',
                imageUrl: 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600&h=300&fit=crop&q=80',
                linkUrl: '/tarif/1'
              },
              {
                id: 'demo2', 
                title: 'Şef Tavsiyesi',
                imageUrl: 'https://images.unsplash.com/photo-1547592166-23ac45744acd?w=600&h=300&fit=crop&q=80',
                linkUrl: '/tarif/2'
              }
            ]).slice(0, 2).map((banner) => (
              <Link
                key={banner.id}
                to={banner.linkUrl || `/tarif/${banner.id}`}
                className="chef-poster-card"
                style={{
                  display: "block",
                  position: "relative",
                  borderRadius: "16px",
                  overflow: "hidden",
                  boxShadow: "0 4px 16px rgba(0,0,0,0.1)",
                  transition: "transform 0.3s ease, box-shadow 0.3s ease",
                  textDecoration: "none",
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform =
                    "translateY(-6px) scale(1.01)";
                  e.currentTarget.style.boxShadow =
                    "0 12px 32px rgba(255,107,53,0.25)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = "translateY(0) scale(1)";
                  e.currentTarget.style.boxShadow =
                    "0 4px 16px rgba(0,0,0,0.1)";
                }}
              >
                <img
                  src={banner.imageUrl}
                  alt={banner.title || "Şef Tavsiyesi"}
                  style={{
                    width: "100%",
                    height: "auto",
                    aspectRatio: "2 / 1",
                    objectFit: "cover",
                    display: "block",
                  }}
                  loading="lazy"
                />
              </Link>
            ))}
          </div>

          {/* Responsive CSS - Mobilde tek sütun */}
          <style>{`
            @media (max-width: 768px) {
              .chef-posters-grid {
                grid-template-columns: 1fr !important;
                gap: 16px !important;
              }
              .chef-recommendation-section h2 {
                font-size: 1.15rem !important;
              }
            }
          `}</style>
        </section>

      {/* Sepete Ekleme Modal */}
      <AddToCartModal
        isOpen={cartModalOpen}
        onClose={() => setCartModalOpen(false)}
        product={addedProduct}
      />
    </div>
  );
}
