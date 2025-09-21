import React from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import "./App.css";

function App() {
  return (
    <div className="App">
      {/* Main Header */}
      <header className="main-header bg-white shadow-sm py-3">
        <div className="container-fluid px-4">
          <div className="row align-items-center">
            {/* Logo */}
            <div className="col-md-3">
              <div className="d-flex align-items-center">
                <div className="logo-container me-2">
                  <div className="logo-circle bg-danger text-white">TC</div>
                </div>
                <div>
                  <h4 className="mb-0 fw-bold">TicaretiCenter</h4>
                  <small className="text-muted">
                    Türkiye'nin E-Ticaret Lideri
                  </small>
                </div>
              </div>
            </div>

            {/* Search Bar */}
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

            {/* Right Actions */}
            <div className="col-md-3">
              <div className="header-actions justify-content-end">
                <a href="#" className="header-action">
                  <i className="fas fa-user"></i>
                  <span>Hesabım</span>
                </a>
                <a href="#" className="header-action position-relative">
                  <i className="fas fa-shopping-cart"></i>
                  <span>Sepetim</span>
                  <span className="badge cart-badge position-absolute top-0 start-0">
                    3
                  </span>
                </a>
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
              <div className="d-flex justify-content-between">
                <a href="#" className="nav-category">
                  Süpermarket
                </a>
                <a href="#" className="nav-category">
                  Elektronik
                </a>
                <a href="#" className="nav-category">
                  Moda
                </a>
                <a href="#" className="nav-category">
                  Ev & Yaşam
                </a>
                <a href="#" className="nav-category">
                  Kozmetik
                </a>
                <a href="#" className="nav-category">
                  Spor
                </a>
                <a href="#" className="nav-category">
                  Kitap
                </a>
                <a href="#" className="nav-category">
                  Oyuncak
                </a>
                <a href="#" className="nav-category">
                  Otomotiv
                </a>
                <a href="#" className="nav-category text-danger">
                  İndirimli Ürünler
                </a>
              </div>
            </div>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="hero-section py-5">
        <div className="container-fluid px-4">
          <div className="row align-items-center">
            <div className="col-md-8">
              <div className="hero-content">
                <div className="hero-badge mb-3">
                  <span className="badge bg-warning text-dark fw-bold px-3 py-2">
                    <i className="fas fa-star me-2"></i>
                    2024'ün Yeni E-Ticaret Platformu
                  </span>
                </div>
                <h1 className="display-4 fw-bold mb-4">
                  Güvenilir <br />
                  <span className="text-danger">Alışverişin Adresi</span>
                </h1>
                <p className="lead text-muted mb-4">
                  Kaliteli ürünler, güvenli ödeme sistemi ve müşteri odaklı
                  hizmet anlayışı. Modern e-ticaret deneyimini keşfedin.
                </p>
                {/* Yeni Site Özellikleri */}
                <div className="new-features mb-4">
                  <div className="row">
                    <div className="col-md-4">
                      <div className="feature-highlight">
                        <i className="fas fa-certificate text-warning mb-2"></i>
                        <h6>Kaliteli Ürünler</h6>
                        <small className="text-muted">
                          Özenle seçilmiş markalar
                        </small>
                      </div>
                    </div>
                    <div className="col-md-4">
                      <div className="feature-highlight">
                        <i className="fas fa-lock text-warning mb-2"></i>
                        <h6>Güvenli Alışveriş</h6>
                        <small className="text-muted">
                          256-bit SSL koruması
                        </small>
                      </div>
                    </div>
                    <div className="col-md-4">
                      <div className="feature-highlight">
                        <i className="fas fa-shipping-fast text-warning mb-2"></i>
                        <h6>Hızlı Teslimat</h6>
                        <small className="text-muted">
                          Türkiye geneli kargo
                        </small>
                      </div>
                    </div>
                  </div>
                </div>
                {/* CTA Butonları */}
                <div className="cta-buttons mb-4">
                  <button className="btn btn-warning btn-lg me-3 px-4 py-3 shadow">
                    <i className="fas fa-shopping-cart me-2"></i>
                    Alışverişe Başla
                  </button>
                  <button className="btn btn-outline-warning btn-lg px-4 py-3 shadow">
                    <i className="fas fa-percentage me-2"></i>
                    Kampanyalar
                  </button>
                </div>{" "}
                {/* Statistics - Yeni Site için daha mütevazi */}
                <div className="row stats-row">
                  <div className="col-md-4">
                    <div className="stat-item">
                      <h3 className="text-danger fw-bold">2024</h3>
                      <p className="text-muted">Kuruluş Yılı</p>
                    </div>
                  </div>
                  <div className="col-md-4">
                    <div className="stat-item">
                      <h3 className="text-danger fw-bold">100+</h3>
                      <p className="text-muted">Marka</p>
                    </div>
                  </div>
                  <div className="col-md-4">
                    <div className="stat-item">
                      <h3 className="text-danger fw-bold">7/24</h3>
                      <p className="text-muted">Destek</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="col-md-4">
              <div className="hero-image">
                <div className="warehouse-container">
                  <div className="delivery-badge">
                    <i className="fas fa-clock text-warning"></i>
                    <strong>24/7</strong>
                    <span>Hızlı Teslimat</span>
                  </div>
                  <div className="warehouse-visual">
                    <div className="shopping-bags-scene">
                      <div className="bags-grid-3d">
                        <div className="bag-row front-row">
                          <div className="shopping-bag-3d bag-1">
                            <div className="bag-handle"></div>
                            <div className="bag-body"></div>
                            <div className="bag-shadow"></div>
                          </div>
                          <div className="shopping-bag-3d bag-2">
                            <div className="bag-handle"></div>
                            <div className="bag-body"></div>
                            <div className="bag-shadow"></div>
                          </div>
                          <div className="shopping-bag-3d bag-3">
                            <div className="bag-handle"></div>
                            <div className="bag-body"></div>
                            <div className="bag-shadow"></div>
                          </div>
                        </div>
                        <div className="bag-row middle-row">
                          <div className="shopping-bag-3d bag-4">
                            <div className="bag-handle"></div>
                            <div className="bag-body"></div>
                            <div className="bag-shadow"></div>
                          </div>
                          <div className="shopping-bag-3d bag-5">
                            <div className="bag-handle"></div>
                            <div className="bag-body"></div>
                            <div className="bag-shadow"></div>
                          </div>
                          <div className="shopping-bag-3d bag-6">
                            <div className="bag-handle"></div>
                            <div className="bag-body"></div>
                            <div className="bag-shadow"></div>
                          </div>
                        </div>
                        <div className="bag-row back-row">
                          <div className="shopping-bag-3d bag-7">
                            <div className="bag-handle"></div>
                            <div className="bag-body"></div>
                            <div className="bag-shadow"></div>
                          </div>
                          <div className="shopping-bag-3d bag-8">
                            <div className="bag-handle"></div>
                            <div className="bag-body"></div>
                            <div className="bag-shadow"></div>
                          </div>
                          <div className="shopping-bag-3d bag-9">
                            <div className="bag-handle"></div>
                            <div className="bag-body"></div>
                            <div className="bag-shadow"></div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section - Kompakt Tasarım */}
      <section className="features-section py-4 bg-light">
        <div className="container-fluid px-4">
          <div className="row">
            <div className="col-md-3">
              <div className="feature-card text-center">
                <div className="feature-icon">
                  <i className="fas fa-truck text-warning"></i>
                </div>
                <h6>Ücretsiz Kargo</h6>
                <small className="text-muted">
                  Türkiye geneli ücretsiz teslimat
                </small>
              </div>
            </div>
            <div className="col-md-3">
              <div className="feature-card text-center">
                <div className="feature-icon">
                  <i className="fas fa-bolt text-warning"></i>
                </div>
                <h6>Hızlı Teslimat</h6>
                <small className="text-muted">Aynı gün teslimat imkanı</small>
              </div>
            </div>
            <div className="col-md-3">
              <div className="feature-card text-center">
                <div className="feature-icon">
                  <i className="fas fa-shield-alt text-warning"></i>
                </div>
                <h6>Güvenli Ödeme</h6>
                <small className="text-muted">256-bit SSL güvenlik</small>
              </div>
            </div>
            <div className="col-md-3">
              <div className="feature-card text-center">
                <div className="feature-icon">
                  <i className="fas fa-undo-alt text-warning"></i>
                </div>
                <h6>Kolay İade</h6>
                <small className="text-muted">14 gün ücretsiz iade</small>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Ürün Kategorilerimiz Section */}
      <section className="categories-section py-5">
        <div className="container-fluid px-4">
          <div className="text-center mb-5">
            <h2 className="section-title mb-3">Ürün Kategorilerimiz</h2>
            <p className="section-subtitle text-muted">
              Her geçen gün genişleyen ürün yelpazemizle size hizmet veriyoruz
            </p>
          </div>

          <div className="row">
            {/* İlk satır */}
            <div className="col-lg-3 col-md-6 mb-4">
              <div className="category-card">
                <div className="category-icon-wrapper">
                  <div className="category-icon blue">
                    <i className="fas fa-mobile-alt"></i>
                  </div>
                  <span className="category-badge yakinda">Yakında</span>
                </div>
                <h5 className="category-title">Elektronik</h5>
                <p className="category-subtitle">
                  Telefon, Bilgisayar, Aksesuar
                </p>
              </div>
            </div>

            <div className="col-lg-3 col-md-6 mb-4">
              <div className="category-card">
                <div className="category-icon-wrapper">
                  <div className="category-icon pink">
                    <i className="fas fa-tshirt"></i>
                  </div>
                  <span className="category-badge mevcut">Mevcut</span>
                </div>
                <h5 className="category-title">Moda</h5>
                <p className="category-subtitle">Kadın, Erkek Giyim</p>
              </div>
            </div>

            <div className="col-lg-3 col-md-6 mb-4">
              <div className="category-card">
                <div className="category-icon-wrapper">
                  <div className="category-icon green">
                    <i className="fas fa-home"></i>
                  </div>
                  <span className="category-badge mevcut">Mevcut</span>
                </div>
                <h5 className="category-title">Ev & Yaşam</h5>
                <p className="category-subtitle">Ev Dekorasyonu</p>
              </div>
            </div>

            <div className="col-lg-3 col-md-6 mb-4">
              <div className="category-card">
                <div className="category-icon-wrapper">
                  <div className="category-icon orange">
                    <i className="fas fa-gamepad"></i>
                  </div>
                  <span className="category-badge hazirlaniyor">
                    Hazırlanıyor
                  </span>
                </div>
                <h5 className="category-title">Oyun & Hobi</h5>
                <p className="category-subtitle">Oyuncak, Oyun</p>
                <small className="coming-soon">Çok yakında...</small>
              </div>
            </div>

            {/* İkinci satır */}
            <div className="col-lg-3 col-md-6 mb-4">
              <div className="category-card">
                <div className="category-icon-wrapper">
                  <div className="category-icon gray">
                    <i className="fas fa-car"></i>
                  </div>
                  <span className="category-badge planlaniyor">
                    Planlanıyor
                  </span>
                </div>
                <h5 className="category-title">Otomotiv</h5>
                <p className="category-subtitle">Araç Bakım & Aksesuar</p>
              </div>
            </div>

            <div className="col-lg-3 col-md-6 mb-4">
              <div className="category-card">
                <div className="category-icon-wrapper">
                  <div className="category-icon pink-alt">
                    <i className="fas fa-baby"></i>
                  </div>
                  <span className="category-badge mevcut">Mevcut</span>
                </div>
                <h5 className="category-title">Anne & Bebek</h5>
                <p className="category-subtitle">Bebek Bakım & Oyuncak</p>
              </div>
            </div>

            <div className="col-lg-3 col-md-6 mb-4">
              <div className="category-card">
                <div className="category-icon-wrapper">
                  <div className="category-icon orange-alt">
                    <i className="fas fa-dumbbell"></i>
                  </div>
                  <span className="category-badge yakinda">Yakında</span>
                </div>
                <h5 className="category-title">Spor & Outdoor</h5>
                <p className="category-subtitle">Fitness & Spor Malzemeleri</p>
              </div>
            </div>

            <div className="col-lg-3 col-md-6 mb-4">
              <div className="category-card">
                <div className="category-icon-wrapper">
                  <div className="category-icon purple">
                    <i className="fas fa-book"></i>
                  </div>
                  <span className="category-badge mevcut">Mevcut</span>
                </div>
                <h5 className="category-title">Kitap & Kırtasiye</h5>
                <p className="category-subtitle">Roman, Akademik, Kırtasiye</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Özenle Seçilen Ürünler Section */}
      <section className="featured-products-section py-5">
        <div className="container-fluid px-4">
          {/* Section Header */}
          <div className="text-center mb-5">
            <div className="section-badge mb-3">
              <i className="fas fa-bolt me-2"></i>
              Yeni Ürünlerimiz
            </div>
            <h2 className="featured-title mb-3">Özenle Seçilen Ürünler</h2>
            <p className="featured-subtitle">
              Kalite standartlarımıza uygun, müşteri memnuniyeti odaklı
              ürünlerimizi keşfedin
            </p>
          </div>

          {/* Products Grid */}
          <div className="row">
            {/* Ürün 1 - Bluetooth Kulaklık */}
            <div className="col-lg-3 col-md-6 mb-4">
              <div className="product-card">
                <div className="product-image">
                  <div className="product-badges">
                    <span className="badge-new">Yeni</span>
                    <span className="badge-shipping">Hızlı Kargo</span>
                  </div>
                  <div className="product-discount">%25 İndirim</div>
                  <div className="product-img-placeholder yellow">
                    <i className="fas fa-headphones"></i>
                  </div>
                </div>
                <div className="product-info">
                  <div className="product-brand">TechSound</div>
                  <h5 className="product-title">Bluetooth Kulaklık</h5>
                  <div className="product-rating">
                    <div className="stars">
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="far fa-star"></i>
                    </div>
                    <span className="rating-text">4.5 (12 değerlendirme)</span>
                  </div>
                  <div className="product-price">
                    <span className="current-price">₺299</span>
                    <span className="old-price">₺399</span>
                  </div>
                  <div className="product-features">
                    <span className="feature-badge shipping">
                      <i className="fas fa-truck"></i> Hızlı Kargo
                    </span>
                    <span className="feature-badge secure">
                      <i className="fas fa-shield-alt"></i> Güvenli
                    </span>
                  </div>
                  <button className="btn-add-cart">
                    <i className="fas fa-shopping-cart me-2"></i>
                    Sepete Ekle
                  </button>
                </div>
              </div>
            </div>

            {/* Ürün 2 - Kadın Triko Kazak */}
            <div className="col-lg-3 col-md-6 mb-4">
              <div className="product-card">
                <div className="product-image">
                  <div className="product-badges">
                    <span className="badge-popular">Popüler</span>
                    <span className="badge-shipping">Hızlı Kargo</span>
                  </div>
                  <div className="product-discount">%27 İndirim</div>
                  <div className="product-img-placeholder beige">
                    <i className="fas fa-tshirt"></i>
                  </div>
                </div>
                <div className="product-info">
                  <div className="product-brand">StyleWear</div>
                  <h5 className="product-title">Kadın Triko Kazak</h5>
                  <div className="product-rating">
                    <div className="stars">
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                    </div>
                    <span className="rating-text">4.7 (8 değerlendirme)</span>
                  </div>
                  <div className="product-price">
                    <span className="current-price">₺159</span>
                    <span className="old-price">₺219</span>
                  </div>
                  <div className="product-features">
                    <span className="feature-badge shipping">
                      <i className="fas fa-truck"></i> Hızlı Kargo
                    </span>
                    <span className="feature-badge secure">
                      <i className="fas fa-shield-alt"></i> Güvenli
                    </span>
                  </div>
                  <button className="btn-add-cart">
                    <i className="fas fa-shopping-cart me-2"></i>
                    Sepete Ekle
                  </button>
                </div>
              </div>
            </div>

            {/* Ürün 3 - Ev Dekoru Vazo */}
            <div className="col-lg-3 col-md-6 mb-4">
              <div className="product-card">
                <div className="product-image">
                  <div className="product-badges">
                    <span className="badge-sale">İndirimde</span>
                  </div>
                  <div className="product-discount">%25 İndirim</div>
                  <div className="product-img-placeholder blue">
                    <i className="fas fa-vase"></i>
                  </div>
                </div>
                <div className="product-info">
                  <div className="product-brand">HomeArt</div>
                  <h5 className="product-title">Ev Dekoru Vazo</h5>
                  <div className="product-rating">
                    <div className="stars">
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="far fa-star"></i>
                    </div>
                    <span className="rating-text">4.3 (5 değerlendirme)</span>
                  </div>
                  <div className="product-price">
                    <span className="current-price">₺89</span>
                    <span className="old-price">₺119</span>
                  </div>
                  <div className="product-features">
                    <span className="feature-badge secure">
                      <i className="fas fa-shield-alt"></i> Güvenli
                    </span>
                  </div>
                  <button className="btn-add-cart">
                    <i className="fas fa-shopping-cart me-2"></i>
                    Sepete Ekle
                  </button>
                </div>
              </div>
            </div>

            {/* Ürün 4 - Spor Ayakkabı */}
            <div className="col-lg-3 col-md-6 mb-4">
              <div className="product-card">
                <div className="product-image">
                  <div className="product-badges">
                    <span className="badge-trend">Trend</span>
                    <span className="badge-shipping">Hızlı Kargo</span>
                  </div>
                  <div className="product-discount">%25 İndirim</div>
                  <div className="product-img-placeholder brown">
                    <i className="fas fa-shoe-prints"></i>
                  </div>
                </div>
                <div className="product-info">
                  <div className="product-brand">ActiveStep</div>
                  <h5 className="product-title">Spor Ayakkabı</h5>
                  <div className="product-rating">
                    <div className="stars">
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                      <i className="fas fa-star"></i>
                    </div>
                    <span className="rating-text">4.6 (15 değerlendirme)</span>
                  </div>
                  <div className="product-price">
                    <span className="current-price">₺449</span>
                    <span className="old-price">₺599</span>
                  </div>
                  <div className="product-features">
                    <span className="feature-badge shipping">
                      <i className="fas fa-truck"></i> Hızlı Kargo
                    </span>
                    <span className="feature-badge secure">
                      <i className="fas fa-shield-alt"></i> Güvenli
                    </span>
                  </div>
                  <button className="btn-add-cart">
                    <i className="fas fa-shopping-cart me-2"></i>
                    Sepete Ekle
                  </button>
                </div>
              </div>
            </div>
          </div>

          {/* Daha Fazla Ürün Keşfet Section */}
          <div className="discover-more-section text-center py-5">
            <h3 className="discover-title mb-3">Daha Fazla Ürün Keşfet</h3>
            <p className="discover-subtitle mb-4">
              Sürekli genişleyen ürün yelpazemizle kaliteli alışverişin keyfini
              çıkarın
            </p>
            <div className="discover-buttons">
              <button className="btn btn-primary btn-lg me-3">
                <i className="fas fa-arrow-right me-2"></i>
                Tüm Ürünler
              </button>
              <button className="btn btn-outline-primary btn-lg">
                <i className="fas fa-th-large me-2"></i>
                Kategoriler
              </button>
            </div>
          </div>
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
    </div>
  );
}

export default App;
