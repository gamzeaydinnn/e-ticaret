import React from 'react';
import { Link } from 'react-router-dom';

/**
 * Ürünleri listelerde göstermek için kullanılan modern ve yeniden kullanılabilir kart bileşeni.
 *
 * @param {object} props
 * @param {object} props.product - Gösterilecek ürün bilgileri.
 * @param {function} props.onAddToCart - Sepete ekle butonuna tıklandığında çalışacak fonksiyon.
 * @param {function} props.onToggleFavorite - Favori butonuna tıklandığında çalışacak fonksiyon.
 * @param {boolean} props.isFavorite - Ürünün favorilerde olup olmadığını belirten boolean.
 */
export default function ProductCard({ product, onAddToCart, onToggleFavorite, isFavorite }) {
  // Eğer ürün bilgisi gelmezse, bileşeni render etme.
  if (!product) {
    return null;
  }

  // Buton tıklamalarının Link'i tetiklemesini engellemek için.
  const handleFavoriteClick = (e) => {
    e.stopPropagation();
    e.preventDefault();
    onToggleFavorite(product.id);
  };

  const handleCartClick = (e) => {
    e.stopPropagation();
    e.preventDefault();
    onAddToCart(product.id);
  };

  return (
    <div
      className="modern-product-card h-100"
      style={{
        background: `linear-gradient(145deg, #ffffff, #f8f9fa)`,
        borderRadius: '20px',
        border: '1px solid rgba(255, 107, 53, 0.1)',
        overflow: 'hidden',
        position: 'relative',
        cursor: 'pointer',
        minHeight: '520px',
        display: 'flex',
        flexDirection: 'column',
        transition: 'all 0.3s ease',
        boxShadow: '0 5px 15px rgba(0, 0, 0, 0.08)',
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = 'translateY(-8px) scale(1.02)';
        e.currentTarget.style.boxShadow = '0 20px 40px rgba(255, 107, 53, 0.15)';
        e.currentTarget.style.borderColor = 'rgba(255, 107, 53, 0.3)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'translateY(0) scale(1)';
        e.currentTarget.style.boxShadow = '0 5px 15px rgba(0, 0, 0, 0.08)';
        e.currentTarget.style.borderColor = 'rgba(255, 107, 53, 0.1)';
      }}
    >
      {/* Link tüm kartı sarmalar, butonlar hariç */}
      <Link to={`/product/${product.id}`} style={{ textDecoration: 'none', color: 'inherit', display: 'flex', flexDirection: 'column', flexGrow: 1 }}>

        {/* Badge - Sol Üst */}
        {product.badge && (
          <div className="position-absolute top-0 start-0 p-2" style={{ zIndex: 3 }}>
            <span
              className="badge-modern"
              style={{
                background:
                  product.badge === 'İndirim'
                    ? 'linear-gradient(135deg, #ff6b35, #ff8c00)'
                    : product.badge === 'Yeni'
                    ? 'linear-gradient(135deg, #28a745, #20c997)'
                    : 'linear-gradient(135deg, #ffc107, #fd7e14)',
                color: 'white',
                padding: '4px 12px',
                borderRadius: '15px',
                fontSize: '0.7rem',
                fontWeight: '700',
                boxShadow: '0 2px 8px rgba(255, 107, 53, 0.3)',
              }}
            >
              {product.badge}
            </span>
          </div>
        )}

        {/* Ürün Resmi */}
        <div className="product-image-container" style={{ height: 200, background: 'linear-gradient(135deg, #f8f9fa, #e9ecef)', position: 'relative', overflow: 'hidden' }}>
          <div
            className="image-wrapper d-flex align-items-center justify-content-center h-100"
            style={{ transition: 'transform 0.4s ease', position: 'relative' }}
          >
            <img
              src={product.imageUrl || 'https://via.placeholder.com/160'}
              alt={product.name}
              className="product-image"
              style={{ maxHeight: '160px', maxWidth: '160px', objectFit: 'contain', filter: 'drop-shadow(0 4px 12px rgba(0,0,0,0.1))' }}
            />
          </div>
        </div>

        {/* Kart Gövdesi */}
        <div className="card-body p-4 d-flex flex-column" style={{ background: 'linear-gradient(135deg, rgba(255,255,255,0.9), rgba(248,249,250,0.9))', minHeight: '280px', flexGrow: 1 }}>
          {/* Puanlama */}
          {product.rating && (
            <div className="d-flex align-items-center mb-2">
              <div className="star-rating me-2">
                {[...Array(5)].map((_, i) => (
                  <i key={i} className={`fas fa-star ${i < Math.floor(product.rating) ? 'text-warning' : 'text-muted'}`} style={{ fontSize: '0.7rem' }}></i>
                ))}
              </div>
              <small className="text-muted">({product.reviewCount})</small>
            </div>
          )}

          {/* Ürün Adı */}
          <h6 className="product-title mb-3" style={{ height: '50px', fontSize: '0.95rem', fontWeight: '600', color: '#2c3e50', display: 'flex', alignItems: 'center' }}>
            {product.name}
          </h6>

          <div className="flex-grow-1 d-flex flex-column justify-content-between">
            {/* Fiyat Bölümü */}
            <div className="price-section mb-3">
              {product.originalPrice ? (
                <div>
                  <div className="d-flex align-items-center mb-2">
                    <span className="old-price me-2" style={{ fontSize: '0.85rem', textDecoration: 'line-through', color: '#6c757d' }}>
                      {product.originalPrice.toFixed(2)} TL
                    </span>
                    {product.discountPercentage > 0 && (
                      <span className="discount-badge" style={{ background: 'linear-gradient(135deg, #dc3545, #c82333)', color: 'white', padding: '3px 8px', borderRadius: '12px', fontSize: '0.7rem', fontWeight: '700' }}>
                        -%{product.discountPercentage}
                      </span>
                    )}
                  </div>
                  <div className="current-price" style={{ fontSize: '1.4rem', fontWeight: '800', background: 'linear-gradient(135deg, #ff6b35, #ff8c00)', backgroundClip: 'text', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent' }}>
                    {product.price.toFixed(2)} TL
                  </div>
                </div>
              ) : (
                <div className="current-price" style={{ fontSize: '1.4rem', fontWeight: '800', background: 'linear-gradient(135deg, #28a745, #20c997)', backgroundClip: 'text', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent' }}>
                  {product.price.toFixed(2)} TL
                </div>
              )}
            </div>
          </div>
        </div>
      </Link>
      
      {/* Link dışında kalan butonlar */}
      {/* Favori Butonu */}
      <div className="position-absolute top-0 end-0 p-2" style={{ zIndex: 3 }}>
        <button
          className="btn-favorite"
          type="button"
          onClick={handleFavoriteClick}
          style={{
            background: isFavorite ? 'linear-gradient(135deg, #ff6b35, #ff8c00)' : 'rgba(255, 255, 255, 0.9)',
            border: 'none',
            borderRadius: '50%',
            width: '35px',
            height: '35px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: isFavorite ? 'white' : '#ff6b35',
            transition: 'all 0.3s ease',
            boxShadow: isFavorite ? '0 4px 15px rgba(255, 107, 53, 0.4)' : 'none',
          }}
        >
          <i className={isFavorite ? 'fas fa-heart' : 'far fa-heart'}></i>
        </button>
      </div>

      {/* Sepete Ekle Butonu - Kartın en altında */}
      <div className="action-buttons p-4 pt-0" style={{ marginTop: 'auto' }}>
        <button
          className="modern-add-btn w-100"
          onClick={handleCartClick}
          style={{
            background: 'linear-gradient(135deg, #ff6b35, #ff8c00)',
            border: 'none',
            borderRadius: '25px',
            padding: '12px 24px',
            fontSize: '0.9rem',
            fontWeight: '700',
            color: 'white',
            transition: 'all 0.3s ease',
            boxShadow: '0 4px 15px rgba(255, 107, 53, 0.3)',
          }}
        >
          <i className="fas fa-shopping-cart me-2"></i>
          Sepete Ekle
        </button>
      </div>
    </div>
  );
}