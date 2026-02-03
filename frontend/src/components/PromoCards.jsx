/**
 * PromoCards.jsx - Ana Sayfa Promosyon Kartları Bileşeni
 *
 * Bu bileşen, ana sayfadaki promosyon/kampanya kartlarını
 * yatay kaydırmalı carousel olarak render eder.
 *
 * Özellikler:
 * - Yatay kaydırmalı carousel (web: ok butonları, mobil: swipe)
 * - Responsive tasarım (mobilde 3 poster görünür)
 * - Hover efektleri
 * - Link desteği
 * - Lazy loading (performans için)
 * - Placeholder/skeleton (yükleme durumu)
 * - Fallback görsel (hata durumu)
 * - Erişilebilirlik (ARIA etiketleri)
 *
 * @author Senior Developer
 * @version 2.0.0 - Carousel yapısına dönüştürüldü
 */

import React, { useState, useCallback, memo, useRef, useEffect } from "react";
import PropTypes from "prop-types";
import "./PromoCards.css";

// ============================================
// SABİTLER
// ============================================

/** Varsayılan placeholder görseli */
const PLACEHOLDER_IMAGE = "/images/placeholder.png";

/** Skeleton sayısı (yükleme durumunda gösterilecek) */
const SKELETON_COUNT = 5;

// ============================================
// STIL SABİTLERİ - CAROUSEL YAPISI
// ============================================

const styles = {
  section: {
    marginBottom: "24px",
    overflow: "hidden",
  },
  header: {
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    marginBottom: "16px",
    padding: "0 20px",
    maxWidth: "1400px",
    margin: "0 auto 16px",
  },
  title: {
    fontSize: "1.25rem",
    fontWeight: "600",
    margin: 0,
    display: "flex",
    alignItems: "center",
    gap: "8px",
  },
  icon: {
    color: "#f97316",
  },
  // Carousel wrapper - oklar dahil
  carouselWrapper: {
    position: "relative",
    maxWidth: "1400px",
    margin: "0 auto",
    padding: "0 60px", // Ok butonları için yer
  },
  // Scroll container - yatay kaydırma
  scrollContainer: {
    display: "flex",
    gap: "12px",
    overflowX: "auto",
    scrollBehavior: "smooth",
    padding: "10px 5px",
    scrollbarWidth: "none", // Firefox
    msOverflowStyle: "none", // IE/Edge
    scrollSnapType: "x proximity",
  },
  card: {
    position: "relative",
    display: "block",
    borderRadius: "16px",
    overflow: "hidden",
    boxShadow: "0 4px 12px rgba(0,0,0,0.1)",
    transition: "transform 0.3s ease, box-shadow 0.3s ease",
    backgroundColor: "#fff",
    textDecoration: "none",
    flexShrink: 0,
    scrollSnapAlign: "start",
  },
  cardHover: {
    transform: "translateY(-4px)",
    boxShadow: "0 8px 24px rgba(0,0,0,0.15)",
  },
  imageContainer: {
    position: "relative",
    width: "100%",
    paddingTop: "56%", // 16:9 oranına yakın
    overflow: "hidden",
    backgroundColor: "#f3f4f6",
  },
  image: {
    position: "absolute",
    top: 0,
    left: 0,
    width: "100%",
    height: "100%",
    objectFit: "cover",
    objectPosition: "center top",
    transition: "transform 0.3s ease",
  },
  imageHover: {
    transform: "scale(1.05)",
  },
  overlay: {
    position: "absolute",
    bottom: 0,
    left: 0,
    right: 0,
    padding: "10px",
    background: "linear-gradient(transparent, rgba(0,0,0,0.75))",
    color: "white",
  },
  cardTitle: {
    fontSize: "0.85rem",
    fontWeight: "600",
    margin: 0,
    textShadow: "0 1px 2px rgba(0,0,0,0.3)",
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  badge: {
    position: "absolute",
    top: "6px",
    right: "6px",
    backgroundColor: "#ef4444",
    color: "white",
    padding: "3px 7px",
    borderRadius: "10px",
    fontSize: "0.65rem",
    fontWeight: "bold",
    zIndex: 5,
  },
  // Ok butonları
  arrow: {
    position: "absolute",
    top: "50%",
    transform: "translateY(-50%)",
    width: "44px",
    height: "44px",
    borderRadius: "50%",
    background: "#ffffff",
    border: "none",
    boxShadow: "0 4px 15px rgba(0, 0, 0, 0.12)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    cursor: "pointer",
    zIndex: 10,
    transition: "all 0.3s ease",
    color: "#333",
    fontSize: "16px",
  },
  arrowLeft: {
    left: "10px",
  },
  arrowRight: {
    right: "10px",
  },
  skeleton: {
    borderRadius: "14px",
    overflow: "hidden",
    backgroundColor: "#e5e7eb",
    flexShrink: 0,
  },
  skeletonImage: {
    width: "100%",
    paddingTop: "56%",
    backgroundColor: "#d1d5db",
    animation: "pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite",
  },
  emptyState: {
    textAlign: "center",
    padding: "40px 20px",
    color: "#9ca3af",
    backgroundColor: "#f9fafb",
    borderRadius: "12px",
  },
};

// ============================================
// PROMO CARD BİLEŞENİ
// ============================================

/**
 * Tek bir promosyon kartı
 */
const PromoCard = memo(function PromoCard({ promo, index }) {
  const [isHovered, setIsHovered] = useState(false);
  const [imageError, setImageError] = useState(false);

  const handleImageError = useCallback(() => {
    setImageError(true);
  }, []);

  const imageSrc = imageError
    ? PLACEHOLDER_IMAGE
    : promo.imageUrl || PLACEHOLDER_IMAGE;

  const cardStyle = {
    ...styles.card,
    ...(isHovered ? styles.cardHover : {}),
  };

  const imageStyle = {
    ...styles.image,
    ...(isHovered ? styles.imageHover : {}),
  };

  // Link veya div olarak render
  const CardWrapper = promo.linkUrl ? "a" : "div";
  const linkProps = promo.linkUrl ? { href: promo.linkUrl } : {};

  return (
    <CardWrapper
      {...linkProps}
      className="promo-card-item"
      style={cardStyle}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      role="article"
      aria-label={promo.title || "Promosyon"}
    >
      {/* Görsel Container */}
      <div className="promo-card-image-container" style={styles.imageContainer}>
        <img
          src={imageSrc}
          alt={promo.title || "Promosyon görseli"}
          className="promo-card-image"
          style={imageStyle}
          loading={index < 4 ? "eager" : "lazy"} // İlk 4 görsel hemen yükle
          onError={handleImageError}
        />

        {/* Badge (opsiyonel) */}
        {promo.badge && <span style={styles.badge}>{promo.badge}</span>}
      </div>

      {/* Title Overlay (opsiyonel) */}
      {promo.title && (
        <div style={styles.overlay}>
          <h3 style={styles.cardTitle} title={promo.title}>
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

// ============================================
// SKELETON CARD BİLEŞENİ
// ============================================

const SkeletonCard = memo(function SkeletonCard() {
  return (
    <div style={styles.skeleton}>
      <div style={styles.skeletonImage}></div>
    </div>
  );
});

// ============================================
// ANA BİLEŞEN - CAROUSEL YAPISINA DÖNÜŞTÜRÜLDÜ
// ============================================

/**
 * PromoCards - Promosyon kartları carousel bileşeni
 *
 * @param {Array} promos - Promosyon listesi
 * @param {boolean} loading - Yükleme durumu
 * @param {string} title - Bölüm başlığı
 * @param {string} icon - Font Awesome ikon sınıfı
 * @param {number} columns - Minimum kolon sayısı (kullanılmıyor - carousel)
 * @param {boolean} showTitle - Başlığı göster
 */
function PromoCards({
  promos = [],
  loading = false,
  title = "Kampanyalar",
  icon = "fa-tags",
  columns = 4,
  showTitle = true,
}) {
  // =====================================================
  // CAROUSEL SCROLL KONTROL
  // =====================================================
  const scrollContainerRef = useRef(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(true);

  // Scroll durumunu kontrol et
  const checkScroll = useCallback(() => {
    const container = scrollContainerRef.current;
    if (container) {
      setCanScrollLeft(container.scrollLeft > 10);
      setCanScrollRight(
        container.scrollLeft <
          container.scrollWidth - container.clientWidth - 10,
      );
    }
  }, []);

  // Kaydırma fonksiyonu
  const scroll = useCallback(
    (direction) => {
      const container = scrollContainerRef.current;
      if (container) {
        // Her kaydırmada bir kart genişliği kadar kaydır
        const scrollAmount = 280;
        container.scrollBy({
          left: direction === "left" ? -scrollAmount : scrollAmount,
          behavior: "smooth",
        });
        setTimeout(checkScroll, 350);
      }
    },
    [checkScroll],
  );

  // Component mount ve data değişiminde scroll durumunu kontrol et
  useEffect(() => {
    checkScroll();
    window.addEventListener("resize", checkScroll);
    return () => window.removeEventListener("resize", checkScroll);
  }, [checkScroll, promos]);

  // ============================================
  // LOADING STATE
  // ============================================

  if (loading) {
    return (
      <section style={styles.section}>
        {showTitle && (
          <div style={styles.header}>
            <h2 style={styles.title}>
              <i className={`fas ${icon}`} style={styles.icon}></i>
              {title}
            </h2>
          </div>
        )}
        <div
          className="promo-cards-carousel-wrapper"
          style={styles.carouselWrapper}
        >
          <div
            className="promo-cards-scroll-container"
            style={styles.scrollContainer}
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

  // ============================================
  // EMPTY STATE
  // ============================================

  if (!promos || promos.length === 0) {
    return null; // Promo yoksa bölümü gösterme
  }

  // ============================================
  // ANA RENDER - CAROUSEL
  // ============================================

  return (
    <section style={styles.section} className="promo-cards-section">
      {/* Başlık */}
      {showTitle && (
        <div style={styles.header}>
          <h2 style={styles.title}>
            <i className={`fas ${icon}`} style={styles.icon}></i>
            {title}
          </h2>
        </div>
      )}

      {/* Carousel Wrapper - Ok butonları dahil */}
      <div
        className="promo-cards-carousel-wrapper"
        style={styles.carouselWrapper}
      >
        {/* Sol Ok - Her zaman görünür, scroll yoksa soluk */}
        {promos.length > 4 && (
          <button
            className={`promo-cards-arrow promo-cards-arrow-left ${!canScrollLeft ? "disabled-arrow" : ""}`}
            style={{
              ...styles.arrow,
              ...styles.arrowLeft,
              opacity: canScrollLeft ? 1 : 0.3,
              cursor: canScrollLeft ? "pointer" : "default",
            }}
            onClick={() => canScrollLeft && scroll("left")}
            aria-label="Sola kaydır"
            disabled={!canScrollLeft}
          >
            <i className="fas fa-chevron-left"></i>
          </button>
        )}

        {/* Scroll Container */}
        <div
          className="promo-cards-scroll-container"
          style={styles.scrollContainer}
          ref={scrollContainerRef}
          onScroll={checkScroll}
          role="list"
          aria-label={title}
        >
          {promos.map((promo, index) => (
            <PromoCard key={promo.id} promo={promo} index={index} />
          ))}
        </div>

        {/* Sağ Ok - Her zaman görünür, scroll yoksa soluk */}
        {promos.length > 4 && (
          <button
            className={`promo-cards-arrow promo-cards-arrow-right ${!canScrollRight ? "disabled-arrow" : ""}`}
            style={{
              ...styles.arrow,
              ...styles.arrowRight,
              opacity: canScrollRight ? 1 : 0.3,
              cursor: canScrollRight ? "pointer" : "default",
            }}
            onClick={() => canScrollRight && scroll("right")}
            aria-label="Sağa kaydır"
            disabled={!canScrollRight}
          >
            <i className="fas fa-chevron-right"></i>
          </button>
        )}
      </div>
    </section>
  );
}

// ============================================
// PROP TYPES
// ============================================

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
