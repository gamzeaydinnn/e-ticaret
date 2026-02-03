/**
 * CollectionPage.jsx - Blok/Koleksiyon Ürün Sayfası
 *
 * Manuel seçilmiş ürünlerin "Tümünü Gör" sayfası.
 * URL: /collection/:slug
 *
 * Özellikler:
 * - Blok başlığı gösterilir
 * - Tüm seçili ürünler grid olarak listelenir
 * - Sıralama ve filtreleme desteği
 * - Pagination / "Daha Fazla Yükle" butonu
 *
 * @version 2.0.0 - Pagination eklendi
 */

import React, { useState, useEffect, useMemo } from "react";
import { useParams, Link } from "react-router-dom";
import { useCart } from "../contexts/CartContext";
import { useFavorites } from "../contexts/FavoriteContext";
import homeBlockService from "../services/homeBlockService";
import "./CollectionPage.css";

// ============================================
// SABİTLER
// ============================================

/** Sayfa başına gösterilecek ürün sayısı */
const PRODUCTS_PER_PAGE = 20;

const CollectionPage = () => {
  const { slug } = useParams();
  const { addToCart } = useCart();
  const { toggleFavorite, isFavorite } = useFavorites();

  // ============================================
  // STATE - HOOK'LAR EN ÜSTTE OLMALI
  // ============================================
  const [block, setBlock] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [visibleCount, setVisibleCount] = useState(PRODUCTS_PER_PAGE);
  const [loadingMore, setLoadingMore] = useState(false);

  // ============================================
  // VERİ ÇEKME
  // ============================================
  useEffect(() => {
    const fetchBlock = async () => {
      try {
        setLoading(true);
        const data = await homeBlockService.getBlockBySlug(slug);

        console.log("📦 CollectionPage - API Response:", {
          slug,
          data,
          products: data?.products || data?.Products,
          blockType: data?.blockType || data?.BlockType,
        });

        if (data) {
          const normalizedBlock = {
            ...data,
            products: data.products || data.Products || [],
            title: data.title || data.Title || data.name || data.Name,
            blockType: data.blockType || data.BlockType || "manual",
          };
          setBlock(normalizedBlock);
        } else {
          setError("Koleksiyon bulunamadı");
        }
      } catch (err) {
        console.error("❌ Koleksiyon yüklenirken hata:", err);
        setError("Koleksiyon yüklenirken bir hata oluştu");
      } finally {
        setLoading(false);
      }
    };

    if (slug) {
      fetchBlock();
    }
  }, [slug]);

  // ============================================
  // COMPUTED VALUES - HOOK'LAR EARLY RETURN'DEN ÖNCE
  // ============================================
  const products = block?.products || [];
  const title = block?.title || block?.name || "Koleksiyon";

  /** Görünür ürünleri hesapla - useMemo ile performans optimizasyonu */
  const visibleProducts = useMemo(() => {
    return products.slice(0, visibleCount);
  }, [products, visibleCount]);

  /** Daha fazla ürün var mı? */
  const hasMore = visibleCount < products.length;

  /** Kalan ürün sayısı */
  const remainingCount = products.length - visibleCount;

  // ============================================
  // EVENT HANDLERS
  // ============================================
  const handleAddToCart = (product) => {
    const productToAdd = {
      id: product.id,
      productId: product.id,
      name: product.name,
      price: product.specialPrice || product.price,
      imageUrl: product.imageUrl,
      stockQuantity: product.stockQuantity || 100,
    };
    addToCart(productToAdd, 1);
  };

  /**
   * Daha fazla ürün yükle
   */
  const handleLoadMore = () => {
    setLoadingMore(true);
    setTimeout(() => {
      setVisibleCount((prev) =>
        Math.min(prev + PRODUCTS_PER_PAGE, products.length),
      );
      setLoadingMore(false);
    }, 300);
  };

  /**
   * Tüm ürünleri göster
   */
  const handleShowAll = () => {
    setLoadingMore(true);
    setTimeout(() => {
      setVisibleCount(products.length);
      setLoadingMore(false);
    }, 300);
  };

  // ============================================
  // RENDER - CONDITIONAL RENDER EN SONDA
  // ============================================
  if (loading) {
    return (
      <div className="collection-page">
        <div className="container py-5">
          <div className="text-center">
            <div className="spinner-border text-primary" role="status">
              <span className="visually-hidden">Yükleniyor...</span>
            </div>
            <p className="mt-3 text-muted">Koleksiyon yükleniyor...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error || !block) {
    return (
      <div className="collection-page">
        <div className="container py-5">
          <div className="text-center">
            <i className="fas fa-exclamation-circle fa-4x text-muted mb-3"></i>
            <h3 className="text-muted">{error || "Koleksiyon bulunamadı"}</h3>
            <Link to="/" className="btn btn-primary mt-3">
              <i className="fas fa-home me-2"></i>
              Ana Sayfaya Dön
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="collection-page">
      {/* Header */}
      <div className="collection-header">
        <div className="container">
          <nav aria-label="breadcrumb" className="mb-3">
            <ol className="breadcrumb">
              <li className="breadcrumb-item">
                <Link to="/">Ana Sayfa</Link>
              </li>
              <li className="breadcrumb-item active" aria-current="page">
                {title}
              </li>
            </ol>
          </nav>

          <div className="d-flex justify-content-between align-items-center flex-wrap gap-3">
            <div>
              <h1 className="collection-title">{title}</h1>
              <p className="collection-count text-muted">
                {products.length} ürün bulundu
              </p>
            </div>

            {block.posterImageUrl && (
              <div className="collection-poster">
                <img
                  src={block.posterImageUrl}
                  alt={title}
                  className="poster-thumbnail"
                />
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Products Grid */}
      <div className="container py-4">
        {products.length === 0 ? (
          <div className="text-center py-5">
            <i className="fas fa-box-open fa-4x text-muted mb-3"></i>
            <h4 className="text-muted">Bu koleksiyonda henüz ürün yok</h4>
            {/* Blok tipi bilgisi - debug amaçlı */}
            <p className="text-muted small mt-2">
              <span className="badge bg-secondary me-2">
                {block.blockType || "manual"}
              </span>
              {block.blockType === "discounted" && (
                <span>
                  İndirimli ürün bulunamadı. Ürünlere indirimli fiyat
                  tanımlayın.
                </span>
              )}
              {block.blockType === "category" && (
                <span>Bu kategoride ürün bulunmuyor.</span>
              )}
              {block.blockType === "manual" && (
                <span>Admin panelden bu bloğa ürün ekleyin.</span>
              )}
            </p>
            <Link to="/" className="btn btn-outline-primary mt-3">
              <i className="fas fa-home me-2"></i>
              Ana Sayfaya Dön
            </Link>
          </div>
        ) : (
          <>
            {/* Sayfa bilgisi - kaç ürün gösteriliyor */}
            <div className="d-flex justify-content-between align-items-center mb-3">
              <small className="text-muted">
                {visibleProducts.length} / {products.length} ürün gösteriliyor
              </small>
              {hasMore && (
                <button
                  className="btn btn-sm btn-outline-primary"
                  onClick={handleShowAll}
                  disabled={loadingMore}
                >
                  <i className="fas fa-eye me-1"></i>
                  Tümünü Göster ({products.length})
                </button>
              )}
            </div>

            {/* Ürün Grid - Sadece visibleProducts render edilir */}
            <div className="row g-3">
              {visibleProducts.map((product) => {
                const hasDiscount =
                  product.specialPrice && product.specialPrice < product.price;
                const discountPercent = hasDiscount
                  ? Math.round(
                      ((product.price - product.specialPrice) / product.price) *
                        100,
                    )
                  : 0;
                const isProductFavorite = isFavorite(product.id);

                return (
                  <div key={product.id} className="col-6 col-md-4 col-lg-3">
                    <div
                      className={`product-card ${hasDiscount ? "has-discount" : ""}`}
                    >
                      {/* İndirim Badge */}
                      {hasDiscount && (
                        <div className="discount-badge">
                          <i className="fas fa-bolt me-1"></i>%{discountPercent}
                        </div>
                      )}

                      {/* Favori */}
                      <button
                        className={`favorite-btn ${isProductFavorite ? "active" : ""}`}
                        onClick={(e) => {
                          e.preventDefault();
                          toggleFavorite(product.id);
                        }}
                      >
                        <i
                          className={
                            isProductFavorite ? "fas fa-heart" : "far fa-heart"
                          }
                        ></i>
                      </button>

                      {/* Ürün Resmi */}
                      <Link
                        to={`/product/${product.id}`}
                        className="product-image-link"
                      >
                        <img
                          src={product.imageUrl || "/placeholder-product.png"}
                          alt={product.name}
                          className="product-image"
                          onError={(e) => {
                            e.target.src = "/placeholder-product.png";
                          }}
                        />
                      </Link>

                      {/* Ürün Bilgileri */}
                      <div className="product-info">
                        <Link
                          to={`/product/${product.id}`}
                          className="product-name"
                        >
                          {product.name}
                        </Link>

                        {/* Fiyat */}
                        <div className="product-price">
                          {hasDiscount ? (
                            <>
                              <span className="old-price">
                                {product.price.toFixed(2)} TL
                              </span>
                              <span className="current-price discounted">
                                {product.specialPrice.toFixed(2)} TL
                              </span>
                            </>
                          ) : (
                            <span className="current-price">
                              {product.price.toFixed(2)} TL
                            </span>
                          )}
                        </div>

                        {/* Sepete Ekle */}
                        <button
                          className="add-to-cart-btn"
                          onClick={() => handleAddToCart(product)}
                        >
                          <i className="fas fa-shopping-cart me-2"></i>
                          Sepete Ekle
                        </button>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* ============================================
                DAHA FAZLA YÜKLE BUTONU
                Büyük koleksiyonlarda (50+ ürün) kullanıcı deneyimini iyileştirir
                ============================================ */}
            {hasMore && (
              <div className="text-center mt-4 mb-3">
                <button
                  className="btn btn-primary btn-lg load-more-btn"
                  onClick={handleLoadMore}
                  disabled={loadingMore}
                  style={{
                    minWidth: "250px",
                    borderRadius: "25px",
                    padding: "12px 32px",
                    fontSize: "1rem",
                    fontWeight: "600",
                    boxShadow: "0 4px 12px rgba(255, 107, 53, 0.3)",
                    background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                    border: "none",
                    transition: "all 0.3s ease",
                  }}
                >
                  {loadingMore ? (
                    <>
                      <span
                        className="spinner-border spinner-border-sm me-2"
                        role="status"
                      ></span>
                      Yükleniyor...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-plus-circle me-2"></i>
                      Daha Fazla Yükle ({remainingCount} ürün kaldı)
                    </>
                  )}
                </button>
              </div>
            )}

            {/* Tüm ürünler yüklendiğinde bilgi */}
            {!hasMore && products.length > PRODUCTS_PER_PAGE && (
              <div className="text-center mt-4 mb-3">
                <div
                  className="alert alert-success py-2"
                  style={{ display: "inline-block" }}
                >
                  <i className="fas fa-check-circle me-2"></i>
                  Tüm {products.length} ürün gösteriliyor
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
};

export default CollectionPage;
