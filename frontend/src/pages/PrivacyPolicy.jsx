import React from "react";
import { Helmet } from "react-helmet-async";

export default function PrivacyPolicy() {
  return (
    <div className="container py-5">
      <Helmet>
        <title>Gizlilik Politikası — Gölköy Gurme</title>
        <meta
          name="description"
          content="Gölköy Gurme gizlilik politikası. Kişisel verilerinizin nasıl toplandığı, işlendiği ve korunduğu hakkında bilgi."
        />
      </Helmet>

      <h1 className="h3 fw-bold mb-4" style={{ color: "#2d3748" }}>
        <i className="fas fa-shield-alt me-2" style={{ color: "#f57c00" }}></i>
        Gizlilik Politikası
      </h1>

      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <p className="text-muted mb-3">
            <strong>Son Güncelleme:</strong> 01.01.2026
          </p>
          <p className="text-muted">
            Gölköy Gurme Market olarak kişisel verilerinizin güvenliğine büyük
            önem vermekteyiz. Bu Gizlilik Politikası, web sitemizi ve
            hizmetlerimizi kullanırken kişisel verilerinizin nasıl toplandığını,
            kullanıldığını, saklandığını ve korunduğunu açıklamaktadır.
          </p>
        </div>
      </div>

      {/* 1. Toplanan Veriler */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            1. Toplanan Kişisel Veriler
          </h5>
          <p className="text-muted mb-3">
            Hizmetlerimizi sunabilmek amacıyla aşağıdaki kişisel verileri
            toplayabiliriz:
          </p>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              <strong>Kimlik Bilgileri:</strong> Ad, soyad
            </li>
            <li className="mb-2">
              <strong>İletişim Bilgileri:</strong> E-posta adresi, telefon
              numarası, teslimat adresi
            </li>
            <li className="mb-2">
              <strong>Hesap Bilgileri:</strong> Kullanıcı adı, şifre
              (şifrelenmiş olarak saklanır)
            </li>
            <li className="mb-2">
              <strong>Sipariş Bilgileri:</strong> Sipariş geçmişi, ödeme
              bilgileri (kart numaraları tarafımızca saklanmaz)
            </li>
            <li className="mb-2">
              <strong>Teknik Veriler:</strong> IP adresi, tarayıcı türü, cihaz
              bilgileri, çerez verileri
            </li>
          </ul>
        </div>
      </div>

      {/* 2. Verilerin Kullanım Amaçları */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            2. Verilerin Kullanım Amaçları
          </h5>
          <p className="text-muted mb-3">
            Toplanan kişisel veriler aşağıdaki amaçlarla kullanılmaktadır:
          </p>
          <ul className="text-muted ps-3">
            <li className="mb-2">Siparişlerinizin işlenmesi ve teslimatı</li>
            <li className="mb-2">
              Müşteri hesabınızın oluşturulması ve yönetimi
            </li>
            <li className="mb-2">Müşteri hizmetleri desteğinin sağlanması</li>
            <li className="mb-2">
              Yasal yükümlülüklerin yerine getirilmesi (fatura, vergi vb.)
            </li>
            <li className="mb-2">
              Hizmet kalitemizin artırılması ve kullanıcı deneyiminin
              iyileştirilmesi
            </li>
            <li className="mb-2">
              Kampanya, indirim ve yenilikler hakkında bilgilendirme (izniniz
              doğrultusunda)
            </li>
          </ul>
        </div>
      </div>

      {/* 3. Verilerin Paylaşımı */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            3. Verilerin Üçüncü Taraflarla Paylaşımı
          </h5>
          <p className="text-muted mb-3">
            Kişisel verileriniz yalnızca aşağıdaki durumlarda üçüncü taraflarla
            paylaşılabilir:
          </p>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              <strong>Teslimat Hizmetleri:</strong> Siparişinizin teslimatı için
              kurye ekibimiz ile adres ve iletişim bilgileri paylaşılır.
            </li>
            <li className="mb-2">
              <strong>Ödeme Altyapısı:</strong> Ödeme işlemleriniz güvenli ödeme
              altyapı sağlayıcıları üzerinden gerçekleştirilir. Kart
              bilgileriniz tarafımızca saklanmaz.
            </li>
            <li className="mb-2">
              <strong>Yasal Zorunluluklar:</strong> Yetkili kamu kurum ve
              kuruluşlarının talep etmesi halinde yasal yükümlülükler kapsamında
              paylaşılabilir.
            </li>
          </ul>
        </div>
      </div>

      {/* 4. Veri Güvenliği */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            4. Veri Güvenliği
          </h5>
          <p className="text-muted">
            Kişisel verilerinizin güvenliğini sağlamak için SSL şifreleme,
            güvenli sunucu altyapısı ve erişim kontrolü gibi teknik ve idari
            tedbirler uygulamaktayız. Şifreleriniz tek yönlü şifreleme (hash)
            yöntemiyle saklanmakta olup hiçbir çalışanımız tarafından
            görüntülenemez.
          </p>
        </div>
      </div>

      {/* 5. Çerezler */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            5. Çerezler (Cookies)
          </h5>
          <p className="text-muted">
            Web sitemiz, kullanıcı deneyimini iyileştirmek ve hizmetlerimizi
            geliştirmek amacıyla çerez teknolojisini kullanmaktadır. Çerez
            kullanımına ilişkin detaylı bilgi için{" "}
            <a href="/cerez-politikasi" style={{ color: "#f57c00" }}>
              Çerez Politikası
            </a>{" "}
            sayfamızı inceleyebilirsiniz.
          </p>
        </div>
      </div>

      {/* 6. Haklar */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            6. Haklarınız
          </h5>
          <p className="text-muted mb-3">
            6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında aşağıdaki
            haklara sahipsiniz:
          </p>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Kişisel verilerinizin işlenip işlenmediğini öğrenme
            </li>
            <li className="mb-2">
              Kişisel verileriniz işlenmişse buna ilişkin bilgi talep etme
            </li>
            <li className="mb-2">
              Kişisel verilerin işlenme amacını ve amacına uygun kullanılıp
              kullanılmadığını öğrenme
            </li>
            <li className="mb-2">
              Eksik veya yanlış işlenmiş kişisel verilerin düzeltilmesini isteme
            </li>
            <li className="mb-2">
              Kişisel verilerin silinmesini veya yok edilmesini isteme
            </li>
            <li className="mb-2">
              İşlenen verilerin münhasıran otomatik sistemler vasıtasıyla analiz
              edilmesi suretiyle kişinin kendisi aleyhine bir sonucun ortaya
              çıkmasına itiraz etme
            </li>
          </ul>
        </div>
      </div>

      {/* 7. İletişim */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            7. İletişim
          </h5>
          <p className="text-muted mb-2">
            Gizlilik politikamız ile ilgili sorularınız veya talepleriniz için
            bize aşağıdaki kanallardan ulaşabilirsiniz:
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
