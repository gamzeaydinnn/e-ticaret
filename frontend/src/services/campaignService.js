// src/services/campaignService.js
import api from "./api";
import { shouldUseMockData, debugLog } from "../config/apiConfig";

const mockCampaigns = [
  {
    id: 1,
    slug: "ilk-alisveris-indirim",
    title: "İlk Alışverişinize %25 İndirim",
    summary: "Yeni üyeler için sepette ekstra indirim",
    description:
      "Gölköy Gourmet Market'e hoş geldiniz! İlk alışverişinizde sepet toplamınıza özel %25 indirim fırsatını kaçırmayın.",
    image: "/images/ilk-alisveris-indirim-banner.png",
    badge: "%25 İndirim",
    validUntil: null,
    categoryId: null,
  },
  {
    id: 2,
    slug: "meyve-sebze-haftasi",
    title: "Meyve & Sebze Haftası",
    summary: "Tazelikte fırsat haftası: %20'ye varan indirimler",
    description:
      "Doğanın en taze lezzetleri sofranızda! Meyve & Sebze kategorisinde bu hafta özel fırsatlar sizi bekliyor.",
    image: "/images/meyve-reyonu-banner.png",
    badge: "%20'ye Varan",
    validUntil: null,
    categoryId: 1, // Meyve & Sebze
  },
  {
    id: 3,
    slug: "iceceklerde-2-al-1-ode",
    title: "İçeceklerde 2 Al 1 Öde",
    summary: "Serinleten fırsat! Seçili içeceklerde 2 al 1 öde",
    description:
      "Seçili gazlı ve soğuk içeceklerde 2 al 1 öde kampanyası ile dolabınızı doldurun!",
    image: "/images/taze-dogal-indirim-banner.png",
    badge: "2 Al 1 Öde",
    validUntil: null,
    categoryId: 5, // İçecekler
  },
];

const findMockBySlug = (slug) => mockCampaigns.find((c) => c.slug === slug);

export const CampaignService = {
  list: async () => {
    if (shouldUseMockData()) {
      debugLog("Campaigns - Mock data kullanılıyor");
      return mockCampaigns;
    }

    try {
      const data = await api.get("/campaigns");
      // Backend hazır değilse ya da farklı shape dönerse güvenli tarafta kal
      if (Array.isArray(data)) return data;
      if (Array.isArray(data?.data)) return data.data;
      return mockCampaigns;
    } catch (e) {
      // Backend kampanya endpoint'i henüz yoksa (404 vb.) sessizce mock'a düş
      debugLog("Campaigns API başarısız, mock'a düşülüyor", {
        error: e?.message,
      });
      return mockCampaigns;
    }
  },

  getBySlug: async (slug) => {
    if (shouldUseMockData()) {
      debugLog("Campaign detail - Mock data kullanılıyor", { slug });
      const c = findMockBySlug(slug);
      if (!c) throw new Error("Kampanya bulunamadı");
      return c;
    }

    try {
      const data = await api.get(`/campaigns/${encodeURIComponent(slug)}`);
      if (data) return data.data || data;
    } catch (e) {
      debugLog("Campaign detail API başarısız, mock'a düşülüyor", {
        slug,
        error: e?.message,
      });
    }

    const fallback = findMockBySlug(slug);
    if (!fallback) throw new Error("Kampanya bulunamadı");
    return fallback;
  },

  // ============================================================
  // YENİ KAMPANYA SİSTEMİ METODLARİ
  // ============================================================

  /**
   * Aktif kampanyaları getirir (yeni sistem)
   * Frontend banner/slider gösterimi için
   * @returns {Promise<Array>} Aktif kampanya listesi
   */
  getActiveCampaigns: async () => {
    try {
      // Doğru endpoint: /api/promotions/active
      const response = await api.get("/api/promotions/active");
      if (Array.isArray(response) && response.length > 0) return response;
    } catch (error) {
      debugLog("Aktif kampanyalar alınamadı:", { error: error?.message });
    }

    // Fallback: alternatif endpoint (eski sistem)
    try {
      const altResponse = await api.get("/api/campaigns");
      if (Array.isArray(altResponse) && altResponse.length > 0)
        return altResponse;
    } catch (error) {
      debugLog("Alternatif kampanya endpoint'i başarısız:", {
        error: error?.message,
      });
    }

    // Son çare: mevcut liste (mock veya genel kampanyalar)
    return CampaignService.list();
  },

  /**
   * Belirli bir ürün için geçerli kampanyaları getirir
   * @param {number} productId - Ürün ID
   * @returns {Promise<Array>} Ürün için geçerli kampanyalar
   */
  getCampaignsForProduct: async (productId, categoryId) => {
    try {
      // Doğru endpoint: /api/promotions/product/{id}
      const query =
        categoryId !== undefined && categoryId !== null
          ? `?categoryId=${encodeURIComponent(categoryId)}`
          : "";
      const response = await api.get(
        `/api/promotions/product/${productId}${query}`,
      );
      if (Array.isArray(response) && response.length > 0) return response;
    } catch (error) {
      debugLog(`Ürün kampanyaları alınamadı (ID: ${productId}):`, {
        error: error?.message,
      });
    }

    // Fallback: eski route (varsa)
    try {
      const altResponse = await api.get(
        `/api/promotions/campaigns/product/${productId}`,
      );
      if (Array.isArray(altResponse) && altResponse.length > 0)
        return altResponse;
    } catch (error) {
      debugLog(`Alternatif ürün kampanyaları endpoint'i başarısız:`, {
        error: error?.message,
      });
    }

    return [];
  },

  /**
   * Belirli bir kategori için geçerli kampanyaları getirir
   * @param {number} categoryId - Kategori ID
   * @returns {Promise<Array>} Kategori için geçerli kampanyalar
   */
  getCampaignsForCategory: async (categoryId) => {
    try {
      // Doğru endpoint: /api/promotions/category/{id}
      const response = await api.get(`/api/promotions/category/${categoryId}`);
      if (Array.isArray(response) && response.length > 0) return response;
    } catch (error) {
      debugLog(`Kategori kampanyaları alınamadı (ID: ${categoryId}):`, {
        error: error?.message,
      });
    }

    // Fallback: eski route (varsa)
    try {
      const altResponse = await api.get(
        `/api/promotions/campaigns/category/${categoryId}`,
      );
      if (Array.isArray(altResponse) && altResponse.length > 0)
        return altResponse;
    } catch (error) {
      debugLog(`Alternatif kategori kampanyaları endpoint'i başarısız:`, {
        error: error?.message,
      });
    }

    return [];
  },

  // ============================================================
  // KAMPANYA YARDIMCI FONKSİYONLARI
  // ============================================================

  /**
   * Kampanya türü adını döner
   * @param {number|string} type - Kampanya türü
   * @returns {string} Türkçe kampanya türü adı
   */
  getCampaignTypeName: (type) => {
    const typeNames = {
      0: "Yüzde İndirim",
      1: "Sabit Tutar İndirim",
      2: "X Al Y Öde",
      3: "Ücretsiz Kargo",
      Percentage: "Yüzde İndirim",
      FixedAmount: "Sabit Tutar İndirim",
      BuyXPayY: "X Al Y Öde",
      FreeShipping: "Ücretsiz Kargo",
    };
    return typeNames[type] || "İndirim";
  },

  /**
   * Kampanya badge bilgisini döner
   * @param {number|string} type - Kampanya türü
   * @returns {object} Badge bilgisi {text, icon, color}
   */
  getCampaignBadge: (type) => {
    const badges = {
      0: { text: "İndirim", icon: "fa-percent", color: "danger" },
      1: { text: "İndirim", icon: "fa-tag", color: "warning" },
      2: { text: "X Al Y Öde", icon: "fa-gift", color: "success" },
      3: { text: "Ücretsiz Kargo", icon: "fa-truck", color: "info" },
    };
    const typeNum = typeof type === "string" ? parseInt(type) : type;
    return (
      badges[typeNum] || { text: "İndirim", icon: "fa-tag", color: "secondary" }
    );
  },

  /**
   * Kampanya indirim metnini oluşturur
   * @param {object} campaign - Kampanya objesi
   * @returns {string} İndirim metni (örn: "%20 İndirim", "3 Al 2 Öde")
   */
  getDiscountText: (campaign) => {
    if (!campaign) return "";

    const type =
      typeof campaign.type === "string"
        ? parseInt(campaign.type)
        : campaign.type;

    switch (type) {
      case 0: // Percentage
        return `%${campaign.discountValue || 0} İndirim`;
      case 1: // FixedAmount
        return `₺${campaign.discountValue || 0} İndirim`;
      case 2: // BuyXPayY
        return `${campaign.buyQty || 3} Al ${campaign.payQty || 2} Öde`;
      case 3: // FreeShipping
        return "Ücretsiz Kargo";
      default:
        return campaign.badge || "Kampanya";
    }
  },

  /**
   * Kampanya için kısa açıklama metni oluşturur
   * @param {object} campaign - Kampanya objesi
   * @returns {string} Kısa açıklama
   */
  getShortDescription: (campaign) => {
    if (!campaign) return "";

    const type =
      typeof campaign.type === "string"
        ? parseInt(campaign.type)
        : campaign.type;

    switch (type) {
      case 0: // Percentage
        return `Bu üründe %${campaign.discountValue || 0} indirim!`;
      case 1: // FixedAmount
        return `Bu üründe ₺${campaign.discountValue || 0} indirim!`;
      case 2: // BuyXPayY
        const freeQty = (campaign.buyQty || 3) - (campaign.payQty || 2);
        return `${campaign.buyQty || 3} tane al, ${freeQty} tanesi bedava!`;
      case 3: // FreeShipping
        return "Bu ürün ücretsiz kargo!";
      default:
        return campaign.description || campaign.summary || "Kampanya fırsatı!";
    }
  },

  /**
   * Kampanyanın geçerli olup olmadığını kontrol eder
   * @param {object} campaign - Kampanya objesi
   * @returns {boolean} Geçerli mi?
   */
  isValidCampaign: (campaign) => {
    if (!campaign || !campaign.isActive) return false;

    const now = new Date();
    const startDate = campaign.startDate ? new Date(campaign.startDate) : null;
    const endDate = campaign.endDate ? new Date(campaign.endDate) : null;

    if (startDate && now < startDate) return false;
    if (endDate && now > endDate) return false;

    return true;
  },

  /**
   * Ürün için en iyi kampanyayı seçer
   * @param {Array} campaigns - Kampanya listesi
   * @param {number} price - Ürün fiyatı
   * @returns {object|null} En iyi kampanya
   */
  getBestCampaign: (campaigns, price) => {
    if (!campaigns || campaigns.length === 0) return null;

    const validCampaigns = campaigns.filter((c) =>
      CampaignService.isValidCampaign(c),
    );
    if (validCampaigns.length === 0) return null;

    // İndirim miktarına göre sırala ve en iyisini seç
    return validCampaigns.reduce((best, current) => {
      const bestDiscount = CampaignService.calculateDiscount(best, price);
      const currentDiscount = CampaignService.calculateDiscount(current, price);
      return currentDiscount > bestDiscount ? current : best;
    });
  },

  /**
   * Kampanya indirim tutarını hesaplar
   * @param {object} campaign - Kampanya objesi
   * @param {number} price - Ürün fiyatı
   * @param {number} quantity - Adet (BuyXPayY için)
   * @returns {number} İndirim tutarı
   */
  calculateDiscount: (campaign, price, quantity = 1) => {
    if (!campaign) return 0;

    const type =
      typeof campaign.type === "string"
        ? parseInt(campaign.type)
        : campaign.type;

    switch (type) {
      case 0: // Percentage
        let discount = (price * quantity * (campaign.discountValue || 0)) / 100;
        if (
          campaign.maxDiscountAmount &&
          discount > campaign.maxDiscountAmount
        ) {
          discount = campaign.maxDiscountAmount;
        }
        return discount;
      case 1: // FixedAmount
        return Math.min(campaign.discountValue || 0, price * quantity);
      case 2: // BuyXPayY
        const buyQty = campaign.buyQty || 3;
        const payQty = campaign.payQty || 2;
        const freeQty = buyQty - payQty;
        const sets = Math.floor(quantity / buyQty);
        return sets * freeQty * price;
      case 3: // FreeShipping
        return 0; // Kargo indirimi ayrı hesaplanır
      default:
        return 0;
    }
  },
};
