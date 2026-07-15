import { useCallback, useEffect, useRef, useState } from "react";

/**
 * Yatay transform tabanlı otomatik kaydırma (ürün blokları ile aynı mantık).
 * Tıklama / dokunma sırasında duraklar; prefers-reduced-motion'a saygı duyar.
 *
 * @param {boolean} seamlessLoop - true ise uçtan uca sürekli ileri kayar (promo banner).
 * @param {number} loopCopies - seamlessLoop için track içinde kaç set çoğaltıldı.
 */
export function useAutoSlideCarousel({
  motionActive = false,
  cardSelector = ".carousel-slide-card",
  itemCount = 0,
  seamlessLoop = false,
  loopCopies = 2,
} = {}) {
  const scrollContainerRef = useRef(null);
  const trackRef = useRef(null);
  const autoScrollRafRef = useRef(null);
  const userInteractUntilRef = useRef(0);
  const slideOffsetRef = useRef(0);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(true);

  const readMaxOffset = useCallback(() => {
    const container = scrollContainerRef.current;
    const track = trackRef.current;
    if (!container || !track) return 0;
    return Math.max(0, track.scrollWidth - container.clientWidth);
  }, []);

  const readLoopSpan = useCallback(() => {
    const track = trackRef.current;
    if (!track) return 0;
    const copies = Math.max(2, loopCopies);
    return track.scrollWidth / copies;
  }, [loopCopies]);

  const applyOffset = useCallback(
    (value, { looping = false } = {}) => {
      const track = trackRef.current;
      if (!track) return 0;

      let clamped;
      if (looping) {
        const span = readLoopSpan();
        if (span <= 0) {
          clamped = 0;
        } else {
          clamped = value % span;
          if (clamped < 0) clamped += span;
        }
      } else {
        const maxOffset = readMaxOffset();
        clamped = Math.max(0, Math.min(maxOffset, value));
      }

      slideOffsetRef.current = clamped;
      track.style.transform = `translate3d(${-clamped}px, 0, 0)`;

      const maxOffset = readMaxOffset();
      const nextLeft = clamped > 2;
      const nextRight = looping
        ? maxOffset > 8
        : clamped < maxOffset - 2;
      setCanScrollLeft((prev) => (prev === nextLeft ? prev : nextLeft));
      setCanScrollRight((prev) => (prev === nextRight ? prev : nextRight));
      return clamped;
    },
    [readLoopSpan, readMaxOffset],
  );

  const checkScroll = useCallback(() => {
    const maxOffset = readMaxOffset();
    const offset = slideOffsetRef.current;
    setCanScrollLeft(offset > 2);
    setCanScrollRight(seamlessLoop ? maxOffset > 8 : offset < maxOffset - 2);
  }, [readMaxOffset, seamlessLoop]);

  const scroll = useCallback(
    (direction) => {
      const container = scrollContainerRef.current;
      const track = trackRef.current;
      if (!container || !track) return;

      userInteractUntilRef.current = Date.now() + 5000;

      const card = track.querySelector(cardSelector);
      const cardWidth = card?.offsetWidth || 200;
      const gap = parseFloat(getComputedStyle(track).gap) || 14;
      const scrollAmount = Math.max(cardWidth + gap, 120);

      const next =
        direction === "left"
          ? slideOffsetRef.current - scrollAmount
          : slideOffsetRef.current + scrollAmount;

      track.style.transition = "transform 0.35s cubic-bezier(0.22, 1, 0.36, 1)";
      applyOffset(next, { looping: seamlessLoop });

      window.setTimeout(() => {
        if (track) track.style.transition = "";
        checkScroll();
      }, 380);
    },
    [applyOffset, cardSelector, checkScroll, seamlessLoop],
  );

  useEffect(() => {
    checkScroll();
    window.addEventListener("resize", checkScroll);
    return () => window.removeEventListener("resize", checkScroll);
  }, [checkScroll, itemCount]);

  useEffect(() => {
    const container = scrollContainerRef.current;
    const track = trackRef.current;
    if (!container || !track || !motionActive) return undefined;

    const prefersReducedMotion = window.matchMedia?.(
      "(prefers-reduced-motion: reduce)",
    )?.matches;
    if (prefersReducedMotion) return undefined;

    const pauseAutoScroll = (ms = 4200) => {
      userInteractUntilRef.current = Date.now() + ms;
    };

    const onUserInteract = (event) => {
      if (event.target?.closest?.("button, a")) {
        pauseAutoScroll(5500);
        return;
      }
      pauseAutoScroll(4500);
    };

    container.classList.add("is-auto-scrolling");
    track.style.willChange = "transform";
    applyOffset(slideOffsetRef.current, { looping: seamlessLoop });

    container.addEventListener("pointerdown", onUserInteract, { passive: true });
    container.addEventListener("wheel", onUserInteract, { passive: true });
    container.addEventListener("touchstart", onUserInteract, { passive: true });

    let lastTs = 0;
    let holdUntil = 0;
    let returning = false;
    const viewportWidth = window.innerWidth || 0;
    const isMobile = viewportWidth <= 768;
    const isTablet = viewportWidth > 768 && viewportWidth <= 1024;
    // Promo sürekli kayınca webde hissedilir olsun
    const speedPxPerSec = seamlessLoop
      ? isMobile
        ? 32
        : isTablet
          ? 36
          : 42
      : isMobile
        ? 28
        : isTablet
          ? 26
          : 34;

    const tick = (ts) => {
      if (!lastTs) lastTs = ts;
      const dt = Math.min((ts - lastTs) / 1000, 0.064);
      lastTs = ts;

      const canMove =
        Date.now() >= userInteractUntilRef.current &&
        Date.now() >= holdUntil &&
        !document.hidden;

      if (canMove) {
        if (seamlessLoop) {
          const span = readLoopSpan();
          const maxOffset = readMaxOffset();
          // Taşma yoksa (kartlar henüz ölçülmedi) bekle
          if (span > 40 && maxOffset > 8) {
            applyOffset(slideOffsetRef.current + speedPxPerSec * dt, {
              looping: true,
            });
          } else {
            checkScroll();
          }
        } else {
          const maxOffset = readMaxOffset();
          if (maxOffset > 8) {
            const delta = speedPxPerSec * dt * (returning ? -2.2 : 1);
            const next = slideOffsetRef.current + delta;

            if (!returning && next >= maxOffset - 0.5) {
              applyOffset(maxOffset);
              holdUntil = Date.now() + 900;
              returning = true;
            } else if (returning && next <= 0.5) {
              applyOffset(0);
              returning = false;
              holdUntil = Date.now() + 600;
            } else {
              applyOffset(next);
            }
          } else {
            checkScroll();
          }
        }
      }

      autoScrollRafRef.current = requestAnimationFrame(tick);
    };

    // Layout/görsel ölçülsün diye kısa gecikme
    const startTimer = window.setTimeout(() => {
      applyOffset(0, { looping: seamlessLoop });
      autoScrollRafRef.current = requestAnimationFrame(tick);
    }, 350);

    const onResize = () =>
      applyOffset(slideOffsetRef.current, { looping: seamlessLoop });
    window.addEventListener("resize", onResize);

    // Görseller yüklenince track genişliği değişir — yeniden ölç
    const imgs = track.querySelectorAll("img");
    const onImgLoad = () =>
      applyOffset(slideOffsetRef.current, { looping: seamlessLoop });
    imgs.forEach((img) => {
      if (!img.complete) img.addEventListener("load", onImgLoad, { once: true });
    });

    return () => {
      window.clearTimeout(startTimer);
      window.removeEventListener("resize", onResize);
      imgs.forEach((img) => img.removeEventListener("load", onImgLoad));
      if (autoScrollRafRef.current) {
        cancelAnimationFrame(autoScrollRafRef.current);
      }
      container.classList.remove("is-auto-scrolling");
      track.style.willChange = "";
      container.removeEventListener("pointerdown", onUserInteract);
      container.removeEventListener("wheel", onUserInteract);
      container.removeEventListener("touchstart", onUserInteract);
    };
  }, [
    motionActive,
    itemCount,
    seamlessLoop,
    applyOffset,
    readMaxOffset,
    readLoopSpan,
    checkScroll,
  ]);

  return {
    scrollContainerRef,
    trackRef,
    canScrollLeft,
    canScrollRight,
    scroll,
    checkScroll,
  };
}

/**
 * Viewport giriş animasyonu + kayma tetikleyicisi.
 */
export function useInViewMotion({ threshold = 0.08 } = {}) {
  const sectionRef = useRef(null);
  const [hasEntered, setHasEntered] = useState(false);
  const [isMotionActive, setIsMotionActive] = useState(false);

  useEffect(() => {
    const section = sectionRef.current;
    if (!section) return undefined;

    if (typeof IntersectionObserver === "undefined") {
      setHasEntered(true);
      setIsMotionActive(true);
      return undefined;
    }

    // İlk boyamada zaten görünürse hemen göster
    const rect = section.getBoundingClientRect();
    const vh = window.innerHeight || 0;
    if (rect.top < vh && rect.bottom > 0) {
      setHasEntered(true);
      setIsMotionActive(true);
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        const ratio = entry.isIntersecting ? entry.intersectionRatio : 0;
        if (entry.isIntersecting || ratio > 0.01) {
          setHasEntered(true);
        }
        // Section görünürken kaymayı açık tut
        setIsMotionActive(entry.isIntersecting || ratio >= threshold);
      },
      {
        threshold: [0, 0.01, 0.05, 0.1, 0.15, 0.25, 0.4, 0.6, 1],
        rootMargin: "80px 0px 80px 0px",
      },
    );

    observer.observe(section);
    return () => observer.disconnect();
  }, [threshold]);

  return { sectionRef, hasEntered, isMotionActive };
}
