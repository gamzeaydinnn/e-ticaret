// Güvenlik yardımcı fonksiyonları
import DOMPurify from "dompurify";

/**
 * URL'nin güvenli olup olmadığını kontrol eder (Open Redirect koruması)
 * Sadece kendi domain'lerine ve güvenilir ödeme sağlayıcılarına izin verir
 */
export const isAllowedRedirectUrl = (url) => {
  if (!url || typeof url !== "string") {
    return false;
  }

  try {
    const parsed = new URL(url, window.location.origin);

    // İzin verilen domain'ler (production ve development)
    const allowedDomains = [
      // Kendi domain'lerimiz
      window.location.hostname,
      "localhost",
      "127.0.0.1",
      // Yapı Kredi / POSNET
      "posnet.yapikredi.com.tr",
      "posnettest.yapikredi.com.tr",
      "www.posnet.yapikredi.com.tr",
      // Diğer güvenilir ödeme sağlayıcıları (gerekirse ekleyin)
      "3dsecure.yapikredi.com.tr",
      "vpos.yapikredi.com.tr",
    ];

    // Hostname kontrolü
    const hostname = parsed.hostname.toLowerCase();
    const isAllowed = allowedDomains.some(
      (domain) => hostname === domain || hostname.endsWith("." + domain),
    );

    // HTTPS zorunluluğu (localhost hariç)
    const isSecure =
      parsed.protocol === "https:" ||
      hostname === "localhost" ||
      hostname === "127.0.0.1";

    return isAllowed && isSecure;
  } catch (e) {
    // Geçersiz URL
    return false;
  }
};

/**
 * Güvenli yönlendirme - Sadece izin verilen URL'lere yönlendirir
 */
export const safeRedirect = (url, fallbackPath = "/") => {
  if (isAllowedRedirectUrl(url)) {
    window.location.href = url;
    return true;
  } else {
    console.error("Güvensiz redirect URL engellendi:", url);
    // Fallback olarak ana sayfaya yönlendir
    window.location.href = fallbackPath;
    return false;
  }
};

/**
 * 3D Secure HTML içeriğini sanitize eder (XSS koruması)
 * Sadece form submission için gerekli elementlere izin verir
 */
export const sanitize3DSecureHtml = (htmlContent) => {
  if (!htmlContent || typeof htmlContent !== "string") {
    return "";
  }

  // DOMPurify yapılandırması - sadece form ve gerekli elementlere izin ver
  const config = {
    ALLOWED_TAGS: [
      "form",
      "input",
      "button",
      "script",
      "noscript",
      "div",
      "span",
      "p",
      "body",
      "html",
      "head",
      "meta",
    ],
    ALLOWED_ATTR: [
      "action",
      "method",
      "name",
      "value",
      "type",
      "id",
      "class",
      "style",
      "hidden",
      "enctype",
      "target",
      "src",
      "charset",
      "content",
      "http-equiv",
    ],
    // Form action URL'lerini kontrol et
    ALLOW_DATA_ATTR: false,
    // URI'leri kontrol et
    ALLOWED_URI_REGEXP:
      /^(?:(?:(?:f|ht)tps?|mailto|tel|callto|sms|cid|xmpp):|[^a-z]|[a-z+.\-]+(?:[^a-z+.\-:]|$))/i,
  };

  return DOMPurify.sanitize(htmlContent, config);
};

/**
 * URL parametrelerini sanitize eder
 */
export const sanitizeUrlParam = (param) => {
  if (!param || typeof param !== "string") {
    return "";
  }

  // Tehlikeli karakterleri temizle
  return param
    .replace(/[<>'"]/g, "")
    .replace(/javascript:/gi, "")
    .replace(/data:/gi, "")
    .trim();
};

/**
 * Sayısal ID parametresini doğrular
 */
export const validateNumericId = (id) => {
  if (id === undefined || id === null) {
    return null;
  }

  const numId = parseInt(id, 10);
  if (isNaN(numId) || numId < 0 || numId > Number.MAX_SAFE_INTEGER) {
    return null;
  }

  return numId;
};

/**
 * Production ortamında olup olmadığını kontrol eder
 */
export const isProduction = () => {
  return process.env.NODE_ENV === "production";
};

/**
 * Debug log - Sadece development ortamında çalışır
 */
export const debugLog = (...args) => {
  if (process.env.NODE_ENV !== "production") {
    console.log(...args);
  }
};

/**
 * Debug error - Sadece development ortamında çalışır
 */
export const debugError = (...args) => {
  if (process.env.NODE_ENV !== "production") {
    console.error(...args);
  }
};
