import React, { useEffect, useState, useCallback, useRef, useMemo } from "react";
import { MicroService } from "../../services/microService";

// Yardımcı fonksiyonlar
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

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
    pageSize: 500,
  });
  const [grupKodFilter, setGrupKodFilter] = useState("");
  const [connectionStatus, setConnectionStatus] = useState(null);
  const [connectionLoading, setConnectionLoading] = useState(false);

  // Fiyat listesi ve depo seçimi için yeni state'ler
  const [fiyatListesiNo, setFiyatListesiNo] = useState(1); // 1-10 arası, varsayılan Perakende
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

  const [bulkFetchConfig, setBulkFetchConfig] = useState({
    pageSize: 50, // Sayfa başına ürün
    throttleDelayMs: 300, // İstekler arası bekleme (ms)
    maxRetryCount: 3, // Max retry sayısı
    retryDelayMs: 3000, // Retry bekleme süresi (ms)
    syncMode: "newOnly", // newOnly | full
    grupKod: "", // Grup kodu filtresi
    fiyatListesiNo: 1, // Fiyat listesi numarası
    depoNo: 0, // Depo numarası (0 = tüm depolar)
  });

  // Abort controller ref - iptal için
  const bulkFetchAbortRef = useRef(null);

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
      const [prods, stk] = await Promise.all([
        MicroService.getProducts(),
        MicroService.getStocks(),
      ]);
      setProducts(prods || []);
      setStocks(stk || []);
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
    async (sayfa = 1, sayfaBuyuklugu = 500, grupKod = grupKodFilter) => {
      setLoading(true);
      try {
        const result = await MicroService.getStokListesi({
          sayfa,
          sayfaBuyuklugu,
          depoNo: depoNo, // Seçilen depo (0 = tüm depolar)
          fiyatListesiNo: fiyatListesiNo, // Seçilen fiyat listesi (1-10)
          grupKod: (grupKod || "").trim() || undefined,
          sadeceAktif: true,
        });

        if (result?.success) {
          const initialBatch = Array.isArray(result.data) ? result.data : [];
          let mergedData = [...initialBatch];

          // Mikro bazı ortamlarda her çağrıda en fazla ~20 kayıt döndürüyor.
          // Kullanıcı 500 istediğinde ek sayfaları otomatik çekip listeyi tamamla.
          if (
            sayfa === 1 &&
            mergedData.length > 0 &&
            mergedData.length < sayfaBuyuklugu
          ) {
            const estimatedPageSize = Math.max(1, mergedData.length);
            const maxExtraPages = Math.max(
              10,
              Math.ceil(sayfaBuyuklugu / estimatedPageSize) + 5,
            );
            let nextPage = 2;
            const seenFirstSkus = new Set();
            const firstSku = mergedData[0]?.stokKod;
            if (firstSku) {
              seenFirstSkus.add(firstSku);
            }

            for (
              let i = 0;
              i < maxExtraPages && mergedData.length < sayfaBuyuklugu;
              i += 1
            ) {
              const nextResult = await MicroService.getStokListesi({
                sayfa: nextPage,
                sayfaBuyuklugu,
                depoNo: depoNo,
                fiyatListesiNo: fiyatListesiNo,
                grupKod: (grupKod || "").trim() || undefined,
                sadeceAktif: true,
              });

              const nextBatch =
                nextResult?.success && Array.isArray(nextResult?.data)
                  ? nextResult.data
                  : [];

              if (!nextBatch.length) {
                break;
              }

              const nextFirstSku = nextBatch[0]?.stokKod;
              if (nextFirstSku && seenFirstSkus.has(nextFirstSku)) {
                break;
              }

              if (nextFirstSku) {
                seenFirstSkus.add(nextFirstSku);
              }

              mergedData = mergedData.concat(nextBatch);
              nextPage += 1;
            }

            if (mergedData.length > sayfaBuyuklugu) {
              mergedData = mergedData.slice(0, sayfaBuyuklugu);
            }
          }

          setStokListesi(mergedData);

          const fetchedCount = mergedData.length;
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
    [grupKodFilter, fiyatListesiNo, depoNo],
  );

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
  const startBulkFetch = useCallback(async () => {
    // Zaten çalışıyorsa durdur
    if (bulkFetchState.isRunning) {
      console.warn("⚠️ Zaten bir toplu çekme işlemi devam ediyor.");
      return;
    }

    // Abort controller oluştur
    bulkFetchAbortRef.current = new AbortController();

    // State'i sıfırla ve başlat
    setBulkFetchState({
      isRunning: true,
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

    const config = bulkFetchConfig;
    const startTime = Date.now();
    const allProducts = [];
    const failedPages = [];
    let currentPage = 1;
    let totalCount = 0;
    let totalPages = null;
    let consecutiveErrors = 0;

    // İstatistikler
    const stats = {
      totalRequests: 0,
      successfulRequests: 0,
      failedRequests: 0,
      retryCount: 0,
      avgResponseTimeMs: 0,
      responseTimes: [],
    };

    console.log("🚀 Mikro API'den toplu ürün çekme başlatıldı...");
    console.log(
      `📊 Konfigürasyon: pageSize=${config.pageSize}, throttle=${config.throttleDelayMs}ms`,
    );

    setMessage("Toplu ürün çekme işlemi başlatıldı...");
    setMessageType("info");

    // ============================================================
    // SIRALI DÖNGÜ - while ile sequential request (Promise.all YOK!)
    // ============================================================
    while (true) {
      // İptal kontrolü
      if (bulkFetchAbortRef.current?.signal?.aborted) {
        console.warn("⚠️ İşlem kullanıcı tarafından iptal edildi.");
        setBulkFetchState((prev) => ({
          ...prev,
          isRunning: false,
          error: "İşlem iptal edildi",
        }));
        setMessage("İşlem iptal edildi.");
        setMessageType("warning");
        return;
      }

      const requestStartTime = Date.now();
      let pageSuccess = false;
      let retryAttempt = 0;

      // ============================================================
      // RETRY MEKANIZMASI - try-catch bloğu
      // ============================================================
      while (retryAttempt < config.maxRetryCount && !pageSuccess) {
        try {
          stats.totalRequests++;

          // Mikro API'den stok listesi çek (await ile bekle - sequential)
          // Fiyat listesi ve depo numarası config'den alınır
          const response = await MicroService.getStokListesi({
            sayfa: currentPage,
            sayfaBuyuklugu: config.pageSize,
            depoNo: config.depoNo || 0,
            fiyatListesiNo: config.fiyatListesiNo || 1,
            grupKod: config.grupKod?.trim() || undefined,
            sadeceAktif: true,
          });

          // Yanıtı işle
          if (response?.success) {
            const items = Array.isArray(response.data) ? response.data : [];

            // İlk sayfada toplam değerleri ayarla
            if (currentPage === 1) {
              totalCount = Number(
                response.toplamKayit || response.totalCount || 0,
              );
              totalPages = Math.ceil(totalCount / config.pageSize) || 1;
              console.log(
                `📦 Toplam ${totalCount} ürün, ${totalPages} sayfa bulundu. (Fiyat Listesi: ${config.fiyatListesiNo}, Depo: ${config.depoNo || "Tüm"})`,
              );
            }

            // Ürünleri ana diziye ekle
            allProducts.push(...items);

            // İstatistikleri güncelle
            const responseTime = Date.now() - requestStartTime;
            stats.responseTimes.push(responseTime);
            stats.successfulRequests++;
            stats.avgResponseTimeMs =
              stats.responseTimes.reduce((a, b) => a + b, 0) /
              stats.responseTimes.length;

            pageSuccess = true;
            consecutiveErrors = 0;
          } else {
            throw new Error(response?.message || "API yanıtı başarısız");
          }
        } catch (error) {
          retryAttempt++;
          stats.retryCount++;
          consecutiveErrors++;

          const errorMessage = error.message || "Bilinmeyen hata";
          console.warn(
            `⚠️ Sayfa ${currentPage} - Hata (Deneme ${retryAttempt}/${config.maxRetryCount}): ${errorMessage}`,
          );

          // Son deneme değilse bekle ve tekrar dene
          if (retryAttempt < config.maxRetryCount) {
            console.log(`⏳ ${config.retryDelayMs}ms bekleniyor...`);
            await sleep(config.retryDelayMs);
          }
        }
      }

      // Tüm retry'lar başarısız olduysa sayfayı hatalı işaretle
      if (!pageSuccess) {
        console.error(`❌ Sayfa ${currentPage} - Tüm denemeler başarısız!`);
        failedPages.push(currentPage);
        stats.failedRequests++;
      }

      // ============================================================
      // İLERLEME TAKİBİ - Progress = (Current_Page / Total_Pages) * 100
      // ============================================================
      const elapsedMs = Date.now() - startTime;
      const progressPercentage = totalPages
        ? Math.round((currentPage / totalPages) * 100)
        : 0;
      const avgTimePerPage = elapsedMs / currentPage;
      const remainingPages = (totalPages || 1) - currentPage;
      const estimatedRemainingMs = avgTimePerPage * remainingPages;

      // State güncelle
      setBulkFetchState((prev) => ({
        ...prev,
        progress: progressPercentage,
        currentPage,
        totalPages: totalPages || 0,
        fetchedCount: allProducts.length,
        totalCount,
        elapsedMs,
        estimatedRemainingMs,
        failedPages: [...failedPages],
      }));

      // Ürünleri state'e ekle (gerçek zamanlı gösterim için)
      setBulkFetchProducts([...allProducts]);
      
      // Debug: State güncelleme sonrasında ürün sayısını logla
      console.log(`[DEBUG] setBulkFetchProducts çağrıldı: ${allProducts.length} ürün, örnek:`, 
        allProducts.length > 0 ? allProducts[0] : 'boş');

      // Konsola log
      console.log(
        `📄 [${progressPercentage}%] Sayfa ${currentPage}/${totalPages || "?"} ` +
          `| Çekilen: ${allProducts.length}/${totalCount} ürün ` +
          `| Süre: ${(elapsedMs / 1000).toFixed(1)}s`,
      );

      // ============================================================
      // DÖNGÜ SONLANDIRMA KONTROLÜ
      // ============================================================
      // Son sayfaya ulaştık mı?
      if (totalPages !== null && currentPage >= totalPages) {
        console.log("✅ Tüm sayfalar tamamlandı!");
        break;
      }

      // Hiç ürün gelmiyorsa döngüyü sonlandır (güvenlik)
      if (pageSuccess && allProducts.length === 0 && currentPage > 1) {
        console.warn("⚠️ Boş yanıt alındı, döngü sonlandırılıyor.");
        break;
      }

      // Art arda çok fazla hata varsa dur (sunucu sorunu)
      if (consecutiveErrors >= 5) {
        console.error("❌ Art arda 5 hata! İşlem durduruluyor.");
        setBulkFetchState((prev) => ({
          ...prev,
          error: "Art arda 5 hata oluştu",
        }));
        break;
      }

      // ============================================================
      // THROTTLE (NEFES ALDIRMA) GECİKMESİ - Rate limit önleme
      // ============================================================
      await sleep(config.throttleDelayMs);

      // Sonraki sayfaya geç
      currentPage++;
    }

    // ============================================================
    // İŞLEM TAMAMLANDI - Sonuçları kaydet
    // ============================================================
    const totalElapsedMs = Date.now() - startTime;

    setBulkFetchState((prev) => ({
      ...prev,
      isRunning: false,
      isComplete: true,
      progress: 100,
      elapsedMs: totalElapsedMs,
      estimatedRemainingMs: 0,
      stats: {
        ...stats,
        totalPages: totalPages || currentPage,
        pagesProcessed: currentPage,
        avgResponseTimeMs: Math.round(stats.avgResponseTimeMs),
      },
    }));

    // Final log ve mesaj
    console.log("════════════════════════════════════════════════");
    console.log("📊 TOPLU ÜRÜN ÇEKME İŞLEMİ TAMAMLANDI");
    console.log("════════════════════════════════════════════════");
    console.log(`✅ Başarıyla çekilen: ${allProducts.length} ürün`);
    console.log(
      `❌ Başarısız sayfalar: ${failedPages.length > 0 ? failedPages.join(", ") : "Yok"}`,
    );
    console.log(`⏱️ Toplam süre: ${(totalElapsedMs / 1000).toFixed(2)} saniye`);
    console.log("════════════════════════════════════════════════");

    if (failedPages.length > 0) {
      setMessage(
        `${allProducts.length} ürün çekildi (${failedPages.length} sayfa hatalı)`,
      );
      setMessageType("warning");
    } else {
      setMessage(
        `${allProducts.length} ürün başarıyla çekildi! (${(totalElapsedMs / 1000).toFixed(1)}s)`,
      );
      setMessageType("success");
    }
  }, [bulkFetchState.isRunning, bulkFetchConfig]);

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

  // CSV Export fonksiyonu
  const exportBulkFetchToCSV = useCallback(() => {
    if (bulkFetchProducts.length === 0) {
      setMessage("Export edilecek ürün yok!");
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
    const rows = bulkFetchProducts.map((p) => [
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
      BOM + headers.join(";") + "\n" + rows.map((r) => r.join(";")).join("\n");

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

    setMessage(`${bulkFetchProducts.length} ürün CSV olarak indirildi.`);
    setMessageType("success");
  }, [bulkFetchProducts]);

  // CSV Export fonksiyonu (Alias - Yeni tablo için)
  const exportBulkProductsToCSV = exportBulkFetchToCSV;

  // ============================================================================
  // BULK FETCH SAYFALAMA FONKSİYONLARI
  // ============================================================================
  // Sayfalanmış ürünleri hesapla
  // Filtrelenmiş ürün listesini hesapla
  const filteredBulkProducts = useMemo(() => {
    if (!bulkFetchProducts.length) return [];

    return bulkFetchProducts.filter((product) => {
      // Stok kodu filtresi
      if (bulkFetchFilters.stokKod) {
        const stokKod = (product.stokKod || "").toLowerCase();
        if (!stokKod.includes(bulkFetchFilters.stokKod.toLowerCase())) {
          return false;
        }
      }
      // Grup kodu filtresi
      if (bulkFetchFilters.grupKod) {
        const grupKod = (product.grupKod || "").toLowerCase();
        if (!grupKod.includes(bulkFetchFilters.grupKod.toLowerCase())) {
          return false;
        }
      }
      // Stok durumu filtresi
      const stokMiktari = product.depoMiktari ?? product.satilabilirMiktar ?? 0;
      if (bulkFetchFilters.stokDurumu === "stokta" && stokMiktari <= 0) {
        return false;
      }
      if (bulkFetchFilters.stokDurumu === "stoksuz" && stokMiktari > 0) {
        return false;
      }
      // Minimum stok filtresi
      if (
        bulkFetchFilters.minStok !== "" &&
        bulkFetchFilters.minStok !== null
      ) {
        const minValue = parseFloat(bulkFetchFilters.minStok);
        if (!isNaN(minValue) && stokMiktari < minValue) {
          return false;
        }
      }
      return true;
    });
  }, [bulkFetchProducts, bulkFetchFilters]);

  // Eski fonksiyonu uyumluluk için tut (varolan kullanımlar için)
  const getFilteredBulkProducts = useCallback(() => {
    return filteredBulkProducts;
  }, [filteredBulkProducts]);

  // Sayfalanmış ürünleri hesapla (useMemo ile performanslı)
  const paginatedBulkProducts = useMemo(() => {
    if (!filteredBulkProducts.length) return [];

    const pageSize = bulkFetchPagination.pageSize || 50;
    const currentPage = bulkFetchPagination.currentPage || 1;
    const startIndex = (currentPage - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    
    console.log(`[DEBUG] paginatedBulkProducts: filtered=${filteredBulkProducts.length}, page=${currentPage}, pageSize=${pageSize}, start=${startIndex}, end=${endIndex}`);
    
    return filteredBulkProducts.slice(startIndex, endIndex);
  }, [filteredBulkProducts, bulkFetchPagination]);

  // Eski fonksiyonu uyumluluk için tut
  const getPaginatedBulkProducts = useCallback(() => {
    return paginatedBulkProducts;
  }, [paginatedBulkProducts]);

  // Toplam sayfa sayısını hesapla
  const bulkProductsTotalPages = useMemo(() => {
    const pageSize = bulkFetchPagination.pageSize || 50;
    return Math.ceil(filteredBulkProducts.length / pageSize) || 1;
  }, [filteredBulkProducts.length, bulkFetchPagination.pageSize]);

  // Eski fonksiyonu uyumluluk için tut
  const getBulkProductsTotalPages = useCallback(() => {
    return bulkProductsTotalPages;
  }, [bulkProductsTotalPages]);

  // Sayfa değiştir - filtrelenmiş ürün sayısına göre hesapla
  const changeBulkProductsPage = useCallback(
    (newPage) => {
      // Filtrelenmiş ürün sayısına göre toplam sayfa hesapla
      const pageSize = bulkFetchPagination.pageSize || 50;
      const totalPages = Math.ceil(filteredBulkProducts.length / pageSize) || 1;
      
      console.log(`[DEBUG] changeBulkProductsPage: newPage=${newPage}, totalPages=${totalPages}, filtered=${filteredBulkProducts.length}, pageSize=${pageSize}`);
      
      if (newPage >= 1 && newPage <= totalPages) {
        setBulkFetchPagination((prev) => ({ ...prev, currentPage: newPage }));
      }
    },
    [filteredBulkProducts.length, bulkFetchPagination.pageSize],
  );

  // Sayfa boyutunu değiştir
  const changeBulkProductsPageSize = useCallback((newPageSize) => {
    setBulkFetchPagination((prev) => ({
      currentPage: 1, // Sayfa boyutu değişince ilk sayfaya dön
      pageSize: newPageSize,
    }));
  }, []);

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
            onClick={() => setActiveTab("bulkFetch")}
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
            onClick={() => setActiveTab("settings")}
          >
            <i className="fas fa-cog me-1"></i> Ayarlar
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
            {/* Ürünler */}
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
                  Toplam: {stokListesiMeta.totalCount} kayıt | 
                  Fiyat Listesi: {fiyatListesiNo} | 
                  Depo: {depoNo === 0 ? "Tümü" : depoNo}
                </small>
              </div>
              <button
                className="btn btn-primary btn-sm"
                onClick={() => loadStokListesi(1, 500, grupKodFilter)}
                disabled={loading}
              >
                <i className="fas fa-download me-1"></i> Mikro'dan Çek (500)
              </button>
            </div>
            
            {/* Filtre Satırı */}
            <div className="row g-2 align-items-end">
              <div className="col-md-3">
                <label className="form-label small mb-1">
                  <i className="fas fa-tags me-1 text-success"></i>
                  Fiyat Listesi
                </label>
                <select
                  className="form-select form-select-sm"
                  value={fiyatListesiNo}
                  onChange={(e) => setFiyatListesiNo(parseInt(e.target.value, 10))}
                  disabled={loading}
                >
                  <option value={1}>1 - Perakende</option>
                  <option value={2}>2 - Toptan</option>
                  <option value={3}>3 - Bayi</option>
                  <option value={4}>4 - İndirimli</option>
                  <option value={5}>5 - Özel</option>
                  <option value={6}>6 - Liste 6</option>
                  <option value={7}>7 - Liste 7</option>
                  <option value={8}>8 - Liste 8</option>
                  <option value={9}>9 - Liste 9</option>
                  <option value={10}>10 - Liste 10</option>
                </select>
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
                  <option value={0}>Tüm Depolar</option>
                  <option value={1}>Depo 1</option>
                  <option value={2}>Depo 2</option>
                  <option value={3}>Depo 3</option>
                  <option value={4}>Depo 4</option>
                  <option value={5}>Depo 5</option>
                </select>
              </div>
              <div className="col-md-4">
                <label className="form-label small mb-1">Grup Kodu</label>
                <input
                  type="text"
                  className="form-control form-control-sm"
                  placeholder="Grup kodu ara"
                  value={grupKodFilter}
                  onChange={(e) => setGrupKodFilter(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      loadStokListesi(1, 500, grupKodFilter);
                    }
                  }}
                  disabled={loading}
                />
              </div>
              <div className="col-md-3">
                <button
                  className="btn btn-outline-secondary btn-sm w-100"
                  onClick={() => loadStokListesi(1, 500, grupKodFilter)}
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
                    <th className="text-end">Fiyat (Liste {fiyatListesiNo})</th>
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
                        <option value={20}>20 ürün/sayfa</option>
                        <option value={50}>50 ürün/sayfa</option>
                        <option value={100}>100 ürün/sayfa</option>
                      </select>
                      <small className="text-muted">
                        Her istekte çekilecek ürün sayısı
                      </small>
                    </div>
                    <div className="col-6">
                      <label className="form-label small fw-semibold">
                        Gecikme (ms)
                      </label>
                      <select
                        className="form-select form-select-sm"
                        value={bulkFetchConfig.throttleDelayMs}
                        onChange={(e) =>
                          setBulkFetchConfig((prev) => ({
                            ...prev,
                            throttleDelayMs: Number(e.target.value),
                          }))
                        }
                        disabled={bulkFetchState.isRunning}
                      >
                        <option value={200}>200ms (Hızlı)</option>
                        <option value={300}>300ms (Normal)</option>
                        <option value={500}>500ms (Yavaş)</option>
                        <option value={1000}>1000ms (Çok Yavaş)</option>
                      </select>
                      <small className="text-muted">
                        İstekler arası bekleme
                      </small>
                    </div>
                    
                    {/* Fiyat Listesi Seçimi */}
                    <div className="col-md-6">
                      <label className="form-label small fw-semibold">
                        <i className="fas fa-tags me-1 text-success"></i>
                        Fiyat Listesi No
                      </label>
                      <select
                        className="form-select form-select-sm"
                        value={bulkFetchConfig.fiyatListesiNo}
                        onChange={(e) =>
                          setBulkFetchConfig((prev) => ({
                            ...prev,
                            fiyatListesiNo: parseInt(e.target.value, 10),
                          }))
                        }
                        disabled={bulkFetchState.isRunning}
                      >
                        <option value={1}>1 - Perakende</option>
                        <option value={2}>2 - Toptan</option>
                        <option value={3}>3 - Bayi</option>
                        <option value={4}>4 - İndirimli</option>
                        <option value={5}>5 - Özel</option>
                        <option value={6}>6 - Liste 6</option>
                        <option value={7}>7 - Liste 7</option>
                        <option value={8}>8 - Liste 8</option>
                        <option value={9}>9 - Liste 9</option>
                        <option value={10}>10 - Liste 10</option>
                      </select>
                      <small className="text-muted">
                        Hangi fiyat listesinden fiyat çekilecek
                      </small>
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
                  <li>Ürünler sayfa sayfa sıralı olarak çekilir</li>
                  <li>
                    Sunucu yüklenmemesi için istekler arası bekleme yapılır
                  </li>
                  <li>Hata durumunda otomatik 3 kez yeniden denenir</li>
                  <li>İşlem sürerken iptal edilebilir</li>
                </ul>
              </div>

              {/* Aksiyon Butonları */}
              <div className="d-flex gap-2 flex-wrap">
                {!bulkFetchState.isRunning && !bulkFetchState.isComplete && (
                  <button
                    className="btn text-white fw-semibold"
                    style={{
                      background: "linear-gradient(135deg, #10b981, #059669)",
                    }}
                    onClick={startBulkFetch}
                  >
                    <i className="fas fa-rocket me-2"></i>
                    Toplu Çekmeyi Başlat
                  </button>
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

                {bulkFetchProducts.length > 0 && (
                  <button
                    className="btn btn-success fw-semibold"
                    onClick={exportBulkFetchToCSV}
                  >
                    <i className="fas fa-file-csv me-2"></i>
                    CSV İndir ({formatNumber(bulkFetchProducts.length)})
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
                        Çekilen Ürünler ({bulkFetchProducts.length} adet)
                        {getFilteredBulkProducts().length !==
                          bulkFetchProducts.length && (
                          <span className="badge bg-info ms-2">
                            {getFilteredBulkProducts().length} sonuç
                          </span>
                        )}
                      </h6>

                      {/* Sayfa boyutu seçici */}
                      <div className="d-flex align-items-center gap-2">
                        <span className="small text-muted">Sayfa boyutu:</span>
                        <select
                          className="form-select form-select-sm"
                          style={{ width: "80px" }}
                          value={bulkFetchPagination.pageSize}
                          onChange={(e) =>
                            setBulkFetchPagination((prev) => ({
                              ...prev,
                              pageSize: Number(e.target.value),
                              currentPage: 1,
                            }))
                          }
                        >
                          <option value={10}>10</option>
                          <option value={25}>25</option>
                          <option value={50}>50</option>
                          <option value={100}>100</option>
                          <option value={200}>200</option>
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
                            placeholder="Stok Kodu ara..."
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
                          />
                          {bulkFetchFilters.stokKod && (
                            <button
                              className="btn btn-outline-secondary"
                              onClick={() => {
                                setBulkFetchFilters((prev) => ({
                                  ...prev,
                                  stokKod: "",
                                }));
                                setBulkFetchPagination((prev) => ({
                                  ...prev,
                                  currentPage: 1,
                                }));
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
                            placeholder="Grup Kodu ara..."
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
                          />
                          {bulkFetchFilters.grupKod && (
                            <button
                              className="btn btn-outline-secondary"
                              onClick={() => {
                                setBulkFetchFilters((prev) => ({
                                  ...prev,
                                  grupKod: "",
                                }));
                                setBulkFetchPagination((prev) => ({
                                  ...prev,
                                  currentPage: 1,
                                }));
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
                              setBulkFetchFilters((prev) => ({
                                ...prev,
                                stokDurumu: e.target.value,
                              }));
                              setBulkFetchPagination((prev) => ({
                                ...prev,
                                currentPage: 1,
                              }));
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
                          />
                        </div>
                      </div>

                      <div className="col-md-3 d-flex gap-2">
                        <button
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => {
                            setBulkFetchFilters({
                              stokKod: "",
                              grupKod: "",
                              stokDurumu: "hepsi",
                              minStok: "",
                            });
                            setBulkFetchPagination((prev) => ({
                              ...prev,
                              currentPage: 1,
                            }));
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
                              <td colSpan="9" className="text-center py-5 text-muted">
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
                                  {((bulkFetchPagination.currentPage || 1) - 1) *
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
                                <td className="py-2">{product.birim || "-"}</td>
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
                          {filteredBulkProducts.length !== bulkFetchProducts.length ? (
                            <>
                              {filteredBulkProducts.length} sonuçtan{" "}
                              {filteredBulkProducts.length > 0
                                ? Math.min(
                                    ((bulkFetchPagination.currentPage || 1) - 1) *
                                      (bulkFetchPagination.pageSize || 50) +
                                      1,
                                    filteredBulkProducts.length,
                                  )
                                : 0}
                              -
                              {Math.min(
                                (bulkFetchPagination.currentPage || 1) *
                                  (bulkFetchPagination.pageSize || 50),
                                filteredBulkProducts.length,
                              )}{" "}
                              arası gösteriliyor (toplam{" "}
                              {bulkFetchProducts.length} ürün)
                            </>
                          ) : bulkFetchProducts.length > 0 ? (
                            <>
                              Toplam {bulkFetchProducts.length} üründen{" "}
                              {Math.max(1, ((bulkFetchPagination.currentPage || 1) - 1) *
                                (bulkFetchPagination.pageSize || 50) +
                                1)}
                              -
                              {Math.min(
                                (bulkFetchPagination.currentPage || 1) *
                                  (bulkFetchPagination.pageSize || 50),
                                bulkFetchProducts.length,
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
                      connectionStatus?.isConnected ? "Bağlı" : "Bağlantı Yok"
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
                        ? new Date(connectionStatus.timestamp).toLocaleString(
                            "tr-TR",
                          )
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
      )}
    </div>
  );
}
