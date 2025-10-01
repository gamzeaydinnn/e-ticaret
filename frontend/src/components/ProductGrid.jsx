import React, { useEffect, useState, useMemo } from "react";
import { getAllProducts } from "../services/productService";
import { addToCart } from "../services/cartService";
import { useCartCount } from "../hooks/useCartCount";

export default function ProductGrid() {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error] = useState("");
  const { refresh: refreshCart } = useCartCount();

  // Tüm ürünleri göster (filtreleme kaldırıldı)
  const filteredProducts = useMemo(() => {
    return [...data];
  }, [data]);

  const handleAddToCart = async (productId) => {
    try {
      await addToCart(productId);
      refreshCart();
      // Success feedback
      const button = document.querySelector(`[data-product-id="${productId}"]`);
      if (button) {
        const originalText = button.innerHTML;
        button.innerHTML = '<i class="fas fa-check me-2"></i>Eklendi!';
        button.classList.add("btn-success");
        button.classList.remove("btn-primary");
        setTimeout(() => {
          button.innerHTML = originalText;
          button.classList.remove("btn-success");
          button.classList.add("btn-primary");
        }, 1500);
      }
    } catch (error) {
      console.error("Sepete ekleme hatası:", error);
    }
  };

  useEffect(() => {
    getAllProducts()
      .then(setData)
      .catch((e) => {
        console.error("API hatası, demo data kullanılıyor:", e);
        // Demo süpermarket ürünleri - API çalışmıyorsa
        setData([
          {
            id: 1,
            name: "Yeşil Cif Krem",
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
            name: "Ülker Altınbaşak Tahıl Cipsi",
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
            name: "Lipton Ice Tea Limon Aromalı",
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
            name: "Dana Kuşbaşı Et",
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
            name: "Nescafe 2'si 1 Arada",
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
            name: "Sek Kaşar Peyniri",
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
            name: "Mis Bulgur Pilavlık",
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
            name: "Coca Cola 330ml",
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
          {filteredProducts.map((p) => (
            <div key={p.id} className="col-lg-3 col-md-6 mb-4">
              <div
                className="product-card h-100 shadow-sm border-0"
                style={{
                  borderRadius: "12px",
                  transition: "transform 0.3s ease, box-shadow 0.3s ease",
                  backgroundColor: "white",
                }}
                onMouseEnter={(e) => {
                  e.target.style.transform = "translateY(-3px)";
                  e.target.style.boxShadow = "0 8px 25px rgba(0, 0, 0, 0.1)";
                }}
                onMouseLeave={(e) => {
                  e.target.style.transform = "translateY(0)";
                  e.target.style.boxShadow = "0 2px 8px rgba(0, 0, 0, 0.05)";
                }}
              >
                <div
                  style={{
                    height: 180,
                    borderTopLeftRadius: "12px",
                    borderTopRightRadius: "12px",
                    backgroundColor: "#f8f9fa",
                    position: "relative",
                  }}
                  className="d-flex align-items-center justify-content-center"
                >
                  {/* Favori butonu */}
                  <button
                    className="btn btn-link position-absolute top-0 end-0 m-2 p-1"
                    style={{ zIndex: 2, color: "#dee2e6" }}
                  >
                    <i className="far fa-heart"></i>
                  </button>

                  {p.imageUrl ? (
                    <img
                      src={p.imageUrl}
                      alt={p.name}
                      className="img-fluid"
                      style={{
                        maxHeight: "140px",
                        maxWidth: "140px",
                        objectFit: "contain",
                      }}
                    />
                  ) : (
                    <div
                      className="d-flex align-items-center justify-content-center"
                      style={{
                        width: "100px",
                        height: "100px",
                        backgroundColor: "#e9ecef",
                        borderRadius: "8px",
                        fontSize: "2rem",
                      }}
                    >
                      🛒
                    </div>
                  )}
                </div>

                <div className="card-body p-3">
                  <h6
                    className="card-title mb-2"
                    style={{
                      minHeight: "40px",
                      fontSize: "0.9rem",
                      fontWeight: "400",
                      lineHeight: "1.3",
                    }}
                  >
                    {p.name}
                  </h6>

                  {/* Fiyat Bilgileri */}
                  <div className="price-info mb-2">
                    {p.originalPrice ? (
                      <div>
                        <div className="d-flex align-items-center mb-1">
                          <span
                            className="text-decoration-line-through text-muted me-2"
                            style={{ fontSize: "0.85rem" }}
                          >
                            {p.originalPrice.toFixed(2)} TL
                          </span>
                        </div>
                        {p.badge && (
                          <div className="mb-1">
                            <span
                              className="badge"
                              style={{
                                backgroundColor:
                                  p.badge === "İndirim" ? "#ff6b35" : "#28a745",
                                color: "white",
                                fontSize: "0.7rem",
                                padding: "2px 6px",
                              }}
                            >
                              <i
                                className={`fas ${
                                  p.badge === "İndirim"
                                    ? "fa-percentage"
                                    : "fa-star"
                                } me-1`}
                              ></i>
                              {p.badge}
                            </span>
                          </div>
                        )}
                        {p.specialPrice && (
                          <div
                            className="fw-bold"
                            style={{ fontSize: "0.85rem" }}
                          >
                            {p.specialPrice.toFixed(2)} TL
                          </div>
                        )}
                      </div>
                    ) : (
                      <div className="fw-bold" style={{ fontSize: "0.9rem" }}>
                        {p.price.toFixed(2)} TL
                      </div>
                    )}
                  </div>

                  <div className="d-flex align-items-center justify-content-between mt-3">
                    <div></div>

                    <button
                      className="btn btn-outline-warning btn-sm"
                      data-product-id={p.id}
                      style={{
                        borderRadius: "20px",
                        padding: "6px 12px",
                        fontSize: "0.8rem",
                        fontWeight: "600",
                        border: "2px solid #ff6b35",
                        color: "#ff6b35",
                        transition: "all 0.3s ease",
                      }}
                      onClick={() => handleAddToCart(p.id)}
                      onMouseEnter={(e) => {
                        e.target.style.backgroundColor = "#ff6b35";
                        e.target.style.color = "white";
                        e.target.style.transform = "scale(1.05)";
                      }}
                      onMouseLeave={(e) => {
                        e.target.style.backgroundColor = "transparent";
                        e.target.style.color = "#ff6b35";
                        e.target.style.transform = "scale(1)";
                      }}
                    >
                      <i className="fas fa-plus me-1"></i>
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
