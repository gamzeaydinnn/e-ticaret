import React, { useState, useEffect } from 'react';

const ProductFilter = ({ onFilterChange, categories = [] }) => {
  const [filters, setFilters] = useState({
    search: '',
    category: '',
    minPrice: '',
    maxPrice: '',
    sortBy: 'name',
    sortOrder: 'asc'
  });

  useEffect(() => {
    onFilterChange(filters);
  }, [filters, onFilterChange]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFilters(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const clearFilters = () => {
    setFilters({
      search: '',
      category: '',
      minPrice: '',
      maxPrice: '',
      sortBy: 'name',
      sortOrder: 'asc'
    });
  };

  return (
    <div 
      className="card shadow-lg border-0 mb-4"
      style={{ 
        borderRadius: '20px',
        background: 'linear-gradient(135deg, #fff8f0 0%, #fff3e0 100%)'
      }}
    >
      <div 
        className="card-header text-white border-0"
        style={{ 
          background: 'linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)',
          borderTopLeftRadius: '20px',
          borderTopRightRadius: '20px',
          padding: '1rem 1.5rem'
        }}
      >
        <h5 className="mb-0 fw-bold">
          <i className="fas fa-filter me-2"></i>Ürün Filtreleme
        </h5>
      </div>
      
      <div className="card-body" style={{ padding: '1.5rem' }}>
        <div className="row g-3">
          {/* Arama */}
          <div className="col-md-4">
            <label className="form-label fw-bold text-warning">
              <i className="fas fa-search me-2"></i>Ürün Ara
            </label>
            <input
              type="text"
              name="search"
              className="form-control border-0 shadow-sm"
              style={{ 
                backgroundColor: '#fff8f0',
                borderRadius: '15px',
                padding: '0.75rem 1rem'
              }}
              placeholder="Ürün adı veya açıklama..."
              value={filters.search}
              onChange={handleInputChange}
            />
          </div>

          {/* Kategori */}
          <div className="col-md-3">
            <label className="form-label fw-bold text-warning">
              <i className="fas fa-tags me-2"></i>Kategori
            </label>
            <select
              name="category"
              className="form-select border-0 shadow-sm"
              style={{ 
                backgroundColor: '#fff8f0',
                borderRadius: '15px',
                padding: '0.75rem 1rem'
              }}
              value={filters.category}
              onChange={handleInputChange}
            >
              <option value="">Tüm Kategoriler</option>
              {categories.map(cat => (
                <option key={cat.id} value={cat.id}>{cat.name}</option>
              ))}
            </select>
          </div>

          {/* Fiyat Aralığı */}
          <div className="col-md-2">
            <label className="form-label fw-bold text-warning">
              <i className="fas fa-tag me-2"></i>Min Fiyat
            </label>
            <input
              type="number"
              name="minPrice"
              className="form-control border-0 shadow-sm"
              style={{ 
                backgroundColor: '#fff8f0',
                borderRadius: '15px',
                padding: '0.75rem 1rem'
              }}
              placeholder="₺0"
              value={filters.minPrice}
              onChange={handleInputChange}
            />
          </div>

          <div className="col-md-2">
            <label className="form-label fw-bold text-warning">
              <i className="fas fa-tag me-2"></i>Max Fiyat
            </label>
            <input
              type="number"
              name="maxPrice"
              className="form-control border-0 shadow-sm"
              style={{ 
                backgroundColor: '#fff8f0',
                borderRadius: '15px',
                padding: '0.75rem 1rem'
              }}
              placeholder="₺999999"
              value={filters.maxPrice}
              onChange={handleInputChange}
            />
          </div>

          {/* Sıralama */}
          <div className="col-md-1">
            <label className="form-label fw-bold text-warning">
              <i className="fas fa-sort me-2"></i>Sırala
            </label>
            <select
              name="sortBy"
              className="form-select border-0 shadow-sm"
              style={{ 
                backgroundColor: '#fff8f0',
                borderRadius: '15px',
                padding: '0.75rem 1rem'
              }}
              value={filters.sortBy}
              onChange={handleInputChange}
            >
              <option value="name">İsim</option>
              <option value="price">Fiyat</option>
              <option value="createdDate">Tarih</option>
            </select>
          </div>
        </div>

        <div className="row mt-3">
          <div className="col-md-3">
            <select
              name="sortOrder"
              className="form-select border-0 shadow-sm"
              style={{ 
                backgroundColor: '#fff8f0',
                borderRadius: '15px',
                padding: '0.75rem 1rem'
              }}
              value={filters.sortOrder}
              onChange={handleInputChange}
            >
              <option value="asc">A-Z / Düşük-Yüksek</option>
              <option value="desc">Z-A / Yüksek-Düşük</option>
            </select>
          </div>
          
          <div className="col-md-9 d-flex justify-content-end align-items-end">
            <button
              type="button"
              className="btn btn-outline-warning fw-bold shadow-sm"
              style={{ 
                borderRadius: '15px',
                padding: '0.75rem 2rem'
              }}
              onClick={clearFilters}
            >
              <i className="fas fa-eraser me-2"></i>Filtreleri Temizle
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProductFilter;