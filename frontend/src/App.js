import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap/dist/js/bootstrap.bundle.min.js";
import React, { useState } from "react";
import { GoogleOAuthProvider } from "@react-oauth/google";
import {
  Link,
  Route,
  BrowserRouter as Router,
  Routes,
  useLocation,
  useNavigate,
} from "react-router-dom";
import AdminPanel from "./admin/AdminPanel";
import "./App.css";
import "./styles/adminMobile.css";
import AccountPage from "./components/AccountPage";
import CartPage from "./components/CartPage";
import FavoritesPage from "./components/FavoritesPage";
import LoginModal from "./components/LoginModal";
import OrderSuccess from "./components/OrderSuccess";
import OrderTracking from "./components/OrderTracking";
import PaymentPage from "./components/PaymentPage";
import ProductGrid from "./components/ProductGrid";
import { AuthProvider, useAuth } from "./contexts/AuthContext";
import { CartProvider, useCart } from "./contexts/CartContext";
import { FavoriteProvider, useFavorites } from "./contexts/FavoriteContext";
import { ProductProvider } from "./contexts/ProductContext";
import { CourierAuthProvider } from "./contexts/CourierAuthContext";
import { CourierSignalRProvider } from "./contexts/CourierSignalRContext";
import { useCartCount } from "./hooks/useCartCount";
import AdminIndex from "./pages/Admin/AdminIndex.jsx";
import AdminMicro from "./pages/Admin/AdminMicro";
// Admin sayfaları
import AdminCampaigns from "./pages/Admin/AdminCampaigns";
import AdminCategories from "./pages/Admin/AdminCategories.jsx";
import AdminCouriers from "./pages/Admin/AdminCouriers";
import AdminLogin from "./pages/Admin/AdminLogin";
import AdminOrders from "./pages/Admin/AdminOrders";
import AdminProducts from "./pages/Admin/AdminProducts";
import AdminReports from "./pages/Admin/AdminReports";
import AdminUsers from "./pages/Admin/AdminUsers";
import AdminWeightManagement from "./pages/Admin/AdminWeightManagement";
import PosterManagement from "./pages/Admin/PosterManagement";
import AdminHomeBlocks from "./pages/Admin/AdminHomeBlocks"; // Ana sayfa blok yönetimi
import Dashboard from "./pages/Admin/Dashboard";
// Kupon Yönetimi
import CouponManagement from "./pages/Admin/CouponManagement";
// Bülten Yönetimi
import AdminNewsletter from "./pages/Admin/AdminNewsletter";
// Kargo Ayarları
import AdminShippingSettings from "./pages/Admin/AdminShippingSettings";
import AuditLogsPage from "./pages/Admin/logs/AuditLogsPage";
import ErrorLogsPage from "./pages/Admin/logs/ErrorLogsPage";
import InventoryLogsPage from "./pages/Admin/logs/InventoryLogsPage";
import SystemLogsPage from "./pages/Admin/logs/SystemLogsPage";
// Rol ve İzin Yönetimi Sayfaları
import AdminAccessDenied from "./pages/Admin/AdminAccessDenied";
// Kurye sayfaları
import CourierDashboard from "./pages/Courier/CourierDashboard";
import CourierLogin from "./pages/Courier/CourierLogin";
import CourierOrders from "./pages/Courier/CourierOrders";
import CourierDeliveryDetail from "./pages/Courier/CourierDeliveryDetail";
import CourierWeightEntry from "./pages/Courier/CourierWeightEntry";
// Dispatcher (Sevkiyat Görevlisi) sayfaları
import DispatcherLogin from "./pages/Dispatcher/DispatcherLogin";
import DispatcherDashboard from "./pages/Dispatcher/DispatcherDashboard";
import { DispatcherAuthProvider } from "./contexts/DispatcherAuthContext";
import {
  DispatcherGuard,
  DispatcherLoginGuard,
} from "./guards/DispatcherGuard";
// Store Attendant (Market Görevlisi) sayfaları
import StoreAttendantLogin from "./pages/StoreAttendant/StoreAttendantLogin";
import StoreAttendantDashboard from "./pages/StoreAttendant/StoreAttendantDashboard";
import { StoreAttendantAuthProvider } from "./contexts/StoreAttendantAuthContext";
import {
  StoreAttendantGuard,
  StoreAttendantLoginGuard,
} from "./guards/StoreAttendantGuard";
// Admin guards
import Footer from "./components/Footer";
import { GlobalToastContainer } from "./components/ToastProvider";
import { AdminGuard, AdminLoginGuard } from "./guards/AdminGuard";
import AdminLayout from "./components/AdminLayout";
import { CompareProvider, useCompare } from "./contexts/CompareContext";
import About from "./pages/About.jsx";
import Addresses from "./pages/Addresses";
import CampaignDetail from "./pages/CampaignDetail.jsx";
import Campaigns from "./pages/Campaigns.jsx";
import Career from "./pages/Career.jsx";
import Cart from "./pages/Cart";
import Category from "./pages/Category";
import Checkout from "./pages/Checkout";
import CollectionPage from "./pages/CollectionPage"; // Koleksiyon/Blok Ürün Sayfası
import ComparePage from "./pages/ComparePage";
import Contact from "./pages/Contact.jsx";
import Faq from "./pages/Faq.jsx";
import Feedback from "./pages/Feedback.jsx";
import HelpCenter from "./pages/HelpCenter.jsx";
import Home from "./pages/Home";
import OrderHistory from "./pages/OrderHistory";
import PaymentOptions from "./pages/PaymentOptions.jsx";
import PressKit from "./pages/PressKit.jsx";
import Product from "./pages/Product";
import Profile from "./pages/Profile";
import RecipePage from "./pages/RecipePage"; // Şef Tavsiyesi Tarif Sayfası
import ResetPassword from "./pages/ResetPassword.jsx";
import Returns from "./pages/Returns.jsx";
import SearchPage from "./pages/SearchPage";
import SecurityInfo from "./pages/SecurityInfo.jsx";
import ShippingInfo from "./pages/ShippingInfo.jsx";
import Sustainability from "./pages/Sustainability.jsx";
import VisionMission from "./pages/VisionMission.jsx";
import SearchAutocomplete from "./components/SearchAutocomplete";
import categoryServiceReal from "./services/categoryServiceReal";
import bannerService from "./services/bannerService";
import homeBlockService from "./services/homeBlockService";
import ProductBlockSection from "./pages/components/ProductBlockSection";
import { subscribe, SUBSCRIPTION_SOURCES } from "./services/newsletterService";
// 3D Secure Ödeme Callback Sayfaları
import {
  PaymentSuccessPage,
  PaymentFailurePage,
  ThreeDSecureCallbackPage,
} from "./components/payment/PaymentResultPages";
// Mobil Bottom Navigation
import MobileBottomNav from "./components/MobileBottomNav";
import "./styles/mobileNav.css";
import "./styles/panelMobile.css"; // Panel sayfaları için mobil stiller

function Header() {
  const { count: cartCount } = useCartCount();
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [showUserDropdown, setShowUserDropdown] = useState(false);
  const [categories, setCategories] = useState([]);

  // ============================================================
  // KATEGORİLERİ YÜKLE - Backend API'den
  // Bu effect component mount olduğunda çalışır ve kategorileri çeker
  // ============================================================
  React.useEffect(() => {
    // Kategorileri API'den çekip state'e kaydet
    const loadCategories = async () => {
      try {
        const cats = await categoryServiceReal.getActive();
        setCategories(cats || []);
      } catch (err) {
        // Hata durumunda konsola yaz, UI boş kategori ile devam eder
        console.error("[Header] Kategoriler yüklenemedi:", err.message);
        setCategories([]);
      }
    };

    // İlk yükleme
    loadCategories();

    // Admin panelden kategori güncellenirse yeniden yükle
    const unsubscribe = categoryServiceReal.subscribe(loadCategories);

    // Cleanup: component unmount olduğunda subscription'ı kaldır
    return () => unsubscribe && unsubscribe();
  }, []);

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

  // Slug oluşturma fonksiyonu
  const createSlug = (name) => {
    return name
      .toLowerCase()
      .replace(/ç/g, "c")
      .replace(/ğ/g, "g")
      .replace(/ı/g, "i")
      .replace(/ö/g, "o")
      .replace(/ş/g, "s")
      .replace(/ü/g, "u")
      .replace(/[^a-z0-9\s-]/g, "")
      .replace(/\s+/g, "-")
      .replace(/-+/g, "-")
      .trim();
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
      <GlobalToastContainer />
      {/* Main Header */}
      <header className="main-header bg-white shadow-sm py-1">
        <div className="container-fluid px-4">
          <div className="row align-items-center">
            {/* Logo */}
            <div className="col-md-3 col-auto">
              <Link to="/" className="text-decoration-none">
                <div className="d-flex align-items-center logo-stack">
                  <div className="logo-container">
                    <img
                      src="/images/golkoy-logo.png"
                      alt="Gölköy Gourmet Market"
                      className="main-logo"
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
                  <div
                    className="secondary-logo-container"
                    style={{ display: "flex", alignItems: "center" }}
                  >
                    <img
                      className="secondary-logo"
                      src="/images/dogadan-sofranza-logo.png"
                      alt="Doğadan Sofranza"
                      style={{
                        height: "180px",
                        width: "auto",
                        marginLeft: "-70px",
                        marginTop: "-15px",
                        marginBottom: "-15px",
                        filter: "drop-shadow(0 3px 8px rgba(0,0,0,0.2))",
                      }}
                    />
                  </div>
                </div>
              </Link>
            </div>

            {/* Modern Search Bar - Mobilde gizlenir */}
            <div className="col-md-6 d-none d-md-block">
              <SearchAutocomplete />
            </div>

            {/* Modern Action Buttons */}
            <div className="col-md-3 col-auto ms-auto">
              <div className="header-actions d-flex justify-content-end align-items-center gap-1 gap-md-2">
                {/* Hoş geldin butonu - sadece desktop'ta görünür */}
                <div className="position-relative d-none d-md-block">
                  <button
                    onClick={handleAccountClick}
                    className="modern-action-btn d-flex flex-column align-items-center p-1 p-md-2 border-0 bg-transparent"
                    title={user ? `Hoş geldin, ${user.name}` : "Giriş Yap"}
                  >
                    <div
                      className="action-icon p-1 p-md-2 rounded-circle"
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
                        style={{ fontSize: "0.8rem" }}
                      ></i>
                    </div>
                    <small
                      className="text-muted fw-semibold mt-1 d-none d-md-block"
                      style={{ fontSize: "0.7rem" }}
                    >
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
                            navigate("/profile");
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
                            navigate("/orders/history");
                          }}
                        >
                          <div
                            className="rounded-circle d-flex align-items-center justify-content-center me-3"
                            style={{
                              width: "32px",
                              height: "32px",
                              background:
                                "linear-gradient(135deg, #6c5ce7, #a363d9)",
                              color: "white",
                            }}
                          >
                            <i
                              className="fas fa-history"
                              style={{ fontSize: "14px" }}
                            ></i>
                          </div>
                          <div>
                            <div className="fw-semibold text-dark">
                              Sipariş Geçmişi
                            </div>
                            <small className="text-muted">
                              Önceki siparişlerim
                            </small>
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

                {/* Siparişlerim butonu - Mobilde gizli (bottom nav'da var) */}
                <button
                  onClick={() => navigate("/orders")}
                  className="modern-action-btn d-none d-md-flex flex-column align-items-center p-1 p-md-2 border-0 bg-transparent"
                  title="Siparişlerim"
                >
                  <div
                    className="action-icon p-1 p-md-2 rounded-circle"
                    style={{
                      background: "linear-gradient(135deg, #ff6b35, #ffa500)",
                      boxShadow: "0 2px 8px rgba(255,107,53,0.3)",
                      transition: "all 0.3s ease",
                    }}
                  >
                    <i
                      className="fas fa-truck text-white"
                      style={{ fontSize: "0.8rem" }}
                    ></i>
                  </div>
                  <small
                    className="text-muted fw-semibold mt-1 d-none d-md-block"
                    style={{ fontSize: "0.7rem" }}
                  >
                    Siparişlerim
                  </small>
                </button>

                {/* Sepetim butonu - sadece desktop'ta görünür, mobilde bottom nav'da var */}
                <button
                  onClick={() => navigate("/cart")}
                  className="modern-action-btn d-none d-md-flex flex-column align-items-center p-1 p-md-2 border-0 bg-transparent position-relative"
                  title="Sepetim"
                >
                  <div
                    className="action-icon p-1 p-md-2 rounded-circle position-relative"
                    style={{
                      background: "linear-gradient(135deg, #27ae60, #58d68d)",
                      boxShadow: "0 2px 8px rgba(39,174,96,0.3)",
                      transition: "all 0.3s ease",
                    }}
                  >
                    <i
                      className="fas fa-shopping-cart text-white"
                      style={{ fontSize: "0.8rem" }}
                    ></i>
                    {cartCount > 0 && (
                      <span
                        className="position-absolute top-0 start-100 translate-middle badge rounded-pill"
                        style={{
                          background:
                            "linear-gradient(135deg, #e74c3c, #ec7063)",
                          fontSize: "0.6rem",
                          minWidth: "16px",
                          height: "16px",
                          animation: "pulse 2s infinite",
                        }}
                      >
                        {cartCount}
                      </span>
                    )}
                  </div>
                  <small
                    className="text-muted fw-semibold mt-1 d-none d-md-block"
                    style={{ fontSize: "0.7rem" }}
                  >
                    Sepetim
                  </small>
                </button>
              </div>
            </div>
          </div>

          {/* Mobil Arama - Sadece mobilde görünür */}
          <div className="row d-md-none mt-2">
            <div className="col-12">
              <SearchAutocomplete />
            </div>
          </div>
        </div>
      </header>

      {/* Professional Single Line Category Navigation - Mobilde gizli (bottom nav'da var) */}
      <nav className="single-line-categories d-none d-md-block">
        <div className="container-fluid">
          <div className="category-scroll-container">
            <button
              className={
                "category-btn" + (location.pathname === "/" ? " active" : "")
              }
              type="button"
              onClick={() => navigate("/")}
            >
              KATEGORİLER
            </button>
            {categories.map((cat) => {
              const slug = cat.slug || createSlug(cat.name);
              const isActive = location.pathname.startsWith(
                `/category/${slug}`,
              );
              return (
                <button
                  key={cat.id}
                  className={"category-btn" + (isActive ? " active" : "")}
                  type="button"
                  onClick={() => navigate(`/category/${slug}`)}
                >
                  {cat.name.toUpperCase()}
                </button>
              );
            })}
            <button
              className="category-btn"
              type="button"
              onClick={() => (window.location.href = "/favorites")}
            >
              FAVORİLERİM
            </button>
            <button
              className="category-btn"
              type="button"
              onClick={() => navigate("/campaigns")}
            >
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
  const location = useLocation();
  const showGlobalFooter = location.pathname !== "/";

  // Admin, kurye, dispatcher ve store attendant sayfalarında Header'ı gizle
  const isAdminPage = location.pathname.startsWith("/admin");
  const isCourierPage = location.pathname.startsWith("/courier");
  const isDispatcherPage = location.pathname.startsWith("/dispatch");
  const isStoreAttendantPage = location.pathname.startsWith("/store");
  const showHeader =
    !isAdminPage &&
    !isCourierPage &&
    !isDispatcherPage &&
    !isStoreAttendantPage;

  return (
    <div className="App">
      {showHeader && <Header />}
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/account" element={<AccountPage />} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/favorites" element={<FavoritesPage />} />
        <Route path="/compare" element={<ComparePage />} />
        <Route path="/search" element={<SearchPage />} />
        <Route path="/orders" element={<OrderTracking />} />
        <Route path="/orders/history" element={<OrderHistory />} />
        <Route path="/payment" element={<PaymentPage />} />
        <Route path="/order-success" element={<OrderSuccess />} />
        <Route path="/reset-password" element={<ResetPassword />} />
        {/* Ana site rotaları */}
        <Route path="/pages/home" element={<Home />} />
        <Route path="/pages/cart" element={<Cart />} />
        <Route path="/category/:slug" element={<Category />} />
        <Route path="/kategori/:slug" element={<Category />} />
        <Route path="/collection/:slug" element={<CollectionPage />} />
        <Route path="/campaigns" element={<Campaigns />} />
        <Route path="/campaigns/:slug" element={<CampaignDetail />} />
        <Route path="/product/:id" element={<Product />} />
        <Route path="/checkout" element={<Checkout />} />
        {/* 3D Secure Ödeme Callback Sayfaları */}
        <Route path="/checkout/success" element={<PaymentSuccessPage />} />
        <Route path="/checkout/fail" element={<PaymentFailurePage />} />
        <Route
          path="/checkout/3dsecure-callback"
          element={<ThreeDSecureCallbackPage />}
        />
        <Route path="/profile" element={<Profile />} />
        <Route path="/addresses" element={<Addresses />} />

        {/* Şef Tavsiyesi / Yemek Tarifi Sayfası */}
        <Route path="/tarif" element={<RecipePage />} />
        <Route path="/tarif/:id" element={<RecipePage />} />

        {/* Destek / Bilgi sayfaları */}
        <Route path="/yardim" element={<HelpCenter />} />
        <Route path="/iletisim" element={<Contact />} />
        <Route path="/siparis-takibi" element={<OrderTracking />} />
        <Route path="/iade-degisim" element={<Returns />} />
        <Route path="/kargo-bilgileri" element={<ShippingInfo />} />
        <Route path="/odeme-secenekleri" element={<PaymentOptions />} />
        <Route path="/guvenli-alisveris" element={<SecurityInfo />} />
        <Route path="/sss" element={<Faq />} />
        <Route path="/geri-bildirim" element={<Feedback />} />
        <Route path="/hakkimizda" element={<About />} />
        <Route path="/vizyon-misyon" element={<VisionMission />} />
        <Route path="/kariyer" element={<Career />} />
        <Route path="/basin-kiti" element={<PressKit />} />
        <Route path="/surdurulebilirlik" element={<Sustainability />} />

        {/* Admin giriş noktası */}
        <Route path="/admin" element={<AdminIndex />} />
        {/* Eski admin (geçici) */}
        <Route path="/admin/legacy" element={<AdminPanel />} />

        {/* Yeni Admin Panel Rotaları */}
        <Route
          path="/admin/login"
          element={
            <AdminLoginGuard>
              <AdminLogin />
            </AdminLoginGuard>
          }
        />
        {/* ================================================================
            RBAC İzin Kontrollü Admin Route'ları
            Her route için requiredPermission parametresi eklendi.
            Bu sayede menüde görünmeyen sayfalara URL ile erişim engellenir.
            ================================================================ */}
        <Route
          path="/admin/dashboard"
          element={
            <AdminGuard requiredPermission="dashboard.view">
              <AdminLayout>
                <Dashboard />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/micro"
          element={
            <AdminGuard requiredPermission="settings.system">
              <AdminLayout>
                <AdminMicro />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/users"
          element={
            <AdminGuard requiredPermission="users.view">
              <AdminLayout>
                <AdminUsers />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/orders"
          element={
            <AdminGuard requiredPermission="orders.view">
              <AdminLayout>
                <AdminOrders />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/products"
          element={
            <AdminGuard requiredPermission="products.view">
              <AdminLayout>
                <AdminProducts />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/categories"
          element={
            <AdminGuard requiredPermission="categories.view">
              <AdminLayout>
                <AdminCategories />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/couriers"
          element={
            <AdminGuard requiredPermission="couriers.view">
              <AdminLayout>
                <AdminCouriers />
              </AdminLayout>
            </AdminGuard>
          }
        />
        {/* Reports: reports.view VEYA reports.sales izni gerekli (OR logic) */}
        <Route
          path="/admin/reports"
          element={
            <AdminGuard requiredPermission={["reports.view", "reports.sales"]}>
              <AdminLayout>
                <AdminReports />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/posters"
          element={
            <AdminGuard requiredPermission="banners.view">
              <AdminLayout>
                <PosterManagement />
              </AdminLayout>
            </AdminGuard>
          }
        />
        {/* Ana Sayfa Blok Yönetimi: Admin banner izni gerekli */}
        <Route
          path="/admin/home-blocks"
          element={
            <AdminGuard requiredPermission="banners.view">
              <AdminLayout>
                <AdminHomeBlocks />
              </AdminLayout>
            </AdminGuard>
          }
        />
        {/* Weight Reports: reports.weight VEYA orders.view izni gerekli (OR logic) */}
        <Route
          path="/admin/weight-reports"
          element={
            <AdminGuard requiredPermission={["reports.weight", "orders.view"]}>
              <AdminLayout>
                <AdminWeightManagement />
              </AdminLayout>
            </AdminGuard>
          }
        />
        {/* Weight Management: Ağırlık Bazlı Ödeme Yönetimi */}
        <Route
          path="/admin/weight-management"
          element={
            <AdminGuard requiredPermission={["orders.view", "orders.manage"]}>
              <AdminLayout>
                <AdminWeightManagement />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/campaigns"
          element={
            <AdminGuard requiredPermission="campaigns.view">
              <AdminLayout>
                <AdminCampaigns />
              </AdminLayout>
            </AdminGuard>
          }
        />
        {/* Kargo Ayarları Sayfası */}
        <Route
          path="/admin/shipping-settings"
          element={
            <AdminGuard requiredPermission="settings.system">
              <AdminLayout>
                <AdminShippingSettings />
              </AdminLayout>
            </AdminGuard>
          }
        />
        {/* Kupon Yönetimi Sayfası */}
        <Route
          path="/admin/coupons"
          element={
            <AdminGuard requiredPermission="coupons.view">
              <AdminLayout>
                <CouponManagement />
              </AdminLayout>
            </AdminGuard>
          }
        />
        {/* Bülten Yönetimi Sayfası */}
        <Route
          path="/admin/newsletter"
          element={
            <AdminGuard requiredPermission="newsletter.view">
              <AdminLayout>
                <AdminNewsletter />
              </AdminLayout>
            </AdminGuard>
          }
        />
        {/* Log Sayfaları - Her biri için spesifik izin kontrolü */}
        <Route
          path="/admin/logs/audit"
          element={
            <AdminGuard requiredPermission="logs.audit">
              <AdminLayout>
                <AuditLogsPage />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/logs/errors"
          element={
            <AdminGuard requiredPermission="logs.error">
              <AdminLayout>
                <ErrorLogsPage />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/logs/system"
          element={
            <AdminGuard requiredPermission="logs.view">
              <AdminLayout>
                <SystemLogsPage />
              </AdminLayout>
            </AdminGuard>
          }
        />
        <Route
          path="/admin/logs/inventory"
          element={
            <AdminGuard requiredPermission="logs.view">
              <AdminLayout>
                <InventoryLogsPage />
              </AdminLayout>
            </AdminGuard>
          }
        />

        {/* Rol ve İzin Yönetimi */}
        <Route
          path="/admin/access-denied"
          element={
            <AdminGuard>
              <AdminLayout>
                <AdminAccessDenied />
              </AdminLayout>
            </AdminGuard>
          }
        />

        {/* Kurye Panel Rotaları */}
        <Route path="/courier/login" element={<CourierLogin />} />
        <Route path="/courier/dashboard" element={<CourierDashboard />} />
        <Route path="/courier/orders" element={<CourierOrders />} />
        <Route
          path="/courier/orders/:taskId"
          element={<CourierDeliveryDetail />}
        />
        <Route
          path="/courier/orders/:orderId/weight"
          element={<CourierWeightEntry />}
        />

        {/* Dispatcher (Sevkiyat Görevlisi) Panel Rotaları */}
        <Route
          path="/dispatch/login"
          element={
            <DispatcherLoginGuard>
              <DispatcherLogin />
            </DispatcherLoginGuard>
          }
        />
        <Route
          path="/dispatch/dashboard"
          element={
            <DispatcherGuard>
              <DispatcherDashboard />
            </DispatcherGuard>
          }
        />
        <Route
          path="/dispatch"
          element={
            <DispatcherGuard>
              <DispatcherDashboard />
            </DispatcherGuard>
          }
        />

        {/* Store Attendant (Market Görevlisi) Panel Rotaları */}
        <Route
          path="/store/login"
          element={
            <StoreAttendantLoginGuard>
              <StoreAttendantLogin />
            </StoreAttendantLoginGuard>
          }
        />
        <Route
          path="/store/dashboard"
          element={
            <StoreAttendantGuard>
              <StoreAttendantDashboard />
            </StoreAttendantGuard>
          }
        />
        <Route
          path="/store"
          element={
            <StoreAttendantGuard>
              <StoreAttendantDashboard />
            </StoreAttendantGuard>
          }
        />
      </Routes>

      {/* Footer - Mobilde gizli (desktop-only-footer class'ı ile) */}
      {showGlobalFooter && showHeader && <Footer />}

      {/* Mobile Bottom Navigation - Sadece mobilde görünür, admin/kurye sayfalarında gizli */}
      {showHeader && <MobileBottomNav />}
    </div>
  );
}

// Google OAuth Client ID
const GOOGLE_CLIENT_ID =
  "305859967035-hbb2ef3s1e5nb3j2gv4ucbl0om56oaf0.apps.googleusercontent.com";

function AppWithProviders() {
  return (
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <AuthProvider>
        <CourierAuthProvider>
          <CourierSignalRProvider>
            <DispatcherAuthProvider>
              <StoreAttendantAuthProvider>
                <ProductProvider>
                  <CartProvider>
                    <FavoriteProvider>
                      <CompareProvider>
                        <Router>
                          <ScrollToTop />
                          <App />
                          <CompareFloatingButton />
                        </Router>
                      </CompareProvider>
                    </FavoriteProvider>
                  </CartProvider>
                </ProductProvider>
              </StoreAttendantAuthProvider>
            </DispatcherAuthProvider>
          </CourierSignalRProvider>
        </CourierAuthProvider>
      </AuthProvider>
    </GoogleOAuthProvider>
  );
}

// Karşılaştırma Floating Button
function CompareFloatingButton() {
  const { compareItems } = useCompare();
  const navigate = useNavigate();
  const location = useLocation();

  // Admin, kurye, dispatcher veya store attendant sayfalarında gösterme
  if (
    location.pathname.startsWith("/admin") ||
    location.pathname.startsWith("/courier") ||
    location.pathname.startsWith("/dispatch") ||
    location.pathname.startsWith("/store")
  ) {
    return null;
  }

  if (compareItems.length === 0) return null;

  return (
    <button
      onClick={() => navigate("/compare")}
      className="btn btn-warning shadow-lg"
      style={{
        position: "fixed",
        bottom: "100px",
        right: "20px",
        zIndex: 1000,
        borderRadius: "50px",
        padding: "12px 20px",
        fontWeight: "bold",
      }}
    >
      <i className="fas fa-balance-scale me-2"></i>
      Karşılaştır ({compareItems.length})
    </button>
  );
}

function ScrollToTop() {
  const location = useLocation();

  React.useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: "instant" });
  }, [location.pathname]);

  return null;
}

const NEWSLETTER_EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function HomePage() {
  const [currentSlide, setCurrentSlide] = React.useState(0);
  const [isPaused, setIsPaused] = React.useState(false);
  const [slides, setSlides] = React.useState([]);
  const [promoImages, setPromoImages] = React.useState([]);
  const [recipeBanners, setRecipeBanners] = React.useState([]);
  const [newsletterEmail, setNewsletterEmail] = React.useState("");
  const [newsletterStatus, setNewsletterStatus] = React.useState("idle");
  const [newsletterMessage, setNewsletterMessage] = React.useState("");
  const [isNewsletterSubscribed, setIsNewsletterSubscribed] =
    React.useState(false);

  // Ana Sayfa Blokları State'leri (Admin panelden yönetilen poster + ürün blokları)
  const [homeBlocks, setHomeBlocks] = React.useState([]);
  const [blocksLoading, setBlocksLoading] = React.useState(true);

  React.useEffect(() => {
    const cachedSubscription = localStorage.getItem("newsletter_subscribed");
    if (cachedSubscription) {
      setIsNewsletterSubscribed(true);
    }
  }, []);

  // Ana Sayfa Bloklarını API'den çek - homeBlockService kullanarak
  React.useEffect(() => {
    const fetchHomeBlocks = async () => {
      console.log("[HomePage] 🚀 Home blocks yükleme başlatılıyor...");
      setBlocksLoading(true);
      try {
        const blocks = await homeBlockService.getActiveBlocks();
        console.log(
          "[HomePage] 📦 Home blocks loaded:",
          blocks?.length || 0,
          blocks,
        );
        setHomeBlocks(blocks || []);
      } catch (err) {
        console.error("[HomePage] ❌ Home blocks error:", err?.message || err);
        setHomeBlocks([]);
      } finally {
        setBlocksLoading(false);
        console.log("[HomePage] ✅ Home blocks yükleme tamamlandı");
      }
    };
    fetchHomeBlocks();
  }, []);

  // Posterleri API'den çek - bannerService kullanarak
  React.useEffect(() => {
    const fetchPosters = async () => {
      try {
        // Slider, promo ve recipe bannerları paralel olarak çek
        const [sliderData, promoData, recipeData] = await Promise.all([
          bannerService.getSliderBanners(),
          bannerService.getPromoBanners(),
          bannerService.getRecipeBanners(),
        ]);

        console.log("[HomePage] Slider banners:", sliderData?.length || 0);
        console.log("[HomePage] Promo banners:", promoData?.length || 0);
        console.log("[HomePage] 🍳 Recipe banners:", recipeData?.length || 0);

        // Slider banner'ları ayarla
        if (Array.isArray(sliderData) && sliderData.length > 0) {
          setSlides(
            sliderData.map((p) => ({
              id: p.id,
              title: p.title,
              image: p.imageUrl,
              link: p.linkUrl,
            })),
          );
        } else {
          // Slider için fallback
          setSlides([
            {
              id: 1,
              title: "TAZE VE DOĞAL İNDİRİM REYONU",
              image: "/images/taze-dogal-indirim-banner.png",
            },
            {
              id: 2,
              title: "İLK ALIŞVERİŞİNİZE %25 İNDİRİM",
              image: "/images/ilk-alisveris-indirim-banner.png",
            },
            {
              id: 3,
              title: "MEYVE REYONUMUZ",
              image: "/images/meyve-reyonu-banner.png",
            },
          ]);
        }

        // Promo banner'ları ayarla
        if (Array.isArray(promoData) && promoData.length > 0) {
          setPromoImages(
            promoData.map((p) => ({
              id: p.id,
              title: p.title,
              image: p.imageUrl,
              link: p.linkUrl,
            })),
          );
        } else {
          // Promo için fallback
          setPromoImages([
            {
              id: 1,
              title: "Temizlik Malzemeleri",
              image: "/images/temizlik-malzemeleri.png",
            },
            {
              id: 2,
              title: "Taze ve Doğal",
              image: "/images/taze-dogal-urunler.png",
            },
            {
              id: 3,
              title: "Taze Günlük Lezzetli",
              image: "/images/taze-gunluk-lezzetli.png",
            },
            {
              id: 4,
              title: "Özel Fiyat Köy Sütü",
              image: "/images/ozel-fiyat-koy-sutu.png",
            },
          ]);
        }

        // Recipe banner'ları ayarla
        if (Array.isArray(recipeData) && recipeData.length > 0) {
          setRecipeBanners(
            recipeData.map((p) => ({
              id: p.id,
              title: p.title,
              image: p.imageUrl,
              link: p.linkUrl || `/tarif/${p.id}`,
            })),
          );
        }
      } catch (error) {
        console.error("[HomePage] Poster yükleme hatası:", error);
        // Fallback statik veriler
        setSlides([
          {
            id: 1,
            title: "TAZE VE DOĞAL İNDİRİM REYONU",
            image: "/images/taze-dogal-indirim-banner.png",
          },
          {
            id: 2,
            title: "İLK ALIŞVERİŞİNİZE %25 İNDİRİM",
            image: "/images/ilk-alisveris-indirim-banner.png",
          },
          {
            id: 3,
            title: "MEYVE REYONUMUZ",
            image: "/images/meyve-reyonu-banner.png",
          },
        ]);
        setPromoImages([
          {
            id: 1,
            title: "Temizlik Malzemeleri",
            image: "/images/temizlik-malzemeleri.png",
          },
          {
            id: 2,
            title: "Taze ve Doğal",
            image: "/images/taze-dogal-urunler.png",
          },
          {
            id: 3,
            title: "Taze Günlük Lezzetli",
            image: "/images/taze-gunluk-lezzetli.png",
          },
          {
            id: 4,
            title: "Özel Fiyat Köy Sütü",
            image: "/images/ozel-fiyat-koy-sutu.png",
          },
        ]);
      }
    };
    fetchPosters();
  }, []);

  const handleNewsletterSubmit = async (event) => {
    event.preventDefault();

    const emailValue = newsletterEmail.trim();

    if (!emailValue) {
      setNewsletterStatus("error");
      setNewsletterMessage("Lütfen e-posta adresinizi girin.");
      return;
    }

    if (!NEWSLETTER_EMAIL_REGEX.test(emailValue)) {
      setNewsletterStatus("error");
      setNewsletterMessage("Geçerli bir e-posta adresi girin.");
      return;
    }

    try {
      setNewsletterStatus("loading");
      setNewsletterMessage("");

      const result = await subscribe({
        email: emailValue,
        fullName: null,
        source: SUBSCRIPTION_SOURCES.WEB_FOOTER,
      });

      if (result.success) {
        localStorage.setItem(
          "newsletter_subscribed",
          JSON.stringify({
            email: emailValue.toLowerCase(),
            subscribedAt: new Date().toISOString(),
            source: SUBSCRIPTION_SOURCES.WEB_FOOTER,
            subscriberId: result.subscriberId,
          }),
        );

        setNewsletterStatus("success");
        setNewsletterMessage(
          result.message || "Bültenimize başarıyla abone oldunuz!",
        );
        setIsNewsletterSubscribed(true);
        setNewsletterEmail("");
      } else {
        setNewsletterStatus("error");
        setNewsletterMessage(
          result.message || "Abonelik işlemi başarısız oldu.",
        );
      }
    } catch (error) {
      console.error("[HomePage] Newsletter subscribe error:", error);
      setNewsletterStatus("error");
      setNewsletterMessage("Bağlantı hatası, lütfen tekrar deneyin.");
    }
  };

  // Auto-slide effect
  React.useEffect(() => {
    if (isPaused || slides.length === 0) return;
    const interval = setInterval(() => {
      setCurrentSlide((prev) => (prev + 1) % slides.length);
    }, 4000);
    return () => clearInterval(interval);
  }, [slides.length, isPaused]);

  return (
    <>
      {/* Auto Kampanya Carousel */}
      <section
        className="campaign-carousel py-4"
        style={{ overflowX: "hidden" }}
        onMouseEnter={() => setIsPaused(true)}
        onMouseLeave={() => setIsPaused(false)}
        aria-label="Kampanya Carousel"
      >
        <div
          className="container-fluid"
          style={{ padding: "0 15px", maxWidth: "100vw" }}
        >
          <div className="position-relative">
            {/* Navigation Arrows */}
            <button
              className="carousel-nav carousel-nav-prev position-absolute top-50 start-0 translate-middle-y ms-3"
              style={{
                width: "50px",
                height: "50px",
                borderRadius: "50%",
                border: "none",
                background: "rgba(255,255,255,0.95)",
                boxShadow: "0 4px 15px rgba(0,0,0,0.2)",
                cursor: "pointer",
                zIndex: 10,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                transition: "all 0.3s ease",
              }}
              onClick={() =>
                setCurrentSlide(
                  (prev) => (prev - 1 + slides.length) % slides.length,
                )
              }
              aria-label="Önceki Slayt"
            >
              <i
                className="fas fa-chevron-left"
                style={{ fontSize: "18px", color: "#ff6b35" }}
              ></i>
            </button>

            <button
              className="carousel-nav carousel-nav-next position-absolute top-50 end-0 translate-middle-y me-3"
              style={{
                width: "50px",
                height: "50px",
                borderRadius: "50%",
                border: "none",
                background: "rgba(255,255,255,0.95)",
                boxShadow: "0 4px 15px rgba(0,0,0,0.2)",
                cursor: "pointer",
                zIndex: 10,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                transition: "all 0.3s ease",
              }}
              onClick={() =>
                setCurrentSlide((prev) => (prev + 1) % slides.length)
              }
              aria-label="Sonraki Slayt"
            >
              <i
                className="fas fa-chevron-right"
                style={{ fontSize: "18px", color: "#ff6b35" }}
              ></i>
            </button>

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
                    minHeight: "260px",
                    height: "min(500px, 70vw)",
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
                    backgroundSize: "cover",
                    backgroundPosition: "center center",
                    backgroundRepeat: "no-repeat",
                  }}
                  role="group"
                  aria-label={`Slayt ${index + 1} / ${slides.length}: ${
                    slide.title
                  }`}
                  aria-hidden={index !== currentSlide}
                >
                  {/* Slide content (title, subtitle, etc.) eklenebilir */}
                </div>
              ))}
              {/* Slide Indicators */}
              <div className="position-absolute bottom-0 start-50 translate-middle-x mb-4">
                <div
                  className="d-flex gap-2"
                  role="tablist"
                  aria-label="Slayt Göstergeleri"
                >
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
                      role="tab"
                      aria-label={`Slayt ${indicatorIndex + 1}`}
                      aria-selected={indicatorIndex === currentSlide}
                    />
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ========== PROMOSYON BANNER'LARI (2x2 Grid) ========== */}
      <section className="promo-section py-3" style={{ background: "#f8f9fa" }}>
        <div className="container-fluid px-4">
          <div className="promo-grid">
            {promoImages.map((promo) => (
              <div
                key={promo.id}
                className="promo-grid-item"
                style={{
                  borderRadius: "12px",
                  overflow: "hidden",
                  boxShadow: "0 2px 10px rgba(0,0,0,0.08)",
                  cursor: "pointer",
                  transition: "transform 0.3s ease",
                }}
                onMouseEnter={(e) =>
                  (e.currentTarget.style.transform = "translateY(-5px)")
                }
                onMouseLeave={(e) =>
                  (e.currentTarget.style.transform = "translateY(0)")
                }
                onClick={() =>
                  promo.link && (window.location.href = promo.link)
                }
              >
                <img
                  src={promo.image}
                  alt={promo.title}
                  className="promo-grid-image"
                  onError={(e) => {
                    e.target.src = "/images/placeholder.png";
                  }}
                />
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ========== ANA SAYFA ÜRÜN BLOKLARI SECTION (Poster + Ürünler) ========== */}
      {/* 
        ÖNEMLİ: "İlgini Çekebilecek Ürünler" dahil TÜM ürün blokları
        Admin Panel > Ana Sayfa Blokları'ndan yönetilir.
        
        Admin panelden yeni blok eklemek için:
        1. Admin Panel > Ana Sayfa Blokları
        2. "Yeni Blok Ekle"
        3. Başlık: "İlgini Çekebilecek Ürünler" / "Süt Ürünleri" / "İndirimli Ürünler" vb.
        4. Blok Tipi: Manuel Seçim (ID ile ürün ekleme) / Kategori Bazlı / İndirimli vb.
        5. Poster görseli yükle
        6. Ürünleri seç (ID veya isim ile arama)
      */}
      {/* 
        Bu bölüm Admin Panel > Ana Sayfa Blokları'ndan kontrol edilir.
        Her blok: Sol tarafta poster, sağ tarafta ürün grid'i
        Blok Tipleri: manual, category, discounted, newest, bestseller
      */}
      {blocksLoading ? (
        <section className="py-4">
          <div className="container-fluid px-4">
            <div
              style={{
                textAlign: "center",
                padding: "40px 20px",
                color: "#6b7280",
              }}
            >
              <i
                className="fas fa-spinner fa-spin me-2"
                style={{ fontSize: "24px" }}
              ></i>
              <p className="mt-2">Ürün blokları yükleniyor...</p>
            </div>
          </div>
        </section>
      ) : homeBlocks.length > 0 ? (
        <section
          className="product-blocks-section py-4"
          style={{ background: "#f8f9fa" }}
        >
          <div className="container-fluid px-4">
            {console.log(
              "🏠 [HomePage] Rendering homeBlocks:",
              homeBlocks.length,
              homeBlocks,
            )}
            {homeBlocks.map((block) => (
              <ProductBlockSection key={block.id || block.Id} block={block} />
            ))}
          </div>
        </section>
      ) : null}

      {/* ========== ŞEF ÖNERİSİ SECTION ========== */}
      {recipeBanners.length > 0 && (
        <section
          className="recipe-section py-4"
          style={{
            background: "#f8f9fa",
          }}
        >
          <div className="container-fluid px-4" style={{ maxWidth: "1100px" }}>
            <div className="mb-3">
              <h3
                className="fw-bold mb-0"
                style={{
                  fontSize: "1.5rem",
                  color: "#333",
                  display: "flex",
                  alignItems: "center",
                  gap: "10px",
                }}
              >
                Şef Önerisi
                <span
                  className="badge"
                  style={{
                    backgroundColor: "#10b981",
                    color: "white",
                    fontSize: "0.6rem",
                    fontWeight: "600",
                    padding: "3px 8px",
                    borderRadius: "10px",
                  }}
                >
                  YENİ
                </span>
              </h3>
            </div>

            <div
              className="recipe-grid"
              style={{
                display: "flex",
                flexWrap: "wrap",
                justifyContent: "center",
                gap: "16px",
                maxWidth: "1100px",
                margin: "0 auto",
              }}
            >
              {recipeBanners.slice(0, 4).map((recipe) => (
                <div
                  key={recipe.id}
                  className="recipe-card"
                  style={{
                    borderRadius: "16px",
                    overflow: "hidden",
                    boxShadow: "0 4px 12px rgba(0,0,0,0.08)",
                    transition: "transform 0.3s ease, box-shadow 0.3s ease",
                    cursor: "pointer",
                    backgroundColor: "#fff",
                    position: "relative",
                    flex: "1 1 280px",
                    maxWidth: "500px",
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.transform = "translateY(-4px)";
                    e.currentTarget.style.boxShadow =
                      "0 8px 24px rgba(0,0,0,0.12)";
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.transform = "translateY(0)";
                    e.currentTarget.style.boxShadow =
                      "0 4px 12px rgba(0,0,0,0.08)";
                  }}
                  onClick={() =>
                    recipe.link && (window.location.href = recipe.link)
                  }
                >
                  <img
                    src={recipe.image}
                    alt={recipe.title || "Şef Önerisi"}
                    style={{
                      width: "100%",
                      height: "140px",
                      objectFit: "cover",
                      display: "block",
                    }}
                    onError={(e) => {
                      e.target.src = "/images/placeholder.png";
                    }}
                  />
                  {/* Başlık Overlay */}
                  {recipe.title && (
                    <div
                      style={{
                        position: "absolute",
                        bottom: 0,
                        left: 0,
                        right: 0,
                        background:
                          "linear-gradient(transparent, rgba(0,0,0,0.7))",
                        padding: "20px 10px 10px",
                        color: "white",
                      }}
                    >
                      <p
                        style={{
                          margin: 0,
                          fontSize: "0.75rem",
                          fontWeight: "600",
                          textShadow: "1px 1px 2px rgba(0,0,0,0.5)",
                          lineHeight: 1.2,
                        }}
                      >
                        {recipe.title}
                      </p>
                    </div>
                  )}
                </div>
              ))}
            </div>

            {/* Mobil responsive */}
            <style>{`
              @media (max-width: 576px) {
                .recipe-grid {
                  gap: 10px !important;
                  max-width: 100% !important;
                }
                .recipe-card img {
                  height: 120px !important;
                }
                .recipe-card {
                  border-radius: 10px !important;
                }
              }
            `}</style>
          </div>
        </section>
      )}

      {/* Features Section - 4 Özellik Kartları */}
      <section className="features-section py-5">
        <div className="container-fluid px-4">
          <div className="row g-4 justify-content-center">
            <div className="col-6 col-lg-3">
              <div className="feature-card text-center p-4 bg-white rounded-4 shadow-sm h-100">
                <div className="feature-icon mb-3">
                  <i
                    className="fas fa-credit-card"
                    style={{ fontSize: "2.5rem", color: "#ff9500" }}
                  ></i>
                </div>
                <h5 className="feature-title fw-bold mb-2">
                  Esnek Ödeme İmkanları
                </h5>
                <p className="feature-desc text-muted small mb-0">
                  Kapıda veya Kredi Kartı ile Online Ödeme Yapın
                </p>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="feature-card text-center p-4 bg-white rounded-4 shadow-sm h-100">
                <div className="feature-icon mb-3">
                  <i
                    className="fas fa-truck"
                    style={{ fontSize: "2.5rem", color: "#ff9500" }}
                  ></i>
                </div>
                <h5 className="feature-title fw-bold mb-2">
                  İstediğin Saatte Teslimat
                </h5>
                <p className="feature-desc text-muted small mb-0">
                  Haftanın 7 günü İstediğin Saatte Teslim Edelim
                </p>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="feature-card text-center p-4 bg-white rounded-4 shadow-sm h-100">
                <div className="feature-icon mb-3">
                  <i
                    className="fas fa-box-open"
                    style={{ fontSize: "2.5rem", color: "#ff9500" }}
                  ></i>
                </div>
                <h5 className="feature-title fw-bold mb-2">
                  Özenle Seçilmiş, Paketlenmiş Ürünler
                </h5>
                <p className="feature-desc text-muted small mb-0">
                  Senin İçin Tüm Siparişlerini Özenle Hazırlıyoruz
                </p>
              </div>
            </div>
            <div className="col-6 col-lg-3">
              <div className="feature-card text-center p-4 bg-white rounded-4 shadow-sm h-100">
                <div className="feature-icon mb-3">
                  <i
                    className="fas fa-leaf"
                    style={{ fontSize: "2.5rem", color: "#27ae60" }}
                  ></i>
                </div>
                <h5 className="feature-title fw-bold mb-2">
                  Doğal Ürün Garantisi
                </h5>
                <p className="feature-desc text-muted small mb-0">
                  Tüm Ürünlerimiz %100 Doğal ve Taze Garantilidir
                </p>
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

              <form
                onSubmit={handleNewsletterSubmit}
                className="newsletter-form newsletter-form-animated"
              >
                <div className="input-group newsletter-input-group">
                  <input
                    type="email"
                    className="form-control form-control-lg newsletter-input"
                    placeholder="E-mail adresiniz"
                    value={newsletterEmail}
                    onChange={(event) => {
                      setNewsletterEmail(event.target.value);
                      if (newsletterStatus === "error") {
                        setNewsletterStatus("idle");
                        setNewsletterMessage("");
                      }
                    }}
                    disabled={
                      newsletterStatus === "loading" || isNewsletterSubscribed
                    }
                    aria-label="E-posta adresi"
                    aria-invalid={newsletterStatus === "error"}
                  />
                  <button
                    type="submit"
                    className="btn btn-light btn-lg newsletter-btn"
                    disabled={
                      newsletterStatus === "loading" || isNewsletterSubscribed
                    }
                  >
                    {newsletterStatus === "loading" ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2"></span>
                        Gönderiliyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-paper-plane me-2"></i>
                        Katıl
                      </>
                    )}
                  </button>
                </div>

                {newsletterMessage && (
                  <div
                    className={`mt-3 alert ${
                      newsletterStatus === "success"
                        ? "alert-success"
                        : "alert-danger"
                    } py-2`}
                    role={newsletterStatus === "success" ? "status" : "alert"}
                  >
                    {newsletterMessage}
                  </div>
                )}

                {isNewsletterSubscribed && !newsletterMessage && (
                  <div className="mt-3 alert alert-info py-2" role="status">
                    Zaten bültene kayıtlısınız.
                  </div>
                )}

                <div className="newsletter-note mt-3 newsletter-note-animated">
                  <i className="fas fa-heart me-2 beating-heart"></i>
                  Spam göndermiyoruz, sadece değerli içerik
                </div>
              </form>
            </div>
          </div>
        </div>
      </section>

      {/* Modern Footer - Mobilde gizli */}
      <footer
        className="modern-footer d-none d-md-block"
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
                    src="/images/golkoy-logo1.png"
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
                  <a href="/category/meyve-sebze" className="footer-link">
                    Meyve & Sebze
                  </a>
                </li>
                <li>
                  <a href="/category/et-tavuk-balik" className="footer-link">
                    Et & Tavuk & Balık
                  </a>
                </li>
                <li>
                  <a href="/category/sut-urunleri" className="footer-link">
                    Süt Ürünleri
                  </a>
                </li>
                <li>
                  <a href="/category/temel-gida" className="footer-link">
                    Temel Gıda
                  </a>
                </li>
                <li>
                  <a href="/category/icecekler" className="footer-link">
                    İçecekler
                  </a>
                </li>
                <li>
                  <a href="/category/atistirmalik" className="footer-link">
                    Atıştırmalık
                  </a>
                </li>
                <li>
                  <a href="/category/temizlik" className="footer-link">
                    Temizlik
                  </a>
                </li>
                <li>
                  <a href="/favorites" className="footer-link">
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
                  <Link to="/yardim" className="footer-link">
                    Yardım Merkezi
                  </Link>
                </li>
                <li>
                  <Link to="/iletisim" className="footer-link text-warning">
                    İletişim
                  </Link>
                </li>
                <li>
                  <Link to="/siparis-takibi" className="footer-link">
                    Sipariş Takibi
                  </Link>
                </li>
                <li>
                  <Link to="/iade-degisim" className="footer-link">
                    İade & Değişim
                  </Link>
                </li>
                <li>
                  <Link to="/kargo-bilgileri" className="footer-link">
                    Kargo Bilgileri
                  </Link>
                </li>
                <li>
                  <Link to="/odeme-secenekleri" className="footer-link">
                    Ödeme Seçenekleri
                  </Link>
                </li>
                <li>
                  <Link to="/guvenli-alisveris" className="footer-link">
                    Güvenli Alışveriş
                  </Link>
                </li>
                <li>
                  <Link to="/sss" className="footer-link">
                    S.S.S
                  </Link>
                </li>
                <li>
                  <Link to="/geri-bildirim" className="footer-link">
                    Geri Bildirim
                  </Link>
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
                  <Link to="/hakkimizda" className="footer-link">
                    Hakkımızda
                  </Link>
                </li>
                <li>
                  <Link to="/vizyon-misyon" className="footer-link">
                    Vizyon & Misyon
                  </Link>
                </li>
                <li>
                  <Link to="/kariyer" className="footer-link">
                    Kariyer (Yakında)
                  </Link>
                </li>
                <li>
                  <Link to="/basin-kiti" className="footer-link">
                    Basın Kiti
                  </Link>
                </li>
                <li>
                  <Link to="/surdurulebilirlik" className="footer-link">
                    Sürdürülebilirlik
                  </Link>
                </li>
              </ul>

              {/* Social Media */}
              <div className="social-media mt-4">
                <h6 className="footer-title">Sosyal Medya</h6>
                <div className="social-links">
                  <a
                    href="https://www.facebook.com/golkoygurmebodrum"
                    target="_blank"
                    rel="noopener noreferrer"
                    className="social-link"
                  >
                    <i className="fab fa-facebook-f"></i>
                  </a>
                  <a
                    href="https://www.instagram.com/golkoygurmebodrum?igsh=aWJwMHJsbXdjYmt4"
                    target="_blank"
                    rel="noopener noreferrer"
                    className="social-link"
                  >
                    <i className="fab fa-instagram"></i>
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
                  <span>Tüm haklar Gölköy Gurme Markete aittir.</span>
                  <button
                    type="button"
                    className="footer-bottom-link btn btn-link"
                  >
                    Gizlilik Politikası
                  </button>
                  <button
                    type="button"
                    className="footer-bottom-link btn btn-link"
                  >
                    Kullanım Şartları
                  </button>
                  <button
                    type="button"
                    className="footer-bottom-link btn btn-link"
                  >
                    KVKK
                  </button>
                  <button
                    type="button"
                    className="footer-bottom-link btn btn-link"
                  >
                    Çerez Politikası
                  </button>
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

export default AppWithProviders;
