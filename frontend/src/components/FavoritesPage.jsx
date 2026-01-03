import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useFavorite } from "../hooks/useFavorite";
import { ProductService } from "../services/productService";

/**
 * FavoritesPage.jsx
 * - Temiz, BOM içermeyen bir dosya olacak şekilde hazırlandı.
 * - useFavorite hook'u favorites dizisini ve removeFavorite fonksiyonunu sağlamalı.
 * - ProductService.list() metodu tüm ürünleri döndürmeli veya hata atmalı.
 */

const FavoritesPage = () => {
  const [productsMap, setProductsMap] = useState({}); // { [id]: product }
  const [productsLoading, setProductsLoading] = useState(true);
  const {
    favorites = [],
    loading: favoritesLoading,
    removeFavorite,
  } = useFavorite();

  useEffect(() => {
    loadProductData();
    // favorites değiştikçe ürünleri tekrar yükle
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [favorites]);

  const loadProductData = async () => {
    setProductsLoading(true);

    try {
      let allProducts = [];

      try {
        // Gerçek servis varsa buradan çekecek
        allProducts = await ProductService.list();
      } catch (err) {
        console.log("Backend API hatası, sahte veri kullanılıyor:", err);
        // Servis yoksa veya hata olursa test amaçlı fallback (sahte veri) - ProductGrid'deki aynı veri
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
            categoryId: 2,
            categoryName: "Süt ve Süt Ürünleri",
            imageUrl: "/images/pinar-nestle-sut.jpg",
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
            categoryId: 7,
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
        ];
      }

      const map = {};
      for (const id of favorites) {
        const p = allProducts.find((item) => item.id === id);
        if (p) map[id] = p;
      }
      setProductsMap(map);
    } catch (error) {
      console.error("Ürün verileri yüklenirken hata:", error);
      setProductsMap({});
    } finally {
      setProductsLoading(false);
    }
  };

  const handleRemoveFavorite = async (productId) => {
    try {
      const result = await removeFavorite(productId);
      // useFavorite removeFavorite fonksiyonu success flag döndürmüyorsa bu kontrolü kaldırabilirsiniz
      if (result && result.success === false) {
        alert("Favori silinirken bir hata oluştu.");
      }
    } catch (error) {
      console.error("Favori silinirken hata:", error);
      alert("Favori silinirken bir hata oluştu.");
    }
  };

  const isLoading = favoritesLoading || productsLoading;

  return (
    <div
      style={{
        minHeight: "100vh",
        padding: "2rem 0",
        background:
          "linear-gradient(135deg,#fff3e0 0%,#ffe0b2 50%,#ffcc80 100%)",
      }}
    >
      <div className="container">
        <div className="row">
          <div className="col-md-10 mx-auto">
            <div
              className="card shadow-lg border-0"
              style={{ borderRadius: 20 }}
            >
              <div
                className="card-header d-flex justify-content-between align-items-center border-0 text-white"
                style={{
                  background: "linear-gradient(45deg,#e91e63,#ad1457,#880e4f)",
                  borderTopLeftRadius: 20,
                  borderTopRightRadius: 20,
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
                    borderRadius: 50,
                  }}
                >
                  {favorites.length} Ürün
                </span>
              </div>

              <div className="card-body" style={{ padding: "2rem" }}>
                {isLoading ? (
                  <div className="text-center py-5">
                    <div
                      className="spinner-border mb-3"
                      role="status"
                      style={{ width: "3rem", height: "3rem" }}
                    >
                      <span className="visually-hidden">Loading...</span>
                    </div>
                    <p className="text-muted fw-bold">
                      Favoriler yükleniyor...
                    </p>
                  </div>
                ) : favorites.length > 0 ? (
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

                    {favorites.map((productId) => {
                      const product = productsMap[productId];
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
                                  width: 80,
                                  height: 80,
                                  objectFit: "contain",
                                  borderRadius: 15,
                                  padding: 8,
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
                                    borderRadius: 8,
                                    background:
                                      "linear-gradient(135deg,#e91e63,#ad1457)",
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
                                      product.specialPrice ?? product.price
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
                                onClick={() => handleRemoveFavorite(productId)}
                                style={{ borderRadius: 10 }}
                              >
                                <i className="fas fa-trash"></i>
                              </button>
                              <Link
                                to={`/product/${productId}`}
                                className="btn btn-outline-primary btn-sm"
                                style={{ borderRadius: 10 }}
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
                    <Link
                      to="/"
                      className="btn btn-lg fw-semibold"
                      style={{
                        borderRadius: 25,
                        padding: "12px 30px",
                        background: "linear-gradient(135deg,#e91e63,#ad1457)",
                        color: "white",
                        border: "none",
                      }}
                    >
                      <i className="fas fa-shopping-bag me-2"></i>Alışverişe
                      Başla
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
