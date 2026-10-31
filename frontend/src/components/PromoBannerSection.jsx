import React, { memo, useMemo } from "react";
import PropTypes from "prop-types";
import { Link } from "react-router-dom";
import BannerMedia from "./BannerMedia";
import bannerService from "../services/bannerService";
import {
  useAutoSlideCarousel,
  useInViewMotion,
} from "../hooks/useAutoSlideCarousel";
import "./PromoBannerSection.css";

/** Web geniş ekranda taşma + seamless loop için yeterli kart. */
const MIN_SLIDE_CARDS = 14;
const LOOP_COPIES = 3;

function getPromoTargetUrl(promo) {
  const raw = promo.link || promo.linkUrl;
  if (raw && raw !== "#") return raw;

  const t = (promo.title || "").toLowerCase();
  if (t.includes("meyve") || t.includes("sebze")) return "/category/meyve-ve-sebze";
  if (t.includes("et") || t.includes("butcher") || t.includes("kasap") || t.includes("tavuk")) return "/category/et-ve-et-urunleri";
  if (t.includes("süt") || t.includes("sut") || t.includes("peynir") || t.includes("yoğurt") || t.includes("yogurt")) return "/category/sut-ve-sut-urunleri";
  if (t.includes("temizlik") || t.includes("deterjan")) return "/category/ev-ve-mutfak";
  if (t.includes("indirim") || t.includes("kaçırma") || t.includes("kacirma") || t.includes("fırsat")) return "/indirimli-urunler";
  if (t.includes("günlük") || t.includes("gunluk") || t.includes("gurme")) return "/kategoriler";

  return "/kategoriler";
}

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

                const targetUrl = getPromoTargetUrl(promo);
                const isExternal = /^https?:\/\//i.test(targetUrl);
                const CardWrapper = isExternal ? "a" : Link;
                const wrapperProps = isExternal
                  ? {
                      href: targetUrl,
                      target: "_blank",
                      rel: "noopener noreferrer",
                      onClick: () => promo.id && bannerService.trackBannerClick(promo.id),
                    }
                  : {
                      to: targetUrl,
                      onClick: () => promo.id && bannerService.trackBannerClick(promo.id),
                    };

                return (
                  <CardWrapper
                    key={`${promo.id}-${index}`}
                    className={`promo-grid-item is-clickable carousel-slide-card${hasEntered ? " promo-banner-item--enter is-animated" : ""}`}
                    style={{
                      "--enter-delay": `${enterDelayMs}ms`,
                      textDecoration: "none",
                      display: "block",
                      cursor: "pointer",
                    }}
                    role="link"
                    aria-label={promo.title || "Promosyon Ürünlerini İncele"}
                    {...wrapperProps}
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
                  </CardWrapper>
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
