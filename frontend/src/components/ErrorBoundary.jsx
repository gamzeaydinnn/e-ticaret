import React from "react";
import "./ErrorBoundary.css";

/**
 * React Error Boundary - Çalışma zamanı hatalarını yakalar
 *
 * NEDEN GEREKLİ:
 * - React componentlerinde meydana gelen JavaScript hatalarını yakalar
 * - Hata oluştuğunda uygulamanın tamamen çökmesini engeller
 * - Kullanıcıya anlamlı bir hata mesajı gösterir
 * - Production'da hataları loglama servisi göndermek için kullanılabilir
 *
 * KULLANIM:
 * <ErrorBoundary>
 *   <App />
 * </ErrorBoundary>
 *
 * YAKALANAN HATALAR:
 * - Render sırasında oluşan hatalar
 * - Lifecycle metodlarında oluşan hatalar
 * - Constructor hatalarında oluşan hatalar
 *
 * YAKALANMAYAN HATALAR:
 * - Event handler'larda oluşan hatalar (try-catch ile yakalanmalı)
 * - Async kod (setTimeout, Promise) hatalar
 * - Server-side rendering hataları
 * - Error Boundary'nin kendi hataları
 */
class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    };
  }

  /**
   * Hata yakalandığında state güncellenir
   * Bu sayede fallback UI gösterilir
   */
  static getDerivedStateFromError(error) {
    // Bir sonraki render'da fallback UI gösterilmesi için state güncelle
    return { hasError: true };
  }

  /**
   * Hata detaylarını logla
   * Production'da bu bilgileri Sentry, LogRocket gibi servislere gönderebiliriz
   */
  componentDidCatch(error, errorInfo) {
    // Hata detaylarını state'e kaydet
    this.setState({
      error: error,
      errorInfo: errorInfo,
    });

    // ═══════════════════════════════════════════════════════════
    // PRODUCTION LOGGING
    // Hataları merkezi loglama servisine gönder
    // Örnek: Sentry.captureException(error);
    // ═══════════════════════════════════════════════════════════
    if (process.env.NODE_ENV === "production") {
      // Production'da console.error yerine log servisi kullan
      // this.logErrorToService(error, errorInfo);
    } else {
      // Development'ta console'a yaz
      console.error("ErrorBoundary caught an error:", error, errorInfo);
    }
  }

  /**
   * Sayfayı yeniden yükle - kullanıcıya ikinci şans ver
   */
  handleReload = () => {
    window.location.reload();
  };

  /**
   * Ana sayfaya yönlendir
   */
  handleGoHome = () => {
    window.location.href = "/";
  };

  render() {
    if (this.state.hasError) {
      // Hata UI'ı göster
      return (
        <div className="error-boundary-container">
          <div className="error-boundary-content">
            {/* Hata İkonu */}
            <div className="error-boundary-icon">⚠️</div>

            {/* Hata Başlığı */}
            <h1 className="error-boundary-title">Bir Hata Oluştu</h1>

            {/* Hata Açıklaması */}
            <p className="error-boundary-description">
              Üzgünüz, beklenmeyen bir hata oluştu. Teknik ekibimiz bu durumdan
              haberdar edildi ve sorunu en kısa sürede çözecektir.
            </p>

            {/* Aksiyon Butonları */}
            <div className="error-boundary-actions">
              <button
                onClick={this.handleReload}
                className="error-boundary-btn error-boundary-btn-primary"
              >
                Sayfayı Yenile
              </button>
              <button
                onClick={this.handleGoHome}
                className="error-boundary-btn error-boundary-btn-secondary"
              >
                Ana Sayfaya Dön
              </button>
            </div>

            {/* Development'ta hata detaylarını göster */}
            {process.env.NODE_ENV === "development" && this.state.error && (
              <details className="error-boundary-details">
                <summary className="error-boundary-summary">
                  Hata Detayları (Sadece Development)
                </summary>
                <div className="error-boundary-stack">
                  <p className="error-message">
                    <strong>Hata:</strong> {this.state.error.toString()}
                  </p>
                  <pre className="error-stack">
                    {this.state.errorInfo?.componentStack}
                  </pre>
                </div>
              </details>
            )}

            {/* Destek Linki */}
            <p className="error-boundary-support">
              Sorun devam ediyorsa{" "}
              <a href="/iletisim" className="error-boundary-link">
                destek ekibimizle iletişime geçin
              </a>
            </p>
          </div>
        </div>
      );
    }

    // Hata yoksa normal child component'leri render et
    return this.props.children;
  }
}

export default ErrorBoundary;
