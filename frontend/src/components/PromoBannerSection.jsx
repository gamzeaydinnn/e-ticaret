import React, { memo, useMemo } from "react";
import PropTypes from "prop-types";
import BannerMedia from "./BannerMedia";
import {
  useAutoSlideCarousel,
  useInViewMotion,
} from "../hooks/useAutoSlideCarousel";
import "./PromoBannerSection.css";

/** Web geniş ekranda taşma + seamless loop için yeterli kart. */
const MIN_SLIDE_CARDS = 14;
const LOOP_COPIES = 3;

function buildLoopSlides(promos) {
  if (!promos?.length) return { slides: [], copies: 0 };
  const slides = [];
  while (slides.length < Math.max(MIN_SLIDE_CARDS, promos.length * LOOP_COPIES)) {
    slides.push(...promos);
  }
  // Tam set sayısı (seamless reset için)
  const copies = Math.max(LOOP_COPIES, Math.floor(slides.length / promos.length));
  while (slides.length < promos.length * copies) {
    slides.push(...promos);
  }
  return { slides, copies };
}

/**
 * Ana sayfa promo banner alanı — sürekli kayan animasyon.
 */
function PromoBannerSection({ promos = [], className = "" }) {
  const { sectionRef, hasEntered, isMotionActive } = useInViewMotion({
    threshold: 0.01,
  });

  const { slides: slidePromos, copies: loopCopies } = useMemo(
    () => buildLoopSlides(promos),
    [promos],
  );

  const {
    scrollContainerRef,
    trackRef,
    canScrollLeft,
    canScrollRight,
    scroll,
  } = useAutoSlideCarousel({
    // Promo her zaman kaymalı (Intersection gecikmesi animasyonu kesmesin)
    motionActive: true,
    cardSelector: ".promo-grid-item",
    itemCount: slidePromos.length,
    seamlessLoop: true,
    loopCopies,
  });

  if (!promos || promos.length === 0) {
    return null;
  }

  const showArrows = slidePromos.length > 1;

  return (
    <section
      ref={sectionRef}
      className={`promo-section py-3 promo-banner-section ${hasEntered ? "is-in-view" : ""}${isMotionActive || hasEntered ? " is-motion-active" : ""} ${className}`.trim()}
      style={{ background: "#f8f9fa" }}
      aria-label="Promosyon bannerları"
    >
      <div className="container-fluid px-4">
        <div className="promo-banner-slider-wrapper">
          {showArrows && canScrollLeft && (
            <button
              type="button"
              className="promo-banner-arrow promo-banner-arrow-left"
              onClick={() => scroll("left")}
              aria-label="Sola kaydır"
            >
              <i className="fas fa-chevron-left" aria-hidden="true" />
            </button>
          )}

          <div
            className="promo-banner-scroll-container"
            ref={scrollContainerRef}
          >
            <div className="promo-banner-scroll-track" ref={trackRef}>
              {slidePromos.map((promo, index) => {
                const enterDelayMs = Math.min(index, 10) * 45;
                const handleClick = () => {
                  if (promo.link) {
                    window.location.href = promo.link;
                  }
                };
                const handleKeyDown = (event) => {
                  if (
                    (event.key === "Enter" || event.key === " ") &&
                    promo.link
                  ) {
                    event.preventDefault();
                    window.location.href = promo.link;
                  }
                };

                return (
                  <div
                    key={`${promo.id}-${index}`}
                    className={`promo-grid-item carousel-slide-card${hasEntered ? " promo-banner-item--enter is-animated" : ""}`}
                    style={{ "--enter-delay": `${enterDelayMs}ms` }}
                    role={promo.link ? "link" : "presentation"}
                    tabIndex={promo.link ? 0 : undefined}
                    onClick={handleClick}
                    onKeyDown={handleKeyDown}
                  >
                    <BannerMedia
                      src={promo.image}
                      alt={promo.title || "Promosyon"}
                      className="promo-grid-image"
                      onError={(e) => {
                        if (e?.target?.tagName === "IMG") {
                          e.target.src = "/images/placeholder.png";
                        }
                      }}
                    />
                  </div>
                );
              })}
            </div>
          </div>

          {showArrows && canScrollRight && (
            <button
              type="button"
              className="promo-banner-arrow promo-banner-arrow-right"
              onClick={() => scroll("right")}
              aria-label="Sağa kaydır"
            >
              <i className="fas fa-chevron-right" aria-hidden="true" />
            </button>
          )}
        </div>
      </div>
    </section>
  );
}

PromoBannerSection.propTypes = {
  promos: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
      title: PropTypes.string,
      image: PropTypes.string,
      link: PropTypes.string,
    }),
  ),
  className: PropTypes.string,
};

export default memo(PromoBannerSection);
