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
import homeBlockService from "../services/homeBlockService";
import { Helmet } from "react-helmet-async";
import ProductCard from "./components/ProductCard";
import CategoryTile from "./components/CategoryTile";
import ProductBlockSection from "./components/ProductBlockSection";
import HeroSlider from "../components/HeroSlider";
import PromoCards from "../components/PromoCards";
import { useCart } from "../contexts/CartContext";
import { useAuth } from "../contexts/AuthContext";
import AddToCartModal from "../components/AddToCartModal";

// ============================================
// ANA BİLEŞEN
// ============================================

// GÜVENLİK: Production'da debug log'ları kapalı
const DEBUG = process.env.NODE_ENV === "development";

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

  // Home Block state'leri (Poster + Ürün blokları)
  const [homeBlocks, setHomeBlocks] = useState([]);
  const [blocksLoading, setBlocksLoading] = useState(true);

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

  // Auth context'i - Admin kontrolü için
  const { user } = useAuth();

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
      DEBUG && console.log("[Home] Categories from API:", cats?.length || 0);
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

      DEBUG && console.log("[Home] Sliders loaded:", sliders?.length || 0);
      DEBUG && console.log("[Home] Promos loaded:", promos?.length || 0);

      setSliderBanners(sliders || []);
      setPromoBanners(promos || []);

      // Recipe'leri ayrı yükle (hata durumunda diğerlerini etkilemesin)
      try {
        const recipes = await bannerService.getRecipeBanners();
        DEBUG &&
          console.log(
            "[Home] 🍳 Recipes loaded:",
            recipes?.length || 0,
            recipes,
          );
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

    // Ana sayfa ürün bloklarını yükle (Poster + Ürünler)
    DEBUG && console.log("[Home] 🚀 Home blocks yükleme başlatılıyor...");
    setBlocksLoading(true);
    try {
      DEBUG &&
        console.log(
          "[Home] 📡 API çağrısı yapılıyor: homeBlockService.getActiveBlocks()",
        );
      const blocks = await homeBlockService.getActiveBlocks();
      DEBUG &&
        console.log(
          "[Home] 📦 Home blocks loaded:",
          blocks?.length || 0,
          blocks,
        );
      setHomeBlocks(blocks || []);
    } catch (err) {
      console.error("[Home] ❌ Home blocks error:", err?.message || err);
      setHomeBlocks([]);
    } finally {
      setBlocksLoading(false);
      DEBUG && console.log("[Home] ✅ Home blocks yükleme tamamlandı");
    }
  }, []);

  // ============================================
  // LIFECYCLE EFFECTS
  // ============================================

  useEffect(() => {
    loadData();

    // Sayfa odağına geldiğinde verileri yenile (sekmeler arası senkronizasyon)
    const handleFocus = () => {
      DEBUG && console.log("[Home] Sayfa odaklandı, veriler yenileniyor...");
      loadData();
    };
    window.addEventListener("focus", handleFocus);

    // ProductService subscription - Admin panelinde yapılan CRUD değişikliklerinde
    // ana sayfa otomatik olarak güncellenir (real-time senkronizasyon)
    const unsubscribe = ProductService.subscribe((event) => {
      DEBUG &&
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
    async (productOrId, fallbackId) => {
      let product = null;

      if (productOrId && typeof productOrId === "object") {
        product = productOrId;
      } else {
        const productId =
          typeof productOrId === "number" ? productOrId : fallbackId;
        product = featured.find((p) => p.id === productId) || null;
      }

      if (!product) {
        console.warn("[Home] Sepete ekleme için ürün bulunamadı:", productOrId);
        return;
      }

      try {
        await addToCart(product, 1);
        setAddedProduct(product);
        setCartModalOpen(true);
      } catch (error) {
        console.error("Sepete ekleme hatası:", error);
        alert("Ürün sepete eklenirken bir hata oluştu.");
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
        <title>Gölköy Gurme — Taze ve doğal market ürünleri</title>
        <meta
          name="description"
          content="Gölköy Gurme: Taze meyve, sebze, süt ürünleri ve günlük ihtiyaçlarınızı güvenle sipariş edin."
        />
        <meta
          property="og:title"
          content="Gölköy Gurme — Taze ve doğal market ürünleri"
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

      {/* ========== KAMPANYALAR SECTION (Backend'den) ========== */}
      {activeCampaigns.length > 0 && (
        <section className="mb-4 campaigns-section">
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              marginBottom: "20px",
            }}
          >
            <h2
              style={{
                fontSize: "1.3rem",
                fontWeight: "700",
                margin: 0,
                display: "flex",
                alignItems: "center",
                gap: "10px",
                color: "#1f2937",
              }}
            >
              <div
                style={{
                  width: "36px",
                  height: "36px",
                  borderRadius: "10px",
                  background: "linear-gradient(135deg, #f97316, #ea580c)",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                <i
                  className="fas fa-tags"
                  style={{ color: "#fff", fontSize: "0.9rem" }}
                ></i>
              </div>
              Kampanyalar
            </h2>
            <Link
              to={
                user?.isAdmin ||
                ["Admin", "SuperAdmin", "StoreManager"].includes(user?.role)
                  ? "/admin/campaigns"
                  : "/campaigns"
              }
              className="campaigns-view-all-btn"
              style={{
                color: "#fff",
                textDecoration: "none",
                fontSize: "0.8rem",
                fontWeight: "600",
                display: "flex",
                alignItems: "center",
                gap: "6px",
                background: "linear-gradient(135deg, #f97316, #ea580c)",
                padding: "8px 16px",
                borderRadius: "10px",
                transition: "transform 0.2s ease, box-shadow 0.2s ease",
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = "translateY(-1px)";
                e.currentTarget.style.boxShadow =
                  "0 4px 12px rgba(249,115,22,0.35)";
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = "translateY(0)";
                e.currentTarget.style.boxShadow = "none";
              }}
            >
              {user?.isAdmin ||
              ["Admin", "SuperAdmin", "StoreManager"].includes(user?.role)
                ? "Kampanya Yönetimi"
                : "Tümünü Gör"}
              <i
                className="fas fa-arrow-right"
                style={{ fontSize: "0.7rem" }}
              ></i>
            </Link>
          </div>

          {/* Kampanya Kartları */}
          {(() => {
            const imageCampaigns = activeCampaigns.filter((c) => c.imageUrl);
            const textCampaigns = activeCampaigns.filter((c) => !c.imageUrl);

            const badgeColorMap = {
              danger: "#ef4444",
              warning: "#f59e0b",
              success: "#10b981",
              info: "#6366f1",
              secondary: "#6b7280",
            };

            const typeConfig = {
              0: {
                bg: "#fef3c7",
                border: "#f59e0b",
                icon: "fa-percent",
                color: "#b45309",
                gradient: "linear-gradient(135deg, #fef3c7, #fde68a)",
              },
              1: {
                bg: "#dbeafe",
                border: "#3b82f6",
                icon: "fa-tag",
                color: "#1d4ed8",
                gradient: "linear-gradient(135deg, #dbeafe, #bfdbfe)",
              },
              2: {
                bg: "#dcfce7",
                border: "#22c55e",
                icon: "fa-gift",
                color: "#15803d",
                gradient: "linear-gradient(135deg, #dcfce7, #bbf7d0)",
              },
              3: {
                bg: "#f3e8ff",
                border: "#a855f7",
                icon: "fa-truck",
                color: "#7e22ce",
                gradient: "linear-gradient(135deg, #f3e8ff, #e9d5ff)",
              },
            };

            const formatEndDate = (dateStr) => {
              if (!dateStr) return null;
              try {
                const d = new Date(dateStr);
                return d.toLocaleDateString("tr-TR", {
                  day: "numeric",
                  month: "short",
                });
              } catch {
                return null;
              }
            };

            return (
              <>
                {/* Fotoğraflı Kampanyalar - Yatay Slider */}
                {imageCampaigns.length > 0 && (
                  <div
                    className="campaign-image-slider"
                    style={{
                      display: "flex",
                      gap: "16px",
                      overflowX: "auto",
                      scrollBehavior: "smooth",
                      padding: "4px 4px 16px",
                      scrollbarWidth: "none",
                      msOverflowStyle: "none",
                      scrollSnapType: "x proximity",
                    }}
                  >
                    {imageCampaigns.map((campaign) => {
                      const tc = typeConfig[campaign.type] || typeConfig[0];
                      const endStr = formatEndDate(campaign.endDate);
                      return (
                        <div
                          key={campaign.id}
                          className="campaign-image-card"
                          style={{
                            flexShrink: 0,
                            width: "340px",
                            borderRadius: "16px",
                            overflow: "hidden",
                            boxShadow: "0 2px 12px rgba(0,0,0,0.08)",
                            backgroundColor: "#fff",
                            transition:
                              "transform 0.3s ease, box-shadow 0.3s ease",
                            scrollSnapAlign: "start",
                            position: "relative",
                          }}
                          onMouseEnter={(e) => {
                            e.currentTarget.style.transform =
                              "translateY(-6px) scale(1.01)";
                            e.currentTarget.style.boxShadow =
                              "0 12px 32px rgba(249,115,22,0.18)";
                          }}
                          onMouseLeave={(e) => {
                            e.currentTarget.style.transform =
                              "translateY(0) scale(1)";
                            e.currentTarget.style.boxShadow =
                              "0 2px 12px rgba(0,0,0,0.08)";
                          }}
                        >
                          {/* Kampanya Görseli */}
                          <div style={{ position: "relative" }}>
                            <img
                              src={campaign.imageUrl}
                              alt={campaign.name}
                              style={{
                                width: "100%",
                                height: "190px",
                                objectFit: "cover",
                                display: "block",
                              }}
                              loading="lazy"
                            />
                            {/* Badge */}
                            {campaign.badgeText && (
                              <span
                                style={{
                                  position: "absolute",
                                  top: "12px",
                                  right: "12px",
                                  backgroundColor:
                                    badgeColorMap[campaign.badgeColor] ||
                                    "#6366f1",
                                  color: "white",
                                  padding: "5px 12px",
                                  borderRadius: "20px",
                                  fontSize: "0.75rem",
                                  fontWeight: "700",
                                  boxShadow: "0 2px 8px rgba(0,0,0,0.2)",
                                  letterSpacing: "0.3px",
                                }}
                              >
                                {campaign.badgeText}
                              </span>
                            )}
                            {/* Gradient Overlay */}
                            <div
                              style={{
                                position: "absolute",
                                bottom: 0,
                                left: 0,
                                right: 0,
                                padding: "20px 14px 14px",
                                background:
                                  "linear-gradient(transparent, rgba(0,0,0,0.75))",
                                color: "white",
                              }}
                            >
                              <h3
                                style={{
                                  fontSize: "1rem",
                                  fontWeight: "700",
                                  margin: "0 0 4px 0",
                                  textShadow: "0 1px 3px rgba(0,0,0,0.4)",
                                  lineHeight: "1.3",
                                }}
                              >
                                {campaign.name}
                              </h3>
                              {campaign.displayText && (
                                <p
                                  style={{
                                    margin: 0,
                                    fontSize: "0.8rem",
                                    opacity: 0.9,
                                    fontWeight: "500",
                                  }}
                                >
                                  {campaign.displayText}
                                </p>
                              )}
                            </div>
                          </div>
                          {/* Alt Bilgi */}
                          <div
                            style={{
                              padding: "10px 14px",
                              display: "flex",
                              alignItems: "center",
                              justifyContent: "space-between",
                              borderTop: "1px solid #f3f4f6",
                            }}
                          >
                            <span
                              style={{
                                display: "inline-flex",
                                alignItems: "center",
                                gap: "5px",
                                backgroundColor: tc.bg,
                                color: tc.color,
                                padding: "3px 10px",
                                borderRadius: "6px",
                                fontSize: "0.7rem",
                                fontWeight: "600",
                              }}
                            >
                              <i
                                className={`fas ${tc.icon}`}
                                style={{ fontSize: "0.6rem" }}
                              ></i>
                              {campaign.displayText || campaign.name}
                            </span>
                            {endStr && (
                              <span
                                style={{
                                  fontSize: "0.7rem",
                                  color: "#9ca3af",
                                  fontWeight: "500",
                                }}
                              >
                                <i
                                  className="fas fa-clock"
                                  style={{
                                    marginRight: "4px",
                                    fontSize: "0.6rem",
                                  }}
                                ></i>
                                {endStr}'e kadar
                              </span>
                            )}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}

                {/* Yazılı Kampanyalar - Kart Grid */}
                {textCampaigns.length > 0 && (
                  <div
                    className="campaign-text-list"
                    style={{
                      display: "grid",
                      gridTemplateColumns:
                        "repeat(auto-fill, minmax(300px, 1fr))",
                      gap: "14px",
                      marginTop: imageCampaigns.length > 0 ? "20px" : "0",
                    }}
                  >
                    {textCampaigns.map((campaign) => {
                      const tc = typeConfig[campaign.type] || typeConfig[0];
                      const endStr = formatEndDate(campaign.endDate);

                      return (
                        <div
                          key={campaign.id}
                          className="campaign-text-card"
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "14px",
                            padding: "16px 18px",
                            background: tc.gradient,
                            borderRadius: "14px",
                            border: `1.5px solid ${tc.border}30`,
                            transition:
                              "transform 0.2s ease, box-shadow 0.2s ease",
                            cursor: "default",
                          }}
                          onMouseEnter={(e) => {
                            e.currentTarget.style.transform =
                              "translateY(-3px)";
                            e.currentTarget.style.boxShadow = `0 8px 20px ${tc.border}25`;
                          }}
                          onMouseLeave={(e) => {
                            e.currentTarget.style.transform = "translateY(0)";
                            e.currentTarget.style.boxShadow = "none";
                          }}
                        >
                          {/* Tip ikonu */}
                          <div
                            style={{
                              width: "48px",
                              height: "48px",
                              borderRadius: "14px",
                              backgroundColor: tc.border,
                              display: "flex",
                              alignItems: "center",
                              justifyContent: "center",
                              flexShrink: 0,
                              boxShadow: `0 4px 12px ${tc.border}40`,
                            }}
                          >
                            <i
                              className={`fas ${tc.icon}`}
                              style={{ color: "white", fontSize: "1.1rem" }}
                            ></i>
                          </div>
                          {/* Kampanya detay */}
                          <div style={{ flex: 1, minWidth: 0 }}>
                            <h4
                              style={{
                                margin: 0,
                                fontSize: "0.92rem",
                                fontWeight: "700",
                                color: tc.color,
                                lineHeight: "1.3",
                                overflow: "hidden",
                                textOverflow: "ellipsis",
                                whiteSpace: "nowrap",
                              }}
                            >
                              {campaign.name}
                            </h4>
                            {campaign.displayText && (
                              <p
                                style={{
                                  margin: "3px 0 0",
                                  fontSize: "0.78rem",
                                  color: "#4b5563",
                                  fontWeight: "500",
                                  overflow: "hidden",
                                  textOverflow: "ellipsis",
                                  whiteSpace: "nowrap",
                                }}
                              >
                                {campaign.displayText}
                              </p>
                            )}
                            {endStr && (
                              <p
                                style={{
                                  margin: "4px 0 0",
                                  fontSize: "0.68rem",
                                  color: "#9ca3af",
                                  fontWeight: "500",
                                }}
                              >
                                <i
                                  className="fas fa-clock"
                                  style={{ marginRight: "3px" }}
                                ></i>
                                {endStr}'e kadar
                              </p>
                            )}
                          </div>
                          {/* Badge */}
                          {campaign.badgeText && (
                            <span
                              style={{
                                backgroundColor: tc.border,
                                color: "white",
                                padding: "5px 12px",
                                borderRadius: "10px",
                                fontSize: "0.72rem",
                                fontWeight: "700",
                                flexShrink: 0,
                                boxShadow: `0 2px 8px ${tc.border}40`,
                                letterSpacing: "0.3px",
                              }}
                            >
                              {campaign.badgeText}
                            </span>
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}
              </>
            );
          })()}

          {/* Responsive CSS */}
          <style>{`
            .campaign-image-slider::-webkit-scrollbar { display: none; }
            @media (max-width: 768px) {
              .campaign-image-card { width: 280px !important; }
              .campaign-image-card img { height: 160px !important; }
              .campaign-text-list { grid-template-columns: 1fr !important; }
              .campaigns-view-all-btn { padding: 6px 12px !important; font-size: 0.75rem !important; }
            }
            @media (max-width: 480px) {
              .campaign-image-card { width: 240px !important; }
              .campaign-image-card img { height: 140px !important; }
            }
          `}</style>
        </section>
      )}

      {/* Promo Bannerlar (Poster sisteminden) */}
      <PromoCards
        promos={promoBanners}
        loading={bannersLoading}
        title="Fırsatlar"
        icon="fa-fire"
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

      {/* ========== ÜRÜN BLOKLARI SECTION (Poster + Ürünler) ========== */}
      {/* 
        Bu bölüm Admin Panel > Ana Sayfa Blokları'ndan kontrol edilir.
        Her blok: Sol tarafta poster, sağ tarafta ürün grid'i
        Blok Tipleri: manual, category, discounted, newest, bestseller
      */}
      {blocksLoading ? (
        <section className="mb-6">
          <div
            style={{
              textAlign: "center",
              padding: "40px 20px",
              color: "#6b7280",
            }}
          >
            <i
              className="fas fa-spinner fa-spin me-2"
              style={{ fontSize: "24px" }}
            ></i>
            <p className="mt-2">Ürün blokları yükleniyor...</p>
          </div>
        </section>
      ) : homeBlocks.length > 0 ? (
        <section
          className="product-blocks-section"
          style={{ marginTop: "32px" }}
        >
          {DEBUG &&
            console.log(
              "🏠 Rendering homeBlocks:",
              homeBlocks.length,
              homeBlocks,
            )}
          {homeBlocks.map((block) => (
            <ProductBlockSection
              key={block.id}
              block={block}
              onAddToCart={handleAddToCart}
              onToggleFavorite={handleToggleFavorite}
              favorites={favorites}
            />
          ))}
        </section>
      ) : null}

      {/* ========== ÖNE ÇIKANLAR SECTION (Fallback - Bloklar boşsa gösterilir) ========== */}
      {/* Eğer hiç blok tanımlı değilse veya bloklar yükleniyorsa eski sistemi göster */}
      {!blocksLoading && homeBlocks.length === 0 && (
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
      )}

      {/* ========== NE PİŞİRSEM? - ŞEF TAVSİYESİ SECTION ========== */}
      {/* 
        Bu bölüm Admin Panel > Poster Yönetimi'nden kontrol edilir.
        Banner tipi: "recipe" - Önerilen boyut: 600x300px (2:1 oran)
        Posterler tıklanabilir, /tarif/:id sayfasına yönlendirir.
      */}
      {/* DEBUG: Bu bölüm her zaman görünür olmalı */}
      {DEBUG &&
        console.log(
          "[Home] 🍳 Ne Pişirsem Bölümü Render - recipeBanners:",
          recipeBanners?.length || 0,
        )}
      <section
        className="chef-recommendation-section"
        style={{
          marginTop: "40px",
          marginBottom: "40px",
          padding: "24px",
          backgroundColor: "#fff8f5",
          borderRadius: "16px",
          border: "2px solid #ff6b35",
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
          {(recipeBanners.length > 0
            ? recipeBanners
            : [
                {
                  id: "demo1",
                  title: "Gurme Lezzetler",
                  imageUrl:
                    "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600&h=300&fit=crop&q=80",
                  linkUrl: "/tarif/1",
                },
                {
                  id: "demo2",
                  title: "Şef Tavsiyesi",
                  imageUrl:
                    "https://images.unsplash.com/photo-1547592166-23ac45744acd?w=600&h=300&fit=crop&q=80",
                  linkUrl: "/tarif/2",
                },
              ]
          )
            .slice(0, 2)
            .map((banner) => (
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
