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
      const data = await api.get("/api/campaigns");
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
};
