import React from "react";
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Link,
  useNavigate,
} from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";
import "./App.css";
import ProductGrid from "./components/ProductGrid";
import AccountPage from "./components/AccountPage";
import CartPage from "./components/CartPage";
import OrderTracking from "./components/OrderTracking";
import PaymentPage from "./components/PaymentPage";
import AdminPanel from "./admin/AdminPanel";
import { useCartCount } from "./hooks/useCartCount";

function Header() {
  const { count: cartCount } = useCartCount();
  const navigate = useNavigate();

  return (
    <div className="App">
      {/* Main Header */}
      <header className="main-header bg-white shadow-sm py-3">
        <div className="container-fluid px-4">
          <div className="row align-items-center">
            {/* Logo */}
            <div className="col-md-3">
              <Link to="/" className="text-decoration-none">
                <div className="d-flex align-items-center">
                  <div className="logo-container me-3">
                    <img
                      src="/images/golkoy-logo.png"
                      alt="Gölköy Gourmet Market"
                      style={{
                        height: "50px",
                        width: "auto",
                      }}
                    />
                  </div>
                  <div>
                    <h4 className="mb-0 fw-bold text-dark">
                      Gölköy Gourmet Market
                    </h4>
                    <small className="text-muted">
                      Doğal ve Kaliteli Ürünler
                    </small>
                  </div>
                </div>
              </Link>
            </div>

            {/* Search Bar */}
            <div className="col-md-6">
              <div className="search-container">
                <div className="input-group">
                  <input
                    type="text"
                    className="form-control form-control-lg"
                    placeholder="Doğal ve kaliteli ürünler arasında ara..."
                  />
                  <button
                    className="btn btn-lg text-white border-0"
                    style={{
                      background: "linear-gradient(45deg, #ff6f00, #ff8f00)",
                    }}
                  >
                    <i className="fas fa-search"></i>
                  </button>
                </div>
              </div>
            </div>

            {/* Right Actions */}
            <div className="col-md-3">
              <div className="header-actions justify-content-end">
                <button
                  onClick={() => navigate("/account")}
                  className="header-action btn btn-link p-0 text-decoration-none"
                >
                  <i className="fas fa-user"></i>
                  <span>Hesabım</span>
                </button>
                <button
                  onClick={() => navigate("/orders")}
                  className="header-action btn btn-link p-0 text-decoration-none"
                >
                  <i className="fas fa-truck"></i>
                  <span>Siparişlerim</span>
                </button>
                <button
                  onClick={() => navigate("/cart")}
                  className="header-action btn btn-link p-0 text-decoration-none position-relative"
                >
                  <i className="fas fa-shopping-cart"></i>
                  <span>Sepetim</span>
                  <span className="badge cart-badge position-absolute top-0 start-0">
                    {cartCount}
                  </span>
                </button>
                <button
                  onClick={() => navigate("/admin")}
                  className="header-action btn btn-link p-0 text-decoration-none"
                >
                  <i className="fas fa-cog text-warning"></i>
                  <span>Admin</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </header>

      {/* Professional Single Line Category Navigation */}
      <nav className="single-line-categories">
        <div className="container-fluid">
          <div className="category-scroll-container">
            <button className="category-btn active" type="button">
              KATEGORİLER
            </button>
            <button className="category-btn" type="button">
              MEYVE &amp; SEBZE
            </button>
            <button className="category-btn" type="button">
              ET &amp; TAVUK &amp; BALIK
            </button>
            <button className="category-btn" type="button">
              SÜT ÜRÜNLERİ
            </button>
            <button className="category-btn" type="button">
              TEMEL GIDA
            </button>
            <button className="category-btn" type="button">
              İÇECEKLER
            </button>
            <button className="category-btn" type="button">
              ATIŞTIRMALIK
            </button>
            <button className="category-btn" type="button">
              TEMİZLİK
            </button>
            <button className="category-btn" type="button">
              FAVORİLERİM
            </button>
            <button className="category-btn" type="button">
              KAMPANYALAR
            </button>
          </div>
        </div>
      </nav>
    </div>
  );
}

function App() {
  return (
    <Router>
      <div className="App">
        <Header />
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/account" element={<AccountPage />} />
          <Route path="/cart" element={<CartPage />} />
          <Route path="/orders" element={<OrderTracking />} />
          <Route path="/payment" element={<PaymentPage />} />
          <Route path="/admin" element={<AdminPanel />} />
        </Routes>
      </div>
    </Router>
  );
}

function HomePage() {
  return (
    <>
      {/* Kampanya Carousel */}
      <section className="campaign-carousel py-4">
        <div className="container-fluid px-4">
          <div className="position-relative">
            {/* Carousel Container */}
            <div
              className="carousel-container"
              style={{ borderRadius: "20px", overflow: "hidden" }}
            >
              <div
                className="carousel-slide active"
                style={{
                  background:
                    "linear-gradient(135deg, #ff6b35 0%, #ff8f42 100%)",
                  minHeight: "300px",
                  position: "relative",
                  overflow: "hidden",
                }}
              >
                <div className="row align-items-center h-100">
                  <div className="col-md-6 p-5">
                    <div className="campaign-badge mb-3">
                      <span className="badge bg-light text-dark px-3 py-2 rounded-pill fw-bold">
                        Kahve Keyfi Aç!
                      </span>
                    </div>
                    <h2
                      className="text-white fw-bold mb-3"
                      style={{ fontSize: "2.5rem" }}
                    >
                      SEÇİLİ ÜRÜNLERDE
                      <br />
                      <span className="fw-bolder">%25 İNDİRİM!</span>
                    </h2>
                    <p className="text-white mb-4 opacity-90">
                      *Seçili ürünlerden verilecek 150 TL ve üzeri siparişlerde
                      geçerlidir.
                      <br />
                      Kampanya kapsamında listelenen ürünler için tıklayın.
                    </p>
                  </div>
                  <div className="col-md-6 d-flex justify-content-center align-items-center p-4">
                    <div className="campaign-visual">
                      <div className="product-basket position-relative">
                        <div className="floating-items">
                          <div
                            className="product-item"
                            style={{
                              position: "absolute",
                              top: "20px",
                              right: "30px",
                              background: "rgba(255,255,255,0.9)",
                              padding: "15px",
                              borderRadius: "10px",
                              fontSize: "1.5rem",
                            }}
                          >
                            🍫
                          </div>
                          <div
                            className="product-item"
                            style={{
                              position: "absolute",
                              top: "80px",
                              right: "80px",
                              background: "rgba(255,255,255,0.9)",
                              padding: "15px",
                              borderRadius: "10px",
                              fontSize: "1.5rem",
                            }}
                          >
                            🍪
                          </div>
                          <div
                            className="product-item"
                            style={{
                              position: "absolute",
                              top: "60px",
                              right: "10px",
                              background: "rgba(255,255,255,0.9)",
                              padding: "15px",
                              borderRadius: "10px",
                              fontSize: "1.5rem",
                            }}
                          >
                            ☕
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Slide Counter */}
                <div className="position-absolute top-0 end-0 m-3">
                  <span className="badge bg-dark bg-opacity-50 px-3 py-2 rounded-pill">
                    1 / 3
                  </span>
                </div>
              </div>
            </div>

            {/* Navigation Arrows */}
            <button
              className="carousel-nav carousel-prev position-absolute top-50 start-0 translate-middle-y"
              style={{
                background: "rgba(255,255,255,0.9)",
                border: "none",
                borderRadius: "50%",
                width: "50px",
                height: "50px",
                marginLeft: "-25px",
                boxShadow: "0 4px 15px rgba(0,0,0,0.1)",
                zIndex: 10,
              }}
            >
              <i className="fas fa-chevron-left text-dark"></i>
            </button>

            <button
              className="carousel-nav carousel-next position-absolute top-50 end-0 translate-middle-y"
              style={{
                background: "rgba(255,255,255,0.9)",
                border: "none",
                borderRadius: "50%",
                width: "50px",
                height: "50px",
                marginRight: "-25px",
                boxShadow: "0 4px 15px rgba(0,0,0,0.1)",
                zIndex: 10,
              }}
            >
              <i className="fas fa-chevron-right text-dark"></i>
            </button>
          </div>
        </div>
      </section>

      {/* İlgini Çekebilecek Ürünler Section */}
      <section className="featured-products-section py-5 bg-light">
        <div className="container-fluid px-4">
          {/* Section Header */}
          <div className="text-center mb-5">
            <h2 className="section-title mb-3 fw-bold">
              İlgini Çekebilecek Ürünler
            </h2>
            <div className="section-subtitle">
              <span className="badge bg-warning text-dark px-3 py-2 fs-6">
                <i className="fas fa-fire me-2"></i>
                Bu Fırsatları Kaçırma
              </span>
            </div>
          </div>

          {/* Products Grid */}
          <ProductGrid />
        </div>
      </section>

      {/* Bu Yolculukta Bizimle Olun Section */}
      <section className="join-journey-section">
        <div className="container-fluid px-4">
          <div className="row justify-content-center">
            <div className="col-lg-10">
              <div className="join-journey-card text-center">
                <h2 className="journey-title mb-4">
                  Bu Yolculukta Bizimle Olun
                </h2>
                <p className="journey-subtitle mb-4">
                  AlışMarket ailesi olarak, sizlerle birlikte büyümek ve
                  gelişmek istiyoruz.
                  <br />
                  Görüşleriniz bizim için çok değerli.
                </p>
                <div className="journey-buttons">
                  <button className="btn btn-light btn-lg me-3">
                    <i className="fas fa-envelope me-2"></i>
                    İletişime Geçin
                  </button>
                  <button className="btn btn-outline-light btn-lg">
                    <i className="fas fa-users me-2"></i>
                    Bültenimize Katılın
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Newsletter Section */}
      <section className="newsletter-section py-5">
        <div className="container-fluid px-4">
          <div className="row justify-content-center align-items-center">
            <div className="col-lg-8 text-center">
              <h2 className="newsletter-title mb-3">
                Yeni Ürünlerden İlk Sen Haberdar Ol
              </h2>
              <p className="newsletter-subtitle mb-4">
                Yeni ürünler, özel kampanyalar ve fırsatlar için e-bültenimize
                katıl
              </p>
              <div className="newsletter-form">
                <div className="input-group">
                  <input
                    type="email"
                    className="form-control form-control-lg"
                    placeholder="E-mail adresiniz"
                  />
                  <button className="btn btn-light btn-lg">Katıl</button>
                </div>
                <div className="newsletter-note mt-3">
                  <i className="fas fa-heart me-2"></i>
                  Spam göndermiyoruz, sadece değerli içerik
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="main-footer">
        <div className="container-fluid px-4">
          <div className="row">
            {/* Company Info */}
            <div className="col-lg-3 col-md-6 mb-4">
              <div className="footer-brand">
                <div className="d-flex align-items-center mb-3">
                  <div className="footer-logo me-2">AM</div>
                  <div>
                    <h5 className="footer-brand-name">AlışMarket</h5>
                    <small className="footer-brand-tagline">
                      Yeni Nesil E-Ticaret
                    </small>
                  </div>
                </div>
                <p className="footer-description">
                  2024 yılında kurulan AlışMarket, kaliteli ürünler ve güvenilir
                  hizmet anlayışıyla Türkiye'nin yeni e-ticaret deneyimini
                  sunuyor.
                </p>
                <div className="footer-features">
                  <div className="footer-feature">
                    <i className="fas fa-shield-alt text-success me-2"></i>
                    SSL güvenli alışveriş
                  </div>
                  <div className="footer-feature">
                    <i className="fas fa-credit-card text-info me-2"></i>
                    Güvenli ödeme sistemi
                  </div>
                  <div className="footer-feature">
                    <i className="fas fa-heart text-danger me-2"></i>
                    Müşteri memnuniyeti odaklı
                  </div>
                </div>
              </div>
            </div>

            {/* Categories */}
            <div className="col-lg-2 col-md-6 mb-4">
              <h6 className="footer-title">Kategoriler</h6>
              <ul className="footer-links">
                <li>
                  <a href="#" className="footer-link">
                    Elektronik
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Moda
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Ev & Yaşam
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Kozmetik
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Spor & Outdoor
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Kitap & Kırtasiye
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Anne & Bebek
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Oyuncak
                  </a>
                </li>
              </ul>
            </div>

            {/* Customer Services */}
            <div className="col-lg-2 col-md-6 mb-4">
              <h6 className="footer-title">Müşteri Hizmetleri</h6>
              <ul className="footer-links">
                <li>
                  <a href="#" className="footer-link">
                    Yardım Merkezi
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link text-warning">
                    İletişim
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Sipariş Takibi
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    İade & Değişim
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Kargo Bilgileri
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Ödeme Seçenekleri
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Güvenli Alışveriş
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    S.S.S
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Geri Bildirim
                  </a>
                </li>
              </ul>
            </div>

            {/* Contact Info */}
            <div className="col-lg-2 col-md-6 mb-4">
              <h6 className="footer-title">İletişim</h6>
              <div className="contact-info">
                <div className="contact-item">
                  <i className="fas fa-phone text-warning me-2"></i>
                  <div>
                    <strong>0850 XXX XX XX</strong>
                    <br />
                    <small>Müşteri Hizmetleri</small>
                  </div>
                </div>
                <div className="contact-item">
                  <i className="fas fa-envelope text-warning me-2"></i>
                  <div>
                    <strong>info@alismarket.com.tr</strong>
                    <br />
                    <small>Genel bilgi ve destek</small>
                  </div>
                </div>
                <div className="contact-item">
                  <i className="fas fa-map-marker-alt text-warning me-2"></i>
                  <div>
                    <strong>İstanbul, Türkiye</strong>
                    <br />
                    <small>Merkez Ofis</small>
                  </div>
                </div>
              </div>
            </div>

            {/* Corporate */}
            <div className="col-lg-3 col-md-6 mb-4">
              <h6 className="footer-title">Kurumsal</h6>
              <ul className="footer-links">
                <li>
                  <a href="#" className="footer-link">
                    Hakkımızda
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Vizyon & Misyon
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Kariyer (Yakında)
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Basın Kiti
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Sürdürülebilirlik
                  </a>
                </li>
              </ul>

              {/* Social Media */}
              <div className="social-media mt-4">
                <h6 className="footer-title">Sosyal Medya</h6>
                <div className="social-links">
                  <a href="#" className="social-link">
                    <i className="fab fa-facebook-f"></i>
                  </a>
                  <a href="#" className="social-link">
                    <i className="fab fa-instagram"></i>
                  </a>
                  <a href="#" className="social-link">
                    <i className="fab fa-twitter"></i>
                  </a>
                  <a href="#" className="social-link">
                    <i className="fab fa-youtube"></i>
                  </a>
                  <a href="#" className="social-link">
                    <i className="fab fa-linkedin-in"></i>
                  </a>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Footer Bottom */}
        <div className="footer-bottom">
          <div className="container-fluid px-4">
            <div className="row align-items-center">
              <div className="col-md-8">
                <div className="footer-bottom-links">
                  <span>© 2024 AlışMarket. Tüm hakları saklıdır.</span>
                  <a href="#" className="footer-bottom-link">
                    Gizlilik Politikası
                  </a>
                  <a href="#" className="footer-bottom-link">
                    Kullanım Şartları
                  </a>
                  <a href="#" className="footer-bottom-link">
                    KVKK
                  </a>
                  <a href="#" className="footer-bottom-link">
                    Çerez Politikası
                  </a>
                </div>
              </div>
              <div className="col-md-4 text-end">
                <div className="payment-methods">
                  <span className="payment-text">Kabul Edilen Kartlar:</span>
                  <div className="payment-cards">
                    <span className="payment-card">VISA</span>
                    <span className="payment-card">MC</span>
                    <span className="payment-card">TROY</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </footer>
    </>
  );
}

export default App;
