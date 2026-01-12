/**
 * HeroSlider.jsx - Ana Sayfa Hero Slider Bileşeni
 *
 * Bu bileşen, ana sayfadaki büyük banner slider'ını render eder.
 *
 * Özellikler:
 * - Otomatik geçiş (5 saniye)
 * - Ok navigasyonu (sol/sağ)
 * - Dot navigasyonu
 * - Touch/Swipe desteği (mobil için)
 * - Lazy loading (performans için)
 * - Placeholder/skeleton (yükleme durumu)
 * - Fallback görsel (hata durumu)
 * - Erişilebilirlik (ARIA etiketleri)
 *
 * @author Senior Developer
 * @version 1.0.0
 */

import React, { useState, useEffect, useCallback, useRef, memo } from "react";
import PropTypes from "prop-types";

// ============================================
// SABİTLER
// ============================================

/** Otomatik geçiş süresi (milisaniye) */
const AUTO_SLIDE_INTERVAL = 5000;

/** Minimum swipe mesafesi (piksel) */
const MIN_SWIPE_DISTANCE = 50;

/** Varsayılan placeholder görseli */
const PLACEHOLDER_IMAGE = "/images/placeholder.png";

// ============================================
// STIL SABİTLERİ
// ============================================

const styles = {
  container: {
    position: "relative",
    borderRadius: "12px",
    overflow: "hidden",
    boxShadow: "0 4px 12px rgba(0,0,0,0.1)",
    backgroundColor: "#f3f4f6",
  },
  slideContainer: {
    position: "relative",
    width: "100%",
    height: "300px",
    // Mobil için responsive height - CSS media query kullanılabilir
  },
  slide: (isActive) => ({
    position: "absolute",
    top: 0,
    left: 0,
    width: "100%",
    height: "100%",
    opacity: isActive ? 1 : 0,
    visibility: isActive ? "visible" : "hidden",
    transition: "opacity 0.5s ease-in-out, visibility 0.5s",
    zIndex: isActive ? 1 : 0,
  }),
  slideLink: {
    display: "block",
    width: "100%",
    height: "100%",
    textDecoration: "none",
  },
  slideImage: {
    width: "100%",
    height: "100%",
    objectFit: "cover",
    display: "block",
  },
  slideContent: {
    position: "absolute",
    bottom: 0,
    left: 0,
    right: 0,
    padding: "40px 24px 24px",
    background: "linear-gradient(transparent, rgba(0,0,0,0.7))",
    color: "white",
  },
  slideTitle: {
    fontSize: "1.5rem",
    fontWeight: "bold",
    marginBottom: "8px",
    textShadow: "0 2px 4px rgba(0,0,0,0.3)",
  },
  slideSubtitle: {
    fontSize: "1rem",
    opacity: 0.9,
    marginBottom: "12px",
  },
  slideButton: {
    display: "inline-block",
    padding: "8px 20px",
    backgroundColor: "#f97316",
    color: "white",
    borderRadius: "20px",
    fontSize: "0.9rem",
    fontWeight: "600",
    textDecoration: "none",
    transition: "background-color 0.2s",
  },
  navButton: (position) => ({
    position: "absolute",
    [position]: "12px",
    top: "50%",
    transform: "translateY(-50%)",
    background: "rgba(0,0,0,0.5)",
    color: "white",
    border: "none",
    borderRadius: "50%",
    width: "44px",
    height: "44px",
    cursor: "pointer",
    zIndex: 10,
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    transition: "background-color 0.2s, transform 0.2s",
    fontSize: "1rem",
  }),
  dotsContainer: {
    position: "absolute",
    bottom: "16px",
    left: "50%",
    transform: "translateX(-50%)",
    display: "flex",
    gap: "10px",
    zIndex: 10,
  },
  dot: (isActive) => ({
    width: "12px",
    height: "12px",
    borderRadius: "50%",
    border: "2px solid white",
    backgroundColor: isActive ? "white" : "transparent",
    cursor: "pointer",
    transition: "all 0.3s ease",
    boxShadow: "0 2px 4px rgba(0,0,0,0.2)",
  }),
  skeleton: {
    width: "100%",
    height: "300px",
    backgroundColor: "#e5e7eb",
    borderRadius: "12px",
    animation: "pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite",
  },
  emptyState: {
    background: "linear-gradient(135deg, #38bdf8 0%, #6366f1 100%)",
    borderRadius: "12px",
    padding: "48px 32px",
    color: "white",
    textAlign: "center",
  },
  emptyTitle: {
    fontSize: "1.75rem",
    fontWeight: "bold",
    marginBottom: "12px",
  },
  emptySubtitle: {
    opacity: 0.9,
    fontSize: "1.1rem",
  },
};

// ============================================
// ANA BİLEŞEN
// ============================================

/**
 * HeroSlider - Ana sayfa slider bileşeni
 *
 * @param {Array} banners - Banner listesi
 * @param {boolean} loading - Yükleme durumu
 * @param {number} autoSlideInterval - Otomatik geçiş süresi (ms)
 * @param {boolean} showNavigation - Ok navigasyonu göster
 * @param {boolean} showDots - Dot navigasyonu göster
 * @param {boolean} showContent - Slider içeriğini göster (title, subtitle, button)
 * @param {Function} onSlideChange - Slide değiştiğinde callback
 */
function HeroSlider({
  banners = [],
  loading = false,
  autoSlideInterval = AUTO_SLIDE_INTERVAL,
  showNavigation = true,
  showDots = true,
  showContent = false,
  onSlideChange = null,
}) {
  // ============================================
  // STATE YÖNETİMİ
  // ============================================

  const [currentSlide, setCurrentSlide] = useState(0);
  const [isPaused, setIsPaused] = useState(false);
  const [imageErrors, setImageErrors] = useState({});

  // Touch/Swipe için ref ve state
  const containerRef = useRef(null);
  const touchStartX = useRef(0);
  const touchEndX = useRef(0);

  // ============================================
  // OTOMATİK GEÇİŞ
  // ============================================

  useEffect(() => {
    // Banner yoksa veya tek banner varsa auto-slide devre dışı
    if (banners.length <= 1 || isPaused) return;

    const timer = setInterval(() => {
      setCurrentSlide((prev) => (prev + 1) % banners.length);
    }, autoSlideInterval);

    return () => clearInterval(timer);
  }, [banners.length, autoSlideInterval, isPaused]);

  // ============================================
  // SLIDE DEĞİŞİM CALLBACK
  // ============================================

  useEffect(() => {
    if (onSlideChange && banners.length > 0) {
      onSlideChange(currentSlide, banners[currentSlide]);
    }
  }, [currentSlide, banners, onSlideChange]);

  // ============================================
  // NAVİGASYON FONKSİYONLARI
  // ============================================

  /** Önceki slide'a git */
  const goToPrevious = useCallback(() => {
    setCurrentSlide((prev) => (prev - 1 + banners.length) % banners.length);
  }, [banners.length]);

  /** Sonraki slide'a git */
  const goToNext = useCallback(() => {
    setCurrentSlide((prev) => (prev + 1) % banners.length);
  }, [banners.length]);

  /** Belirli bir slide'a git */
  const goToSlide = useCallback(
    (index) => {
      if (index >= 0 && index < banners.length) {
        setCurrentSlide(index);
      }
    },
    [banners.length]
  );

  // ============================================
  // TOUCH/SWIPE DESTEĞI
  // ============================================

  const handleTouchStart = useCallback((e) => {
    touchStartX.current = e.touches[0].clientX;
    setIsPaused(true); // Dokunma sırasında auto-slide'ı durdur
  }, []);

  const handleTouchMove = useCallback((e) => {
    touchEndX.current = e.touches[0].clientX;
  }, []);

  const handleTouchEnd = useCallback(() => {
    const distance = touchStartX.current - touchEndX.current;

    if (Math.abs(distance) > MIN_SWIPE_DISTANCE) {
      if (distance > 0) {
        // Sola kaydırma - sonraki slide
        goToNext();
      } else {
        // Sağa kaydırma - önceki slide
        goToPrevious();
      }
    }

    // 1 saniye sonra auto-slide'ı tekrar başlat
    setTimeout(() => setIsPaused(false), 1000);
  }, [goToNext, goToPrevious]);

  // ============================================
  // GÖRSEL HATA YÖNETİMİ
  // ============================================

  const handleImageError = useCallback((bannerId) => {
    setImageErrors((prev) => ({ ...prev, [bannerId]: true }));
  }, []);

  const getImageSrc = useCallback(
    (banner) => {
      if (imageErrors[banner.id]) {
        return PLACEHOLDER_IMAGE;
      }
      return banner.imageUrl || PLACEHOLDER_IMAGE;
    },
    [imageErrors]
  );

  // ============================================
  // LOADING STATE
  // ============================================

  if (loading) {
    return (
      <div style={styles.skeleton} aria-label="Slider yükleniyor">
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            height: "100%",
            color: "#9ca3af",
          }}
        >
          <i className="fas fa-spinner fa-spin fa-2x"></i>
        </div>
      </div>
    );
  }

  // ============================================
  // EMPTY STATE
  // ============================================

  if (!banners || banners.length === 0) {
    return (
      <div style={styles.emptyState}>
        <h1 style={styles.emptyTitle}>
          <i className="fas fa-shopping-basket me-2"></i>
          Bugün ne sipariş ediyorsun?
        </h1>
        <p style={styles.emptySubtitle}>
          Hızlı teslimat — Taze ürünler — Güvenli ödeme
        </p>
      </div>
    );
  }

  // ============================================
  // ANA RENDER
  // ============================================

  return (
    <div
      ref={containerRef}
      style={styles.container}
      onMouseEnter={() => setIsPaused(true)}
      onMouseLeave={() => setIsPaused(false)}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
      role="region"
      aria-label="Ana sayfa slider"
      aria-roledescription="carousel"
    >
      {/* Slide Container */}
      <div style={styles.slideContainer}>
        {banners.map((banner, index) => {
          const isActive = index === currentSlide;

          return (
            <div
              key={banner.id}
              style={styles.slide(isActive)}
              role="group"
              aria-roledescription="slide"
              aria-label={`${index + 1} / ${banners.length}: ${
                banner.title || "Banner"
              }`}
              aria-hidden={!isActive}
            >
              {/* Banner Link veya Div */}
              {banner.linkUrl ? (
                <a
                  href={banner.linkUrl}
                  style={styles.slideLink}
                  tabIndex={isActive ? 0 : -1}
                >
                  <img
                    src={getImageSrc(banner)}
                    alt={banner.title || "Banner"}
                    style={styles.slideImage}
                    loading={index === 0 ? "eager" : "lazy"}
                    onError={() => handleImageError(banner.id)}
                  />
                </a>
              ) : (
                <img
                  src={getImageSrc(banner)}
                  alt={banner.title || "Banner"}
                  style={styles.slideImage}
                  loading={index === 0 ? "eager" : "lazy"}
                  onError={() => handleImageError(banner.id)}
                />
              )}

              {/* Slider Content (opsiyonel) */}
              {showContent && (banner.title || banner.subTitle) && (
                <div style={styles.slideContent}>
                  {banner.title && (
                    <h2 style={styles.slideTitle}>{banner.title}</h2>
                  )}
                  {banner.subTitle && (
                    <p style={styles.slideSubtitle}>{banner.subTitle}</p>
                  )}
                  {banner.buttonText && banner.linkUrl && (
                    <a
                      href={banner.linkUrl}
                      style={styles.slideButton}
                      tabIndex={isActive ? 0 : -1}
                    >
                      {banner.buttonText}
                    </a>
                  )}
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Navigation Arrows */}
      {showNavigation && banners.length > 1 && (
        <>
          <button
            onClick={goToPrevious}
            style={styles.navButton("left")}
            aria-label="Önceki slide"
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = "rgba(0,0,0,0.7)";
              e.currentTarget.style.transform = "translateY(-50%) scale(1.1)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = "rgba(0,0,0,0.5)";
              e.currentTarget.style.transform = "translateY(-50%)";
            }}
          >
            <i className="fas fa-chevron-left"></i>
          </button>
          <button
            onClick={goToNext}
            style={styles.navButton("right")}
            aria-label="Sonraki slide"
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = "rgba(0,0,0,0.7)";
              e.currentTarget.style.transform = "translateY(-50%) scale(1.1)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = "rgba(0,0,0,0.5)";
              e.currentTarget.style.transform = "translateY(-50%)";
            }}
          >
            <i className="fas fa-chevron-right"></i>
          </button>
        </>
      )}

      {/* Navigation Dots */}
      {showDots && banners.length > 1 && (
        <div
          style={styles.dotsContainer}
          role="tablist"
          aria-label="Slide seçimi"
        >
          {banners.map((_, index) => (
            <button
              key={index}
              onClick={() => goToSlide(index)}
              style={styles.dot(index === currentSlide)}
              role="tab"
              aria-selected={index === currentSlide}
              aria-label={`Slide ${index + 1}`}
              onMouseEnter={(e) => {
                if (index !== currentSlide) {
                  e.currentTarget.style.backgroundColor =
                    "rgba(255,255,255,0.5)";
                }
              }}
              onMouseLeave={(e) => {
                if (index !== currentSlide) {
                  e.currentTarget.style.backgroundColor = "transparent";
                }
              }}
            />
          ))}
        </div>
      )}
    </div>
  );
}

// ============================================
// PROP TYPES
// ============================================

HeroSlider.propTypes = {
  banners: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
      imageUrl: PropTypes.string.isRequired,
      title: PropTypes.string,
      subTitle: PropTypes.string,
      buttonText: PropTypes.string,
      linkUrl: PropTypes.string,
    })
  ),
  loading: PropTypes.bool,
  autoSlideInterval: PropTypes.number,
  showNavigation: PropTypes.bool,
  showDots: PropTypes.bool,
  showContent: PropTypes.bool,
  onSlideChange: PropTypes.func,
};

// Memo ile gereksiz re-render'ları önle
export default memo(HeroSlider);
