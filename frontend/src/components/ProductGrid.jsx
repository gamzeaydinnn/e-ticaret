import React, { useEffect, useState, useMemo } from "react";
import { ProductService } from "../services/productService";

export default function ProductGrid() {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error] = useState("");
  const [selectedProduct, setSelectedProduct] = useState(null);
  const [showModal, setShowModal] = useState(false);

  // Tüm ürünleri göster (filtreleme kaldırıldı)
  const filteredProducts = useMemo(() => {
    return [...data];
  }, [data]);

  const handleAddToCart = async (productId) => {
    // Sepete ekleme işlemi için giriş sayfasına yönlendir
    window.location.href = "/cart";
  };

  const handleProductClick = (product, event) => {
    // Sepete ekle butonuna tıklanmışsa modal açma
    if (
      event.target.closest(".modern-add-btn") ||
      event.target.closest(".btn-favorite")
    ) {
      return;
    }

    setSelectedProduct(product);
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setSelectedProduct(null);
  };

  useEffect(() => {
    ProductService.list()
      .then(setData)
      .catch((e) => {
        console.error("API hatası, demo data kullanılıyor:", e);
        // Demo süpermarket ürünleri - API çalışmıyorsa
        setData([
          {
            id: 1,
            name: "Cif Krem Doğanın Gücü Hijyen 675Ml",
            description: "Yüzey temizleyici, çok amaçlı temizlik",
            price: 204.95,
            originalPrice: 229.95,
            categoryId: 7,
            categoryName: "Temizlik",
            imageUrl: "/images/yeşil-cif-krem.jpg",
            isNew: true,
            discountPercentage: 11,
            rating: 4.5,
            reviewCount: 128,
            badge: "İndirim",
            specialPrice: 129.95,
          },
          {
            id: 2,
            name: "Ülker Altınbaşak Tahıl Cipsi 50 Gr",
            description: "Taco aromalı & çıtır tahıl cipsi",
            price: 18.0,
            categoryId: 6,
            categoryName: "Atıştırmalık",
            imageUrl: "/images/tahil-cipsi.jpg",
            isNew: false,
            discountPercentage: 17,
            rating: 4.8,
            reviewCount: 256,
            badge: "İndirim",
            specialPrice: 14.9,
          },
          {
            id: 3,
            name: "Lipton Ice Tea Limon 330 Ml",
            description: "Soğuk çay, kutu 330ml",
            price: 60.0,
            categoryId: 5,
            categoryName: "İçecekler",
            imageUrl: "/images/lipton-ice-tea.jpg",
            isNew: false,
            discountPercentage: 32,
            rating: 4.2,
            reviewCount: 89,
            badge: "İndirim",
            specialPrice: 40.9,
          },
          {
            id: 4,
            name: "Dana But Tas Kebaplık Et Çiftlik Kg",
            description: "Taze dana eti, kuşbaşı doğranmış 500g",
            price: 375.95,
            originalPrice: 429.95,
            categoryId: 2,
            categoryName: "Et & Tavuk & Balık",
            imageUrl: "/images/dana-kusbasi.jpg",
            isNew: true,
            discountPercentage: 26,
            rating: 4.7,
            reviewCount: 67,
            badge: "İndirim",
            specialPrice: 279.0,
          },
          {
            id: 5,
            name: "Kuzu İncik Kg",
            description: "Taze kuzu incik, kilogram",
            price: 1399.95,
            categoryId: 2,
            categoryName: "Et & Tavuk & Balık",
            imageUrl: "/images/kuzu-incik.webp",
            isNew: false,
            discountPercentage: 0,
            rating: 4.4,
            reviewCount: 195,
            badge: "İyi Fiyat",
            specialPrice: 699.95,
          },
          {
            id: 6,
            name: "Nescafe 2si 1 Arada Sütlü Köpüklü 15 x 10g",
            description: "Kahve karışımı, paket 15 x 10g",
            price: 145.55,
            originalPrice: 169.99,
            categoryId: 5,
            categoryName: "İçecekler",
            imageUrl: "/images/nescafe.jpg",
            isNew: false,
            discountPercentage: 14,
            rating: 4.3,
            reviewCount: 143,
            badge: "İndirim",
            specialPrice: 84.5,
          },
          {
            id: 7,
            name: "Domates Kg",
            description: "Taze domates, kilogram",
            price: 45.9,
            categoryId: 1,
            categoryName: "Meyve & Sebze",
            imageUrl: "/images/domates.webp",
            isNew: true,
            discountPercentage: 0,
            rating: 4.9,
            reviewCount: 312,
          },
          {
            id: 8,
            name: "Pınar Süt 1L",
            description: "Tam yağlı UHT süt 1 litre",
            price: 28.5,
            categoryId: 3,
            categoryName: "Süt Ürünleri",
            imageUrl: "/images/pınar-süt.jpg",
            isNew: false,
            discountPercentage: 0,
            rating: 4.6,
            reviewCount: 234,
          },
          {
            id: 9,
            name: "Sek Kaşar Peyniri 200 G",
            description: "Dilimli kaşar peyniri 200g",
            price: 75.9,
            categoryId: 3,
            categoryName: "Süt Ürünleri",
            imageUrl: "/images/sek-kasar-peyniri-200-gr-38be46-1650x1650.jpg",
            isNew: false,
            discountPercentage: 15,
            rating: 4.4,
            reviewCount: 156,
            badge: "İndirim",
            specialPrice: 64.5,
          },
          {
            id: 10,
            name: "Mis Bulgur Pilavlık 1Kg",
            description: "Birinci sınıf bulgur 1kg",
            price: 32.9,
            categoryId: 4,
            categoryName: "Temel Gıda",
            imageUrl: "/images/bulgur.png",
            isNew: true,
            discountPercentage: 0,
            rating: 4.7,
            reviewCount: 89,
          },
          {
            id: 11,
            name: "Coca-Cola Orijinal Tat Kutu 330ml",
            description: "Kola gazlı içecek kutu",
            price: 12.5,
            categoryId: 5,
            categoryName: "İçecekler",
            imageUrl: "/images/coca-cola.jpg",
            isNew: false,
            discountPercentage: 20,
            rating: 4.2,
            reviewCount: 445,
            badge: "İndirim",
            specialPrice: 10.0,
          },
          {
            id: 12,
            name: "Salatalık Kg",
            description: "Taze salatalık, kilogram",
            price: 28.9,
            categoryId: 1,
            categoryName: "Meyve & Sebze",
            imageUrl: "/images/salatalik.jpg",
            isNew: false,
            discountPercentage: 0,
            rating: 4.3,
            reviewCount: 67,
          },
        ]);
      })
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="text-center py-5">
        <div
          className="spinner-border mb-3"
          role="status"
          style={{ color: "#ff8f00", width: "3rem", height: "3rem" }}
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
            backgroundColor: "#fff8f0",
            width: "120px",
            height: "120px",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <i
            className="fas fa-box-open text-warning"
            style={{ fontSize: "3rem" }}
          ></i>
        </div>
        <h4 className="text-warning fw-bold mb-3">Henüz Ürün Yok</h4>
        <p className="text-muted fs-5">
          Yakında harika ürünlerle karşınızda olacağız!
        </p>
      </div>
    );
  }

  return (
    <div>
      {/* Sonuç Bilgisi */}
      <div className="text-center mb-4">
        <p className="text-muted mb-0">
          <strong>{filteredProducts.length}</strong> ürün listeleniyor
        </p>
      </div>

      {/* Ürün Grid */}
      {filteredProducts.length === 0 ? (
        <div className="text-center py-5">
          <div
            className="p-4 rounded-circle mx-auto mb-4 shadow-lg"
            style={{
              backgroundColor: "#fff8f0",
              width: "120px",
              height: "120px",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <i
              className="fas fa-search text-warning"
              style={{ fontSize: "3rem" }}
            ></i>
          </div>
          <h4 className="text-warning fw-bold mb-3">
            Aradığınız Kriterlerde Ürün Bulunamadı
          </h4>
          <p className="text-muted fs-5">
            Lütfen filtreleri değiştirerek tekrar deneyin
          </p>
        </div>
      ) : (
        <div className="row">
          {filteredProducts.map((p, index) => (
            <div key={p.id} className="col-lg-3 col-md-6 mb-4">
              <div
                className="modern-product-card h-100"
                style={{
                  background: `linear-gradient(145deg, #ffffff, #f8f9fa)`,
                  borderRadius: "20px",
                  border: "1px solid rgba(255, 107, 53, 0.1)",
                  overflow: "hidden",
                  position: "relative",
                  animation: `fadeInUp 0.6s ease ${index * 0.1}s both`,
                  cursor: "pointer",
                  minHeight: "520px",
                  display: "flex",
                  flexDirection: "column",
                }}
                onClick={(e) => handleProductClick(p, e)}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform =
                    "translateY(-8px) scale(1.02)";
                  e.currentTarget.style.boxShadow =
                    "0 20px 40px rgba(255, 107, 53, 0.15)";
                  e.currentTarget.style.borderColor = "rgba(255, 107, 53, 0.3)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = "translateY(0) scale(1)";
                  e.currentTarget.style.boxShadow =
                    "0 5px 15px rgba(0, 0, 0, 0.08)";
                  e.currentTarget.style.borderColor = "rgba(255, 107, 53, 0.1)";
                }}
              >
                {/* Badge - Sol Üst */}
                {p.badge && (
                  <div
                    className="position-absolute top-0 start-0 p-2"
                    style={{ zIndex: 3 }}
                  >
                    <span
                      className="badge-modern"
                      style={{
                        background:
                          p.badge === "İndirim"
                            ? "linear-gradient(135deg, #ff6b35, #ff8c00)"
                            : p.badge === "Yeni"
                            ? "linear-gradient(135deg, #28a745, #20c997)"
                            : "linear-gradient(135deg, #ffc107, #fd7e14)",
                        color: "white",
                        padding: "4px 12px",
                        borderRadius: "15px",
                        fontSize: "0.7rem",
                        fontWeight: "700",
                        textTransform: "uppercase",
                        boxShadow: "0 2px 8px rgba(255, 107, 53, 0.3)",
                        animation: "pulse 2s infinite",
                      }}
                    >
                      {p.badge}
                    </span>
                  </div>
                )}

                {/* Favori Butonu - Sağ Üst */}
                <div
                  className="position-absolute top-0 end-0 p-2"
                  style={{ zIndex: 3 }}
                >
                  <button
                    className="btn-favorite"
                    type="button"
                    style={{
                      background: "rgba(255, 255, 255, 0.9)",
                      border: "none",
                      borderRadius: "50%",
                      width: "35px",
                      height: "35px",
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                      color: "#ff6b35",
                      transition: "all 0.3s ease",
                      backdropFilter: "blur(10px)",
                    }}
                    onMouseEnter={(e) => {
                      e.target.style.transform = "scale(1.1)";
                      e.target.style.background = "#ff6b35";
                      e.target.style.color = "white";
                    }}
                    onMouseLeave={(e) => {
                      e.target.style.transform = "scale(1)";
                      e.target.style.background = "rgba(255, 255, 255, 0.9)";
                      e.target.style.color = "#ff6b35";
                    }}
                  >
                    <i className="far fa-heart"></i>
                  </button>
                </div>

                <div
                  className="product-image-container"
                  style={{
                    height: 200,
                    background: "linear-gradient(135deg, #f8f9fa, #e9ecef)",
                    position: "relative",
                    overflow: "hidden",
                  }}
                >
                  <div
                    className="image-wrapper d-flex align-items-center justify-content-center h-100"
                    style={{
                      transition: "all 0.4s ease",
                      position: "relative",
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.transform = "scale(1.1)";
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.transform = "scale(1)";
                    }}
                  >
                    {p.imageUrl ? (
                      <img
                        src={p.imageUrl}
                        alt={p.name}
                        className="product-image"
                        style={{
                          maxHeight: "160px",
                          maxWidth: "160px",
                          objectFit: "contain",
                          filter: "drop-shadow(0 4px 12px rgba(0,0,0,0.1))",
                          transition: "all 0.3s ease",
                        }}
                      />
                    ) : (
                      <div
                        className="d-flex align-items-center justify-content-center"
                        style={{
                          width: "120px",
                          height: "120px",
                          background:
                            "linear-gradient(135deg, #e9ecef, #dee2e6)",
                          borderRadius: "15px",
                          fontSize: "2.5rem",
                        }}
                      >
                        🛒
                      </div>
                    )}
                  </div>
                </div>

                <div
                  className="card-body p-4 d-flex flex-column"
                  style={{
                    background:
                      "linear-gradient(135deg, rgba(255,255,255,0.9), rgba(248,249,250,0.9))",
                    minHeight: "280px",
                  }}
                >
                  {/* Rating */}
                  {p.rating && (
                    <div className="d-flex align-items-center mb-2">
                      <div className="star-rating me-2">
                        {[...Array(5)].map((_, i) => (
                          <i
                            key={i}
                            className={`fas fa-star ${
                              i < Math.floor(p.rating)
                                ? "text-warning"
                                : "text-muted"
                            }`}
                            style={{ fontSize: "0.7rem" }}
                          ></i>
                        ))}
                      </div>
                      <small className="text-muted">({p.reviewCount})</small>
                    </div>
                  )}

                  <h6
                    className="product-title mb-3"
                    style={{
                      height: "50px",
                      fontSize: "0.95rem",
                      fontWeight: "600",
                      lineHeight: "1.4",
                      color: "#2c3e50",
                      display: "flex",
                      alignItems: "center",
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                    }}
                  >
                    {p.name}
                  </h6>

                  {/* Content Area - Flexible */}
                  <div className="content-area flex-grow-1 d-flex flex-column justify-content-between">
                    {/* Modern Fiyat Bilgileri */}
                    <div className="price-section mb-3">
                      {p.originalPrice ? (
                        <div className="price-container">
                          <div className="d-flex align-items-center mb-2">
                            <span
                              className="old-price me-2"
                              style={{
                                fontSize: "0.85rem",
                                textDecoration: "line-through",
                                color: "#6c757d",
                              }}
                            >
                              {p.originalPrice.toFixed(2)} TL
                            </span>
                            {p.discountPercentage > 0 && (
                              <span
                                className="discount-badge"
                                style={{
                                  background:
                                    "linear-gradient(135deg, #dc3545, #c82333)",
                                  color: "white",
                                  padding: "3px 8px",
                                  borderRadius: "12px",
                                  fontSize: "0.7rem",
                                  fontWeight: "700",
                                  animation: "bounce 2s infinite",
                                }}
                              >
                                -%{p.discountPercentage}
                              </span>
                            )}
                          </div>
                          <div
                            className="current-price"
                            style={{
                              fontSize: "1.4rem",
                              fontWeight: "800",
                              background:
                                "linear-gradient(135deg, #ff6b35, #ff8c00)",
                              backgroundClip: "text",
                              WebkitBackgroundClip: "text",
                              WebkitTextFillColor: "transparent",
                              display: "inline-block",
                              textShadow: "0 2px 4px rgba(255,107,53,0.3)",
                            }}
                          >
                            {p.price.toFixed(2)} TL
                          </div>
                        </div>
                      ) : (
                        <div
                          className="current-price"
                          style={{
                            fontSize: "1.4rem",
                            fontWeight: "800",
                            background:
                              "linear-gradient(135deg, #28a745, #20c997)",
                            backgroundClip: "text",
                            WebkitBackgroundClip: "text",
                            WebkitTextFillColor: "transparent",
                            display: "inline-block",
                          }}
                        >
                          {p.price.toFixed(2)} TL
                        </div>
                      )}
                    </div>

                    {/* Modern Action Buttons - Always at Bottom */}
                    <div className="action-buttons mt-auto">
                      <button
                        className="modern-add-btn"
                        data-product-id={p.id}
                        style={{
                          background:
                            "linear-gradient(135deg, #ff6b35, #ff8c00)",
                          border: "none",
                          borderRadius: "25px",
                          padding: "12px 24px",
                          fontSize: "0.9rem",
                          fontWeight: "700",
                          color: "white",
                          transition: "all 0.4s cubic-bezier(0.4, 0, 0.2, 1)",
                          boxShadow: "0 4px 15px rgba(255, 107, 53, 0.3)",
                          position: "relative",
                          overflow: "hidden",
                          width: "100%",
                          textTransform: "uppercase",
                          letterSpacing: "0.5px",
                        }}
                        onClick={() => handleAddToCart(p.id)}
                        onMouseEnter={(e) => {
                          e.target.style.background =
                            "linear-gradient(135deg, #ff8c00, #ffa500)";
                          e.target.style.transform =
                            "translateY(-2px) scale(1.02)";
                          e.target.style.boxShadow =
                            "0 8px 25px rgba(255, 107, 53, 0.4)";
                        }}
                        onMouseLeave={(e) => {
                          e.target.style.background =
                            "linear-gradient(135deg, #ff6b35, #ff8c00)";
                          e.target.style.transform = "translateY(0) scale(1)";
                          e.target.style.boxShadow =
                            "0 4px 15px rgba(255, 107, 53, 0.3)";
                        }}
                      >
                        <i className="fas fa-shopping-cart me-2"></i>
                        Sepete Ekle
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Product Detail Modal */}
      {showModal && selectedProduct && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
          onClick={(e) => e.target === e.currentTarget && closeModal()}
        >
          <div className="modal-dialog modal-lg modal-dialog-centered">
            <div
              className="modal-content border-0 shadow-lg"
              style={{ borderRadius: "20px" }}
            >
              <div className="modal-header border-0 p-0">
                <div
                  className="w-100 d-flex justify-content-between align-items-center p-3"
                  style={{
                    background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                    borderRadius: "20px 20px 0 0",
                  }}
                >
                  <h5 className="modal-title text-white fw-bold mb-0">
                    <i className="fas fa-info-circle me-2"></i>
                    Ürün Detayları
                  </h5>
                  <button
                    type="button"
                    className="btn btn-link text-white p-0 border-0"
                    onClick={closeModal}
                    style={{
                      fontSize: "1.5rem",
                      textDecoration: "none",
                      opacity: 0.9,
                      transition: "all 0.3s ease",
                    }}
                    onMouseEnter={(e) => {
                      e.target.style.opacity = "1";
                      e.target.style.transform = "scale(1.1)";
                    }}
                    onMouseLeave={(e) => {
                      e.target.style.opacity = "0.9";
                      e.target.style.transform = "scale(1)";
                    }}
                  >
                    <i className="fas fa-times"></i>
                  </button>
                </div>
              </div>

              <div className="modal-body px-4 pb-4">
                <div className="row align-items-center">
                  <div className="col-md-5 text-center">
                    <div
                      className="product-image-large mb-3"
                      style={{
                        background: "linear-gradient(135deg, #f8f9fa, #e9ecef)",
                        borderRadius: "20px",
                        padding: "30px",
                        height: "300px",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                      }}
                    >
                      {selectedProduct.imageUrl ? (
                        <img
                          src={selectedProduct.imageUrl}
                          alt={selectedProduct.name}
                          style={{
                            maxHeight: "240px",
                            maxWidth: "240px",
                            objectFit: "contain",
                            filter: "drop-shadow(0 8px 20px rgba(0,0,0,0.1))",
                          }}
                        />
                      ) : (
                        <div style={{ fontSize: "4rem", color: "#dee2e6" }}>
                          🛒
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="col-md-7">
                    <div className="product-details">
                      {selectedProduct.badge && (
                        <span
                          className="badge mb-3"
                          style={{
                            background:
                              selectedProduct.badge === "İndirim"
                                ? "linear-gradient(135deg, #ff6b35, #ff8c00)"
                                : "linear-gradient(135deg, #28a745, #20c997)",
                            color: "white",
                            padding: "6px 15px",
                            borderRadius: "20px",
                            fontSize: "0.8rem",
                            fontWeight: "700",
                          }}
                        >
                          {selectedProduct.badge}
                        </span>
                      )}

                      <h3
                        className="product-title mb-3"
                        style={{
                          fontWeight: "700",
                          color: "#2c3e50",
                          lineHeight: "1.3",
                        }}
                      >
                        {selectedProduct.name}
                      </h3>

                      {selectedProduct.rating && (
                        <div className="rating mb-3 d-flex align-items-center">
                          <div className="stars me-2">
                            {[...Array(5)].map((_, i) => (
                              <i
                                key={i}
                                className={`fas fa-star ${
                                  i < Math.floor(selectedProduct.rating)
                                    ? "text-warning"
                                    : "text-muted"
                                }`}
                              ></i>
                            ))}
                          </div>
                          <span className="text-muted">
                            ({selectedProduct.reviewCount || 0} değerlendirme)
                          </span>
                        </div>
                      )}

                      <p
                        className="product-description mb-4 text-muted"
                        style={{
                          fontSize: "1.1rem",
                          lineHeight: "1.6",
                        }}
                      >
                        {selectedProduct.description ||
                          "Bu ürün hakkında detaylı bilgi için müşteri hizmetleri ile iletişime geçebilirsiniz."}
                      </p>

                      <div className="price-info mb-4">
                        {selectedProduct.originalPrice ? (
                          <div>
                            <span
                              className="old-price me-3"
                              style={{
                                fontSize: "1.1rem",
                                textDecoration: "line-through",
                                color: "#6c757d",
                              }}
                            >
                              {selectedProduct.originalPrice.toFixed(2)} TL
                            </span>
                            <span
                              className="current-price"
                              style={{
                                fontSize: "2rem",
                                fontWeight: "800",
                                background:
                                  "linear-gradient(135deg, #ff6b35, #ff8c00)",
                                backgroundClip: "text",
                                WebkitBackgroundClip: "text",
                                WebkitTextFillColor: "transparent",
                              }}
                            >
                              {selectedProduct.price.toFixed(2)} TL
                            </span>
                          </div>
                        ) : (
                          <span
                            className="current-price"
                            style={{
                              fontSize: "2rem",
                              fontWeight: "800",
                              background:
                                "linear-gradient(135deg, #28a745, #20c997)",
                              backgroundClip: "text",
                              WebkitBackgroundClip: "text",
                              WebkitTextFillColor: "transparent",
                            }}
                          >
                            {selectedProduct.price.toFixed(2)} TL
                          </span>
                        )}
                      </div>

                      <div className="product-actions d-flex justify-content-center">
                        <button
                          className="btn btn-lg w-75"
                          style={{
                            background:
                              "linear-gradient(135deg, #ff6b35, #ff8c00)",
                            border: "none",
                            borderRadius: "25px",
                            padding: "15px 40px",
                            color: "white",
                            fontWeight: "700",
                            fontSize: "1.1rem",
                            boxShadow: "0 4px 15px rgba(255, 107, 53, 0.3)",
                            transition: "all 0.3s ease",
                            textTransform: "uppercase",
                            letterSpacing: "0.5px",
                          }}
                          onClick={() => {
                            // Modal'ı kapat ve sepet sayfasına yönlendir
                            closeModal();
                            window.location.href = "/cart";
                          }}
                          onMouseEnter={(e) => {
                            e.target.style.transform =
                              "translateY(-3px) scale(1.05)";
                            e.target.style.boxShadow =
                              "0 8px 25px rgba(255, 107, 53, 0.4)";
                          }}
                          onMouseLeave={(e) => {
                            e.target.style.transform = "translateY(0) scale(1)";
                            e.target.style.boxShadow =
                              "0 4px 15px rgba(255, 107, 53, 0.3)";
                          }}
                        >
                          <i className="fas fa-shopping-cart me-2"></i>
                          Sepete Ekle
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
