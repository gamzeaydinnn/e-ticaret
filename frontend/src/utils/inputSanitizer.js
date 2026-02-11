/**
 * Input Sanitization Utility
 *
 * XSS, SQL injection ve diğer güvenlik tehditlerini önlemek için
 * kullanıcı girdilerini temizler ve normalize eder.
 *
 * KULLANIM:
 * import { sanitizeInput, sanitizeNumber } from './utils/inputSanitizer';
 *
 * const safeName = sanitizeInput(userInput, { preventXSS: true, maxLength: 200 });
 * const safePrice = sanitizeNumber(priceInput, { min: 0, max: 10000, decimals: 2 });
 */

/**
 * String girişlerini temizler ve normalize eder
 *
 * @param {string|any} value - Temizlenecek değer
 * @param {Object} options - Temizleme seçenekleri
 * @param {boolean} options.preventXSS - XSS koruması (varsayılan: true)
 * @param {boolean} options.normalizeWhitespace - Boşlukları normalize et (varsayılan: false)
 * @param {number} options.maxLength - Maksimum uzunluk (varsayılan: yok)
 * @param {boolean} options.trim - Başta/sonda boşlukları kaldır (varsayılan: true)
 * @param {boolean} options.toLowerCase - Küçük harfe çevir (varsayılan: false)
 * @param {boolean} options.toUpperCase - Büyük harfe çevir (varsayılan: false)
 * @returns {string} Temizlenmiş string
 */
export const sanitizeInput = (value, options = {}) => {
  // Varsayılan ayarlar
  const {
    preventXSS = true,
    normalizeWhitespace = false,
    maxLength = null,
    trim = true,
    toLowerCase = false,
    toUpperCase = false,
  } = options;

  // Tip kontrolü
  if (typeof value !== "string") {
    if (value === null || value === undefined) return "";
    value = String(value);
  }

  let sanitized = value;

  // Trim
  if (trim) {
    sanitized = sanitized.trim();
  }

  // XSS Koruması - Tehlikeli karakterleri encode et
  if (preventXSS) {
    sanitized = sanitized
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#x27;")
      .replace(/\//g, "&#x2F;")
      // Ek güvenlik: Event handler'lar
      .replace(/javascript:/gi, "")
      .replace(/on\w+\s*=/gi, "")
      // Ek güvenlik: Data URI
      .replace(/data:text\/html/gi, "");
  }

  // Whitespace Normalize
  if (normalizeWhitespace) {
    // Birden fazla boşluğu tek boşluğa indir
    sanitized = sanitized.replace(/\s+/g, " ");
  }

  // Max Length
  if (maxLength && sanitized.length > maxLength) {
    sanitized = sanitized.substring(0, maxLength);
  }

  // Lower/Upper Case
  if (toLowerCase) {
    sanitized = sanitized.toLowerCase();
  } else if (toUpperCase) {
    sanitized = sanitized.toUpperCase();
  }

  return sanitized;
};

/**
 * Sayısal girişleri validate eder ve temizler
 *
 * @param {string|number|any} value - Temizlenecek sayısal değer
 * @param {Object} options - Temizleme seçenekleri
 * @param {number} options.min - Minimum değer (varsayılan: yok)
 * @param {number} options.max - Maksimum değer (varsayılan: yok)
 * @param {number} options.decimals - Ondalık basamak sayısı (varsayılan: yok)
 * @param {number} options.default - Geçersiz değer için varsayılan (varsayılan: 0)
 * @param {boolean} options.allowNegative - Negatif değerlere izin ver (varsayılan: true)
 * @returns {number} Temizlenmiş sayı
 */
export const sanitizeNumber = (value, options = {}) => {
  const {
    min = null,
    max = null,
    decimals = null,
    default: defaultValue = 0,
    allowNegative = true,
  } = options;

  // String ise parse et
  if (typeof value === "string") {
    value = value.trim();

    // Türkçe ondalık ayırıcı desteği (virgül -> nokta)
    value = value.replace(",", ".");
  }

  // Number'a dönüştür
  const num = Number(value);

  // NaN kontrolü
  if (isNaN(num)) {
    return defaultValue;
  }

  let sanitized = num;

  // Negatif kontrol
  if (!allowNegative && sanitized < 0) {
    sanitized = Math.abs(sanitized);
  }

  // Min/Max kontrolü
  if (min !== null && sanitized < min) {
    sanitized = min;
  }

  if (max !== null && sanitized > max) {
    sanitized = max;
  }

  // Decimal precision
  if (decimals !== null) {
    sanitized = Number(sanitized.toFixed(decimals));
  }

  return sanitized;
};

/**
 * Boolean girişleri normalize eder
 *
 * @param {any} value - Değer
 * @param {boolean} defaultValue - Varsayılan değer (varsayılan: false)
 * @returns {boolean} Boolean değer
 */
export const sanitizeBoolean = (value, defaultValue = false) => {
  if (typeof value === "boolean") return value;

  if (typeof value === "string") {
    const lower = value.toLowerCase().trim();
    return (
      lower === "true" || lower === "1" || lower === "yes" || lower === "evet"
    );
  }

  if (typeof value === "number") {
    return value !== 0;
  }

  return defaultValue;
};

/**
 * Email adresini temizler ve validate eder
 *
 * @param {string} email - Email adresi
 * @returns {object} { valid: boolean, sanitized: string, error: string|null }
 */
export const sanitizeEmail = (email) => {
  if (typeof email !== "string") {
    return { valid: false, sanitized: "", error: "Geçersiz email formatı" };
  }

  // Trim ve lowercase
  const sanitized = email.trim().toLowerCase();

  // Boş kontrol
  if (sanitized.length === 0) {
    return { valid: false, sanitized: "", error: "Email adresi boş olamaz" };
  }

  // Basit email regex (RFC 5322'nin minimal versiyonu)
  const emailRegex = /^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$/i;

  if (!emailRegex.test(sanitized)) {
    return { valid: false, sanitized: "", error: "Geçersiz email formatı" };
  }

  return { valid: true, sanitized, error: null };
};

/**
 * Telefon numarasını temizler (Türkiye formatı)
 *
 * @param {string} phone - Telefon numarası
 * @returns {object} { valid: boolean, sanitized: string, formatted: string, error: string|null }
 */
export const sanitizePhone = (phone) => {
  if (typeof phone !== "string") {
    return {
      valid: false,
      sanitized: "",
      formatted: "",
      error: "Geçersiz telefon formatı",
    };
  }

  // Sadece rakamları al
  const digitsOnly = phone.replace(/\D/g, "");

  // Boş kontrol
  if (digitsOnly.length === 0) {
    return {
      valid: false,
      sanitized: "",
      formatted: "",
      error: "Telefon numarası boş olamaz",
    };
  }

  // Türkiye formatı: 10 veya 12 haneli (90 ile başlıyorsa)
  let normalized = digitsOnly;

  // 90 ile başlıyorsa kaldır
  if (normalized.startsWith("90") && normalized.length === 12) {
    normalized = normalized.substring(2);
  }

  // 0 ile başlıyorsa kaldır
  if (normalized.startsWith("0") && normalized.length === 11) {
    normalized = normalized.substring(1);
  }

  // 10 haneli olmalı
  if (normalized.length !== 10) {
    return {
      valid: false,
      sanitized: digitsOnly,
      formatted: "",
      error: "Telefon numarası 10 haneli olmalıdır (5XX XXX XX XX)",
    };
  }

  // 5 ile başlamalı (GSM)
  if (!normalized.startsWith("5")) {
    return {
      valid: false,
      sanitized: normalized,
      formatted: "",
      error: "Telefon numarası 5 ile başlamalıdır",
    };
  }

  // Format: 5XX XXX XX XX
  const formatted = `${normalized.substring(0, 3)} ${normalized.substring(3, 6)} ${normalized.substring(6, 8)} ${normalized.substring(8, 10)}`;

  return {
    valid: true,
    sanitized: normalized, // 5xxxxxxxxx
    formatted, // 5XX XXX XX XX
    error: null,
  };
};

/**
 * URL'yi temizler ve validate eder
 *
 * @param {string} url - URL
 * @param {Object} options - Seçenekler
 * @param {Array<string>} options.allowedProtocols - İzin verilen protokoller (varsayılan: ['http:', 'https:'])
 * @returns {object} { valid: boolean, sanitized: string, error: string|null }
 */
export const sanitizeURL = (url, options = {}) => {
  const { allowedProtocols = ["http:", "https:"] } = options;

  if (typeof url !== "string") {
    return { valid: false, sanitized: "", error: "Geçersiz URL formatı" };
  }

  const sanitized = url.trim();

  if (sanitized.length === 0) {
    return { valid: false, sanitized: "", error: "URL boş olamaz" };
  }

  try {
    const urlObj = new URL(sanitized);

    // Protocol kontrolü
    if (!allowedProtocols.includes(urlObj.protocol)) {
      return {
        valid: false,
        sanitized: "",
        error: `Sadece ${allowedProtocols.join(", ")} protokollerine izin verilir`,
      };
    }

    return { valid: true, sanitized: urlObj.href, error: null };
  } catch (e) {
    return { valid: false, sanitized: "", error: "Geçersiz URL formatı" };
  }
};

/**
 * Kısa yardımcı: Form data object'indeki tüm string alanları temizle
 *
 * @param {Object} formData - Form verisi
 * @param {Object} options - Temizleme seçenekleri
 * @returns {Object} Temizlenmiş form verisi
 */
export const sanitizeFormData = (formData, options = {}) => {
  const sanitized = {};

  for (const [key, value] of Object.entries(formData)) {
    if (typeof value === "string") {
      sanitized[key] = sanitizeInput(value, options);
    } else if (typeof value === "number") {
      sanitized[key] = value;
    } else if (typeof value === "boolean") {
      sanitized[key] = value;
    } else if (value === null || value === undefined) {
      sanitized[key] = value;
    } else {
      // Object veya Array - deep copy yap
      sanitized[key] = JSON.parse(JSON.stringify(value));
    }
  }

  return sanitized;
};

export default {
  sanitizeInput,
  sanitizeNumber,
  sanitizeBoolean,
  sanitizeEmail,
  sanitizePhone,
  sanitizeURL,
  sanitizeFormData,
};
