// src/pages/admin/AdminBulkProductExport.jsx
// ============================================================
// TOPLU ÜRÜN EXPORT SAYFASI
// ============================================================
// Bu sayfa, tüm ürünleri API'den toplu olarak çeker ve
// CSV/Excel formatında dışa aktarır. İlerleme durumu,
// iptal işlevselliği ve detaylı istatistikler gösterir.
// ============================================================

import React, { useCallback, useMemo } from "react";
import useBulkProductFetcher from "../../hooks/useBulkProductFetcher";

// ============================================================
// YARDIMCI FONKSİYONLAR
// ============================================================

/**
 * Milisaniyeyi okunabilir formata çevirir
 * @param {number} ms - Milisaniye
 * @returns {string} Formatlanmış süre
 */
const formatDuration = (ms) => {
  if (!ms || ms <= 0) return "-";

  const seconds = Math.floor(ms / 1000);
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;

  if (minutes > 0) {
    return `${minutes}dk ${remainingSeconds}sn`;
  }
  return `${seconds}sn`;
};

/**
 * Sayıyı binlik ayraçlı formata çevirir
 * @param {number} num - Sayı
 * @returns {string} Formatlanmış sayı
 */
const formatNumber = (num) => {
  if (num === undefined || num === null) return "-";
  return new Intl.NumberFormat("tr-TR").format(num);
};

// ============================================================
// ANA BİLEŞEN
// ============================================================
const AdminBulkProductExport = () => {
  // Hook'u kullan
  const {
    isLoading,
    isComplete,
    products,
    progress,
    progressInfo,
    error,
    stats,
    failedPages,
    productCount,
    hasError,
    canExport,
    startFetch,
    cancelFetch,
    reset,
    exportToCSV,
  } = useBulkProductFetcher({
    pageSize: 50,
    throttleDelayMs: 300,
    maxRetryCount: 3,
  });

  // ============================================================
  // EVENT HANDLERS
  // ============================================================

  // Çekme işlemini başlat
  const handleStartFetch = useCallback(() => {
    startFetch({
      onProgress: (info) => {
        // Konsola da yazdır (debug için)
        if (info.percentage % 10 === 0) {
          console.log(`📊 İlerleme: %${info.percentage}`);
        }
      },
    });
  }, [startFetch]);

  // CSV olarak indir
  const handleExportCSV = useCallback(() => {
    const timestamp = new Date().toISOString().split("T")[0];
    exportToCSV(`urunler_${timestamp}.csv`);
  }, [exportToCSV]);

  // ============================================================
  // HESAPLANMIŞ DEĞERLER
  // ============================================================

  // İlerleme çubuğu rengi
  const progressBarColor = useMemo(() => {
    if (hasError) return "#ef4444"; // Kırmızı
    if (isComplete) return "#22c55e"; // Yeşil
    return "#3b82f6"; // Mavi
  }, [hasError, isComplete]);

  // Durum metni
  const statusText = useMemo(() => {
    if (isLoading) {
      return progressInfo?.status || "Yükleniyor...";
    }
    if (isComplete) {
      return failedPages.length > 0
        ? `Tamamlandı (${failedPages.length} sayfa hatalı)`
        : "Tamamlandı!";
    }
    if (error) {
      return `Hata: ${error}`;
    }
    return "Başlamak için butona tıklayın";
  }, [isLoading, isComplete, error, progressInfo, failedPages]);

  // ============================================================
  // RENDER - STYLES
  // ============================================================
  const styles = {
    container: {
      padding: "24px",
      maxWidth: "900px",
      margin: "0 auto",
    },
    card: {
      backgroundColor: "#fff",
      borderRadius: "12px",
      boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
      padding: "24px",
      marginBottom: "20px",
    },
    title: {
      fontSize: "24px",
      fontWeight: "600",
      marginBottom: "8px",
      color: "#1f2937",
    },
    subtitle: {
      fontSize: "14px",
      color: "#6b7280",
      marginBottom: "24px",
    },
    progressContainer: {
      marginBottom: "24px",
    },
    progressBar: {
      width: "100%",
      height: "12px",
      backgroundColor: "#e5e7eb",
      borderRadius: "6px",
      overflow: "hidden",
    },
    progressFill: {
      height: "100%",
      backgroundColor: progressBarColor,
      borderRadius: "6px",
      transition: "width 0.3s ease",
      width: `${progress}%`,
    },
    progressText: {
      display: "flex",
      justifyContent: "space-between",
      marginTop: "8px",
      fontSize: "14px",
      color: "#6b7280",
    },
    statusText: {
      fontSize: "15px",
      fontWeight: "500",
      color: hasError ? "#ef4444" : isComplete ? "#22c55e" : "#374151",
      marginBottom: "20px",
    },
    buttonGroup: {
      display: "flex",
      gap: "12px",
      flexWrap: "wrap",
    },
    button: {
      padding: "12px 24px",
      borderRadius: "8px",
      border: "none",
      fontSize: "15px",
      fontWeight: "500",
      cursor: "pointer",
      transition: "all 0.2s",
      display: "flex",
      alignItems: "center",
      gap: "8px",
    },
    primaryButton: {
      backgroundColor: "#3b82f6",
      color: "#fff",
    },
    primaryButtonDisabled: {
      backgroundColor: "#93c5fd",
      cursor: "not-allowed",
    },
    dangerButton: {
      backgroundColor: "#ef4444",
      color: "#fff",
    },
    successButton: {
      backgroundColor: "#22c55e",
      color: "#fff",
    },
    secondaryButton: {
      backgroundColor: "#f3f4f6",
      color: "#374151",
      border: "1px solid #d1d5db",
    },
    statsGrid: {
      display: "grid",
      gridTemplateColumns: "repeat(auto-fit, minmax(150px, 1fr))",
      gap: "16px",
      marginTop: "24px",
    },
    statCard: {
      backgroundColor: "#f9fafb",
      borderRadius: "8px",
      padding: "16px",
      textAlign: "center",
    },
    statValue: {
      fontSize: "24px",
      fontWeight: "600",
      color: "#1f2937",
    },
    statLabel: {
      fontSize: "13px",
      color: "#6b7280",
      marginTop: "4px",
    },
    infoBox: {
      backgroundColor: "#eff6ff",
      border: "1px solid #bfdbfe",
      borderRadius: "8px",
      padding: "16px",
      marginBottom: "24px",
    },
    infoTitle: {
      fontSize: "14px",
      fontWeight: "600",
      color: "#1e40af",
      marginBottom: "8px",
    },
    infoText: {
      fontSize: "13px",
      color: "#3b82f6",
      lineHeight: "1.5",
    },
    errorBox: {
      backgroundColor: "#fef2f2",
      border: "1px solid #fecaca",
      borderRadius: "8px",
      padding: "16px",
      marginBottom: "24px",
    },
    errorTitle: {
      fontSize: "14px",
      fontWeight: "600",
      color: "#dc2626",
      marginBottom: "8px",
    },
    errorText: {
      fontSize: "13px",
      color: "#ef4444",
    },
  };

  // ============================================================
  // RENDER
  // ============================================================
  return (
    <div style={styles.container}>
      {/* Ana Kart */}
      <div style={styles.card}>
        <h1 style={styles.title}>📦 Toplu Ürün Export</h1>
        <p style={styles.subtitle}>
          Tüm ürünleri API'den çeker ve CSV formatında dışa aktarır. 5000+ ürün
          bile güvenle çekilir.
        </p>

        {/* Bilgi Kutusu */}
        <div style={styles.infoBox}>
          <div style={styles.infoTitle}>ℹ️ Nasıl Çalışır?</div>
          <div style={styles.infoText}>
            • Ürünler sayfa sayfa (50'şer) sıralı olarak çekilir
            <br />
            • Sunucu yüklenmemesi için istekler arası 300ms beklenir
            <br />
            • Hata durumunda otomatik 3 kez yeniden denenir
            <br />• İşlem sürerken iptal edilebilir
          </div>
        </div>

        {/* Hata Kutusu */}
        {hasError && (
          <div style={styles.errorBox}>
            <div style={styles.errorTitle}>⚠️ Hata Oluştu</div>
            <div style={styles.errorText}>{error}</div>
            {failedPages.length > 0 && (
              <div style={{ ...styles.errorText, marginTop: "8px" }}>
                Başarısız sayfalar: {failedPages.join(", ")}
              </div>
            )}
          </div>
        )}

        {/* İlerleme Çubuğu */}
        <div style={styles.progressContainer}>
          <div style={styles.progressBar}>
            <div style={styles.progressFill}></div>
          </div>
          <div style={styles.progressText}>
            <span>%{progress}</span>
            <span>{formatNumber(productCount)} ürün çekildi</span>
          </div>
        </div>

        {/* Durum Metni */}
        <div style={styles.statusText}>{statusText}</div>

        {/* Butonlar */}
        <div style={styles.buttonGroup}>
          {/* Başlat/Çekiliyor */}
          {!isLoading && !isComplete && (
            <button
              style={{ ...styles.button, ...styles.primaryButton }}
              onClick={handleStartFetch}
            >
              🚀 Ürünleri Çek
            </button>
          )}

          {/* Yükleniyor - İptal */}
          {isLoading && (
            <button
              style={{ ...styles.button, ...styles.dangerButton }}
              onClick={cancelFetch}
            >
              🛑 İptal Et
            </button>
          )}

          {/* Tamamlandı - Export */}
          {canExport && (
            <button
              style={{ ...styles.button, ...styles.successButton }}
              onClick={handleExportCSV}
            >
              📥 CSV İndir
            </button>
          )}

          {/* Sıfırla */}
          {(isComplete || hasError) && (
            <button
              style={{ ...styles.button, ...styles.secondaryButton }}
              onClick={reset}
            >
              🔄 Sıfırla
            </button>
          )}
        </div>
      </div>

      {/* İstatistikler Kartı */}
      {stats && (
        <div style={styles.card}>
          <h2 style={{ ...styles.title, fontSize: "18px" }}>
            📊 İstatistikler
          </h2>
          <div style={styles.statsGrid}>
            <div style={styles.statCard}>
              <div style={styles.statValue}>
                {formatNumber(stats.totalRequests)}
              </div>
              <div style={styles.statLabel}>Toplam İstek</div>
            </div>
            <div style={styles.statCard}>
              <div style={styles.statValue}>
                {formatNumber(stats.successfulRequests)}
              </div>
              <div style={styles.statLabel}>Başarılı</div>
            </div>
            <div style={styles.statCard}>
              <div style={styles.statValue}>
                {formatNumber(stats.failedRequests)}
              </div>
              <div style={styles.statLabel}>Başarısız</div>
            </div>
            <div style={styles.statCard}>
              <div style={styles.statValue}>{stats.retryCount}</div>
              <div style={styles.statLabel}>Retry Sayısı</div>
            </div>
            <div style={styles.statCard}>
              <div style={styles.statValue}>{stats.avgResponseTimeMs}ms</div>
              <div style={styles.statLabel}>Ort. Yanıt Süresi</div>
            </div>
            <div style={styles.statCard}>
              <div style={styles.statValue}>
                {formatDuration(progressInfo?.elapsedMs)}
              </div>
              <div style={styles.statLabel}>Toplam Süre</div>
            </div>
          </div>
        </div>
      )}

      {/* Ürün Önizleme (İlk 10) */}
      {products.length > 0 && (
        <div style={styles.card}>
          <h2 style={{ ...styles.title, fontSize: "18px" }}>
            👀 Önizleme (İlk 10 Ürün)
          </h2>
          <div style={{ overflowX: "auto", marginTop: "16px" }}>
            <table
              style={{
                width: "100%",
                borderCollapse: "collapse",
                fontSize: "14px",
              }}
            >
              <thead>
                <tr style={{ backgroundColor: "#f9fafb" }}>
                  <th
                    style={{
                      padding: "12px",
                      textAlign: "left",
                      borderBottom: "1px solid #e5e7eb",
                    }}
                  >
                    ID
                  </th>
                  <th
                    style={{
                      padding: "12px",
                      textAlign: "left",
                      borderBottom: "1px solid #e5e7eb",
                    }}
                  >
                    Ürün Adı
                  </th>
                  <th
                    style={{
                      padding: "12px",
                      textAlign: "left",
                      borderBottom: "1px solid #e5e7eb",
                    }}
                  >
                    Kategori
                  </th>
                  <th
                    style={{
                      padding: "12px",
                      textAlign: "right",
                      borderBottom: "1px solid #e5e7eb",
                    }}
                  >
                    Fiyat
                  </th>
                  <th
                    style={{
                      padding: "12px",
                      textAlign: "right",
                      borderBottom: "1px solid #e5e7eb",
                    }}
                  >
                    Stok
                  </th>
                </tr>
              </thead>
              <tbody>
                {products.slice(0, 10).map((product) => (
                  <tr key={product.id}>
                    <td
                      style={{
                        padding: "12px",
                        borderBottom: "1px solid #e5e7eb",
                      }}
                    >
                      {product.id}
                    </td>
                    <td
                      style={{
                        padding: "12px",
                        borderBottom: "1px solid #e5e7eb",
                      }}
                    >
                      {product.name?.substring(0, 40)}
                      {product.name?.length > 40 ? "..." : ""}
                    </td>
                    <td
                      style={{
                        padding: "12px",
                        borderBottom: "1px solid #e5e7eb",
                      }}
                    >
                      {product.categoryName || "-"}
                    </td>
                    <td
                      style={{
                        padding: "12px",
                        borderBottom: "1px solid #e5e7eb",
                        textAlign: "right",
                      }}
                    >
                      ₺{product.price?.toFixed(2) || "0.00"}
                    </td>
                    <td
                      style={{
                        padding: "12px",
                        borderBottom: "1px solid #e5e7eb",
                        textAlign: "right",
                      }}
                    >
                      {product.stock ?? 0}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {products.length > 10 && (
            <div
              style={{
                textAlign: "center",
                padding: "12px",
                color: "#6b7280",
                fontSize: "13px",
              }}
            >
              ... ve {formatNumber(products.length - 10)} ürün daha
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default AdminBulkProductExport;
