/**
 * AddToCartModal.js - Sepete Ekleme Başarılı Pop-up
 *
 * Ürün sepete eklendiğinde gösterilen pop-up modal
 */
import React from "react";
import { useNavigate } from "react-router-dom";
import "./AddToCartModal.css";

export default function AddToCartModal({ isOpen, onClose, product }) {
  const navigate = useNavigate();

  if (!isOpen || !product) return null;

  const handleGoToCart = () => {
    onClose();
    navigate("/cart");
  };

  const handleContinueShopping = () => {
    onClose();
  };

  // Ürün resmi için URL
  const getImageUrl = () => {
    if (!product.imageUrl) return "/images/placeholder.png";
    if (product.imageUrl.startsWith("http")) return product.imageUrl;
    return `/api${product.imageUrl}`;
  };

  return (
    <div className="add-to-cart-modal-overlay" onClick={onClose}>
      <div className="add-to-cart-modal" onClick={(e) => e.stopPropagation()}>
        {/* Başlık */}
        <div className="add-to-cart-modal-header">
          <div className="success-icon">
            <i className="fas fa-check-circle"></i>
          </div>
          <h3>Sepete Eklendi!</h3>
          <button className="close-btn" onClick={onClose}>
            <i className="fas fa-times"></i>
          </button>
        </div>

        {/* Ürün Bilgileri */}
        <div className="add-to-cart-modal-body">
          <div className="product-info">
            <img
              src={getImageUrl()}
              alt={product.name}
              className="product-image"
              onError={(e) => {
                e.target.src = "/images/placeholder.png";
              }}
            />
            <div className="product-details">
              <h4 className="product-name">{product.name}</h4>
              <p className="product-price">
                {product.discountedPrice &&
                product.discountedPrice < product.price ? (
                  <>
                    <span className="original-price">
                      {Number(product.price).toFixed(2)} TL
                    </span>
                    <span className="discounted-price">
                      {Number(product.discountedPrice).toFixed(2)} TL
                    </span>
                  </>
                ) : (
                  <span className="current-price">
                    {Number(product.price).toFixed(2)} TL
                  </span>
                )}
              </p>
            </div>
          </div>
        </div>

        {/* Butonlar */}
        <div className="add-to-cart-modal-footer">
          <button className="btn-continue" onClick={handleContinueShopping}>
            <i className="fas fa-arrow-left me-2"></i>
            Alışverişe Devam Et
          </button>
          <button className="btn-go-to-cart" onClick={handleGoToCart}>
            <i className="fas fa-shopping-cart me-2"></i>
            Sepete Git
          </button>
        </div>
      </div>
    </div>
  );
}
