// ============================================================================
// Pagination.jsx - Sayfalama Bileşeni
// ============================================================================
// Kullanıcı listesi ve diğer tablolar için sayfalama kontrolü sağlar.
// ============================================================================

/**
 * Sayfalama bileşeni
 * @param {Object} props
 * @param {number} props.currentPage - Mevcut sayfa (1'den başlar)
 * @param {number} props.totalItems - Toplam öğe sayısı
 * @param {number} props.itemsPerPage - Sayfa başına öğe sayısı
 * @param {Function} props.onPageChange - Sayfa değişikliğinde çağrılacak callback
 * @param {number} props.maxVisiblePages - Görünür sayfa sayısı (default: 5)
 */
const Pagination = ({
  currentPage = 1,
  totalItems = 0,
  itemsPerPage = 20,
  onPageChange,
  maxVisiblePages = 5,
}) => {
  const totalPages = Math.ceil(totalItems / itemsPerPage);

  // Sayfa yoksa veya tek sayfa varsa gösterme
  if (totalPages <= 1) return null;

  // Görünür sayfa numaralarını hesapla
  const getVisiblePages = () => {
    const pages = [];
    let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

    // Başlangıç sayfasını ayarla
    if (endPage - startPage + 1 < maxVisiblePages) {
      startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    return pages;
  };

  const visiblePages = getVisiblePages();
  const startItem = (currentPage - 1) * itemsPerPage + 1;
  const endItem = Math.min(currentPage * itemsPerPage, totalItems);

  return (
    <div className="d-flex flex-column flex-md-row justify-content-between align-items-center gap-3 mt-4">
      {/* Sonuç bilgisi */}
      <div className="text-muted small">
        <span className="fw-medium">{startItem}</span>
        {" - "}
        <span className="fw-medium">{endItem}</span>
        {" / "}
        <span className="fw-medium">{totalItems}</span>
        {" sonuç gösteriliyor"}
      </div>

      {/* Sayfalama kontrolleri */}
      <nav aria-label="Sayfalama">
        <ul className="pagination pagination-sm mb-0">
          {/* İlk sayfa */}
          <li className={`page-item ${currentPage === 1 ? "disabled" : ""}`}>
            <button
              className="page-link"
              onClick={() => onPageChange?.(1)}
              disabled={currentPage === 1}
              aria-label="İlk sayfa"
              style={{ borderRadius: "8px 0 0 8px" }}
            >
              <i className="fas fa-angle-double-left"></i>
            </button>
          </li>

          {/* Önceki sayfa */}
          <li className={`page-item ${currentPage === 1 ? "disabled" : ""}`}>
            <button
              className="page-link"
              onClick={() => onPageChange?.(currentPage - 1)}
              disabled={currentPage === 1}
              aria-label="Önceki sayfa"
            >
              <i className="fas fa-angle-left"></i>
            </button>
          </li>

          {/* İlk sayfa göstergesi */}
          {visiblePages[0] > 1 && (
            <>
              <li className="page-item">
                <button className="page-link" onClick={() => onPageChange?.(1)}>
                  1
                </button>
              </li>
              {visiblePages[0] > 2 && (
                <li className="page-item disabled">
                  <span className="page-link">...</span>
                </li>
              )}
            </>
          )}

          {/* Sayfa numaraları */}
          {visiblePages.map((page) => (
            <li
              key={page}
              className={`page-item ${currentPage === page ? "active" : ""}`}
            >
              <button
                className="page-link"
                onClick={() => onPageChange?.(page)}
                style={
                  currentPage === page
                    ? {
                        background: "linear-gradient(135deg, #f97316, #fb923c)",
                        borderColor: "#f97316",
                        color: "white",
                      }
                    : {}
                }
              >
                {page}
              </button>
            </li>
          ))}

          {/* Son sayfa göstergesi */}
          {visiblePages[visiblePages.length - 1] < totalPages && (
            <>
              {visiblePages[visiblePages.length - 1] < totalPages - 1 && (
                <li className="page-item disabled">
                  <span className="page-link">...</span>
                </li>
              )}
              <li className="page-item">
                <button
                  className="page-link"
                  onClick={() => onPageChange?.(totalPages)}
                >
                  {totalPages}
                </button>
              </li>
            </>
          )}

          {/* Sonraki sayfa */}
          <li
            className={`page-item ${
              currentPage === totalPages ? "disabled" : ""
            }`}
          >
            <button
              className="page-link"
              onClick={() => onPageChange?.(currentPage + 1)}
              disabled={currentPage === totalPages}
              aria-label="Sonraki sayfa"
            >
              <i className="fas fa-angle-right"></i>
            </button>
          </li>

          {/* Son sayfa */}
          <li
            className={`page-item ${
              currentPage === totalPages ? "disabled" : ""
            }`}
          >
            <button
              className="page-link"
              onClick={() => onPageChange?.(totalPages)}
              disabled={currentPage === totalPages}
              aria-label="Son sayfa"
              style={{ borderRadius: "0 8px 8px 0" }}
            >
              <i className="fas fa-angle-double-right"></i>
            </button>
          </li>
        </ul>
      </nav>
    </div>
  );
};

/**
 * Sayfalanmış veri döndürür
 * @param {Array} data - Tüm veri
 * @param {number} currentPage - Mevcut sayfa
 * @param {number} itemsPerPage - Sayfa başına öğe
 * @returns {Array} Sayfalanmış veri
 */
export const paginateData = (data, currentPage, itemsPerPage) => {
  if (!data || !Array.isArray(data)) return [];

  const startIndex = (currentPage - 1) * itemsPerPage;
  const endIndex = startIndex + itemsPerPage;

  return data.slice(startIndex, endIndex);
};

export default Pagination;
