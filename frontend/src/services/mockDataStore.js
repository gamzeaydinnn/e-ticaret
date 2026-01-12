// src/services/mockDataStore.js
// Centralized mock data store - admin paneli ve ana sayfa arasında senkronizasyon sağlar
// localStorage ile kalıcı veri depolama

const STORAGE_KEYS = {
  categories: "mockStore_categories",
  products: "mockStore_products",
  posters: "mockStore_posters",
  version: "mockStore_version",
};

// Versiyon kontrolü - yeni özellikler eklendiğinde localStorage'ı sıfırlar
const CURRENT_VERSION = "3.0.0"; // Slider düzeltmesi

// Slug oluşturma yardımcı fonksiyonu
const createSlug = (name) => {
  return name
    .toLowerCase()
    .replace(/ç/g, "c")
    .replace(/ğ/g, "g")
    .replace(/ı/g, "i")
    .replace(/ö/g, "o")
    .replace(/ş/g, "s")
    .replace(/ü/g, "u")
    .replace(/[^a-z0-9\s-]/g, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .trim();
};

// Varsayılan kategoriler
const defaultCategories = [
  {
    id: 1,
    name: "Et ve Et Ürünleri",
    slug: "et-ve-et-urunleri",
    description: "Taze et ve şarküteri ürünleri",
    icon: "fa-drumstick-bite",
    productCount: 3,
    isActive: true,
  },
  {
    id: 2,
    name: "Süt Ürünleri",
    slug: "sut-ve-sut-urunleri",
    description: "Süt, peynir, yoğurt ve türevleri",
    icon: "fa-cheese",
    productCount: 2,
    isActive: true,
  },
  {
    id: 3,
    name: "Meyve ve Sebze",
    slug: "meyve-ve-sebze",
    description: "Taze meyve ve sebzeler",
    icon: "fa-apple-alt",
    productCount: 3,
    isActive: true,
  },
  {
    id: 4,
    name: "İçecekler",
    slug: "icecekler",
    description: "Soğuk ve sıcak içecekler",
    icon: "fa-coffee",
    productCount: 3,
    isActive: true,
  },
  {
    id: 5,
    name: "Atıştırmalık",
    slug: "atistirmalik",
    description: "Cipsi, kraker ve atıştırmalıklar",
    icon: "fa-cookie-bite",
    productCount: 1,
    isActive: true,
  },
  {
    id: 6,
    name: "Temizlik",
    slug: "temizlik",
    description: "Ev temizlik ürünleri",
    icon: "fa-broom",
    productCount: 1,
    isActive: true,
  },
  {
    id: 7,
    name: "Temel Gıda",
    slug: "temel-gida",
    description: "Bakliyat, makarna, pirinç",
    icon: "fa-seedling",
    productCount: 1,
    isActive: true,
  },
];

// Varsayılan ürünler
const defaultProducts = [
  {
    id: 1,
    name: "Dana Kuşbaşı",
    categoryId: 1,
    categoryName: "Et ve Et Ürünleri",
    price: 89.9,
    stock: 25,
    description: "Taze dana eti kuşbaşı",
    imageUrl: "/images/dana-kusbasi.jpg",
    isActive: true,
  },
  {
    id: 2,
    name: "Kuzu İncik",
    categoryId: 1,
    categoryName: "Et ve Et Ürünleri",
    price: 95.5,
    stock: 15,
    description: "Taze kuzu incik eti",
    imageUrl: "/images/kuzu-incik.webp",
    isActive: true,
  },
  {
    id: 3,
    name: "Sucuk 250gr",
    categoryId: 1,
    categoryName: "Et ve Et Ürünleri",
    price: 24.9,
    stock: 30,
    description: "Geleneksel sucuk",
    imageUrl: "/images/sucuk.jpg",
    isActive: true,
  },
  {
    id: 4,
    name: "Pınar Süt 1L",
    categoryId: 2,
    categoryName: "Süt Ürünleri",
    price: 12.5,
    stock: 50,
    description: "Taze tam yağlı süt",
    imageUrl: "/images/pinar-nestle-sut.jpg",
    isActive: true,
  },
  {
    id: 5,
    name: "Şek Kaşar Peyniri 200gr",
    categoryId: 2,
    categoryName: "Süt Ürünleri",
    price: 35.9,
    stock: 20,
    description: "Eski kaşar peynir",
    imageUrl: "/images/sek-kasar-peyniri-200-gr-38be46-1650x1650.jpg",
    isActive: true,
  },
  {
    id: 6,
    name: "Domates Kg",
    categoryId: 3,
    categoryName: "Meyve ve Sebze",
    price: 8.75,
    stock: 100,
    description: "Taze domates",
    imageUrl: "/images/domates.webp",
    isActive: true,
  },
  {
    id: 7,
    name: "Salatalık Kg",
    categoryId: 3,
    categoryName: "Meyve ve Sebze",
    price: 6.5,
    stock: 80,
    description: "Taze salatalık",
    imageUrl: "/images/salatalik.jpg",
    isActive: true,
  },
  {
    id: 8,
    name: "Bulgur 1 Kg",
    categoryId: 7,
    categoryName: "Temel Gıda",
    price: 15.9,
    stock: 40,
    description: "Pilavlık bulgur",
    imageUrl: "/images/bulgur.png",
    isActive: true,
  },
  {
    id: 9,
    name: "Coca Cola 330ml",
    categoryId: 4,
    categoryName: "İçecekler",
    price: 5.5,
    stock: 75,
    description: "Coca Cola teneke kutu",
    imageUrl: "/images/coca-cola.jpg",
    isActive: true,
  },
  {
    id: 10,
    name: "Lipton Ice Tea 330ml",
    categoryId: 4,
    categoryName: "İçecekler",
    price: 4.75,
    stock: 60,
    description: "Şeftali aromalı ice tea",
    imageUrl: "/images/lipton-ice-tea.jpg",
    isActive: true,
  },
  {
    id: 11,
    name: "Nescafe 200gr",
    categoryId: 4,
    categoryName: "İçecekler",
    price: 45.9,
    stock: 25,
    description: "Klasik nescafe",
    imageUrl: "/images/nescafe.jpg",
    isActive: true,
  },
  {
    id: 12,
    name: "Tahıl Cipsi 150gr",
    categoryId: 5,
    categoryName: "Atıştırmalık",
    price: 12.9,
    stock: 35,
    description: "Çıtır tahıl cipsi",
    imageUrl: "/images/tahil-cipsi.jpg",
    isActive: true,
  },
  {
    id: 13,
    name: "Cif Krem Temizleyici",
    categoryId: 6,
    categoryName: "Temizlik",
    price: 15.9,
    stock: 45,
    description: "Mutfak temizleyici",
    imageUrl: "/images/yeşil-cif-krem.jpg",
    isActive: true,
  },
];

// Varsayılan posterler (3 slider + 4 promo)
const defaultPosters = [
  {
    id: 1,
    title: "İlk Alışveriş İndirimi",
    imageUrl: "/images/ilk-alisveris-indirim-banner.png",
    linkUrl: "/kampanyalar/ilk-alisveris",
    type: "slider",
    displayOrder: 1,
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 2,
    title: "Taze ve Doğal İndirim Reyonu",
    imageUrl: "/images/taze-dogal-indirim-banner.png",
    linkUrl: "/kategori/meyve-ve-sebze",
    type: "slider",
    displayOrder: 2,
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 3,
    title: "Meyve Reyonumuz",
    imageUrl: "/images/meyve-reyonu-banner.png",
    linkUrl: "/kategori/meyve-ve-sebze",
    type: "slider",
    displayOrder: 3,
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 4,
    title: "Özel Fiyat Köy Sütü",
    imageUrl: "/images/ozel-fiyat-koy-sutu.png",
    linkUrl: "/urun/koy-sutu",
    type: "promo",
    displayOrder: 1,
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 5,
    title: "Temizlik Malzemeleri",
    imageUrl: "/images/temizlik-malzemeleri.png",
    linkUrl: "/kategori/temizlik",
    type: "promo",
    displayOrder: 2,
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 6,
    title: "Taze Günlük Lezzetli",
    imageUrl: "/images/taze-gunluk-lezzetli.png",
    linkUrl: "/kategori/sut-ve-sut-urunleri",
    type: "promo",
    displayOrder: 3,
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 7,
    title: "Gölköy Gurme Et",
    imageUrl: "/images/golkoy-banner-2.png",
    linkUrl: "/kategori/et-ve-et-urunleri",
    type: "promo",
    displayOrder: 4,
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
];

// Versiyon kontrolü - eski localStorage verileri varsa sıfırla
const checkVersion = () => {
  try {
    const storedVersion = localStorage.getItem(STORAGE_KEYS.version);
    if (storedVersion !== CURRENT_VERSION) {
      console.log("MockDataStore: Yeni versiyon, tüm veriler sıfırlanıyor...");
      // Tüm mock verileri temizle
      Object.values(STORAGE_KEYS).forEach(key => localStorage.removeItem(key));
      localStorage.setItem(STORAGE_KEYS.version, CURRENT_VERSION);
    }
  } catch (e) {
    console.warn("Versiyon kontrolü hatası:", e);
  }
};
checkVersion();

// localStorage'dan veri yükle veya varsayılanı kullan
const loadFromStorage = (key, defaultData) => {
  try {
    const stored = localStorage.getItem(key);
    if (stored) {
      return JSON.parse(stored);
    }
  } catch (e) {
    console.warn(`localStorage okuma hatası (${key}):`, e);
  }
  return [...defaultData];
};

// localStorage'a kaydet
const saveToStorage = (key, data) => {
  try {
    localStorage.setItem(key, JSON.stringify(data));
  } catch (e) {
    console.warn(`localStorage yazma hatası (${key}):`, e);
  }
};

// Verileri localStorage'dan yükle
let categories = loadFromStorage(STORAGE_KEYS.categories, defaultCategories);
let products = loadFromStorage(STORAGE_KEYS.products, defaultProducts);
let posters = loadFromStorage(STORAGE_KEYS.posters, defaultPosters);

// Event listeners for real-time updates
const listeners = {
  products: [],
  categories: [],
  posters: [],
};

// Subscribe to changes
export const subscribe = (type, callback) => {
  if (listeners[type]) {
    listeners[type].push(callback);
    return () => {
      listeners[type] = listeners[type].filter((cb) => cb !== callback);
    };
  }
};

// Notify listeners
const notify = (type) => {
  if (listeners[type]) {
    listeners[type].forEach((cb) => cb());
  }
};

// ============ PRODUCTS ============

export const getProducts = () => {
  return [...products].filter((p) => p.isActive);
};

export const getAllProducts = () => {
  return [...products];
};

export const getProductById = (id) => {
  return products.find((p) => p.id === Number(id));
};

export const createProduct = (productData) => {
  const newId = Math.max(...products.map((p) => p.id), 0) + 1;
  const category = categories.find(
    (c) => c.id === Number(productData.categoryId)
  );

  const newProduct = {
    ...productData,
    id: newId,
    price: parseFloat(productData.price) || 0,
    stock: parseInt(productData.stock) || 0,
    categoryName: category ? category.name : "Kategori Yok",
    isActive: productData.isActive !== false,
  };

  products.push(newProduct);

  // Update category product count
  if (category) {
    category.productCount = (category.productCount || 0) + 1;
    saveToStorage(STORAGE_KEYS.categories, categories);
  }

  saveToStorage(STORAGE_KEYS.products, products);
  notify("products");
  return newProduct;
};

export const updateProduct = (id, productData) => {
  const index = products.findIndex((p) => p.id === Number(id));
  if (index === -1) throw new Error("Ürün bulunamadı");

  const category = categories.find(
    (c) => c.id === Number(productData.categoryId)
  );

  products[index] = {
    ...products[index],
    ...productData,
    id: Number(id),
    price: parseFloat(productData.price) || products[index].price,
    stock: parseInt(productData.stock) || products[index].stock,
    categoryName: category ? category.name : products[index].categoryName,
  };

  saveToStorage(STORAGE_KEYS.products, products);
  notify("products");
  return products[index];
};

export const deleteProduct = (id) => {
  const index = products.findIndex((p) => p.id === Number(id));
  if (index === -1) throw new Error("Ürün bulunamadı");

  const product = products[index];
  const category = categories.find((c) => c.id === product.categoryId);

  products.splice(index, 1);

  // Update category product count
  if (category && category.productCount > 0) {
    category.productCount--;
    saveToStorage(STORAGE_KEYS.categories, categories);
  }

  saveToStorage(STORAGE_KEYS.products, products);
  notify("products");
  return { success: true };
};

// ============ CATEGORIES ============

export const getCategories = () => {
  return [...categories].filter((c) => c.isActive);
};

export const getAllCategories = () => {
  return [...categories];
};

export const getCategoryById = (id) => {
  return categories.find((c) => c.id === Number(id));
};

export const createCategory = (categoryData) => {
  const newId = Math.max(...categories.map((c) => c.id), 0) + 1;

  // Otomatik slug oluştur
  const slug = categoryData.slug || createSlug(categoryData.name);

  const newCategory = {
    ...categoryData,
    id: newId,
    slug,
    productCount: 0,
    isActive: categoryData.isActive !== false,
  };

  categories.push(newCategory);
  saveToStorage(STORAGE_KEYS.categories, categories);
  notify("categories");
  return newCategory;
};

export const updateCategory = (id, categoryData) => {
  const index = categories.findIndex((c) => c.id === Number(id));
  if (index === -1) throw new Error("Kategori bulunamadı");

  const oldName = categories[index].name;

  // Slug güncelle
  const slug = categoryData.slug || createSlug(categoryData.name);

  categories[index] = {
    ...categories[index],
    ...categoryData,
    id: Number(id),
    slug,
  };

  // Update products with new category name
  if (oldName !== categoryData.name) {
    products.forEach((p) => {
      if (p.categoryId === Number(id)) {
        p.categoryName = categoryData.name;
      }
    });
    saveToStorage(STORAGE_KEYS.products, products);
    notify("products");
  }

  saveToStorage(STORAGE_KEYS.categories, categories);
  notify("categories");
  return categories[index];
};

export const deleteCategory = (id) => {
  const index = categories.findIndex((c) => c.id === Number(id));
  if (index === -1) throw new Error("Kategori bulunamadı");

  categories.splice(index, 1);
  saveToStorage(STORAGE_KEYS.categories, categories);
  notify("categories");
  return { success: true };
};

// ============ POSTERS ============

export const getPosters = () => {
  return [...posters].filter((p) => p.isActive);
};

export const getAllPosters = () => {
  return [...posters];
};

export const getPosterById = (id) => {
  return posters.find((p) => p.id === Number(id));
};

export const createPoster = (posterData) => {
  // Validation: title and imageUrl are required
  if (!posterData.title || !posterData.title.trim()) {
    throw new Error("Poster başlığı zorunludur");
  }
  if (!posterData.imageUrl || !posterData.imageUrl.trim()) {
    throw new Error("Görsel URL zorunludur");
  }
  // Validate type
  if (posterData.type && !["slider", "promo"].includes(posterData.type)) {
    throw new Error("Poster tipi 'slider' veya 'promo' olmalıdır");
  }

  const newId = Math.max(...posters.map((p) => p.id), 0) + 1;
  const now = new Date().toISOString();

  const newPoster = {
    id: newId,
    title: posterData.title.trim(),
    imageUrl: posterData.imageUrl.trim(),
    linkUrl: posterData.linkUrl || "",
    type: posterData.type || "slider",
    displayOrder: parseInt(posterData.displayOrder) || 1,
    isActive: posterData.isActive !== false,
    createdAt: now,
    updatedAt: now,
  };

  posters.push(newPoster);
  saveToStorage(STORAGE_KEYS.posters, posters);
  notify("posters");
  return newPoster;
};

export const updatePoster = (id, posterData) => {
  const index = posters.findIndex((p) => p.id === Number(id));
  if (index === -1) throw new Error("Poster bulunamadı");

  // Validation: title and imageUrl are required if provided
  if (posterData.title !== undefined && !posterData.title.trim()) {
    throw new Error("Poster başlığı boş olamaz");
  }
  if (posterData.imageUrl !== undefined && !posterData.imageUrl.trim()) {
    throw new Error("Görsel URL boş olamaz");
  }
  // Validate type if provided
  if (posterData.type && !["slider", "promo"].includes(posterData.type)) {
    throw new Error("Poster tipi 'slider' veya 'promo' olmalıdır");
  }

  posters[index] = {
    ...posters[index],
    ...posterData,
    id: Number(id), // Preserve original ID
    displayOrder:
      posterData.displayOrder !== undefined
        ? parseInt(posterData.displayOrder)
        : posters[index].displayOrder,
    updatedAt: new Date().toISOString(),
  };

  saveToStorage(STORAGE_KEYS.posters, posters);
  notify("posters");
  return posters[index];
};

export const deletePoster = (id) => {
  const index = posters.findIndex((p) => p.id === Number(id));
  if (index === -1) {
    // Idempotent: return success even if not found
    return { success: true };
  }

  posters.splice(index, 1);
  saveToStorage(STORAGE_KEYS.posters, posters);
  notify("posters");
  return { success: true };
};

// Get active slider posters sorted by displayOrder, then by ID
export const getSliderPosters = () => {
  return [...posters]
    .filter((p) => p.isActive && p.type === "slider")
    .sort((a, b) => {
      if (a.displayOrder !== b.displayOrder) {
        return a.displayOrder - b.displayOrder;
      }
      return a.id - b.id;
    });
};

// Get active promo posters sorted by displayOrder, then by ID
export const getPromoPosters = () => {
  return [...posters]
    .filter((p) => p.isActive && p.type === "promo")
    .sort((a, b) => {
      if (a.displayOrder !== b.displayOrder) {
        return a.displayOrder - b.displayOrder;
      }
      return a.id - b.id;
    });
};

// ============ SEARCH ============

export const searchProducts = (query) => {
  const q = query.toLowerCase();
  return products.filter(
    (p) =>
      p.isActive &&
      (p.name.toLowerCase().includes(q) ||
        p.description?.toLowerCase().includes(q) ||
        p.categoryName?.toLowerCase().includes(q))
  );
};

export const getProductsByCategory = (categoryId) => {
  return products.filter(
    (p) => p.isActive && p.categoryId === Number(categoryId)
  );
};

// ============ RESET (Geliştirme için) ============

export const resetToDefaults = () => {
  categories = [...defaultCategories];
  products = [...defaultProducts];
  posters = [...defaultPosters];
  saveToStorage(STORAGE_KEYS.categories, categories);
  saveToStorage(STORAGE_KEYS.products, products);
  saveToStorage(STORAGE_KEYS.posters, posters);
  notify("categories");
  notify("products");
  notify("posters");
};

export default {
  // Products
  getProducts,
  getAllProducts,
  getProductById,
  createProduct,
  updateProduct,
  deleteProduct,
  searchProducts,
  getProductsByCategory,
  // Categories
  getCategories,
  getAllCategories,
  getCategoryById,
  createCategory,
  updateCategory,
  deleteCategory,
  // Posters
  getPosters,
  getAllPosters,
  getPosterById,
  createPoster,
  updatePoster,
  deletePoster,
  getSliderPosters,
  getPromoPosters,
  // Subscription
  subscribe,
  // Reset
  resetToDefaults,
};
