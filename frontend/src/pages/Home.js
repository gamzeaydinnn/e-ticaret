// Hero banner, kategori grid, kampanyalar, öne çıkan ürünler
import { useEffect, useState } from "react";
import { ProductService } from "../services/productService";
import { shouldUseMockData } from "../config/apiConfig";
import mockDataStore from "../services/mockDataStore";
import api from "../services/api";
import { Helmet } from "react-helmet-async";
import ProductCard from "./components/ProductCard";
import CategoryTile from "./components/CategoryTile";

export default function Home() {
  const [categories, setCategories] = useState([]);
  const [featured, setFeatured] = useState([]);
  const [productLoading, setProductLoading] = useState(true);
  const [productError, setProductError] = useState(null);
  const [sliderPosters, setSliderPosters] = useState([]);
  const [promoPosters, setPromoPosters] = useState([]);
  const [currentSlide, setCurrentSlide] = useState(0);
  const [favorites, setFavorites] = useState(() => {
    // localStorage'dan favori ürünleri yükle
    try {
      const stored = localStorage.getItem("favorites");
      return stored ? JSON.parse(stored) : [];
    } catch {
      return [];
    }
  });

  const loadData = () => {
    // Categories
    if (shouldUseMockData()) {
      setCategories(mockDataStore.getCategories());
    } else {
      api
        .get("/Categories")
        .then((r) => setCategories(r.data || r))
        .catch(() => {});
    }

    // Products
    setProductLoading(true);
    ProductService.list()
      .then((items) => setFeatured(items))
      .catch((e) => setProductError(e?.message || "Ürünler yüklenemedi"))
      .finally(() => setProductLoading(false));

    // Posters
    setSliderPosters(mockDataStore.getSliderPosters());
    setPromoPosters(mockDataStore.getPromoPosters());
  };

  // Favori ürün ekle/çıkar
  const handleToggleFavorite = (productId) => {
    setFavorites((prev) => {
      const updated = prev.includes(productId)
        ? prev.filter((id) => id !== productId)
        : [...prev, productId];
      localStorage.setItem("favorites", JSON.stringify(updated));
      return updated;
    });
  };

  // Sepete ekle
  const handleAddToCart = (productId) => {
    const product = featured.find((p) => p.id === productId);
    if (product) {
      // Sepet işlemleri burada yapılacak (Context/Redux kullanılıyorsa)
      alert(`${product.name} sepete eklendi!`);
    }
  };

  useEffect(() => {
    loadData();

    // Subscribe to changes for real-time updates
    const unsubProducts = mockDataStore.subscribe("products", loadData);
    const unsubCategories = mockDataStore.subscribe("categories", () => {
      if (shouldUseMockData()) {
        setCategories(mockDataStore.getCategories());
      }
    });
    const unsubPosters = mockDataStore.subscribe("posters", () => {
      setSliderPosters(mockDataStore.getSliderPosters());
      setPromoPosters(mockDataStore.getPromoPosters());
    });

    // Farklı sekmeler arası senkronizasyon
    const handleStorageChange = (e) => {
      if (e.key?.startsWith("mockStore_")) {
        loadData();
      }
    };
    window.addEventListener("storage", handleStorageChange);

    return () => {
      unsubProducts && unsubProducts();
      unsubCategories && unsubCategories();
      unsubPosters && unsubPosters();
      window.removeEventListener("storage", handleStorageChange);
    };
  }, []);

  // Auto-rotation for slider (5 seconds)
  useEffect(() => {
    if (sliderPosters.length <= 1) return;
    const timer = setInterval(() => {
      setCurrentSlide((prev) => (prev + 1) % sliderPosters.length);
    }, 5000);
    return () => clearInterval(timer);
  }, [sliderPosters.length]);

  return (
    <div style={{ maxWidth: "1200px", margin: "0 auto", padding: "16px" }}>
      <Helmet>
        {(() => {
          const siteUrl =
            process.env.REACT_APP_SITE_URL ||
            (typeof window !== "undefined" ? window.location.origin : "");
          return (
            <>
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
            </>
          );
        })()}
      </Helmet>
      {/* Hero Slider Section */}
      <section className="mb-4">
        {sliderPosters.length > 0 ? (
          <div
            style={{
              position: "relative",
              borderRadius: "12px",
              overflow: "hidden",
              boxShadow: "0 4px 6px rgba(0,0,0,0.1)",
            }}
          >
            {/* Slider Container */}
            <div
              style={{ position: "relative", height: "300px", width: "100%" }}
            >
              {sliderPosters.map((poster, index) => (
                <a
                  key={poster.id}
                  href={poster.linkUrl || "#"}
                  style={{
                    position: "absolute",
                    top: 0,
                    left: 0,
                    width: "100%",
                    height: "100%",
                    opacity: index === currentSlide ? 1 : 0,
                    visibility: index === currentSlide ? "visible" : "hidden",
                    transition: "opacity 0.5s ease-in-out, visibility 0.5s",
                    zIndex: index === currentSlide ? 1 : 0,
                  }}
                >
                  <img
                    src={poster.imageUrl}
                    alt={poster.title}
                    style={{
                      width: "100%",
                      height: "100%",
                      objectFit: "cover",
                      display: "block",
                    }}
                    onError={(e) => {
                      e.target.src = "/images/placeholder.png";
                    }}
                  />
                </a>
              ))}
            </div>

            {/* Navigation Arrows */}
            {sliderPosters.length > 1 && (
              <>
                <button
                  onClick={() =>
                    setCurrentSlide(
                      (prev) =>
                        (prev - 1 + sliderPosters.length) % sliderPosters.length
                    )
                  }
                  style={{
                    position: "absolute",
                    left: "10px",
                    top: "50%",
                    transform: "translateY(-50%)",
                    background: "rgba(0,0,0,0.5)",
                    color: "white",
                    border: "none",
                    borderRadius: "50%",
                    width: "40px",
                    height: "40px",
                    cursor: "pointer",
                    zIndex: 10,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                  }}
                  aria-label="Önceki"
                >
                  <i className="fas fa-chevron-left"></i>
                </button>
                <button
                  onClick={() =>
                    setCurrentSlide((prev) => (prev + 1) % sliderPosters.length)
                  }
                  style={{
                    position: "absolute",
                    right: "10px",
                    top: "50%",
                    transform: "translateY(-50%)",
                    background: "rgba(0,0,0,0.5)",
                    color: "white",
                    border: "none",
                    borderRadius: "50%",
                    width: "40px",
                    height: "40px",
                    cursor: "pointer",
                    zIndex: 10,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                  }}
                  aria-label="Sonraki"
                >
                  <i className="fas fa-chevron-right"></i>
                </button>
              </>
            )}

            {/* Navigation Dots */}
            {sliderPosters.length > 1 && (
              <div
                style={{
                  position: "absolute",
                  bottom: "12px",
                  left: "50%",
                  transform: "translateX(-50%)",
                  display: "flex",
                  gap: "8px",
                  zIndex: 10,
                }}
              >
                {sliderPosters.map((_, index) => (
                  <button
                    key={index}
                    onClick={() => setCurrentSlide(index)}
                    style={{
                      width: "10px",
                      height: "10px",
                      borderRadius: "50%",
                      border: "2px solid white",
                      cursor: "pointer",
                      backgroundColor:
                        index === currentSlide ? "white" : "transparent",
                      transition: "background-color 0.3s",
                    }}
                    aria-label={`Slide ${index + 1}`}
                  />
                ))}
              </div>
            )}
          </div>
        ) : (
          <div
            style={{
              background: "linear-gradient(135deg, #38bdf8 0%, #6366f1 100%)",
              borderRadius: "12px",
              padding: "40px 32px",
              color: "white",
            }}
          >
            <h1
              style={{
                fontSize: "1.75rem",
                fontWeight: "bold",
                marginBottom: "8px",
              }}
            >
              Bugün ne sipariş ediyorsun?
            </h1>
            <p style={{ opacity: 0.9 }}>
              Hızlı teslimat — Taze ürünler — Güvenli ödeme
            </p>
          </div>
        )}
      </section>

      {/* Promo Grid Section */}
      {promoPosters.length > 0 && (
        <section className="mb-4">
          <h2
            style={{
              fontSize: "1.25rem",
              fontWeight: "600",
              marginBottom: "12px",
            }}
          >
            <i className="fas fa-tags me-2" style={{ color: "#f97316" }}></i>
            Kampanyalar
          </h2>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(150px, 1fr))",
              gap: "12px",
            }}
          >
            {promoPosters.map((promo) => (
              <a
                key={promo.id}
                href={promo.linkUrl || "#"}
                style={{
                  display: "block",
                  borderRadius: "8px",
                  overflow: "hidden",
                  boxShadow: "0 2px 4px rgba(0,0,0,0.1)",
                  transition: "transform 0.2s, box-shadow 0.2s",
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = "translateY(-4px)";
                  e.currentTarget.style.boxShadow =
                    "0 8px 16px rgba(0,0,0,0.15)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = "translateY(0)";
                  e.currentTarget.style.boxShadow = "0 2px 4px rgba(0,0,0,0.1)";
                }}
              >
                <img
                  src={promo.imageUrl}
                  alt={promo.title}
                  style={{
                    width: "100%",
                    height: "120px",
                    objectFit: "cover",
                    display: "block",
                  }}
                  onError={(e) => {
                    e.target.src = "/images/placeholder.png";
                  }}
                />
              </a>
            ))}
          </div>
        </section>
      )}

      <section className="mb-4">
        <h2
          style={{
            fontSize: "1.25rem",
            fontWeight: "600",
            marginBottom: "12px",
          }}
        >
          <i className="fas fa-th-large me-2" style={{ color: "#10b981" }}></i>
          Kategoriler
        </h2>
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
      </section>

      <section>
        <h2
          style={{
            fontSize: "1.25rem",
            fontWeight: "600",
            marginBottom: "12px",
          }}
        >
          <i className="fas fa-star me-2" style={{ color: "#eab308" }}></i>
          Öne Çıkanlar
        </h2>
        {productLoading && (
          <div
            style={{ textAlign: "center", padding: "20px", color: "#6b7280" }}
          >
            <i className="fas fa-spinner fa-spin me-2"></i>Ürünler yükleniyor…
          </div>
        )}
        {productError && !productLoading && (
          <div
            style={{
              color: "#dc2626",
              padding: "12px",
              background: "#fef2f2",
              borderRadius: "8px",
            }}
          >
            <i className="fas fa-exclamation-circle me-2"></i>Hata:{" "}
            {productError}
          </div>
        )}
        {!productLoading && !productError && (
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
