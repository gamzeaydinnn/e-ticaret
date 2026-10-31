import React, { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import PropTypes from "prop-types";
import ProductCard from "../../pages/components/ProductCard";
import BannerMedia from "../BannerMedia";
import homeBlockService from "../../services/homeBlockService";

/**
 * HomeProductBlock Bileşeni
 * 
 * Ana sayfada poster ve ürün ızgarasını render eder.
 * TDD Dokümanına uygun olarak:
 * - Desktop: Sol %30 poster + Sağ %70 ürün ızgarası
 * - Mobil: Tam genişlik 16:9 poster + ürün listesi
 * - Tıklanabilir poster ve analitik takip (track-click, track-view)
 */
export default function HomeProductBlock({
  block,
  onToggleFavorite,
  favorites = [],
  onAddToCart,
}) {
  const blockRef = useRef(null);
  const [hasTrackedView, setHasTrackedView] = useState(false);

  // Intersection Observer for view tracking (%50 görünürlük)
  useEffect(() => {
    if (!blockRef.current || hasTrackedView || !block?.id) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting && !hasTrackedView) {
            homeBlockService.trackBlockView(block.id);
            setHasTrackedView(true);
          }
        });
      },
      { threshold: 0.5 }
    );

    observer.observe(blockRef.current);

    return () => observer.disconnect();
  }, [block?.id, hasTrackedView]);

  const handlePosterClick = () => {
    if (block?.id) {
      homeBlockService.trackPosterClick(block.id);
    }
  };

  const posterClickTarget =
    block?.posterClickTarget ||
    block?.banner?.linkUrl ||
    block?.posterLinkUrl ||
    null;

  const isExternal = posterClickTarget?.startsWith("http");
  const PosterWrapper = posterClickTarget
    ? isExternal
      ? "a"
      : Link
    : "div";

  const posterProps = posterClickTarget
    ? isExternal
      ? {
          href: posterClickTarget,
          target: "_blank",
          rel: "noopener noreferrer",
          onClick: handlePosterClick,
        }
      : {
          to: posterClickTarget,
          onClick: handlePosterClick,
        }
    : {};

  const posterImageUrl =
    block?.banner?.imageUrl || block?.posterImageUrl || null;

  return (
    <section ref={blockRef} className="mb-4 home-product-block">
      {/* Block Title */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: 12,
        }}
      >
        <h2 style={{ fontSize: "1.25rem", fontWeight: 600, margin: 0 }}>
          {block.title || block.name}
        </h2>
        {block.viewAllUrl && (
          <Link
            to={block.viewAllUrl}
            style={{
              fontSize: "0.875rem",
              color: "#3b82f6",
              textDecoration: "none",
              display: "flex",
              alignItems: "center",
              gap: 4,
            }}
          >
            {block.viewAllText || "Tümünü Gör"}
            <i
              className="fas fa-arrow-right"
              style={{ fontSize: "0.75rem" }}
            ></i>
          </Link>
        )}
      </div>

      {/* Desktop Layout: Poster (30%) + Products (70%) */}
      <div className="d-none d-md-flex" style={{ gap: 16 }}>
        {/* Poster */}
        {posterImageUrl && (
          <PosterWrapper
            {...posterProps}
            style={{
              flex: "0 0 30%",
              borderRadius: 12,
              overflow: "hidden",
              boxShadow: "0 4px 6px rgba(0,0,0,0.1)",
              cursor: posterClickTarget ? "pointer" : "default",
              touchAction: "manipulation",
              transition: "transform 0.2s ease",
              display: "block",
              textDecoration: "none",
            }}
            onMouseEnter={(e) => {
              if (posterClickTarget) {
                e.currentTarget.style.transform = "scale(1.02)";
              }
            }}
            onMouseLeave={(e) => {
              if (posterClickTarget) {
                e.currentTarget.style.transform = "scale(1)";
              }
            }}
          >
            <BannerMedia
              src={posterImageUrl}
              alt={block.title || block.name}
              style={{
                width: "100%",
                height: "100%",
                objectFit: "cover",
                display: "block",
                minHeight: 300,
              }}
              onError={(e) => {
                if (e?.target) {
                  e.target.style.background = block.backgroundColor || "#e5e7eb";
                }
              }}
            />
          </PosterWrapper>
        )}

        {/* Products Grid */}
        <div
          style={{
            flex: "1",
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill, minmax(130px, 1fr))",
            gap: 10,
          }}
        >
          {block.products?.map((product) => (
            <ProductCard
              key={product.id}
              product={product}
              onToggleFavorite={() => onToggleFavorite?.(product.id)}
              isFavorite={favorites?.includes(product.id)}
              onAddToCart={onAddToCart}
              cardMaxWidth="220px"
            />
          ))}
        </div>
      </div>

      {/* Mobile Layout: Poster (full width 16:9) + Products (stacked) */}
      <div className="d-md-none">
        {/* Poster */}
        {posterImageUrl && (
          <PosterWrapper
            {...posterProps}
            style={{
              display: "block",
              borderRadius: 12,
              overflow: "hidden",
              boxShadow: "0 4px 6px rgba(0,0,0,0.1)",
              marginBottom: 12,
              cursor: posterClickTarget ? "pointer" : "default",
              touchAction: "manipulation",
              textDecoration: "none",
            }}
          >
            <BannerMedia
              src={posterImageUrl}
              alt={block.title || block.name}
              style={{
                width: "100%",
                height: "auto",
                aspectRatio: "16/9",
                objectFit: "cover",
                display: "block",
              }}
              onError={(e) => {
                if (e?.target) {
                  e.target.style.background = block.backgroundColor || "#e5e7eb";
                }
              }}
            />
          </PosterWrapper>
        )}

        {/* Products Grid */}
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill, minmax(130px, 1fr))",
            gap: 10,
          }}
        >
          {block.products?.map((product) => (
            <ProductCard
              key={product.id}
              product={product}
              onToggleFavorite={() => onToggleFavorite?.(product.id)}
              isFavorite={favorites?.includes(product.id)}
              onAddToCart={onAddToCart}
              cardMaxWidth="220px"
            />
          ))}
        </div>
      </div>
    </section>
  );
}

HomeProductBlock.propTypes = {
  block: PropTypes.shape({
    id: PropTypes.number.isRequired,
    name: PropTypes.string,
    title: PropTypes.string,
    posterImageUrl: PropTypes.string,
    posterLinkUrl: PropTypes.string,
    posterClickTarget: PropTypes.string,
    backgroundColor: PropTypes.string,
    viewAllUrl: PropTypes.string,
    viewAllText: PropTypes.string,
    banner: PropTypes.shape({
      imageUrl: PropTypes.string,
      linkUrl: PropTypes.string,
    }),
    products: PropTypes.array,
  }).isRequired,
  onToggleFavorite: PropTypes.func,
  favorites: PropTypes.array,
  onAddToCart: PropTypes.func,
};

