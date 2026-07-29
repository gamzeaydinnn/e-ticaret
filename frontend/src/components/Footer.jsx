import { Link } from "react-router-dom";
import { useEffect, useState } from "react";
import { getFooterData } from "../services/siteSettingsService";
import categoryServiceReal, {
  normalizeCategorySlug,
} from "../services/categoryServiceReal";

const dedupeArray = (values = []) =>
  [...new Set((values || []).filter(Boolean))];

const createSlug = (name) => {
  if (!name) return "";
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

const getCategoryPath = (category) => {
  const slug = normalizeCategorySlug(category.slug || createSlug(category.name));
  return slug ? `/category/${slug}` : "/";
};

/**
 * Footer - Site Alt Bilgi Bileşeni
 *
 * Desktop'ta tam footer görünür.
 * Mobilde MobileFooterStrip ile Favorilerim vb. linkler sunulur.
 * Tüm bilgiler backend'den dinamik olarak çekilir - Gölköy Gurme'ye özel.
 *
 * Requirements: 4.1, 4.2
 */
export default function Footer() {
  const [footerData, setFooterData] = useState(null);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchFooterData = async () => {
      try {
        const data = await getFooterData();
        setFooterData(data);
      } catch (error) {
        console.error("Footer verisi yüklenemedi:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchFooterData();
  }, []);

  useEffect(() => {
    const loadCategories = async () => {
      try {
        const cats = await categoryServiceReal.getActive();
        const rootCategories = (cats || [])
          .filter((cat) => !cat.parentId)
          .sort(
            (a, b) =>
              (Number(a.sortOrder) || 0) - (Number(b.sortOrder) || 0) ||
              String(a.name || "").localeCompare(String(b.name || ""), "tr"),
          );
        setCategories(rootCategories);
      } catch (error) {
        console.error("Footer kategorileri yüklenemedi:", error);
        setCategories([]);
      }
    };

    loadCategories();
    const unsubscribe = categoryServiceReal.subscribe(loadCategories);
    return () => unsubscribe && unsubscribe();
  }, []);

  // Yükleniyor durumu
  if (loading) {
    return null;
  }

  // Veri destructure
  const company = footerData?.company || {};
  const contact = footerData?.contact || {};
  const socialMedia = footerData?.socialMedia || {};
  const footer = footerData?.footer || {};

  // WhatsApp link oluştur
  const whatsAppUrl = contact.whatsAppNumber
    ? `https://wa.me/${contact.whatsAppNumber}?text=${encodeURIComponent(contact.whatsAppMessage || "")}`
    : "#";

  const footerLogoSrc = "/images/golkoy-header-logo.png";

  const phoneDisplay = contact.phoneDisplay || contact.phone;
  const hasPhone = Boolean(contact.whatsAppNumber || phoneDisplay);
  const securityFeatures = dedupeArray(footer.securityFeatures);

  return (
    <footer
      className="modern-footer desktop-only-footer d-none d-lg-block"
      style={{
        background: "linear-gradient(135deg, #2c3e50 0%, #34495e 100%)",
        color: "white",
      }}
    >
      <div className="footer-shell py-5">
        <div className="row footer-main-row g-4">
          {/* Company Info */}
          <div className="col-lg-3 col-md-6 footer-col footer-col-brand">
            <div className="footer-brand">
              <div className="footer-brand-logos mb-3">
                <Link to="/" className="footer-brand-link" aria-label="Ana sayfa">
                  <img
                    src={footerLogoSrc}
                    alt={company.name || "Gölköy Gurme"}
                    className="footer-brand-logo-image"
                  />
                </Link>
              </div>
              {company.description && (
                <p className="footer-description">{company.description}</p>
              )}
              <div className="footer-trust-row">
                <span className="footer-trust-chip">Taze Ürün</span>
                <span className="footer-trust-chip">Soğuk Zincir</span>
                <span className="footer-trust-chip">Kapıda Teslimat</span>
              </div>
              {footer.showSSLBadge && securityFeatures.length > 0 && (
                <div className="footer-features">
                  {securityFeatures.map((feature, index) => (
                    <div className="footer-feature" key={feature}>
                      <i
                        className={`fas ${
                          index === 0
                            ? "fa-shield-alt text-success"
                            : index === 1
                              ? "fa-credit-card text-info"
                              : "fa-heart text-danger"
                        } me-2`}
                      ></i>
                      {feature}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Categories */}
          <div className="col-lg-2 col-md-6 footer-col">
            <h6 className="footer-title">Kategoriler</h6>
            <ul className="footer-links">
              {categories.length > 0 ? (
                categories.map((category) => (
                  <li key={category.id || category.slug || category.name}>
                    <Link
                      to={getCategoryPath(category)}
                      className="footer-link"
                    >
                      {category.name}
                    </Link>
                  </li>
                ))
              ) : (
                <li>
                  <span className="footer-link text-white-50">
                    Kategoriler yükleniyor…
                  </span>
                </li>
              )}
              <li>
                <Link to="/favorites" className="footer-link">
                  Favorilerim
                </Link>
              </li>
            </ul>
          </div>

          {/* Customer Services */}
          <div className="col-lg-2 col-md-6 footer-col">
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
                  Teslimat Bilgileri
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
          <div className="col-lg-3 col-md-6 footer-col footer-col-contact">
            <h6 className="footer-title">İletişim</h6>
            <div className="contact-info">
              {hasPhone && (
                <div className="contact-item">
                  <a
                    href={
                      contact.whatsAppNumber
                        ? whatsAppUrl
                        : `tel:${String(phoneDisplay).replace(/\s/g, "")}`
                    }
                    target="_blank"
                    rel="noopener noreferrer"
                    className="footer-contact-link"
                  >
                    <span className="contact-item-icon" aria-hidden="true">
                      <svg
                        width="20"
                        height="20"
                        viewBox="0 0 24 24"
                        fill="none"
                        xmlns="http://www.w3.org/2000/svg"
                      >
                        <path
                          d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z"
                          fill="#25D366"
                        />
                      </svg>
                    </span>
                    <span className="contact-text-block">
                      <strong>{phoneDisplay}</strong>
                      <small>{contact.phoneLabel || "Müşteri Hizmetleri"}</small>
                    </span>
                  </a>
                </div>
              )}
              {contact.email && (
                <div className="contact-item">
                  <span className="contact-item-icon" aria-hidden="true">
                    <i className="fas fa-envelope text-warning"></i>
                  </span>
                  <div className="contact-text-block">
                    <strong className="footer-contact-email">{contact.email}</strong>
                    <small>{contact.emailLabel}</small>
                  </div>
                </div>
              )}
              {contact.address && (
                <div className="contact-item">
                  <span className="contact-item-icon" aria-hidden="true">
                    <i className="fas fa-map-marker-alt text-warning"></i>
                  </span>
                  <div className="contact-text-block">
                    <strong>{contact.address}</strong>
                    <small>{contact.addressLabel}</small>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Eski "Kurumsal" alanı: Markalar kaldirildi, yerine Kategoriler */}
          <div className="col-lg-2 col-md-6 footer-col footer-col-corporate">
            <ul className="footer-links">
              <li>
                <Link to="/kategoriler" className="footer-link">
                  Kategoriler
                </Link>
              </li>
              <li>
                <Link to="/hakkimizda" className="footer-link">
                  Hakkımızda
                </Link>
              </li>
              <li>
                <Link to="/vizyon-misyon" className="footer-link">
                  Vizyonumuz
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
                {socialMedia.facebook && (
                  <a
                    href={socialMedia.facebook}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="social-link"
                  >
                    <i className="fab fa-facebook-f"></i>
                  </a>
                )}
                {socialMedia.instagram && (
                  <a
                    href={socialMedia.instagram}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="social-link"
                  >
                    <i className="fab fa-instagram"></i>
                  </a>
                )}
                {socialMedia.twitter && (
                  <a
                    href={socialMedia.twitter}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="social-link"
                  >
                    <i className="fab fa-twitter"></i>
                  </a>
                )}
                {socialMedia.youTube && (
                  <a
                    href={socialMedia.youTube}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="social-link"
                  >
                    <i className="fab fa-youtube"></i>
                  </a>
                )}
                {socialMedia.linkedIn && (
                  <a
                    href={socialMedia.linkedIn}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="social-link"
                  >
                    <i className="fab fa-linkedin-in"></i>
                  </a>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Footer Bottom */}
      <div className="footer-bottom">
        <div className="footer-shell">
          <div className="row align-items-center">
            <div className="col-md-8">
              <div className="footer-bottom-links">
                <span>{company.copyrightText}</span>
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
              {footer.showPaymentMethods && footer.paymentMethods && (
                <div className="payment-methods">
                  <span className="payment-text">Kabul Edilen Kartlar:</span>
                  <div className="payment-cards">
                    {footer.paymentMethods.map((method, index) => (
                      <span className="payment-card" key={index}>
                        {method}
                      </span>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </footer>
  );
}
