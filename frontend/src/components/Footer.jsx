import { Link } from "react-router-dom";

export default function Footer() {
  return (
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
                <span>
                  Copyright ©2022 - 2024 Tüm Hakları İdol Media'ya Aittir.
                </span>
                <Link to="/gizlilik-politikasi" className="footer-bottom-link">
                  Gizlilik Politikası
                </Link>
                <Link to="/kullanim-sartlari" className="footer-bottom-link">
                  Kullanım Şartları
                </Link>
                <Link to="/kvkk" className="footer-bottom-link">
                  KVKK
                </Link>
                <Link to="/cerez-politikasi" className="footer-bottom-link">
                  Çerez Politikası
                </Link>
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
  );
}
