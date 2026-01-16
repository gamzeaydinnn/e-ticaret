/**
 * PromoCards.jsx - Ana Sayfa Promosyon Kartları Bileşeni
 *
 * Bu bileşen, ana sayfadaki promosyon/kampanya kartlarını grid layout ile render eder.
 *
 * Özellikler:
 * - Responsive grid layout (2-4 kolon)
 * - Hover efektleri
 * - Link desteği
 * - Lazy loading (performans için)
 * - Placeholder/skeleton (yükleme durumu)
 * - Fallback görsel (hata durumu)
 * - Erişilebilirlik (ARIA etiketleri)
 *
 * @author Senior Developer
 * @version 1.0.0
 */

import React, { useState, useCallback, memo } from "react";
import PropTypes from "prop-types";

// ============================================
// SABİTLER
// ============================================

/** Varsayılan placeholder görseli */
const PLACEHOLDER_IMAGE = "/images/placeholder.png";

/** Skeleton sayısı (yükleme durumunda gösterilecek) */
const SKELETON_COUNT = 4;

// ============================================
// STIL SABİTLERİ
// ============================================

const styles = {
  section: {
    marginBottom: "24px",
  },
  header: {
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    marginBottom: "16px",
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
  grid: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fill, minmax(140px, 180px))",
    gap: "14px",
    justifyContent: "center",
    maxWidth: "1200px",
    margin: "0 auto",
  },
  card: {
    position: "relative",
    display: "block",
    borderRadius: "14px",
    overflow: "hidden",
    boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
    transition: "transform 0.3s ease, box-shadow 0.3s ease",
    backgroundColor: "#fff",
    textDecoration: "none",
    maxWidth: "180px",
    margin: "0 auto",
  },
  cardHover: {
    transform: "translateY(-4px)",
    boxShadow: "0 8px 20px rgba(0,0,0,0.12)",
  },
  imageContainer: {
    position: "relative",
    width: "100%",
    paddingTop: "75%", // 4:3 aspect ratio - daha kompakt
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
  skeleton: {
    borderRadius: "14px",
    overflow: "hidden",
    backgroundColor: "#e5e7eb",
    maxWidth: "180px",
    margin: "0 auto",
  },
  skeletonImage: {
    width: "100%",
    paddingTop: "75%",
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
      style={cardStyle}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      role="article"
      aria-label={promo.title || "Promosyon"}
    >
      {/* Görsel Container */}
      <div style={styles.imageContainer}>
        <img
          src={imageSrc}
          alt={promo.title || "Promosyon görseli"}
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
// ANA BİLEŞEN
// ============================================

/**
 * PromoCards - Promosyon kartları grid bileşeni
 *
 * @param {Array} promos - Promosyon listesi
 * @param {boolean} loading - Yükleme durumu
 * @param {string} title - Bölüm başlığı
 * @param {string} icon - Font Awesome ikon sınıfı
 * @param {number} columns - Minimum kolon sayısı
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
        <div style={styles.grid} aria-label="Kampanyalar yükleniyor">
          {Array.from({ length: SKELETON_COUNT }).map((_, index) => (
            <SkeletonCard key={`skeleton-${index}`} />
          ))}
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
  // ANA RENDER
  // ============================================

  // Grid template columns hesapla
  const gridStyle = {
    ...styles.grid,
    gridTemplateColumns: `repeat(auto-fill, minmax(${Math.floor(
      280 / columns
    )}px, 1fr))`,
  };

  return (
    <section style={styles.section}>
      {/* Başlık */}
      {showTitle && (
        <div style={styles.header}>
          <h2 style={styles.title}>
            <i className={`fas ${icon}`} style={styles.icon}></i>
            {title}
          </h2>
        </div>
      )}

      {/* Promo Grid */}
      <div style={gridStyle} role="list" aria-label={title}>
        {promos.map((promo, index) => (
          <PromoCard key={promo.id} promo={promo} index={index} />
        ))}
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
    })
  ),
  loading: PropTypes.bool,
  title: PropTypes.string,
  icon: PropTypes.string,
  columns: PropTypes.number,
  showTitle: PropTypes.bool,
};

export default memo(PromoCards);
