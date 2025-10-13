import React, { useState } from "react";
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Link,
  useNavigate,
} from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap/dist/js/bootstrap.bundle.min.js";
import "./App.css";
import ProductGrid from "./components/ProductGrid";
import AccountPage from "./components/AccountPage";
import CartPage from "./components/CartPage";
import FavoritesPage from "./components/FavoritesPage";
import OrderTracking from "./components/OrderTracking";
import PaymentPage from "./components/PaymentPage";
import AdminPanel from "./admin/AdminPanel";
import LoginModal from "./components/LoginModal";
import { useCartCount } from "./hooks/useCartCount";
import { AuthProvider, useAuth } from "./contexts/AuthContext";
import AdminMicro from "./pages/Admin/AdminMicro";
// Admin sayfaları
import AdminLogin from "./pages/Admin/AdminLogin";
import Dashboard from "./pages/Admin/Dashboard";
import AdminUsers from "./pages/Admin/AdminUsers";
import AdminOrders from "./pages/Admin/AdminOrders";
import AdminProducts from "./pages/Admin/AdminProducts";
import AdminCategories from "./pages/Admin/AdminCategories";
// Admin guards
import { AdminGuard, AdminLoginGuard } from "./guards/AdminGuard";
import Home from "./pages/Home";
import Cart from "./pages/Cart";
import Category from "./pages/Category";
import Product from "./pages/Product";
import Checkout from "./pages/Checkout";

function Header() {
  const { count: cartCount } = useCartCount();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [showUserDropdown, setShowUserDropdown] = useState(false);

  const handleAccountClick = () => {
    if (user) {
      setShowUserDropdown(!showUserDropdown);
    } else {
      setShowLoginModal(true);
    }
  };

  const handleLogout = async () => {
    setShowUserDropdown(false);
    await logout();
    navigate("/");
  };

  // Dropdown dışına tıklama ile kapatma
  React.useEffect(() => {
    const handleClickOutside = (event) => {
      if (showUserDropdown && !event.target.closest(".position-relative")) {
        setShowUserDropdown(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [showUserDropdown]);

  return (
    <div className="App">
      {/* Main Header */}
      <header className="main-header bg-white shadow-sm py-1">
        <div className="container-fluid px-4">
          <div className="row align-items-center">
            {/* Logo */}
            <div className="col-md-3">
              <Link to="/" className="text-decoration-none">
                <div className="d-flex align-items-center">
                  <div className="logo-container me-2">
                    <img
                      src="/images/golkoy-logo.png"
                      alt="Gölköy Gourmet Market"
                      style={{
                        height: "150px",
                        width: "auto",
                        filter: "drop-shadow(0 3px 10px rgba(255,107,53,0.4))",
                        transition: "all 0.3s ease",
                        marginTop: "-15px",
                        marginBottom: "-15px",
                      }}
                      onMouseEnter={(e) => {
                        e.target.style.transform = "scale(1.05)";
                        e.target.style.filter =
                          "drop-shadow(0 4px 12px rgba(255,107,53,0.4))";
                      }}
                      onMouseLeave={(e) => {
                        e.target.style.transform = "scale(1)";
                        e.target.style.filter =
                          "drop-shadow(0 2px 8px rgba(255,107,53,0.3))";
                      }}
                    />
                  </div>
                  <div style={{ display: "flex", alignItems: "center" }}>
                    <img
                      src="/images/dogadan-sofranza-logo.png"
                      alt="Doğadan Sofranza"
                      style={{
                        height: "180px",
                        width: "auto",
                        marginLeft: "-40px",
                        marginTop: "-15px",
                        marginBottom: "-15px",
                        filter: "drop-shadow(0 3px 8px rgba(0,0,0,0.2))",
                      }}
                    />
                  </div>
                </div>
              </Link>
            </div>

            {/* Modern Search Bar */}
            <div className="col-md-6">
              <div className="modern-search-container position-relative">
                <div className="input-group shadow-sm">
                  <input
                    type="text"
                    className="form-control form-control-lg border-0"
                    placeholder="Doğal ve kaliteli ürünler arasında ara..."
                    style={{
                      borderRadius: "25px 0 0 25px",
                      fontSize: "0.95rem",
                      paddingLeft: "20px",
                    }}
                  />
                  <button
                    className="btn btn-lg border-0 px-4"
                    style={{
                      background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      borderRadius: "0 25px 25px 0",
                      boxShadow: "0 2px 10px rgba(255,107,53,0.3)",
                    }}
                  >
                    <i className="fas fa-search text-white"></i>
                  </button>
                </div>
                <div className="search-suggestions position-absolute w-100 bg-white rounded-3 shadow-lg mt-1 p-2 d-none">
                  <div className="suggestion-item p-2 rounded text-muted">
                    <i className="fas fa-fire me-2 text-warning"></i>Popüler:
                    Organik süt, Taze meyve, Doğal bal
                  </div>
                </div>
              </div>
            </div>

            {/* Modern Action Buttons */}
            <div className="col-md-3">
              <div className="header-actions d-flex justify-content-end align-items-center gap-3">
                <div className="position-relative">
                  <button
                    onClick={handleAccountClick}
                    className="modern-action-btn d-flex flex-column align-items-center p-2 border-0 bg-transparent"
                    title={user ? `Hoş geldin, ${user.name}` : "Giriş Yap"}
                  >
                    <div
                      className="action-icon p-2 rounded-circle"
                      style={{
                        background: user
                          ? "linear-gradient(135deg, #27ae60, #58d68d)"
                          : "linear-gradient(135deg, #ff6b35, #ff8c00)",
                        boxShadow: "0 2px 8px rgba(255,107,53,0.3)",
                        transition: "all 0.3s ease",
                      }}
                    >
                      <i
                        className={`fas ${
                          user ? "fa-user-check" : "fa-user"
                        } text-white`}
                      ></i>
                    </div>
                    <small className="text-muted fw-semibold mt-1">
                      {user ? user.name.split(" ")[0] : "Giriş Yap"}
                    </small>
                  </button>

                  {user && showUserDropdown && (
                    <div
                      className="position-absolute bg-white shadow-lg rounded-3 border-0 mt-2 overflow-hidden"
                      style={{
                        right: 0,
                        minWidth: "240px",
                        zIndex: 1000,
                        top: "80%",
                        boxShadow: "0 10px 40px rgba(0,0,0,0.15)",
                        backdropFilter: "blur(10px)",
                        animation: "fadeIn 0.2s ease-out",
                      }}
                    >
                      {/* Kullanıcı Bilgileri Header */}
                      <div
                        className="px-4 py-3 border-bottom"
                        style={{
                          background:
                            "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                          color: "white",
                        }}
                      >
                        <div className="d-flex align-items-center">
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center me-3"
                            style={{
                              width: "40px",
                              height: "40px",
                              background: "rgba(255,255,255,0.2)",
                              backdropFilter: "blur(10px)",
                            }}
                          >
                            <i className="fas fa-user text-white"></i>
                          </div>
                          <div>
                            <div className="fw-bold">{user.name}</div>
                            <small style={{ opacity: 0.9 }}>{user.email}</small>
                          </div>
                        </div>
                      </div>

                      {/* Menü Items */}
                      <div className="py-2">
                        <button
                          className="dropdown-item d-flex align-items-center px-4 py-3 border-0 bg-transparent w-100 text-start"
                          style={{
                            transition: "all 0.2s ease",
                            fontSize: "14px",
                          }}
                          onMouseEnter={(e) => {
                            e.target.style.background =
                              "linear-gradient(135deg, #f8f9ff, #e3f2fd)";
                            e.target.style.transform = "translateX(5px)";
                          }}
                          onMouseLeave={(e) => {
                            e.target.style.background = "transparent";
                            e.target.style.transform = "translateX(0)";
                          }}
                          onClick={() => {
                            setShowUserDropdown(false);
                            navigate("/account");
                          }}
                        >
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center me-3"
                            style={{
                              width: "32px",
                              height: "32px",
                              background:
                                "linear-gradient(135deg, #667eea, #764ba2)",
                              color: "white",
                            }}
                          >
                            <i
                              className="fas fa-user-circle"
                              style={{ fontSize: "14px" }}
                            ></i>
                          </div>
                          <div>
                            <div className="fw-semibold text-dark">Hesabım</div>
                            <small className="text-muted">
                              Profil ayarları
                            </small>
                          </div>
                        </button>

                        <button
                          className="dropdown-item d-flex align-items-center px-4 py-3 border-0 bg-transparent w-100 text-start"
                          style={{
                            transition: "all 0.2s ease",
                            fontSize: "14px",
                          }}
                          onMouseEnter={(e) => {
                            e.target.style.background =
                              "linear-gradient(135deg, #fff0f0, #ffe6e6)";
                            e.target.style.transform = "translateX(5px)";
                          }}
                          onMouseLeave={(e) => {
                            e.target.style.background = "transparent";
                            e.target.style.transform = "translateX(0)";
                          }}
                          onClick={() => {
                            setShowUserDropdown(false);
                            navigate("/favorites");
                          }}
                        >
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center me-3"
                            style={{
                              width: "32px",
                              height: "32px",
                              background:
                                "linear-gradient(135deg, #ff6b6b, #ee5a52)",
                              color: "white",
                            }}
                          >
                            <i
                              className="fas fa-heart"
                              style={{ fontSize: "14px" }}
                            ></i>
                          </div>
                          <div>
                            <div className="fw-semibold text-dark">
                              Favorilerim
                            </div>
                            <small className="text-muted">
                              Beğendiğim ürünler
                            </small>
                          </div>
                        </button>

                        <button
                          className="dropdown-item d-flex align-items-center px-4 py-3 border-0 bg-transparent w-100 text-start"
                          style={{
                            transition: "all 0.2s ease",
                            fontSize: "14px",
                          }}
                          onMouseEnter={(e) => {
                            e.target.style.background =
                              "linear-gradient(135deg, #fff5e6, #ffe0cc)";
                            e.target.style.transform = "translateX(5px)";
                          }}
                          onMouseLeave={(e) => {
                            e.target.style.background = "transparent";
                            e.target.style.transform = "translateX(0)";
                          }}
                          onClick={() => {
                            setShowUserDropdown(false);
                            navigate("/orders");
                          }}
                        >
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center me-3"
                            style={{
                              width: "32px",
                              height: "32px",
                              background:
                                "linear-gradient(135deg, #ff9500, #ff6b35)",
                              color: "white",
                            }}
                          >
                            <i
                              className="fas fa-truck"
                              style={{ fontSize: "14px" }}
                            ></i>
                          </div>
                          <div>
                            <div className="fw-semibold text-dark">
                              Siparişlerim
                            </div>
                            <small className="text-muted">Sipariş takibi</small>
                          </div>
                        </button>

                        <hr className="mx-3 my-2" style={{ opacity: 0.1 }} />

                        <button
                          className="dropdown-item d-flex align-items-center px-4 py-3 border-0 bg-transparent w-100 text-start"
                          style={{
                            transition: "all 0.2s ease",
                            fontSize: "14px",
                          }}
                          onMouseEnter={(e) => {
                            e.target.style.background =
                              "linear-gradient(135deg, #ffe6e6, #ffcccc)";
                            e.target.style.transform = "translateX(5px)";
                          }}
                          onMouseLeave={(e) => {
                            e.target.style.background = "transparent";
                            e.target.style.transform = "translateX(0)";
                          }}
                          onClick={handleLogout}
                        >
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center me-3"
                            style={{
                              width: "32px",
                              height: "32px",
                              background:
                                "linear-gradient(135deg, #e74c3c, #c0392b)",
                              color: "white",
                            }}
                          >
                            <i
                              className="fas fa-sign-out-alt"
                              style={{ fontSize: "14px" }}
                            ></i>
                          </div>
                          <div>
                            <div className="fw-semibold text-danger">
                              Çıkış Yap
                            </div>
                            <small className="text-muted">Hesaptan çık</small>
                          </div>
                        </button>
                      </div>
                    </div>
                  )}
                </div>

                <button
                  onClick={() => navigate("/orders")}
                  className="modern-action-btn d-flex flex-column align-items-center p-2 border-0 bg-transparent"
                  title="Siparişlerim"
                >
                  <div
                    className="action-icon p-2 rounded-circle"
                    style={{
                      background: "linear-gradient(135deg, #ff6b35, #ffa500)",
                      boxShadow: "0 2px 8px rgba(255,107,53,0.3)",
                      transition: "all 0.3s ease",
                    }}
                  >
                    <i className="fas fa-truck text-white"></i>
                  </div>
                  <small className="text-muted fw-semibold mt-1">
                    Siparişlerim
                  </small>
                </button>

                <button
                  onClick={() => navigate("/cart")}
                  className="modern-action-btn d-flex flex-column align-items-center p-2 border-0 bg-transparent position-relative"
                  title="Sepetim"
                >
                  <div
                    className="action-icon p-2 rounded-circle position-relative"
                    style={{
                      background: "linear-gradient(135deg, #27ae60, #58d68d)",
                      boxShadow: "0 2px 8px rgba(39,174,96,0.3)",
                      transition: "all 0.3s ease",
                    }}
                  >
                    <i className="fas fa-shopping-cart text-white"></i>
                    {cartCount > 0 && (
                      <span
                        className="position-absolute top-0 start-100 translate-middle badge rounded-pill"
                        style={{
                          background:
                            "linear-gradient(135deg, #e74c3c, #ec7063)",
                          fontSize: "0.65rem",
                          minWidth: "18px",
                          height: "18px",
                          animation: "pulse 2s infinite",
                        }}
                      >
                        {cartCount}
                      </span>
                    )}
                  </div>
                  <small className="text-muted fw-semibold mt-1">Sepetim</small>
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
            <button
              className="category-btn"
              type="button"
              onClick={() => (window.location.href = "/favorites")}
            >
              FAVORİLERİM
            </button>
            <button className="category-btn" type="button">
              KAMPANYALAR
            </button>
          </div>
        </div>
      </nav>

      {/* Login Modal */}
      <LoginModal
        show={showLoginModal}
        onHide={() => setShowLoginModal(false)}
        onLoginSuccess={() => {
          setShowLoginModal(false);
          // Başarılı giriş sonrası gerekli işlemler
        }}
      />
    </div>
  );
}

function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="App">
          <Header />
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/account" element={<AccountPage />} />
            <Route path="/cart" element={<CartPage />} />
            <Route path="/favorites" element={<FavoritesPage />} />
            <Route path="/orders" element={<OrderTracking />} />
            <Route path="/payment" element={<PaymentPage />} />
            {/* Ana site rotaları */}
            <Route path="/pages/home" element={<Home />} />
            <Route path="/pages/cart" element={<Cart />} />
            <Route path="/category/:slug" element={<Category />} />
            <Route path="/product/:id" element={<Product />} />
            <Route path="/checkout" element={<Checkout />} />

            {/* Eski admin (geçici) */}
            <Route path="/admin" element={<AdminPanel />} />
            <Route path="/admin/micro" element={<AdminMicro />} />

            {/* Yeni Admin Panel Rotaları */}
            <Route
              path="/admin/login"
              element={
                <AdminLoginGuard>
                  <AdminLogin />
                </AdminLoginGuard>
              }
            />
            <Route
              path="/admin/dashboard"
              element={
                <AdminGuard>
                  <Dashboard />
                </AdminGuard>
              }
            />
            <Route
              path="/admin/users"
              element={
                <AdminGuard>
                  <AdminUsers />
                </AdminGuard>
              }
            />
            <Route
              path="/admin/orders"
              element={
                <AdminGuard>
                  <AdminOrders />
                </AdminGuard>
              }
            />
            <Route
              path="/admin/products"
              element={
                <AdminGuard>
                  <AdminProducts />
                </AdminGuard>
              }
            />
            <Route
              path="/admin/categories"
              element={
                <AdminGuard>
                  <AdminCategories />
                </AdminGuard>
              }
            />
          </Routes>
        </div>
      </Router>
    </AuthProvider>
  );
}

function HomePage() {
  const [currentSlide, setCurrentSlide] = React.useState(0);
  const slides = [
    {
      id: 1,
      title: "TAZE VE DOĞAL İNDİRİM REYONU",
      subtitle: "",
      description: "",
      badge: "",
      image: "/images/taze-dogal-indirim-banner.png",
    },
    {
      id: 2,
      title: "İLK ALIŞVERİŞİNİZE %25 İNDİRİM",
      subtitle: "",
      description: "",
      badge: "",
      image: "/images/ilk-alisveris-indirim-banner.png",
    },
    {
      id: 3,
      title: "MEYVE REYONUMUZ",
      subtitle: "EN TAZELERİ",
      description: "",
      badge: "",
      image: "/images/meyve-reyonu-banner.png",
    },
  ];

  // Auto-slide effect
  React.useEffect(() => {
    const interval = setInterval(() => {
      setCurrentSlide((prev) => (prev + 1) % slides.length);
    }, 4000); // 4 saniyede bir değişir
    return () => clearInterval(interval);
  }, [slides.length]);

  return (
    <>
      {/* Auto Kampanya Carousel */}
      <section
        className="campaign-carousel py-4"
        style={{ overflowX: "hidden" }}
      >
        <div
          className="container-fluid"
          style={{ padding: "0 15px", maxWidth: "100vw" }}
        >
          <div className="position-relative">
            {/* Carousel Container */}
            <div
              className="carousel-container"
              style={{
                borderRadius: "10px",
                overflow: "hidden",
                position: "relative",
                width: "100%",
                maxWidth: "calc(100vw - 30px)",
                margin: "0 auto",
              }}
            >
              {slides.map((slide, index) => (
                <div
                  key={slide.id}
                  className={`carousel-slide ${
                    index === currentSlide ? "active" : ""
                  }`}
                  style={{
                    minHeight: "500px",
                    height: "500px",
                    position: index === currentSlide ? "relative" : "absolute",
                    top: index === currentSlide ? "auto" : "0",
                    left: index === currentSlide ? "auto" : "0",
                    width: "100%",
                    maxWidth: "100%",
                    overflow: "hidden",
                    opacity: index === currentSlide ? 1 : 0,
                    transition: "opacity 0.8s ease-in-out",
                    zIndex: index === currentSlide ? 2 : 1,
                    backgroundImage: `url(${slide.image})`,
                    backgroundSize: "100% auto",
                    backgroundPosition: "center 55%",
                    backgroundRepeat: "no-repeat",
                  }}
                >
                  {/* Slide Counter */}
                  <div className="position-absolute top-0 end-0 m-3">
                    <span className="badge bg-dark bg-opacity-50 px-3 py-2 rounded-pill">
                      {currentSlide + 1} / {slides.length}
                    </span>
                  </div>

                  {/* Slide Indicators */}
                  <div className="position-absolute bottom-0 start-50 translate-middle-x mb-4">
                    <div className="d-flex gap-2">
                      {slides.map((_, indicatorIndex) => (
                        <button
                          key={indicatorIndex}
                          className={`slide-indicator ${
                            indicatorIndex === currentSlide ? "active" : ""
                          }`}
                          style={{
                            width: "12px",
                            height: "12px",
                            borderRadius: "50%",
                            border: "none",
                            background:
                              indicatorIndex === currentSlide
                                ? "white"
                                : "rgba(255,255,255,0.5)",
                            transition: "all 0.3s ease",
                            cursor: "pointer",
                          }}
                          onClick={() => setCurrentSlide(indicatorIndex)}
                        />
                      ))}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* İlgini Çekebilecek Ürünler Section */}
      <section
        className="featured-products-section py-5"
        style={{
          background: "linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%)",
          position: "relative",
        }}
      >
        <div
          className="section-bg-pattern position-absolute w-100 h-100"
          style={{
            backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ff6b35' fill-opacity='0.03'%3E%3Cpath d='m36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
            opacity: 0.5,
          }}
        ></div>

        <div className="container-fluid px-4 position-relative">
          {/* Modern Section Header */}
          <div className="text-center mb-5">
            <div
              className="section-header-animation"
              style={{ animation: "fadeInUp 1s ease" }}
            >
              <div className="section-badge mb-3">
                <span
                  className="modern-badge px-4 py-2 rounded-pill fw-bold"
                  style={{
                    background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                    color: "white",
                    fontSize: "0.85rem",
                    letterSpacing: "0.5px",
                    textTransform: "uppercase",
                    boxShadow: "0 4px 15px rgba(255, 107, 53, 0.3)",
                  }}
                >
                  <i className="fas fa-fire me-2 animate__animated animate__pulse animate__infinite"></i>
                  Özel Seçimler
                </span>
              </div>

              <h2
                className="section-title mb-3"
                style={{
                  fontSize: "2.5rem",
                  fontWeight: "800",
                  background: "linear-gradient(135deg, #2c3e50, #34495e)",
                  backgroundClip: "text",
                  WebkitBackgroundClip: "text",
                  WebkitTextFillColor: "transparent",
                  letterSpacing: "-0.5px",
                }}
              >
                İlgini Çekebilecek Ürünler
              </h2>

              <p
                className="section-description text-muted mb-4"
                style={{
                  fontSize: "1.1rem",
                  maxWidth: "600px",
                  margin: "0 auto",
                }}
              >
                Gölköy Gourmet Market'in özenle seçilmiş, doğal ve taze
                ürünleriyle tanışın
              </p>

              <div
                className="section-decorative-line mx-auto"
                style={{
                  width: "80px",
                  height: "3px",
                  background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                  borderRadius: "2px",
                }}
              ></div>
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
                <div className="journey-icon mb-4">
                  <div className="animated-icons">
                    <i className="fas fa-heart pulse-icon"></i>
                    <i className="fas fa-handshake bounce-icon"></i>
                    <i className="fas fa-star twinkle-icon"></i>
                  </div>
                </div>

                <h2 className="journey-title mb-4 animated-title">
                  Bu Yolculukta Bizimle Olun
                </h2>

                <p className="journey-subtitle mb-4 fade-in-text">
                  Gölköy Gourmet Market ailesi olarak, sizlerle birlikte büyümek
                  ve gelişmek istiyoruz.
                  <br />
                  Görüşleriniz bizim için çok değerli.
                </p>

                <div className="journey-buttons animated-buttons">
                  <button className="btn btn-light btn-lg me-3 hover-lift">
                    <i className="fas fa-envelope me-2"></i>
                    İletişime Geçin
                  </button>
                  <button className="btn btn-outline-light btn-lg hover-lift">
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
              <div className="newsletter-icon-animation mb-4">
                <div className="floating-envelope">
                  <i className="fas fa-envelope-open newsletter-main-icon"></i>
                  <div className="newsletter-sparkles">
                    <i className="fas fa-star sparkle-1"></i>
                    <i className="fas fa-star sparkle-2"></i>
                    <i className="fas fa-star sparkle-3"></i>
                    <i className="fas fa-star sparkle-4"></i>
                  </div>
                </div>
              </div>

              <h2 className="newsletter-title mb-3 newsletter-title-animated">
                Yeni Ürünlerden İlk Sen Haberdar Ol
              </h2>

              <p className="newsletter-subtitle mb-4 newsletter-subtitle-animated">
                Yeni ürünler, özel kampanyalar ve fırsatlar için e-bültenimize
                katıl
              </p>

              <div className="newsletter-form newsletter-form-animated">
                <div className="input-group newsletter-input-group">
                  <input
                    type="email"
                    className="form-control form-control-lg newsletter-input"
                    placeholder="E-mail adresiniz"
                  />
                  <button className="btn btn-light btn-lg newsletter-btn">
                    <i className="fas fa-paper-plane me-2"></i>
                    Katıl
                  </button>
                </div>
                <div className="newsletter-note mt-3 newsletter-note-animated">
                  <i className="fas fa-heart me-2 beating-heart"></i>
                  Spam göndermiyoruz, sadece değerli içerik
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Modern Footer */}
      <footer
        className="modern-footer"
        style={{
          background: "linear-gradient(135deg, #2c3e50 0%, #34495e 100%)",
          color: "white",
        }}
      >
        <div className="container-fluid px-4 py-5">
          <div className="row">
            {/* Company Info */}
            <div className="col-lg-4 col-md-6 mb-4">
              <div className="footer-brand">
                <div className="d-flex align-items-center mb-4">
                  <img
                    src="/images/golkoy-logo.png"
                    alt="Gölköy Gourmet Market"
                    style={{
                      height: "140px",
                      width: "auto",
                      filter: "drop-shadow(0 3px 12px rgba(255,107,53,0.4))",
                    }}
                    className="me-4"
                  />
                  <img
                    src="/images/dogadan-sofranza-logo.png"
                    alt="Doğadan Sofranza"
                    style={{
                      height: "140px",
                      width: "auto",
                      filter: "drop-shadow(0 3px 12px rgba(255,107,53,0.4))",
                    }}
                  />
                </div>
                <p className="footer-description">
                  Gölköy Gurme olarak, doğanın bize sunduğu en saf ve lezzetli
                  ürünleri, en yüksek kalite standartlarında siz değerli
                  müşterilerimize sunmayı amaçlıyoruz.
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
                    Meyve & Sebze
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Et & Tavuk & Balık
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Süt Ürünleri
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Temel Gıda
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    İçecekler
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Atıştırmalık
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Temizlik
                  </a>
                </li>
                <li>
                  <a href="#" className="footer-link">
                    Favorilerim
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
                    <strong>+90 533 478 30 72</strong>
                    <br />
                    <small>Müşteri Hizmetleri</small>
                  </div>
                </div>
                <div className="contact-item">
                  <i className="fas fa-envelope text-warning me-2"></i>
                  <div>
                    <strong>golturkbuku@golkoygurme.com.tr</strong>
                    <br />
                    <small>Genel bilgi ve destek</small>
                  </div>
                </div>
                <div className="contact-item">
                  <i className="fas fa-map-marker-alt text-warning me-2"></i>
                  <div>
                    <strong>Gölköy Mah. 67 Sokak No: 1/A Bodrum/Muğla</strong>
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
                  <span>
                    Copyright ©2022 - 2024 Tüm Hakları İdol Media'ya Aittir.
                  </span>
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
