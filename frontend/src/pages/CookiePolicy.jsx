import React from "react";
import { Helmet } from "react-helmet-async";

export default function CookiePolicy() {
  return (
    <div className="container py-5">
      <Helmet>
        <title>Çerez Politikası — Gölköy Gurme</title>
        <meta
          name="description"
          content="Gölköy Gurme çerez (cookie) politikası. Web sitemizde kullanılan çerezler hakkında bilgi."
        />
      </Helmet>

      <h1 className="h3 fw-bold mb-4" style={{ color: "#2d3748" }}>
        <i className="fas fa-cookie-bite me-2" style={{ color: "#f57c00" }}></i>
        Çerez Politikası
      </h1>

      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <p className="text-muted mb-3">
            <strong>Son Güncelleme:</strong> 01.01.2026
          </p>
          <p className="text-muted">
            Gölköy Gurme Market web sitesi, kullanıcı deneyimini iyileştirmek,
            site performansını analiz etmek ve hizmetlerimizi geliştirmek
            amacıyla çerez (cookie) teknolojisini kullanmaktadır. Bu politika,
            hangi çerezlerin kullanıldığı ve bunları nasıl yönetebileceğiniz
            hakkında bilgi vermektedir.
          </p>
        </div>
      </div>

      {/* 1. Çerez Nedir? */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            1. Çerez Nedir?
          </h5>
          <p className="text-muted">
            Çerezler (cookies), web sitesini ziyaret ettiğinizde tarayıcınız
            aracılığıyla cihazınıza (bilgisayar, tablet veya telefon)
            yerleştirilen küçük metin dosyalarıdır. Çerezler, web sitesinin
            düzgün çalışmasını, güvenliğini, kullanıcı deneyiminin
            iyileştirilmesini ve ziyaretçi davranışlarının analiz edilmesini
            sağlar.
          </p>
        </div>
      </div>

      {/* 2. Kullanılan Çerez Türleri */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            2. Kullanılan Çerez Türleri
          </h5>

          <div className="table-responsive">
            <table className="table table-bordered">
              <thead style={{ backgroundColor: "#f8f9fa" }}>
                <tr>
                  <th style={{ color: "#2d3748" }}>Çerez Türü</th>
                  <th style={{ color: "#2d3748" }}>Amaç</th>
                  <th style={{ color: "#2d3748" }}>Süre</th>
                </tr>
              </thead>
              <tbody className="text-muted">
                <tr>
                  <td>
                    <strong>Zorunlu Çerezler</strong>
                  </td>
                  <td>
                    Web sitesinin temel işlevlerinin çalışması için gereklidir.
                    Oturum yönetimi, sepet bilgileri ve güvenlik amaçlı
                    kullanılır. Bu çerezler olmadan site düzgün çalışmaz.
                  </td>
                  <td>Oturum süresi</td>
                </tr>
                <tr>
                  <td>
                    <strong>Kimlik Doğrulama Çerezleri</strong>
                  </td>
                  <td>
                    Oturum açtığınızda kimliğinizi doğrulamak ve güvenli erişim
                    sağlamak için kullanılır. JWT token bilgisi httpOnly çerez
                    olarak saklanır (JavaScript ile erişilemez).
                  </td>
                  <td>Oturum süresi / 7 gün</td>
                </tr>
                <tr>
                  <td>
                    <strong>Tercih Çerezleri</strong>
                  </td>
                  <td>
                    Dil, bölge ve görüntüleme tercihlerinizi hatırlamak için
                    kullanılır. Çerez onayı tercihiniz de bu kapsamda saklanır.
                  </td>
                  <td>1 yıl</td>
                </tr>
                <tr>
                  <td>
                    <strong>Analitik Çerezler</strong>
                  </td>
                  <td>
                    Ziyaretçi sayısı, sayfa görüntülenme ve site kullanım
                    istatistiklerini anonim olarak toplar. Site performansının
                    iyileştirilmesinde kullanılır.
                  </td>
                  <td>2 yıl</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* 3. Çerez Yönetimi */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            3. Çerezleri Nasıl Yönetebilirsiniz?
          </h5>
          <p className="text-muted mb-3">
            Tarayıcınızın ayarlarından çerezleri yönetebilir, engelleyebilir
            veya silebilirsiniz. Ancak zorunlu çerezlerin engellenmesi durumunda
            web sitesinin bazı bölümleri düzgün çalışmayabilir.
          </p>

          <h6 className="fw-bold mb-2" style={{ color: "#2d3748" }}>
            Popüler Tarayıcılarda Çerez Ayarları:
          </h6>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              <strong>Google Chrome:</strong> Ayarlar → Gizlilik ve Güvenlik →
              Çerezler ve diğer site verileri
            </li>
            <li className="mb-2">
              <strong>Mozilla Firefox:</strong> Seçenekler → Gizlilik ve
              Güvenlik → Çerezler ve Site Verileri
            </li>
            <li className="mb-2">
              <strong>Safari:</strong> Tercihler → Gizlilik → Çerezler ve web
              sitesi verileri
            </li>
            <li className="mb-2">
              <strong>Microsoft Edge:</strong> Ayarlar → Çerezler ve site
              izinleri → Çerezleri ve site verilerini yönet
            </li>
          </ul>
        </div>
      </div>

      {/* 4. Üçüncü Taraf Çerezleri */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            4. Üçüncü Taraf Çerezleri
          </h5>
          <p className="text-muted">
            Web sitemizde ödeme işlemleri sırasında ödeme altyapı sağlayıcıları
            tarafından çerezler kullanılabilir. Bu çerezler, ödeme güvenliği ve
            dolandırıcılığı önleme amacıyla kullanılmaktadır. Üçüncü taraf
            çerezleri, ilgili hizmet sağlayıcıların gizlilik politikalarına
            tabidir.
          </p>
        </div>
      </div>

      {/* 5. Çerez Onayı */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            5. Çerez Onayı
          </h5>
          <p className="text-muted">
            Web sitemizi ilk ziyaretinizde çerez kullanımına ilişkin bir
            bildirim gösterilmektedir. "Kabul Et" seçeneğini tıklayarak tüm
            çerezlerin kullanımına onay vermiş olursunuz. Zorunlu çerezler
            dışındaki çerezleri kabul etmeme hakkınız saklıdır. Tercihlerinizi
            istediğiniz zaman tarayıcı ayarlarından değiştirebilirsiniz.
          </p>
        </div>
      </div>

      {/* 6. Hukuki Dayanak */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            6. Hukuki Dayanak
          </h5>
          <p className="text-muted">
            Çerez kullanımımız, 6698 sayılı Kişisel Verilerin Korunması Kanunu
            (KVKK), 5809 sayılı Elektronik Haberleşme Kanunu ve ilgili ikincil
            mevzuat hükümlerine uygun olarak gerçekleştirilmektedir.
          </p>
        </div>
      </div>

      {/* 7. İletişim */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            7. İletişim
          </h5>
          <p className="text-muted mb-2">
            Çerez politikamız ile ilgili sorularınız için:
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
