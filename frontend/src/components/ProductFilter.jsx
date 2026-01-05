import React, { useState, useEffect } from "react";

const ProductFilter = ({ onFilterChange, categories = [] }) => {
  const [filters, setFilters] = useState({
    search: "",
    category: "",
    minPrice: "",
    maxPrice: "",
    sortBy: "name",
    sortOrder: "asc",
  });
  const [isExpanded, setIsExpanded] = useState(true);

  useEffect(() => {
    onFilterChange(filters);
  }, [filters, onFilterChange]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
  };

  const clearFilters = () => {
    setFilters({
      search: "",
      category: "",
      minPrice: "",
      maxPrice: "",
      sortBy: "name",
      sortOrder: "asc",
    });
  };

  return (
    <div className="card shadow-sm border-0 mb-3" style={{ borderRadius: "12px" }}>
      <div
        className="card-header bg-warning text-white border-0 d-flex justify-content-between align-items-center"
        style={{ borderTopLeftRadius: "12px", borderTopRightRadius: "12px", padding: "0.75rem 1rem", cursor: "pointer" }}
        onClick={() => setIsExpanded(!isExpanded)}
      >
        <h6 className="mb-0 fw-bold">
          <i className="fas fa-filter me-2"></i>Filtreler
        </h6>
        <button className="btn btn-sm btn-link text-white p-0" type="button">
          <i className={`fas fa-chevron-${isExpanded ? "up" : "down"}`}></i>
        </button>
      </div>

      {isExpanded && (
        <div className="card-body p-3">
          <div className="row g-2">
            {/* Arama */}
            <div className="col-12 col-md-6 col-lg-3">
              <input
                type="text"
                name="search"
                className="form-control form-control-sm"
                placeholder="Ürün ara..."
                value={filters.search}
                onChange={handleInputChange}
              />
            </div>

            {/* Kategori */}
            <div className="col-12 col-md-6 col-lg-3">
              <select
                name="category"
                className="form-select form-select-sm"
                value={filters.category}
                onChange={handleInputChange}
              >
                <option value="">Tüm Kategoriler</option>
                {categories.map((cat) => (
                  <option key={cat.id} value={cat.id}>{cat.name}</option>
                ))}
              </select>
            </div>

            {/* Fiyat */}
            <div className="col-6 col-md-3 col-lg-2">
              <input
                type="number"
                name="minPrice"
                className="form-control form-control-sm"
                placeholder="Min ₺"
                value={filters.minPrice}
                onChange={handleInputChange}
              />
            </div>

            <div className="col-6 col-md-3 col-lg-2">
              <input
                type="number"
                name="maxPrice"
                className="form-control form-control-sm"
                placeholder="Max ₺"
                value={filters.maxPrice}
                onChange={handleInputChange}
              />
            </div>

            {/* Sıralama */}
            <div className="col-12 col-md-6 col-lg-2">
              <select
                name="sortBy"
                className="form-select form-select-sm"
                value={filters.sortBy}
                onChange={handleInputChange}
              >
                <option value="name">İsim</option>
                <option value="price">Fiyat</option>
                <option value="createdDate">Tarih</option>
              </select>
            </div>
          </div>

          <div className="row g-2 mt-2">
            <div className="col-12 col-md-6 col-lg-3">
              <select
                name="sortOrder"
                className="form-select form-select-sm"
                value={filters.sortOrder}
                onChange={handleInputChange}
              >
                <option value="asc">A-Z / Düşük-Yüksek</option>
                <option value="desc">Z-A / Yüksek-Düşük</option>
              </select>
            </div>

            <div className="col-12 col-md-6 col-lg-9 d-flex justify-content-end">
              <button
                type="button"
                className="btn btn-sm btn-outline-warning"
                onClick={clearFilters}
              >
                <i className="fas fa-eraser me-1"></i>Temizle
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ProductFilter;
