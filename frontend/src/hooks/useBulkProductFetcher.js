// src/hooks/useBulkProductFetcher.js
// ============================================================
// TOPLU ÜRÜN ÇEKİCİ HOOK'U
// ============================================================
// Bu hook, bulkProductFetcher servisini React bileşenlerinde
// kullanmayı kolaylaştırır. İlerleme durumu, hata yönetimi ve
// iptal işlevselliği sağlar.
// ============================================================

import { useState, useCallback, useRef, useEffect } from "react";
import {
  fetchAllProductsSequential,
  productsToCSV,
  downloadCSV,
} from "../services/bulkProductFetcher";

/**
 * @typedef {Object} FetchState
 * @property {boolean} isLoading - Yükleme devam ediyor mu
 * @property {boolean} isComplete - İşlem tamamlandı mı
 * @property {Array} products - Çekilen ürünler
 * @property {number} progress - İlerleme yüzdesi (0-100)
 * @property {Object|null} progressInfo - Detaylı ilerleme bilgisi
 * @property {string|null} error - Hata mesajı
 * @property {Object|null} stats - İstatistikler
 */

/**
 * Toplu ürün çekme hook'u
 * @param {Object} options - Konfigürasyon seçenekleri
 * @returns {Object} State ve aksiyonlar
 */
const useBulkProductFetcher = (options = {}) => {
  // ============================================================
  // STATE YÖNETİMİ
  // ============================================================
  const [state, setState] = useState({
    isLoading: false,
    isComplete: false,
    products: [],
    progress: 0,
    progressInfo: null,
    error: null,
    stats: null,
    failedPages: [],
  });

  // AbortController referansı (iptal için)
  const abortControllerRef = useRef(null);

  // Unmount kontrolü
  const isMountedRef = useRef(true);

  useEffect(() => {
    isMountedRef.current = true;
    return () => {
      isMountedRef.current = false;
      // Unmount olduğunda aktif işlemi iptal et
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, []);

  // ============================================================
  // SAFE STATE UPDATE (Unmount sonrası güncelleme önleme)
  // ============================================================
  const safeSetState = useCallback((updater) => {
    if (isMountedRef.current) {
      setState(updater);
    }
  }, []);

  // ============================================================
  // FETCH İŞLEMİNİ BAŞLAT
  // ============================================================
  const startFetch = useCallback(
    async (fetchOptions = {}) => {
      // Zaten çalışıyorsa durdur
      if (state.isLoading) {
        console.warn("⚠️ Zaten bir çekme işlemi devam ediyor.");
        return null;
      }

      // Yeni AbortController oluştur
      abortControllerRef.current = new AbortController();

      // State'i sıfırla ve loading başlat
      safeSetState((prev) => ({
        ...prev,
        isLoading: true,
        isComplete: false,
        products: [],
        progress: 0,
        progressInfo: null,
        error: null,
        stats: null,
        failedPages: [],
      }));

      try {
        // Toplu fetch başlat
        const result = await fetchAllProductsSequential({
          ...options,
          ...fetchOptions,
          signal: abortControllerRef.current.signal,

          // İlerleme callback'i
          onProgress: (info) => {
            safeSetState((prev) => ({
              ...prev,
              progress: info.percentage,
              progressInfo: info,
            }));

            // Kullanıcının callback'ini de çağır
            if (fetchOptions.onProgress) {
              fetchOptions.onProgress(info);
            }
          },

          // Hata callback'i
          onError: (err) => {
            console.warn(`Sayfa ${err.page} hatası:`, err.error);
            if (fetchOptions.onError) {
              fetchOptions.onError(err);
            }
          },

          // Sayfa tamamlandı callback'i
          onPageComplete: (info) => {
            // Ürünleri gerçek zamanlı olarak state'e ekle (isteğe bağlı)
            if (fetchOptions.onPageComplete) {
              fetchOptions.onPageComplete(info);
            }
          },
        });

        // Başarılı sonuç
        safeSetState((prev) => ({
          ...prev,
          isLoading: false,
          isComplete: true,
          products: result.products,
          progress: 100,
          stats: result.stats,
          failedPages: result.failedPages,
          error:
            result.failedPages.length > 0
              ? `${result.failedPages.length} sayfa yüklenemedi`
              : null,
        }));

        return result;
      } catch (err) {
        // İptal edildi mi kontrol et
        if (err.name === "AbortError" || err.message === "canceled") {
          safeSetState((prev) => ({
            ...prev,
            isLoading: false,
            isComplete: false,
            error: "İşlem iptal edildi",
          }));
          return null;
        }

        // Gerçek hata
        const errorMessage =
          err.response?.data?.message || err.message || "Bilinmeyen hata";
        safeSetState((prev) => ({
          ...prev,
          isLoading: false,
          isComplete: false,
          error: errorMessage,
        }));

        throw err;
      }
    },
    [state.isLoading, options, safeSetState],
  );

  // ============================================================
  // İŞLEMİ İPTAL ET
  // ============================================================
  const cancelFetch = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      safeSetState((prev) => ({
        ...prev,
        isLoading: false,
        error: "İşlem iptal edildi",
      }));
      console.log("🛑 Toplu çekme işlemi iptal edildi.");
    }
  }, [safeSetState]);

  // ============================================================
  // STATE'İ SIFIRLA
  // ============================================================
  const reset = useCallback(() => {
    cancelFetch();
    safeSetState({
      isLoading: false,
      isComplete: false,
      products: [],
      progress: 0,
      progressInfo: null,
      error: null,
      stats: null,
      failedPages: [],
    });
  }, [cancelFetch, safeSetState]);

  // ============================================================
  // CSV EXPORT
  // ============================================================
  const exportToCSV = useCallback(
    (filename = "urunler.csv") => {
      if (state.products.length === 0) {
        console.warn("⚠️ Export edilecek ürün yok.");
        return false;
      }

      const csvContent = productsToCSV(state.products);
      downloadCSV(csvContent, filename);

      console.log(
        `📥 ${state.products.length} ürün CSV olarak indirildi: ${filename}`,
      );
      return true;
    },
    [state.products],
  );

  // ============================================================
  // DÖNDÜRÜLEN DEĞERLER
  // ============================================================
  return {
    // State
    ...state,

    // Hesaplanmış değerler
    productCount: state.products.length,
    hasError: !!state.error,
    canExport: state.products.length > 0,

    // Aksiyonlar
    startFetch,
    cancelFetch,
    reset,
    exportToCSV,
  };
};

export default useBulkProductFetcher;
