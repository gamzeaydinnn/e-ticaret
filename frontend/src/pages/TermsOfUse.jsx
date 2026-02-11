import React from "react";
import { Helmet } from "react-helmet-async";

export default function TermsOfUse() {
  return (
    <div className="container py-5">
      <Helmet>
        <title>Kullanım Şartları — Gölköy Gurme</title>
        <meta
          name="description"
          content="Gölköy Gurme web sitesi kullanım şartları ve koşulları."
        />
      </Helmet>

      <h1 className="h3 fw-bold mb-4" style={{ color: "#2d3748" }}>
        <i
          className="fas fa-file-contract me-2"
          style={{ color: "#f57c00" }}
        ></i>
        Kullanım Şartları
      </h1>

      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <p className="text-muted mb-3">
            <strong>Son Güncelleme:</strong> 01.01.2026
          </p>
          <p className="text-muted">
            Bu kullanım şartları, Gölköy Gurme Market web sitesini
            (golkoygurme.com.tr) kullanan tüm ziyaretçi ve müşteriler için
            geçerlidir. Siteyi kullanarak aşağıdaki şartları kabul etmiş
            sayılırsınız.
          </p>
        </div>
      </div>

      {/* 1. Genel Hükümler */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            1. Genel Hükümler
          </h5>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Bu web sitesi Gölköy Gurme Market tarafından işletilmektedir.
            </li>
            <li className="mb-2">
              Siteye erişim ve kullanım, Türkiye Cumhuriyeti kanunlarına
              tabidir.
            </li>
            <li className="mb-2">
              Gölköy Gurme, bu kullanım şartlarını önceden haber vermeksizin
              güncelleme hakkını saklı tutar.
            </li>
            <li className="mb-2">
              Siteyi kullanmaya devam etmeniz, güncellenmiş şartları kabul
              ettiğiniz anlamına gelir.
            </li>
          </ul>
        </div>
      </div>

      {/* 2. Üyelik ve Hesap */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            2. Üyelik ve Hesap Güvenliği
          </h5>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Üyelik sırasında verilen bilgilerin doğru ve eksiksiz olması
              gerekmektedir.
            </li>
            <li className="mb-2">
              Hesap bilgilerinizin (e-posta, şifre) güvenliğinden siz
              sorumlusunuz.
            </li>
            <li className="mb-2">
              Hesabınızın yetkisiz kullanıldığını fark ederseniz derhal bize
              bildirmeniz gerekmektedir.
            </li>
            <li className="mb-2">
              Gölköy Gurme, kurallara aykırı davranan hesapları askıya alma veya
              kapatma hakkını saklı tutar.
            </li>
          </ul>
        </div>
      </div>

      {/* 3. Sipariş ve Ödeme */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            3. Sipariş ve Ödeme
          </h5>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Sitede listelenen ürün fiyatları KDV dahildir ve Türk Lirası
              cinsindendir.
            </li>
            <li className="mb-2">
              Gölköy Gurme, fiyat değişikliği yapma hakkını saklı tutar.
              Siparişiniz onaylandıktan sonra fiyat değişikliği uygulanmaz.
            </li>
            <li className="mb-2">
              Ödemeler güvenli ödeme altyapısı üzerinden (kredi kartı / banka
              kartı) gerçekleştirilir.
            </li>
            <li className="mb-2">
              Sipariş onayı, ödemenin başarıyla tamamlanması ve stok durumuna
              bağlıdır.
            </li>
            <li className="mb-2">
              Stok yetersizliği durumunda sipariş iptal edilebilir ve ödemeniz
              iade edilir.
            </li>
          </ul>
        </div>
      </div>

      {/* 4. Teslimat */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            4. Teslimat Koşulları
          </h5>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Teslimatlar Gölköy Gurme'nin kendi kurye ekibi tarafından
              gerçekleştirilmektedir.
            </li>
            <li className="mb-2">
              Teslimat adresi doğru ve eksiksiz girilmelidir. Hatalı adres
              nedeniyle yaşanacak gecikmelerden Gölköy Gurme sorumlu tutulamaz.
            </li>
            <li className="mb-2">
              Teslimat süreleri hizmet bölgesine göre değişiklik gösterebilir.
            </li>
            <li className="mb-2">
              Teslim alınmayan siparişler için yeniden teslimat ücreti talep
              edilebilir.
            </li>
          </ul>
        </div>
      </div>

      {/* 5. İade ve Değişim */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            5. İade ve Değişim
          </h5>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Taze gıda ürünlerinde yalnızca bozuk veya hasarlı ürünler için
              iade kabul edilir. Teslimat sırasında fotoğraflı bildirim
              yapılmalıdır.
            </li>
            <li className="mb-2">
              Paketli ürünlerde ambalajı açılmamış ürünler 7 gün içinde iade
              edilebilir.
            </li>
            <li className="mb-2">
              Detaylı iade ve değişim koşulları için{" "}
              <a href="/iade-degisim" style={{ color: "#f57c00" }}>
                İade ve Değişim
              </a>{" "}
              sayfamızı inceleyebilirsiniz.
            </li>
          </ul>
        </div>
      </div>

      {/* 6. Fikri Mülkiyet */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            6. Fikri Mülkiyet Hakları
          </h5>
          <p className="text-muted">
            Bu web sitesindeki tüm içerikler (tasarım, logo, görseller,
            metinler, yazılım vb.) Gölköy Gurme Market'e aittir ve 5846 sayılı
            Fikir ve Sanat Eserleri Kanunu ile korunmaktadır. İzinsiz kopyalama,
            dağıtma veya kullanma yasaktır.
          </p>
        </div>
      </div>

      {/* 7. Sorumluluk Sınırı */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            7. Sorumluluk Sınırlaması
          </h5>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Gölköy Gurme, teknik arızalar, internet bağlantı sorunları veya
              mücbir sebepler nedeniyle oluşabilecek aksaklıklardan sorumlu
              değildir.
            </li>
            <li className="mb-2">
              Ürün görselleri temsilidir; gerçek ürün ile küçük farklılıklar
              olabilir.
            </li>
          </ul>
        </div>
      </div>

      {/* 8. Uyuşmazlık Çözümü */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            8. Uyuşmazlık Çözümü
          </h5>
          <p className="text-muted">
            Bu kullanım şartlarından doğabilecek uyuşmazlıklarda Muğla
            Mahkemeleri ve İcra Daireleri yetkilidir. Tüketici haklarına ilişkin
            şikâyetler için Tüketici Hakem Heyeti'ne başvurulabilir.
          </p>
        </div>
      </div>

      {/* İletişim */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            9. İletişim
          </h5>
          <p className="text-muted mb-2">
            Kullanım şartları ile ilgili sorularınız için:
          </p>
          <ul className="text-muted ps-3 mb-0">
            <li className="mb-2">
              <strong>E-posta:</strong>{" "}
              <a
                href="mailto:golturkbuku@golkoygurme.com.tr"
                style={{ color: "#f57c00" }}
              >
                golturkbuku@golkoygurme.com.tr
              </a>
            </li>
            <li className="mb-2">
              <strong>Adres:</strong> Gölköy Mah. Bodrum / Muğla
            </li>
          </ul>
        </div>
      </div>
    </div>
  );
}
