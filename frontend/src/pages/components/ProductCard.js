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

  const handleShare = async (e) => {
    e.stopPropagation();
    e.preventDefault();
    const url = `${window.location.origin}/product/${product.id}`;
    const title = product.name;
    const text = `${product.name} - ${Number(product.price).toFixed(2)} TL`;
    try {
      if (navigator.share) {
        await navigator.share({ title, text, url });
        return;
      }
    } catch {}
    // Web Share yoksa kopyala
    try {
      await navigator.clipboard.writeText(url);
      alert("Ürün bağlantısı kopyalandı");
    } catch {
      window.open(url, "_blank");
    }
  };

  return (
    <div
      className="modern-product-card h-100"
      style={{
        background: `linear-gradient(145deg, #ffffff, #f8f9fa)`,
        borderRadius: '16px',
        border: '1px solid rgba(255, 107, 53, 0.1)',
        overflow: 'hidden',
        position: 'relative',
        cursor: 'pointer',
        minHeight: '440px',
        maxWidth: '280px',
        margin: '0 auto',
        display: 'flex',
        flexDirection: 'column',
        transition: 'all 0.3s ease',
        boxShadow: '0 4px 12px rgba(0, 0, 0, 0.08)',
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
                padding: '3px 10px',
                borderRadius: '12px',
                fontSize: '0.65rem',
                fontWeight: '700',
                boxShadow: '0 2px 6px rgba(255, 107, 53, 0.3)',
              }}
            >
              {product.badge}
            </span>
          </div>
        )}

        {/* Ürün Resmi */}
        <div className="product-image-container" style={{ height: 160, background: 'linear-gradient(135deg, #f8f9fa, #e9ecef)', position: 'relative', overflow: 'hidden' }}>
          <div
            className="image-wrapper d-flex align-items-center justify-content-center h-100"
            style={{ transition: 'transform 0.4s ease', position: 'relative', padding: '12px' }}
          >
            <img
              src={product.imageUrl || 'https://via.placeholder.com/140'}
              alt={product.name}
              className="product-image"
              style={{ maxHeight: '130px', maxWidth: '130px', objectFit: 'contain', filter: 'drop-shadow(0 3px 8px rgba(0,0,0,0.1))' }}
            />
          </div>
        </div>

        {/* Kart Gövdesi */}
        <div className="card-body p-3 d-flex flex-column" style={{ background: 'linear-gradient(135deg, rgba(255,255,255,0.9), rgba(248,249,250,0.9))', minHeight: '220px', flexGrow: 1 }}>
          {/* Puanlama */}
          {product.rating && (
            <div className="d-flex align-items-center mb-2">
              <div className="star-rating me-2">
                {[...Array(5)].map((_, i) => (
                  <i key={i} className={`fas fa-star ${i < Math.floor(product.rating) ? 'text-warning' : 'text-muted'}`} style={{ fontSize: '0.65rem' }}></i>
                ))}
              </div>
              <small className="text-muted" style={{ fontSize: '0.7rem' }}>({product.reviewCount})</small>
            </div>
          )}

          {/* Ürün Adı */}
          <h6 className="product-title mb-2" style={{ height: '42px', fontSize: '0.875rem', fontWeight: '600', color: '#2c3e50', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden', lineHeight: '1.4' }}>
            {product.name}
          </h6>

          <div className="flex-grow-1 d-flex flex-column justify-content-between">
            {/* Fiyat Bölümü */}
            <div className="price-section mb-2">
              {product.originalPrice ? (
                <div>
                  <div className="d-flex align-items-center mb-1">
                    <span className="old-price me-2" style={{ fontSize: '0.75rem', textDecoration: 'line-through', color: '#6c757d' }}>
                      {product.originalPrice.toFixed(2)} TL
                    </span>
                    {product.discountPercentage > 0 && (
                      <span className="discount-badge" style={{ background: 'linear-gradient(135deg, #dc3545, #c82333)', color: 'white', padding: '2px 6px', borderRadius: '10px', fontSize: '0.65rem', fontWeight: '700' }}>
                        -%{product.discountPercentage}
                      </span>
                    )}
                  </div>
                  <div className="current-price" style={{ fontSize: '1.25rem', fontWeight: '800', background: 'linear-gradient(135deg, #ff6b35, #ff8c00)', backgroundClip: 'text', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent' }}>
                    {product.price.toFixed(2)} TL
                  </div>
                </div>
              ) : (
                <div className="current-price" style={{ fontSize: '1.25rem', fontWeight: '800', background: 'linear-gradient(135deg, #28a745, #20c997)', backgroundClip: 'text', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent' }}>
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
            width: '32px',
            height: '32px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: isFavorite ? 'white' : '#ff6b35',
            transition: 'all 0.3s ease',
            boxShadow: isFavorite ? '0 3px 12px rgba(255, 107, 53, 0.4)' : 'none',
            fontSize: '0.8rem'
          }}
        >
          <i className={isFavorite ? 'fas fa-heart' : 'far fa-heart'}></i>
        </button>
      </div>

      {/* Sepete Ekle Butonu - Kartın en altında */}
      <div className="action-buttons p-3 pt-0" style={{ marginTop: 'auto' }}>
        <button
          className="modern-add-btn w-100"
          onClick={handleCartClick}
          style={{
            background: 'linear-gradient(135deg, #ff6b35, #ff8c00)',
            border: 'none',
            borderRadius: '20px',
            padding: '10px 20px',
            fontSize: '0.825rem',
            fontWeight: '700',
            color: 'white',
            transition: 'all 0.3s ease',
            boxShadow: '0 3px 12px rgba(255, 107, 53, 0.3)',
          }}
        >
          <i className="fas fa-shopping-cart me-1" style={{ fontSize: '0.75rem' }}></i>
          Sepete Ekle
        </button>
        <button
          className="btn w-100 mt-2"
          onClick={handleShare}
          style={{
            borderRadius: '20px',
            border: '1px solid rgba(0,0,0,0.1)',
            background: '#fff',
            fontWeight: '600',
            padding: '8px 16px',
            fontSize: '0.75rem'
          }}
        >
          <i className="fas fa-share-alt me-1" style={{ fontSize: '0.7rem' }}></i>
          Paylaş
        </button>
      </div>
    </div>
  );
}
