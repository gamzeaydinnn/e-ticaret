// Ürün detay + varyant seçici + add to cart
import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { ProductService } from "../services/productService";
import { useCart } from "../contexts/CartContext";

export default function Product() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [quantity, setQuantity] = useState(1);
  const [selectedVariant, setSelectedVariant] = useState(null);
  const { addToCart: ctxAddToCart } = useCart();

  useEffect(() => {
    setLoading(true);
    setError(null);
    ProductService.get(id)
      .then((data) => {
        if (data && data.id) {
          setProduct(data);
        } else {
          setError("Ürün bulunamadı");
        }
      })
      .catch((err) => {
        console.error("Ürün yüklenirken hata:", err);
        setError("Ürün yüklenirken bir hata oluştu");
      })
      .finally(() => setLoading(false));
  }, [id]);

  const handleAddToCart = () => {
    if (!product) return;
    ctxAddToCart(product, quantity);
  };

  // Görsel URL'ini normalize et
  const getImageUrl = (url) => {
    if (!url) return "/images/placeholder.png";
    if (url.startsWith("http")) return url;
    if (url.startsWith("/")) return url;
    return `/${url}`;
  };

  if (loading) {
    return (
      <div className="container py-5">
        <div
          className="d-flex justify-content-center align-items-center"
          style={{ minHeight: "400px" }}
        >
          <div
            className="spinner-border text-warning"
            role="status"
            style={{ width: "3rem", height: "3rem" }}
          >
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container py-5">
        <div className="text-center">
          <div
            className="alert alert-warning d-inline-block"
            style={{ borderRadius: "15px" }}
          >
            <i className="fas fa-exclamation-triangle me-2"></i>
            {error}
          </div>
          <div className="mt-3">
            <a href="/" className="btn btn-outline-warning">
              <i className="fas fa-home me-2"></i>Ana Sayfaya Dön
            </a>
          </div>
        </div>
      </div>
    );
  }

  if (!product) {
    return (
      <div className="container py-5">
        <div className="text-center">
          <div
            className="alert alert-info d-inline-block"
            style={{ borderRadius: "15px" }}
          >
            <i className="fas fa-info-circle me-2"></i>Ürün bulunamadı
          </div>
          <div className="mt-3">
            <a href="/" className="btn btn-outline-primary">
              <i className="fas fa-home me-2"></i>Ana Sayfaya Dön
            </a>
          </div>
        </div>
      </div>
    );
  }

  // Fiyat hesaplamaları
  const basePrice = Number(product.price || product.unitPrice || 0);
  const specialPrice = Number(
    product.specialPrice || product.discountPrice || 0
  );
  const hasDiscount = specialPrice > 0 && specialPrice < basePrice;
  const currentPrice = hasDiscount ? specialPrice : basePrice;
  const discountPercent = hasDiscount
    ? Math.round(100 - (specialPrice / basePrice) * 100)
    : 0;

  // Stok durumu
  const stock = product.stock ?? product.stockQuantity ?? 0;
  const isOutOfStock = stock <= 0;
  const isLowStock = stock > 0 && stock <= 5;

  // Görsel URL
  const imageUrl = getImageUrl(product.imageUrl || product.image);

  return (
    <div className="container py-3">
      <div className="row g-3">
        {/* Sol Taraf - Ürün Görseli */}
        <div className="col-12 col-md-5">
          <div className="bg-white rounded-3 shadow-sm p-2 position-relative">
            {hasDiscount && (
              <span className="badge bg-danger position-absolute" style={{ top: "10px", left: "10px", zIndex: 10 }}>
                %{discountPercent}
              </span>
            )}
            <img
              src={imageUrl}
              alt={product.name}
              className="w-100"
              style={{ height: "250px", objectFit: "contain" }}
              onError={(e) => { e.target.src = "/images/placeholder.png"; }}
            />
          </div>
        </div>

        {/* Sağ Taraf - Ürün Bilgileri */}
        <div className="col-12 col-md-7">
          <div className="bg-white rounded-3 shadow-sm p-3">
            {product.categoryName && (
              <span className="badge bg-warning text-dark mb-2" style={{ fontSize: "0.7rem" }}>
                {product.categoryName}
              </span>
            )}

            <h1 className="h5 fw-bold mb-2">{product.name}</h1>
            
            {product.description && (
              <p className="text-muted small mb-3">{product.description}</p>
            )}

            {/* Fiyat */}
            <div className="mb-3">
              {hasDiscount && (
                <span className="text-decoration-line-through text-muted me-2">₺{basePrice.toFixed(2)}</span>
              )}
              <span className="fs-4 fw-bold text-warning">₺{currentPrice.toFixed(2)}</span>
              {product.unit && <span className="text-muted ms-1">/ {product.unit}</span>}
            </div>

            {/* Stok */}
            <div className="mb-3">
              {isOutOfStock ? (
                <span className="badge bg-danger"><i className="fas fa-times-circle me-1"></i>Stokta Yok</span>
              ) : isLowStock ? (
                <span className="badge bg-warning text-dark"><i className="fas fa-exclamation-triangle me-1"></i>Son {stock} Adet</span>
              ) : (
                <span className="badge bg-success"><i className="fas fa-check-circle me-1"></i>Stokta Var</span>
              )}
            </div>

            <hr className="my-2" />

            {/* Miktar */}
            <div className="mb-3">
              <label className="form-label small text-muted mb-1">Miktar</label>
              <div className="d-flex align-items-center gap-2">
                <div className="input-group" style={{ width: "120px" }}>
                  <button
                    className="btn btn-sm btn-outline-warning"
                    type="button"
                    onClick={() => setQuantity(Math.max(1, quantity - 1))}
                    disabled={isOutOfStock}
                  >
                    <i className="fas fa-minus"></i>
                  </button>
                  <input
                    type="number"
                    className="form-control form-control-sm text-center"
                    value={quantity}
                    min={1}
                    onChange={(e) => setQuantity(Math.max(1, Number(e.target.value)))}
                    disabled={isOutOfStock}
                  />
                  <button
                    className="btn btn-sm btn-outline-warning"
                    type="button"
                    onClick={() => setQuantity(quantity + 1)}
                    disabled={isOutOfStock}
                  >
                    <i className="fas fa-plus"></i>
                  </button>
                </div>
                <span className="text-muted small">{product.unit || "adet"}</span>
              </div>
            </div>

            {/* Varyant */}
            {product.variants?.length > 0 && (
              <div className="mb-3">
                <label className="form-label small text-muted mb-1">Varyant</label>
                <select
                  className="form-select form-select-sm"
                  onChange={(e) => setSelectedVariant(e.target.value)}
                >
                  <option value="">Seçiniz</option>
                  {product.variants.map((v) => (
                    <option key={v.id} value={v.id}>{v.name}</option>
                  ))}
                </select>
              </div>
            )}

            {/* Butonlar */}
            <div className="d-grid gap-2">
              <button
                onClick={handleAddToCart}
                disabled={isOutOfStock}
                className="btn btn-warning fw-bold"
              >
                <i className="fas fa-shopping-cart me-2"></i>
                {isOutOfStock ? "Stokta Yok" : "Sepete Ekle"}
              </button>

              <button
                className="btn btn-sm btn-outline-secondary"
                onClick={() => {
                  const url = `${window.location.origin}/product/${product.id}`;
                  if (navigator.share) {
                    navigator.share({ title: product.name, url }).catch(() => {});
                  } else {
                    navigator.clipboard.writeText(url).then(() => alert("Bağlantı kopyalandı"));
                  }
                }}
              >
                <i className="fas fa-share-alt me-1"></i>Paylaş
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
