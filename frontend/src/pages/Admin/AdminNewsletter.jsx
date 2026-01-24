// =============================================================================
// AdminNewsletter.jsx - Bülten Yönetimi Sayfası
// =============================================================================
// Bu sayfa admin panelinde bülten abonelerini yönetmek ve toplu e-posta
// göndermek için kullanılır.
//
// Özellikler:
// - Abone listesi görüntüleme (sayfalama, arama, filtreleme)
// - Toplu e-posta gönderimi (HTML editör ile)
// - Test e-postası gönderimi
// - İstatistik kartları
// - GDPR uyumlu abone silme
// =============================================================================

import { useState, useEffect, useCallback, useMemo } from "react";
import { useAuth } from "../../contexts/AuthContext";
import { PERMISSIONS } from "../../services/permissionService";
import newsletterService from "../../services/newsletterService";
import "./styles/AdminNewsletter.css";

// =============================================================================
// Ana Component
// =============================================================================
export default function AdminNewsletter() {
  // =========================================================================
  // STATE YÖNETİMİ
  // =========================================================================
  const { hasPermission, user } = useAuth();
  const isSuperAdmin = user?.role === "SuperAdmin";

  // Abone listesi state
  const [subscribers, setSubscribers] = useState([]);
  const [stats, setStats] = useState({
    totalSubscribers: 0,
    activeSubscribers: 0,
    newThisMonth: 0,
    newThisWeek: 0,
  });
  const [loading, setLoading] = useState(true);
  const [statsLoading, setStatsLoading] = useState(true);
  const [error, setError] = useState(null);

  // Sayfalama state
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  // Arama ve filtreleme state
  const [searchTerm, setSearchTerm] = useState("");
  const [isActiveFilter, setIsActiveFilter] = useState(null);

  // E-posta gönderim state
  const [showEmailModal, setShowEmailModal] = useState(false);
  const [emailSubject, setEmailSubject] = useState("");
  const [emailBody, setEmailBody] = useState("");
  const [testEmail, setTestEmail] = useState("");
  const [sendingEmail, setSendingEmail] = useState(false);
  const [sendingTestEmail, setSendingTestEmail] = useState(false);

  // Silme modalı state
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [subscriberToDelete, setSubscriberToDelete] = useState(null);
  const [deleting, setDeleting] = useState(false);

  // Toast mesajları
  const [toast, setToast] = useState({
    show: false,
    message: "",
    type: "success",
  });

  // =========================================================================
  // İZİN KONTROL
  // =========================================================================
  const canView = useMemo(
    () => isSuperAdmin || hasPermission?.(PERMISSIONS.NEWSLETTER_VIEW),
    [isSuperAdmin, hasPermission],
  );

  const canSend = useMemo(
    () => isSuperAdmin || hasPermission?.(PERMISSIONS.NEWSLETTER_SEND),
    [isSuperAdmin, hasPermission],
  );

  const canDelete = useMemo(
    () => isSuperAdmin || hasPermission?.(PERMISSIONS.NEWSLETTER_DELETE),
    [isSuperAdmin, hasPermission],
  );

  const canViewStats = useMemo(
    () => isSuperAdmin || hasPermission?.(PERMISSIONS.NEWSLETTER_STATS),
    [isSuperAdmin, hasPermission],
  );

  // =========================================================================
  // VERİ ÇEKME FONKSİYONLARI
  // =========================================================================

  /**
   * Abone listesini API'den çeker
   */
  const loadSubscribers = useCallback(async () => {
    if (!canView) return;

    try {
      setLoading(true);
      setError(null);

      const response = await newsletterService.getSubscribers({
        page: currentPage,
        pageSize,
        search: searchTerm || undefined,
        isActive: isActiveFilter,
      });

      const items = response?.items || response?.Items || response?.data || [];
      const totalPages =
        response?.totalPages || response?.TotalPages || response?.totalPages || 1;
      const totalCount =
        response?.totalCount ||
        response?.TotalCount ||
        response?.totalCount ||
        items.length ||
        0;

      if (Array.isArray(items)) {
        setSubscribers(items);
        setTotalPages(totalPages);
        setTotalCount(totalCount);
      } else {
        throw new Error("Aboneler yüklenemedi");
      }
    } catch (err) {
      console.error("[AdminNewsletter] loadSubscribers error:", err);
      setError(err.message || "Aboneler yüklenirken bir hata oluştu");
      setSubscribers([]);
    } finally {
      setLoading(false);
    }
  }, [canView, currentPage, pageSize, searchTerm, isActiveFilter]);

  /**
   * İstatistikleri API'den çeker
   */
  const loadStats = useCallback(async () => {
    if (!canViewStats) return;

    try {
      setStatsLoading(true);

      const response = await newsletterService.getStatistics();
      const totalSubscribers =
        response?.totalSubscribers || response?.TotalSubscribers || 0;
      const activeSubscribers =
        response?.activeSubscribers || response?.ActiveSubscribers || 0;
      const newThisWeek =
        response?.newSubscribersLast7Days ||
        response?.NewSubscribersLast7Days ||
        0;
      const newThisMonth =
        response?.newSubscribersLast30Days ||
        response?.NewSubscribersLast30Days ||
        0;

      setStats({
        totalSubscribers,
        activeSubscribers,
        newThisMonth,
        newThisWeek,
      });
    } catch (err) {
      console.error("[AdminNewsletter] loadStats error:", err);
      // İstatistik hatası kritik değil, sadece log
    } finally {
      setStatsLoading(false);
    }
  }, [canViewStats]);

  // =========================================================================
  // İLK YÜKLEME VE BAĞIMLILIK DEĞİŞİKLİKLERİ
  // =========================================================================
  useEffect(() => {
    loadSubscribers();
  }, [loadSubscribers]);

  useEffect(() => {
    loadStats();
  }, [loadStats]);

  // =========================================================================
  // TOAST MESAJI GÖSTERME
  // =========================================================================
  const showToast = useCallback((message, type = "success") => {
    setToast({ show: true, message, type });
    setTimeout(
      () => setToast({ show: false, message: "", type: "success" }),
      5000,
    );
  }, []);

  // =========================================================================
  // ABONE SİLME
  // =========================================================================
  const handleDeleteClick = useCallback((subscriber) => {
    setSubscriberToDelete(subscriber);
    setShowDeleteModal(true);
  }, []);

  const handleDeleteConfirm = useCallback(async () => {
    if (!subscriberToDelete) return;

    try {
      setDeleting(true);

      await newsletterService.deleteSubscriber(subscriberToDelete.id);
      showToast(
        `"${subscriberToDelete.email}" abonelikten kaldırıldı`,
        "success",
      );
      loadSubscribers();
      loadStats();
    } catch (err) {
      console.error("[AdminNewsletter] delete error:", err);
      showToast(
        err.message || "Silme işlemi sırasında bir hata oluştu",
        "danger",
      );
    } finally {
      setDeleting(false);
      setShowDeleteModal(false);
      setSubscriberToDelete(null);
    }
  }, [subscriberToDelete, loadSubscribers, loadStats, showToast]);

  // =========================================================================
  // E-POSTA GÖNDERİMİ
  // =========================================================================
  const handleSendEmail = useCallback(async () => {
    if (!emailSubject.trim() || !emailBody.trim()) {
      showToast("Lütfen konu ve içerik alanlarını doldurun", "warning");
      return;
    }

    if (emailSubject.trim().length < 3) {
      showToast("E-posta konusu en az 3 karakter olmalıdır", "warning");
      return;
    }

    if (emailBody.trim().length < 10) {
      showToast("E-posta içeriği en az 10 karakter olmalıdır", "warning");
      return;
    }

    try {
      setSendingEmail(true);

      const response = await newsletterService.sendBulkEmail({
        subject: emailSubject.trim(),
        body: emailBody,
        isHtml: true,
      });

      if (response?.success || response?.Success) {
        const queuedCount = response?.queuedCount || response?.QueuedCount || 0;
        const totalSubscribers =
          response?.totalSubscribers || response?.TotalSubscribers || 0;
        const failedCount = response?.failedCount || response?.FailedCount || 0;

        if (failedCount > 0) {
          showToast(
            `E-posta gönderildi: ${queuedCount} başarılı, ${failedCount} başarısız`,
            "warning",
          );
        } else {
          showToast(
            `E-posta ${totalSubscribers || queuedCount} aboneye başarıyla gönderildi`,
            "success",
          );
        }

        // Modal'ı kapat ve formu temizle
        setShowEmailModal(false);
        setEmailSubject("");
        setEmailBody("");
      } else {
        throw new Error(response?.message || "E-posta gönderilemedi");
      }
    } catch (err) {
      console.error("[AdminNewsletter] sendEmail error:", err);
      showToast(
        err.message || "E-posta gönderilirken bir hata oluştu",
        "danger",
      );
    } finally {
      setSendingEmail(false);
    }
  }, [emailSubject, emailBody, showToast]);

  /**
   * Test e-postası gönderir
   */
  const handleSendTestEmail = useCallback(async () => {
    if (!emailSubject.trim() || !emailBody.trim()) {
      showToast("Lütfen konu ve içerik alanlarını doldurun", "warning");
      return;
    }

    if (emailSubject.trim().length < 3) {
      showToast("E-posta konusu en az 3 karakter olmalıdır", "warning");
      return;
    }

    if (emailBody.trim().length < 10) {
      showToast("E-posta içeriği en az 10 karakter olmalıdır", "warning");
      return;
    }

    if (!testEmail.trim()) {
      showToast("Lütfen test e-posta adresini girin", "warning");
      return;
    }

    try {
      setSendingTestEmail(true);

      const response = await newsletterService.sendTestEmail({
        subject: emailSubject.trim(),
        body: emailBody,
        testEmails: [testEmail.trim()],
        isHtml: true,
      });

      if (response?.success || response?.Success) {
        showToast(`Test e-postası ${testEmail} adresine gönderildi`, "success");
      } else {
        throw new Error(response?.message || "Test e-postası gönderilemedi");
      }
    } catch (err) {
      console.error("[AdminNewsletter] sendTestEmail error:", err);
      showToast(
        err.message || "Test e-postası gönderilirken bir hata oluştu",
        "danger",
      );
    } finally {
      setSendingTestEmail(false);
    }
  }, [emailSubject, emailBody, testEmail, showToast]);

  // =========================================================================
  // ARAMA VE FİLTRELEME
  // =========================================================================
  const handleSearch = useCallback((e) => {
    e.preventDefault();
    setCurrentPage(1); // Aramada ilk sayfaya dön
  }, []);

  const handleClearFilters = useCallback(() => {
    setSearchTerm("");
    setIsActiveFilter(null);
    setCurrentPage(1);
  }, []);

  // =========================================================================
  // SAYFALAMA
  // =========================================================================
  const handlePageChange = useCallback(
    (newPage) => {
      if (newPage >= 1 && newPage <= totalPages) {
        setCurrentPage(newPage);
      }
    },
    [totalPages],
  );

  // =========================================================================
  // RENDER: İzin Yok
  // =========================================================================
  if (!canView) {
    return (
      <div className="container-fluid py-4">
        <div className="alert alert-warning">
          <i className="fas fa-exclamation-triangle me-2"></i>
          Bu sayfayı görüntüleme yetkiniz bulunmamaktadır.
        </div>
      </div>
    );
  }

  // =========================================================================
  // RENDER: Hata
  // =========================================================================
  if (error && subscribers.length === 0) {
    return (
      <div className="container-fluid py-4">
        <div className="alert alert-danger">
          <i className="fas fa-exclamation-circle me-2"></i>
          {error}
          <button className="btn btn-link" onClick={loadSubscribers}>
            Tekrar Dene
          </button>
        </div>
      </div>
    );
  }

  // =========================================================================
  // RENDER: Ana Sayfa
  // =========================================================================
  return (
    <div className="container-fluid py-4 admin-newsletter">
      {/* ===================================================================
          BAŞLIK
          =================================================================== */}
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h3 mb-1">
            <i className="fas fa-envelope-open-text me-2 text-primary"></i>
            Bülten Yönetimi
          </h1>
          <p className="text-muted mb-0">
            Bülten abonelerini yönetin ve toplu e-posta gönderin
          </p>
        </div>

        {canSend && (
          <button
            className="btn btn-primary"
            onClick={() => setShowEmailModal(true)}
            disabled={stats.activeSubscribers === 0}
          >
            <i className="fas fa-paper-plane me-2"></i>
            Toplu E-posta Gönder
          </button>
        )}
      </div>

      {/* ===================================================================
          İSTATİSTİK KARTLARI
          =================================================================== */}
      {canViewStats && (
        <div className="row mb-4">
          <div className="col-md-3 col-sm-6 mb-3">
            <div className="card stat-card border-0 shadow-sm h-100">
              <div className="card-body">
                <div className="d-flex align-items-center">
                  <div className="stat-icon bg-primary bg-opacity-10 text-primary">
                    <i className="fas fa-users"></i>
                  </div>
                  <div className="ms-3">
                    <h6 className="mb-0 text-muted">Toplam Abone</h6>
                    <h3 className="mb-0">
                      {statsLoading ? (
                        <span className="placeholder-glow">
                          <span className="placeholder col-6"></span>
                        </span>
                      ) : (
                        stats.totalSubscribers.toLocaleString("tr-TR")
                      )}
                    </h3>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="col-md-3 col-sm-6 mb-3">
            <div className="card stat-card border-0 shadow-sm h-100">
              <div className="card-body">
                <div className="d-flex align-items-center">
                  <div className="stat-icon bg-success bg-opacity-10 text-success">
                    <i className="fas fa-check-circle"></i>
                  </div>
                  <div className="ms-3">
                    <h6 className="mb-0 text-muted">Aktif Abone</h6>
                    <h3 className="mb-0">
                      {statsLoading ? (
                        <span className="placeholder-glow">
                          <span className="placeholder col-6"></span>
                        </span>
                      ) : (
                        stats.activeSubscribers.toLocaleString("tr-TR")
                      )}
                    </h3>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="col-md-3 col-sm-6 mb-3">
            <div className="card stat-card border-0 shadow-sm h-100">
              <div className="card-body">
                <div className="d-flex align-items-center">
                  <div className="stat-icon bg-info bg-opacity-10 text-info">
                    <i className="fas fa-calendar-alt"></i>
                  </div>
                  <div className="ms-3">
                    <h6 className="mb-0 text-muted">Bu Ay</h6>
                    <h3 className="mb-0">
                      {statsLoading ? (
                        <span className="placeholder-glow">
                          <span className="placeholder col-6"></span>
                        </span>
                      ) : (
                        `+${stats.newThisMonth.toLocaleString("tr-TR")}`
                      )}
                    </h3>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="col-md-3 col-sm-6 mb-3">
            <div className="card stat-card border-0 shadow-sm h-100">
              <div className="card-body">
                <div className="d-flex align-items-center">
                  <div className="stat-icon bg-warning bg-opacity-10 text-warning">
                    <i className="fas fa-calendar-week"></i>
                  </div>
                  <div className="ms-3">
                    <h6 className="mb-0 text-muted">Bu Hafta</h6>
                    <h3 className="mb-0">
                      {statsLoading ? (
                        <span className="placeholder-glow">
                          <span className="placeholder col-6"></span>
                        </span>
                      ) : (
                        `+${stats.newThisWeek.toLocaleString("tr-TR")}`
                      )}
                    </h3>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ===================================================================
          ARAMA VE FİLTRE
          =================================================================== */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body">
          <form onSubmit={handleSearch}>
            <div className="row g-3 align-items-end">
              <div className="col-md-5">
                <label className="form-label">Arama</label>
                <div className="input-group">
                  <span className="input-group-text bg-white">
                    <i className="fas fa-search text-muted"></i>
                  </span>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="E-posta veya ad ile ara..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                  />
                </div>
              </div>

              <div className="col-md-3">
                <label className="form-label">Durum</label>
                <select
                  className="form-select"
                  value={
                    isActiveFilter === null ? "" : isActiveFilter.toString()
                  }
                  onChange={(e) => {
                    const val = e.target.value;
                    setIsActiveFilter(val === "" ? null : val === "true");
                    setCurrentPage(1);
                  }}
                >
                  <option value="">Tümü</option>
                  <option value="true">Aktif</option>
                  <option value="false">Pasif</option>
                </select>
              </div>

              <div className="col-md-4">
                <div className="d-flex gap-2">
                  <button type="submit" className="btn btn-primary">
                    <i className="fas fa-search me-1"></i>
                    Ara
                  </button>
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={handleClearFilters}
                  >
                    <i className="fas fa-times me-1"></i>
                    Temizle
                  </button>
                </div>
              </div>
            </div>
          </form>
        </div>
      </div>

      {/* ===================================================================
          ABONE LİSTESİ
          =================================================================== */}
      <div className="card border-0 shadow-sm">
        <div className="card-header bg-white py-3">
          <div className="d-flex justify-content-between align-items-center">
            <h5 className="mb-0">
              <i className="fas fa-list me-2"></i>
              Abone Listesi
            </h5>
            <span className="badge bg-primary">
              {totalCount.toLocaleString("tr-TR")} kayıt
            </span>
          </div>
        </div>

        <div className="card-body p-0">
          {loading ? (
            <div className="text-center py-5">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Yükleniyor...</span>
              </div>
              <p className="mt-2 text-muted">Aboneler yükleniyor...</p>
            </div>
          ) : subscribers.length === 0 ? (
            <div className="text-center py-5">
              <i className="fas fa-inbox fa-3x text-muted mb-3"></i>
              <h5 className="text-muted">Abone Bulunamadı</h5>
              <p className="text-muted">
                {searchTerm || isActiveFilter !== null
                  ? "Arama kriterlerinize uygun abone bulunamadı."
                  : "Henüz bülten abonesi bulunmuyor."}
              </p>
            </div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover mb-0">
                <thead className="bg-light">
                  <tr>
                    <th>E-posta</th>
                    <th>Ad Soyad</th>
                    <th>Durum</th>
                    <th>Kaynak</th>
                    <th>Kayıt Tarihi</th>
                    {canDelete && <th className="text-end">İşlem</th>}
                  </tr>
                </thead>
                <tbody>
                  {subscribers.map((subscriber) => (
                    <tr key={subscriber.id}>
                      <td>
                        <div className="d-flex align-items-center">
                          <div className="subscriber-avatar me-2">
                            {subscriber.email?.charAt(0).toUpperCase()}
                          </div>
                          <span>{subscriber.email}</span>
                        </div>
                      </td>
                      <td>
                        {subscriber.fullName || (
                          <span className="text-muted fst-italic">
                            Belirtilmemiş
                          </span>
                        )}
                      </td>
                      <td>
                        {subscriber.isActive ? (
                          <span className="badge bg-success">
                            <i className="fas fa-check me-1"></i>
                            Aktif
                          </span>
                        ) : (
                          <span className="badge bg-secondary">
                            <i className="fas fa-times me-1"></i>
                            Pasif
                          </span>
                        )}
                      </td>
                      <td>
                        <span className="badge bg-light text-dark">
                          {getSourceLabel(subscriber.source)}
                        </span>
                      </td>
                      <td>
                        {new Date(subscriber.createdAt).toLocaleDateString(
                          "tr-TR",
                          {
                            year: "numeric",
                            month: "short",
                            day: "numeric",
                            hour: "2-digit",
                            minute: "2-digit",
                          },
                        )}
                      </td>
                      {canDelete && (
                        <td className="text-end">
                          <button
                            className="btn btn-sm btn-outline-danger"
                            onClick={() => handleDeleteClick(subscriber)}
                            title="Aboneliği Kaldır"
                          >
                            <i className="fas fa-trash-alt"></i>
                          </button>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* SAYFALAMA */}
        {totalPages > 1 && (
          <div className="card-footer bg-white">
            <nav aria-label="Sayfalama">
              <ul className="pagination pagination-sm justify-content-center mb-0">
                <li
                  className={`page-item ${currentPage === 1 ? "disabled" : ""}`}
                >
                  <button
                    className="page-link"
                    onClick={() => handlePageChange(1)}
                    disabled={currentPage === 1}
                  >
                    <i className="fas fa-angle-double-left"></i>
                  </button>
                </li>
                <li
                  className={`page-item ${currentPage === 1 ? "disabled" : ""}`}
                >
                  <button
                    className="page-link"
                    onClick={() => handlePageChange(currentPage - 1)}
                    disabled={currentPage === 1}
                  >
                    <i className="fas fa-angle-left"></i>
                  </button>
                </li>

                {/* Sayfa numaraları */}
                {(() => {
                  const pages = [];
                  const start = Math.max(1, currentPage - 2);
                  const end = Math.min(totalPages, currentPage + 2);

                  for (let i = start; i <= end; i++) {
                    pages.push(
                      <li
                        key={i}
                        className={`page-item ${currentPage === i ? "active" : ""}`}
                      >
                        <button
                          className="page-link"
                          onClick={() => handlePageChange(i)}
                        >
                          {i}
                        </button>
                      </li>,
                    );
                  }
                  return pages;
                })()}

                <li
                  className={`page-item ${currentPage === totalPages ? "disabled" : ""}`}
                >
                  <button
                    className="page-link"
                    onClick={() => handlePageChange(currentPage + 1)}
                    disabled={currentPage === totalPages}
                  >
                    <i className="fas fa-angle-right"></i>
                  </button>
                </li>
                <li
                  className={`page-item ${currentPage === totalPages ? "disabled" : ""}`}
                >
                  <button
                    className="page-link"
                    onClick={() => handlePageChange(totalPages)}
                    disabled={currentPage === totalPages}
                  >
                    <i className="fas fa-angle-double-right"></i>
                  </button>
                </li>
              </ul>
            </nav>
            <div className="text-center mt-2 text-muted small">
              Sayfa {currentPage} / {totalPages} (
              {totalCount.toLocaleString("tr-TR")} kayıt)
            </div>
          </div>
        )}
      </div>

      {/* ===================================================================
          E-POSTA GÖNDERİM MODALI
          =================================================================== */}
      {showEmailModal && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-lg modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">
                  <i className="fas fa-paper-plane me-2"></i>
                  Toplu E-posta Gönder
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setShowEmailModal(false)}
                  disabled={sendingEmail || sendingTestEmail}
                ></button>
              </div>

              <div className="modal-body">
                <div className="alert alert-info mb-3">
                  <i className="fas fa-info-circle me-2"></i>
                  <strong>
                    {stats.activeSubscribers.toLocaleString("tr-TR")}
                  </strong>{" "}
                  aktif aboneye e-posta gönderilecektir.
                </div>

                <div className="mb-3">
                  <label className="form-label">E-posta Konusu *</label>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="E-posta konusunu girin..."
                    value={emailSubject}
                    onChange={(e) => setEmailSubject(e.target.value)}
                    disabled={sendingEmail || sendingTestEmail}
                  />
                </div>

                <div className="mb-3">
                  <label className="form-label">E-posta İçeriği (HTML) *</label>
                  <textarea
                    className="form-control font-monospace"
                    rows="10"
                    placeholder="E-posta HTML içeriğini girin..."
                    value={emailBody}
                    onChange={(e) => setEmailBody(e.target.value)}
                    disabled={sendingEmail || sendingTestEmail}
                  ></textarea>
                  <div className="form-text">
                    HTML formatında içerik girebilirsiniz. Örn:
                    &lt;h1&gt;Başlık&lt;/h1&gt;&lt;p&gt;İçerik&lt;/p&gt;
                  </div>
                </div>

                <hr />

                <div className="mb-3">
                  <label className="form-label">Test E-postası Gönder</label>
                  <div className="input-group">
                    <input
                      type="email"
                      className="form-control"
                      placeholder="test@ornek.com"
                      value={testEmail}
                      onChange={(e) => setTestEmail(e.target.value)}
                      disabled={sendingEmail || sendingTestEmail}
                    />
                    <button
                      className="btn btn-outline-secondary"
                      type="button"
                      onClick={handleSendTestEmail}
                      disabled={
                        sendingEmail ||
                        sendingTestEmail ||
                        !emailSubject ||
                        !emailBody ||
                        !testEmail
                      }
                    >
                      {sendingTestEmail ? (
                        <>
                          <span className="spinner-border spinner-border-sm me-1"></span>
                          Gönderiliyor...
                        </>
                      ) : (
                        <>
                          <i className="fas fa-flask me-1"></i>
                          Test Gönder
                        </>
                      )}
                    </button>
                  </div>
                  <div className="form-text">
                    Toplu gönderim yapmadan önce test e-postası göndererek
                    içeriği kontrol edebilirsiniz.
                  </div>
                </div>
              </div>

              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowEmailModal(false)}
                  disabled={sendingEmail || sendingTestEmail}
                >
                  İptal
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleSendEmail}
                  disabled={
                    sendingEmail ||
                    sendingTestEmail ||
                    !emailSubject ||
                    !emailBody
                  }
                >
                  {sendingEmail ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-1"></span>
                      Gönderiliyor...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-paper-plane me-1"></i>
                      {stats.activeSubscribers.toLocaleString("tr-TR")} Aboneye
                      Gönder
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ===================================================================
          SİLME ONAY MODALI
          =================================================================== */}
      {showDeleteModal && subscriberToDelete && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title text-danger">
                  <i className="fas fa-exclamation-triangle me-2"></i>
                  Aboneliği Kaldır
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setShowDeleteModal(false)}
                  disabled={deleting}
                ></button>
              </div>

              <div className="modal-body">
                <p>
                  <strong>{subscriberToDelete.email}</strong> adresini bülten
                  listesinden kaldırmak istediğinizden emin misiniz?
                </p>
                <p className="text-muted mb-0">
                  Bu işlem geri alınamaz ve GDPR kapsamında abonenin tüm
                  verileri silinecektir.
                </p>
              </div>

              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowDeleteModal(false)}
                  disabled={deleting}
                >
                  İptal
                </button>
                <button
                  type="button"
                  className="btn btn-danger"
                  onClick={handleDeleteConfirm}
                  disabled={deleting}
                >
                  {deleting ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-1"></span>
                      Siliniyor...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-trash-alt me-1"></i>
                      Evet, Kaldır
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ===================================================================
          TOAST MESAJI
          =================================================================== */}
      {toast.show && (
        <div
          className={`toast show position-fixed bottom-0 end-0 m-3`}
          role="alert"
          style={{ zIndex: 1100 }}
        >
          <div className={`toast-header bg-${toast.type} text-white`}>
            <i
              className={`fas ${toast.type === "success" ? "fa-check-circle" : toast.type === "danger" ? "fa-times-circle" : "fa-exclamation-circle"} me-2`}
            ></i>
            <strong className="me-auto">
              {toast.type === "success"
                ? "Başarılı"
                : toast.type === "danger"
                  ? "Hata"
                  : "Uyarı"}
            </strong>
            <button
              type="button"
              className="btn-close btn-close-white"
              onClick={() =>
                setToast({ show: false, message: "", type: "success" })
              }
            ></button>
          </div>
          <div className="toast-body">{toast.message}</div>
        </div>
      )}
    </div>
  );
}

// =============================================================================
// YARDIMCI FONKSİYONLAR
// =============================================================================

/**
 * Kaynak (source) değerini Türkçe etikete çevirir
 */
function getSourceLabel(source) {
  const labels = {
    web_footer: "Web Sitesi (Footer)",
    web_popup: "Web Sitesi (Popup)",
    mobile_app: "Mobil Uygulama",
    checkout: "Ödeme Sayfası",
    admin_import: "Admin İçe Aktarma",
    Website: "Web Sitesi",
    Footer: "Footer Formu",
    Popup: "Popup",
    Checkout: "Ödeme Sayfası",
    API: "API",
    Import: "İçe Aktarma",
    Admin: "Admin Paneli",
  };
  return labels[source] || source || "Bilinmiyor";
}
