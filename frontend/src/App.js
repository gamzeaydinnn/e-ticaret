import "bootstrap/dist/css/bootstrap.min.css";
import "./App.css";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import AdminApp from "./pages/Admin/Admin";

// Normal site bileşeni
const HomePage = () => (
  <div>
    {/* Main Header */}
    <header className="main-header bg-white shadow-sm py-3">
      <div className="container-fluid px-4">
        <div className="row align-items-center">
          <div className="col-md-3">
            <div className="d-flex align-items-center">
              <div className="logo-container me-2">
                <div className="logo-circle bg-danger text-white">TC</div>
              </div>
              <div>
                <h4 className="mb-0 fw-bold">TicaretiCenter</h4>
                <small className="text-muted">Türkiye'nin E-Ticaret Lideri</small>
              </div>
            </div>
          </div>
          <div className="col-md-6">
            <div className="search-container">
              <div className="input-group">
                <input
                  type="text"
                  className="form-control form-control-lg"
                  placeholder="20 milyondan fazla ürün arasında ara..."
                />
                <button className="btn btn-danger btn-lg">
                  <i className="fas fa-search"></i>
                </button>
              </div>
            </div>
          </div>
          <div className="col-md-3">
            <div className="header-actions justify-content-end">
              <button className="header-action">
                <i className="fas fa-user"></i>
                <span>Hesabım</span>
              </button>
              <button className="header-action position-relative">
                <i className="fas fa-shopping-cart"></i>
                <span>Sepetim</span>
                <span className="badge cart-badge position-absolute top-0 start-0">
                  3
                </span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </header>

    {/* Navigation Categories */}
    <nav className="categories-nav bg-light py-2">
      <div className="container-fluid px-4">
        <div className="row">
          <div className="col-12">
            <div className="d-flex justify-content-between flex-wrap">
              {["Süpermarket","Elektronik","Moda","Ev & Yaşam","Kozmetik","Spor","Kitap","Oyuncak","Otomotiv","İndirimli Ürünler"].map((cat, i) => (
                <a key={i} className={`nav-category ${cat === "İndirimli Ürünler" ? "text-danger" : ""}`} href="#">{cat}</a>
              ))}
            </div>
          </div>
        </div>
      </div>
    </nav>

    <main className="container py-5">
      <h1 className="text-center">Hoşgeldiniz! Ana Sayfa İçeriği Buraya Gelecek</h1>
    </main>

    <footer className="main-footer mt-5">
      <div className="container text-center py-4">
        <span>© 2024 TicaretiCenter. Tüm hakları saklıdır.</span>
      </div>
    </footer>
  </div>
);

function App() {
  return (
    <Router>
      <Routes>
        {/* Admin panel rotası */}
        <Route path="/admin/*" element={<AdminApp />} />
        
        {/* Normal site */}
        <Route path="/" element={<HomePage />} />
      </Routes>
    </Router>
  );
}

export default App;
