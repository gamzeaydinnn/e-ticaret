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
      "Gölköy Gurme'ye hoş geldiniz! İlk alışverişinizde sepet toplamınıza özel %25 indirim fırsatını kaçırmayın.",
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
      const response = await api.get("/api/promotions/active");
      if (Array.isArray(response)) return response;
      if (Array.isArray(response?.data)) return response.data;
      return [];
    } catch (error) {
      debugLog("Aktif kampanyalar alınamadı:", { error: error?.message });
      return [];
    }
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

  // ============================================================
  // ÜCRETSİZ KARGO KONTROLÜ
  // ============================================================

  /**
   * Ücretsiz kargo durumunu kontrol eder (YENİ - Kategori bazlı validasyon destekli)
   *
   * KRİTİK: Bu metod sepet ürünlerini backend'e göndererek kategori bazlı
   * kampanyalarda doğru validasyon yapılmasını sağlar.
   *
   * @param {number} cartTotal - Sepet toplam tutarı
   * @param {Array} cartItems - Sepet ürünleri (productId, categoryId, quantity, unitPrice)
   * @returns {Promise<FreeShippingStatusDto>} Ücretsiz kargo durumu
   */
  checkFreeShipping: async (cartTotal, cartItems = []) => {
    try {
      // Sepet ürünlerini backend formatına dönüştür
      const items = cartItems.map((item) => ({
        productId: item.productId || item.id || item.product?.id,
        categoryId:
          item.categoryId ||
          item.product?.categoryId ||
          item.product?.category?.id ||
          0,
        quantity: item.quantity || 1,
        unitPrice: item.unitPrice || item.price || item.product?.price || 0,
      }));

      // Yeni POST endpoint'i çağır
      const response = await api.post("/api/promotions/free-shipping", {
        cartTotal: Number(cartTotal) || 0,
        items: items,
      });

      debugLog("Ücretsiz kargo kontrolü sonucu:", response);
      return response;
    } catch (error) {
      debugLog("Ücretsiz kargo kontrolü hatası:", { error: error?.message });

      // Fallback: Eski GET endpoint'i dene (geriye dönük uyumluluk)
      try {
        const fallbackResponse = await api.get(
          `/api/promotions/free-shipping?cartTotal=${encodeURIComponent(cartTotal)}`,
        );
        debugLog("Fallback ücretsiz kargo kontrolü:", fallbackResponse);
        return fallbackResponse;
      } catch (fallbackError) {
        debugLog("Fallback da başarısız:", { error: fallbackError?.message });
        return {
          isFreeShipping: false,
          message: "Kargo kontrolü yapılamadı.",
        };
      }
    }
  },

  /**
   * Sepet ürünlerinin belirli bir kampanyanın hedef kategorilerinde olup olmadığını kontrol eder
   * (Client-side ön kontrol - backend validasyonu yerine geçmez)
   *
   * @param {Array} cartItems - Sepet ürünleri
   * @param {Array} targetCategoryIds - Hedef kategori ID'leri
   * @returns {boolean} Tüm ürünler hedef kategorilerde mi?
   */
  areAllItemsInTargetCategories: (cartItems, targetCategoryIds) => {
    if (!cartItems || cartItems.length === 0) return false;
    if (!targetCategoryIds || targetCategoryIds.length === 0) return true;

    const targetSet = new Set(targetCategoryIds);
    return cartItems.every((item) => {
      const categoryId =
        item.categoryId ||
        item.product?.categoryId ||
        item.product?.category?.id;
      return categoryId && targetSet.has(categoryId);
    });
  },

  /**
   * Sepetteki kampanya kapsamı dışındaki ürünleri bulur
   *
   * @param {Array} cartItems - Sepet ürünleri
   * @param {Array} targetCategoryIds - Hedef kategori ID'leri
   * @returns {Array} Kapsam dışı ürünler
   */
  getOutOfScopeItems: (cartItems, targetCategoryIds) => {
    if (!cartItems || cartItems.length === 0) return [];
    if (!targetCategoryIds || targetCategoryIds.length === 0) return [];

    const targetSet = new Set(targetCategoryIds);
    return cartItems.filter((item) => {
      const categoryId =
        item.categoryId ||
        item.product?.categoryId ||
        item.product?.category?.id;
      return !categoryId || !targetSet.has(categoryId);
    });
  },

  // ============================================================
  // KUPON KODU DOĞRULAMA
  // ============================================================

  /**
   * Kupon kodunu doğrular ve indirim hesaplar
   *
   * GÜVENLİK:
   * - Kupon kodu trim + uppercase işlenir
   * - Backend'de kullanıcı bazlı limit kontrolü yapılır
   * - IP/UserAgent fraud detection için loglanır
   *
   * @param {string} couponCode - Kupon kodu
   * @param {number} cartTotal - Sepet toplam tutarı
   * @param {Array} cartItems - Sepet ürünleri [{productId, quantity, price, categoryId}]
   * @returns {Promise<Object>} { success, message, discount, campaign, appliedCouponCode }
   */
  validateCouponCode: async (couponCode, cartTotal, cartItems = []) => {
    try {
      // Input validasyonu
      if (!couponCode || typeof couponCode !== "string") {
        return {
          success: false,
          discount: 0,
          message: "Lütfen geçerli bir kupon kodu girin.",
        };
      }

      // Kupon kodunu normalize et (trim + uppercase)
      const normalizedCode = couponCode.trim().toUpperCase();

      if (normalizedCode.length === 0) {
        return {
          success: false,
          discount: 0,
          message: "Kupon kodu boş olamaz.",
        };
      }

      // Sepet ürünlerini backend formatına dönüştür
      const items = cartItems.map((item) => ({
        productId: item.productId || item.id || item.product?.id || 0,
        categoryId:
          item.categoryId ||
          item.product?.categoryId ||
          item.product?.category?.id ||
          0,
        quantity: item.quantity || 1,
        price:
          item.unitPrice ||
          item.price ||
          item.product?.price ||
          item.product?.specialPrice ||
          0,
      }));

      debugLog("Kupon doğrulama isteği gönderiliyor:", {
        code: normalizedCode,
        cartTotal: Number(cartTotal),
        itemCount: items.length,
      });

      // Backend'e doğrulama isteği gönder
      const response = await api.post("/api/promotions/validate-coupon", {
        code: normalizedCode,
        cartTotal: Number(cartTotal) || 0,
        items: items,
      });

      debugLog("Kupon doğrulama yanıtı:", response);

      // Başarılı yanıt
      if (response && response.success !== false) {
        return {
          success: true,
          discount: response.discount || 0,
          campaign: response.campaign || null,
          appliedCouponCode: normalizedCode,
          message:
            response.message ||
            `${normalizedCode} kupon kodu başarıyla uygulandı!`,
        };
      }

      // Başarısız yanıt
      return {
        success: false,
        discount: 0,
        message: response.message || "Kupon kodu geçersiz veya süresi dolmuş.",
      };
    } catch (error) {
      debugLog("Kupon doğrulama hatası:", { error: error?.message });

      // Hata mesajını kullanıcıya anlamlı şekilde göster
      let errorMessage = "Kupon kontrol edilemedi. Lütfen tekrar deneyin.";

      if (error?.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else if (error?.message) {
        // Network hatası gibi
        errorMessage =
          "Bağlantı hatası. Lütfen internet bağlantınızı kontrol edin.";
      }

      return {
        success: false,
        discount: 0,
        message: errorMessage,
      };
    }
  },

  /**
   * Uygulanmış kuponu kaldırır
   * @param {string} couponCode - Kaldırılacak kupon kodu
   * @returns {Object} { success, message }
   */
  removeCouponCode: (couponCode) => {
    debugLog("Kupon kaldırıldı:", { couponCode });
    return {
      success: true,
      message: `${couponCode} kupon kodu kaldırıldı.`,
    };
  },
};

export default CampaignService;
