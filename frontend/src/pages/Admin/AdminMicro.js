import React, {
  useEffect,
  useState,
  useCallback,
  useRef,
  useMemo,
} from "react";
import { MicroService } from "../../services/microService";
import { useGlobalStockUpdates } from "../../hooks/useStockUpdates";
import SyncHealthDashboard from "../../components/Admin/SyncHealthDashboard";

// Yardımcı fonksiyonlar
const formatDuration = (ms) => {
  if (!ms || ms <= 0) return "-";
  const seconds = Math.floor(ms / 1000);
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  if (minutes > 0) return `${minutes}dk ${remainingSeconds}sn`;
  return `${seconds}sn`;
};

const formatNumber = (num) => {
  if (num === undefined || num === null) return "-";
  return new Intl.NumberFormat("tr-TR").format(num);
};

export default function AdminMicro() {
  // State tanımlamaları
  const [products, setProducts] = useState([]);
  const [stocks, setStocks] = useState([]);
  const [stokListesi, setStokListesi] = useState([]);
  const [stokListesiMeta, setStokListesiMeta] = useState({
    totalCount: 0,
    page: 1,
    pageSize: 100,
  });
  const [grupKodFilter, setGrupKodFilter] = useState("");
  const [grupKodlari, setGrupKodlari] = useState([]);
  const [depoListesi, setDepoListesi] = useState([
    { depoNo: 0, depoAdi: "Tum Depolar" },
  ]);
  const [aramaMetni, setAramaMetni] = useState("");
  const [stokDurumuFilter, setStokDurumuFilter] = useState("hepsi");
  const [connectionStatus, setConnectionStatus] = useState(null);
  const [connectionLoading, setConnectionLoading] = useState(false);

  // Depo seçimi state'i
  const [depoNo, setDepoNo] = useState(0); // 0 = Tüm depolar

  // Genel durum
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("info"); // success | danger | info | warning
  const [loading, setLoading] = useState(false);

  // Aktif sekme: 'overview' | 'stokListesi' | 'bulkFetch' | 'settings'
  const [activeTab, setActiveTab] = useState("overview");

  // ============================================================================
  // BULK FETCH STATE - Toplu ürün çekme için state'ler
  // ============================================================================
  const [bulkFetchState, setBulkFetchState] = useState({
    isRunning: false, // İşlem devam ediyor mu
    isComplete: false, // İşlem tamamlandı mı
    progress: 0, // İlerleme yüzdesi (0-100)
    currentPage: 0, // Şu anki sayfa
    totalPages: 0, // Toplam sayfa
    fetchedCount: 0, // Çekilen ürün sayısı
    totalCount: 0, // Toplam ürün sayısı
    failedPages: [], // Başarısız sayfalar
    elapsedMs: 0, // Geçen süre
    estimatedRemainingMs: 0, // Tahmini kalan süre
    error: null, // Hata mesajı
    stats: null, // İstatistikler
  });
  const [bulkFetchProducts, setBulkFetchProducts] = useState([]); // Çekilen ürünler

  // Bulk fetch sonuçları için sayfalama
  const [bulkFetchPagination, setBulkFetchPagination] = useState({
    currentPage: 1,
    pageSize: 50,
  });

  // Bulk fetch sonuçları için arama filtreleri
  const [bulkFetchFilters, setBulkFetchFilters] = useState({
    stokKod: "",
    grupKod: "",
    stokDurumu: "hepsi", // hepsi, stokta, stoksuz
    minStok: "",
  });

  // loadBulkSqlPage her re-render'da yeniden oluştuğundan, stale closure olmadan
  // kendinden çağrı yapabilmek için ref tutuyoruz (auto-retry page 1 için)
  const loadBulkSqlPageRef = useRef(null);

  const [bulkFetchConfig, setBulkFetchConfig] = useState({
    pageSize: 50, // Sayfa başına ürün
    throttleDelayMs: 300, // İstekler arası bekleme (ms)
    maxRetryCount: 3, // Max retry sayısı
    retryDelayMs: 3000, // Retry bekleme süresi (ms)
    syncMode: "newOnly", // newOnly | full
    grupKod: "", // Grup kodu filtresi
    fiyatListesiNo: 0, // 0 = Tüm fiyatlar / ilk uygun fiyat
    depoNo: 0, // Depo numarası (0 = tüm depolar)
    pasifDahil: false, // Pasif ürünleri de dahil et
  });

  // Ayarlar tab - aktif/pasif yonetimi ve import state
  const [activeProducts, setActiveProducts] = useState([]);
  const [activeProductsLoading, setActiveProductsLoading] = useState(false);
  const [activeSearchText, setActiveSearchText] = useState("");
  const [activeFilter, setActiveFilter] = useState("hepsi");
  const [activePage, setActivePage] = useState(1);
  const [activePageSize, setActivePageSize] = useState(20);
  const [activeTotalPages, setActiveTotalPages] = useState(1);
  const [activeTotalCount, setActiveTotalCount] = useState(0);
  const [selectedStokKodlar, setSelectedStokKodlar] = useState([]);
  const [importFile, setImportFile] = useState(null);
  const [syncDiagnostics, setSyncDiagnostics] = useState(null);
  const [syncDiagnosticsLoading, setSyncDiagnosticsLoading] = useState(false);
  const [syncActionLoading, setSyncActionLoading] = useState(null);

  // Abort controller ref - iptal için
  const bulkFetchAbortRef = useRef(null);

  // ==========================================================================
  // ANLIK STOK TAKİBİ (SignalR StockHub global feed)
  // NEDEN: HotPoll 10sn'de bir delta yayınlıyor, admin bu canlı akışı izlemeli
  // ==========================================================================
  const {
    updates: realtimeStockUpdates,
    isConnected: stockHubConnected,
    clearHistory: clearStockHistory,
    totalUpdates: totalStockUpdates,
  } = useGlobalStockUpdates({ maxHistory: 200 });

  // ============================================================================
  // Sayfa açılışında verileri yükle
  // ============================================================================
  useEffect(() => {
    loadInitial();
    testConnection();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Cleanup - component unmount olduğunda işlemi iptal et
  useEffect(() => {
    return () => {
      if (bulkFetchAbortRef.current) {
        bulkFetchAbortRef.current.abort();
      }
    };
  }, []);

  const loadInitial = async () => {
    try {
      setLoading(true);
      const [prods, stk, grupResult, depoResult] = await Promise.all([
        MicroService.getProducts(),
        MicroService.getStocks(),
        MicroService.getGrupKodlari(),
        MicroService.getDepoListesi(),
      ]);
      setProducts(prods || []);
      setStocks(stk || []);
      setGrupKodlari(Array.isArray(grupResult?.data) ? grupResult.data : []);
      setDepoListesi(
        Array.isArray(depoResult?.data) && depoResult.data.length > 0
          ? depoResult.data
          : [{ depoNo: 0, depoAdi: "Tum Depolar" }],
      );
    } catch (err) {
      setMessage(err.message || "Veriler yüklenemedi");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  };

  // ============================================================================
  // Mikro API Bağlantı Testi
  // ============================================================================
  const testConnection = useCallback(async () => {
    setConnectionLoading(true);
    try {
      const result = await MicroService.testConnection();

      // Backend'den gelen response: { isConnected, mikroApiOnline, databaseOnline, message, ... }
      // veya { success, message, toplamUrunSayisi, ... }
      const isConnected = result?.isConnected ?? result?.success ?? false;
      const mikroApiOnline = result?.mikroApiOnline ?? isConnected;
      const databaseOnline = result?.databaseOnline ?? true;
      const toplamUrunSayisi =
        Number(result?.toplamUrunSayisi || 0) > 0
          ? Number(result?.toplamUrunSayisi || 0)
          : Number(
              result?.cekilenKayitSayisi || result?.veritabaniUrunSayisi || 0,
            );

      setConnectionStatus({
        isConnected: isConnected || databaseOnline, // En az biri çalışıyorsa bağlı say
        mikroApiOnline,
        databaseOnline,
        message: result?.message,
        apiUrl: result?.apiUrl,
        toplamUrunSayisi,
      });

      if (mikroApiOnline) {
        setMessage("Mikro API bağlantısı başarılı!");
        setMessageType("success");
      } else if (databaseOnline) {
        setMessage(
          result?.message || "Mikro API offline. Veritabanından çalışılıyor.",
        );
        setMessageType("warning");
      } else {
        setMessage(result?.message || "Bağlantı kurulamadı");
        setMessageType("danger");
      }
    } catch (err) {
      setConnectionStatus({ isConnected: false, message: err.message });
      setMessage(
        "Bağlantı testi başarısız: " + (err.message || "Bilinmeyen hata"),
      );
      setMessageType("danger");
    } finally {
      setConnectionLoading(false);
    }
  }, []);

  // ============================================================================
  // Mikro API'den Stok Listesi Çek (StokListesiV2)
  // Fiyat listesi ve depo seçimine göre doğru verileri çeker
  // ============================================================================
  const loadStokListesi = useCallback(
    async (
      sayfa = 1,
      sayfaBuyuklugu = 100,
      grupKod = grupKodFilter,
      arama = aramaMetni,
    ) => {
      setLoading(true);
      try {
        const normalizedSearch = (arama || "").trim();
        const effectiveSearch =
          normalizedSearch.length >= 3 ? normalizedSearch : undefined;

        const result = await MicroService.getStokListesi({
          sayfa,
          sayfaBuyuklugu,
          depoNo: depoNo, // Seçilen depo (0 = tüm depolar)
          fiyatListesiNo: 0, // Tüm fiyatlar
          grupKod: (grupKod || "").trim() || undefined,
          aramaMetni: effectiveSearch,
          sadeceStoklu:
            stokDurumuFilter === "stokta"
              ? true
              : stokDurumuFilter === "stoksuz"
                ? false
                : undefined,
          sadeceAktif: true,
        });

        if (result?.success) {
          const fetchedData = Array.isArray(result.data) ? result.data : [];
          setStokListesi(fetchedData);

          const fetchedCount = fetchedData.length;
          const totalCountFromApi = Number(
            result.toplamKayit || result.totalCount || 0,
          );
          const effectiveTotalCount =
            totalCountFromApi > 0 ? totalCountFromApi : fetchedCount;

          // Backend'den gelen field isimleri: toplamKayit, sayfa, sayfaBuyuklugu
          setStokListesiMeta({
            totalCount: effectiveTotalCount,
            page: result.sayfa || result.page || sayfa,
            pageSize:
              result.sayfaBuyuklugu || result.pageSize || sayfaBuyuklugu,
          });

          // Offline mod kontrolü
          const offlineWarning = result.isOfflineMode
            ? " (Veritabanından)"
            : " (Mikro API'den)";
          setMessage(`${fetchedCount} ürün yüklendi${offlineWarning}`);
          setMessageType(result.isOfflineMode ? "warning" : "success");
        } else {
          setMessage(result?.message || "Stok listesi alınamadı");
          setMessageType("danger");
        }
      } catch (err) {
        setMessage(
          "Stok listesi alınamadı: " + (err.message || "Bilinmeyen hata"),
        );
        setMessageType("danger");
      } finally {
        setLoading(false);
      }
    },
    [grupKodFilter, aramaMetni, depoNo, stokDurumuFilter],
  );

  // Arama alanı için debounce (500ms) + minimum 3 karakter.
  useEffect(() => {
    if (activeTab !== "stokListesi") {
      return undefined;
    }

    const handle = setTimeout(() => {
      const value = (aramaMetni || "").trim();
      if (value.length === 0 || value.length >= 3) {
        loadStokListesi(1, 100, grupKodFilter, value);
      }
    }, 500);

    return () => clearTimeout(handle);
  }, [aramaMetni, activeTab, grupKodFilter, loadStokListesi]);

  const syncProducts = async () => {
    setLoading(true);
    try {
      const res = await MicroService.syncProducts();
      setMessage(res?.message || "Ürünler senkronize edildi");
      setMessageType("success");
      await fetchProducts();
    } catch (err) {
      setMessage(err.message || "Ürün senkronizasyonu başarısız");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  };

  const fetchProducts = async () => {
    try {
      const res = await MicroService.getProducts();
      setProducts(res || []);
    } catch (err) {
      setMessage(err.message || "Ürünler getirilemedi");
      setMessageType("danger");
    }
  };

  const fetchStocks = async () => {
    try {
      const res = await MicroService.getStocks();
      setStocks(res || []);
    } catch (err) {
      setMessage(err.message || "Stoklar getirilemedi");
      setMessageType("danger");
    }
  };

  const exportOrders = async () => {
    setLoading(true);
    try {
      const orders = []; // Geliştirme: seçim listesi entegre edilebilir
      const res = await MicroService.exportOrders(orders);
      setMessage(res?.message || "Siparişler ERP'ye aktarıldı");
      setMessageType("success");
    } catch (err) {
      setMessage(err.message || "Siparişler ERP'ye gönderilemedi");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  };

  const syncStocksFromERP = async () => {
    setLoading(true);
    try {
      const res = await MicroService.syncStocksFromERP();
      setMessage(res?.message || "Stoklar ERP'den alınıp güncellendi");
      setMessageType("success");
      await fetchStocks();
    } catch (err) {
      setMessage(err.message || "Stok senkronizasyonu başarısız");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  };

  const syncPricesFromERP = async () => {
    setLoading(true);
    try {
      const res = await MicroService.syncPricesFromERP();
      setMessage(res?.message || "Fiyatlar ERP'den alınıp güncellendi");
      setMessageType("success");
      await fetchProducts();
    } catch (err) {
      setMessage(err.message || "Fiyat senkronizasyonu başarısız");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  };

  const loadActiveProducts = useCallback(
    async (page = 1) => {
      setActiveProductsLoading(true);
      try {
        const normalizedSearch = activeSearchText.trim();
        const sadeceAktif =
          activeFilter === "hepsi" ? undefined : activeFilter === "aktif";

        // Aktif/pasif yönetimi, kullanıcı müdahalesi (toggle) yapılan cache
        // kaynağından okunur. Böylece listede görülen durum ile yazılan durum
        // birebir tutarlı kalır.
        const response = await MicroService.getCachedProducts({
          page,
          pageSize: activePageSize,
          search: normalizedSearch || undefined,
          sadeceAktif,
          sortBy: "stokKod",
          sortDesc: false,
        });

        if (!response?.success) {
          setMessage(response?.message || "Ürün listesi alınamadı");
          setMessageType("danger");
          return;
        }

        const items = Array.isArray(response.data) ? response.data : [];
        const pagination = response.pagination || {};
        const currentPage = Number(pagination.page || page || 1);
        const totalPages = Number(pagination.totalPages || 1);
        const totalCount = Number(pagination.totalCount || items.length || 0);

        setActiveProducts(items);
        setActivePage(currentPage);
        setActiveTotalPages(Math.max(1, totalPages));
        setActiveTotalCount(totalCount);
        setSelectedStokKodlar((prev) =>
          prev.filter((code) => items.some((item) => item.stokKod === code)),
        );
      } catch (err) {
        setMessage(err.message || "Ürün listesi alınamadı");
        setMessageType("danger");
      } finally {
        setActiveProductsLoading(false);
      }
    },
    [activeFilter, activePageSize, activeSearchText],
  );

  const toggleSingleProductActive = useCallback(
    async (stokKod, mevcutAktif) => {
      setLoading(true);
      try {
        const result = await MicroService.toggleProductActive(
          stokKod,
          !mevcutAktif,
        );
        if (!result?.success) {
          setMessage(result?.message || "Aktiflik durumu guncellenemedi");
          setMessageType("danger");
          return;
        }

        setMessage(result?.message || "Urun durumu guncellendi");
        setMessageType("success");
        await loadActiveProducts(activePage);
      } catch (err) {
        setMessage(err.message || "Aktiflik durumu guncellenemedi");
        setMessageType("danger");
      } finally {
        setLoading(false);
      }
    },
    [activePage, loadActiveProducts],
  );

  const bulkChangeProductStatus = useCallback(
    async (aktif) => {
      if (selectedStokKodlar.length === 0) {
        setMessage("Toplu islem icin en az bir urun secin");
        setMessageType("warning");
        return;
      }

      setLoading(true);
      try {
        const result = await MicroService.bulkToggleProductActive(
          selectedStokKodlar,
          aktif,
        );

        if (!result?.success) {
          setMessage(result?.message || "Toplu aktiflik guncellenemedi");
          setMessageType("danger");
          return;
        }

        setMessage(result?.message || "Toplu aktiflik guncellendi");
        setMessageType("success");
        setSelectedStokKodlar([]);
        await loadActiveProducts(activePage);
      } catch (err) {
        setMessage(err.message || "Toplu aktiflik guncellenemedi");
        setMessageType("danger");
      } finally {
        setLoading(false);
      }
    },
    [activePage, loadActiveProducts, selectedStokKodlar],
  );

  const handleImportActiveProducts = useCallback(async () => {
    if (!importFile) {
      setMessage("Import icin once dosya secin");
      setMessageType("warning");
      return;
    }

    setLoading(true);
    try {
      const result = await MicroService.importActiveProducts(importFile);
      if (!result?.success) {
        setMessage(result?.message || "Excel import basarisiz");
        setMessageType("danger");
        return;
      }

      const stats = result?.stats || {};
      setMessage(
        `${result?.message || "Import tamamlandi"} (Basarili: ${stats.successCount || 0}, Basarisiz: ${stats.failedCount || 0}, Atlanan: ${stats.skippedCount || 0})`,
      );
      setMessageType("success");
      setImportFile(null);
      await loadActiveProducts(1);
    } catch (err) {
      setMessage(err.message || "Excel import basarisiz");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  }, [importFile, loadActiveProducts]);

  const handleDownloadImportTemplate = useCallback(async () => {
    setLoading(true);
    try {
      const result = await MicroService.downloadImportTemplate();
      if (!result?.success) {
        setMessage(result?.message || "Template indirilemedi");
        setMessageType("danger");
        return;
      }

      setMessage("Import template indirildi");
      setMessageType("success");
    } catch (err) {
      setMessage(err.message || "Template indirilemedi");
      setMessageType("danger");
    } finally {
      setLoading(false);
    }
  }, []);

  const loadSyncDiagnostics = useCallback(async (hours = 24) => {
    setSyncDiagnosticsLoading(true);
    try {
      const result = await MicroService.getSyncDiagnostics(hours);
      if (!result?.success) {
        setMessage(result?.message || "Sync diagnostics alınamadı");
        setMessageType("danger");
        return;
      }

      setSyncDiagnostics(result.data || null);
    } catch (err) {
      setMessage(err.message || "Sync diagnostics alınamadı");
      setMessageType("danger");
    } finally {
      setSyncDiagnosticsLoading(false);
    }
  }, []);

  const handleRetrySyncLog = useCallback(
    async (logId) => {
      setSyncActionLoading(`retry-${logId}`);
      try {
        const result = await MicroService.retrySyncLog(logId);
        if (!result?.success) {
          setMessage(result?.message || "Retry işlemi başarısız");
          setMessageType("danger");
          return;
        }

        setMessage(result?.message || "Kayıt retry kuyruğuna alındı");
        setMessageType("success");
        await loadSyncDiagnostics(24);
      } catch (err) {
        setMessage(err.message || "Retry işlemi başarısız");
        setMessageType("danger");
      } finally {
        setSyncActionLoading(null);
      }
    },
    [loadSyncDiagnostics],
  );

  const handleResolveConflict = useCallback(
    async (logId, strategy) => {
      setSyncActionLoading(`resolve-${logId}-${strategy}`);
      try {
        const result = await MicroService.resolveSyncConflict(logId, strategy);
        if (!result?.success) {
          setMessage(result?.message || "Conflict çözüm işlemi başarısız");
          setMessageType("danger");
          return;
        }

        setMessage(result?.message || "Conflict çözüldü");
        setMessageType("success");
        await loadSyncDiagnostics(24);
      } catch (err) {
        setMessage(err.message || "Conflict çözüm işlemi başarısız");
        setMessageType("danger");
      } finally {
        setSyncActionLoading(null);
      }
    },
    [loadSyncDiagnostics],
  );

  const allVisibleActiveProductsSelected =
    activeProducts.length > 0 &&
    activeProducts.every((item) => selectedStokKodlar.includes(item.stokKod));

  const toggleSelectAllVisibleActiveProducts = () => {
    const visibleCodes = activeProducts
      .map((item) => item.stokKod)
      .filter(Boolean);

    if (allVisibleActiveProductsSelected) {
      setSelectedStokKodlar((prev) =>
        prev.filter((code) => !visibleCodes.includes(code)),
      );
      return;
    }

    setSelectedStokKodlar((prev) => {
      const merged = new Set([...prev, ...visibleCodes]);
      return Array.from(merged);
    });
  };

  const toggleSelectActiveProduct = (stokKod) => {
    setSelectedStokKodlar((prev) =>
      prev.includes(stokKod)
        ? prev.filter((code) => code !== stokKod)
        : [...prev, stokKod],
    );
  };

  // Sayfalama için
  const handlePageChange = (newPage) => {
    loadStokListesi(newPage, stokListesiMeta.pageSize, grupKodFilter);
  };

  // ============================================================================
  // BULK FETCH - Sıralı (Sequential) Toplu Ürün Çekme Fonksiyonu
  // ============================================================================
  // Gemini talimatlarına göre: Promise.all KULLANMA, sıralı istek yap,
  // retry mekanizması, throttle gecikmesi, ilerleme takibi
  // ============================================================================
  const loadBulkSqlPage = useCallback(
    async (
      page = 1,
      {
        syncFirst = false,
        syncModeOverride = null,
        pageSizeOverride = null,
        filterOverrides = null, // Filtre temizleme/override için (stale closure sorununu önler)
      } = {},
    ) => {
      if (bulkFetchState.isRunning) return;

      const startedAt = Date.now();
      const pageSize = Number(
        pageSizeOverride || bulkFetchPagination.pageSize || 50,
      );
      // filterOverrides varsa onu kullan (Temizle butonu vb.), yoksa state'ten oku
      const activeFilters = filterOverrides ?? bulkFetchFilters;

      setBulkFetchState((prev) => ({
        ...prev,
        isRunning: true,
        error: null,
      }));

      try {
        // Bu bölüm artık cache tablosunu değil, doğrudan SQL tabanlı Mikro
        // stok listesini kullanıyor. syncFirst parametresi geriye dönük uyumluluk
        // için korunuyor ama veri akışını etkilemiyor.
        const response = await MicroService.getStokListesi({
          sayfa: page,
          sayfaBuyuklugu: pageSize,
          depoNo: bulkFetchConfig.depoNo,
          fiyatListesiNo: bulkFetchConfig.fiyatListesiNo || 1,
          stokKod: activeFilters.stokKod || undefined,
          grupKod:
            activeFilters.grupKod || bulkFetchConfig.grupKod || undefined,
          sadeceAktif: true,
          sadeceStoklu:
            activeFilters.stokDurumu === "stokta"
              ? true
              : activeFilters.stokDurumu === "stoksuz"
                ? false
                : undefined,
        });

        if (!response?.success) {
          throw new Error(response?.message || "Ürünler getirilemedi");
        }

        const items = Array.isArray(response.data) ? response.data : [];
        const totalPages = Number(
          response.toplamSayfa || response.totalPages || 1,
        );
        const totalCount = Number(
          response.toplamKayit || response.totalCount || items.length || 0,
        );
        const currentPage = Number(
          response.sayfa || response.page || page || 1,
        );
        const elapsedMs = Date.now() - startedAt;

        // Geçersiz sayfa: filtre değişti ama eski page > yeni totalPages ise sayfa 1'e dön
        if (items.length === 0 && page > 1 && page > totalPages) {
          setBulkFetchState((prev) => ({ ...prev, isRunning: false }));
          setBulkFetchPagination((prev) => ({ ...prev, currentPage: 1 }));
          setTimeout(() => {
            if (loadBulkSqlPageRef.current) loadBulkSqlPageRef.current(1);
          }, 0);
          return;
        }

        setBulkFetchProducts(items);
        setBulkFetchPagination((prev) => ({
          ...prev,
          currentPage,
          pageSize,
        }));

        setBulkFetchState((prev) => ({
          ...prev,
          isRunning: false,
          isComplete: true,
          progress: Math.min(
            100,
            Math.round((currentPage / Math.max(1, totalPages)) * 100),
          ),
          currentPage,
          totalPages,
          fetchedCount: items.length,
          totalCount,
          failedPages: [],
          elapsedMs,
          estimatedRemainingMs: 0,
          error: null,
          stats: {
            totalRequests: 1,
            successfulRequests: 1,
            failedRequests: 0,
            retryCount: 0,
            avgResponseTimeMs: elapsedMs,
          },
        }));

        setMessage(
          `SQL sayfa ${currentPage}/${Math.max(1, totalPages)} yüklendi. (${formatNumber(totalCount)} toplam ürün)`,
        );
        setMessageType("success");
      } catch (error) {
        setBulkFetchState((prev) => ({
          ...prev,
          isRunning: false,
          error: error.message || "İşlem başarısız",
        }));
        setMessage(error.message || "İşlem başarısız");
        setMessageType("danger");
      }
    },
    [
      bulkFetchConfig,
      bulkFetchFilters,
      bulkFetchPagination.pageSize,
      bulkFetchState.isRunning,
    ],
  );

  // loadBulkSqlPage her yeniden oluştuğunda ref'i güncelle (auto-retry için gerekli)
  useEffect(() => {
    loadBulkSqlPageRef.current = loadBulkSqlPage;
  }, [loadBulkSqlPage]);

  // ============================================================================
  // SSE (Server-Sent Events) ile Gerçek Zamanlı Cache Sync
  // ============================================================================
  // Backend'den streaming progress alarak UI'ı gerçek zamanlı günceller.
  // Bu sayede uzun süren işlemlerde tarayıcı donmaz.
  // ============================================================================
  const startStreamingSync = useCallback(async () => {
    if (bulkFetchState.isRunning) return;

    const startedAt = Date.now();

    setBulkFetchState((prev) => ({
      ...prev,
      isRunning: true,
      isComplete: false,
      progress: 0,
      currentPage: 0,
      totalPages: 0,
      fetchedCount: 0,
      totalCount: 0,
      error: null,
      elapsedMs: 0,
      estimatedRemainingMs: 0,
    }));

    setMessage("Mikro ERP'den ürünler çekiliyor...");
    setMessageType("info");

    try {
      // SSE bağlantısı oluştur
      const apiUrl = process.env.REACT_APP_API_URL || "";
      const sseUrl = `${apiUrl}/api/admin/micro/cache/sync-stream?fiyatListesiNo=${bulkFetchConfig.fiyatListesiNo}&depoNo=${bulkFetchConfig.depoNo}&syncMode=full`;

      const eventSource = new EventSource(sseUrl, { withCredentials: true });
      bulkFetchAbortRef.current = { abort: () => eventSource.close() };

      eventSource.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);

          if (data.type === "progress") {
            // İlerleme güncellemesi
            setBulkFetchState((prev) => ({
              ...prev,
              progress: data.progressPercent || 0,
              currentPage: data.currentPage || 0,
              totalPages: data.totalPages || 0,
              fetchedCount: data.fetchedCount || 0,
              totalCount: data.totalCount || 0,
              elapsedMs: (data.elapsedSeconds || 0) * 1000,
              estimatedRemainingMs:
                (data.estimatedRemainingSeconds || 0) * 1000,
            }));
          } else if (data.type === "complete") {
            // İşlem tamamlandı
            eventSource.close();

            const elapsedMs = Date.now() - startedAt;

            setBulkFetchState((prev) => ({
              ...prev,
              isRunning: false,
              isComplete: true,
              progress: 100,
              fetchedCount: data.totalFetched || 0,
              elapsedMs,
              estimatedRemainingMs: 0,
              error: data.success ? null : data.message,
              stats: {
                totalFetched: data.totalFetched,
                newProducts: data.newProducts,
                updatedProducts: data.updatedProducts,
                unchangedProducts: data.unchangedProducts,
                durationSeconds: data.durationSeconds,
              },
            }));

            if (data.success) {
              setMessage(
                `✅ ${formatNumber(data.totalFetched)} ürün işlendi. ` +
                  `Yeni: ${data.newProducts}, Güncellenen: ${data.updatedProducts}, ` +
                  `Değişmeyen: ${data.unchangedProducts}. Süre: ${formatDuration(elapsedMs)}`,
              );
              setMessageType("success");

              // SQL tabanlı ilk sayfayı yükle
              loadBulkSqlPage(1, { syncFirst: false });
            } else {
              setMessage(`❌ Hata: ${data.message}`);
              setMessageType("danger");
            }
          } else if (data.type === "error") {
            eventSource.close();
            setBulkFetchState((prev) => ({
              ...prev,
              isRunning: false,
              error: data.message,
            }));
            setMessage(`❌ Hata: ${data.message}`);
            setMessageType("danger");
          }
        } catch (parseErr) {
          console.error("SSE parse hatası:", parseErr, event.data);
        }
      };

      eventSource.onerror = (err) => {
        console.error("SSE bağlantı hatası:", err);
        eventSource.close();

        setBulkFetchState((prev) => ({
          ...prev,
          isRunning: false,
          error: "Sunucu bağlantısı kesildi",
        }));
        setMessage("Sunucu bağlantısı kesildi. Tekrar deneyin.");
        setMessageType("danger");
      };
    } catch (error) {
      setBulkFetchState((prev) => ({
        ...prev,
        isRunning: false,
        error: error.message || "İşlem başarısız",
      }));
      setMessage(error.message || "İşlem başarısız");
      setMessageType("danger");
    }
  }, [bulkFetchConfig, bulkFetchState.isRunning, loadBulkSqlPage]);

  // Eski startBulkFetch'i güncelle - tüm sayfaları sıralı çeker
  const startBulkFetch = useCallback(async () => {
    if (bulkFetchState.isRunning) return;

    const startedAt = Date.now();
    const pageSize = Number(bulkFetchPagination.pageSize || 50);

    setBulkFetchState((prev) => ({
      ...prev,
      isRunning: true,
      isComplete: false,
      progress: 0,
      currentPage: 0,
      totalPages: 0,
      fetchedCount: 0,
      totalCount: 0,
      error: null,
      elapsedMs: 0,
      estimatedRemainingMs: 0,
    }));

    setMessage("SQL'den tüm sayfalar yükleniyor...");
    setMessageType("info");

    let allProducts = [];
    let page = 1;
    let totalPages = 1;
    let totalCount = 0;

    try {
      // İlk sayfayı çek, toplam sayfa sayısını öğren
      while (page <= totalPages) {
        const response = await MicroService.getStokListesi({
          sayfa: page,
          sayfaBuyuklugu: pageSize,
          depoNo: bulkFetchConfig.depoNo,
          fiyatListesiNo: bulkFetchConfig.fiyatListesiNo || 1,
          stokKod: bulkFetchFilters.stokKod || undefined,
          grupKod:
            bulkFetchFilters.grupKod || bulkFetchConfig.grupKod || undefined,
          sadeceAktif: true,
          sadeceStoklu:
            bulkFetchFilters.stokDurumu === "stokta"
              ? true
              : bulkFetchFilters.stokDurumu === "stoksuz"
                ? false
                : undefined,
        });

        if (!response?.success) {
          throw new Error(response?.message || "Ürünler getirilemedi");
        }

        const items = Array.isArray(response.data) ? response.data : [];

        if (page === 1) {
          totalPages = Number(response.toplamSayfa || response.totalPages || 1);
          totalCount = Number(response.toplamKayit || response.totalCount || 0);
        }

        allProducts = [...allProducts, ...items];
        const elapsedMs = Date.now() - startedAt;
        const progress = Math.min(
          100,
          Math.round((page / Math.max(1, totalPages)) * 100),
        );

        setBulkFetchState((prev) => ({
          ...prev,
          progress,
          currentPage: page,
          totalPages,
          fetchedCount: allProducts.length,
          totalCount,
          elapsedMs,
          estimatedRemainingMs:
            page < totalPages ? (elapsedMs / page) * (totalPages - page) : 0,
        }));

        // Son sayfa veya boş yanıt → dur
        if (items.length === 0 || page >= totalPages) break;
        page++;
      }

      const elapsedMs = Date.now() - startedAt;

      // Son sayfa ürünlerini göster, state'i tamamlandı olarak işaretle
      setBulkFetchProducts(allProducts.slice(-pageSize));
      setBulkFetchPagination((prev) => ({
        ...prev,
        currentPage: totalPages,
        pageSize,
      }));

      setBulkFetchState((prev) => ({
        ...prev,
        isRunning: false,
        isComplete: true,
        progress: 100,
        currentPage: totalPages,
        totalPages,
        fetchedCount: allProducts.length,
        totalCount,
        elapsedMs,
        estimatedRemainingMs: 0,
        error: null,
        stats: {
          totalRequests: totalPages,
          successfulRequests: totalPages,
          failedRequests: 0,
          retryCount: 0,
          avgResponseTimeMs: Math.round(elapsedMs / Math.max(1, totalPages)),
        },
      }));

      setMessage(
        `✅ Tüm sayfalar yüklendi. ${formatNumber(allProducts.length)} ürün çekildi. Süre: ${formatDuration(elapsedMs)}`,
      );
      setMessageType("success");
    } catch (error) {
      setBulkFetchState((prev) => ({
        ...prev,
        isRunning: false,
        error: error.message || "İşlem başarısız",
      }));
      setMessage(error.message || "İşlem başarısız");
      setMessageType("danger");
    }
  }, [
    bulkFetchConfig,
    bulkFetchFilters,
    bulkFetchPagination.pageSize,
    bulkFetchState.isRunning,
  ]);

  // İptal fonksiyonu
  const cancelBulkFetch = useCallback(() => {
    if (bulkFetchAbortRef.current) {
      bulkFetchAbortRef.current.abort();
      setBulkFetchState((prev) => ({
        ...prev,
        isRunning: false,
        error: "İşlem iptal edildi",
      }));
      setMessage("Toplu çekme işlemi iptal edildi.");
      setMessageType("warning");
      console.log("🛑 Toplu çekme işlemi iptal edildi.");
    }
  }, []);

  // Sıfırlama fonksiyonu
  const resetBulkFetch = useCallback(() => {
    cancelBulkFetch();
    setBulkFetchState({
      isRunning: false,
      isComplete: false,
      progress: 0,
      currentPage: 0,
      totalPages: 0,
      fetchedCount: 0,
      totalCount: 0,
      failedPages: [],
      elapsedMs: 0,
      estimatedRemainingMs: 0,
      error: null,
      stats: null,
    });
    setBulkFetchProducts([]);
    setBulkFetchPagination({ currentPage: 1, pageSize: 50 });
  }, [cancelBulkFetch]);

  // CSV Export fonksiyonu - TÜM ürünleri SQL'den sayfa sayfa çeker ve indirir
  const exportBulkFetchToCSV = useCallback(async () => {
    // Ürün yoksa uyar
    if (bulkFetchState.totalCount === 0 && bulkFetchProducts.length === 0) {
      setMessage("Export edilecek ürün yok! Önce ürünleri çekin.");
      setMessageType("warning");
      return;
    }

    setMessage("Tüm ürünler SQL'den çekiliyor, lütfen bekleyin...");
    setMessageType("info");

    try {
      // Tüm sayfaları sıralı çekerek tam ürün listesini oluştur
      const pageSize = 200; // CSV export için daha büyük sayfa
      let allProducts = [];
      let page = 1;
      let totalPages = 1;

      while (page <= totalPages) {
        const response = await MicroService.getStokListesi({
          sayfa: page,
          sayfaBuyuklugu: pageSize,
          depoNo: bulkFetchConfig.depoNo,
          fiyatListesiNo: bulkFetchConfig.fiyatListesiNo || 1,
          stokKod: bulkFetchFilters.stokKod || undefined,
          grupKod:
            bulkFetchFilters.grupKod || bulkFetchConfig.grupKod || undefined,
          sadeceAktif: true,
          sadeceStoklu:
            bulkFetchFilters.stokDurumu === "stokta"
              ? true
              : bulkFetchFilters.stokDurumu === "stoksuz"
                ? false
                : undefined,
        });

        if (!response?.success || !Array.isArray(response.data)) {
          throw new Error(response?.message || "Ürünler alınamadı");
        }

        const items = response.data;
        if (page === 1) {
          totalPages = Number(response.toplamSayfa || 1);
        }

        allProducts = [...allProducts, ...items];
        if (items.length === 0) break;
        page++;
      }

      if (allProducts.length === 0) {
        setMessage("Export edilecek ürün bulunamadı!");
        setMessageType("warning");
        return;
      }

      // CSV başlık satırı
      const headers = [
        "Stok Kodu",
        "Stok Adı",
        "Grup Kodu",
        "Barkod",
        "Birim",
        "Satış Fiyatı",
        "KDV Oranı",
        "Stok Miktarı",
      ];

      // CSV satırları
      const rows = allProducts.map((p) => [
        p.stokKod || "",
        `"${(p.stokAd || "").replace(/"/g, '""')}"`,
        p.grupKod || "",
        p.barkod || "",
        p.birim || "",
        p.satisFiyati ?? p.fiyat ?? 0,
        p.kdvOrani ?? 0,
        p.depoMiktari ?? p.satilabilirMiktar ?? p.stokMiktar ?? 0,
      ]);

      // UTF-8 BOM + içerik
      const BOM = "\uFEFF";
      const csv =
        BOM +
        headers.join(";") +
        "\n" +
        rows.map((r) => r.join(";")).join("\n");

      // Dosya indir
      const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
      const link = document.createElement("a");
      const url = URL.createObjectURL(blob);
      const timestamp = new Date().toISOString().split("T")[0];

      link.setAttribute("href", url);
      link.setAttribute("download", `mikro_urunler_${timestamp}.csv`);
      link.style.visibility = "hidden";
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      setMessage(
        `${formatNumber(allProducts.length)} ürün CSV olarak indirildi.`,
      );
      setMessageType("success");
    } catch (error) {
      console.error("CSV export hatası:", error);
      setMessage("CSV export sırasında hata oluştu: " + error.message);
      setMessageType("danger");
    }
  }, [
    bulkFetchState.totalCount,
    bulkFetchProducts.length,
    bulkFetchFilters,
    bulkFetchConfig,
  ]);

  // CSV Export fonksiyonu (Alias - Yeni tablo için)
  const exportBulkProductsToCSV = exportBulkFetchToCSV;

  // ============================================================================
  // BULK FETCH SAYFALAMA FONKSİYONLARI
  // ============================================================================
  // Sayfalanmış ürünleri hesapla
  // Filtrelenmiş ürün listesini hesapla
  const filteredBulkProducts = useMemo(() => {
    return bulkFetchProducts;
  }, [bulkFetchProducts]);

  // Eski fonksiyonu uyumluluk için tut (varolan kullanımlar için)
  const getFilteredBulkProducts = useCallback(() => {
    return filteredBulkProducts;
  }, [filteredBulkProducts]);

  // Sayfalanmış ürünleri hesapla (useMemo ile performanslı)
  const paginatedBulkProducts = useMemo(() => {
    return filteredBulkProducts;
  }, [filteredBulkProducts]);

  // Eski fonksiyonu uyumluluk için tut
  const getPaginatedBulkProducts = useCallback(() => {
    return paginatedBulkProducts;
  }, [paginatedBulkProducts]);

  // Toplam sayfa sayısını hesapla
  const bulkProductsTotalPages = useMemo(() => {
    return Math.max(1, Number(bulkFetchState.totalPages || 1));
  }, [bulkFetchState.totalPages]);

  // Eski fonksiyonu uyumluluk için tut
  const getBulkProductsTotalPages = useCallback(() => {
    return bulkProductsTotalPages;
  }, [bulkProductsTotalPages]);

  // Sayfa değiştir - filtrelenmiş ürün sayısına göre hesapla
  const changeBulkProductsPage = useCallback(
    (newPage) => {
      const totalPages = Math.max(1, Number(bulkFetchState.totalPages || 1));

      if (newPage >= 1 && newPage <= totalPages) {
        loadBulkSqlPage(newPage);
      }
    },
    [bulkFetchState.totalPages, loadBulkSqlPage],
  );

  // Sayfa boyutunu değiştir
  const changeBulkProductsPageSize = useCallback(
    (newPageSize) => {
      setBulkFetchPagination((prev) => ({
        currentPage: 1, // Sayfa boyutu değişince ilk sayfaya dön
        pageSize: newPageSize,
      }));
      loadBulkSqlPage(1, { pageSizeOverride: newPageSize });
    },
    [loadBulkSqlPage],
  );

  const bulkAutoReloadKey = useMemo(
    () =>
      JSON.stringify({
        stokKod: bulkFetchFilters.stokKod,
        grupKod: bulkFetchFilters.grupKod,
        stokDurumu: bulkFetchFilters.stokDurumu,
        minStok: bulkFetchFilters.minStok,
        configGrupKod: bulkFetchConfig.grupKod,
        pageSize: bulkFetchPagination.pageSize,
      }),
    [
      bulkFetchConfig.grupKod,
      bulkFetchFilters.grupKod,
      bulkFetchFilters.minStok,
      bulkFetchFilters.stokDurumu,
      bulkFetchFilters.stokKod,
      bulkFetchPagination.pageSize,
    ],
  );

  // Otomatik tekrar yukleme kapatildi.
  // NEDEN: surekli state degisimlerinde tekrar API cagrisi yapip UI'da takilmaya neden oluyordu.
  // Veri yukleme artik sadece kullanici aksiyonlariyla (Toplu Cek, sayfa degistir, filtre ara) tetiklenir.

  return (
    <div className="container-fluid p-2 p-md-4">
      {/* Sayfa Başlığı */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-3 mb-md-4 gap-2">
        <div className="mb-2 mb-md-0">
          <h1 className="h4 h3-md fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i className="fas fa-plug me-2" style={{ color: "#f57c00" }} />
            ERP / Mikro Entegrasyon
          </h1>
          <div className="text-muted" style={{ fontSize: "0.8rem" }}>
            ERP ile ürün, stok ve fiyat senkronizasyonu
          </div>
        </div>
      </div>

      {/* Bağlantı Durumu Kartı */}
      <div className="row mb-3">
        <div className="col-12">
          <div
            className={`card border-0 shadow-sm ${
              connectionStatus?.mikroApiOnline
                ? "border-start border-success border-4"
                : connectionStatus?.databaseOnline
                  ? "border-start border-warning border-4"
                  : "border-start border-danger border-4"
            }`}
            style={{ borderLeftWidth: "4px" }}
          >
            <div className="card-body py-3 d-flex align-items-center justify-content-between flex-wrap gap-2">
              <div className="d-flex align-items-center">
                <div
                  className={`rounded-circle d-flex align-items-center justify-content-center me-3`}
                  style={{
                    width: 48,
                    height: 48,
                    background: connectionStatus?.mikroApiOnline
                      ? "linear-gradient(135deg, #10b981, #059669)"
                      : connectionStatus?.databaseOnline
                        ? "linear-gradient(135deg, #f59e0b, #d97706)"
                        : "linear-gradient(135deg, #ef4444, #dc2626)",
                  }}
                >
                  <i
                    className={`fas ${
                      connectionStatus?.mikroApiOnline
                        ? "fa-check"
                        : connectionStatus?.databaseOnline
                          ? "fa-database"
                          : "fa-times"
                    } text-white`}
                  ></i>
                </div>
                <div>
                  <h6 className="mb-0 fw-bold">
                    {connectionStatus?.mikroApiOnline
                      ? "Mikro API Bağlantısı"
                      : connectionStatus?.databaseOnline
                        ? "Offline Mod (Veritabanı)"
                        : "Bağlantı Durumu"}
                  </h6>
                  <small
                    className={
                      connectionStatus?.mikroApiOnline
                        ? "text-success"
                        : connectionStatus?.databaseOnline
                          ? "text-warning"
                          : "text-danger"
                    }
                  >
                    {connectionStatus?.mikroApiOnline
                      ? `Bağlı - ${connectionStatus?.toplamUrunSayisi || 0} ürün`
                      : connectionStatus?.databaseOnline
                        ? `${connectionStatus?.toplamUrunSayisi || 0} ürün veritabanında`
                        : "Bağlantı Yok"}
                  </small>
                </div>
              </div>
              <button
                className="btn btn-outline-primary btn-sm"
                onClick={testConnection}
                disabled={connectionLoading}
              >
                {connectionLoading ? (
                  <>
                    <i className="fas fa-spinner fa-spin me-1"></i> Test
                    Ediliyor...
                  </>
                ) : (
                  <>
                    <i className="fas fa-sync-alt me-1"></i> Bağlantı Testi
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Tab Navigasyon */}
      <ul className="nav nav-tabs mb-3">
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "overview" ? "active" : ""}`}
            onClick={() => setActiveTab("overview")}
          >
            <i className="fas fa-home me-1"></i> Genel Bakış
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "stokListesi" ? "active" : ""}`}
            onClick={() => {
              setActiveTab("stokListesi");
              loadStokListesi();
            }}
          >
            <i className="fas fa-list me-1"></i> Mikro Stok Listesi
          </button>
        </li>
        {/* Yeni: Toplu Ürün Çek Tab'ı */}
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "bulkFetch" ? "active" : ""}`}
            onClick={() => {
              setActiveTab("bulkFetch");
              if (bulkFetchState.totalCount > 0) {
                loadBulkSqlPage(1);
              }
            }}
          >
            <i className="fas fa-cloud-download-alt me-1"></i> Toplu Ürün Çek
            {bulkFetchState.isRunning && (
              <span className="badge bg-primary ms-2">
                %{bulkFetchState.progress}
              </span>
            )}
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "settings" ? "active" : ""}`}
            onClick={() => {
              setActiveTab("settings");
              loadActiveProducts(1);
              loadSyncDiagnostics(24);
            }}
          >
            <i className="fas fa-cog me-1"></i> Ayarlar
          </button>
        </li>
        {/* Sync Sağlık & Monitoring — Phase 5 */}
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "syncHealth" ? "active" : ""}`}
            onClick={() => setActiveTab("syncHealth")}
          >
            <i className="fas fa-heartbeat me-1"></i> Sync Sağlık
          </button>
        </li>
        {/* Anlık Stok Akışı — SignalR StockHub canlı feed */}
        <li className="nav-item">
          <button
            className={`nav-link ${activeTab === "realtime" ? "active" : ""}`}
            onClick={() => setActiveTab("realtime")}
          >
            <i className="fas fa-broadcast-tower me-1"></i> Anlık Stok
            {stockHubConnected && (
              <span
                className="badge bg-success ms-2"
                style={{ fontSize: "0.6rem" }}
              >
                CANLI
              </span>
            )}
            {totalStockUpdates > 0 && (
              <span className="badge bg-info ms-1">{totalStockUpdates}</span>
            )}
          </button>
        </li>
      </ul>

      {/* Operasyon Bilgileri */}
      {loading && (
        <div
          className="alert alert-warning d-flex align-items-center py-2"
          role="alert"
        >
          <i className="fas fa-spinner fa-spin me-2"></i>
          İşlem yapılıyor, lütfen bekleyin...
        </div>
      )}
      {message && (
        <div
          className={`alert alert-${messageType} py-2 alert-dismissible`}
          role="alert"
        >
          {message}
          <button
            type="button"
            className="btn-close"
            onClick={() => setMessage("")}
          ></button>
        </div>
      )}

      {/* Tab İçerikleri */}
      {activeTab === "overview" && (
        <>
          {/* Aksiyon Butonları */}
          <div className="row mb-4">
            <div className="col-12">
              <div className="card border-0 shadow-sm">
                <div className="card-header bg-white py-3">
                  <h6 className="mb-0 fw-bold">
                    <i className="fas fa-sync me-2 text-primary"></i>
                    Senkronizasyon İşlemleri
                  </h6>
                </div>
                <div className="card-body">
                  <div className="row g-2">
                    <div className="col-6 col-md-3">
                      <button
                        className="btn text-white fw-semibold w-100"
                        style={{
                          background:
                            "linear-gradient(135deg, #2b6cb0, #3182ce)",
                        }}
                        onClick={syncProducts}
                        disabled={loading}
                      >
                        <i className="fas fa-sync-alt me-2"></i>
                        Ürünleri Senkronize
                      </button>
                    </div>
                    <div className="col-6 col-md-3">
                      <button
                        className="btn text-white fw-semibold w-100"
                        style={{
                          background:
                            "linear-gradient(135deg, #553c9a, #6b46c1)",
                        }}
                        onClick={syncStocksFromERP}
                        disabled={loading}
                      >
                        <i className="fas fa-boxes me-2"></i>
                        Stokları ERP'den Çek
                      </button>
                    </div>
                    <div className="col-6 col-md-3">
                      <button
                        className="btn text-white fw-semibold w-100"
                        style={{
                          background:
                            "linear-gradient(135deg, #b83280, #d53f8c)",
                        }}
                        onClick={syncPricesFromERP}
                        disabled={loading}
                      >
                        <i className="fas fa-tags me-2"></i>
                        Fiyatları ERP'den Çek
                      </button>
                    </div>
                    <div className="col-6 col-md-3">
                      <button
                        className="btn text-white fw-semibold w-100"
                        style={{
                          background:
                            "linear-gradient(135deg, #6b46c1, #805ad5)",
                        }}
                        onClick={exportOrders}
                        disabled={loading}
                      >
                        <i className="fas fa-paper-plane me-2"></i>
                        Siparişleri Gönder
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Ürünler ve Stoklar Tabloları */}
          <div className="row g-3">
            <div className="col-12 col-xl-6">
              <div className="card border-0 shadow-sm h-100">
                <div className="card-header bg-white d-flex justify-content-between align-items-center py-2 px-3">
                  <div style={{ fontSize: "0.9rem" }}>
                    <i className="fas fa-box me-2 text-primary"></i>
                    Yerel Ürünler ({products?.length || 0})
                  </div>
                  <button
                    className="btn btn-sm btn-outline-secondary"
                    onClick={fetchProducts}
                    disabled={loading}
                  >
                    <i className="fas fa-refresh me-1"></i> Yenile
                  </button>
                </div>
                <div className="card-body p-2 p-md-3">
                  <div
                    className="table-responsive"
                    style={{ maxHeight: "400px", overflowY: "auto" }}
                  >
                    <table
                      className="table table-sm align-middle"
                      style={{ fontSize: "0.8rem" }}
                    >
                      <thead className="table-light sticky-top">
                        <tr>
                          <th style={{ width: 60 }}>ID</th>
                          <th>Ürün Adı</th>
                          <th style={{ width: 100 }}>Fiyat</th>
                          <th className="d-none d-md-table-cell">Kategori</th>
                        </tr>
                      </thead>
                      <tbody>
                        {products?.length ? (
                          products.map((p) => (
                            <tr key={p.sku || p.stokKod || p.id}>
                              <td>{p.sku || p.stokKod || p.id || "-"}</td>
                              <td>{p.name || p.stokAd || "-"}</td>
                              <td>
                                ₺
                                {typeof p.price === "number"
                                  ? p.price.toLocaleString("tr-TR", {
                                      minimumFractionDigits: 2,
                                    })
                                  : p.price}
                              </td>
                              <td className="d-none d-md-table-cell">
                                {p.category ||
                                  p.categoryName ||
                                  p.grupKod ||
                                  "-"}
                              </td>
                            </tr>
                          ))
                        ) : (
                          <tr>
                            <td
                              colSpan="4"
                              className="text-muted text-center py-3"
                            >
                              Kayıt bulunamadı.
                            </td>
                          </tr>
                        )}
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            </div>

            {/* Stoklar */}
            <div className="col-12 col-xl-6">
              <div className="card border-0 shadow-sm h-100">
                <div className="card-header bg-white d-flex justify-content-between align-items-center py-2 px-3">
                  <div style={{ fontSize: "0.9rem" }}>
                    <i className="fas fa-warehouse me-2 text-warning"></i>
                    Yerel Stoklar ({stocks?.length || 0})
                  </div>
                  <button
                    className="btn btn-sm btn-outline-secondary"
                    onClick={fetchStocks}
                    disabled={loading}
                  >
                    <i className="fas fa-refresh me-1"></i> Yenile
                  </button>
                </div>
                <div className="card-body p-2 p-md-3">
                  <div
                    className="table-responsive"
                    style={{ maxHeight: "400px", overflowY: "auto" }}
                  >
                    <table
                      className="table table-sm align-middle"
                      style={{ fontSize: "0.8rem" }}
                    >
                      <thead className="table-light sticky-top">
                        <tr>
                          <th style={{ width: 100 }}>Ürün ID</th>
                          <th>Stok Miktarı</th>
                        </tr>
                      </thead>
                      <tbody>
                        {stocks?.length ? (
                          stocks.map((s) => (
                            <tr key={s.sku || s.stokKod || s.productId}>
                              <td>
                                {s.sku || s.stokKod || s.productId || "-"}
                              </td>
                              <td
                                className={
                                  Number(s.quantity) <= 0
                                    ? "text-danger fw-bold"
                                    : ""
                                }
                              >
                                {s.quantity}
                              </td>
                            </tr>
                          ))
                        ) : (
                          <tr>
                            <td
                              colSpan="2"
                              className="text-muted text-center py-3"
                            >
                              Kayıt bulunamadı.
                            </td>
                          </tr>
                        )}
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </>
      )}

      {/* Mikro Stok Listesi Tab'ı */}
      {activeTab === "stokListesi" && (
        <div className="card border-0 shadow-sm">
          <div className="card-header bg-white py-3">
            <div className="d-flex justify-content-between align-items-center mb-3">
              <div>
                <h6 className="mb-0 fw-bold">
                  <i className="fas fa-database me-2 text-success"></i>
                  Mikro API Stok Listesi
                </h6>
                <small className="text-muted">
                  Toplam: {stokListesiMeta.totalCount} kayıt | Fiyat: Otomatik |
                  Depo: {depoNo === 0 ? "Tümü" : depoNo}
                </small>
              </div>
              <button
                className="btn btn-primary btn-sm"
                onClick={() =>
                  loadStokListesi(1, 100, grupKodFilter, aramaMetni)
                }
                disabled={loading}
              >
                <i className="fas fa-download me-1"></i> Mikro'dan Çek (100)
              </button>
            </div>

            {/* Filtre Satırı */}
            <div className="row g-2 align-items-end">
              <div className="col-md-3">
                <label className="form-label small mb-1">
                  <i className="fas fa-tags me-1 text-success"></i>
                  Fiyat
                </label>
                <div className="form-control form-control-sm bg-light text-muted">
                  Otomatik - tüm listeler
                </div>
              </div>
              <div className="col-md-2">
                <label className="form-label small mb-1">
                  <i className="fas fa-warehouse me-1 text-info"></i>
                  Depo
                </label>
                <select
                  className="form-select form-select-sm"
                  value={depoNo}
                  onChange={(e) => setDepoNo(parseInt(e.target.value, 10))}
                  disabled={loading}
                >
                  {depoListesi.map((depo) => (
                    <option key={depo.depoNo} value={depo.depoNo}>
                      {depo.depoAdi}
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-md-3">
                <label className="form-label small mb-1">Arama (min 3)</label>
                <input
                  type="text"
                  className="form-control form-control-sm"
                  placeholder="Stok kodu / ad / barkod"
                  value={aramaMetni}
                  onChange={(e) => setAramaMetni(e.target.value)}
                  disabled={loading}
                />
              </div>
              <div className="col-md-2">
                <label className="form-label small mb-1">Stok Durumu</label>
                <select
                  className="form-select form-select-sm"
                  value={stokDurumuFilter}
                  onChange={(e) => setStokDurumuFilter(e.target.value)}
                  disabled={loading}
                >
                  <option value="hepsi">Hepsi</option>
                  <option value="stokta">Sadece Stokta</option>
                  <option value="stoksuz">Sadece Stoksuz</option>
                </select>
              </div>
              <div className="col-md-2">
                <label className="form-label small mb-1">Grup Kodu</label>
                <select
                  className="form-select form-select-sm"
                  value={grupKodFilter}
                  onChange={(e) => setGrupKodFilter(e.target.value)}
                  disabled={loading}
                >
                  <option value="">Tum Gruplar</option>
                  {grupKodlari.map((g) => (
                    <option key={g} value={g}>
                      {g}
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-md-2">
                <button
                  className="btn btn-outline-secondary btn-sm w-100"
                  onClick={() =>
                    loadStokListesi(1, 100, grupKodFilter, aramaMetni)
                  }
                  disabled={loading}
                >
                  <i className="fas fa-search me-1"></i> Ara / Yenile
                </button>
              </div>
            </div>
          </div>
          <div className="card-body p-0">
            <div className="table-responsive">
              <table
                className="table table-hover mb-0"
                style={{ fontSize: "0.85rem" }}
              >
                <thead className="table-light">
                  <tr>
                    <th>Stok Kodu</th>
                    <th>Stok Adı</th>
                    <th>Barkod</th>
                    <th className="text-end">Fiyat</th>
                    <th className="text-end">Miktar</th>
                    <th>Birim</th>
                    <th>Grup</th>
                  </tr>
                </thead>
                <tbody>
                  {stokListesi?.length ? (
                    stokListesi.map((s, idx) => (
                      <tr key={s.stokKod || idx}>
                        <td>
                          <code>{s.stokKod}</code>
                        </td>
                        <td>{s.stokAd}</td>
                        <td>
                          <small className="text-muted">
                            {s.barkod || "-"}
                          </small>
                        </td>
                        <td className="text-end">
                          ₺
                          {typeof (s.satisFiyati ?? s.fiyat) === "number"
                            ? (s.satisFiyati ?? s.fiyat).toLocaleString(
                                "tr-TR",
                                {
                                  minimumFractionDigits: 2,
                                },
                              )
                            : (s.satisFiyati ?? s.fiyat) || "0.00"}
                        </td>
                        <td
                          className={`text-end ${Number(s.depoMiktari ?? s.satilabilirMiktar ?? s.stokMiktar) <= 0 ? "text-danger fw-bold" : ""}`}
                        >
                          {s.depoMiktari ??
                            s.satilabilirMiktar ??
                            s.stokMiktar ??
                            0}
                        </td>
                        <td>{s.birim || "-"}</td>
                        <td>
                          <span className="badge bg-secondary">
                            {s.grupKod || "-"}
                          </span>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="7" className="text-center py-4 text-muted">
                        <i className="fas fa-info-circle me-2"></i>
                        Mikro API'den veri çekmek için yukarıdaki butona
                        tıklayın.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
          {/* Sayfalama */}
          {stokListesiMeta.totalCount > stokListesiMeta.pageSize && (
            <div className="card-footer bg-white d-flex justify-content-between align-items-center">
              <small className="text-muted">
                Sayfa {stokListesiMeta.page} /{" "}
                {Math.ceil(
                  stokListesiMeta.totalCount / stokListesiMeta.pageSize,
                )}
              </small>
              <div>
                <button
                  className="btn btn-sm btn-outline-secondary me-2"
                  disabled={stokListesiMeta.page <= 1 || loading}
                  onClick={() => handlePageChange(stokListesiMeta.page - 1)}
                >
                  <i className="fas fa-chevron-left"></i> Önceki
                </button>
                <button
                  className="btn btn-sm btn-outline-secondary"
                  disabled={
                    stokListesiMeta.page >=
                      Math.ceil(
                        stokListesiMeta.totalCount / stokListesiMeta.pageSize,
                      ) || loading
                  }
                  onClick={() => handlePageChange(stokListesiMeta.page + 1)}
                >
                  Sonraki <i className="fas fa-chevron-right"></i>
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ============================================================================
          TOPLU ÜRÜN ÇEK TAB'I - Sequential Bulk Fetcher
          Gemini talimatlarına göre: Sıralı istek, retry, throttle, progress bar
      ============================================================================ */}
      {activeTab === "bulkFetch" && (
        <>
          {/* İlk Satır: Kontrol Paneli ve İlerleme */}
          <div className="row mb-4">
            {/* Sol Kolon: Kontrol Paneli */}
            <div className="col-12 col-lg-5 mb-4">
              {/* Konfigürasyon Kartı */}
              <div className="card border-0 shadow-sm mb-3">
                <div className="card-header bg-white py-3">
                  <h6 className="mb-0 fw-bold">
                    <i className="fas fa-cogs me-2 text-primary"></i>
                    Çekme Ayarları
                  </h6>
                </div>
                <div className="card-body">
                  <div className="row g-3">
                    <div className="col-6">
                      <label className="form-label small fw-semibold">
                        Sayfa Boyutu
                      </label>
                      <select
                        className="form-select form-select-sm"
                        value={bulkFetchConfig.pageSize}
                        onChange={(e) =>
                          setBulkFetchConfig((prev) => ({
                            ...prev,
                            pageSize: Number(e.target.value),
                          }))
                        }
                        disabled={bulkFetchState.isRunning}
                      >
                        <option value={50}>50 ürün/sayfa</option>
                        <option value={100}>100 ürün/sayfa</option>
                        <option value={250}>250 ürün/sayfa</option>
                        <option value={500}>500 ürün/sayfa</option>
                      </select>
                      <small className="text-muted">
                        Her sayfa geçişinde SQL'den okunacak ürün sayısı
                      </small>
                    </div>
                    <div className="col-6">
                      <label className="form-label small fw-semibold">
                        Senkron Modu
                      </label>
                      <select
                        className="form-select form-select-sm"
                        value={bulkFetchConfig.syncMode}
                        onChange={(e) =>
                          setBulkFetchConfig((prev) => ({
                            ...prev,
                            syncMode: e.target.value,
                          }))
                        }
                        disabled={bulkFetchState.isRunning}
                      >
                        <option value="newOnly">
                          Sadece yeni ürün varsa güncelle
                        </option>
                        <option value="full">
                          Tam senkron (tüm ürünleri kontrol et)
                        </option>
                      </select>
                      <small className="text-muted">
                        Varsayılan: yeni ürün yoksa ERP'ye toplu çekim yapma
                      </small>
                    </div>

                    {/* Fiyat Listesi Seçimi kaldırıldı */}
                    <div className="col-md-6">
                      <label className="form-label small fw-semibold">
                        <i className="fas fa-tags me-1 text-success"></i>
                        Fiyat
                      </label>
                      <div className="form-control form-control-sm bg-light text-muted">
                        Otomatik - tüm fiyatlar
                      </div>
                    </div>

                    {/* Depo Seçimi */}
                    <div className="col-md-6">
                      <label className="form-label small fw-semibold">
                        <i className="fas fa-warehouse me-1 text-info"></i>
                        Depo No
                      </label>
                      <select
                        className="form-select form-select-sm"
                        value={bulkFetchConfig.depoNo}
                        onChange={(e) =>
                          setBulkFetchConfig((prev) => ({
                            ...prev,
                            depoNo: parseInt(e.target.value, 10),
                          }))
                        }
                        disabled={bulkFetchState.isRunning}
                      >
                        <option value={0}>Tüm Depolar</option>
                        <option value={1}>Depo 1</option>
                        <option value={2}>Depo 2</option>
                        <option value={3}>Depo 3</option>
                        <option value={4}>Depo 4</option>
                        <option value={5}>Depo 5</option>
                      </select>
                      <small className="text-muted">
                        Hangi deponun stoğu gösterilecek
                      </small>
                    </div>

                    <div className="col-12">
                      <label className="form-label small fw-semibold">
                        Grup Kodu Filtresi (Opsiyonel)
                      </label>
                      <input
                        type="text"
                        className="form-control form-control-sm"
                        placeholder="Örn: GIDA, ICECEK"
                        value={bulkFetchConfig.grupKod}
                        onChange={(e) =>
                          setBulkFetchConfig((prev) => ({
                            ...prev,
                            grupKod: e.target.value,
                          }))
                        }
                        disabled={bulkFetchState.isRunning}
                      />
                      <small className="text-muted">
                        Boş bırakılırsa tüm ürünler çekilir
                      </small>
                    </div>
                  </div>
                </div>
              </div>

              {/* Bilgi Kartı */}
              <div
                className="alert alert-info py-2 mb-3"
                style={{ fontSize: "0.85rem" }}
              >
                <i className="fas fa-info-circle me-2"></i>
                <strong>Nasıl Çalışır?</strong>
                <ul className="mb-0 mt-1 ps-3">
                  <li>İlk adımda ürünler cache'e senkronize edilir</li>
                  <li>
                    Sonrasında sadece açtığın sayfa SQL kaynağından yüklenir
                  </li>
                  <li>Yeni ürün yoksa tekrar toplu ERP çekimi yapılmaz</li>
                  <li>6000+ ürün listesi daha hızlı ve stabil açılır</li>
                </ul>
              </div>

              {/* Aksiyon Butonları */}
              <div className="d-flex gap-2 flex-wrap">
                {!bulkFetchState.isRunning && !bulkFetchState.isComplete && (
                  <>
                    {/* SQL'den yükle (hızlı) */}
                    <button
                      className="btn btn-outline-primary fw-semibold"
                      onClick={startBulkFetch}
                    >
                      <i className="fas fa-database me-2"></i>
                      SQL'den Yükle
                    </button>

                    {/* ERP'den çek (streaming ile) */}
                    <button
                      className="btn text-white fw-semibold"
                      style={{
                        background: "linear-gradient(135deg, #10b981, #059669)",
                      }}
                      onClick={startStreamingSync}
                    >
                      <i className="fas fa-cloud-download-alt me-2"></i>
                      ERP'den Tüm Ürünleri Çek
                    </button>
                  </>
                )}

                {bulkFetchState.isRunning && (
                  <button
                    className="btn btn-danger fw-semibold"
                    onClick={cancelBulkFetch}
                  >
                    <i className="fas fa-stop-circle me-2"></i>
                    İptal Et
                  </button>
                )}

                {(bulkFetchProducts.length > 0 ||
                  bulkFetchState.totalCount > 0) && (
                  <button
                    className="btn btn-success fw-semibold"
                    onClick={exportBulkFetchToCSV}
                  >
                    <i className="fas fa-file-csv me-2"></i>
                    CSV İndir (
                    {formatNumber(
                      bulkFetchState.totalCount || bulkFetchProducts.length,
                    )}{" "}
                    ürün)
                  </button>
                )}

                {(bulkFetchState.isComplete || bulkFetchState.error) && (
                  <button
                    className="btn btn-outline-secondary"
                    onClick={resetBulkFetch}
                  >
                    <i className="fas fa-redo me-2"></i>
                    Sıfırla
                  </button>
                )}
              </div>
            </div>

            {/* Sağ Kolon: İlerleme Durumu */}
            <div className="col-12 col-lg-7">
              {/* İlerleme Kartı */}
              <div className="card border-0 shadow-sm">
                <div className="card-header bg-white py-3">
                  <h6 className="mb-0 fw-bold">
                    <i className="fas fa-tasks me-2 text-warning"></i>
                    İlerleme Durumu
                  </h6>
                </div>
                <div className="card-body">
                  {/* Progress Bar */}
                  <div className="mb-3">
                    <div className="d-flex justify-content-between mb-1">
                      <span className="small fw-semibold">
                        {bulkFetchState.isRunning
                          ? "Çekiliyor..."
                          : bulkFetchState.isComplete
                            ? "Tamamlandı!"
                            : bulkFetchState.error
                              ? "Hata!"
                              : "Bekliyor"}
                      </span>
                      <span className="small fw-bold">
                        %{bulkFetchState.progress}
                      </span>
                    </div>
                    <div className="progress" style={{ height: "12px" }}>
                      <div
                        className={`progress-bar ${
                          bulkFetchState.error
                            ? "bg-danger"
                            : bulkFetchState.isComplete
                              ? "bg-success"
                              : "bg-primary progress-bar-striped progress-bar-animated"
                        }`}
                        style={{ width: `${bulkFetchState.progress}%` }}
                      ></div>
                    </div>
                  </div>

                  {/* İstatistikler Grid */}
                  <div className="row g-2 text-center">
                    <div className="col-4">
                      <div className="bg-light rounded p-2">
                        <div className="h5 mb-0 text-primary">
                          {formatNumber(bulkFetchState.fetchedCount)}
                        </div>
                        <small className="text-muted">Çekilen</small>
                      </div>
                    </div>
                    <div className="col-4">
                      <div className="bg-light rounded p-2">
                        <div className="h5 mb-0 text-secondary">
                          {formatNumber(bulkFetchState.totalCount)}
                        </div>
                        <small className="text-muted">Toplam</small>
                      </div>
                    </div>
                    <div className="col-4">
                      <div className="bg-light rounded p-2">
                        <div className="h5 mb-0 text-info">
                          {bulkFetchState.currentPage}/
                          {bulkFetchState.totalPages || "?"}
                        </div>
                        <small className="text-muted">Sayfa</small>
                      </div>
                    </div>
                    <div className="col-6">
                      <div className="bg-light rounded p-2">
                        <div className="h6 mb-0">
                          {formatDuration(bulkFetchState.elapsedMs)}
                        </div>
                        <small className="text-muted">Geçen Süre</small>
                      </div>
                    </div>
                    <div className="col-6">
                      <div className="bg-light rounded p-2">
                        <div className="h6 mb-0">
                          {formatDuration(bulkFetchState.estimatedRemainingMs)}
                        </div>
                        <small className="text-muted">Kalan Süre</small>
                      </div>
                    </div>
                  </div>

                  {/* Hata Gösterimi */}
                  {bulkFetchState.error && (
                    <div className="alert alert-danger mt-3 mb-0 py-2">
                      <i className="fas fa-exclamation-triangle me-2"></i>
                      {bulkFetchState.error}
                    </div>
                  )}

                  {/* Başarısız Sayfalar */}
                  {bulkFetchState.failedPages.length > 0 && (
                    <div className="alert alert-warning mt-3 mb-0 py-2">
                      <i className="fas fa-exclamation-circle me-2"></i>
                      Başarısız sayfalar:{" "}
                      {bulkFetchState.failedPages.join(", ")}
                    </div>
                  )}

                  {/* İstatistikler (Tamamlandığında) */}
                  {bulkFetchState.stats && (
                    <div className="mt-3 pt-3 border-top">
                      <h6 className="small fw-bold mb-2">
                        <i className="fas fa-chart-bar me-1"></i> Detaylı
                        İstatistikler
                      </h6>
                      <div className="row g-2 small">
                        <div className="col-6">
                          <span className="text-muted">Toplam İstek:</span>{" "}
                          <strong>{bulkFetchState.stats.totalRequests}</strong>
                        </div>
                        <div className="col-6">
                          <span className="text-muted">Başarılı:</span>{" "}
                          <strong className="text-success">
                            {bulkFetchState.stats.successfulRequests}
                          </strong>
                        </div>
                        <div className="col-6">
                          <span className="text-muted">Başarısız:</span>{" "}
                          <strong className="text-danger">
                            {bulkFetchState.stats.failedRequests}
                          </strong>
                        </div>
                        <div className="col-6">
                          <span className="text-muted">Retry Sayısı:</span>{" "}
                          <strong>{bulkFetchState.stats.retryCount}</strong>
                        </div>
                        <div className="col-12">
                          <span className="text-muted">Ort. Yanıt Süresi:</span>{" "}
                          <strong>
                            {bulkFetchState.stats.avgResponseTimeMs}ms
                          </strong>
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Bilgilendirme Metni */}
              {!bulkFetchState.isRunning && !bulkFetchState.isComplete && (
                <div className="text-center text-muted py-4">
                  <i className="fas fa-info-circle me-2"></i>
                  Toplu çekme işlemini başlatmak için yukarıdaki butona
                  tıklayın.
                </div>
              )}
            </div>
          </div>

          {/* İkinci Satır: Ürün Tablosu (Tam Genişlik) */}
          {bulkFetchProducts.length > 0 && (
            <div className="row">
              <div className="col-12">
                <div className="card border-0 shadow-sm">
                  <div className="card-header bg-white py-3">
                    <div className="d-flex justify-content-between align-items-center mb-3">
                      <h6 className="mb-0 fw-bold">
                        <i className="fas fa-table me-2 text-success"></i>
                        Ürünler (Bu sayfa: {bulkFetchProducts.length} | Toplam:{" "}
                        {bulkFetchState.totalCount || 0})
                      </h6>

                      {/* Sayfa boyutu seçici */}
                      <div className="d-flex align-items-center gap-2">
                        <span className="small text-muted">Sayfa boyutu:</span>
                        <select
                          className="form-select form-select-sm"
                          style={{ width: "80px" }}
                          value={bulkFetchPagination.pageSize}
                          onChange={(e) =>
                            changeBulkProductsPageSize(Number(e.target.value))
                          }
                        >
                          <option value={10}>10</option>
                          <option value={25}>25</option>
                          <option value={50}>50</option>
                          <option value={100}>100</option>
                        </select>

                        {/* CSV Export Butonu */}
                        <button
                          className="btn btn-sm btn-success"
                          onClick={exportBulkProductsToCSV}
                          disabled={bulkFetchProducts.length === 0}
                          title="Excel'e Aktar"
                        >
                          <i className="fas fa-file-excel me-1"></i>
                          Excel
                        </button>
                      </div>
                    </div>

                    {/* Arama Filtreleri */}
                    <div className="row g-2">
                      <div className="col-md-3">
                        <div className="input-group input-group-sm">
                          <span className="input-group-text">
                            <i className="fas fa-search"></i>
                          </span>
                          <input
                            type="text"
                            className="form-control"
                            placeholder="Stok Kodu ara... (Enter ile ara)"
                            value={bulkFetchFilters.stokKod}
                            onChange={(e) => {
                              setBulkFetchFilters((prev) => ({
                                ...prev,
                                stokKod: e.target.value,
                              }));
                              setBulkFetchPagination((prev) => ({
                                ...prev,
                                currentPage: 1,
                              }));
                            }}
                            onKeyDown={(e) => {
                              if (e.key === "Enter") loadBulkSqlPage(1);
                            }}
                          />
                          {bulkFetchFilters.stokKod && (
                            <button
                              className="btn btn-outline-secondary"
                              title="Temizle ve yeniden ara"
                              onClick={() => {
                                const cleared = {
                                  ...bulkFetchFilters,
                                  stokKod: "",
                                };
                                setBulkFetchFilters(cleared);
                                setBulkFetchPagination((prev) => ({
                                  ...prev,
                                  currentPage: 1,
                                }));
                                loadBulkSqlPage(1, {
                                  filterOverrides: cleared,
                                });
                              }}
                            >
                              <i className="fas fa-times"></i>
                            </button>
                          )}
                        </div>
                      </div>
                      <div className="col-md-3">
                        <div className="input-group input-group-sm">
                          <span className="input-group-text">
                            <i className="fas fa-folder"></i>
                          </span>
                          <input
                            type="text"
                            className="form-control"
                            placeholder="Grup Kodu ara... (Enter ile ara)"
                            value={bulkFetchFilters.grupKod}
                            onChange={(e) => {
                              setBulkFetchFilters((prev) => ({
                                ...prev,
                                grupKod: e.target.value,
                              }));
                              setBulkFetchPagination((prev) => ({
                                ...prev,
                                currentPage: 1,
                              }));
                            }}
                            onKeyDown={(e) => {
                              if (e.key === "Enter") loadBulkSqlPage(1);
                            }}
                          />
                          {bulkFetchFilters.grupKod && (
                            <button
                              className="btn btn-outline-secondary"
                              title="Temizle ve yeniden ara"
                              onClick={() => {
                                const cleared = {
                                  ...bulkFetchFilters,
                                  grupKod: "",
                                };
                                setBulkFetchFilters(cleared);
                                setBulkFetchPagination((prev) => ({
                                  ...prev,
                                  currentPage: 1,
                                }));
                                loadBulkSqlPage(1, {
                                  filterOverrides: cleared,
                                });
                              }}
                            >
                              <i className="fas fa-times"></i>
                            </button>
                          )}
                        </div>
                      </div>

                      {/* Stok Durumu Filtresi */}
                      <div className="col-md-3">
                        <div className="input-group input-group-sm">
                          <span className="input-group-text">
                            <i className="fas fa-boxes"></i>
                          </span>
                          <select
                            className="form-select form-select-sm"
                            value={bulkFetchFilters.stokDurumu}
                            onChange={(e) => {
                              const val = e.target.value;
                              const updated = {
                                ...bulkFetchFilters,
                                stokDurumu: val,
                              };
                              setBulkFetchFilters(updated);
                              setBulkFetchPagination((prev) => ({
                                ...prev,
                                currentPage: 1,
                              }));
                              // Dropdown değişince hemen sayfa 1'den ara
                              loadBulkSqlPage(1, {
                                filterOverrides: updated,
                              });
                            }}
                          >
                            <option value="hepsi">Tüm Ürünler</option>
                            <option value="stokta">Stokta Olanlar</option>
                            <option value="stoksuz">Stoksuz Ürünler</option>
                          </select>
                        </div>
                      </div>

                      {/* Minimum Stok Filtresi */}
                      <div className="col-md-2">
                        <div className="input-group input-group-sm">
                          <span className="input-group-text" title="Min. Stok">
                            ≥
                          </span>
                          <input
                            type="number"
                            className="form-control"
                            placeholder="Min stok"
                            min="0"
                            value={bulkFetchFilters.minStok}
                            onChange={(e) => {
                              setBulkFetchFilters((prev) => ({
                                ...prev,
                                minStok: e.target.value,
                              }));
                              setBulkFetchPagination((prev) => ({
                                ...prev,
                                currentPage: 1,
                              }));
                            }}
                            onKeyDown={(e) => {
                              if (e.key === "Enter") loadBulkSqlPage(1);
                            }}
                          />
                        </div>
                      </div>

                      <div className="col-md-3 d-flex gap-2">
                        <button
                          className="btn btn-sm btn-primary"
                          onClick={() => loadBulkSqlPage(1)}
                          disabled={bulkFetchState.isRunning}
                          title="Filtreyle sayfa 1'den ara"
                        >
                          <i className="fas fa-search me-1"></i>
                          Ara
                        </button>
                        <button
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => {
                            const cleared = {
                              stokKod: "",
                              grupKod: "",
                              stokDurumu: "hepsi",
                              minStok: "",
                            };
                            setBulkFetchFilters(cleared);
                            setBulkFetchPagination((prev) => ({
                              ...prev,
                              currentPage: 1,
                            }));
                            // filterOverrides ile stale closure sorununu önle
                            loadBulkSqlPage(1, { filterOverrides: cleared });
                          }}
                          disabled={
                            !bulkFetchFilters.stokKod &&
                            !bulkFetchFilters.grupKod &&
                            bulkFetchFilters.stokDurumu === "hepsi" &&
                            !bulkFetchFilters.minStok
                          }
                        >
                          <i className="fas fa-eraser me-1"></i>
                          Temizle
                        </button>
                      </div>
                    </div>
                  </div>

                  <div className="card-body p-0">
                    <div className="table-responsive">
                      <table className="table table-hover mb-0">
                        <thead className="table-light">
                          <tr>
                            <th className="py-3 ps-3">#</th>
                            <th className="py-3">Stok Kodu</th>
                            <th className="py-3">Ürün Adı</th>
                            <th className="py-3">Grup Kodu</th>
                            <th className="py-3">Barkod</th>
                            <th className="py-3">Birim</th>
                            <th className="py-3 text-center">Stok</th>
                            <th className="py-3">Satış Fiyatı</th>
                            <th className="py-3">KDV</th>
                          </tr>
                        </thead>
                        <tbody>
                          {paginatedBulkProducts.length === 0 ? (
                            <tr>
                              <td
                                colSpan="9"
                                className="text-center py-5 text-muted"
                              >
                                <i className="fas fa-inbox fa-3x mb-3 d-block opacity-50"></i>
                                {bulkFetchProducts.length === 0
                                  ? "Henüz ürün çekilmedi. Yukarıdan 'Toplu Çekmeyi Başlat' butonuna tıklayın."
                                  : "Filtrelere uygun ürün bulunamadı."}
                              </td>
                            </tr>
                          ) : (
                            paginatedBulkProducts.map((product, index) => {
                              const stokMiktari =
                                product.depoMiktari ??
                                product.satilabilirMiktar ??
                                0;
                              const urunAdi =
                                product.stokAd ||
                                product.aciklama ||
                                product.stokKod ||
                                "İsimsiz Ürün";
                              return (
                                <tr key={product.stokKod || index}>
                                  <td className="py-2 ps-3 text-muted small">
                                    {((bulkFetchPagination.currentPage || 1) -
                                      1) *
                                      (bulkFetchPagination.pageSize || 50) +
                                      index +
                                      1}
                                  </td>
                                  <td className="py-2">
                                    <code className="text-primary small">
                                      {product.stokKod || "-"}
                                    </code>
                                  </td>
                                  <td className="py-2">
                                    <span
                                      className={`fw-medium ${!product.stokAd ? "text-warning" : ""}`}
                                      title={
                                        !product.stokAd
                                          ? "Mikro'dan isim gelmedi"
                                          : ""
                                      }
                                    >
                                      {urunAdi}
                                    </span>
                                  </td>
                                  <td className="py-2">
                                    <span className="badge bg-secondary-subtle text-dark">
                                      {product.grupKod || "-"}
                                    </span>
                                  </td>
                                  <td className="py-2">
                                    <span className="badge bg-light text-dark">
                                      {product.barkod || "-"}
                                    </span>
                                  </td>
                                  <td className="py-2">
                                    {product.birim || "-"}
                                  </td>
                                  <td className="py-2 text-center">
                                    <span
                                      className={`badge ${stokMiktari > 0 ? "bg-success" : "bg-danger"}`}
                                    >
                                      {stokMiktari}
                                    </span>
                                  </td>
                                  <td className="py-2">
                                    <span className="fw-semibold text-success">
                                      {typeof product.satisFiyati === "number"
                                        ? `${product.satisFiyati.toFixed(2)} ₺`
                                        : product.satisFiyati || "0.00 ₺"}
                                    </span>
                                  </td>
                                  <td className="py-2">
                                    <span className="badge bg-warning text-dark">
                                      %{product.kdvOrani || "0"}
                                    </span>
                                  </td>
                                </tr>
                              );
                            })
                          )}
                        </tbody>
                      </table>
                    </div>

                    {/* Sayfalama Kontrolleri */}
                    <div className="d-flex justify-content-between align-items-center p-3 bg-light">
                      {/* Sol: Bilgi */}
                      <div className="text-muted small">
                        <span>
                          {bulkFetchState.totalCount > 0 ? (
                            <>
                              Toplam {bulkFetchState.totalCount} üründen{" "}
                              {(bulkFetchPagination.currentPage - 1) *
                                bulkFetchPagination.pageSize +
                                1}
                              -
                              {Math.min(
                                bulkFetchPagination.currentPage *
                                  bulkFetchPagination.pageSize,
                                bulkFetchState.totalCount,
                              )}{" "}
                              arası gösteriliyor
                            </>
                          ) : (
                            <>Henüz ürün çekilmedi</>
                          )}
                        </span>
                      </div>

                      {/* Sağ: Sayfalama Butonları */}
                      <div className="btn-group btn-group-sm" role="group">
                        {/* İlk sayfa */}
                        <button
                          className="btn btn-outline-secondary"
                          disabled={(bulkFetchPagination.currentPage || 1) <= 1}
                          onClick={() => changeBulkProductsPage(1)}
                          title="İlk sayfa"
                        >
                          <i className="fas fa-angle-double-left"></i>
                        </button>

                        {/* Önceki sayfa */}
                        <button
                          className="btn btn-outline-secondary"
                          disabled={(bulkFetchPagination.currentPage || 1) <= 1}
                          onClick={() =>
                            changeBulkProductsPage(
                              (bulkFetchPagination.currentPage || 1) - 1,
                            )
                          }
                          title="Önceki sayfa"
                        >
                          <i className="fas fa-chevron-left"></i>
                        </button>

                        {/* Sayfa numaraları (mevcut +/- 2) */}
                        {(() => {
                          const totalPages = bulkProductsTotalPages;
                          const current = bulkFetchPagination.currentPage || 1;
                          const start = Math.max(1, current - 2);
                          const end = Math.min(totalPages, current + 2);
                          const pages = [];

                          for (let i = start; i <= end; i++) {
                            pages.push(
                              <button
                                key={i}
                                className={`btn ${i === current ? "btn-primary" : "btn-outline-secondary"}`}
                                onClick={() => changeBulkProductsPage(i)}
                              >
                                {i}
                              </button>,
                            );
                          }
                          return pages;
                        })()}

                        {/* Sonraki sayfa */}
                        <button
                          className="btn btn-outline-secondary"
                          disabled={
                            (bulkFetchPagination.currentPage || 1) >=
                            bulkProductsTotalPages
                          }
                          onClick={() =>
                            changeBulkProductsPage(
                              (bulkFetchPagination.currentPage || 1) + 1,
                            )
                          }
                          title="Sonraki sayfa"
                        >
                          <i className="fas fa-chevron-right"></i>
                        </button>

                        {/* Son sayfa */}
                        <button
                          className="btn btn-outline-secondary"
                          disabled={
                            (bulkFetchPagination.currentPage || 1) >=
                            bulkProductsTotalPages
                          }
                          onClick={() =>
                            changeBulkProductsPage(bulkProductsTotalPages)
                          }
                          title="Son sayfa"
                        >
                          <i className="fas fa-angle-double-right"></i>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}
        </>
      )}

      {/* Ayarlar Tab'ı */}
      {activeTab === "settings" && (
        <div className="row g-3">
          <div className="col-12">
            <div className="card border-0 shadow-sm">
              <div className="card-header bg-white py-3">
                <h6 className="mb-0 fw-bold">
                  <i className="fas fa-cog me-2 text-secondary"></i>
                  ERP Bağlantı Ayarları
                </h6>
              </div>
              <div className="card-body">
                <div className="alert alert-info">
                  <i className="fas fa-info-circle me-2"></i>
                  <strong>Not:</strong> ERP bağlantı ayarları{" "}
                  <code>appsettings.json</code> dosyasından yönetilmektedir.
                  Değişiklik yapmak için sunucu yapılandırmasını güncelleyin.
                </div>
                <div className="row">
                  <div className="col-md-6">
                    <div className="mb-3">
                      <label className="form-label fw-semibold">API URL</label>
                      <input
                        type="text"
                        className="form-control"
                        value={connectionStatus?.apiUrl || "Bilinmiyor"}
                        disabled
                      />
                    </div>
                    <div className="mb-3">
                      <label className="form-label fw-semibold">
                        Bağlantı Durumu
                      </label>
                      <input
                        type="text"
                        className={`form-control ${connectionStatus?.isConnected ? "text-success" : "text-danger"}`}
                        value={
                          connectionStatus?.isConnected
                            ? "Bağlı"
                            : "Bağlantı Yok"
                        }
                        disabled
                      />
                    </div>
                  </div>
                  <div className="col-md-6">
                    <div className="mb-3">
                      <label className="form-label fw-semibold">
                        Son Bağlantı Zamanı
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        value={
                          connectionStatus?.timestamp
                            ? new Date(
                                connectionStatus.timestamp,
                              ).toLocaleString("tr-TR")
                            : "-"
                        }
                        disabled
                      />
                    </div>
                    <div className="mb-3">
                      <label className="form-label fw-semibold">
                        Varsayılan Depo No
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        value="1"
                        disabled
                      />
                      <small className="text-muted">
                        Tek depo kullanılmaktadır.
                      </small>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="col-12">
            <div className="card border-0 shadow-sm">
              <div className="card-header bg-white py-3 d-flex justify-content-between align-items-center flex-wrap gap-2">
                <h6 className="mb-0 fw-bold">
                  <i className="fas fa-power-off me-2 text-warning"></i>
                  Aktif/Pasif Ürün Yönetimi
                </h6>
                <small className="text-muted">
                  Toplam: {formatNumber(activeTotalCount)} ürün
                </small>
              </div>
              <div className="card-body">
                <div className="row g-2 mb-3">
                  <div className="col-md-4">
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      placeholder="Stok kodu veya urun adi ara"
                      value={activeSearchText}
                      onChange={(e) => setActiveSearchText(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") {
                          loadActiveProducts(1);
                        }
                      }}
                    />
                  </div>
                  <div className="col-md-3">
                    <select
                      className="form-select form-select-sm"
                      value={activeFilter}
                      onChange={(e) => setActiveFilter(e.target.value)}
                    >
                      <option value="hepsi">Tum Durumlar</option>
                      <option value="aktif">Sadece Aktif</option>
                      <option value="pasif">Sadece Pasif</option>
                    </select>
                  </div>
                  <div className="col-md-2">
                    <select
                      className="form-select form-select-sm"
                      value={activePageSize}
                      onChange={(e) =>
                        setActivePageSize(Number(e.target.value))
                      }
                    >
                      <option value={20}>20</option>
                      <option value={50}>50</option>
                      <option value={100}>100</option>
                    </select>
                  </div>
                  <div className="col-md-3 d-flex gap-2">
                    <button
                      className="btn btn-sm btn-outline-primary"
                      onClick={() => loadActiveProducts(1)}
                      disabled={activeProductsLoading}
                    >
                      <i className="fas fa-search me-1"></i>
                      Listele
                    </button>
                    <button
                      className="btn btn-sm btn-outline-secondary"
                      onClick={() => {
                        setActiveSearchText("");
                        setActiveFilter("hepsi");
                        setActivePage(1);
                        loadActiveProducts(1);
                      }}
                    >
                      Temizle
                    </button>
                  </div>
                </div>

                <div className="d-flex flex-wrap gap-2 mb-3">
                  <button
                    className="btn btn-sm btn-success"
                    onClick={() => bulkChangeProductStatus(true)}
                    disabled={selectedStokKodlar.length === 0 || loading}
                  >
                    <i className="fas fa-check me-1"></i>
                    Secilenleri Aktif Et ({selectedStokKodlar.length})
                  </button>
                  <button
                    className="btn btn-sm btn-warning"
                    onClick={() => bulkChangeProductStatus(false)}
                    disabled={selectedStokKodlar.length === 0 || loading}
                  >
                    <i className="fas fa-ban me-1"></i>
                    Secilenleri Pasif Et ({selectedStokKodlar.length})
                  </button>
                </div>

                <div className="table-responsive">
                  <table className="table table-sm table-hover align-middle">
                    <thead className="table-light">
                      <tr>
                        <th style={{ width: 40 }}>
                          <input
                            type="checkbox"
                            checked={allVisibleActiveProductsSelected}
                            onChange={toggleSelectAllVisibleActiveProducts}
                          />
                        </th>
                        <th>Stok Kodu</th>
                        <th>Urun Adi</th>
                        <th>Durum</th>
                        <th className="text-end">Islem</th>
                      </tr>
                    </thead>
                    <tbody>
                      {activeProductsLoading ? (
                        <tr>
                          <td
                            colSpan="5"
                            className="text-center py-3 text-muted"
                          >
                            <i className="fas fa-spinner fa-spin me-2"></i>
                            Yukleniyor...
                          </td>
                        </tr>
                      ) : activeProducts.length === 0 ? (
                        <tr>
                          <td
                            colSpan="5"
                            className="text-center py-3 text-muted"
                          >
                            Kayit bulunamadi.
                          </td>
                        </tr>
                      ) : (
                        activeProducts.map((item) => {
                          const stokKod = item.stokKod || item.sku;
                          const aktif = item.aktif === true;
                          return (
                            <tr key={stokKod}>
                              <td>
                                <input
                                  type="checkbox"
                                  checked={selectedStokKodlar.includes(stokKod)}
                                  onChange={() =>
                                    toggleSelectActiveProduct(stokKod)
                                  }
                                />
                              </td>
                              <td>
                                <code>{stokKod}</code>
                              </td>
                              <td>{item.stokAd || item.name || "-"}</td>
                              <td>
                                <span
                                  className={`badge ${aktif ? "bg-success" : "bg-secondary"}`}
                                >
                                  {aktif ? "Aktif" : "Pasif"}
                                </span>
                              </td>
                              <td className="text-end">
                                <button
                                  className={`btn btn-sm ${aktif ? "btn-outline-warning" : "btn-outline-success"}`}
                                  onClick={() =>
                                    toggleSingleProductActive(stokKod, aktif)
                                  }
                                  disabled={loading}
                                >
                                  {aktif ? "Pasif Yap" : "Aktif Yap"}
                                </button>
                              </td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>

                <div className="d-flex justify-content-between align-items-center mt-2">
                  <small className="text-muted">
                    Sayfa {activePage} / {activeTotalPages}
                  </small>
                  <div className="btn-group btn-group-sm">
                    <button
                      className="btn btn-outline-secondary"
                      disabled={activePage <= 1 || activeProductsLoading}
                      onClick={() => loadActiveProducts(activePage - 1)}
                    >
                      <i className="fas fa-chevron-left"></i>
                    </button>
                    <button
                      className="btn btn-outline-secondary"
                      disabled={
                        activePage >= activeTotalPages || activeProductsLoading
                      }
                      onClick={() => loadActiveProducts(activePage + 1)}
                    >
                      <i className="fas fa-chevron-right"></i>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="col-12">
            <div className="card border-0 shadow-sm">
              <div className="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                <h6 className="mb-0 fw-bold">
                  <i className="fas fa-shield-alt me-2 text-danger"></i>
                  Sync Sağlığı ve Conflict Özeti
                </h6>
                <button
                  className="btn btn-sm btn-outline-secondary"
                  onClick={() => loadSyncDiagnostics(24)}
                  disabled={syncDiagnosticsLoading}
                >
                  <i className="fas fa-sync-alt me-1"></i>
                  Yenile
                </button>
              </div>
              <div className="card-body">
                {syncDiagnosticsLoading ? (
                  <div className="text-muted">
                    <i className="fas fa-spinner fa-spin me-2"></i>
                    Diagnostics yükleniyor...
                  </div>
                ) : !syncDiagnostics ? (
                  <div className="text-muted">
                    Henüz diagnostics verisi yok.
                  </div>
                ) : (
                  <>
                    <div className="row g-2 mb-3">
                      <div className="col-6 col-md-2">
                        <div className="border rounded p-2 text-center bg-light">
                          <div className="fw-bold">
                            {formatNumber(
                              syncDiagnostics?.summary?.totalOperations || 0,
                            )}
                          </div>
                          <small className="text-muted">Toplam</small>
                        </div>
                      </div>
                      <div className="col-6 col-md-2">
                        <div className="border rounded p-2 text-center bg-light">
                          <div className="fw-bold text-success">
                            {formatNumber(
                              syncDiagnostics?.summary?.successfulOperations ||
                                0,
                            )}
                          </div>
                          <small className="text-muted">Başarılı</small>
                        </div>
                      </div>
                      <div className="col-6 col-md-2">
                        <div className="border rounded p-2 text-center bg-light">
                          <div className="fw-bold text-danger">
                            {formatNumber(
                              syncDiagnostics?.summary?.failedOperations || 0,
                            )}
                          </div>
                          <small className="text-muted">Başarısız</small>
                        </div>
                      </div>
                      <div className="col-6 col-md-2">
                        <div className="border rounded p-2 text-center bg-light">
                          <div className="fw-bold text-warning">
                            {formatNumber(
                              syncDiagnostics?.summary?.pendingRetries || 0,
                            )}
                          </div>
                          <small className="text-muted">Pending Retry</small>
                        </div>
                      </div>
                      <div className="col-6 col-md-2">
                        <div className="border rounded p-2 text-center bg-light">
                          <div className="fw-bold text-primary">
                            {formatNumber(
                              syncDiagnostics?.summary?.conflictCount || 0,
                            )}
                          </div>
                          <small className="text-muted">Conflict</small>
                        </div>
                      </div>
                      <div className="col-6 col-md-2">
                        <div className="border rounded p-2 text-center bg-light">
                          <div className="fw-bold">
                            %{syncDiagnostics?.summary?.successRate ?? 0}
                          </div>
                          <small className="text-muted">Başarı Oranı</small>
                        </div>
                      </div>
                    </div>

                    <div className="row g-3">
                      <div className="col-12">
                        <h6 className="small fw-bold mb-2">
                          Son Conflict Kayıtları
                        </h6>
                        <div
                          className="table-responsive"
                          style={{ maxHeight: "240px" }}
                        >
                          <table className="table table-sm align-middle">
                            <thead className="table-light">
                              <tr>
                                <th>Entity</th>
                                <th>Yön</th>
                                <th>Mesaj</th>
                                <th className="text-end">Aksiyon</th>
                              </tr>
                            </thead>
                            <tbody>
                              {(syncDiagnostics?.recentConflicts || [])
                                .length === 0 ? (
                                <tr>
                                  <td
                                    colSpan="4"
                                    className="text-muted text-center"
                                  >
                                    Kayıt yok
                                  </td>
                                </tr>
                              ) : (
                                (syncDiagnostics?.recentConflicts || [])
                                  .slice(0, 20)
                                  .map((x) => (
                                    <tr key={`c-${x.id}`}>
                                      <td>{x.entityType || "-"}</td>
                                      <td>{x.direction || "-"}</td>
                                      <td title={x.message || ""}>
                                        {x.message || "-"}
                                      </td>
                                      <td className="text-end">
                                        <div className="btn-group btn-group-sm">
                                          <button
                                            className="btn btn-outline-primary"
                                            disabled={Boolean(
                                              syncActionLoading,
                                            )}
                                            onClick={() =>
                                              handleResolveConflict(
                                                x.id,
                                                "erpWins",
                                              )
                                            }
                                          >
                                            {syncActionLoading ===
                                            `resolve-${x.id}-erpWins`
                                              ? "..."
                                              : "ERP'yi Al"}
                                          </button>
                                          <button
                                            className="btn btn-outline-secondary"
                                            disabled={Boolean(
                                              syncActionLoading,
                                            )}
                                            onClick={() =>
                                              handleResolveConflict(
                                                x.id,
                                                "localWins",
                                              )
                                            }
                                          >
                                            {syncActionLoading ===
                                            `resolve-${x.id}-localWins`
                                              ? "..."
                                              : "Local'i Tut"}
                                          </button>
                                        </div>
                                      </td>
                                    </tr>
                                  ))
                              )}
                            </tbody>
                          </table>
                        </div>
                      </div>

                      <div className="col-md-6">
                        <h6 className="small fw-bold mb-2">Son Hatalar</h6>
                        <div
                          className="table-responsive"
                          style={{ maxHeight: "240px" }}
                        >
                          <table className="table table-sm align-middle">
                            <thead className="table-light">
                              <tr>
                                <th>Entity</th>
                                <th>Yön</th>
                                <th>Durum</th>
                                <th>Hata</th>
                              </tr>
                            </thead>
                            <tbody>
                              {(syncDiagnostics?.recentFailures || [])
                                .length === 0 ? (
                                <tr>
                                  <td
                                    colSpan="4"
                                    className="text-muted text-center"
                                  >
                                    Kayıt yok
                                  </td>
                                </tr>
                              ) : (
                                (syncDiagnostics?.recentFailures || [])
                                  .slice(0, 20)
                                  .map((x) => (
                                    <tr key={`f-${x.id}`}>
                                      <td>{x.entityType || "-"}</td>
                                      <td>{x.direction || "-"}</td>
                                      <td>{x.status || "-"}</td>
                                      <td
                                        className="text-danger"
                                        title={x.lastError || ""}
                                      >
                                        {x.lastError || "-"}
                                      </td>
                                    </tr>
                                  ))
                              )}
                            </tbody>
                          </table>
                        </div>
                      </div>

                      <div className="col-md-6">
                        <h6 className="small fw-bold mb-2">
                          Pending Retry Kayıtları
                        </h6>
                        <div
                          className="table-responsive"
                          style={{ maxHeight: "240px" }}
                        >
                          <table className="table table-sm align-middle">
                            <thead className="table-light">
                              <tr>
                                <th>Entity</th>
                                <th>Yön</th>
                                <th>Deneme</th>
                                <th>Mesaj</th>
                                <th className="text-end">Aksiyon</th>
                              </tr>
                            </thead>
                            <tbody>
                              {(syncDiagnostics?.pendingRetries || [])
                                .length === 0 ? (
                                <tr>
                                  <td
                                    colSpan="5"
                                    className="text-muted text-center"
                                  >
                                    Kayıt yok
                                  </td>
                                </tr>
                              ) : (
                                (syncDiagnostics?.pendingRetries || [])
                                  .slice(0, 20)
                                  .map((x) => (
                                    <tr key={`p-${x.id}`}>
                                      <td>{x.entityType || "-"}</td>
                                      <td>{x.direction || "-"}</td>
                                      <td>{x.attempts || 0}</td>
                                      <td title={x.message || ""}>
                                        {x.message || "-"}
                                      </td>
                                      <td className="text-end">
                                        <button
                                          className="btn btn-sm btn-outline-primary"
                                          disabled={Boolean(syncActionLoading)}
                                          onClick={() =>
                                            handleRetrySyncLog(x.id)
                                          }
                                        >
                                          {syncActionLoading === `retry-${x.id}`
                                            ? "..."
                                            : "Retry"}
                                        </button>
                                      </td>
                                    </tr>
                                  ))
                              )}
                            </tbody>
                          </table>
                        </div>
                      </div>
                    </div>
                  </>
                )}
              </div>
            </div>
          </div>

          <div className="col-12">
            <div className="card border-0 shadow-sm">
              <div className="card-header bg-white py-3">
                <h6 className="mb-0 fw-bold">
                  <i className="fas fa-file-import me-2 text-primary"></i>
                  Excel / CSV Import (Aktiflik)
                </h6>
              </div>
              <div className="card-body">
                <div className="row g-2 align-items-end">
                  <div className="col-md-6">
                    <label className="form-label small fw-semibold">
                      Dosya Sec
                    </label>
                    <input
                      type="file"
                      className="form-control form-control-sm"
                      accept=".csv,.xlsx,.xls"
                      onChange={(e) =>
                        setImportFile(e.target.files?.[0] || null)
                      }
                    />
                    <small className="text-muted">
                      Beklenen kolonlar: StokKod,Aktif
                    </small>
                  </div>
                  <div className="col-md-6 d-flex gap-2 flex-wrap">
                    <button
                      className="btn btn-outline-secondary btn-sm"
                      onClick={handleDownloadImportTemplate}
                      disabled={loading}
                    >
                      <i className="fas fa-download me-1"></i>
                      Template Indir
                    </button>
                    <button
                      className="btn btn-primary btn-sm"
                      onClick={handleImportActiveProducts}
                      disabled={!importFile || loading}
                    >
                      <i className="fas fa-upload me-1"></i>
                      Import Et
                    </button>
                    {importFile && (
                      <span className="badge bg-light text-dark border align-self-center">
                        {importFile.name}
                      </span>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ================================================================= */}
      {/* SYNC SAĞLIK TAB — Phase 5 Monitoring Dashboard                    */}
      {/* Kanal sağlığı, metrikler, uyarılar, ürün bilgi sync operasyonları */}
      {/* ================================================================= */}
      {activeTab === "syncHealth" && <SyncHealthDashboard />}

      {/* ================================================================= */}
      {/* ANLIK STOK AKIŞI TAB — SignalR StockHub canlı feed                 */}
      {/* HotPoll her 10sn'de delta tarayıp StockHub'a push ediyor.          */}
      {/* Bu panel, admin'e anlık görünürlük sağlar.                         */}
      {/* ================================================================= */}
      {activeTab === "realtime" && (
        <div className="card">
          <div className="card-header d-flex justify-content-between align-items-center">
            <h5 className="mb-0">
              <i className="fas fa-broadcast-tower me-2"></i>
              Anlık Stok Değişiklikleri
            </h5>
            <div className="d-flex align-items-center gap-2">
              <span
                className={`badge ${stockHubConnected ? "bg-success" : "bg-danger"}`}
              >
                {stockHubConnected ? "Bağlı" : "Bağlantı Yok"}
              </span>
              <span className="badge bg-secondary">
                Toplam: {totalStockUpdates}
              </span>
              <button
                className="btn btn-sm btn-outline-secondary"
                onClick={clearStockHistory}
                title="Geçmişi temizle"
              >
                <i className="fas fa-trash-alt me-1"></i> Temizle
              </button>
            </div>
          </div>
          <div className="card-body p-0">
            {realtimeStockUpdates.length === 0 ? (
              <div className="text-center text-muted py-5">
                <i className="fas fa-satellite-dish fa-3x mb-3 d-block opacity-50"></i>
                <p>Henüz stok değişikliği alınmadı.</p>
                <small>
                  HotPoll servisi 10 saniyede bir delta taraması yapar,
                  değişiklik olduğunda burada görünür.
                </small>
              </div>
            ) : (
              <div
                className="table-responsive"
                style={{ maxHeight: "600px", overflowY: "auto" }}
              >
                <table className="table table-sm table-hover mb-0">
                  <thead className="table-light sticky-top">
                    <tr>
                      <th style={{ width: 50 }}>Tip</th>
                      <th>Ürün ID</th>
                      <th>Ürün Adı</th>
                      <th className="text-end">Önceki</th>
                      <th className="text-end">Yeni</th>
                      <th className="text-end">Fark</th>
                      <th>Zaman</th>
                    </tr>
                  </thead>
                  <tbody>
                    {realtimeStockUpdates.map((update, idx) => {
                      const isStockUpdate =
                        update.type === "stock" ||
                        update.newQuantity !== undefined;
                      const isPriceUpdate =
                        update.type === "price" ||
                        update.newPrice !== undefined;
                      const oldVal = isStockUpdate
                        ? update.oldQuantity
                        : update.oldPrice;
                      const newVal = isStockUpdate
                        ? update.newQuantity
                        : update.newPrice;
                      const diff = (newVal ?? 0) - (oldVal ?? 0);
                      const isNegative = diff < 0;
                      const isZeroStock = isStockUpdate && (newVal ?? 0) <= 0;

                      return (
                        <tr
                          key={`${update.productId}-${update.timestamp || idx}`}
                          className={isZeroStock ? "table-danger" : ""}
                        >
                          <td>
                            {isStockUpdate && (
                              <span
                                className="badge bg-primary"
                                title="Stok değişikliği"
                              >
                                <i className="fas fa-boxes"></i>
                              </span>
                            )}
                            {isPriceUpdate && (
                              <span
                                className="badge bg-warning text-dark"
                                title="Fiyat değişikliği"
                              >
                                <i className="fas fa-tag"></i>
                              </span>
                            )}
                          </td>
                          <td>
                            <code>#{update.productId}</code>
                          </td>
                          <td>
                            {update.productName || "-"}
                            {isZeroStock && (
                              <span className="badge bg-danger ms-1">
                                TÜKENDİ
                              </span>
                            )}
                          </td>
                          <td className="text-end text-muted">
                            {oldVal !== undefined && oldVal !== null
                              ? isPriceUpdate
                                ? `₺${Number(oldVal).toFixed(2)}`
                                : formatNumber(oldVal)
                              : "-"}
                          </td>
                          <td className="text-end fw-bold">
                            {newVal !== undefined && newVal !== null
                              ? isPriceUpdate
                                ? `₺${Number(newVal).toFixed(2)}`
                                : formatNumber(newVal)
                              : "-"}
                          </td>
                          <td
                            className={`text-end fw-bold ${isNegative ? "text-danger" : "text-success"}`}
                          >
                            {diff !== 0
                              ? `${isNegative ? "" : "+"}${isPriceUpdate ? `₺${diff.toFixed(2)}` : formatNumber(diff)}`
                              : "-"}
                          </td>
                          <td className="text-muted small">
                            {update.timestamp
                              ? new Date(update.timestamp).toLocaleTimeString(
                                  "tr-TR",
                                )
                              : "-"}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
