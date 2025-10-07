import React, { useState, useEffect } from "react";
import { FavoriteService } from "../services/favoriteService";
import { ProductService } from "../services/productService";
import { useAuth } from "../contexts/AuthContext";
import { Link } from "react-router-dom";

const FavoritesPage = () => {
  const [favoriteItems, setFavoriteItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [products, setProducts] = useState({});
  const { user } = useAuth();

  useEffect(() => {
    loadFavoriteData();
  }, [user]); // user değiştiğinde yeniden yükle

  const loadFavoriteData = async () => {
    setLoading(true);

    try {
      let favoriteIds = [];

      if (user) {
        // Kayıtlı kullanıcı için backend'den favorileri getir
        try {
          const favorites = await FavoriteService.getFavorites();
          favoriteIds = favorites.map((f) => f.id);
        } catch (error) {
          console.log("Backend bağlantısı yok, localStorage kullanılıyor");
          favoriteIds = FavoriteService.getGuestFavorites();
        }
      } else {
        // Misafir kullanıcı için localStorage'dan favorileri getir
        favoriteIds = FavoriteService.getGuestFavorites();
      }

      setFavoriteItems(favoriteIds);

      // Ürün detaylarını getir - sahte verilerle
      try {
        let allProducts = [];

        try {
          allProducts = await ProductService.list();
        } catch (error) {
          // Sahte ürün verileri - ProductGrid'dekilerle aynı
          allProducts = [
            {
              id: 1,
              name: "Cif Krem Doğanın Gücü Hijyen 675Ml",
              description: "Yüzey temizleyici, çok amaçlı temizlik",
              price: 204.95,
              originalPrice: 229.95,
              categoryId: 7,
              categoryName: "Temizlik",
              imageUrl: "/images/yeşil-cif-krem.jpg",
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
              specialPrice: 45.9,
            },
            {
              id: 8,
              name: "Pınar Süt 1L",
              description: "Tam yağlı UHT süt 1 litre",
              price: 28.5,
              categoryId: 3,
              categoryName: "Süt Ürünleri",
              imageUrl: "/images/pınar-süt.jpg",
              specialPrice: 28.5,
            },
            {
              id: 9,
              name: "Sek Kaşar Peyniri 200 G",
              description: "Dilimli kaşar peyniri 200g",
              price: 75.9,
              categoryId: 3,
              categoryName: "Süt Ürünleri",
              imageUrl: "/images/sek-kasar-peyniri-200-gr-38be46-1650x1650.jpg",
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
              specialPrice: 32.9,
            },
            {
              id: 11,
              name: "Coca-Cola Orijinal Tat Kutu 330ml",
              description: "Kola gazlı içecek kutu",
              price: 12.5,
              categoryId: 5,
              categoryName: "İçecekler",
              imageUrl: "/images/coca-cola.jpg",
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
              specialPrice: 28.9,
            },
          ];
        }

        const productData = {};
        for (const productId of favoriteIds) {
          const product = allProducts.find((p) => p.id === productId);
          if (product) {
            productData[productId] = product;
          }
        }
        setProducts(productData);
      } catch (error) {
        console.error("Ürün verileri yüklenirken hata:", error);
      }
    } catch (error) {
      console.error("Favori verileri yüklenirken hata:", error);
    } finally {
      setLoading(false);
    }
  };

  const removeFavorite = async (productId) => {
    try {
      if (user) {
        await FavoriteService.removeFavorite(productId);
      } else {
        FavoriteService.removeFromGuestFavorites(productId);
      }
      await loadFavoriteData();
    } catch (error) {
      console.error("Favori silinirken hata:", error);
      alert("Favori silinirken bir hata oluştu.");
    }
  };

  return (
    <div
      style={{
        minHeight: "100vh",
        background:
          "linear-gradient(135deg, #fff3e0 0%, #ffe0b2 50%, #ffcc80 100%)",
        paddingTop: "2rem",
        paddingBottom: "2rem",
      }}
    >
      <div className="container">
        <div className="row">
          <div className="col-md-10 mx-auto">
            <div
              className="card shadow-lg border-0"
              style={{ borderRadius: "20px" }}
            >
              <div
                className="card-header text-white d-flex justify-content-between align-items-center border-0"
                style={{
                  background:
                    "linear-gradient(45deg, #e91e63, #ad1457, #880e4f)",
                  borderTopLeftRadius: "20px",
                  borderTopRightRadius: "20px",
                  padding: "1.5rem 2rem",
                }}
              >
                <h3 className="mb-0 fw-bold">
                  <i className="fas fa-heart me-3"></i>Favorilerim
                </h3>
                <span
                  className="badge fs-6 fw-bold px-3 py-2"
                  style={{
                    backgroundColor: "rgba(255,255,255,0.2)",
                    borderRadius: "50px",
                  }}
                >
                  {favoriteItems.length} Ürün
                </span>
              </div>

              <div className="card-body" style={{ padding: "2rem" }}>
                {loading ? (
                  <div className="text-center py-5">
                    <div
                      className="spinner-border mb-3"
                      role="status"
                      style={{
                        color: "#e91e63",
                        width: "3rem",
                        height: "3rem",
                      }}
                    >
                      <span className="visually-hidden">Loading...</span>
                    </div>
                    <p className="text-muted fw-bold">
                      Favoriler yükleniyor...
                    </p>
                  </div>
                ) : favoriteItems.length > 0 ? (
                  <>
                    <div
                      className="row pb-3 mb-4 fw-bold"
                      style={{
                        borderBottom: "2px solid #fce4ec",
                        color: "#e91e63",
                      }}
                    >
                      <div className="col-md-8">
                        <h6>
                          <i className="fas fa-heart me-2"></i>Ürün
                        </h6>
                      </div>
                      <div className="col-md-2 text-center">
                        <h6>
                          <i className="fas fa-tag me-2"></i>Fiyat
                        </h6>
                      </div>
                      <div className="col-md-2 text-center">
                        <h6>
                          <i className="fas fa-cog me-2"></i>İşlemler
                        </h6>
                      </div>
                    </div>

                    {favoriteItems.map((productId) => {
                      const product = products[productId];
                      return (
                        <div
                          key={productId}
                          className="row align-items-center py-3 border-bottom"
                        >
                          <div className="col-md-8">
                            <div className="d-flex align-items-center">
                              <img
                                src={
                                  product?.imageUrl || "/images/placeholder.png"
                                }
                                alt={product?.name || "Ürün"}
                                style={{
                                  width: "80px",
                                  height: "80px",
                                  objectFit: "contain",
                                  borderRadius: "15px",
                                  background:
                                    "linear-gradient(135deg, #f8f9fa, #e9ecef)",
                                  padding: "8px",
                                  border: "2px solid #fce4ec",
                                }}
                                className="me-3"
                                onError={(e) => {
                                  e.target.src = "/images/placeholder.png";
                                }}
                              />
                              <div>
                                <h6 className="fw-bold mb-1">
                                  {product?.name || "Yükleniyor..."}
                                </h6>
                                <p className="text-muted small mb-0">
                                  {product?.categoryName || ""}
                                </p>
                                <span
                                  className="badge text-white fw-bold px-2 py-1"
                                  style={{
                                    fontSize: "0.7rem",
                                    borderRadius: "8px",
                                    background:
                                      "linear-gradient(135deg, #e91e63, #ad1457)",
                                  }}
                                >
                                  <i className="fas fa-heart me-1"></i>Favorim
                                </span>
                              </div>
                            </div>
                          </div>
                          <div className="col-md-2 text-center">
                            <div className="d-flex flex-column align-items-center">
                              <p className="fw-bold text-success mb-1">
                                ₺
                                {product
                                  ? (
                                      product.specialPrice || product.price
                                    )?.toFixed(2)
                                  : "0.00"}
                              </p>
                              {product?.specialPrice && (
                                <p className="text-muted text-decoration-line-through small mb-0">
                                  ₺{product.price?.toFixed(2)}
                                </p>
                              )}
                            </div>
                          </div>
                          <div className="col-md-2 text-center">
                            <div className="d-flex justify-content-center gap-2">
                              <button
                                className="btn btn-outline-danger btn-sm"
                                onClick={() => removeFavorite(productId)}
                                style={{ borderRadius: "10px" }}
                              >
                                <i className="fas fa-trash"></i>
                              </button>
                              <Link
                                to={`/product/${productId}`}
                                className="btn btn-outline-primary btn-sm"
                                style={{ borderRadius: "10px" }}
                              >
                                <i className="fas fa-eye"></i>
                              </Link>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </>
                ) : (
                  <div className="text-center py-5">
                    <i
                      className="fas fa-heart text-muted mb-3"
                      style={{ fontSize: "4rem", opacity: 0.3 }}
                    ></i>
                    <h5 className="text-muted fw-bold mb-2">
                      Henüz Favori Ürününüz Yok
                    </h5>
                    <p className="text-muted mb-4">
                      Beğendiğiniz ürünleri kalp butonuna tıklayarak
                      favorilerinize ekleyebilirsiniz!
                    </p>

                    {/* Kampanya Bilgisi */}
                    <div
                      className="alert border-0 mb-4 mx-auto"
                      style={{
                        background: "linear-gradient(135deg, #fff3e0, #ffcc80)",
                        borderRadius: "15px",
                        border: "2px solid #ff6b35",
                        maxWidth: "500px",
                      }}
                    >
                      <div className="d-flex align-items-center text-start">
                        <i
                          className="fas fa-fire text-warning me-3"
                          style={{ fontSize: "1.5rem" }}
                        ></i>
                        <div>
                          <h6
                            className="mb-1 fw-bold"
                            style={{ color: "#ff6b35" }}
                          >
                            🎉 Özel Kampanya! İlk alışverişinize %25 indirim!
                          </h6>
                          <small className="text-muted">
                            Favori ürünlerinizi sepete ekleyin ve avantajlı
                            fiyatlardan yararlanın
                          </small>
                        </div>
                      </div>
                    </div>
                    <p className="text-muted mb-4">
                      Beğendiğiniz ürünleri kalp butonuna tıklayarak
                      favorilerinize ekleyebilirsiniz!
                    </p>
                    <Link
                      to="/"
                      className="btn btn-lg border-2 fw-semibold"
                      style={{
                        borderRadius: "25px",
                        padding: "12px 30px",
                        background: "linear-gradient(135deg, #e91e63, #ad1457)",
                        color: "white",
                        border: "none",
                      }}
                    >
                      <i className="fas fa-shopping-bag me-2"></i>
                      Alışverişe Başla
                    </Link>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default FavoritesPage;
