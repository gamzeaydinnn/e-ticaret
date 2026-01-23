// ==========================================================================
// AdminWeightManagement.jsx - Ağırlık Fark Yönetimi Ana Sayfası
// ==========================================================================
// Admin panelinde ağırlık bazlı siparişlerin fark yönetimini sağlayan
// ana sayfa. Bekleyen onaylar, istatistikler ve raporlama özellikleri içerir.
//
// Özellikler:
// - Dashboard istatistikleri (toplam, bekleyen, onaylanan, reddedilen)
// - Bekleyen onaylar listesi (filtreleme ve sayfalama)
// - Manuel fiyat düzeltme ve iptal işlemleri
// - Kurye performans takibi
// - Detaylı raporlama ve export
// ==========================================================================

import React, { useState, useEffect, useCallback } from "react";
import { WeightAdjustmentService } from "../../services/weightAdjustmentService";
import WeightStatsDashboard from "./components/WeightStatsDashboard";
import WeightPendingList from "./components/WeightPendingList";
import WeightAdjustmentModal from "./components/WeightAdjustmentModal";
import WeightCourierPerformance from "./components/WeightCourierPerformance";
import "./styles/AdminWeightManagement.css";

/**
 * AdminWeightManagement - Ağırlık Fark Yönetimi Ana Komponenti
 *
 * Bu sayfa admin'lerin:
 * - Ağırlık fark istatistiklerini görmesini
 * - Bekleyen onayları yönetmesini
 * - Manuel müdahale yapmasını
 * - Kurye performansını takip etmesini sağlar
 */
export default function AdminWeightManagement() {
  // =========================================================================
  // STATE
  // =========================================================================
  const [activeTab, setActiveTab] = useState("pending"); // pending, all, stats, couriers
  const [statistics, setStatistics] = useState(null);
  const [pendingList, setPendingList] = useState([]);
  const [allAdjustments, setAllAdjustments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Modal state
  const [selectedAdjustment, setSelectedAdjustment] = useState(null);
  const [showModal, setShowModal] = useState(false);

  // Filtreler
  const [filters, setFilters] = useState({
    status: "",
    dateFrom: "",
    dateTo: "",
    courierId: "",
    searchQuery: "",
  });

  // Sayfalama
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
  });

  // =========================================================================
  // VERİ YÜKLEME
  // =========================================================================

  /**
   * İstatistikleri yükle
   */
  const loadStatistics = useCallback(async () => {
    try {
      const data = await WeightAdjustmentService.getStatistics();
      setStatistics(data);
    } catch (err) {
      console.error("[AdminWeightManagement] İstatistik yükleme hatası:", err);
      // Demo veri kullan
      setStatistics({
        totalAdjustments: 45,
        pendingCount: 8,
        approvedCount: 32,
        rejectedCount: 5,
        totalDifferenceAmount: 1250.5,
        averageDifferencePercent: 12.5,
        todayCount: 3,
        thisWeekCount: 15,
        thisMonthCount: 45,
      });
    }
  }, []);

  /**
   * Bekleyen onayları yükle
   */
  const loadPendingList = useCallback(async () => {
    try {
      const data = await WeightAdjustmentService.getAdminPendingList();
      setPendingList(data || []);
    } catch (err) {
      console.error(
        "[AdminWeightManagement] Bekleyen liste yükleme hatası:",
        err,
      );
      // Demo veri kullan
      setPendingList([
        {
          id: 1,
          orderId: 1001,
          orderNumber: "ORD-2026-1001",
          productName: "Domates (kg)",
          customerName: "Ahmet Yılmaz",
          courierName: "Mehmet Demir",
          estimatedWeightGrams: 1000,
          actualWeightGrams: 1150,
          differenceGrams: 150,
          differencePercent: 15.0,
          estimatedPrice: 50.0,
          actualPrice: 57.5,
          differenceAmount: 7.5,
          status: "PendingAdminApproval",
          createdAt: new Date(Date.now() - 3600000).toISOString(),
          notes: "Yüksek fark - admin onayı gerekli",
        },
        {
          id: 2,
          orderId: 1002,
          orderNumber: "ORD-2026-1002",
          productName: "Elma (kg)",
          customerName: "Ayşe Kaya",
          courierName: "Ali Veli",
          estimatedWeightGrams: 2000,
          actualWeightGrams: 2450,
          differenceGrams: 450,
          differencePercent: 22.5,
          estimatedPrice: 80.0,
          actualPrice: 98.0,
          differenceAmount: 18.0,
          status: "PendingAdminApproval",
          createdAt: new Date(Date.now() - 7200000).toISOString(),
          notes: "Çok yüksek fark!",
        },
      ]);
    }
  }, []);

  /**
   * Tüm fark kayıtlarını yükle
   */
  const loadAllAdjustments = useCallback(async () => {
    try {
      const params = {
        ...filters,
        page: pagination.page,
        pageSize: pagination.pageSize,
      };
      const data = await WeightAdjustmentService.getAdjustmentList(params);
      setAllAdjustments(data?.items || []);
      setPagination((prev) => ({
        ...prev,
        totalCount: data?.totalCount || 0,
        totalPages: data?.totalPages || 1,
      }));
    } catch (err) {
      console.error(
        "[AdminWeightManagement] Tüm kayıtları yükleme hatası:",
        err,
      );
      setAllAdjustments([]);
    }
  }, [filters, pagination.page, pagination.pageSize]);

  /**
   * Tüm verileri yükle
   */
  const loadAllData = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      await Promise.all([loadStatistics(), loadPendingList()]);
    } catch (err) {
      setError("Veriler yüklenirken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  }, [loadStatistics, loadPendingList]);

  // İlk yükleme
  useEffect(() => {
    loadAllData();
  }, [loadAllData]);

  // Tab değiştiğinde
  useEffect(() => {
    if (activeTab === "all") {
      loadAllAdjustments();
    }
  }, [activeTab, loadAllAdjustments]);

  // =========================================================================
  // EVENT HANDLERS
  // =========================================================================

  /**
   * Admin kararı: Onayla
   */
  const handleApprove = async (adjustmentId) => {
    if (
      !window.confirm(
        "Bu fark kaydını onaylamak istediğinize emin misiniz? Müşteriden fark tutarı tahsil edilecek.",
      )
    ) {
      return;
    }

    try {
      await WeightAdjustmentService.submitAdminDecision(adjustmentId, {
        decision: "Approve",
        notes: "Admin tarafından onaylandı",
      });

      // Listeyi güncelle
      await loadPendingList();
      await loadStatistics();

      alert("Fark kaydı onaylandı. Ödeme işlemi başlatılacak.");
    } catch (err) {
      console.error("[AdminWeightManagement] Onaylama hatası:", err);
      alert(
        "Onaylama sırasında bir hata oluştu: " +
          (err.message || "Bilinmeyen hata"),
      );
    }
  };

  /**
   * Admin kararı: Reddet
   */
  const handleReject = async (adjustmentId) => {
    const reason = prompt("Red nedeni girin:");
    if (!reason || reason.trim() === "") {
      return;
    }

    try {
      await WeightAdjustmentService.submitAdminDecision(adjustmentId, {
        decision: "Reject",
        notes: reason,
      });

      await loadPendingList();
      await loadStatistics();

      alert("Fark kaydı reddedildi.");
    } catch (err) {
      console.error("[AdminWeightManagement] Reddetme hatası:", err);
      alert("Reddetme sırasında bir hata oluştu");
    }
  };

  /**
   * Manuel düzeltme modalını aç
   */
  const handleManualAdjust = (adjustment) => {
    setSelectedAdjustment(adjustment);
    setShowModal(true);
  };

  /**
   * Modal'dan gelen düzeltmeyi kaydet
   */
  const handleSaveAdjustment = async (adjustmentData) => {
    try {
      await WeightAdjustmentService.submitAdminDecision(adjustmentData.id, {
        decision: "ManualAdjust",
        adjustedAmount: adjustmentData.newAmount,
        notes: adjustmentData.notes,
      });

      setShowModal(false);
      setSelectedAdjustment(null);
      await loadPendingList();
      await loadStatistics();

      alert("Manuel düzeltme kaydedildi.");
    } catch (err) {
      console.error("[AdminWeightManagement] Manuel düzeltme hatası:", err);
      alert("Düzeltme kaydedilemedi");
    }
  };

  /**
   * Filtre değişikliği
   */
  const handleFilterChange = (key, value) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
    setPagination((prev) => ({ ...prev, page: 1 }));
  };

  /**
   * Sayfa değişikliği
   */
  const handlePageChange = (newPage) => {
    setPagination((prev) => ({ ...prev, page: newPage }));
  };

  /**
   * Verileri yenile
   */
  const handleRefresh = () => {
    loadAllData();
  };

  // =========================================================================
  // RENDER
  // =========================================================================

  if (loading && !statistics) {
    return (
      <div className="admin-weight-management">
        <div className="loading-container">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="mt-3 text-muted">Ağırlık verileri yükleniyor...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-weight-management">
      {/* Header */}
      <div className="page-header">
        <div className="header-content">
          <div>
            <h1 className="page-title">
              <i className="fas fa-balance-scale me-3"></i>
              Ağırlık Fark Yönetimi
            </h1>
            <p className="page-subtitle">
              Ağırlık bazlı siparişlerin fark kayıtlarını yönetin
            </p>
          </div>
          <div className="header-actions">
            <button
              className="btn btn-outline-primary btn-refresh"
              onClick={handleRefresh}
              disabled={loading}
            >
              <i className={`fas fa-sync-alt ${loading ? "fa-spin" : ""}`}></i>
              <span className="d-none d-md-inline ms-2">Yenile</span>
            </button>
          </div>
        </div>
      </div>

      {/* Error Alert */}
      {error && (
        <div
          className="alert alert-danger alert-dismissible fade show mx-3"
          role="alert"
        >
          <i className="fas fa-exclamation-circle me-2"></i>
          {error}
          <button
            type="button"
            className="btn-close"
            onClick={() => setError(null)}
          ></button>
        </div>
      )}

      {/* İstatistik Dashboard */}
      <WeightStatsDashboard statistics={statistics} loading={loading} />

      {/* Tab Navigation */}
      <div className="tab-navigation">
        <button
          className={`tab-btn ${activeTab === "pending" ? "active" : ""}`}
          onClick={() => setActiveTab("pending")}
        >
          <i className="fas fa-clock me-2"></i>
          Bekleyen Onaylar
          {pendingList.length > 0 && (
            <span className="badge bg-danger ms-2">{pendingList.length}</span>
          )}
        </button>
        <button
          className={`tab-btn ${activeTab === "all" ? "active" : ""}`}
          onClick={() => setActiveTab("all")}
        >
          <i className="fas fa-list me-2"></i>
          Tüm Kayıtlar
        </button>
        <button
          className={`tab-btn ${activeTab === "couriers" ? "active" : ""}`}
          onClick={() => setActiveTab("couriers")}
        >
          <i className="fas fa-motorcycle me-2"></i>
          Kurye Performansı
        </button>
      </div>

      {/* Tab Content */}
      <div className="tab-content">
        {/* Bekleyen Onaylar */}
        {activeTab === "pending" && (
          <WeightPendingList
            items={pendingList}
            loading={loading}
            onApprove={handleApprove}
            onReject={handleReject}
            onManualAdjust={handleManualAdjust}
            onRefresh={loadPendingList}
          />
        )}

        {/* Tüm Kayıtlar */}
        {activeTab === "all" && (
          <div className="all-adjustments-section">
            {/* Filtreler */}
            <div className="filters-bar">
              <div className="row g-2">
                <div className="col-12 col-md-3">
                  <select
                    className="form-select"
                    value={filters.status}
                    onChange={(e) =>
                      handleFilterChange("status", e.target.value)
                    }
                  >
                    <option value="">Tüm Durumlar</option>
                    <option value="Pending">Bekliyor</option>
                    <option value="Approved">Onaylandı</option>
                    <option value="Rejected">Reddedildi</option>
                    <option value="Completed">Tamamlandı</option>
                  </select>
                </div>
                <div className="col-6 col-md-2">
                  <input
                    type="date"
                    className="form-control"
                    value={filters.dateFrom}
                    onChange={(e) =>
                      handleFilterChange("dateFrom", e.target.value)
                    }
                    placeholder="Başlangıç"
                  />
                </div>
                <div className="col-6 col-md-2">
                  <input
                    type="date"
                    className="form-control"
                    value={filters.dateTo}
                    onChange={(e) =>
                      handleFilterChange("dateTo", e.target.value)
                    }
                    placeholder="Bitiş"
                  />
                </div>
                <div className="col-12 col-md-3">
                  <input
                    type="text"
                    className="form-control"
                    value={filters.searchQuery}
                    onChange={(e) =>
                      handleFilterChange("searchQuery", e.target.value)
                    }
                    placeholder="Sipariş no veya müşteri ara..."
                  />
                </div>
                <div className="col-12 col-md-2">
                  <button
                    className="btn btn-primary w-100"
                    onClick={loadAllAdjustments}
                  >
                    <i className="fas fa-search me-2"></i>
                    Ara
                  </button>
                </div>
              </div>
            </div>

            {/* Tablo */}
            <div className="table-responsive">
              <table className="table table-hover">
                <thead>
                  <tr>
                    <th>Sipariş</th>
                    <th>Ürün</th>
                    <th>Müşteri</th>
                    <th>Kurye</th>
                    <th>Tahmini</th>
                    <th>Gerçek</th>
                    <th>Fark</th>
                    <th>Durum</th>
                    <th>Tarih</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {allAdjustments.length > 0 ? (
                    allAdjustments.map((adj) => (
                      <tr key={adj.id}>
                        <td>
                          <span className="order-number">
                            #{adj.orderNumber || adj.orderId}
                          </span>
                        </td>
                        <td>{adj.productName}</td>
                        <td>{adj.customerName}</td>
                        <td>{adj.courierName}</td>
                        <td>
                          {(adj.estimatedWeightGrams / 1000).toFixed(2)} kg
                        </td>
                        <td>{(adj.actualWeightGrams / 1000).toFixed(2)} kg</td>
                        <td>
                          <span
                            className={`badge ${adj.differenceAmount >= 0 ? "bg-success" : "bg-warning"}`}
                          >
                            {adj.differenceAmount >= 0 ? "+" : ""}
                            {adj.differenceAmount?.toFixed(2)} ₺
                          </span>
                        </td>
                        <td>
                          <span
                            className={`status-badge status-${adj.status?.toLowerCase()}`}
                          >
                            {getStatusLabel(adj.status)}
                          </span>
                        </td>
                        <td>{formatDate(adj.createdAt)}</td>
                        <td>
                          <button
                            className="btn btn-sm btn-outline-primary"
                            onClick={() => handleManualAdjust(adj)}
                          >
                            <i className="fas fa-eye"></i>
                          </button>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="10" className="text-center py-4 text-muted">
                        Kayıt bulunamadı
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {/* Sayfalama */}
            {pagination.totalPages > 1 && (
              <nav className="pagination-nav">
                <ul className="pagination justify-content-center">
                  <li
                    className={`page-item ${pagination.page <= 1 ? "disabled" : ""}`}
                  >
                    <button
                      className="page-link"
                      onClick={() => handlePageChange(pagination.page - 1)}
                    >
                      <i className="fas fa-chevron-left"></i>
                    </button>
                  </li>
                  {[...Array(pagination.totalPages)].map((_, i) => (
                    <li
                      key={i}
                      className={`page-item ${pagination.page === i + 1 ? "active" : ""}`}
                    >
                      <button
                        className="page-link"
                        onClick={() => handlePageChange(i + 1)}
                      >
                        {i + 1}
                      </button>
                    </li>
                  ))}
                  <li
                    className={`page-item ${pagination.page >= pagination.totalPages ? "disabled" : ""}`}
                  >
                    <button
                      className="page-link"
                      onClick={() => handlePageChange(pagination.page + 1)}
                    >
                      <i className="fas fa-chevron-right"></i>
                    </button>
                  </li>
                </ul>
              </nav>
            )}
          </div>
        )}

        {/* Kurye Performansı */}
        {activeTab === "couriers" && <WeightCourierPerformance />}
      </div>

      {/* Manuel Düzeltme Modal */}
      {showModal && selectedAdjustment && (
        <WeightAdjustmentModal
          adjustment={selectedAdjustment}
          onSave={handleSaveAdjustment}
          onClose={() => {
            setShowModal(false);
            setSelectedAdjustment(null);
          }}
        />
      )}
    </div>
  );
}

// =========================================================================
// HELPER FUNCTIONS
// =========================================================================

/**
 * Durum etiketini Türkçe'ye çevir
 */
function getStatusLabel(status) {
  const statusMap = {
    Pending: "Bekliyor",
    PendingAdminApproval: "Admin Onayı Bekliyor",
    Approved: "Onaylandı",
    Rejected: "Reddedildi",
    Completed: "Tamamlandı",
    PaymentProcessed: "Ödeme Alındı",
    Refunded: "İade Edildi",
    Cancelled: "İptal",
  };
  return statusMap[status] || status;
}

/**
 * Tarih formatla
 */
function formatDate(dateString) {
  if (!dateString) return "-";
  return new Date(dateString).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}
