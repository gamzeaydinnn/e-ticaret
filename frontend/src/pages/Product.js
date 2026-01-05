// Ürün detay + varyant seçici + add to cart
import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { ProductService } from "../services/productService";
import api from "../services/api";

export default function Product() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [quantity, setQuantity] = useState(1);
  const [selectedVariant, setSelectedVariant] = useState(null);

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

  const addToCart = async () => {
    try {
      await api.post("/api/CartItems", {
        productId: product.id,
        qty: quantity,
        variant: selectedVariant,
      });
      window.dispatchEvent(new CustomEvent("cart:updated"));
      alert("Sepete eklendi");
    } catch (err) {
      alert("Sepete eklenirken hata oluştu");
    }
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
    <div className="container py-4">
      <div className="row g-4">
        {/* Sol Taraf - Ürün Görseli */}
        <div className="col-lg-6">
          <div
            className="card border-0 shadow-sm"
            style={{ borderRadius: "20px", overflow: "hidden" }}
          >
            <div className="position-relative">
              {hasDiscount && (
                <span
                  className="position-absolute badge"
                  style={{
                    top: "15px",
                    left: "15px",
                    background: "linear-gradient(135deg, #e74c3c, #c0392b)",
                    fontSize: "0.9rem",
                    padding: "8px 15px",
                    borderRadius: "20px",
                    zIndex: 10,
                  }}
                >
                  %{discountPercent} İNDİRİM
                </span>
              )}
              <img
                src={imageUrl}
                alt={product.name}
                className="w-100"
                style={{
                  height: "450px",
                  objectFit: "contain",
                  background: "#f8f9fa",
                  padding: "20px",
                }}
                onError={(e) => {
                  e.target.src = "/images/placeholder.png";
                }}
              />
            </div>
          </div>
        </div>

        {/* Sağ Taraf - Ürün Bilgileri */}
        <div className="col-lg-6">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "20px" }}
          >
            <div className="card-body p-4">
              {/* Kategori */}
              {product.categoryName && (
                <span
                  className="badge mb-3"
                  style={{
                    background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                    fontSize: "0.8rem",
                    padding: "6px 12px",
                    borderRadius: "15px",
                  }}
                >
                  {product.categoryName}
                </span>
              )}

              {/* Ürün Adı */}
              <h1
                className="fw-bold mb-3"
                style={{ fontSize: "1.8rem", color: "#2c3e50" }}
              >
                {product.name}
              </h1>

              {/* Açıklama */}
              {product.description && (
                <p className="text-muted mb-4" style={{ lineHeight: "1.7" }}>
                  {product.description}
                </p>
              )}

              {/* Fiyat */}
              <div className="mb-4">
                {hasDiscount && (
                  <div className="mb-1">
                    <span
                      className="text-decoration-line-through text-muted me-2"
                      style={{ fontSize: "1.1rem" }}
                    >
                      ₺{basePrice.toFixed(2)}
                    </span>
                  </div>
                )}
                <div className="d-flex align-items-center gap-2">
                  <span
                    className="fw-bold"
                    style={{
                      fontSize: "2rem",
                      background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      WebkitBackgroundClip: "text",
                      WebkitTextFillColor: "transparent",
                    }}
                  >
                    ₺{currentPrice.toFixed(2)}
                  </span>
                  {product.unit && (
                    <span className="text-muted">/ {product.unit}</span>
                  )}
                </div>
              </div>

              {/* Stok Durumu */}
              <div className="mb-4">
                {isOutOfStock ? (
                  <span
                    className="badge bg-danger"
                    style={{
                      fontSize: "0.85rem",
                      padding: "8px 15px",
                      borderRadius: "10px",
                    }}
                  >
                    <i className="fas fa-times-circle me-1"></i>Stokta Yok
                  </span>
                ) : isLowStock ? (
                  <span
                    className="badge bg-warning text-dark"
                    style={{
                      fontSize: "0.85rem",
                      padding: "8px 15px",
                      borderRadius: "10px",
                    }}
                  >
                    <i className="fas fa-exclamation-triangle me-1"></i>Son{" "}
                    {stock} Adet
                  </span>
                ) : (
                  <span
                    className="badge bg-success"
                    style={{
                      fontSize: "0.85rem",
                      padding: "8px 15px",
                      borderRadius: "10px",
                    }}
                  >
                    <i className="fas fa-check-circle me-1"></i>Stokta Var
                  </span>
                )}
              </div>

              <hr className="my-4" />

              {/* Miktar Seçimi */}
              <div className="mb-4">
                <label className="form-label fw-semibold text-muted mb-2">
                  Miktar
                </label>
                <div className="d-flex align-items-center gap-3">
                  <div className="input-group" style={{ maxWidth: "150px" }}>
                    <button
                      className="btn btn-outline-secondary"
                      type="button"
                      onClick={() => setQuantity(Math.max(1, quantity - 1))}
                      disabled={isOutOfStock}
                    >
                      <i className="fas fa-minus"></i>
                    </button>
                    <input
                      type="number"
                      className="form-control text-center"
                      value={quantity}
                      min={1}
                      onChange={(e) =>
                        setQuantity(Math.max(1, Number(e.target.value)))
                      }
                      disabled={isOutOfStock}
                      style={{ borderLeft: 0, borderRight: 0 }}
                    />
                    <button
                      className="btn btn-outline-secondary"
                      type="button"
                      onClick={() => setQuantity(quantity + 1)}
                      disabled={isOutOfStock}
                    >
                      <i className="fas fa-plus"></i>
                    </button>
                  </div>
                  <span className="text-muted">{product.unit || "adet"}</span>
                </div>
              </div>

              {/* Varyant Seçimi */}
              {product.variants?.length > 0 && (
                <div className="mb-4">
                  <label className="form-label fw-semibold text-muted mb-2">
                    Varyant
                  </label>
                  <select
                    className="form-select"
                    onChange={(e) => setSelectedVariant(e.target.value)}
                    style={{ borderRadius: "10px", padding: "12px" }}
                  >
                    <option value="">Seçiniz</option>
                    {product.variants.map((v) => (
                      <option key={v.id} value={v.id}>
                        {v.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              {/* Butonlar */}
              <div className="d-grid gap-2">
                <button
                  onClick={addToCart}
                  disabled={isOutOfStock}
                  className="btn btn-lg text-white"
                  style={{
                    background: isOutOfStock
                      ? "#ccc"
                      : "linear-gradient(135deg, #27ae60, #2ecc71)",
                    border: "none",
                    borderRadius: "15px",
                    padding: "15px",
                    fontSize: "1.1rem",
                    fontWeight: "600",
                    boxShadow: isOutOfStock
                      ? "none"
                      : "0 4px 15px rgba(39, 174, 96, 0.3)",
                    transition: "all 0.3s ease",
                  }}
                >
                  <i className="fas fa-shopping-cart me-2"></i>
                  {isOutOfStock ? "Stokta Yok" : "Sepete Ekle"}
                </button>

                <button
                  className="btn btn-outline-secondary"
                  style={{ borderRadius: "15px", padding: "12px" }}
                  onClick={() => {
                    const url = `${window.location.origin}/product/${product.id}`;
                    const title = product.name;
                    const text = `${product.name} - ₺${currentPrice.toFixed(
                      2
                    )}`;
                    if (navigator.share) {
                      navigator.share({ title, text, url }).catch(() => {});
                    } else {
                      navigator.clipboard
                        .writeText(url)
                        .then(() => alert("Bağlantı kopyalandı"));
                    }
                  }}
                >
                  <i className="fas fa-share-alt me-2"></i>Paylaş
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
