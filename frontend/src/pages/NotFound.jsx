import React from "react";
import { Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import "./NotFound.css";

/**
 * 404 - Sayfa Bulunamadı
 *
 * KULLANIM ALANLARI:
 * - Tanımlanmamış route'lara giden kullanıcılar için
 * - Silinmiş/mevcut olmayan ürün/kategori sayfaları için
 * - SEO: 404 hatası Google'a doğru şekilde raporlanır
 *
 * UX İYİLEŞTİRMELERİ:
 * - Ana sayfaya dönüş butonu
 * - Popüler kategorilere hızlı erişim
 * - Arama önerisi
 */
const NotFound = () => {
  return (
    <>
      <Helmet>
        <title>404 - Sayfa Bulunamadı | Gölköy Gurme</title>
        <meta name="robots" content="noindex, nofollow" />
      </Helmet>

      <div className="not-found-container">
        <div className="not-found-content">
          {/* 404 İllüstrasyonu */}
          <div className="not-found-illustration">
            <span className="not-found-number">404</span>
            <div className="not-found-icon">🛒</div>
          </div>

          {/* Hata Mesajı */}
          <h1 className="not-found-title">Aradığınız Sayfa Bulunamadı</h1>
          <p className="not-found-description">
            Üzgünüz, aradığınız sayfa kaldırılmış, adı değiştirilmiş veya geçici
            olarak kullanılamıyor olabilir.
          </p>

          {/* Aksiyon Butonları */}
          <div className="not-found-actions">
            <Link to="/" className="not-found-btn not-found-btn-primary">
              Ana Sayfaya Dön
            </Link>
            <Link
              to="/search"
              className="not-found-btn not-found-btn-secondary"
            >
              Ürün Ara
            </Link>
          </div>

          {/* Popüler Kategoriler - Yönlendirme */}
          <div className="not-found-suggestions">
            <p className="suggestions-title">
              Popüler Kategorilerimize Göz Atın:
            </p>
            <div className="suggestions-links">
              <Link to="/category/meyve-sebze" className="suggestion-link">
                🍎 Meyve & Sebze
              </Link>
              <Link to="/category/sut-urunleri" className="suggestion-link">
                🥛 Süt Ürünleri
              </Link>
              <Link to="/category/et-tavuk" className="suggestion-link">
                🍗 Et & Tavuk
              </Link>
              <Link to="/campaigns" className="suggestion-link">
                🎉 Kampanyalar
              </Link>
            </div>
          </div>

          {/* İletişim Bilgisi */}
          <p className="not-found-support">
            Yardıma mı ihtiyacınız var?{" "}
            <Link to="/iletisim" className="support-link">
              Bizimle iletişime geçin
            </Link>
          </p>
        </div>
      </div>
    </>
  );
};

export default NotFound;
