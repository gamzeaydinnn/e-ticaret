/**
 * PromoCards.jsx - Ana Sayfa Promosyon Kartları Bileşeni
 *
 * Yatay kaydırmalı carousel + ürün blokları ile aynı giriş/kayma animasyonu.
 */

import React, { useState, useCallback, memo } from "react";
import PropTypes from "prop-types";
import BannerMedia from "./BannerMedia";
import {
  useAutoSlideCarousel,
  useInViewMotion,
} from "../hooks/useAutoSlideCarousel";
import "./PromoCards.css";

const PLACEHOLDER_IMAGE = "/images/placeholder.png";
const SKELETON_COUNT = 5;

const PromoCard = memo(function PromoCard({ promo, index }) {
  const [isHovered, setIsHovered] = useState(false);
  const [imageError, setImageError] = useState(false);

  const handleImageError = useCallback(() => {
    setImageError(true);
  }, []);

  const imageSrc = imageError
    ? PLACEHOLDER_IMAGE
    : promo.imageUrl || PLACEHOLDER_IMAGE;

  const enterDelayMs = Math.min(index, 10) * 55;
  const CardWrapper = promo.linkUrl ? "a" : "div";
  const linkProps = promo.linkUrl
    ? { href: promo.linkUrl, onClick: (e) => e.stopPropagation() }
    : {};

  return (
    <CardWrapper
      {...linkProps}
      className={`promo-card-item carousel-slide-card promo-card-item--enter${isHovered ? " is-hovered" : ""}`}
      style={{ "--enter-delay": `${enterDelayMs}ms` }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      role="article"
      aria-label={promo.title || "Promosyon"}
    >
      <div className="promo-card-image-container">
        <BannerMedia
          src={imageSrc}
          alt={promo.title || "Promosyon görseli"}
          className="promo-card-image"
          loading={index < 4 ? "eager" : "lazy"}
          onError={handleImageError}
        />
        {promo.badge && <span className="promo-card-badge">{promo.badge}</span>}
      </div>

      {promo.title && (
        <div className="promo-card-overlay">
          <h3 className="promo-card-title" title={promo.title}>
            {promo.title}
          </h3>
        </div>
      )}
    </CardWrapper>
  );
});

PromoCard.propTypes = {
  promo: PropTypes.shape({
    id: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
    imageUrl: PropTypes.string.isRequired,
    title: PropTypes.string,
    linkUrl: PropTypes.string,
    badge: PropTypes.string,
  }).isRequired,
  index: PropTypes.number.isRequired,
};

const SkeletonCard = memo(function SkeletonCard() {
  return (
    <div className="promo-card-skeleton">
      <div className="promo-card-skeleton-image" />
    </div>
  );
});

function PromoCards({
  promos = [],
  loading = false,
  title = "Kampanyalar",
  icon = "fa-tags",
  showTitle = true,
}) {
  const { sectionRef, hasEntered, isMotionActive } = useInViewMotion();
  const {
    scrollContainerRef,
    trackRef,
    canScrollLeft,
    canScrollRight,
    scroll,
  } = useAutoSlideCarousel({
    motionActive: isMotionActive,
    cardSelector: ".promo-card-item",
    itemCount: promos.length,
  });

  if (loading) {
    return (
      <section className="promo-cards-section">
        {showTitle && (
          <div className="promo-cards-header">
            <h2 className="promo-cards-title">
              <i className={`fas ${icon} promo-cards-icon`} />
              {title}
            </h2>
          </div>
        )}
        <div className="promo-cards-carousel-wrapper">
          <div
            className="promo-cards-scroll-container"
            aria-label="Kampanyalar yükleniyor"
          >
            {Array.from({ length: SKELETON_COUNT }).map((_, index) => (
              <SkeletonCard key={`skeleton-${index}`} />
            ))}
          </div>
        </div>
      </section>
    );
  }

  if (!promos || promos.length === 0) {
    return null;
  }

  const showArrows = promos.length > 2;

  return (
    <section
      ref={sectionRef}
      className={`promo-cards-section promo-cards-animated ${hasEntered ? "is-in-view" : ""}${isMotionActive ? " is-motion-active" : ""}`}
    >
      {showTitle && (
        <div className="promo-cards-header">
          <h2 className="promo-cards-title">
            <i className={`fas ${icon} promo-cards-icon`} />
            {title}
          </h2>
        </div>
      )}

      <div className="promo-cards-carousel-wrapper">
        {showArrows && canScrollLeft && (
          <button
            type="button"
            className="promo-cards-arrow promo-cards-arrow-left"
            onClick={() => scroll("left")}
            aria-label="Sola kaydır"
          >
            <i className="fas fa-chevron-left" aria-hidden="true" />
          </button>
        )}

        <div
          className="promo-cards-scroll-container"
          ref={scrollContainerRef}
          role="list"
          aria-label={title}
        >
          <div className="promo-cards-scroll-track" ref={trackRef}>
            {promos.map((promo, index) => (
              <PromoCard key={promo.id} promo={promo} index={index} />
            ))}
          </div>
        </div>

        {showArrows && canScrollRight && (
          <button
            type="button"
            className="promo-cards-arrow promo-cards-arrow-right"
            onClick={() => scroll("right")}
            aria-label="Sağa kaydır"
          >
            <i className="fas fa-chevron-right" aria-hidden="true" />
          </button>
        )}
      </div>
    </section>
  );
}

PromoCards.propTypes = {
  promos: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
      imageUrl: PropTypes.string.isRequired,
      title: PropTypes.string,
      linkUrl: PropTypes.string,
      badge: PropTypes.string,
    }),
  ),
  loading: PropTypes.bool,
  title: PropTypes.string,
  icon: PropTypes.string,
  columns: PropTypes.number,
  showTitle: PropTypes.bool,
};

export default memo(PromoCards);
