import React, { useState, useEffect } from "react";
import { useCartCount } from "../hooks/useCartCount";
import { Link, useNavigate } from "react-router-dom";
import { CartService } from "../services/cartService";
import { ProductService } from "../services/productService";
import { useAuth } from "../contexts/AuthContext";

const CartPage = () => {
  const { count: cartCount, refresh: refreshCartCount } = useCartCount();
  const navigate = useNavigate();
  const [cartItems, setCartItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [products, setProducts] = useState({});
  const { user } = useAuth();

  useEffect(() => {
    loadCartData();
  }, []);

  const loadCartData = async () => {
    setLoading(true);

    try {
      let items = [];

      if (user) {
        // Kayıtlı kullanıcı için backend'den sepeti getir
        try {
          items = await CartService.getCartItems();
        } catch (error) {
          console.log("Backend bağlantısı yok, localStorage kullanılıyor");
          items = CartService.getGuestCart();
        }
      } else {
        // Misafir kullanıcı için localStorage'dan sepeti getir
        items = CartService.getGuestCart();
      }

      setCartItems(items);

      // Ürün detaylarını getir - sahte verilerle test
      try {
        let allProducts = [];
        let productData = {};

        try {
          allProducts = await ProductService.list();
        } catch (error) {
          // Sahte ürün verileri - ProductGrid'dekilerle TAMAMEN aynı
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

        for (const item of items) {
          const product = allProducts.find((p) => p.id == item.productId); // == kullandım çünkü tip uyumsuzluğu olabilir

          if (product) {
            productData[item.productId] = {
              ...product,
              imageUrl: product.imageUrl || `/images/placeholder.png`,
            };
          } else {
            // Ürün bulunamadıysa placeholder kullan
            productData[item.productId] = {
              id: item.productId,
              name: "Bilinmeyen Ürün",
              price: 0,
              category: "Genel",
              imageUrl: "/images/placeholder.png",
            };
          }
        }

        console.log("💾 Final productData:", productData);
        setProducts(productData);
      } catch (error) {
        console.error("Ürün bilgileri alınırken hata:", error);
      }
    } catch (error) {
      console.error("Sepet verileri yüklenirken hata:", error);
    } finally {
      setLoading(false);
    }
  };

  const updateQuantity = async (item, newQuantity) => {
    if (newQuantity < 1) {
      removeItem(item);
      return;
    }

    try {
      if (user) {
        try {
          await CartService.updateItem(item.id, item.productId, newQuantity);
        } catch (error) {
          // Backend hatası durumunda localStorage'e fallback
          CartService.updateGuestCartItem(item.productId, newQuantity);
        }
      } else {
        CartService.updateGuestCartItem(item.productId, newQuantity);
      }
      await loadCartData();
      await refreshCartCount();
    } catch (error) {
      console.error("Miktar güncellenirken hata:", error);
      alert("Miktar güncellenirken bir hata oluştu.");
    }
  };

  const removeItem = async (item) => {
    try {
      if (user) {
        try {
          await CartService.removeItem(item.id, item.productId);
        } catch (error) {
          // Backend hatası durumunda localStorage'e fallback
          CartService.removeFromGuestCart(item.productId);
        }
      } else {
        CartService.removeFromGuestCart(item.productId);
      }
      await loadCartData();
      await refreshCartCount();
    } catch (error) {
      console.error("Ürün silinirken hata:", error);
      alert("Ürün silinirken bir hata oluştu.");
    }
  };

  const getTotalPrice = () => {
    return cartItems.reduce((total, item) => {
      const product = products[item.productId];
      const price = product ? product.specialPrice || product.price : 0;
      return total + price * item.quantity;
    }, 0);
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
                    "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                  borderTopLeftRadius: "20px",
                  borderTopRightRadius: "20px",
                  padding: "1.5rem 2rem",
                }}
              >
                <h3 className="mb-0 fw-bold">
                  <i className="fas fa-shopping-cart me-3"></i>Sepetim
                </h3>
                <span
                  className="badge fs-6 fw-bold px-3 py-2"
                  style={{
                    backgroundColor: "rgba(255,255,255,0.2)",
                    borderRadius: "50px",
                  }}
                >
                  {cartItems.reduce((total, item) => total + item.quantity, 0)}{" "}
                  Ürün
                </span>
              </div>
              <div className="card-body" style={{ padding: "2rem" }}>
                {cartItems.length > 0 || loading ? (
                  <>
                    <div
                      className="row pb-3 mb-4 fw-bold text-warning"
                      style={{ borderBottom: "2px solid #ffe0b2" }}
                    >
                      <div className="col-md-8">
                        <h6>
                          <i className="fas fa-box me-2"></i>Ürün
                        </h6>
                      </div>
                      <div className="col-md-2 text-center">
                        <h6>
                          <i className="fas fa-sort-numeric-up me-2"></i>Adet
                        </h6>
                      </div>
                      <div className="col-md-2 text-end">
                        <h6>
                          <i className="fas fa-tag me-2"></i>Fiyat
                        </h6>
                      </div>
                    </div>

                    {loading ? (
                      <div className="text-center py-5">
                        <div
                          className="spinner-border mb-3"
                          role="status"
                          style={{
                            color: "#ff8f00",
                            width: "3rem",
                            height: "3rem",
                          }}
                        >
                          <span className="visually-hidden">Loading...</span>
                        </div>
                        <p className="text-muted fw-bold">
                          Sepet detayları yükleniyor...
                        </p>
                      </div>
                    ) : cartItems.length > 0 ? (
                      <>
                        {cartItems.map((item) => {
                          const product = products[item.productId];
                          return (
                            <div
                              key={item.id || item.productId}
                              className="row align-items-center py-3 border-bottom"
                            >
                              <div className="col-md-8">
                                <div className="d-flex align-items-center">
                                  <img
                                    src={
                                      product?.imageUrl ||
                                      "/images/placeholder.png"
                                    }
                                    alt={product?.name || "Ürün"}
                                    style={{
                                      width: "80px",
                                      height: "80px",
                                      objectFit: "contain",
                                      borderRadius: "10px",
                                      background:
                                        "linear-gradient(135deg, #f8f9fa, #e9ecef)",
                                      padding: "8px",
                                      border: "2px solid #ffe0b2",
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
                                      {product?.category || ""}
                                    </p>
                                    <p className="text-warning fw-bold mb-0">
                                      ₺
                                      {product?.specialPrice ||
                                        product?.price ||
                                        "0.00"}
                                    </p>
                                  </div>
                                </div>
                              </div>
                              <div className="col-md-2 text-center">
                                <div className="d-flex align-items-center justify-content-center">
                                  <button
                                    className="btn btn-outline-warning btn-sm me-2"
                                    onClick={() =>
                                      updateQuantity(item, item.quantity - 1)
                                    }
                                    style={{
                                      width: "30px",
                                      height: "30px",
                                      borderRadius: "50%",
                                    }}
                                  >
                                    -
                                  </button>
                                  <span className="fw-bold mx-2">
                                    {item.quantity}
                                  </span>
                                  <button
                                    className="btn btn-outline-warning btn-sm ms-2"
                                    onClick={() =>
                                      updateQuantity(item, item.quantity + 1)
                                    }
                                    style={{
                                      width: "30px",
                                      height: "30px",
                                      borderRadius: "50%",
                                    }}
                                  >
                                    +
                                  </button>
                                </div>
                              </div>
                              <div className="col-md-2 text-end">
                                <div className="d-flex flex-column align-items-end">
                                  <p className="fw-bold text-warning mb-1">
                                    ₺
                                    {product
                                      ? (
                                          (product.specialPrice ||
                                            product.price) * item.quantity
                                        ).toFixed(2)
                                      : "0.00"}
                                  </p>
                                  <button
                                    className="btn btn-link text-danger p-0"
                                    onClick={() => {
                                      console.log("Delete clicked!", item);
                                      removeItem(item);
                                    }}
                                  >
                                    <i className="fas fa-trash"></i>
                                  </button>
                                </div>
                              </div>
                            </div>
                          );
                        })}
                      </>
                    ) : (
                      <div className="text-center py-5">
                        <i
                          className="fas fa-shopping-cart text-warning mb-3"
                          style={{ fontSize: "3rem" }}
                        ></i>
                        <h5 className="text-warning fw-bold mb-2">
                          Sepetiniz Boş
                        </h5>
                        <p className="text-muted">
                          Alışverişe başlamak için ürünleri keşfedin!
                        </p>
                      </div>
                    )}

                    <div className="row mt-4">
                      <div className="col-md-8"></div>
                      <div className="col-md-4">
                        <div
                          className="card border-0 shadow-lg"
                          style={{
                            borderRadius: "20px",
                            backgroundColor: "#fff8f0",
                          }}
                        >
                          <div
                            className="card-body"
                            style={{ padding: "1.5rem" }}
                          >
                            <h6 className="text-warning fw-bold mb-3">
                              <i className="fas fa-calculator me-2"></i>Sipariş
                              Özeti
                            </h6>
                            <hr style={{ borderColor: "#ffe0b2" }} />
                            <div className="d-flex justify-content-between mb-2">
                              <span>Ara Toplam:</span>
                              <span className="fw-bold">
                                ₺{getTotalPrice().toFixed(2)}
                              </span>
                            </div>
                            <div className="d-flex justify-content-between mb-2">
                              <span>Kargo:</span>
                              <span className="text-success fw-bold">
                                {getTotalPrice() > 100 ? "Ücretsiz" : "₺15.00"}
                              </span>
                            </div>
                            <hr style={{ borderColor: "#ffe0b2" }} />
                            <div className="d-flex justify-content-between fw-bold fs-5 mb-3">
                              <span>Toplam:</span>
                              <span className="text-warning">
                                ₺
                                {(
                                  getTotalPrice() +
                                  (getTotalPrice() > 100 ? 0 : 15)
                                ).toFixed(2)}
                              </span>
                            </div>
                            <button
                              className="btn btn-lg w-100 text-white fw-bold shadow-lg border-0"
                              style={{
                                background:
                                  cartItems.length > 0
                                    ? "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)"
                                    : "#cccccc",
                                borderRadius: "50px",
                                padding: "1rem",
                                cursor:
                                  cartItems.length > 0
                                    ? "pointer"
                                    : "not-allowed",
                              }}
                              onClick={() =>
                                cartItems.length > 0 && navigate("/payment")
                              }
                              disabled={cartItems.length === 0}
                            >
                              <i className="fas fa-credit-card me-2"></i>
                              {cartItems.length > 0
                                ? "Ödemeye Geç"
                                : "Sepet Boş"}
                            </button>
                          </div>
                        </div>
                      </div>
                    </div>
                  </>
                ) : (
                  <div className="row justify-content-center">
                    <div className="col-md-8">
                      <div className="text-center mb-4">
                        <div
                          className="p-4 rounded-circle mx-auto mb-4 shadow-lg"
                          style={{
                            backgroundColor: "#fff8f0",
                            width: "100px",
                            height: "100px",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                          }}
                        >
                          <i
                            className="fas fa-user-circle text-warning"
                            style={{ fontSize: "2.5rem" }}
                          ></i>
                        </div>
                        <h4 className="text-warning fw-bold mb-2">
                          Giriş Yapın
                        </h4>
                        <p className="text-muted mb-4">
                          Sepetinize ürün eklemek için hesabınıza giriş yapmanız
                          gerekiyor
                        </p>
                      </div>

                      <form className="login-form">
                        <div className="mb-4">
                          <label className="form-label fw-semibold text-muted">
                            <i className="fas fa-envelope me-2"></i>E-posta
                            Adresi
                          </label>
                          <input
                            type="email"
                            className="form-control form-control-lg"
                            placeholder="ornek@email.com"
                            style={{
                              borderRadius: "15px",
                              border: "2px solid #ffe0b2",
                              padding: "12px 20px",
                            }}
                          />
                        </div>

                        <div className="mb-4">
                          <label className="form-label fw-semibold text-muted">
                            <i className="fas fa-lock me-2"></i>Şifre
                          </label>
                          <input
                            type="password"
                            className="form-control form-control-lg"
                            placeholder="••••••••"
                            style={{
                              borderRadius: "15px",
                              border: "2px solid #ffe0b2",
                              padding: "12px 20px",
                            }}
                          />
                        </div>

                        <div className="d-flex justify-content-between align-items-center mb-4">
                          <div className="form-check">
                            <input
                              className="form-check-input"
                              type="checkbox"
                              id="rememberMe"
                            />
                            <label
                              className="form-check-label text-muted"
                              htmlFor="rememberMe"
                            >
                              Beni hatırla
                            </label>
                          </div>
                          <button
                            type="button"
                            className="btn btn-link text-warning text-decoration-none fw-semibold p-0"
                            onClick={() =>
                              alert("Şifre sıfırlama özelliği yakında...")
                            }
                          >
                            Şifremi unuttum
                          </button>
                        </div>

                        <button
                          type="button"
                          className="btn btn-lg w-100 text-white fw-bold shadow-lg border-0 mb-3"
                          style={{
                            background:
                              "linear-gradient(135deg, #ff6b35, #ff8c00)",
                            borderRadius: "25px",
                            padding: "15px",
                            fontSize: "1.1rem",
                          }}
                          onClick={() => {
                            // Basit demo login - gerçek uygulamada API çağrısı yapılır
                            alert(
                              "Giriş başarılı! Şimdi ürünleri sepetinize ekleyebilirsiniz."
                            );
                            navigate("/");
                          }}
                        >
                          <i className="fas fa-sign-in-alt me-2"></i>
                          Giriş Yap
                        </button>

                        <div className="text-center">
                          <span className="text-muted">Hesabınız yok mu? </span>
                          <button
                            type="button"
                            className="btn btn-link text-warning text-decoration-none fw-semibold p-0"
                            onClick={() =>
                              alert("Kayıt olma özelliği yakında...")
                            }
                          >
                            Kayıt olun
                          </button>
                        </div>
                      </form>

                      <div className="text-center mt-4">
                        <Link
                          to="/"
                          className="btn btn-outline-warning btn-lg border-2 fw-semibold"
                          style={{
                            borderRadius: "25px",
                            padding: "12px 30px",
                          }}
                        >
                          <i className="fas fa-arrow-left me-2"></i>
                          Alışverişe Devam Et
                        </Link>
                      </div>
                    </div>
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

export default CartPage;
