import "./Pagination.css";

const buildPageItems = (currentPage, totalPages) => {
  if (totalPages <= 7) {
    return Array.from({ length: totalPages }, (_, index) => index + 1);
  }

  const pages = new Set([
    1,
    totalPages,
    currentPage,
    currentPage - 1,
    currentPage + 1,
  ]);

  const normalized = [...pages]
    .filter((page) => page >= 1 && page <= totalPages)
    .sort((left, right) => left - right);

  const items = [];
  for (let index = 0; index < normalized.length; index += 1) {
    const page = normalized[index];
    const previous = normalized[index - 1];

    if (previous && page - previous > 1) {
      items.push(`ellipsis-${previous}`);
    }

    items.push(page);
  }

  return items;
};

export const paginateData = (data, currentPage, itemsPerPage) => {
  if (!Array.isArray(data) || itemsPerPage <= 0) {
    return [];
  }

  const startIndex = (Math.max(1, currentPage) - 1) * itemsPerPage;
  return data.slice(startIndex, startIndex + itemsPerPage);
};

export default function Pagination({
  currentPage = 1,
  totalPages,
  totalItems = 0,
  pageSize = 25,
  itemsPerPage,
  onPageChange,
  onPageSizeChange,
}) {
  const effectivePageSize = itemsPerPage ?? pageSize;
  const safeCurrentPage = Math.max(1, currentPage);
  const computedTotalPages =
    totalPages ?? (Math.ceil(totalItems / effectivePageSize) || 1);
  const safeTotalPages = Math.max(1, computedTotalPages);

  if (safeTotalPages <= 1) {
    return null;
  }

  const startItem =
    totalItems === 0 ? 0 : (safeCurrentPage - 1) * effectivePageSize + 1;
  const endItem =
    totalItems === 0
      ? 0
      : Math.min(totalItems, safeCurrentPage * effectivePageSize);
  const pageItems = buildPageItems(safeCurrentPage, safeTotalPages);

  return (
    <div className="pagination-shell">
      <div className="pagination-summary">
        {`${totalItems} üründen ${startItem}-${endItem} gösteriliyor`}
      </div>

      <div className="pagination-controls">
        <button
          type="button"
          className="pagination-nav"
          onClick={() => onPageChange?.(safeCurrentPage - 1)}
          disabled={safeCurrentPage <= 1}
        >
          Önceki
        </button>

        <div className="pagination-number-list" aria-label="Sayfa numaraları">
          {pageItems.map((item) => {
            if (typeof item === "string") {
              return (
                <span key={item} className="pagination-ellipsis">
                  ...
                </span>
              );
            }

            return (
              <button
                key={item}
                type="button"
                className={`pagination-number ${item === safeCurrentPage ? "is-active" : ""}`}
                onClick={() => onPageChange?.(item)}
                aria-current={item === safeCurrentPage ? "page" : undefined}
              >
                {item}
              </button>
            );
          })}
        </div>

        <button
          type="button"
          className="pagination-nav"
          onClick={() => onPageChange?.(safeCurrentPage + 1)}
          disabled={safeCurrentPage >= safeTotalPages}
        >
          Sonraki
        </button>
      </div>

      <label className="pagination-size-picker">
        <span>Sayfa başına</span>
        <select
          value={effectivePageSize}
          onChange={(event) =>
            onPageSizeChange?.(parseInt(event.target.value, 10))
          }
        >
          {[25, 50, 100].map((sizeOption) => (
            <option key={sizeOption} value={sizeOption}>
              {sizeOption}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}
