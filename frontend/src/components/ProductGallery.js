import React, { useEffect, useMemo, useState } from "react";
import "./ProductGallery.css";

const PLACEHOLDER = "/images/placeholder.png";

export default function ProductGallery({
  images = [],
  alt = "Ürün görseli",
  className = "",
}) {
  const normalizedImages = useMemo(
    () =>
      (Array.isArray(images) ? images : [])
        .map((src) => (typeof src === "string" ? src.trim() : ""))
        .filter(Boolean),
    [images],
  );

  const [activeIndex, setActiveIndex] = useState(0);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    setActiveIndex(0);
    setLoaded(false);
  }, [normalizedImages.join("|")]);

  const hasImages = normalizedImages.length > 0;
  const activeImage = hasImages ? normalizedImages[activeIndex] : PLACEHOLDER;
  const showNav = normalizedImages.length > 1;

  const goPrev = () => {
    setLoaded(false);
    setActiveIndex(
      (prev) => (prev - 1 + normalizedImages.length) % normalizedImages.length,
    );
  };

  const goNext = () => {
    setLoaded(false);
    setActiveIndex((prev) => (prev + 1) % normalizedImages.length);
  };

  return (
    <div className={`product-gallery ${className}`.trim()}>
      <div className="product-gallery__main">
        {!loaded && (
          <div className="product-gallery__placeholder">
            <i className="fas fa-image"></i>
            <span>Görsel yükleniyor...</span>
          </div>
        )}

        <img
          src={activeImage}
          alt={alt}
          className={`product-gallery__main-image ${
            loaded ? "product-gallery__main-image--loaded" : ""
          }`}
          onLoad={() => setLoaded(true)}
          onError={(e) => {
            e.currentTarget.src = PLACEHOLDER;
            setLoaded(true);
          }}
        />

        {showNav && (
          <>
            <button
              type="button"
              className="product-gallery__nav product-gallery__nav--prev"
              onClick={goPrev}
              aria-label="Önceki görsel"
            >
              <i className="fas fa-chevron-left"></i>
            </button>
            <button
              type="button"
              className="product-gallery__nav product-gallery__nav--next"
              onClick={goNext}
              aria-label="Sonraki görsel"
            >
              <i className="fas fa-chevron-right"></i>
            </button>
            <span className="product-gallery__counter">
              {activeIndex + 1} / {normalizedImages.length}
            </span>
          </>
        )}
      </div>

      {showNav && (
        <div className="product-gallery__thumbs">
          {normalizedImages.map((src, index) => (
            <button
              key={`${src}-${index}`}
              type="button"
              className={`product-gallery__thumb ${
                index === activeIndex ? "product-gallery__thumb--active" : ""
              }`}
              onClick={() => {
                setLoaded(false);
                setActiveIndex(index);
              }}
              aria-label={`Görsel ${index + 1}`}
            >
              <img src={src} alt="" />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
