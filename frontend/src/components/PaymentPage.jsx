import React, { useState, useEffect } from "react";
import { OrderService } from "../services/orderService";
import { CartService } from "../services/cartService";

const PaymentPage = () => {
  const [cartItems, setCartItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState("creditCard");
  const [formData, setFormData] = useState({
    // Kredi Kartı Bilgileri
    cardNumber: "",
    cardName: "",
    expiryMonth: "",
    expiryYear: "",
    cvv: "",

    // Adres Bilgileri
    firstName: "",
    lastName: "",
    email: "",
    phone: "",
    address: "",
    city: "",
    district: "",
    postalCode: "",

    // Fatura Adresi
    billingAddress: "",
    billingCity: "",
    billingDistrict: "",
    billingPostalCode: "",
    sameAsDelivery: true,
  });

  useEffect(() => {
    loadCartItems();
  }, []);

  const loadCartItems = async () => {
    try {
      const userId = localStorage.getItem("userId");
      const items = await CartService.getCart(userId);
      setCartItems(items || []);
    } catch (error) {
      console.error("Sepet yüklenemedi:", error);
      // Demo data
      setCartItems([
        {
          id: 1,
          productName: "Wireless Bluetooth Kulaklık",
          price: 149.99,
          quantity: 1,
        },
        {
          id: 2,
          productName: "Gaming Mouse",
          price: 299.99,
          quantity: 1,
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const calculateTotals = () => {
    const subtotal = cartItems.reduce(
      (sum, item) => sum + item.price * item.quantity,
      0
    );
    const shipping = subtotal > 150 ? 0 : 15; // 150 TL üzeri ücretsiz kargo
    const tax = subtotal * 0.18; // KDV %18
    const total = subtotal + shipping + tax;

    return { subtotal, shipping, tax, total };
  };

  const handleInputChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const formatCardNumber = (value) => {
    // Sadece rakamları al ve 4'lü gruplar halinde böl
    const v = value.replace(/\s+/g, "").replace(/[^0-9]/gi, "");
    const matches = v.match(/\d{4,16}/g);
    const match = (matches && matches[0]) || "";
    const parts = [];
    for (let i = 0, len = match.length; i < len; i += 4) {
      parts.push(match.substring(i, i + 4));
    }
    if (parts.length) {
      return parts.join(" ");
    } else {
      return v;
    }
  };

  const handleCardNumberChange = (e) => {
    const formatted = formatCardNumber(e.target.value);
    setFormData((prev) => ({ ...prev, cardNumber: formatted }));
  };

  const validateForm = () => {
    const required = [
      "firstName",
      "lastName",
      "email",
      "phone",
      "address",
      "city",
    ];
    if (paymentMethod === "creditCard") {
      required.push(
        "cardNumber",
        "cardName",
        "expiryMonth",
        "expiryYear",
        "cvv"
      );
    }

    return required.every((field) => formData[field].trim() !== "");
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!validateForm()) {
      alert("Lütfen tüm zorunlu alanları doldurun.");
      return;
    }

    if (cartItems.length === 0) {
      alert("Sepetiniz boş. Lütfen önce ürün ekleyin.");
      return;
    }

    setProcessing(true);

    try {
      const { total } = calculateTotals();

      const paymentData = {
        items: cartItems,
        paymentMethod,
        amount: total,
        customerInfo: {
          firstName: formData.firstName,
          lastName: formData.lastName,
          email: formData.email,
          phone: formData.phone,
        },
        deliveryAddress: {
          address: formData.address,
          city: formData.city,
          district: formData.district,
          postalCode: formData.postalCode,
        },
        billingAddress: formData.sameAsDelivery
          ? {
              address: formData.address,
              city: formData.city,
              district: formData.district,
              postalCode: formData.postalCode,
            }
          : {
              address: formData.billingAddress,
              city: formData.billingCity,
              district: formData.billingDistrict,
              postalCode: formData.billingPostalCode,
            },
        cardInfo:
          paymentMethod === "creditCard"
            ? {
                cardNumber: formData.cardNumber.replace(/\s/g, ""),
                cardName: formData.cardName,
                expiryMonth: formData.expiryMonth,
                expiryYear: formData.expiryYear,
                cvv: formData.cvv,
              }
            : null,
      };

      const result = await OrderService.checkout(paymentData);

      if (result.success) {
        alert(
          "Ödeme başarıyla tamamlandı! Sipariş numaranız: " + result.orderNumber
        );
        // Sepeti temizle ve başarı sayfasına yönlendir
        window.location.href =
          "/order-success?orderNumber=" + result.orderNumber;
      } else {
        throw new Error(result.message || "Ödeme işlemi başarısız");
      }
    } catch (error) {
      console.error("Ödeme hatası:", error);
      alert("Ödeme işlemi sırasında bir hata oluştu: " + error.message);
    } finally {
      setProcessing(false);
    }
  };

  const { subtotal, shipping, tax, total } = calculateTotals();

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
        <p className="text-muted fw-bold">Ödeme sayfası yükleniyor...</p>
      </div>
    );
  }

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
        <form onSubmit={handleSubmit}>
          <div className="row">
            {/* Sol Taraf: Ödeme Formu */}
            <div className="col-lg-8">
              {/* Ödeme Yöntemi Seçimi */}
              <div
                className="card shadow-lg border-0 mb-4"
                style={{ borderRadius: "20px" }}
              >
                <div
                  className="card-header text-white border-0"
                  style={{
                    background:
                      "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                    borderTopLeftRadius: "20px",
                    borderTopRightRadius: "20px",
                    padding: "1.5rem",
                  }}
                >
                  <h5 className="mb-0 fw-bold">
                    <i className="fas fa-credit-card me-2"></i>Ödeme Yöntemi
                  </h5>
                </div>
                <div className="card-body" style={{ padding: "2rem" }}>
                  <div className="row">
                    <div className="col-md-6 mb-3">
                      <div
                        className={`card h-100 ${
                          paymentMethod === "creditCard" ? "border-warning" : ""
                        }`}
                        style={{
                          cursor: "pointer",
                          borderRadius: "15px",
                          border:
                            paymentMethod === "creditCard"
                              ? "2px solid #ffa000"
                              : "1px solid #dee2e6",
                        }}
                        onClick={() => setPaymentMethod("creditCard")}
                      >
                        <div className="card-body text-center p-4">
                          <i className="fas fa-credit-card fa-3x text-warning mb-3"></i>
                          <h6 className="fw-bold">Kredi Kartı</h6>
                          <p className="text-muted small mb-0">
                            Visa, MasterCard, AmEx
                          </p>
                        </div>
                      </div>
                    </div>
                    <div className="col-md-6 mb-3">
                      <div
                        className={`card h-100 ${
                          paymentMethod === "bankTransfer"
                            ? "border-warning"
                            : ""
                        }`}
                        style={{
                          cursor: "pointer",
                          borderRadius: "15px",
                          border:
                            paymentMethod === "bankTransfer"
                              ? "2px solid #ffa000"
                              : "1px solid #dee2e6",
                        }}
                        onClick={() => setPaymentMethod("bankTransfer")}
                      >
                        <div className="card-body text-center p-4">
                          <i className="fas fa-university fa-3x text-warning mb-3"></i>
                          <h6 className="fw-bold">Havale/EFT</h6>
                          <p className="text-muted small mb-0">
                            Banka hesabına havale
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              {/* Kredi Kartı Bilgileri */}
              {paymentMethod === "creditCard" && (
                <div
                  className="card shadow-lg border-0 mb-4"
                  style={{ borderRadius: "20px" }}
                >
                  <div
                    className="card-header text-white border-0"
                    style={{
                      background: "linear-gradient(45deg, #17a2b8, #20c997)",
                      borderTopLeftRadius: "20px",
                      borderTopRightRadius: "20px",
                      padding: "1.5rem",
                    }}
                  >
                    <h5 className="mb-0 fw-bold">
                      <i className="fas fa-lock me-2"></i>Kart Bilgileri
                    </h5>
                  </div>
                  <div className="card-body" style={{ padding: "2rem" }}>
                    <div className="row">
                      <div className="col-md-6 mb-3">
                        <label className="form-label fw-bold text-warning">
                          Kart Numarası
                        </label>
                        <input
                          type="text"
                          name="cardNumber"
                          className="form-control form-control-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          placeholder="1234 5678 9012 3456"
                          value={formData.cardNumber}
                          onChange={handleCardNumberChange}
                          maxLength="19"
                          required
                        />
                      </div>
                      <div className="col-md-6 mb-3">
                        <label className="form-label fw-bold text-warning">
                          Kart Üzerindeki İsim
                        </label>
                        <input
                          type="text"
                          name="cardName"
                          className="form-control form-control-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          placeholder="JOHN DOE"
                          value={formData.cardName}
                          onChange={handleInputChange}
                          required
                        />
                      </div>
                    </div>
                    <div className="row">
                      <div className="col-md-4 mb-3">
                        <label className="form-label fw-bold text-warning">
                          Ay
                        </label>
                        <select
                          name="expiryMonth"
                          className="form-select form-select-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          value={formData.expiryMonth}
                          onChange={handleInputChange}
                          required
                        >
                          <option value="">Ay</option>
                          {Array.from({ length: 12 }, (_, i) => (
                            <option
                              key={i + 1}
                              value={String(i + 1).padStart(2, "0")}
                            >
                              {String(i + 1).padStart(2, "0")}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="col-md-4 mb-3">
                        <label className="form-label fw-bold text-warning">
                          Yıl
                        </label>
                        <select
                          name="expiryYear"
                          className="form-select form-select-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          value={formData.expiryYear}
                          onChange={handleInputChange}
                          required
                        >
                          <option value="">Yıl</option>
                          {Array.from({ length: 10 }, (_, i) => {
                            const year = new Date().getFullYear() + i;
                            return (
                              <option key={year} value={year}>
                                {year}
                              </option>
                            );
                          })}
                        </select>
                      </div>
                      <div className="col-md-4 mb-3">
                        <label className="form-label fw-bold text-warning">
                          CVV
                        </label>
                        <input
                          type="text"
                          name="cvv"
                          className="form-control form-control-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          placeholder="123"
                          value={formData.cvv}
                          onChange={handleInputChange}
                          maxLength="4"
                          required
                        />
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* Teslimat Bilgileri */}
              <div
                className="card shadow-lg border-0 mb-4"
                style={{ borderRadius: "20px" }}
              >
                <div
                  className="card-header text-white border-0"
                  style={{
                    background: "linear-gradient(45deg, #6f42c1, #e83e8c)",
                    borderTopLeftRadius: "20px",
                    borderTopRightRadius: "20px",
                    padding: "1.5rem",
                  }}
                >
                  <h5 className="mb-0 fw-bold">
                    <i className="fas fa-truck me-2"></i>Teslimat Bilgileri
                  </h5>
                </div>
                <div className="card-body" style={{ padding: "2rem" }}>
                  <div className="row">
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold text-warning">
                        Ad
                      </label>
                      <input
                        type="text"
                        name="firstName"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.firstName}
                        onChange={handleInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold text-warning">
                        Soyad
                      </label>
                      <input
                        type="text"
                        name="lastName"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.lastName}
                        onChange={handleInputChange}
                        required
                      />
                    </div>
                  </div>
                  <div className="row">
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold text-warning">
                        E-posta
                      </label>
                      <input
                        type="email"
                        name="email"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.email}
                        onChange={handleInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold text-warning">
                        Telefon
                      </label>
                      <input
                        type="tel"
                        name="phone"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.phone}
                        onChange={handleInputChange}
                        required
                      />
                    </div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label fw-bold text-warning">
                      Adres
                    </label>
                    <textarea
                      name="address"
                      className="form-control form-control-lg border-0 shadow-sm"
                      style={{
                        backgroundColor: "#fff8f0",
                        borderRadius: "15px",
                        padding: "1rem 1.5rem",
                      }}
                      rows="3"
                      value={formData.address}
                      onChange={handleInputChange}
                      required
                    />
                  </div>
                  <div className="row">
                    <div className="col-md-4 mb-3">
                      <label className="form-label fw-bold text-warning">
                        İl
                      </label>
                      <input
                        type="text"
                        name="city"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.city}
                        onChange={handleInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-4 mb-3">
                      <label className="form-label fw-bold text-warning">
                        İlçe
                      </label>
                      <input
                        type="text"
                        name="district"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.district}
                        onChange={handleInputChange}
                      />
                    </div>
                    <div className="col-md-4 mb-3">
                      <label className="form-label fw-bold text-warning">
                        Posta Kodu
                      </label>
                      <input
                        type="text"
                        name="postalCode"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.postalCode}
                        onChange={handleInputChange}
                      />
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Sağ Taraf: Sipariş Özeti */}
            <div className="col-lg-4">
              <div
                className="card shadow-lg border-0 position-sticky"
                style={{ borderRadius: "20px", top: "20px" }}
              >
                <div
                  className="card-header text-white border-0"
                  style={{
                    background: "linear-gradient(45deg, #28a745, #20c997)",
                    borderTopLeftRadius: "20px",
                    borderTopRightRadius: "20px",
                    padding: "1.5rem",
                  }}
                >
                  <h5 className="mb-0 fw-bold">
                    <i className="fas fa-receipt me-2"></i>Sipariş Özeti
                  </h5>
                </div>
                <div className="card-body" style={{ padding: "2rem" }}>
                  {/* Ürünler */}
                  <div className="mb-4">
                    <h6 className="text-warning fw-bold mb-3">
                      Sepetinizdeki Ürünler
                    </h6>
                    {cartItems.map((item) => (
                      <div
                        key={item.id}
                        className="d-flex align-items-center mb-3 pb-3 border-bottom"
                      >
                        <div
                          className="me-3 d-flex align-items-center justify-content-center"
                          style={{
                            width: "50px",
                            height: "50px",
                            backgroundColor: "#fff8f0",
                            borderRadius: "10px",
                          }}
                        >
                          📦
                        </div>
                        <div className="flex-grow-1">
                          <h6 className="mb-1" style={{ fontSize: "0.9rem" }}>
                            {item.productName}
                          </h6>
                          <small className="text-muted">
                            {item.quantity} x ₺{Number(item.price).toFixed(2)}
                          </small>
                        </div>
                        <strong className="text-warning">
                          ₺{Number(item.quantity * item.price).toFixed(2)}
                        </strong>
                      </div>
                    ))}
                  </div>

                  {/* Toplam Hesapları */}
                  <div className="mb-4">
                    <div className="d-flex justify-content-between mb-2">
                      <span>Ara Toplam:</span>
                      <span>₺{subtotal.toFixed(2)}</span>
                    </div>
                    <div className="d-flex justify-content-between mb-2">
                      <span>Kargo:</span>
                      <span className={shipping === 0 ? "text-success" : ""}>
                        {shipping === 0
                          ? "Ücretsiz"
                          : `₺${shipping.toFixed(2)}`}
                      </span>
                    </div>
                    <div className="d-flex justify-content-between mb-2">
                      <span>KDV (%18):</span>
                      <span>₺{tax.toFixed(2)}</span>
                    </div>
                    <hr />
                    <div className="d-flex justify-content-between fw-bold fs-5">
                      <span>Toplam:</span>
                      <span className="text-warning">₺{total.toFixed(2)}</span>
                    </div>
                  </div>

                  {/* Ödeme Butonu */}
                  <button
                    type="submit"
                    disabled={processing}
                    className="btn btn-lg w-100 text-white fw-bold shadow-lg border-0"
                    style={{
                      background: processing
                        ? "linear-gradient(45deg, #6c757d, #adb5bd)"
                        : "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                      borderRadius: "15px",
                      padding: "1rem",
                    }}
                  >
                    {processing ? (
                      <>
                        <span
                          className="spinner-border spinner-border-sm me-2"
                          role="status"
                        ></span>
                        İşleniyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-lock me-2"></i>
                        Güvenli Ödeme Yap
                      </>
                    )}
                  </button>

                  {/* Güvenlik Rozeti */}
                  <div className="text-center mt-3">
                    <small className="text-muted d-flex align-items-center justify-content-center">
                      <i className="fas fa-shield-alt text-success me-2"></i>
                      256-bit SSL ile güvence altında
                    </small>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
};

export default PaymentPage;
