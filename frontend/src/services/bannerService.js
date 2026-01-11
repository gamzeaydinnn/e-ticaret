// Banner service
const mockBanners = [
  {
    id: 1,
    title: "TAZE VE DOĞAL İNDİRİM REYONU",
    imageUrl: "/images/taze-dogal-indirim-banner.png",
    type: "slider",
    displayOrder: 1,
    isActive: true,
  },
  {
    id: 2,
    title: "İLK ALIŞVERİŞİNİZE %25 İNDİRİM",
    imageUrl: "/images/ilk-alisveris-indirim-banner.png",
    type: "slider",
    displayOrder: 2,
    isActive: true,
  },
  {
    id: 3,
    title: "MEYVE REYONUMUZ",
    imageUrl: "/images/meyve-reyonu-banner.png",
    type: "slider",
    displayOrder: 3,
    isActive: true,
  },
];

export const getAllBanners = async () => {
  return Promise.resolve(mockBanners);
};

export const getActiveBanners = async () => {
  return Promise.resolve(mockBanners.filter((b) => b.isActive));
};

export default {
  getAllBanners,
  getActiveBanners,
};
