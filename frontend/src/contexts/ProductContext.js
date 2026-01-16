// ============================================================
// PRODUCT CONTEXT - Ürün State Yönetimi ve Global Senkronizasyon
// ============================================================
// Bu context, ürün verilerinin merkezi yönetimini sağlar.
// Admin panelinde yapılan CRUD işlemleri otomatik olarak
// tüm bileşenlere (ana sayfa, kategori sayfaları vb.) yansır.
//
// Özellikler:
// - Merkezi ürün state yönetimi
// - CRUD sonrası otomatik refetch
// - Subscription pattern ile real-time güncellemeler
// - Loading ve error state yönetimi
// - Cache mekanizması (performans optimizasyonu)
// ============================================================

import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  useMemo,
  useRef,
} from "react";
import { ProductService } from "../services/productService";

// ============================================================
// CONTEXT OLUŞTURMA
// ============================================================
const ProductContext = createContext(null);

// ============================================================
// CUSTOM HOOK - useProducts
// ============================================================
/**
 * ProductContext'e erişim için hook
 * Context dışında kullanılırsa hata fırlatır
 * @returns {object} Product context değerleri
 */
export const useProducts = () => {
  const context = useContext(ProductContext);
  if (!context) {
    throw new Error(
      "useProducts hook'u ProductProvider içinde kullanılmalıdır"
    );
  }
  return context;
};

// ============================================================
// CACHE AYARLARI
// ============================================================
const CACHE_CONFIG = {
  // Ürün listesi cache süresi (5 dakika)
  PRODUCTS_TTL: 5 * 60 * 1000,
  // Kategori bazlı cache süresi (3 dakika)
  CATEGORY_TTL: 3 * 60 * 1000,
  // Tek ürün cache süresi (10 dakika)
  PRODUCT_TTL: 10 * 60 * 1000,
};

// ============================================================
// PROVIDER BİLEŞENİ
// ============================================================
export const ProductProvider = ({ children }) => {
  // -----------------------------------------------------------
  // STATE YÖNETİMİ
  // -----------------------------------------------------------

  // Ana ürün listesi (tüm aktif ürünler)
  const [products, setProducts] = useState([]);

  // Admin ürün listesi (aktif + pasif tüm ürünler)
  const [adminProducts, setAdminProducts] = useState([]);

  // Kategori bazlı ürünler cache'i
  const [categoryProducts, setCategoryProducts] = useState({});

  // Loading state'leri
  const [loading, setLoading] = useState(false);
  const [adminLoading, setAdminLoading] = useState(false);

  // Error state'leri
  const [error, setError] = useState(null);

  // Son güncelleme zamanları (cache için)
  const lastFetchRef = useRef({
    products: 0,
    adminProducts: 0,
    categories: {},
  });

  // Component mounted kontrolü (memory leak önleme)
  const isMountedRef = useRef(true);

  // -----------------------------------------------------------
  // CACHE HELPER FONKSİYONLARI
  // -----------------------------------------------------------

  /**
   * Cache'in geçerli olup olmadığını kontrol eder
   * @param {string} key - Cache key'i
   * @param {number} ttl - Time to live (ms)
   * @returns {boolean} Cache geçerli mi?
   */
  const isCacheValid = useCallback((key, ttl) => {
    const lastFetch = lastFetchRef.current[key] || 0;
    return Date.now() - lastFetch < ttl;
  }, []);

  /**
   * Cache zamanını günceller
   * @param {string} key - Cache key'i
   */
  const updateCacheTime = useCallback((key) => {
    lastFetchRef.current[key] = Date.now();
  }, []);

  /**
   * Tüm cache'i invalidate eder
   * CRUD işlemlerinden sonra çağrılır
   */
  const invalidateCache = useCallback(() => {
    lastFetchRef.current = {
      products: 0,
      adminProducts: 0,
      categories: {},
    };
    setCategoryProducts({});
  }, []);

  // -----------------------------------------------------------
  // VERİ YÜKLEME FONKSİYONLARI
  // -----------------------------------------------------------

  /**
   * Aktif ürünleri yükler (ana sayfa için)
   * @param {boolean} forceRefresh - Cache'i yoksay
   * @returns {Promise<Array>} Ürün listesi
   */
  const fetchProducts = useCallback(
    async (forceRefresh = false) => {
      // Cache kontrolü
      if (
        !forceRefresh &&
        isCacheValid("products", CACHE_CONFIG.PRODUCTS_TTL)
      ) {
        return products;
      }

      setLoading(true);
      setError(null);

      try {
        const items = await ProductService.list();

        if (isMountedRef.current) {
          setProducts(items || []);
          updateCacheTime("products");
        }

        return items;
      } catch (err) {
        console.error("❌ Ürünler yüklenemedi:", err);
        if (isMountedRef.current) {
          setError(err.message || "Ürünler yüklenirken hata oluştu");
        }
        return [];
      } finally {
        if (isMountedRef.current) {
          setLoading(false);
        }
      }
    },
    [products, isCacheValid, updateCacheTime]
  );

  /**
   * Admin için tüm ürünleri yükler (aktif + pasif)
   * @param {boolean} forceRefresh - Cache'i yoksay
   * @returns {Promise<Array>} Tüm ürünler
   */
  const fetchAdminProducts = useCallback(
    async (forceRefresh = false) => {
      // Cache kontrolü
      if (
        !forceRefresh &&
        isCacheValid("adminProducts", CACHE_CONFIG.PRODUCTS_TTL)
      ) {
        return adminProducts;
      }

      setAdminLoading(true);

      try {
        const items = await ProductService.getAll();

        if (isMountedRef.current) {
          setAdminProducts(items || []);
          updateCacheTime("adminProducts");
        }

        return items;
      } catch (err) {
        console.error("❌ Admin ürünleri yüklenemedi:", err);
        return [];
      } finally {
        if (isMountedRef.current) {
          setAdminLoading(false);
        }
      }
    },
    [adminProducts, isCacheValid, updateCacheTime]
  );

  /**
   * Kategoriye göre ürünleri yükler
   * @param {number} categoryId - Kategori ID
   * @param {boolean} forceRefresh - Cache'i yoksay
   * @returns {Promise<Array>} Kategori ürünleri
   */
  const fetchByCategory = useCallback(
    async (categoryId, forceRefresh = false) => {
      if (!categoryId) return [];

      const cacheKey = `category_${categoryId}`;

      // Cache kontrolü
      if (!forceRefresh && categoryProducts[categoryId]) {
        const lastFetch = lastFetchRef.current.categories[categoryId] || 0;
        if (Date.now() - lastFetch < CACHE_CONFIG.CATEGORY_TTL) {
          return categoryProducts[categoryId];
        }
      }

      try {
        const items = await ProductService.getByCategory(categoryId);

        if (isMountedRef.current) {
          setCategoryProducts((prev) => ({
            ...prev,
            [categoryId]: items || [],
          }));
          lastFetchRef.current.categories[categoryId] = Date.now();
        }

        return items;
      } catch (err) {
        console.error(`❌ Kategori ${categoryId} ürünleri yüklenemedi:`, err);
        return [];
      }
    },
    [categoryProducts]
  );

  /**
   * Tek ürün detayını getirir
   * @param {number} productId - Ürün ID
   * @returns {Promise<object|null>} Ürün objesi
   */
  const getProduct = useCallback(
    async (productId) => {
      if (!productId) return null;

      // Önce local cache'de ara
      const cached =
        products.find((p) => p.id === productId) ||
        adminProducts.find((p) => p.id === productId);

      // Cache'de varsa ve yakın zamanda yüklendiyse döndür
      if (cached && isCacheValid("products", CACHE_CONFIG.PRODUCT_TTL)) {
        return cached;
      }

      // API'den çek
      try {
        const product = await ProductService.get(productId);
        return product;
      } catch (err) {
        console.error(`❌ Ürün ${productId} bulunamadı:`, err);
        return null;
      }
    },
    [products, adminProducts, isCacheValid]
  );

  // -----------------------------------------------------------
  // CRUD İŞLEMLERİ (Admin)
  // -----------------------------------------------------------

  /**
   * Yeni ürün oluşturur ve cache'i günceller
   * @param {object} productData - Ürün verileri
   * @returns {Promise<object>} Oluşturulan ürün
   */
  const createProduct = useCallback(
    async (productData) => {
      try {
        const result = await ProductService.createAdmin(productData);

        // Cache'i invalidate et - tüm listeler yenilenecek
        invalidateCache();

        // Admin listesini hemen güncelle (optimistic update değil, API sonrası)
        fetchAdminProducts(true);
        fetchProducts(true);

        return result;
      } catch (err) {
        console.error("❌ Ürün oluşturma hatası:", err);
        throw err;
      }
    },
    [invalidateCache, fetchAdminProducts, fetchProducts]
  );

  /**
   * Mevcut ürünü günceller
   * @param {number} productId - Ürün ID
   * @param {object} productData - Güncellenecek veriler
   * @returns {Promise<object>} Güncellenen ürün
   */
  const updateProduct = useCallback(
    async (productId, productData) => {
      try {
        const result = await ProductService.updateAdmin(productId, productData);

        // Cache'i invalidate et
        invalidateCache();

        // Listeleri yenile
        fetchAdminProducts(true);
        fetchProducts(true);

        return result;
      } catch (err) {
        console.error(`❌ Ürün ${productId} güncelleme hatası:`, err);
        throw err;
      }
    },
    [invalidateCache, fetchAdminProducts, fetchProducts]
  );

  /**
   * Ürünü siler
   * @param {number} productId - Silinecek ürün ID
   * @returns {Promise<void>}
   */
  const deleteProduct = useCallback(
    async (productId) => {
      try {
        await ProductService.deleteAdmin(productId);

        // Optimistic update - UI'dan hemen kaldır
        setProducts((prev) => prev.filter((p) => p.id !== productId));
        setAdminProducts((prev) => prev.filter((p) => p.id !== productId));

        // Cache'i invalidate et
        invalidateCache();
      } catch (err) {
        console.error(`❌ Ürün ${productId} silme hatası:`, err);
        // Hata durumunda listeyi yenile
        fetchAdminProducts(true);
        throw err;
      }
    },
    [invalidateCache, fetchAdminProducts]
  );

  /**
   * Ürün stoğunu günceller
   * @param {number} productId - Ürün ID
   * @param {number} newStock - Yeni stok miktarı
   * @returns {Promise<object>}
   */
  const updateStock = useCallback(async (productId, newStock) => {
    try {
      const result = await ProductService.updateStockAdmin(productId, newStock);

      // Optimistic update
      setProducts((prev) =>
        prev.map((p) =>
          p.id === productId
            ? { ...p, stock: newStock, stockQuantity: newStock }
            : p
        )
      );
      setAdminProducts((prev) =>
        prev.map((p) =>
          p.id === productId
            ? { ...p, stock: newStock, stockQuantity: newStock }
            : p
        )
      );

      return result;
    } catch (err) {
      console.error(`❌ Stok güncelleme hatası (ID: ${productId}):`, err);
      throw err;
    }
  }, []);

  // -----------------------------------------------------------
  // EXCEL İŞLEMLERİ
  // -----------------------------------------------------------

  /**
   * Excel'den ürün import eder
   * @param {File} file - Excel dosyası
   * @returns {Promise<object>} Import sonucu
   */
  const importFromExcel = useCallback(
    async (file) => {
      try {
        const result = await ProductService.importExcel(file);

        // Import başarılıysa cache'i invalidate et
        if (result?.successCount > 0) {
          invalidateCache();
          fetchAdminProducts(true);
          fetchProducts(true);
        }

        return result;
      } catch (err) {
        console.error("❌ Excel import hatası:", err);
        throw err;
      }
    },
    [invalidateCache, fetchAdminProducts, fetchProducts]
  );

  /**
   * Mevcut ürünleri Excel'e export eder
   * @returns {Promise<Blob>} Excel dosyası
   */
  const exportToExcel = useCallback(async () => {
    return await ProductService.exportExcel();
  }, []);

  /**
   * Excel şablonu indirir
   * @returns {Promise<Blob>} Şablon dosyası
   */
  const downloadTemplate = useCallback(async () => {
    return await ProductService.downloadTemplate();
  }, []);

  // -----------------------------------------------------------
  // RESİM YÜKLEME
  // -----------------------------------------------------------

  /**
   * Ürün resmi yükler
   * @param {File} imageFile - Resim dosyası
   * @returns {Promise<{success: boolean, imageUrl: string}>}
   */
  const uploadImage = useCallback(async (imageFile) => {
    return await ProductService.uploadImage(imageFile);
  }, []);

  // -----------------------------------------------------------
  // SUBSCRIPTION VE LIFECYCLE
  // -----------------------------------------------------------

  useEffect(() => {
    isMountedRef.current = true;

    // İlk yüklemede ürünleri çek
    fetchProducts();

    // ProductService subscription - diğer tab'larda yapılan değişiklikleri dinle
    const unsubscribe = ProductService.subscribe((event) => {
      console.log("[ProductContext] 📦 Değişiklik algılandı:", event.action);

      // CRUD işlemlerinde cache'i invalidate et ve yenile
      if (["create", "update", "delete", "import"].includes(event.action)) {
        invalidateCache();
        fetchProducts(true);
      }
    });

    // Cleanup
    return () => {
      isMountedRef.current = false;
      unsubscribe();
    };
  }, [fetchProducts, invalidateCache]);

  // -----------------------------------------------------------
  // CONTEXT VALUE
  // -----------------------------------------------------------

  const contextValue = useMemo(
    () => ({
      // State
      products,
      adminProducts,
      loading,
      adminLoading,
      error,

      // Veri yükleme
      fetchProducts,
      fetchAdminProducts,
      fetchByCategory,
      getProduct,

      // CRUD işlemleri
      createProduct,
      updateProduct,
      deleteProduct,
      updateStock,

      // Excel işlemleri
      importFromExcel,
      exportToExcel,
      downloadTemplate,

      // Resim yükleme
      uploadImage,

      // Cache yönetimi
      invalidateCache,

      // Helper - listeyi zorla yenile
      refreshProducts: () => fetchProducts(true),
      refreshAdminProducts: () => fetchAdminProducts(true),
    }),
    [
      products,
      adminProducts,
      loading,
      adminLoading,
      error,
      fetchProducts,
      fetchAdminProducts,
      fetchByCategory,
      getProduct,
      createProduct,
      updateProduct,
      deleteProduct,
      updateStock,
      importFromExcel,
      exportToExcel,
      downloadTemplate,
      uploadImage,
      invalidateCache,
    ]
  );

  return (
    <ProductContext.Provider value={contextValue}>
      {children}
    </ProductContext.Provider>
  );
};

// Default export
export default ProductContext;
