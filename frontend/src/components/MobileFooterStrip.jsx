import { Link } from "react-router-dom";

/**
 * Mobil footer şeridi — tam footer desktop'ta kalır; mobilde keşfedilebilir linkler.
 * Alt navigasyonun üstünde, sayfa sonunda görünür.
 */
export default function MobileFooterStrip() {
  return (
    <footer className="mobile-footer-strip d-lg-none" aria-label="Mobil site linkleri">
      <div className="mobile-footer-strip-inner">
        <nav className="mobile-footer-nav" aria-label="Hızlı linkler">
          <Link to="/favorites" className="mobile-footer-link">
            <i className="fas fa-heart me-1" />
            Favorilerim
          </Link>
          <Link to="/campaigns" className="mobile-footer-link">
            <i className="fas fa-tags me-1" />
            Kampanyalar
          </Link>
          <Link to="/siparis-takibi" className="mobile-footer-link">
            <i className="fas fa-truck me-1" />
            Sipariş Takibi
          </Link>
          <Link to="/iletisim" className="mobile-footer-link">
            <i className="fas fa-envelope me-1" />
            İletişim
          </Link>
        </nav>
        <div className="mobile-footer-legal">
          <Link to="/gizlilik-politikasi">Gizlilik</Link>
          <span aria-hidden="true">·</span>
          <Link to="/kullanim-sartlari">Kullanım</Link>
          <span aria-hidden="true">·</span>
          <Link to="/kvkk">KVKK</Link>
        </div>
      </div>
    </footer>
  );
}
