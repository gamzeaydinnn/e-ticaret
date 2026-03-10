import React, { useState, useRef } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useCart } from "../../contexts/CartContext";
import { useFavorites } from "../../contexts/FavoriteContext";
import { useAuth } from "../../contexts/AuthContext";
import "./ProductBlockSection.css";

// Başlık şablonları - başlığa göre ikon eşleştirmesi için
const TITLE_TEMPLATES = [
  {
    icon: "fas fa-bullseye",
    color: "#ef4444",
    title: "Bu Fırsatları Kaçırmayın",
  },
  { icon: "fas fa-bolt", color: "#f59e0b", title: "Şok İndirimler" },
  { icon: "fas fa-tags", color: "#ef4444", title: "Süper Fırsatlar" },
  {
    icon: "fas fa-percentage",
    color: "#10b981",
    title: "Haftalık Kampanyalar",
  },
  { icon: "fas fa-star", color: "#8b5cf6", title: "Özel Seçimler" },
  { icon: "fas fa-gift", color: "#ec4899", title: "Hediye Fırsatlar" },
  { icon: "fas fa-gem", color: "#6366f1", title: "Premium Koleksiyon" },
  { icon: "fas fa-crown", color: "#f59e0b", title: "Elit Ürünler" },
  { icon: "fas fa-fire", color: "#ef4444", title: "En Çok Satanlar" },
  { icon: "fas fa-trophy", color: "#f59e0b", title: "Haftanın Yıldızları" },
  { icon: "fas fa-heart", color: "#ef4444", title: "Müşteri Favorileri" },
  {
    icon: "fas fa-thumbs-up",
    color: "#3b82f6",
    title: "Sizin İçin Seçtiklerimiz",
  },
  { icon: "fas fa-magic", color: "#8b5cf6", title: "Özel Öneriler" },
  { icon: "fas fa-sparkles", color: "#10b981", title: "Yeni Gelenler" },
  { icon: "fas fa-rocket", color: "#3b82f6", title: "Az Önce Eklendi" },
  { icon: "fas fa-leaf", color: "#10b981", title: "Taze Ürünler" },
  { icon: "fas fa-cheese", color: "#f59e0b", title: "Süt & Süt Ürünleri" },
  {
    icon: "fas fa-drumstick-bite",
    color: "#ef4444",
    title: "Et & Et Ürünleri",
  },
  { icon: "fas fa-carrot", color: "#f97316", title: "Meyve & Sebze" },
  { icon: "fas fa-cheese", color: "#fbbf24", title: "Peynir Dünyası" },
  { icon: "fas fa-bread-slice", color: "#d97706", title: "Fırından Taze" },
  { icon: "fas fa-pump-soap", color: "#06b6d4", title: "Temizlik & Bakım" },
  { icon: "fas fa-cookie", color: "#a855f7", title: "Atıştırmalıklar" },
  { icon: "fas fa-mug-hot", color: "#78350f", title: "Kahve & İçecekler" },
  { icon: "fas fa-wheat-awn", color: "#ca8a04", title: "Bakliyat & Tahıllar" },
  { icon: "fas fa-jar", color: "#65a30d", title: "Konserveler" },
  { icon: "fas fa-egg", color: "#fbbf24", title: "Kahvaltılık Lezzetler" },
  { icon: "fas fa-cart-plus", color: "#ff6b35", title: "Hemen Sepete" },
  { icon: "fas fa-percent", color: "#ef4444", title: "Kampanyalı Ürünler" },
  { icon: "fas fa-bell", color: "#f59e0b", title: "Son Fırsat" },
  { icon: "fas fa-clock", color: "#ef4444", title: "Sınırlı Süre" },
];

const ProductBlockSection = ({
  block,
  onAddToCart,
  onToggleFavorite,
  favorites,
}) => {
  const navigate = useNavigate();
  const scrollContainerRef = useRef(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(true);

  // Toast bildirimi için state
  const [cartNotification, setCartNotification] = useState(null);

  // Context'ler - ProductGrid ile AYNI
  const { addToCart } = useCart();
  const { toggleFavorite, isFavorite } = useFavorites();
  const { user } = useAuth();

  // Toast bildirim gösterme fonksiyonu
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

  // Favori toast bildirimi gösterme fonksiyonu
  const showFavoriteNotification = (product) => {
    setCartNotification({
      type: "favorite",
      product: product,
      message: "Favorilere eklendi!",
    });

    // 3 saniye sonra bildirimi gizle
    setTimeout(() => {
      setCartNotification(null);
    }, 3000);
  };

  // Block yoksa veya aktif değilse gösterme
  if (!block || !block.isActive) return null;

  // Ürünleri al (camelCase veya PascalCase)
  const products = block.products || block.Products || [];
  // BAŞLIK: title öncelikli, yoksa name kullan (eski bloklar için fallback)
  const title = block.title || block.Title || block.name || block.Name || "";
  const posterUrl = block.posterImageUrl || block.PosterImageUrl || "";

  // Debug log - konsol kontrolü için
  console.log("📦 [ProductBlockSection] Block:", {
    id: block.id,
    name: block.name,
    title: block.title,
    displayTitle: title,
    posterUrl: posterUrl,
    productsCount: products.length,
    rawBlock: block,
  });

  // Scroll kontrolü
  const checkScroll = () => {
    const container = scrollContainerRef.current;
    if (container) {
      setCanScrollLeft(container.scrollLeft > 0);
      setCanScrollRight(
        container.scrollLeft <
          container.scrollWidth - container.clientWidth - 10,
      );
    }
  };

  const scroll = (direction) => {
    const container = scrollContainerRef.current;
    if (container) {
      const scrollAmount = 240;
      container.scrollBy({
        left: direction === "left" ? -scrollAmount : scrollAmount,
        behavior: "smooth",
      });
      setTimeout(checkScroll, 300);
    }
  };

  const resolveProductData = (item) => {
    const product = item?.product || item?.Product || item;
    const rawId =
      product?.id ??
      product?.Id ??
      product?.productId ??
      product?.ProductId ??
      item?.productId ??
      item?.ProductId ??
      item?.id ??
      item?.Id;
    const productId =
      rawId === undefined || rawId === null ? null : Number(rawId);
    return {
      product,
      productIdRaw: rawId ?? null,
      productId: Number.isNaN(productId) ? null : productId,
    };
  };

  // Ürüne tıklayınca - ProductGrid ile AYNI
  const handleProductClick = (e, productId) => {
    if (!productId) return;
    // Butonlara tıklanmışsa yönlendirme yapma
    if (
      e.target.closest(".btn-favorite") ||
      e.target.closest(".modern-add-btn")
    ) {
      return;
    }
    navigate(`/product/${productId}`);
  };

  // Sepete ekle - ProductGrid ile AYNI
  const handleAddToCart = (e, product, productId) => {
    e.preventDefault();
    e.stopPropagation();

    const rawResolvedId =
      productId ??
      product?.id ??
      product?.Id ??
      product?.productId ??
      product?.ProductId;
    if (rawResolvedId === undefined || rawResolvedId === null) return;
    const numericResolvedId = Number(rawResolvedId);
    const resolvedId = Number.isNaN(numericResolvedId)
      ? rawResolvedId
      : numericResolvedId;

    // Product objesini düzgün formatta hazırla
    const productToAdd = {
      id: resolvedId,
      productId: resolvedId,
      name: product.name || product.Name || "",
      price:
        product.specialPrice ||
        product.discountedPrice ||
        product.price ||
        product.Price ||
        0,
      imageUrl: product.imageUrl || product.ImageUrl || product.image || "",
      stockQuantity:
        product.stockQuantity || product.StockQuantity || product.stock || 100,
      // Toast bildirimi için orijinal fiyatı da ekle
      originalPrice: product.price || product.Price || 0,
      specialPrice: product.specialPrice || product.discountedPrice || null,
    };

    if (typeof onAddToCart === "function") {
      onAddToCart(productToAdd, resolvedId);
      // Toast bildirimi göster
      showCartNotification(productToAdd, user ? "registered" : "guest");
      return;
    }

    console.log("[ProductBlockSection] Sepete ekleniyor:", productToAdd);
    addToCart(productToAdd, 1);
    // Toast bildirimi göster
    showCartNotification(productToAdd, user ? "registered" : "guest");
  };

  // Favorilere ekle/çıkar - ProductGrid ile AYNI
  const handleToggleFavorite = (e, productId, product) => {
    e.preventDefault();
    e.stopPropagation();

    if (!productId) return;
    const id = parseInt(productId, 10);
    if (Number.isNaN(id)) return;

    // Eğer zaten favoride değilse, bildirimi göster
    const alreadyFavorite = Array.isArray(favorites)
      ? favorites.includes(id)
      : isFavorite(id);

    if (typeof onToggleFavorite === "function") {
      onToggleFavorite(id);
    } else {
      console.log("[ProductBlockSection] Favori toggle:", id);
      toggleFavorite(id);
    }

    // Favorilere ekleme durumunda toast göster
    if (!alreadyFavorite && product) {
      showFavoriteNotification({
        id: id,
        name: product.name || product.Name || "",
        imageUrl: product.imageUrl || product.ImageUrl || product.image || "",
        price: product.price || product.Price || 0,
        specialPrice: product.specialPrice || product.discountedPrice || null,
      });
    }
  };

  // ========== AKILLI TÜMÜNÜ GÖR URL OLUŞTURMA ==========
  // TÜM blok tipleri /collection/:slug route'una yönlendirilir
  // Backend, blok tipine göre (manual, discounted, category, vb.) doğru ürünleri döndürür
  const getSmartViewAllUrl = () => {
    const slug = block.slug || block.Slug;

    // DEBUG: Hangi URL üretildiğini logla
    console.log("🔗 [ProductBlockSection] URL hesaplanıyor:", {
      blockId: block.id,
      blockName: block.name || block.Name,
      slug: slug,
      eskiViewAllUrl: block.viewAllUrl, // Backend'den gelen eski değer (ignore edilecek)
      yeniUrl: slug ? `/collection/${slug}` : "/products",
    });

    // ============================================
    // ZORUNLU: TÜM BLOKLAR /collection/:slug KULLANIMALI
    // Admin panelden girilen özel URL'ler artık görmezden geliniyor
    // Çünkü her bloğun slug'ı var ve backend bu slug ile çalışıyor
    // ============================================

    // Blok slug'ı varsa HER ZAMAN collection sayfasına yönlendir
    if (slug) {
      return `/collection/${slug}`;
    }

    // Slug yoksa (çok nadir durum) - fallback olarak blok ID kullan
    const blockId = block.id || block.Id;
    if (blockId) {
      console.warn(`⚠️ Blok slug'ı yok, ID kullanılıyor: ${blockId}`);
      // Backend slug veya ID ile çalışabilir mi kontrol et
      // Şimdilik products sayfasına yönlendir
      return "/products";
    }

    // Son çare - ana sayfa
    console.error("❌ Blok için yönlendirme yapılamadı:", block);
    return "/";
  };

  const viewAllUrl = getSmartViewAllUrl();
  const viewAllText = block.viewAllText || block.ViewAllText || "Tümünü Gör";

  // Başlık için ikon/emoji analizi
  const renderTitle = () => {
    if (!title) return null;

    // Başlık metnine göre şablondan uygun ikonu bul
    const template = TITLE_TEMPLATES.find((t) => t.title === title);

    if (template) {
      // Şablonda bulunan başlık - ikon ile göster
      return (
        <h2 className="block-title">
          <i
            className={template.icon}
            style={{ color: template.color, marginRight: "10px" }}
          ></i>
          {title}
        </h2>
      );
    }

    // Özel başlık veya eski emoji'li başlık - olduğu gibi göster
    return <h2 className="block-title">{title}</h2>;
  };

  return (
    <section className="product-block-section">
      {/* ========== BAŞLIK BÖLÜMÜ (Referans: "Bu Fırsatları Kaçırmayın" gibi) ========== */}
      <div className="product-block-header">
        <div className="header-left">
          {/* Başlık */}
          {renderTitle()}
        </div>
        <div className="header-right">
          {/* Tümünü Gör butonu */}
          <Link to={viewAllUrl} className="view-all-btn">
            {viewAllText}
            <i className="fas fa-arrow-right ms-2"></i>
          </Link>
        </div>
      </div>

      {/* ========== İÇERİK BÖLÜMÜ (Poster + Ürünler) ========== */}
      <div
        className={`product-block-container ${posterUrl ? "with-poster" : "no-poster"}`}
      >
        {/* Sol Taraf - Poster (Opsiyonel) */}
        {posterUrl && (
          <div className="product-block-poster">
            <img src={posterUrl} alt={title} className="poster-image" />
          </div>
        )}

        {/* Sağ Taraf - Ürünler Carousel */}
        <div className="product-block-content">
          <div className="products-slider-wrapper">
            {/* Sol Ok - Web için */}
            {canScrollLeft && (
              <button
                className="slider-arrow slider-arrow-left"
                onClick={() => scroll("left")}
                aria-label="Sola kaydır"
              >
                <i className="fas fa-chevron-left"></i>
              </button>
            )}

            {/* Ürünler - ProductGrid ile BİREBİR AYNI KART */}
            <div
              className="products-scroll-container"
              ref={scrollContainerRef}
              onScroll={checkScroll}
            >
              {products.map((item, index) => {
                const { product, productId, productIdRaw } =
                  resolveProductData(item);
                if (!product) return null;
                const productName = product.name || product.Name || "";
                const productImage =
                  product.imageUrl || product.ImageUrl || product.image || "";
                const productPrice =
                  parseFloat(product.price || product.Price || 0) || 0;
                const rawSpecialPrice =
                  product.specialPrice ||
                  product.discountedPrice ||
                  product.DiscountedPrice;
                const specialPrice =
                  rawSpecialPrice === undefined || rawSpecialPrice === null
                    ? null
                    : parseFloat(rawSpecialPrice);
                const hasDiscount =
                  specialPrice !== null &&
                  !Number.isNaN(specialPrice) &&
                  specialPrice < productPrice;
                const currentPrice = hasDiscount ? specialPrice : productPrice;
                const originalPrice = hasDiscount ? productPrice : null;
                const stock =
                  product.stockQuantity ||
                  product.StockQuantity ||
                  product.stock ||
                  100;
                const isOutOfStock = stock <= 0;
                const rating = product.rating || product.Rating || 0;
                const reviewCount =
                  product.reviewCount || product.ReviewCount || 0;
                const isProductFavorite = Array.isArray(favorites)
                  ? favorites.includes(productId)
                  : productId !== null
                    ? isFavorite(productId)
                    : false;

                // İndirim yüzdesi hesapla
                const discountPercent =
                  hasDiscount && productPrice > 0
                    ? Math.round(
                        ((productPrice - specialPrice) / productPrice) * 100,
                      )
                    : 0;

                return (
                  <div
                    key={productIdRaw || productId || index}
                    className={`modern-product-card product-block-card ${hasDiscount ? "has-discount" : ""}`}
                    style={{
                      background: "#ffffff",
                      borderRadius: "16px",
                      border: hasDiscount
                        ? "2px solid #ef4444"
                        : "1px solid rgba(255, 107, 53, 0.1)",
                      overflow: "hidden",
                      position: "relative",
                      cursor: "pointer",
                      flexShrink: 0,
                      display: "flex",
                      flexDirection: "column",
                      boxShadow: hasDiscount
                        ? "0 8px 25px rgba(239, 68, 68, 0.2)"
                        : "0 5px 15px rgba(0, 0, 0, 0.08)",
                      transition: "all 0.3s ease",
                    }}
                    onClick={(e) =>
                      handleProductClick(e, productIdRaw || productId)
                    }
                    onMouseEnter={(e) => {
                      e.currentTarget.style.transform = "translateY(-5px)";
                      e.currentTarget.style.boxShadow = hasDiscount
                        ? "0 20px 40px rgba(239, 68, 68, 0.25)"
                        : "0 15px 30px rgba(255, 107, 53, 0.15)";
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.transform = "translateY(0)";
                      e.currentTarget.style.boxShadow = hasDiscount
                        ? "0 8px 25px rgba(239, 68, 68, 0.2)"
                        : "0 5px 15px rgba(0, 0, 0, 0.08)";
                    }}
                  >
                    {/* ========== İNDİRİM BADGE - SOL ÜST ========== */}
                    {hasDiscount && discountPercent > 0 && (
                      <div
                        className="discount-badge-wrapper position-absolute"
                        style={{
                          top: "8px",
                          left: "8px",
                          zIndex: 5,
                        }}
                      >
                        <div
                          className="discount-badge"
                          style={{
                            background:
                              "linear-gradient(135deg, #ef4444, #dc2626)",
                            color: "white",
                            padding: "4px 10px",
                            borderRadius: "20px",
                            fontSize: "0.75rem",
                            fontWeight: "700",
                            boxShadow: "0 4px 12px rgba(239, 68, 68, 0.4)",
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          <i
                            className="fas fa-tag"
                            style={{ fontSize: "0.65rem" }}
                          ></i>
                          %{discountPercent} İNDİRİM
                        </div>
                      </div>
                    )}

                    {/* Favori Butonu - SAĞ ÜST */}
                    <div
                      className="position-absolute top-0 end-0 p-2 d-flex flex-column gap-1"
                      style={{ zIndex: 3 }}
                    >
                      <button
                        className="btn-favorite"
                        type="button"
                        onClick={(e) =>
                          handleToggleFavorite(e, productId, product)
                        }
                        style={{
                          background: isProductFavorite
                            ? "linear-gradient(135deg, #ff6b35, #ff8c00)"
                            : "rgba(255, 255, 255, 0.9)",
                          border: "none",
                          borderRadius: "50%",
                          width: "30px",
                          height: "30px",
                          display: "flex",
                          alignItems: "center",
                          justifyContent: "center",
                          color: isProductFavorite ? "white" : "#ff6b35",
                          transition: "all 0.3s ease",
                          backdropFilter: "blur(10px)",
                          boxShadow: isProductFavorite
                            ? "0 4px 15px rgba(255, 107, 53, 0.4)"
                            : "0 2px 8px rgba(0,0,0,0.1)",
                        }}
                      >
                        <i
                          className={
                            isProductFavorite ? "fas fa-heart" : "far fa-heart"
                          }
                        ></i>
                      </button>
                    </div>

                    {/* Ürün Resmi - ProductGrid ile AYNI */}
                    <div
                      className="product-image-container"
                      style={{
                        background: "#ffffff",
                        position: "relative",
                        overflow: "hidden",
                      }}
                    >
                      <div
                        className="image-wrapper d-flex align-items-center justify-content-center h-100"
                        style={{ transition: "all 0.4s ease" }}
                      >
                        {productImage ? (
                          <img
                            src={productImage}
                            alt={productName}
                            style={{
                              maxHeight: "110px",
                              maxWidth: "110px",
                              objectFit: "contain",
                              filter: "drop-shadow(0 4px 12px rgba(0,0,0,0.1))",
                              transition: "all 0.3s ease",
                            }}
                            onError={(e) => {
                              e.target.src = "/placeholder-product.png";
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

                    {/* Card Body - ProductGrid ile AYNI */}
                    <div
                      className="card-body p-3 d-flex flex-column"
                      style={{ background: "#ffffff" }}
                    >
                      {/* Rating */}
                      {rating > 0 && (
                        <div className="d-flex align-items-center mb-1">
                          <div className="star-rating me-1">
                            {[...Array(5)].map((_, i) => (
                              <i
                                key={i}
                                className={`fas fa-star ${i < Math.floor(rating) ? "text-warning" : "text-muted"}`}
                                style={{ fontSize: "0.6rem" }}
                              ></i>
                            ))}
                          </div>
                          <small
                            className="text-muted"
                            style={{ fontSize: "0.65rem" }}
                          >
                            ({reviewCount})
                          </small>
                        </div>
                      )}

                      {/* Ürün Adı */}
                      <h6 className="product-title mb-1">{productName}</h6>

                      {/* Fiyat - İNDİRİMLİ ÜRÜNLER İÇİN DİKKAT ÇEKİCİ */}
                      <div className="price-section">
                        {hasDiscount && originalPrice ? (
                          <div className="price-container">
                            {/* İndirim Yüzdesi Badge */}
                            <div
                              className="discount-badge-large"
                              style={{
                                background:
                                  "linear-gradient(135deg, #ef4444, #dc2626)",
                                color: "white",
                                padding: "3px 8px",
                                borderRadius: "12px",
                                fontSize: "0.7rem",
                                fontWeight: "700",
                                display: "inline-block",
                                marginBottom: "3px",
                                boxShadow: "0 2px 8px rgba(239, 68, 68, 0.3)",
                              }}
                            >
                              <i
                                className="fas fa-bolt me-1"
                                style={{ fontSize: "0.6rem" }}
                              ></i>
                              %{discountPercent} İNDİRİM
                            </div>
                            {/* Eski Fiyat */}
                            <div
                              className="original-price-large"
                              style={{
                                fontSize: "0.8rem",
                                fontWeight: "600",
                                textDecoration: "line-through",
                                textDecorationColor: "#ef4444",
                                textDecorationThickness: "2px",
                                color: "#6b7280",
                                marginBottom: "1px",
                              }}
                            >
                              {originalPrice.toFixed(2)} TL
                            </div>
                            {/* Yeni Fiyat */}
                            <div
                              className="new-price-large"
                              style={{
                                fontSize: "1.1rem",
                                fontWeight: "800",
                                color: "#ef4444",
                                lineHeight: "1.2",
                              }}
                            >
                              {currentPrice.toFixed(2)} TL
                            </div>
                            {/* Tasarruf Bilgisi */}
                            <div
                              className="savings-info"
                              style={{
                                fontSize: "0.6rem",
                                color: "#10b981",
                                fontWeight: "600",
                                marginTop: "1px",
                              }}
                            >
                              <i className="fas fa-gift me-1"></i>
                              {(originalPrice - currentPrice).toFixed(2)} TL
                              tasarruf
                            </div>
                          </div>
                        ) : (
                          <div className="current-price product-price">
                            {currentPrice.toFixed(2)} TL
                          </div>
                        )}
                      </div>

                      {/* Sepete Ekle Butonu - ProductGrid ile AYNI */}
                      <div className="action-buttons mt-auto">
                        <button
                          className="modern-add-btn"
                          type="button"
                          disabled={isOutOfStock}
                          onClick={(e) =>
                            handleAddToCart(
                              e,
                              product,
                              productIdRaw || productId,
                            )
                          }
                        >
                          {isOutOfStock ? "Stokta Yok" : "Sepete Ekle"}
                        </button>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Sağ Ok */}
            {canScrollRight && products.length > 3 && (
              <button
                className="slider-arrow slider-arrow-right"
                onClick={() => scroll("right")}
                aria-label="Sağa kaydır"
              >
                <i className="fas fa-chevron-right"></i>
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Premium Cart Notification Toast - ProductGrid ile AYNI */}
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
                            ₺
                            {cartNotification.product.originalPrice?.toFixed(2)}
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
                          <div
                            className={
                              cartNotification.userType === "guest"
                                ? "col-8"
                                : "col-12"
                            }
                          >
                            <button
                              className="btn w-100 text-white fw-bold"
                              onClick={() => {
                                setCartNotification(null);
                                navigate("/cart");
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
                                  navigate("/login");
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
    </section>
  );
};

export default ProductBlockSection;
