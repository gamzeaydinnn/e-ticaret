/**
 * siteSettingsService.js - Site Ayarları Servisi
 *
 * Footer, iletişim bilgileri, firma bilgileri vb. için
 * Backend'den dinamik olarak çeker
 */
import api from "./api";

// Cache için
let siteSettingsCache = null;
let footerDataCache = null;
let cacheTimestamp = null;
const CACHE_DURATION = 5 * 60 * 1000; // 5 dakika

/**
 * Cache'in hala geçerli olup olmadığını kontrol eder
 */
const isCacheValid = () => {
  return cacheTimestamp && Date.now() - cacheTimestamp < CACHE_DURATION;
};

const dedupeArray = (values = []) =>
  [...new Set((values || []).filter(Boolean))];

const mergeFooterData = (data = {}) => {
  const defaults = getDefaultSiteSettings();
  const footerFromApi = data.footer || {};
  return {
    ...defaults,
    ...data,
    company: { ...defaults.company, ...(data.company || {}) },
    contact: { ...defaults.contact, ...(data.contact || {}) },
    socialMedia: { ...defaults.socialMedia, ...(data.socialMedia || {}) },
    footer: {
      ...defaults.footer,
      ...footerFromApi,
      paymentMethods: dedupeArray(
        footerFromApi.paymentMethods?.length
          ? footerFromApi.paymentMethods
          : defaults.footer.paymentMethods,
      ),
      securityFeatures: dedupeArray(
        footerFromApi.securityFeatures?.length
          ? footerFromApi.securityFeatures
          : defaults.footer.securityFeatures,
      ),
    },
  };
};

/**
 * axios interceptor zaten response.data döndürür; tekrar .data kullanma.
 */
const unwrapApiResponse = (response) =>
  response && typeof response === "object" && "data" in response
    ? response.data
    : response;

/**
 * Tüm site ayarlarını getirir
 */
export const getSiteSettings = async () => {
  if (siteSettingsCache && isCacheValid()) {
    return siteSettingsCache;
  }

  try {
    const response = await api.get("/api/sitesettings");
    siteSettingsCache = mergeFooterData(unwrapApiResponse(response));
    cacheTimestamp = Date.now();
    return siteSettingsCache;
  } catch (error) {
    console.error("Site ayarları alınamadı:", error);
    // Fallback değerler
    return getDefaultSiteSettings();
  }
};

/**
 * Footer için gereken verileri getirir
 */
export const getFooterData = async () => {
  if (footerDataCache && isCacheValid()) {
    return footerDataCache;
  }

  try {
    const response = await api.get("/api/sitesettings/footer");
    footerDataCache = mergeFooterData(unwrapApiResponse(response));
    cacheTimestamp = Date.now();
    return footerDataCache;
  } catch (error) {
    console.error("Footer verileri alınamadı:", error);
    // Fallback değerler
    return getDefaultFooterData();
  }
};

/**
 * Firma bilgilerini getirir
 */
export const getCompanyInfo = async () => {
  try {
    const response = await api.get("/api/sitesettings/company");
    const defaults = getDefaultSiteSettings().company;
    return { ...defaults, ...unwrapApiResponse(response) };
  } catch (error) {
    console.error("Firma bilgileri alınamadı:", error);
    return getDefaultSiteSettings().company;
  }
};

/**
 * İletişim bilgilerini getirir
 */
export const getContactInfo = async () => {
  try {
    const response = await api.get("/api/sitesettings/contact");
    const defaults = getDefaultSiteSettings().contact;
    return { ...defaults, ...unwrapApiResponse(response) };
  } catch (error) {
    console.error("İletişim bilgileri alınamadı:", error);
    return getDefaultSiteSettings().contact;
  }
};

/**
 * Cache'i temizler
 */
export const clearCache = () => {
  siteSettingsCache = null;
  footerDataCache = null;
  cacheTimestamp = null;
};

/**
 * Backend'e erişilemezse kullanılacak varsayılan değerler
 */
const getDefaultSiteSettings = () => ({
  company: {
    name: "Gölköy Gurme",
    shortName: "Gölköy Gurme",
    legalName: "Gölköy Gurme Market",
    description:
      "Bodrum Gölköy'de taze meyve-sebze, et, süt ürünleri ve günlük market ihtiyaçlarınızı kendi kuryemizle kapınıza getiriyoruz. Yerel lezzetler, soğuk zincir ve güvenli alışveriş.",
    logoUrl: "/images/golkoy-logo1.png",
    secondaryLogoUrl: "/images/dogadan-sofranza-logo.png",
    copyrightText: "Tüm haklar Gölköy Gurme Markete aittir.",
  },
  contact: {
    phone: "+90 533 478 30 72",
    phoneDisplay: "+90 533 478 30 72",
    phoneLabel: "Müşteri Hizmetleri",
    whatsAppNumber: "905334783072",
    whatsAppMessage:
      "Merhaba, Gölköy Gurme siparişim hakkında bilgi almak istiyorum.",
    email: "golturkbuku@golkoygurme.com.tr",
    emailLabel: "Genel bilgi ve destek",
    address: "Gölköy Mah. 67 Sokak No: 1/A Bodrum/Muğla",
    addressLabel: "Gölköy Mağaza",
  },
  socialMedia: {
    facebook: "https://www.facebook.com/golkoygurmebodrum",
    instagram:
      "https://www.instagram.com/golkoygurmebodrum?igsh=aWJwMHJsbXdjYmt4",
    twitter: "",
    youTube: "",
    linkedIn: "",
  },
  footer: {
    showSecondaryLogo: true,
    showSSLBadge: true,
    showPaymentMethods: true,
    paymentMethods: ["VISA", "MC", "TROY"],
    securityFeatures: [
      "Taze ürün garantisi",
      "Soğuk zincir teslimat",
      "Güvenli ödeme sistemi",
    ],
  },
});

/**
 * Footer için varsayılan veriler
 */
const getDefaultFooterData = () => getDefaultSiteSettings();

export default {
  getSiteSettings,
  getFooterData,
  getCompanyInfo,
  getContactInfo,
  clearCache,
};

export const getWhatsAppNumber = async () => {
  const contact = await getContactInfo();
  return contact?.whatsAppNumber || contact?.whatsappNumber || "905334783072";
};

export const buildWhatsAppUrl = async (message) => {
  const number = await getWhatsAppNumber();
  return `https://wa.me/${number}?text=${encodeURIComponent(message)}`;
};
