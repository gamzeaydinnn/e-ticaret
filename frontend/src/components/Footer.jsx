import { Link } from "react-router-dom";

/**
 * Footer - Site Alt Bilgi Bileşeni
 *
 * Mobilde gizlenir (bottom navigation ile çakışmayı önlemek için)
 * Desktop'ta normal şekilde görünür.
 *
 * Requirements: 4.1, 4.2
 */
export default function Footer() {
  return (
    <footer
      className="modern-footer desktop-only-footer d-none d-md-block"
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
                <a
                  href="https://wa.me/905334783072?text=Merhaba,%20sipariş%20hakkında%20bilgi%20almak%20istiyorum."
                  target="_blank"
                  rel="noopener noreferrer"
                  className="whatsapp-link"
                  style={{
                    textDecoration: "none",
                    color: "inherit",
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                    transition: "transform 0.2s",
                  }}
                  onMouseEnter={(e) =>
                    (e.currentTarget.style.transform = "scale(1.05)")
                  }
                  onMouseLeave={(e) =>
                    (e.currentTarget.style.transform = "scale(1)")
                  }
                >
                  <svg
                    width="24"
                    height="24"
                    viewBox="0 0 24 24"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                    style={{ marginRight: "8px" }}
                  >
                    <path
                      d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z"
                      fill="#25D366"
                    />
                  </svg>
                  <div>
                    <strong>+90 533 478 30 72</strong>
                    <br />
                    <small>Müşteri Hizmetleri</small>
                  </div>
                </a>
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
