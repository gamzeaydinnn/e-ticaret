import React, { useEffect, useState, useMemo } from "react";
import { getAllProducts } from "../services/productService";
import { addToCart } from "../services/cartService";
import { useCartCount } from "../hooks/useCartCount";
import ProductFilter from "./ProductFilter";

export default function ProductGrid() {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [filters, setFilters] = useState({});
  const [categories, setCategories] = useState([]);
  const { refresh: refreshCart } = useCartCount();

  // Mock categories - gerçek uygulamada API'den gelecek
  useEffect(() => {
    setCategories([
      { id: 1, name: "Elektronik" },
      { id: 2, name: "Giyim" },
      { id: 3, name: "Ev & Yaşam" },
      { id: 4, name: "Spor" },
      { id: 5, name: "Kitap" }
    ]);
  }, []);

  // Filtrelenmiş ürünler
  const filteredProducts = useMemo(() => {
    let filtered = [...data];

    // Arama filtresi
    if (filters.search) {
      filtered = filtered.filter(product =>
        product.name.toLowerCase().includes(filters.search.toLowerCase()) ||
        (product.description && product.description.toLowerCase().includes(filters.search.toLowerCase()))
      );
    }

    // Kategori filtresi
    if (filters.category) {
      filtered = filtered.filter(product => product.categoryId === parseInt(filters.category));
    }

    // Fiyat filtresi
    if (filters.minPrice) {
      filtered = filtered.filter(product => product.price >= parseFloat(filters.minPrice));
    }
    if (filters.maxPrice) {
      filtered = filtered.filter(product => product.price <= parseFloat(filters.maxPrice));
    }

    // Sıralama
    if (filters.sortBy) {
      filtered.sort((a, b) => {
        let aValue = a[filters.sortBy];
        let bValue = b[filters.sortBy];
        
        if (filters.sortBy === 'price') {
          aValue = parseFloat(aValue);
          bValue = parseFloat(bValue);
        }
        
        if (filters.sortOrder === 'desc') {
          return aValue < bValue ? 1 : -1;
        }
        return aValue > bValue ? 1 : -1;
      });
    }

    return filtered;
  }, [data, filters]);

  const handleAddToCart = async (productId) => {
    try {
      await addToCart(productId);
      refreshCart();
      // Success feedback
      const button = document.querySelector(`[data-product-id="${productId}"]`);
      if (button) {
        const originalText = button.innerHTML;
        button.innerHTML = '<i class="fas fa-check me-2"></i>Eklendi!';
        button.classList.add('btn-success');
        button.classList.remove('btn-primary');
        setTimeout(() => {
          button.innerHTML = originalText;
          button.classList.remove('btn-success');
          button.classList.add('btn-primary');
        }, 1500);
      }
    } catch (error) {
      console.error("Sepete ekleme hatası:", error);
    }
  };

  useEffect(() => {
    getAllProducts()
      .then(setData)
      .catch((e) => setError(e?.response?.data || "Beklenmeyen hata"))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="text-center py-5">
        <div 
          className="spinner-border mb-3" 
          role="status"
          style={{ color: '#ff8f00', width: '3rem', height: '3rem' }}
        >
          <span className="visually-hidden">Loading...</span>
        </div>
        <p className="text-muted fw-bold">Ürünler yükleniyor...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div 
        className="alert border-0 shadow-sm text-center"
        style={{
          backgroundColor: "#fff3e0",
          borderRadius: "15px",
          color: "#ef6c00",
        }}
      >
        <i className="fas fa-exclamation-triangle fa-2x mb-3"></i>
        <h5 className="fw-bold mb-2">Bir Hata Oluştu</h5>
        <p className="mb-0">{String(error)}</p>
      </div>
    );
  }

  if (!data.length) {
    return (
      <div className="text-center py-5">
        <div 
          className="p-4 rounded-circle mx-auto mb-4 shadow-lg"
          style={{ 
            backgroundColor: '#fff8f0',
            width: '120px',
            height: '120px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}
        >
          <i className="fas fa-box-open text-warning" style={{ fontSize: '3rem' }}></i>
        </div>
        <h4 className="text-warning fw-bold mb-3">Henüz Ürün Yok</h4>
        <p className="text-muted fs-5">Yakında harika ürünlerle karşınızda olacağız!</p>
      </div>
    );
  }

  return (
    <div>
      {/* Filtreleme Komponenti */}
      <ProductFilter 
        onFilterChange={setFilters}
        categories={categories}
      />

      {/* Sonuç Bilgisi */}
      <div className="row mb-3">
        <div className="col-md-6">
          <p className="text-muted mb-0">
            <i className="fas fa-info-circle me-2"></i>
            <strong>{filteredProducts.length}</strong> ürün bulundu
            {data.length !== filteredProducts.length && (
              <span> (toplamda {data.length} ürün)</span>
            )}
          </p>
        </div>
      </div>

      {/* Ürün Grid */}
      {filteredProducts.length === 0 ? (
        <div className="text-center py-5">
          <div 
            className="p-4 rounded-circle mx-auto mb-4 shadow-lg"
            style={{ 
              backgroundColor: '#fff8f0',
              width: '120px',
              height: '120px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center'
            }}
          >
            <i className="fas fa-search text-warning" style={{ fontSize: '3rem' }}></i>
          </div>
          <h4 className="text-warning fw-bold mb-3">Aradığınız Kriterlerde Ürün Bulunamadı</h4>
          <p className="text-muted fs-5">Lütfen filtreleri değiştirerek tekrar deneyin</p>
        </div>
      ) : (
        <div className="row">
          {filteredProducts.map((p) => (
            <div key={p.id} className="col-lg-3 col-md-6 mb-4">
              <div 
                className="card h-100 shadow-sm border-0"
                style={{ 
                  borderRadius: '15px',
                  transition: 'transform 0.3s ease, box-shadow 0.3s ease'
                }}
                onMouseEnter={(e) => {
                  e.target.style.transform = 'translateY(-5px)';
                  e.target.style.boxShadow = '0 10px 25px rgba(255, 111, 0, 0.15)';
                }}
                onMouseLeave={(e) => {
                  e.target.style.transform = 'translateY(0)';
                  e.target.style.boxShadow = '';
                }}
              >
                <div
                  style={{ 
                    height: 200,
                    borderTopLeftRadius: '15px',
                    borderTopRightRadius: '15px',
                    background: 'linear-gradient(135deg, #fff8f0 0%, #fff3e0 100%)'
                  }}
                  className="d-flex align-items-center justify-content-center position-relative"
                >
                  {p.imageUrl ? (
                    <img 
                      src={p.imageUrl} 
                      alt={p.name}
                      className="img-fluid"
                      style={{ 
                        maxHeight: '160px',
                        borderRadius: '10px'
                      }}
                    />
                  ) : (
                    <span style={{ fontSize: '4rem', opacity: 0.7 }}>📦</span>
                  )}
                  
                  {/* Yeni Ürün Badge */}
                  {p.isNew && (
                    <span 
                      className="badge position-absolute top-0 start-0 m-2"
                      style={{
                        background: 'linear-gradient(45deg, #ff6f00, #ff8f00)',
                        borderRadius: '20px',
                        padding: '5px 12px'
                      }}
                    >
                      <i className="fas fa-star me-1"></i>Yeni
                    </span>
                  )}
                  
                  {/* İndirim Badge */}
                  {p.discountPercentage > 0 && (
                    <span 
                      className="badge bg-success position-absolute top-0 end-0 m-2"
                      style={{ borderRadius: '20px', padding: '5px 12px' }}
                    >
                      %{p.discountPercentage} İndirim
                    </span>
                  )}
                </div>
                
                <div className="card-body" style={{ padding: '1.25rem' }}>
                  <div className="text-muted small mb-2">
                    <i className="fas fa-tag me-1"></i>
                    {categories.find(cat => cat.id === p.categoryId)?.name || "Kategori"}
                  </div>
                  
                  <h6 className="card-title mb-2 fw-bold" style={{ minHeight: '48px' }}>
                    {p.name}
                  </h6>
                  
                  {p.description && (
                    <p className="card-text text-muted small mb-3" style={{ fontSize: '0.85rem' }}>
                      {p.description.length > 80 
                        ? p.description.substring(0, 80) + '...' 
                        : p.description
                      }
                    </p>
                  )}
                  
                  {/* Rating */}
                  <div className="mb-3">
                    <div className="d-flex align-items-center">
                      {[1, 2, 3, 4, 5].map(star => (
                        <i 
                          key={star}
                          className={`fas fa-star ${star <= (p.rating || 4) ? 'text-warning' : 'text-muted'}`}
                          style={{ fontSize: '0.85rem' }}
                        ></i>
                      ))}
                      <span className="text-muted ms-2 small">
                        ({p.reviewCount || Math.floor(Math.random() * 50) + 1})
                      </span>
                    </div>
                  </div>
                  
                  <div className="d-flex align-items-center justify-content-between">
                    <div>
                      {p.discountPercentage > 0 ? (
                        <div>
                          <small className="text-muted text-decoration-line-through">
                            ₺{Number(p.originalPrice || p.price * 1.2).toFixed(2)}
                          </small>
                          <div className="fw-bold text-success fs-6">
                            ₺{Number(p.price).toFixed(2)}
                          </div>
                        </div>
                      ) : (
                        <strong className="text-warning fs-6">
                          ₺{Number(p.price).toFixed(2)}
                        </strong>
                      )}
                    </div>
                    
                    <button
                      className="btn btn-sm shadow-sm fw-bold"
                      data-product-id={p.id}
                      style={{
                        background: 'linear-gradient(45deg, #ff6f00, #ff8f00)',
                        color: 'white',
                        border: 'none',
                        borderRadius: '20px',
                        padding: '8px 16px',
                        transition: 'all 0.3s ease'
                      }}
                      onClick={() => handleAddToCart(p.id)}
                      onMouseEnter={(e) => {
                        e.target.style.transform = 'scale(1.05)';
                      }}
                      onMouseLeave={(e) => {
                        e.target.style.transform = 'scale(1)';
                      }}
                    >
                      <i className="fas fa-shopping-cart me-2"></i>
                      Sepete Ekle
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
