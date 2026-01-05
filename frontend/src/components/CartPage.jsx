import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { useCart } from "../contexts/CartContext";
import api from "../services/api";
import { CartService } from "../services/cartService";
import { ProductService } from "../services/productService";

const CartPage = () => {
  const {
    cartItems,
    loading: cartLoading,
    updateQuantity,
    removeFromCart,
    getCartTotal,
  } = useCart();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [products, setProducts] = useState({});
  const { user } = useAuth();
  const [shippingMethod, setShippingMethod] = useState(() => {
    try {
      return CartService.getShippingMethod();
    } catch {
      return "motorcycle";
    }
  });
  const [couponCode, setCouponCode] = useState("");
  const [pricing, setPricing] = useState(null);
  const [pricingLoading, setPricingLoading] = useState(false);
  const [pricingError, setPricingError] = useState("");

  // Ürün detaylarını yükle
  const loadProductData = useCallback(async () => {
    if (cartItems.length === 0) {
      setProducts({});
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      let allProducts = [];
      let productData = {};

      try {
        allProducts = await ProductService.list();
      } catch (error) {
        // Fallback ürün verileri
        allProducts = [
          {
            id: 1,
            name: "Cif Krem Doğanın Gücü Hijyen 675Ml",
            price: 204.95,
            specialPrice: 129.95,
            imageUrl: "/images/yeşil-cif-krem.jpg",
          },
          {
            id: 2,
            name: "Ülker Altınbaşak Tahıl Cipsi 50 Gr",
            price: 18.0,
            specialPrice: 14.9,
            imageUrl: "/images/tahil-cipsi.jpg",
          },
          {
            id: 3,
            name: "Lipton Ice Tea Limon 330 Ml",
            price: 60.0,
            specialPrice: 40.9,
            imageUrl: "/images/lipton-ice-tea.jpg",
          },
          {
            id: 4,
            name: "Dana But Tas Kebaplık Et Çiftlik Kg",
            price: 375.95,
            specialPrice: 279.0,
            imageUrl: "/images/dana-kusbasi.jpg",
          },
          {
            id: 5,
            name: "Kuzu İncik Kg",
            price: 1399.95,
            specialPrice: 699.95,
            imageUrl: "/images/kuzu-incik.webp",
          },
          {
            id: 6,
            name: "Nescafe 2si 1 Arada Sütlü Köpüklü 15 x 10g",
            price: 145.55,
            specialPrice: 84.5,
            imageUrl: "/images/nescafe.jpg",
          },
          {
            id: 7,
            name: "Domates Kg",
            price: 45.9,
            specialPrice: 45.9,
            imageUrl: "/images/domates.webp",
          },
          {
            id: 8,
            name: "Pınar Süt 1L",
            price: 28.5,
            specialPrice: 28.5,
            imageUrl: "/images/pinar-nestle-sut.jpg",
          },
          {
            id: 9,
            name: "Sek Kaşar Peyniri 200 G",
            price: 75.9,
            specialPrice: 64.5,
            imageUrl: "/images/sek-kasar-peyniri-200-gr-38be46-1650x1650.jpg",
          },
          {
            id: 10,
            name: "Mis Bulgur Pilavlık 1Kg",
            price: 32.9,
            specialPrice: 32.9,
            imageUrl: "/images/bulgur.png",
          },
          {
            id: 11,
            name: "Coca-Cola Orijinal Tat Kutu 330ml",
            price: 12.5,
            specialPrice: 10.0,
            imageUrl: "/images/coca-cola.jpg",
          },
          {
            id: 12,
            name: "Salatalık Kg",
            price: 28.9,
            specialPrice: 28.9,
            imageUrl: "/images/salatalik.jpg",
          },
        ];
      }

      for (const item of cartItems) {
        const pid = item.productId || item.id;
        const product = allProducts.find(
          (p) => p.id === pid || String(p.id) === String(pid)
        );

        if (product) {
          productData[pid] = {
            ...product,
            imageUrl: product.imageUrl || `/images/placeholder.png`,
          };
        } else if (item.product) {
          // CartContext'den gelen product bilgisini kullan
          productData[pid] = item.product;
        } else {
          productData[pid] = {
            id: pid,
            name: "Bilinmeyen Ürün",
            price: 0,
            imageUrl: "/images/placeholder.png",
          };
        }
      }

      setProducts(productData);
    } catch (error) {
      console.error("Ürün bilgileri alınırken hata:", error);
    } finally {
      setLoading(false);
    }
  }, [cartItems]);

  useEffect(() => {
    loadProductData();
  }, [loadProductData]);

  const getItemUnitPrice = (item) => {
    if (
      item &&
      item.unitPrice !== undefined &&
      item.unitPrice !== null &&
      !Number.isNaN(Number(item.unitPrice))
    ) {
      return Number(item.unitPrice);
    }
    const pid = item.productId || item.id;
    const product = products[pid] || item.product;
    const fallback = (product && (product.specialPrice || product.price)) || 0;
    return Number(fallback) || 0;
  };

  const handleUpdateQuantity = (item, newQuantity) => {
    if (newQuantity < 1) {
      handleRemoveItem(item);
      return;
    }
    const pid = item.productId || item.id;
    updateQuantity(pid, newQuantity);
  };

  const handleRemoveItem = (item) => {
    const pid = item.productId || item.id;
    removeFromCart(pid);
  };

  const getTotalPrice = () => {
    return getCartTotal();
  };

  const getShippingCost = () => {
    // motorcycle (motokurye) cheaper, car (araç) more expensive
    return shippingMethod === "car" ? 30 : 15;
  };

  const handleApplyCoupon = async () => {
    setPricingError("");
    setPricingLoading(true);
    try {
      const itemsPayload = cartItems.map((item) => ({
        productId: item.productId || item.id,
        quantity: item.quantity,
      }));
      const result = await CartService.previewPrice({
        items: itemsPayload,
        couponCode: couponCode?.trim() || null,
      });
      setPricing(result);
    } catch (error) {
      console.error("Kupon uygulanırken hata:", error);
      setPricingError("Kupon geçersiz veya kullanılamıyor.");
    } finally {
      setPricingLoading(false);
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
                                      ₺{getItemUnitPrice(item).toFixed(2)}
                                    </p>
                                  </div>
                                </div>
                              </div>
                              <div className="col-md-2 text-center">
                                <div className="d-flex align-items-center justify-content-center">
                                  <button
                                    className="btn btn-outline-warning btn-sm me-2"
                                    onClick={() =>
                                      handleUpdateQuantity(
                                        item,
                                        item.quantity - 1
                                      )
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
                                      handleUpdateQuantity(
                                        item,
                                        item.quantity + 1
                                      )
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
                                    {(
                                      getItemUnitPrice(item) * item.quantity
                                    ).toFixed(2)}
                                  </p>
                                  <button
                                    className="btn btn-link text-danger p-0"
                                    onClick={() => {
                                      handleRemoveItem(item);
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
                            {/* Kupon Kodu Alanı */}
                            <div className="mt-3 mb-2">
                              <label className="form-label text-muted fw-semibold">
                                Kupon Kodu
                              </label>
                              <div className="input-group">
                                <input
                                  type="text"
                                  className="form-control"
                                  placeholder="Kupon kodunuzu girin"
                                  value={couponCode}
                                  onChange={(e) =>
                                    setCouponCode(e.target.value)
                                  }
                                />
                                <button
                                  className="btn btn-warning fw-bold"
                                  type="button"
                                  disabled={pricingLoading || !cartItems.length}
                                  onClick={handleApplyCoupon}
                                >
                                  {pricingLoading
                                    ? "Uygulanıyor..."
                                    : "Kupon Uygula"}
                                </button>
                              </div>
                              {pricingError && (
                                <div className="alert alert-danger mt-2 py-2 mb-0">
                                  {pricingError}
                                </div>
                              )}
                            </div>

                            {/* Fiyat Kırılımı (Backend fiyat önizleme) */}
                            {pricing && (
                              <div className="mt-3">
                                <hr style={{ borderColor: "#ffe0b2" }} />
                                <div className="d-flex justify-content-between mb-1">
                                  <span>Ara Toplam</span>
                                  <span>
                                    ₺{Number(pricing.subtotal || 0).toFixed(2)}
                                  </span>
                                </div>
                                <div className="d-flex justify-content-between mb-1">
                                  <span>Kampanya İndirimi</span>
                                  <span className="text-success">
                                    -₺
                                    {Number(
                                      pricing.campaignDiscountTotal || 0
                                    ).toFixed(2)}
                                  </span>
                                </div>
                                <div className="d-flex justify-content-between mb-1">
                                  <span>Kupon İndirimi</span>
                                  <span className="text-success">
                                    -₺
                                    {Number(
                                      pricing.couponDiscountTotal || 0
                                    ).toFixed(2)}
                                  </span>
                                </div>
                                <div className="d-flex justify-content-between mb-1">
                                  <span>Kargo Ücreti</span>
                                  <span>
                                    ₺
                                    {Number(pricing.deliveryFee || 0).toFixed(
                                      2
                                    )}
                                  </span>
                                </div>
                                <hr style={{ borderColor: "#ffe0b2" }} />
                                <div className="d-flex justify-content-between fw-bold">
                                  <span>Genel Toplam</span>
                                  <span className="text-warning">
                                    ₺
                                    {Number(pricing.grandTotal || 0).toFixed(2)}
                                  </span>
                                </div>
                                {pricing.appliedCouponCode && (
                                  <div className="mt-2 small text-muted">
                                    Uygulanan Kupon:{" "}
                                    <strong>{pricing.appliedCouponCode}</strong>
                                  </div>
                                )}
                                {Array.isArray(pricing.appliedCampaignNames) &&
                                  pricing.appliedCampaignNames.length > 0 && (
                                    <div className="mt-1 small text-muted">
                                      Kampanyalar:{" "}
                                      {pricing.appliedCampaignNames.join(", ")}
                                    </div>
                                  )}
                              </div>
                            )}
                            <div className="mb-3">
                              <h6 className="text-muted mb-2">Kargo Seçimi</h6>
                              <div className="form-check">
                                <input
                                  className="form-check-input"
                                  type="radio"
                                  name="shipping"
                                  id="shipMoto"
                                  checked={shippingMethod === "motorcycle"}
                                  onChange={() => {
                                    setShippingMethod("motorcycle");
                                    CartService.setShippingMethod("motorcycle");
                                  }}
                                />
                                <label
                                  className="form-check-label"
                                  htmlFor="shipMoto"
                                >
                                  Motokurye (₺15) — Hızlı teslimat
                                </label>
                              </div>
                              <div className="form-check">
                                <input
                                  className="form-check-input"
                                  type="radio"
                                  name="shipping"
                                  id="shipCar"
                                  checked={shippingMethod === "car"}
                                  onChange={() => {
                                    setShippingMethod("car");
                                    CartService.setShippingMethod("car");
                                  }}
                                />
                                <label
                                  className="form-check-label"
                                  htmlFor="shipCar"
                                >
                                  Araç (₺30) — Daha büyük/yoğun siparişler için
                                </label>
                              </div>
                            </div>
                            <div className="d-flex justify-content-between mb-2">
                              <span>Kargo:</span>
                              <span className="text-success fw-bold">
                                ₺{getShippingCost().toFixed(2)}
                              </span>
                            </div>
                            <hr style={{ borderColor: "#ffe0b2" }} />
                            <div className="d-flex justify-content-between fw-bold fs-5 mb-3">
                              <span>Toplam:</span>
                              <span className="text-warning">
                                ₺
                                {(getTotalPrice() + getShippingCost()).toFixed(
                                  2
                                )}
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
