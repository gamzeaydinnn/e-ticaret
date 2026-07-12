/**
 * Banner/poster medya render bileşeni.
 * Görsel veya kısa sessiz HD video (mp4/webm) destekler.
 * Videolar otomatik oynar, sessiz, döngüde ve orijinal kalitede sunulur.
 */
import React, { useEffect, useRef } from "react";
import { isVideoUrl } from "../services/bannerService";

const videoDefaults = {
  autoPlay: true,
  muted: true,
  loop: true,
  playsInline: true,
  preload: "auto",
};

/**
 * @param {object} props
 * @param {string} props.src
 * @param {string} [props.alt]
 * @param {string} [props.className]
 * @param {object} [props.style]
 * @param {boolean} [props.active=true] - false ise video duraklatılır (slider için)
 * @param {function} [props.onError]
 * @param {string} [props.loading] - img için lazy/eager
 */
export default function BannerMedia({
  src,
  alt = "",
  className,
  style,
  active = true,
  onError,
  loading,
  ...rest
}) {
  const videoRef = useRef(null);
  const isVideo = isVideoUrl(src);

  useEffect(() => {
    const el = videoRef.current;
    if (!el || !isVideo) return;
    if (active) {
      el.muted = true;
      const playPromise = el.play();
      if (playPromise?.catch) {
        playPromise.catch(() => {
          /* autoplay policy — muted playsInline yeterli olmalı */
        });
      }
    } else {
      el.pause();
    }
  }, [active, isVideo, src]);

  if (!src) return null;

  if (isVideo) {
    return (
      <video
        ref={videoRef}
        src={src}
        className={className}
        style={style}
        aria-label={alt}
        {...videoDefaults}
        disablePictureInPicture
        controls={false}
        {...rest}
      />
    );
  }

  return (
    <img
      src={src}
      alt={alt}
      className={className}
      style={style}
      onError={onError}
      loading={loading}
      {...rest}
    />
  );
}
