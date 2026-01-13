// ============================================================================
// ResponsiveTable.jsx - Mobil Uyumlu Tablo Bileşeni
// ============================================================================
// 768px altında kart görünümüne geçen responsive tablo bileşeni.
// Admin panelindeki tüm tablolar için kullanılabilir.
// ============================================================================

import { useState, useEffect } from "react";

/**
 * Mobil uyumlu tablo bileşeni
 * @param {Object} props
 * @param {Array} props.columns - Sütun tanımları [{key, label, render?, width?}]
 * @param {Array} props.data - Tablo verileri
 * @param {Function} props.mobileCardRenderer - Mobil kart render fonksiyonu (opsiyonel)
 * @param {string} props.keyField - Benzersiz key alanı (default: 'id')
 * @param {boolean} props.loading - Yükleniyor durumu
 * @param {string} props.emptyMessage - Veri yokken gösterilecek mesaj
 * @param {Function} props.onRowClick - Satır tıklama handler'ı
 * @param {number} props.breakpoint - Mobil breakpoint (default: 768)
 */
const ResponsiveTable = ({
  columns = [],
  data = [],
  mobileCardRenderer,
  keyField = "id",
  loading = false,
  emptyMessage = "Veri bulunamadı",
  onRowClick,
  breakpoint = 768,
  className = "",
  striped = true,
  hover = true,
}) => {
  const [isMobile, setIsMobile] = useState(window.innerWidth < breakpoint);

  // Ekran boyutu değişikliğini dinle
  useEffect(() => {
    const handleResize = () => {
      setIsMobile(window.innerWidth < breakpoint);
    };

    window.addEventListener("resize", handleResize);
    return () => window.removeEventListener("resize", handleResize);
  }, [breakpoint]);

  // Loading durumu
  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center py-5">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  // Boş veri durumu
  if (!data || data.length === 0) {
    return (
      <div className="text-center py-5">
        <i className="fas fa-inbox fa-3x text-muted mb-3"></i>
        <p className="text-muted">{emptyMessage}</p>
      </div>
    );
  }

  // Mobil kart görünümü
  if (isMobile) {
    return (
      <div className={`responsive-card-list ${className}`}>
        {data.map((item, index) => {
          // Özel kart renderer varsa kullan
          if (mobileCardRenderer) {
            return (
              <div key={item[keyField] || index} className="mb-3">
                {mobileCardRenderer(item, index)}
              </div>
            );
          }

          // Varsayılan kart görünümü
          return (
            <div
              key={item[keyField] || index}
              className="card border-0 shadow-sm mb-3"
              style={{ borderRadius: "12px" }}
              onClick={() => onRowClick?.(item)}
              role={onRowClick ? "button" : undefined}
            >
              <div className="card-body p-3">
                {columns.map((col, colIndex) => {
                  const value = col.render
                    ? col.render(item[col.key], item, index)
                    : item[col.key];

                  return (
                    <div
                      key={col.key || colIndex}
                      className="d-flex justify-content-between align-items-center py-2"
                      style={{
                        borderBottom:
                          colIndex < columns.length - 1
                            ? "1px solid #f1f5f9"
                            : "none",
                      }}
                    >
                      <span className="text-muted small fw-medium">
                        {col.label}
                      </span>
                      <span
                        className="fw-medium text-end"
                        style={{ maxWidth: "60%" }}
                      >
                        {value ?? "-"}
                      </span>
                    </div>
                  );
                })}
              </div>
            </div>
          );
        })}
      </div>
    );
  }

  // Desktop tablo görünümü
  return (
    <div className={`table-responsive ${className}`}>
      <table
        className={`table ${striped ? "table-striped" : ""} ${
          hover ? "table-hover" : ""
        } mb-0`}
        style={{ tableLayout: "fixed" }}
      >
        <thead>
          <tr style={{ borderBottom: "2px solid #e2e8f0" }}>
            {columns.map((col, index) => (
              <th
                key={col.key || index}
                className="fw-semibold text-muted border-0 py-3"
                style={{
                  width: col.width || "auto",
                  fontSize: "0.85rem",
                  whiteSpace: "nowrap",
                }}
              >
                {col.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((item, rowIndex) => (
            <tr
              key={item[keyField] || rowIndex}
              onClick={() => onRowClick?.(item)}
              style={{ cursor: onRowClick ? "pointer" : "default" }}
            >
              {columns.map((col, colIndex) => {
                const value = col.render
                  ? col.render(item[col.key], item, rowIndex)
                  : item[col.key];

                return (
                  <td
                    key={col.key || colIndex}
                    className="border-0 py-3"
                    style={{
                      fontSize: "0.9rem",
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                      whiteSpace: col.nowrap !== false ? "nowrap" : "normal",
                    }}
                    data-label={col.label}
                  >
                    {value ?? "-"}
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

/**
 * Responsive tablo için CSS stilleri
 * Bu stilleri global CSS'e veya component'a ekleyin
 */
export const responsiveTableStyles = `
  /* Mobil kart listesi stilleri */
  .responsive-card-list .card {
    transition: transform 0.2s ease, box-shadow 0.2s ease;
  }
  
  .responsive-card-list .card:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important;
  }
  
  /* Tablo responsive stilleri - 768px altı */
  @media (max-width: 768px) {
    .table-responsive table {
      display: block;
    }
    
    .table-responsive thead {
      display: none;
    }
    
    .table-responsive tbody {
      display: block;
    }
    
    .table-responsive tr {
      display: block;
      margin-bottom: 1rem;
      background: white;
      border-radius: 12px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
      padding: 0.5rem;
    }
    
    .table-responsive td {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.75rem 1rem !important;
      border-bottom: 1px solid #f1f5f9;
      text-align: right;
    }
    
    .table-responsive td:last-child {
      border-bottom: none;
    }
    
    .table-responsive td::before {
      content: attr(data-label);
      font-weight: 600;
      color: #64748b;
      font-size: 0.8rem;
      text-align: left;
      flex: 1;
    }
    
    .table-responsive td > * {
      flex: 1;
      text-align: right;
    }
  }
`;

export default ResponsiveTable;
