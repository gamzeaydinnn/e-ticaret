import React, { useEffect, useState, useMemo } from "react";
import { ProductService } from "../services/productService";
import { CartService } from "../services/cartService";
import { FavoriteService } from "../services/favoriteService";
import { useAuth } from "../contexts/AuthContext";
import LoginModal from "./LoginModal";
import LoginRequiredModal from "./LoginRequiredModal";
import { shouldUseMockData, debugLog } from "../config/apiConfig";
import getProductCategoryRules from "../config/productCategoryRules";

const DEMO_PRODUCTS = [
  {
    id: 1,
    name: "Cif Krem Doğanın Gücü Hijyen 675Ml",
    description: "Yüzey temizleyici, çok amaçlı temizlik",
    price: 204.95,
    originalPrice: 229.95,
    categoryId: 7,
    categoryName: "Temizlik",
    imageUrl: "/images/yeşil-cif-krem.jpg",
    isNew: true,
    discountPercentage: 11,
    rating: 4.5,
    reviewCount: 128,
    badge: "İndirim",
    specialPrice: 129.95,
  },
  {
    id: 2,
    name: "Ülker Altınbaşak Tahıl Cipsi 50 Gr",
    description: "Taco aromalı & çıtır tahıl cipsi",
    price: 18.0,
    categoryId: 6,
    categoryName: "Atıştırmalık",
    imageUrl: "/images/tahil-cipsi.jpg",
    isNew: false,
    discountPercentage: 17,
    rating: 4.8,
    reviewCount: 256,
    badge: "İndirim",
    specialPrice: 14.9,
  },
  {
    id: 3,
    name: "Lipton Ice Tea Limon 330 Ml",
    description: "Soğuk çay, kutu 330ml",
    price: 60.0,
    categoryId: 5,
    categoryName: "İçecekler",
    imageUrl: "/images/lipton-ice-tea.jpg",
    isNew: false,
    discountPercentage: 32,
    rating: 4.2,
    reviewCount: 89,
    badge: "İndirim",
    specialPrice: 40.9,
  },
  {
    id: 4,
    name: "Dana But Tas Kebaplık Et Çiftlik Kg",
    description: "Taze dana eti, kuşbaşı doğranmış 500g",
    price: 375.95,
    originalPrice: 429.95,
    categoryId: 2,
    categoryName: "Et & Tavuk & Balık",
    imageUrl: "/images/dana-kusbasi.jpg",
    isNew: true,
    discountPercentage: 26,
    rating: 4.7,
    reviewCount: 67,
    badge: "İndirim",
    specialPrice: 279.0,
  },
  {
    id: 5,
    name: "Kuzu İncik Kg",
    description: "Taze kuzu incik, kilogram",
    price: 1399.95,
    categoryId: 2,
    categoryName: "Et & Tavuk & Balık",
    imageUrl: "/images/kuzu-incik.webp",
    isNew: false,
    discountPercentage: 0,
    rating: 4.4,
    reviewCount: 195,
    badge: "İyi Fiyat",
    specialPrice: 699.95,
  },
  {
    id: 6,
    name: "Nescafe 2si 1 Arada Sütlü Köpüklü 15 x 10g",
    description: "Kahve karışımı, paket 15 x 10g",
    price: 145.55,
    originalPrice: 169.99,
    categoryId: 5,
    categoryName: "İçecekler",
    imageUrl: "/images/nescafe.jpg",
    isNew: false,
    discountPercentage: 14,
    rating: 4.3,
    reviewCount: 143,
    badge: "İndirim",
    specialPrice: 84.5,
  },
  {
    id: 7,
    name: "Domates Kg",
    description: "Taze domates, kilogram",
    price: 45.9,
    categoryId: 1,
    categoryName: "Meyve & Sebze",
    imageUrl: "/images/domates.webp",
    isNew: true,
    discountPercentage: 0,
    rating: 4.9,
    reviewCount: 312,
  },
  {
    id: 8,
    name: "Pınar Süt 1L",
    description: "Tam yağlı UHT süt 1 litre",
    price: 28.5,
    categoryId: 3,
    categoryName: "Süt Ürünleri",
    imageUrl: "/images/pınar-süt.jpg",
    isNew: false,
    discountPercentage: 0,
    rating: 4.6,
    reviewCount: 234,
  },
  {
    id: 9,
    name: "Sek Kaşar Peyniri 200 G",
    description: "Dilimli kaşar peyniri 200g",
    price: 75.9,
    categoryId: 3,
    categoryName: "Süt Ürünleri",
    imageUrl: "/images/sek-kasar-peyniri-200-gr-38be46-1650x1650.jpg",
    isNew: false,
    discountPercentage: 15,
    rating: 4.4,
    reviewCount: 156,
    badge: "İndirim",
    specialPrice: 64.5,
  },
  {
    id: 10,
    name: "Mis Bulgur Pilavlık 1Kg",
    description: "Birinci sınıf bulgur 1kg",
    price: 32.9,
    categoryId: 4,
    categoryName: "Temel Gıda",
    imageUrl: "/images/bulgur.png",
    isNew: true,
    discountPercentage: 0,
    rating: 4.7,
    reviewCount: 89,
  },
  {
    id: 11,
    name: "Coca-Cola Orijinal Tat Kutu 330ml",
    description: "Kola gazlı içecek kutu",
    price: 12.5,
    categoryId: 5,
    categoryName: "İçecekler",
    imageUrl: "/images/coca-cola.jpg",
    isNew: false,
    discountPercentage: 20,
    rating: 4.2,
    reviewCount: 445,
    badge: "İndirim",
    specialPrice: 10.0,
  },
  {
    id: 12,
    name: "Salatalık Kg",
    description: "Taze salatalık, kilogram",
    price: 28.9,
    categoryId: 1,
    categoryName: "Meyve & Sebze",
    imageUrl: "/images/salatalik.jpg",
    isNew: false,
    discountPercentage: 0,
    rating: 4.3,
    reviewCount: 67,
  },
];

export default function ProductGrid({
  products: initialProducts,
  categoryId,
} = {}) {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [usingMockData, setUsingMockData] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [rules, setRules] = useState([]);
  const [modalQuantity, setModalQuantity] = useState(1);
  const [modalRule, setModalRule] = useState(null);
  const [modalError, setModalError] = useState("");
  const [showLoginRequired, setShowLoginRequired] = useState(false);
  const [loginAction, setLoginAction] = useState(null); // 'cart' or 'favorite'
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [showLoginAlert, setShowLoginAlert] = useState(false);
  const [showFavoriteAlert, setShowFavoriteAlert] = useState(false);
  const [cartNotification, setCartNotification] = useState(null);
  const [favorites, setFavorites] = useState([]);
  const { user } = useAuth();

  // Kategori filtresi (varsa)
  const filteredProducts = useMemo(() => {
    if (!categoryId) return [...data];
    const cid = Number(categoryId);
    return data.filter((p) => Number(p.categoryId) === cid);
  }, [data, categoryId]);

  const handleAddToCart = async (productId) => {
    // Kullanıcı giriş yapmış mı kontrol et
    if (!user) {
      // Login gerekli modalını göster
      setLoginAction("cart");
      setShowLoginRequired(true);
      localStorage.setItem("tempProductId", productId);
      return;
    }
    // Example usage (developer-only, not rendered):
    // const rules = getProductCategoryRules().slice(0,2);
    // Giriş yapmışsa backend API'ye sepete ekle
    try {
      await CartService.addItem(productId, 1);
      const product = data.find((p) => p.id === productId);
      showCartNotification(product, "registered");
    } catch (error) {
      console.error("Sepete ekleme hatası:", error);
      // Hata durumunda localStorage'e ekle
      const product = data.find((p) => p.id === productId);
      CartService.addToGuestCart(parseInt(productId), 1);
      showCartNotification(product, "guest");
    }
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
      if (productId) {
        CartService.addToGuestCart(parseInt(productId), 1);
        const product = data.find((p) => p.id === parseInt(productId));
        showCartNotification(product, "guest");
        localStorage.removeItem("tempProductId");
      }
    } else if (loginAction === "favorite") {
      const productId = localStorage.getItem("tempFavoriteProductId");
      if (productId) {
        addToGuestFavorites(parseInt(productId));
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
  const handleAddToFavorites = async (productId) => {
    if (!user) {
      // Login gerekli modalını göster
      setLoginAction("favorite");
      setShowLoginRequired(true);
      localStorage.setItem("tempFavoriteProductId", productId);
      return;
    }

    // Giriş yapmışsa backend API'ye favorilere ekle
    try {
      const result = await FavoriteService.toggleFavorite(productId);

      // Favoriler listesini güncelle
      await loadFavorites();

      // Ürün bilgisini bul ve bildirim göster
      const product = data.find((p) => p.id === productId);
      if (product) {
        showFavoriteNotification(product);
      }

      console.log("Favori işlemi başarılı:", result);
    } catch (error) {
      console.error("Favorilere ekleme hatası:", error);

      // Hata durumunda localStorage'e ekle ve bildirim göster
      const product = data.find((p) => p.id === productId);
      if (product) {
        addToGuestFavorites(productId);
        showFavoriteNotification(product);
      }
    }
  };

  const handleFavoriteGuestContinue = () => {
    setShowFavoriteAlert(false);
    const productId = localStorage.getItem("tempFavoriteProductId");

    if (productId) {
      addToGuestFavorites(parseInt(productId));
      localStorage.removeItem("tempFavoriteProductId");
    }
  };

  const handleFavoriteGoToLogin = () => {
    setShowFavoriteAlert(false);
    window.location.href = "/cart";
  };

  const addToGuestFavorites = (productId) => {
    const guestFavorites = JSON.parse(
      localStorage.getItem("guestFavorites") || "[]"
    );
    if (!guestFavorites.includes(productId)) {
      guestFavorites.push(productId);
      localStorage.setItem("guestFavorites", JSON.stringify(guestFavorites));
      setFavorites(guestFavorites);

      // Başarı bildirimi göster
      const product = data.find((p) => p.id === productId);
      showFavoriteNotification(product);
    }
  };

  const loadFavorites = async () => {
    if (user) {
      try {
        const userFavorites = await FavoriteService.getFavorites();
        // Backend'den gelen favori listesini işle
        if (Array.isArray(userFavorites)) {
          // Eğer backend favori objeleri dönüyorsa ID'leri çıkar
          const favoriteIds = userFavorites.map((f) =>
            typeof f === "object" ? f.id : f
          );
          setFavorites(favoriteIds);
        } else {
          setFavorites([]);
        }
      } catch (error) {
        console.error("Favoriler yüklenirken hata:", error);
        // Hata durumunda localStorage'dan yükle
        const guestFavorites = JSON.parse(
          localStorage.getItem("guestFavorites") || "[]"
        );
        setFavorites(guestFavorites);
      }
    } else {
      // Misafir kullanıcı için localStorage'dan yükle
      const guestFavorites = JSON.parse(
        localStorage.getItem("guestFavorites") || "[]"
      );
      setFavorites(guestFavorites);
    }
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

  const isFavorite = (productId) => {
    return favorites.includes(productId);
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
    // Sepete ekle butonuna tıklanmışsa modal açma
    if (
      event.target.closest(".modern-add-btn") ||
      event.target.closest(".btn-favorite")
    ) {
      return;
    }

    setSelectedProduct(product);
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setSelectedProduct(null);
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
          await loadFavorites();
          return;
        }

        const query = categoryId
          ? `?categoryId=${encodeURIComponent(categoryId)}`
          : "";
        const response = await ProductService.list(query);
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
        await loadFavorites();
      } catch (err) {
        if (!isMounted) {
          return;
        }

        debugLog("Ürün listesi yüklenemedi, fallback değerlendiriliyor", err);

        if (shouldUseMockData()) {
          setData(DEMO_PRODUCTS);
          setError("");
          setUsingMockData(true);
          await loadFavorites();
        } else {
          setError(
            err?.message ||
              "Ürünler yüklenemedi. Lütfen daha sonra tekrar deneyin."
          );
          setData([]);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    loadProducts();

    return () => {
      isMounted = false;
    };
  }, [categoryId, initialProducts]);

  // Kullanıcı giriş/çıkış yapınca favorileri yükle
  useEffect(() => {
    if (!loading) {
      loadFavorites();
    }
  }, [user, loading]);

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
    <div>
      {/* Sonuç Bilgisi */}
      <div className="text-center mb-4">
        <p className="text-muted mb-0">
          <strong>{filteredProducts.length}</strong> ürün listeleniyor
        </p>
      </div>

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
        <div className="row">
          {filteredProducts.map((p, index) => (
            <div key={p.id} className="col-lg-3 col-md-6 mb-4">
              <div
                className="modern-product-card h-100"
                style={{
                  background: "#ffffff",
                  borderRadius: "20px",
                  border: "1px solid rgba(255, 107, 53, 0.1)",
                  overflow: "hidden",
                  position: "relative",
                  animation: `fadeInUp 0.6s ease ${index * 0.1}s both`,
                  cursor: "pointer",
                  minHeight: "520px",
                  display: "flex",
                  flexDirection: "column",
                  boxShadow: "0 5px 15px rgba(0, 0, 0, 0.08)",
                }}
                onClick={(e) => handleProductClick(p, e)}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform =
                    "translateY(-8px) scale(1.02)";
                  e.currentTarget.style.boxShadow =
                    "0 20px 40px rgba(255, 107, 53, 0.15)";
                  e.currentTarget.style.borderColor = "rgba(255, 107, 53, 0.3)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = "translateY(0) scale(1)";
                  e.currentTarget.style.boxShadow =
                    "0 5px 15px rgba(0, 0, 0, 0.08)";
                  e.currentTarget.style.borderColor = "rgba(255, 107, 53, 0.1)";
                }}
              >
                {/* Badge - Sol Üst */}
                {p.badge && (
                  <div
                    className="position-absolute top-0 start-0 p-2"
                    style={{ zIndex: 3 }}
                  >
                    <span
                      className="badge-modern"
                      style={{
                        background:
                          p.badge === "İndirim"
                            ? "linear-gradient(135deg, #ff6b35, #ff8c00)"
                            : p.badge === "Yeni"
                            ? "linear-gradient(135deg, #28a745, #20c997)"
                            : "linear-gradient(135deg, #ffc107, #fd7e14)",
                        color: "white",
                        padding: "4px 12px",
                        borderRadius: "15px",
                        fontSize: "0.7rem",
                        fontWeight: "700",
                        textTransform: "uppercase",
                        boxShadow: "0 2px 8px rgba(255, 107, 53, 0.3)",
                        animation: "pulse 2s infinite",
                      }}
                    >
                      {p.badge}
                    </span>
                  </div>
                )}

                {/* Favori Butonu - Sağ Üst */}
                <div
                  className="position-absolute top-0 end-0 p-2"
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
                      width: "35px",
                      height: "35px",
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
                        e.target.style.background = "rgba(255, 255, 255, 0.9)";
                        e.target.style.color = "#ff6b35";
                      }
                    }}
                  >
                    <i
                      className={
                        isFavorite(p.id) ? "fas fa-heart" : "far fa-heart"
                      }
                    ></i>
                  </button>
                </div>

                <div
                  className="product-image-container"
                  style={{
                    height: 200,
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
                      e.currentTarget.style.transform = "scale(1.1)";
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
                          maxHeight: "160px",
                          maxWidth: "160px",
                          objectFit: "contain",
                          filter: "drop-shadow(0 4px 12px rgba(0,0,0,0.1))",
                          transition: "all 0.3s ease",
                        }}
                      />
                    ) : (
                      <div
                        className="d-flex align-items-center justify-content-center"
                        style={{
                          width: "120px",
                          height: "120px",
                          background: "#ffffff",
                          borderRadius: "15px",
                          fontSize: "2.5rem",
                          border: "1px solid #e9ecef",
                        }}
                      >
                        🛒
                      </div>
                    )}
                  </div>
                </div>

                <div
                  className="card-body p-4 d-flex flex-column"
                  style={{
                    background: "#ffffff",
                    minHeight: "280px",
                  }}
                >
                  {/* Rating */}
                  {p.rating && (
                    <div className="d-flex align-items-center mb-2">
                      <div className="star-rating me-2">
                        {[...Array(5)].map((_, i) => (
                          <i
                            key={i}
                            className={`fas fa-star ${
                              i < Math.floor(p.rating)
                                ? "text-warning"
                                : "text-muted"
                            }`}
                            style={{ fontSize: "0.7rem" }}
                          ></i>
                        ))}
                      </div>
                      <small className="text-muted">({p.reviewCount})</small>
                    </div>
                  )}

                  <h6
                    className="product-title mb-3"
                    style={{
                      height: "50px",
                      fontSize: "0.95rem",
                      fontWeight: "600",
                      lineHeight: "1.4",
                      color: "#2c3e50",
                      display: "flex",
                      alignItems: "center",
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                    }}
                  >
                    {p.name}
                  </h6>

                  {/* Content Area - Flexible */}
                  <div className="content-area flex-grow-1 d-flex flex-column justify-content-between">
                    {/* Modern Fiyat Bilgileri */}
                    <div className="price-section mb-3">
                      {p.originalPrice ? (
                        <div className="price-container">
                          <div className="d-flex align-items-center mb-2">
                            <span
                              className="old-price me-2"
                              style={{
                                fontSize: "0.85rem",
                                textDecoration: "line-through",
                                color: "#6c757d",
                              }}
                            >
                              {p.originalPrice.toFixed(2)} TL
                            </span>
                            {p.discountPercentage > 0 && (
                              <span
                                className="discount-badge"
                                style={{
                                  background:
                                    "linear-gradient(135deg, #dc3545, #c82333)",
                                  color: "white",
                                  padding: "3px 8px",
                                  borderRadius: "12px",
                                  fontSize: "0.7rem",
                                  fontWeight: "700",
                                  animation: "bounce 2s infinite",
                                }}
                              >
                                -%{p.discountPercentage}
                              </span>
                            )}
                          </div>
                          <div
                            className="current-price"
                            style={{
                              fontSize: "1.4rem",
                              fontWeight: "800",
                              background:
                                "linear-gradient(135deg, #ff6b35, #ff8c00)",
                              backgroundClip: "text",
                              WebkitBackgroundClip: "text",
                              WebkitTextFillColor: "transparent",
                              display: "inline-block",
                              textShadow: "0 2px 4px rgba(255,107,53,0.3)",
                            }}
                          >
                            {p.price.toFixed(2)} TL
                          </div>
                        </div>
                      ) : (
                        <div
                          className="current-price"
                          style={{
                            fontSize: "1.4rem",
                            fontWeight: "800",
                            background:
                              "linear-gradient(135deg, #28a745, #20c997)",
                            backgroundClip: "text",
                            WebkitBackgroundClip: "text",
                            WebkitTextFillColor: "transparent",
                            display: "inline-block",
                          }}
                        >
                          {p.price.toFixed(2)} TL
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
                          borderRadius: "25px",
                          padding: "12px 24px",
                          fontSize: "0.9rem",
                          fontWeight: "700",
                          color: "white",
                          transition: "all 0.4s cubic-bezier(0.4, 0, 0.2, 1)",
                          boxShadow: "0 4px 15px rgba(255, 107, 53, 0.3)",
                          position: "relative",
                          overflow: "hidden",
                          width: "100%",
                          textTransform: "uppercase",
                          letterSpacing: "0.5px",
                        }}
                        onClick={(e) => {
                          e.stopPropagation();
                          // open modal to choose quantity
                          setSelectedProduct(p);
                          // find rule for this product
                          let match =
                            (rules || []).find((r) => {
                              const examples = (r.examples || []).map((ex) =>
                                String(ex).toLowerCase()
                              );
                              const pname = (p.name || "").toLowerCase();
                              return (
                                (r.category || "")
                                  .toLowerCase()
                                  .includes(pname) ||
                                examples.some(
                                  (ex) =>
                                    pname.includes(ex) || ex.includes(pname)
                                ) ||
                                (p.categoryName || "")
                                  .toLowerCase()
                                  .includes((r.category || "").toLowerCase())
                              );
                            }) || null;
                          // prefer kg rules for fruits/vegetables and meat categories
                          const pcat = (p.categoryName || "").toLowerCase();
                          if (
                            !match &&
                            (pcat.includes("meyve") ||
                              pcat.includes("sebze") ||
                              pcat.includes("et") ||
                              pcat.includes("tavuk") ||
                              pcat.includes("balık") ||
                              pcat.includes("balik"))
                          ) {
                            match =
                              (rules || []).find(
                                (r) => (r.unit || "").toLowerCase() === "kg"
                              ) || null;
                          }
                          // categories that should be sold as units with min 1 max 10
                          const unitLimitCats = [
                            "süt",
                            "süt ürünleri",
                            "süt urunleri",
                            "temel gıda",
                            "temel gida",
                            "temizlik",
                            "içecek",
                            "icecek",
                            "atıştırmalık",
                            "atistirmalik",
                          ];
                          if (
                            !match &&
                            unitLimitCats.some((tok) => pcat.includes(tok))
                          ) {
                            match = {
                              category: "Kategori adedi sınırı",
                              unit: "adet",
                              min_quantity: 1,
                              max_quantity: 10,
                              step: 1,
                            };
                          }
                          setModalRule(match);
                          setModalQuantity(match ? match.min_quantity || 1 : 1);
                          setModalError("");
                          setShowModal(true);
                        }}
                        onMouseEnter={(e) => {
                          e.target.style.background =
                            "linear-gradient(135deg, #ff8c00, #ffa500)";
                          e.target.style.transform =
                            "translateY(-2px) scale(1.02)";
                          e.target.style.boxShadow =
                            "0 8px 25px rgba(255, 107, 53, 0.4)";
                        }}
                        onMouseLeave={(e) => {
                          e.target.style.background =
                            "linear-gradient(135deg, #ff6b35, #ff8c00)";
                          e.target.style.transform = "translateY(0) scale(1)";
                          e.target.style.boxShadow =
                            "0 4px 15px rgba(255, 107, 53, 0.3)";
                        }}
                      >
                        <i className="fas fa-shopping-cart me-2"></i>
                        Sepete Ekle
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Product Detail Modal */}
      {showModal && selectedProduct && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
          onClick={(e) => e.target === e.currentTarget && closeModal()}
        >
          <div className="modal-dialog modal-lg modal-dialog-centered">
            <div
              className="modal-content border-0 shadow-lg"
              style={{ borderRadius: "20px" }}
            >
              <div className="modal-header border-0 p-0">
                <div
                  className="w-100 d-flex justify-content-between align-items-center p-3"
                  style={{
                    background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                    borderRadius: "20px 20px 0 0",
                  }}
                >
                  <h5 className="modal-title text-white fw-bold mb-0">
                    <i className="fas fa-info-circle me-2"></i>
                    Ürün Detayları
                  </h5>
                  <button
                    type="button"
                    className="btn btn-link text-white p-0 border-0"
                    onClick={closeModal}
                    style={{
                      fontSize: "1.5rem",
                      textDecoration: "none",
                      opacity: 0.9,
                      transition: "all 0.3s ease",
                    }}
                    onMouseEnter={(e) => {
                      e.target.style.opacity = "1";
                      e.target.style.transform = "scale(1.1)";
                    }}
                    onMouseLeave={(e) => {
                      e.target.style.opacity = "0.9";
                      e.target.style.transform = "scale(1)";
                    }}
                  >
                    <i className="fas fa-times"></i>
                  </button>
                </div>
              </div>

              <div className="modal-body px-4 pb-4">
                <div className="row align-items-center">
                  <div className="col-md-5 text-center">
                    <div
                      className="product-image-large mb-3"
                      style={{
                        background: "linear-gradient(135deg, #f8f9fa, #e9ecef)",
                        borderRadius: "20px",
                        padding: "30px",
                        height: "300px",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                      }}
                    >
                      {selectedProduct.imageUrl ? (
                        <img
                          src={selectedProduct.imageUrl}
                          alt={selectedProduct.name}
                          style={{
                            maxHeight: "240px",
                            maxWidth: "240px",
                            objectFit: "contain",
                            filter: "drop-shadow(0 8px 20px rgba(0,0,0,0.1))",
                          }}
                        />
                      ) : (
                        <div style={{ fontSize: "4rem", color: "#dee2e6" }}>
                          🛒
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="col-md-7">
                    <div className="product-details">
                      {selectedProduct.badge && (
                        <span
                          className="badge mb-3"
                          style={{
                            background:
                              selectedProduct.badge === "İndirim"
                                ? "linear-gradient(135deg, #ff6b35, #ff8c00)"
                                : "linear-gradient(135deg, #28a745, #20c997)",
                            color: "white",
                            padding: "6px 15px",
                            borderRadius: "20px",
                            fontSize: "0.8rem",
                            fontWeight: "700",
                          }}
                        >
                          {selectedProduct.badge}
                        </span>
                      )}

                      <h3
                        className="product-title mb-3"
                        style={{
                          fontWeight: "700",
                          color: "#2c3e50",
                          lineHeight: "1.3",
                        }}
                      >
                        {selectedProduct.name}
                      </h3>

                      {selectedProduct.rating && (
                        <div className="rating mb-3 d-flex align-items-center">
                          <div className="stars me-2">
                            {[...Array(5)].map((_, i) => (
                              <i
                                key={i}
                                className={`fas fa-star ${
                                  i < Math.floor(selectedProduct.rating)
                                    ? "text-warning"
                                    : "text-muted"
                                }`}
                              ></i>
                            ))}
                          </div>
                          <span className="text-muted">
                            ({selectedProduct.reviewCount || 0} değerlendirme)
                          </span>
                        </div>
                      )}

                      <p
                        className="product-description mb-4 text-muted"
                        style={{
                          fontSize: "1.1rem",
                          lineHeight: "1.6",
                        }}
                      >
                        {selectedProduct.description ||
                          "Bu ürün hakkında detaylı bilgi için müşteri hizmetleri ile iletişime geçebilirsiniz."}
                      </p>

                      <div className="price-info mb-4">
                        {selectedProduct.originalPrice ? (
                          <div>
                            <span
                              className="old-price me-3"
                              style={{
                                fontSize: "1.1rem",
                                textDecoration: "line-through",
                                color: "#6c757d",
                              }}
                            >
                              {selectedProduct.originalPrice.toFixed(2)} TL
                            </span>
                            <span
                              className="current-price"
                              style={{
                                fontSize: "2rem",
                                fontWeight: "800",
                                background:
                                  "linear-gradient(135deg, #ff6b35, #ff8c00)",
                                backgroundClip: "text",
                                WebkitBackgroundClip: "text",
                                WebkitTextFillColor: "transparent",
                              }}
                            >
                              {selectedProduct.price.toFixed(2)} TL
                            </span>
                          </div>
                        ) : (
                          <span
                            className="current-price"
                            style={{
                              fontSize: "2rem",
                              fontWeight: "800",
                              background:
                                "linear-gradient(135deg, #28a745, #20c997)",
                              backgroundClip: "text",
                              WebkitBackgroundClip: "text",
                              WebkitTextFillColor: "transparent",
                            }}
                          >
                            {selectedProduct.price.toFixed(2)} TL
                          </span>
                        )}
                      </div>

                      <div className="product-actions d-flex justify-content-center flex-column align-items-center">
                        <div className="d-flex align-items-center gap-2 mb-3">
                          <input
                            type="number"
                            className="form-control"
                            style={{ width: 120 }}
                            value={modalQuantity}
                            step={
                              modalRule
                                ? modalRule.step ??
                                  (modalRule.unit === "kg" ? 0.25 : 1)
                                : 1
                            }
                            onChange={(e) => setModalQuantity(e.target.value)}
                          />
                          <div className="text-muted">
                            {modalRule
                              ? modalRule.unit
                              : selectedProduct.unit || "adet"}
                          </div>
                        </div>

                        {modalRule && (
                          <div className="text-muted small mb-2">
                            Min: {modalRule.min_quantity} — Max:{" "}
                            {modalRule.max_quantity} — Adım:{" "}
                            {modalRule.step ??
                              (modalRule.unit === "kg" ? 0.25 : 1)}{" "}
                            {modalRule.unit}
                          </div>
                        )}

                        {modalError && (
                          <div className="text-danger small mb-2">
                            {modalError}
                          </div>
                        )}

                        <button
                          className="btn btn-lg w-75"
                          style={{
                            background:
                              "linear-gradient(135deg, #ff6b35, #ff8c00)",
                            border: "none",
                            borderRadius: "25px",
                            padding: "15px 40px",
                            color: "white",
                            fontWeight: "700",
                            fontSize: "1.1rem",
                            boxShadow: "0 4px 15px rgba(255, 107, 53, 0.3)",
                            transition: "all 0.3s ease",
                            textTransform: "uppercase",
                            letterSpacing: "0.5px",
                          }}
                          onClick={async () => {
                            setModalError("");
                            const q = parseFloat(modalQuantity);
                            if (isNaN(q) || q <= 0)
                              return setModalError("Geçerli miktar girin.");
                            if (modalRule) {
                              const min = parseFloat(
                                modalRule.min_quantity ?? -Infinity
                              );
                              const max = parseFloat(
                                modalRule.max_quantity ?? Infinity
                              );
                              const step = parseFloat(
                                modalRule.step ??
                                  (modalRule.unit === "kg" ? 0.25 : 1)
                              );
                              if (q < min)
                                return setModalError(
                                  `Minimum ${min} ${modalRule.unit} olmalıdır.`
                                );
                              if (q > max)
                                return setModalError(
                                  `Maksimum ${max} ${modalRule.unit} ile sınırlıdır.`
                                );
                              const remainder = Math.abs(
                                (q - min) / step - Math.round((q - min) / step)
                              );
                              if (remainder > 1e-6)
                                return setModalError(
                                  `Miktar ${step} ${modalRule.unit} adımlarıyla olmalıdır.`
                                );
                            } else {
                              if (!selectedProduct.isWeighted && q > 5)
                                return setModalError(
                                  "Bu üründen en fazla 5 adet ekleyebilirsiniz."
                                );
                            }

                            try {
                              if (!user) {
                                // ask user to login or continue guest
                                setLoginAction("cart");
                                setShowLoginRequired(true);
                                localStorage.setItem(
                                  "tempProductId",
                                  selectedProduct.id
                                );
                                localStorage.setItem("tempProductQty", q);
                                return;
                              }
                              await CartService.addItem(selectedProduct.id, q);
                              showCartNotification(
                                selectedProduct,
                                "registered"
                              );
                            } catch (err) {
                              console.error("Sepete ekleme hatası:", err);
                              CartService.addToGuestCart(selectedProduct.id, q);
                              showCartNotification(selectedProduct, "guest");
                            }

                            closeModal();
                          }}
                          onMouseEnter={(e) => {
                            e.target.style.transform =
                              "translateY(-3px) scale(1.05)";
                            e.target.style.boxShadow =
                              "0 8px 25px rgba(255, 107, 53, 0.4)";
                          }}
                          onMouseLeave={(e) => {
                            e.target.style.transform = "translateY(0) scale(1)";
                            e.target.style.boxShadow =
                              "0 4px 15px rgba(255, 107, 53, 0.3)";
                          }}
                        >
                          <i className="fas fa-shopping-cart me-2"></i>
                          Sepete Ekle
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

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
                                fontSize: "0.85rem",
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
    </div>
  );
}
