import { useEffect, useMemo, useState, useRef, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { debugLog } from "../config/apiConfig";
import getProductCategoryRules from "../config/productCategoryRules";
import { useAuth } from "../contexts/AuthContext";
import { useCart } from "../contexts/CartContext";
import { useCompare } from "../contexts/CompareContext";
import { useFavorites } from "../contexts/FavoriteContext";
import { ProductService } from "../services/productService";
import productServiceMock from "../services/productServiceMock";
import LoginModal from "./LoginModal";
import LoginRequiredModal from "./LoginRequiredModal";
import "./ProductGrid.css";

const DEMO_PRODUCTS = [
  // Et ve Et Ürünleri (categoryId: 1)
  {
    id: 1,
    name: "Dana But Tas Kebaplık Et Çiftlik Kg",
    description: "Taze dana eti, kuşbaşı doğranmış 500g",
    price: 375.95,
    originalPrice: 429.95,
    categoryId: 1,
    categoryName: "Et ve Et Ürünleri",
    imageUrl: "/images/dana-kusbasi.jpg",
    isNew: true,
    discountPercentage: 26,
    rating: 4.7,
    reviewCount: 67,
    badge: "İndirim",
    specialPrice: 279.0,
    stockQuantity: 25,
  },
  {
    id: 2,
    name: "Kuzu İncik Kg",
    description: "Taze kuzu incik, kilogram",
    price: 1399.95,
    categoryId: 1,
    categoryName: "Et ve Et Ürünleri",
    imageUrl: "/images/kuzu-incik.webp",
    isNew: false,
    discountPercentage: 50,
    rating: 4.4,
    reviewCount: 195,
    badge: "İndirim",
    specialPrice: 699.95,
    stockQuantity: 15,
  },
  // Süt Ürünleri (categoryId: 2)
  {
    id: 3,
    name: "Pınar Süt 1L",
    description: "Tam yağlı UHT süt 1 litre",
    price: 28.5,
    categoryId: 2,
    categoryName: "Süt Ürünleri",
    imageUrl: "/images/pinar-nestle-sut.jpg",
    isNew: false,
    discountPercentage: 0,
    rating: 4.6,
    reviewCount: 234,
    stockQuantity: 50,
  },
  {
    id: 4,
    name: "Sek Kaşar Peyniri 200 G",
    description: "Dilimli kaşar peyniri 200g",
    price: 75.9,
    categoryId: 2,
    categoryName: "Süt Ürünleri",
    imageUrl: "/images/sek-kasar-peyniri-200-gr-38be46-1650x1650.jpg",
    isNew: false,
    discountPercentage: 15,
    rating: 4.4,
    reviewCount: 156,
    badge: "İndirim",
    specialPrice: 64.5,
    stockQuantity: 30,
  },
  // Meyve ve Sebze (categoryId: 3)
  {
    id: 5,
    name: "Domates Kg",
    description: "Taze domates, kilogram",
    price: 45.9,
    categoryId: 3,
    categoryName: "Meyve ve Sebze",
    imageUrl: "/images/domates.webp",
    isNew: true,
    discountPercentage: 0,
    rating: 4.9,
    reviewCount: 312,
    stockQuantity: 100,
  },
  {
    id: 6,
    name: "Salatalık Kg",
    description: "Taze salatalık, kilogram",
    price: 28.9,
    categoryId: 3,
    categoryName: "Meyve ve Sebze",
    imageUrl: "/images/salatalik.jpg",
    isNew: false,
    discountPercentage: 0,
    rating: 4.3,
    reviewCount: 67,
    stockQuantity: 80,
  },
  // İçecekler (categoryId: 4)
  {
    id: 7,
    name: "Lipton Ice Tea Limon 330 Ml",
    description: "Soğuk çay, kutu 330ml",
    price: 60.0,
    categoryId: 4,
    categoryName: "İçecekler",
    imageUrl: "/images/lipton-ice-tea.jpg",
    isNew: false,
    discountPercentage: 32,
    rating: 4.2,
    reviewCount: 89,
    badge: "İndirim",
    specialPrice: 40.9,
    stockQuantity: 60,
  },
  {
    id: 8,
    name: "Nescafe 2si 1 Arada Sütlü Köpüklü 15 x 10g",
    description: "Kahve karışımı, paket 15 x 10g",
    price: 145.55,
    originalPrice: 169.99,
    categoryId: 4,
    categoryName: "İçecekler",
    imageUrl: "/images/nescafe.jpg",
    isNew: false,
    discountPercentage: 42,
    rating: 4.3,
    reviewCount: 143,
    badge: "İndirim",
    specialPrice: 84.5,
    stockQuantity: 25,
  },
  {
    id: 9,
    name: "Coca-Cola Orijinal Tat Kutu 330ml",
    description: "Kola gazlı içecek kutu",
    price: 12.5,
    categoryId: 4,
    categoryName: "İçecekler",
    imageUrl: "/images/coca-cola.jpg",
    isNew: false,
    discountPercentage: 20,
    rating: 4.2,
    reviewCount: 445,
    badge: "İndirim",
    specialPrice: 10.0,
    stockQuantity: 75,
  },
  // Atıştırmalık (categoryId: 5)
  {
    id: 10,
    name: "Ülker Altınbaşak Tahıl Cipsi 50 Gr",
    description: "Taco aromalı & çıtır tahıl cipsi",
    price: 18.0,
    categoryId: 5,
    categoryName: "Atıştırmalık",
    imageUrl: "/images/tahil-cipsi.jpg",
    isNew: false,
    discountPercentage: 17,
    rating: 4.8,
    reviewCount: 256,
    badge: "İndirim",
    specialPrice: 14.9,
    stockQuantity: 35,
  },
  {
    id: 11,
    name: "Mis Bulgur Pilavlık 1Kg",
    description: "Birinci sınıf bulgur 1kg",
    price: 32.9,
    categoryId: 7,
    categoryName: "Temel Gıda",
    imageUrl: "/images/bulgur.png",
    isNew: true,
    discountPercentage: 0,
    rating: 4.7,
    reviewCount: 89,
    stockQuantity: 40,
  },
  // Temizlik (categoryId: 6)
  {
    id: 12,
    name: "Cif Krem Doğanın Gücü Hijyen 675Ml",
    description: "Yüzey temizleyici, çok amaçlı temizlik",
    price: 204.95,
    originalPrice: 229.95,
    categoryId: 6,
    categoryName: "Temizlik",
    imageUrl: "/images/yeşil-cif-krem.jpg",
    isNew: true,
    discountPercentage: 37,
    rating: 4.5,
    reviewCount: 128,
    badge: "İndirim",
    specialPrice: 129.95,
    stockQuantity: 20,
  },
];

export default function ProductGrid({
  products: initialProducts,
  categoryId,
  title = "İlgini Çekebilecek Ürünler", // Blok başlığı - admin panelden ayarlanabilir
  showTitle = true, // Başlık gösterilsin mi
  showViewAll = true, // "Tümünü Gör" butonu gösterilsin mi
  viewAllLink = "/products", // Tümünü gör linki
  // =====================================================
  // BLOK FORMAT DESTEĞİ (poster + ürünler yan yana)
  // posterUrl verilirse blok layout kullanılır
  // =====================================================
  posterUrl = null, // Sol tarafta gösterilecek poster görseli
  posterAlt = "", // Poster için alternatif metin
  layout = "default", // 'default' (sadece ürünler) veya 'block' (poster + ürünler)
} = {}) {
  const navigate = useNavigate();

  // Carousel scroll referansı ve state'leri
  const scrollContainerRef = useRef(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(true);

  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [usingMockData, setUsingMockData] = useState(false);
  const [rules, setRules] = useState([]);
  // Modal state'leri artık ProductDetailModal içinde yönetiliyor
  // eslint-disable-next-line no-unused-vars
  const [modalQuantity, setModalQuantity] = useState(1);
  // eslint-disable-next-line no-unused-vars
  const [modalRule, setModalRule] = useState(null);
  // eslint-disable-next-line no-unused-vars
  const [modalError, setModalError] = useState("");
  const [showLoginRequired, setShowLoginRequired] = useState(false);
  const [loginAction, setLoginAction] = useState(null); // 'cart' or 'favorite'
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [showLoginAlert, setShowLoginAlert] = useState(false);
  const [showFavoriteAlert, setShowFavoriteAlert] = useState(false);
  const [cartNotification, setCartNotification] = useState(null);
  const { user } = useAuth();
  const { addToCart: ctxAddToCart } = useCart();
  const {
    toggleFavorite: ctxToggleFavorite,
    isFavorite: ctxIsFavorite,
    // eslint-disable-next-line no-unused-vars
    favorites,
  } = useFavorites();
  const { toggleCompare, isInCompare } = useCompare();

  // =====================================================
  // CAROUSEL SCROLL KONTROL FONKSİYONLARI
  // Yatay kaydırma için scroll durumunu takip eder
  // =====================================================
  const checkScroll = useCallback(() => {
    const container = scrollContainerRef.current;
    if (container) {
      // Sol ok gösterilsin mi (başta değilse göster)
      setCanScrollLeft(container.scrollLeft > 10);
      // Sağ ok gösterilsin mi (sonda değilse göster)
      setCanScrollRight(
        container.scrollLeft <
          container.scrollWidth - container.clientWidth - 10,
      );
    }
  }, []);

  // Kaydırma fonksiyonu - ok butonları için
  const scroll = useCallback(
    (direction) => {
      const container = scrollContainerRef.current;
      if (container) {
        // Her kaydırmada bir kart genişliği kadar (240px) kaydır
        const scrollAmount = 260;
        container.scrollBy({
          left: direction === "left" ? -scrollAmount : scrollAmount,
          behavior: "smooth",
        });
        // Scroll sonrası durumu güncelle
        setTimeout(checkScroll, 350);
      }
    },
    [checkScroll],
  );

  // Component mount olduğunda ve data değiştiğinde scroll durumunu kontrol et
  useEffect(() => {
    checkScroll();
    // Resize event'inde de kontrol et
    window.addEventListener("resize", checkScroll);
    return () => window.removeEventListener("resize", checkScroll);
  }, [checkScroll, data]);

  // Kategori filtresi (varsa)
  const filteredProducts = useMemo(() => {
    if (!categoryId) return [...data];
    const cid = Number(categoryId);
    return data.filter((p) => Number(p.categoryId) === cid);
  }, [data, categoryId]);

  const handleAddToCart = (productId) => {
    const product = data.find((p) => String(p.id) === String(productId));
    if (!product) return;

    // Context üzerinden sepete ekle (hem misafir hem kullanıcı için çalışır)
    ctxAddToCart(product, 1);
    showCartNotification(product, user ? "registered" : "guest");
  };

  // load rules once
  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const r = await getProductCategoryRules();
        if (mounted) setRules(Array.isArray(r) ? r : []);
      } catch (e) {
        if (mounted) setRules([]);
      }
    })();
    return () => (mounted = false);
  }, []);

  const handleGuestContinue = async () => {
    setShowLoginRequired(false);

    if (loginAction === "cart") {
      const productId = localStorage.getItem("tempProductId");
      // Misafir modundan devam edilirken seçilen miktarı al (varsayılan: 1)
      const savedQty = localStorage.getItem("tempProductQty");
      const quantity = savedQty ? parseFloat(savedQty) : 1;

      if (productId) {
        const product = data.find((p) => p.id === parseInt(productId));
        if (product) {
          // Kullanıcının seçtiği miktarı doğru şekilde sepete ekle
          ctxAddToCart(product, quantity);
          showCartNotification(product, "guest");
        }
        localStorage.removeItem("tempProductId");
        localStorage.removeItem("tempProductQty");
      }
    } else if (loginAction === "favorite") {
      const productId = localStorage.getItem("tempFavoriteProductId");
      if (productId) {
        ctxToggleFavorite(parseInt(productId));
        localStorage.removeItem("tempFavoriteProductId");
      }
    }

    setLoginAction(null);
  };

  const handleShowLogin = () => {
    setShowLoginRequired(false);
    setShowLoginModal(true);
  };

  const handleGoToLogin = () => {
    setShowLoginAlert(false);
    setShowLoginRequired(false);
    setShowLoginModal(true);
  };

  // Favoriler için fonksiyonlar
  const handleAddToFavorites = (productId) => {
    const product = data.find((p) => p.id === productId);
    if (!product) return;

    // Context üzerinden favori toggle (hem misafir hem kullanıcı için çalışır)
    ctxToggleFavorite(productId);
    showFavoriteNotification(product);
  };

  const handleFavoriteGuestContinue = () => {
    setShowFavoriteAlert(false);
  };

  const handleFavoriteGoToLogin = () => {
    setShowFavoriteAlert(false);
    setShowLoginModal(true);
  };

  // isFavorite artık context'ten geliyor
  const isFavorite = (productId) => {
    return ctxIsFavorite(productId);
  };

  const showFavoriteNotification = (product) => {
    // Toast bildirim göster
    setCartNotification({
      type: "favorite",
      product: product,
      message: "Favorilere eklendi!",
    });

    setTimeout(() => {
      setCartNotification(null);
    }, 3000);
  };

  const showCartNotification = (product, userType) => {
    setCartNotification({
      type: "success",
      product: product,
      userType: userType,
    });

    // 4 saniye sonra bildirimi gizle
    setTimeout(() => {
      setCartNotification(null);
    }, 4000);
  };

  const handleProductClick = (product, event) => {
    // Sepete ekle butonuna tıklanmışsa yönlendirme yapma
    if (
      event.target.closest(".modern-add-btn") ||
      event.target.closest(".btn-favorite")
    ) {
      return;
    }

    // Modal yerine ürün detay sayfasına yönlendir
    navigate(`/product/${product.id}`);
  };

  useEffect(() => {
    let isMounted = true;

    const loadProducts = async () => {
      try {
        // Eğer dışarıdan ürün verisi verilmişse API çağrısı yapma
        if (Array.isArray(initialProducts)) {
          if (!isMounted) return;
          setData(initialProducts);
          setError("");
          setUsingMockData(false);
          return;
        }

        // CategoryId varsa getByCategory, yoksa list kullan
        const response = categoryId
          ? await ProductService.getByCategory(categoryId)
          : await ProductService.list();

        if (!isMounted) {
          return;
        }

        const products = Array.isArray(response)
          ? response
          : Array.isArray(response?.items)
            ? response.items
            : [];

        setData(products);
        setError("");
        setUsingMockData(false);
      } catch (err) {
        if (!isMounted) {
          return;
        }

        debugLog("Ürün listesi yüklenemedi, demo verilere düşülüyor", err);

        // JSON Server bağlantısı başarısızsa demo verileri kullan
        setData(DEMO_PRODUCTS);
        setError("");
        setUsingMockData(true);
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    loadProducts();

    // JSON Server'dan ürün değişikliklerini dinle
    const unsubscribe = productServiceMock.subscribe(() => {
      if (isMounted) {
        loadProducts();
      }
    });

    return () => {
      isMounted = false;
      unsubscribe();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [categoryId, initialProducts]);

  if (loading) {
    return (
      <div className="text-center py-5">
        <div
          className="spinner-border mb-3"
          role="status"
          style={{ color: "#ff8f00", width: "3rem", height: "3rem" }}
        >
          <span className="visually-hidden">Loading...</span>
        </div>
        <p className="text-muted fw-bold">Ürünler yükleniyor...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div
        className="alert border-0 shadow-sm text-center"
        style={{
          backgroundColor: "#fff3e0",
          borderRadius: "15px",
          color: "#ef6c00",
        }}
      >
        <i className="fas fa-exclamation-triangle fa-2x mb-3"></i>
        <h5 className="fw-bold mb-2">Bir Hata Oluştu</h5>
        <p className="mb-0">{String(error)}</p>
      </div>
    );
  }

  if (!data.length) {
    return (
      <div className="text-center py-5">
        <div
          className="p-4 rounded-circle mx-auto mb-4 shadow-lg"
          style={{
            backgroundColor: "#fff8f0",
            width: "120px",
            height: "120px",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <i
            className="fas fa-box-open text-warning"
            style={{ fontSize: "3rem" }}
          ></i>
        </div>
        <h4 className="text-warning fw-bold mb-3">Henüz Ürün Yok</h4>
        <p className="text-muted fs-5">
          Yakında harika ürünlerle karşınızda olacağız!
        </p>
      </div>
    );
  }

  return (
    <section
      className={`product-grid-section ${layout === "block" && posterUrl ? "product-grid-block-layout" : ""}`}
    >
      {/* =====================================================
          BAŞLIK VE TÜMÜNÜ GÖR BUTONU
          Admin panelden başlık ayarlanabilir (title prop'u)
          ===================================================== */}
      {showTitle && (
        <div className="product-grid-header">
          <div className="product-grid-title-wrapper">
            <h2 className="product-grid-title">{title}</h2>
            <span className="product-grid-badge">
              <i className="fas fa-fire"></i>
            </span>
          </div>
          {showViewAll && (
            <a href={viewAllLink} className="product-grid-view-all">
              Tümünü Gör
              <i className="fas fa-chevron-right"></i>
            </a>
          )}
        </div>
      )}

      {usingMockData && (
        <div
          className="alert border-0 shadow-sm mb-4 text-center"
          style={{
            backgroundColor: "#fff8e1",
            color: "#fb8c00",
            borderRadius: "15px",
          }}
        >
          <i className="fas fa-info-circle me-2"></i>
          Demo verisi gösteriliyor. Backend bağlantısı kurulduğunda ürünler
          otomatik güncellenecek.
        </div>
      )}

      {/* =====================================================
          BLOK LAYOUT: POSTER + ÜRÜNLER YAN YANA
          posterUrl ve layout="block" verildiğinde aktif olur
          ===================================================== */}
      <div
        className={`product-grid-content-wrapper ${layout === "block" && posterUrl ? "has-poster" : ""}`}
      >
        {/* Sol Taraf - Poster (sadece blok layout'ta) */}
        {layout === "block" && posterUrl && (
          <div className="product-grid-poster">
            <img
              src={posterUrl}
              alt={posterAlt || title || "Poster"}
              className="product-grid-poster-image"
            />
          </div>
        )}

        {/* Sağ Taraf - Ürün Grid/Carousel */}
        <div className="product-grid-products-area">
          {/* Ürün Grid */}
          {filteredProducts.length === 0 ? (
            <div className="text-center py-5">
              <div
                className="p-4 rounded-circle mx-auto mb-4 shadow-lg"
                style={{
                  backgroundColor: "#fff8f0",
                  width: "120px",
                  height: "120px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                <i
                  className="fas fa-search text-warning"
                  style={{ fontSize: "3rem" }}
                ></i>
              </div>
              <h4 className="text-warning fw-bold mb-3">
                Aradığınız Kriterlerde Ürün Bulunamadı
              </h4>
              <p className="text-muted fs-5">
                Lütfen filtreleri değiştirerek tekrar deneyin
              </p>
            </div>
          ) : (
            /* =====================================================
               CAROUSEL YAPISINA DÖNÜŞTÜRÜLDÜ
               - Yatay kaydırmalı (overflow-x: auto)
               - Web'de ok butonlarıyla kaydırma
               - Mobilde swipe ile kaydırma
               ===================================================== */
            <div className="product-grid-carousel-wrapper">
              {/* Sol Ok - Web için */}
              {canScrollLeft && (
                <button
                  className="product-grid-arrow product-grid-arrow-left"
                  onClick={() => scroll("left")}
                  aria-label="Sola kaydır"
                >
                  <i className="fas fa-chevron-left"></i>
                </button>
              )}

              {/* Ürün Carousel Container */}
              <div
                className="product-grid-scroll-container"
                ref={scrollContainerRef}
                onScroll={checkScroll}
              >
                {filteredProducts.map((p, index) => {
                  const stock = p.stock ?? p.stockQuantity ?? 0;
                  const isOutOfStock = stock <= 0;
                  const isLowStock = !isOutOfStock && stock <= 5;

                  // Kampanya/indirim bilgisi hesaplama
                  // Backend'den gelen specialPrice veya originalPrice'ı kontrol et
                  const basePrice = p.originalPrice || p.price || 0;
                  const specialPrice = p.specialPrice;
                  const hasDiscount = specialPrice && specialPrice < basePrice;

                  const currentPrice = hasDiscount
                    ? specialPrice
                    : typeof p.price === "number"
                      ? p.price
                      : 0;
                  const originalPrice = hasDiscount ? basePrice : null;

                  // İndirim yüzdesi: Sadece specialPrice ve price varsa hesapla, eski discountPercentage kullanma
                  const discountPercentage = 0; // Yüzde badge'i devre dışı - yanlış veri gösteriyordu

                  // Kampanya bilgileri
                  const hasCampaign =
                    p.hasActiveCampaign || (p.campaignId && hasDiscount);
                  const campaignName = p.campaignName;

                  return (
                    <div key={p.id} className="product-grid-card-wrapper">
                      <div
                        className="modern-product-card h-100"
                        style={{
                          background: "#ffffff",
                          borderRadius: "16px",
                          border: hasCampaign
                            ? "2px solid rgba(239, 68, 68, 0.3)"
                            : "1px solid rgba(255, 107, 53, 0.1)",
                          overflow: "hidden",
                          position: "relative",
                          cursor: "pointer",
                          minHeight: "320px",
                          width: "100%",
                          display: "flex",
                          flexDirection: "column",
                          boxShadow: hasCampaign
                            ? "0 5px 20px rgba(239, 68, 68, 0.15)"
                            : "0 5px 15px rgba(0, 0, 0, 0.08)",
                          transition: "all 0.3s ease",
                        }}
                        onClick={(e) => handleProductClick(p, e)}
                        onMouseEnter={(e) => {
                          e.currentTarget.style.transform = "translateY(-5px)";
                          e.currentTarget.style.boxShadow = hasCampaign
                            ? "0 15px 30px rgba(239, 68, 68, 0.25)"
                            : "0 15px 30px rgba(255, 107, 53, 0.15)";
                          e.currentTarget.style.borderColor = hasCampaign
                            ? "rgba(239, 68, 68, 0.5)"
                            : "#ff6b35";
                        }}
                        onMouseLeave={(e) => {
                          e.currentTarget.style.transform = "translateY(0)";
                          e.currentTarget.style.boxShadow = hasCampaign
                            ? "0 5px 20px rgba(239, 68, 68, 0.15)"
                            : "0 5px 15px rgba(0, 0, 0, 0.08)";
                          e.currentTarget.style.borderColor = hasCampaign
                            ? "rgba(239, 68, 68, 0.3)"
                            : "rgba(255, 107, 53, 0.1)";
                        }}
                      >
                        {/* Kampanya İndirim Rozeti - Kaldırıldı (yanlış veri gösteriyordu) */}

                        {/* Stok Rozetleri */}
                        {isLowStock && !isOutOfStock && (
                          <div
                            className="position-absolute top-0 end-0 p-2"
                            style={{ zIndex: 3 }}
                          >
                            <span
                              className="badge bg-warning text-dark"
                              style={{
                                fontSize: "0.65rem",
                                padding: "3px 8px",
                                borderRadius: "12px",
                              }}
                            >
                              Az Stok
                            </span>
                          </div>
                        )}

                        {/* Favori ve Karşılaştırma Butonları - Sağ Üst */}
                        <div
                          className="position-absolute top-0 end-0 p-2 d-flex flex-column gap-1"
                          style={{ zIndex: 3 }}
                        >
                          <button
                            className="btn-favorite"
                            type="button"
                            onClick={(e) => {
                              e.stopPropagation(); // Ürün detay modalının açılmasını engelle
                              handleAddToFavorites(p.id);
                            }}
                            style={{
                              background: isFavorite(p.id)
                                ? "linear-gradient(135deg, #ff6b35, #ff8c00)"
                                : "rgba(255, 255, 255, 0.9)",
                              border: "none",
                              borderRadius: "50%",
                              width: "30px",
                              height: "30px",
                              display: "flex",
                              alignItems: "center",
                              justifyContent: "center",
                              color: isFavorite(p.id) ? "white" : "#ff6b35",
                              transition: "all 0.3s ease",
                              backdropFilter: "blur(10px)",
                              boxShadow: isFavorite(p.id)
                                ? "0 4px 15px rgba(255, 107, 53, 0.4)"
                                : "none",
                            }}
                            onMouseEnter={(e) => {
                              if (!isFavorite(p.id)) {
                                e.target.style.transform = "scale(1.1)";
                                e.target.style.background = "#ff6b35";
                                e.target.style.color = "white";
                              }
                            }}
                            onMouseLeave={(e) => {
                              if (!isFavorite(p.id)) {
                                e.target.style.transform = "scale(1)";
                                e.target.style.background =
                                  "rgba(255, 255, 255, 0.9)";
                                e.target.style.color = "#ff6b35";
                              }
                            }}
                          >
                            <i
                              className={
                                isFavorite(p.id)
                                  ? "fas fa-heart"
                                  : "far fa-heart"
                              }
                            ></i>
                          </button>
                          {/* Karşılaştırma Butonu */}
                          <button
                            className="btn-compare"
                            type="button"
                            title="Karşılaştır"
                            onClick={(e) => {
                              e.stopPropagation();
                              const result = toggleCompare(p);
                              if (result.action === "limit") {
                                alert(result.message);
                              }
                            }}
                            style={{
                              background: isInCompare(p.id)
                                ? "linear-gradient(135deg, #17a2b8, #20c997)"
                                : "rgba(255, 255, 255, 0.9)",
                              border: "none",
                              borderRadius: "50%",
                              width: "30px",
                              height: "30px",
                              display: "flex",
                              alignItems: "center",
                              justifyContent: "center",
                              color: isInCompare(p.id) ? "white" : "#17a2b8",
                              transition: "all 0.3s ease",
                              backdropFilter: "blur(10px)",
                              boxShadow: isInCompare(p.id)
                                ? "0 4px 15px rgba(23, 162, 184, 0.4)"
                                : "none",
                              fontSize: "0.75rem",
                            }}
                          >
                            <i className="fas fa-balance-scale"></i>
                          </button>
                        </div>

                        <div
                          className="product-image-container"
                          style={{
                            height: 120,
                            background: "#ffffff",
                            position: "relative",
                            overflow: "hidden",
                          }}
                        >
                          <div
                            className="image-wrapper d-flex align-items-center justify-content-center h-100"
                            style={{
                              transition: "all 0.4s ease",
                              position: "relative",
                            }}
                            onMouseEnter={(e) => {
                              e.currentTarget.style.transform = "scale(1.05)";
                            }}
                            onMouseLeave={(e) => {
                              e.currentTarget.style.transform = "scale(1)";
                            }}
                          >
                            {p.imageUrl ? (
                              <img
                                src={p.imageUrl}
                                alt={p.name}
                                className="product-image"
                                style={{
                                  maxHeight: "100px",
                                  maxWidth: "100px",
                                  objectFit: "contain",
                                  filter:
                                    "drop-shadow(0 4px 12px rgba(0,0,0,0.1))",
                                  transition: "all 0.3s ease",
                                }}
                              />
                            ) : (
                              <div
                                className="d-flex align-items-center justify-content-center"
                                style={{
                                  width: "80px",
                                  height: "80px",
                                  background: "#ffffff",
                                  borderRadius: "12px",
                                  fontSize: "2rem",
                                  border: "1px solid #e9ecef",
                                }}
                              >
                                🛒
                              </div>
                            )}
                          </div>
                        </div>

                        <div
                          className="card-body p-2 d-flex flex-column"
                          style={{
                            background: "#ffffff",
                            minHeight: "160px",
                          }}
                        >
                          {/* Rating */}
                          {p.rating && (
                            <div className="d-flex align-items-center mb-1">
                              <div className="star-rating me-1">
                                {[...Array(5)].map((_, i) => (
                                  <i
                                    key={i}
                                    className={`fas fa-star ${
                                      i < Math.floor(p.rating)
                                        ? "text-warning"
                                        : "text-muted"
                                    }`}
                                    style={{ fontSize: "0.6rem" }}
                                  ></i>
                                ))}
                              </div>
                              <small
                                className="text-muted"
                                style={{ fontSize: "0.65rem" }}
                              >
                                ({p.reviewCount})
                              </small>
                            </div>
                          )}

                          <h6
                            className="product-title mb-1"
                            style={{
                              height: "36px",
                              fontSize: "0.8rem",
                              fontWeight: "600",
                              lineHeight: "1.3",
                              color: "#2c3e50",
                              overflow: "hidden",
                              textOverflow: "ellipsis",
                              display: "-webkit-box",
                              WebkitLineClamp: 2,
                              WebkitBoxOrient: "vertical",
                            }}
                          >
                            {p.name}
                          </h6>

                          {/* Content Area - Flexible */}
                          <div className="content-area flex-grow-1 d-flex flex-column justify-content-between">
                            {/* Kampanya Adı (varsa) */}
                            {campaignName && (
                              <div
                                className="campaign-name-badge mb-2"
                                style={{
                                  background:
                                    "linear-gradient(135deg, #dbeafe, #bfdbfe)",
                                  color: "#1d4ed8",
                                  padding: "4px 8px",
                                  borderRadius: "6px",
                                  fontSize: "0.65rem",
                                  fontWeight: "600",
                                  display: "flex",
                                  alignItems: "center",
                                  gap: "4px",
                                  width: "fit-content",
                                }}
                              >
                                <i
                                  className="fas fa-tag"
                                  style={{ fontSize: "0.55rem" }}
                                ></i>
                                {campaignName}
                              </div>
                            )}

                            {/* Modern Fiyat Bilgileri */}
                            <div
                              className="price-section mb-2"
                              style={{ minHeight: "50px" }}
                            >
                              {hasDiscount && originalPrice ? (
                                <div className="price-container">
                                  {/* Eski Fiyat - Üzeri Çizili */}
                                  <div className="d-flex align-items-center mb-1">
                                    <span
                                      className="old-price me-2"
                                      style={{
                                        fontSize: "0.75rem",
                                        textDecoration: "line-through",
                                        textDecorationColor: "#ef4444",
                                        textDecorationThickness: "2px",
                                        color: "#9ca3af",
                                      }}
                                    >
                                      {originalPrice.toFixed(2)} TL
                                    </span>
                                  </div>
                                  {/* Yeni Fiyat - Turuncu, Bold */}
                                  <div
                                    className="current-price"
                                    style={{
                                      fontSize: "1.1rem",
                                      fontWeight: "700",
                                      color: "#ff6b35",
                                      display: "inline-block",
                                    }}
                                  >
                                    {currentPrice.toFixed(2)} TL
                                  </div>
                                </div>
                              ) : (
                                <div
                                  className="current-price"
                                  style={{
                                    fontSize: "1.1rem",
                                    fontWeight: "700",
                                    color: "#ff6b35",
                                    display: "inline-block",
                                  }}
                                >
                                  {currentPrice.toFixed(2)} TL
                                </div>
                              )}
                            </div>

                            {/* Modern Action Buttons - Always at Bottom */}
                            <div className="action-buttons mt-auto">
                              <button
                                className="modern-add-btn"
                                data-product-id={p.id}
                                style={{
                                  background:
                                    "linear-gradient(135deg, #ff6b35, #ff8c00)",
                                  border: "none",
                                  borderRadius: "10px",
                                  padding: "10px 12px",
                                  fontSize: "0.8rem",
                                  fontWeight: "600",
                                  color: "white",
                                  transition: "all 0.3s ease",
                                  boxShadow:
                                    "0 2px 8px rgba(255, 107, 53, 0.2)",
                                  width: "100%",
                                }}
                                disabled={isOutOfStock}
                                onClick={(e) => {
                                  e.stopPropagation();
                                  if (isOutOfStock) return;
                                  handleAddToCart(p.id);
                                }}
                                onMouseEnter={(e) => {
                                  e.target.style.background =
                                    "linear-gradient(135deg, #e55a25, #e07800)";
                                  e.target.style.transform = "scale(1.02)";
                                  e.target.style.boxShadow =
                                    "0 4px 15px rgba(255, 107, 53, 0.4)";
                                }}
                                onMouseLeave={(e) => {
                                  e.target.style.background =
                                    "linear-gradient(135deg, #ff6b35, #ff8c00)";
                                  e.target.style.transform = "scale(1)";
                                  e.target.style.boxShadow =
                                    "0 2px 8px rgba(255, 107, 53, 0.2)";
                                }}
                              >
                                {isOutOfStock ? "Stokta Yok" : "Sepete Ekle"}
                              </button>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Sağ Ok - Web için */}
              {canScrollRight && (
                <button
                  className="product-grid-arrow product-grid-arrow-right"
                  onClick={() => scroll("right")}
                  aria-label="Sağa kaydır"
                >
                  <i className="fas fa-chevron-right"></i>
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Login Alert Modal */}
      {showLoginAlert && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)", zIndex: 1050 }}
        >
          <div className="modal-dialog modal-dialog-centered">
            <div
              className="modal-content border-0 shadow-lg"
              style={{ borderRadius: "20px" }}
            >
              <div
                className="modal-header border-0"
                style={{
                  borderRadius: "20px 20px 0 0",
                  background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                  color: "white",
                  padding: "2rem",
                }}
              >
                <div className="text-center w-100">
                  <i
                    className="fas fa-user-plus mb-3"
                    style={{ fontSize: "2.5rem" }}
                  ></i>
                  <h4 className="modal-title fw-bold">Hesap Oluşturun</h4>
                </div>
              </div>

              <div
                className="modal-body text-center"
                style={{ padding: "2rem" }}
              >
                <div className="mb-4">
                  <div
                    className="position-relative overflow-hidden border-0 shadow-lg"
                    style={{
                      borderRadius: "20px",
                      background:
                        "linear-gradient(135deg, #FFD700, #FFA500, #FF6347)",
                      padding: "1.5rem",
                      boxShadow: "0 8px 25px rgba(255, 215, 0, 0.4)",
                    }}
                  >
                    <div
                      className="position-absolute top-0 start-0 w-100 h-100"
                      style={{
                        background:
                          'url(\'data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><defs><pattern id="stars" x="0" y="0" width="20" height="20" patternUnits="userSpaceOnUse"><circle cx="10" cy="10" r="1" fill="%23ffffff" opacity="0.3"/></pattern></defs><rect width="100" height="100" fill="url(%23stars)"/></svg>\') repeat',
                        opacity: 0.6,
                      }}
                    ></div>

                    <div className="position-relative text-center text-white">
                      <div className="mb-3">
                        <i
                          className="fas fa-gift fa-3x text-white"
                          style={{
                            filter: "drop-shadow(0 4px 8px rgba(0,0,0,0.3))",
                            animation: "bounce 2s infinite",
                          }}
                        ></i>
                      </div>

                      <h4
                        className="fw-bold mb-2"
                        style={{
                          textShadow: "2px 2px 4px rgba(0,0,0,0.3)",
                          fontSize: "1.4rem",
                        }}
                      >
                        🎉 SÜPER FIRSAT! 🎉
                      </h4>

                      <div
                        className="bg-white text-dark rounded-pill px-3 py-2 mx-auto d-inline-block mb-2"
                        style={{
                          boxShadow: "0 4px 15px rgba(0,0,0,0.2)",
                        }}
                      >
                        <strong style={{ color: "#FF6347" }}>
                          İLK ALIŞVERİŞ
                        </strong>
                      </div>

                      <p
                        className="mb-2 fw-bold"
                        style={{
                          fontSize: "1.1rem",
                          textShadow: "1px 1px 3px rgba(0,0,0,0.3)",
                        }}
                      >
                        Hesap oluştur ve kaydol
                      </p>

                      <div
                        className="bg-success text-white rounded-pill px-4 py-2 mx-auto d-inline-block"
                        style={{
                          fontSize: "1.2rem",
                          fontWeight: "800",
                          boxShadow: "0 4px 15px rgba(40, 167, 69, 0.4)",
                          animation: "pulse 2s infinite",
                        }}
                      >
                        <i className="fas fa-shipping-fast me-2"></i>
                        KARGO BEDAVA! 🚚
                      </div>
                    </div>
                  </div>
                </div>

                <p className="text-muted mb-4">
                  Sepetinize ürün eklemek için hesap oluşturmanız önerilir.
                  Ancak misafir olarak da alışveriş yapabilirsiniz.
                </p>

                <div className="d-grid gap-3">
                  <button
                    type="button"
                    className="btn btn-lg fw-bold text-white border-0"
                    style={{
                      background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      borderRadius: "15px",
                      padding: "15px",
                      fontSize: "1.1rem",
                    }}
                    onClick={handleGoToLogin}
                  >
                    <i className="fas fa-user-plus me-2"></i>
                    Hesap Oluştur & Giriş Yap
                  </button>

                  <button
                    type="button"
                    className="btn btn-outline-secondary btn-lg fw-bold"
                    style={{
                      borderRadius: "15px",
                      padding: "15px",
                      borderWidth: "2px",
                    }}
                    onClick={handleGuestContinue}
                  >
                    <i className="fas fa-shopping-cart me-2"></i>
                    Hesap Oluşturmadan Devam Et
                  </button>
                </div>
              </div>

              <div
                className="modal-footer border-0"
                style={{ padding: "0 2rem 2rem" }}
              >
                <button
                  type="button"
                  className="btn btn-link text-muted mx-auto"
                  onClick={() => setShowLoginAlert(false)}
                >
                  <i className="fas fa-times me-1"></i> Kapat
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Favorite Alert Modal */}
      {showFavoriteAlert && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)", zIndex: 1050 }}
        >
          <div className="modal-dialog modal-dialog-centered">
            <div
              className="modal-content border-0 shadow-lg"
              style={{ borderRadius: "20px" }}
            >
              <div
                className="modal-header border-0"
                style={{
                  borderRadius: "20px 20px 0 0",
                  background: "linear-gradient(135deg, #e91e63, #ad1457)",
                  color: "white",
                  padding: "2rem",
                }}
              >
                <div className="text-center w-100">
                  <i
                    className="fas fa-heart mb-3"
                    style={{ fontSize: "2.5rem" }}
                  ></i>
                  <h4 className="modal-title fw-bold">Favorilere Ekle</h4>
                </div>
              </div>

              <div
                className="modal-body text-center"
                style={{ padding: "2rem" }}
              >
                <div className="mb-4">
                  <div
                    className="alert border-0 shadow-lg p-4"
                    style={{
                      borderRadius: "20px",
                      background: "linear-gradient(135deg, #fff3cd, #ffeaa7)",
                      border: "3px solid #f39c12",
                    }}
                  >
                    <div className="text-center">
                      <div
                        className="rounded-circle mx-auto mb-3 d-flex align-items-center justify-content-center"
                        style={{
                          width: "60px",
                          height: "60px",
                          background:
                            "linear-gradient(135deg, #e91e63, #ad1457)",
                        }}
                      >
                        <i
                          className="fas fa-heart text-white"
                          style={{ fontSize: "1.5rem" }}
                        ></i>
                      </div>
                      <div
                        className="fw-bold text-warning mb-2"
                        style={{ fontSize: "1.1rem" }}
                      >
                        💖 ÖZEL AVANTAJ! 💖
                      </div>
                      <div className="text-dark mb-2">
                        Hesap oluşturup favorilerinizi kaydedin
                      </div>
                      <div
                        className="badge px-3 py-2 fw-bold"
                        style={{
                          backgroundColor: "#e91e63",
                          fontSize: "0.9rem",
                          borderRadius: "25px",
                        }}
                      >
                        💝 Favorileriniz Her Zaman Yanınızda!
                      </div>
                    </div>
                  </div>
                </div>

                <p className="text-muted mb-4">
                  Favorilerinize ürün eklemek için hesap oluşturmanız önerilir.
                  Ancak misafir olarak da favori ürünlerinizi kaydedebilirsiniz.
                </p>

                <div className="d-grid gap-3">
                  <button
                    type="button"
                    className="btn btn-lg fw-bold text-white border-0"
                    style={{
                      background: "linear-gradient(135deg, #e91e63, #ad1457)",
                      borderRadius: "15px",
                      padding: "15px",
                      fontSize: "1.1rem",
                    }}
                    onClick={handleFavoriteGoToLogin}
                  >
                    <i className="fas fa-user-plus me-2"></i>
                    Hesap Oluştur & Giriş Yap
                  </button>

                  <button
                    type="button"
                    className="btn btn-outline-secondary btn-lg fw-bold"
                    style={{
                      borderRadius: "15px",
                      padding: "15px",
                      borderWidth: "2px",
                    }}
                    onClick={handleFavoriteGuestContinue}
                  >
                    <i className="fas fa-heart me-2"></i>
                    Hesap Oluşturmadan Devam Et
                  </button>
                </div>
              </div>

              <div
                className="modal-footer border-0"
                style={{ padding: "0 2rem 2rem" }}
              >
                <button
                  type="button"
                  className="btn btn-link text-muted mx-auto"
                  onClick={() => setShowFavoriteAlert(false)}
                >
                  <i className="fas fa-times me-1"></i> Kapat
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Premium Cart Notification Toast */}
      {cartNotification && (
        <div
          className="position-fixed top-0 end-0 m-4"
          style={{ zIndex: 9999 }}
        >
          <div
            className="toast show border-0 shadow-lg"
            style={{
              borderRadius: "25px",
              minWidth: "380px",
              background:
                cartNotification.type === "success"
                  ? "linear-gradient(145deg, #ffffff, #f8f9fa)"
                  : cartNotification.type === "favorite"
                    ? "linear-gradient(145deg, #fdf2f8, #fce7f3)"
                    : "linear-gradient(145deg, #fff5f5, #fed7d7)",
              animation:
                "slideInRight 0.6s cubic-bezier(0.68, -0.55, 0.265, 1.55)",
              border:
                cartNotification.type === "success"
                  ? "3px solid #10b981"
                  : cartNotification.type === "favorite"
                    ? "3px solid #e91e63"
                    : "3px solid #ef4444",
              boxShadow:
                "0 20px 40px rgba(0,0,0,0.1), 0 0 0 1px rgba(255,255,255,0.1)",
            }}
          >
            <div
              className="toast-header border-0"
              style={{
                background:
                  cartNotification.type === "success"
                    ? "linear-gradient(135deg, #10b981, #059669)"
                    : cartNotification.type === "favorite"
                      ? "linear-gradient(135deg, #e91e63, #ad1457)"
                      : "linear-gradient(135deg, #ef4444, #dc2626)",
                borderRadius: "22px 22px 0 0",
                color: "white",
                padding: "1rem 1.5rem",
              }}
            >
              <div className="d-flex align-items-center">
                <div
                  className="rounded-circle me-3 d-flex align-items-center justify-content-center"
                  style={{
                    width: "40px",
                    height: "40px",
                    backgroundColor: "rgba(255,255,255,0.2)",
                  }}
                >
                  <i
                    className={`fas ${
                      cartNotification.type === "success"
                        ? "fa-check"
                        : cartNotification.type === "favorite"
                          ? "fa-heart"
                          : "fa-exclamation"
                    }`}
                    style={{ fontSize: "1.2rem" }}
                  ></i>
                </div>
                <strong className="fs-6">
                  {cartNotification.type === "success"
                    ? "Sepete Eklendi!"
                    : cartNotification.type === "favorite"
                      ? "Favorilere Eklendi!"
                      : "Hata Oluştu!"}
                </strong>
              </div>
              <button
                type="button"
                className="btn-close btn-close-white ms-auto"
                onClick={() => setCartNotification(null)}
                style={{ opacity: 0.8 }}
              ></button>
            </div>

            {cartNotification.type === "success" &&
              cartNotification.product && (
                <div className="toast-body p-4">
                  <div className="d-flex align-items-start">
                    <div className="position-relative me-3">
                      <img
                        src={
                          cartNotification.product.imageUrl ||
                          "/images/placeholder.png"
                        }
                        alt={cartNotification.product.name}
                        style={{
                          width: "70px",
                          height: "70px",
                          objectFit: "contain",
                          borderRadius: "15px",
                          border: "3px solid #10b981",
                          background:
                            "linear-gradient(135deg, #f8f9fa, #e9ecef)",
                          padding: "5px",
                        }}
                      />
                      <div
                        className="position-absolute top-0 start-100 translate-middle rounded-circle bg-success d-flex align-items-center justify-content-center"
                        style={{ width: "24px", height: "24px" }}
                      >
                        <i
                          className="fas fa-check text-white"
                          style={{ fontSize: "0.7rem" }}
                        ></i>
                      </div>
                    </div>

                    <div className="flex-grow-1">
                      <h6
                        className="mb-1 fw-bold text-dark"
                        style={{ fontSize: "0.95rem" }}
                      >
                        {cartNotification.product.name}
                      </h6>
                      <div className="d-flex align-items-center mb-2">
                        <span className="text-success fw-bold me-2">
                          ₺
                          {(
                            cartNotification.product.specialPrice ||
                            cartNotification.product.price
                          )?.toFixed(2)}
                        </span>
                        {cartNotification.product.specialPrice && (
                          <span className="text-muted text-decoration-line-through small">
                            ₺{cartNotification.product.price?.toFixed(2)}
                          </span>
                        )}
                      </div>

                      {cartNotification.userType === "guest" && (
                        <div
                          className="alert border-0 p-3 mb-3"
                          style={{
                            borderRadius: "15px",
                            background:
                              "linear-gradient(135deg, #fff3cd, #ffeaa7)",
                            border: "2px solid #f39c12",
                          }}
                        >
                          <div className="d-flex align-items-center">
                            <div
                              className="rounded-circle me-2 d-flex align-items-center justify-content-center"
                              style={{
                                width: "30px",
                                height: "30px",
                                backgroundColor: "#f39c12",
                              }}
                            >
                              <i
                                className="fas fa-gift text-white"
                                style={{ fontSize: "0.8rem" }}
                              ></i>
                            </div>
                            <div>
                              <div className="fw-bold text-warning small mb-1">
                                🎁 SÜPER FIRSAT!
                              </div>
                              <div
                                className="text-dark"
                                style={{ fontSize: "0.75rem" }}
                              >
                                Hesap oluştur,{" "}
                                <strong>ilk alışverişinde kargo bedava!</strong>
                              </div>
                            </div>
                          </div>
                        </div>
                      )}

                      <div className="d-grid gap-2">
                        <div className="row g-2">
                          <div className="col-8">
                            <button
                              className="btn w-100 text-white fw-bold"
                              onClick={() => {
                                setCartNotification(null);
                                window.location.href = "/cart";
                              }}
                              style={{
                                borderRadius: "20px",
                                fontSize: "0.8rem",
                                background:
                                  "linear-gradient(135deg, #10b981, #059669)",
                                border: "none",
                                padding: "10px",
                              }}
                            >
                              <i className="fas fa-shopping-cart me-1"></i>
                              Sepete Git
                            </button>
                          </div>

                          {cartNotification.userType === "guest" && (
                            <div className="col-4">
                              <button
                                className="btn btn-outline-warning w-100 fw-bold"
                                onClick={() => {
                                  setCartNotification(null);
                                  setShowLoginAlert(true);
                                }}
                                style={{
                                  borderRadius: "20px",
                                  fontSize: "0.75rem",
                                  borderWidth: "2px",
                                  padding: "10px 8px",
                                }}
                              >
                                <i
                                  className="fas fa-user-plus"
                                  style={{ fontSize: "0.7rem" }}
                                ></i>
                              </button>
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}

            {cartNotification.type === "error" && (
              <div className="toast-body">
                <p className="mb-0 text-danger fw-bold">
                  {cartNotification.message}
                </p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Login Required Modal */}
      <LoginRequiredModal
        show={showLoginRequired}
        action={loginAction}
        onHide={() => {
          setShowLoginRequired(false);
          setLoginAction(null);
          localStorage.removeItem("tempProductId");
          localStorage.removeItem("tempFavoriteProductId");
        }}
        onGuestContinue={handleGuestContinue}
        onLogin={handleShowLogin}
      />

      {/* Login Modal */}
      <LoginModal
        show={showLoginModal}
        onHide={() => setShowLoginModal(false)}
        onLoginSuccess={() => {
          setShowLoginModal(false);
          // Giriş başarılı olunca işlemi tamamla
          if (loginAction === "cart") {
            const productId = localStorage.getItem("tempProductId");
            if (productId) {
              handleAddToCart(parseInt(productId));
              localStorage.removeItem("tempProductId");
            }
          } else if (loginAction === "favorite") {
            const productId = localStorage.getItem("tempFavoriteProductId");
            if (productId) {
              handleAddToFavorites(parseInt(productId));
              localStorage.removeItem("tempFavoriteProductId");
            }
          }
          setLoginAction(null);
        }}
      />
    </section>
  );
}
