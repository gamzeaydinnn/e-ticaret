import React, { useState } from "react";

const AccountPage = () => {
  const [user, setUser] = useState(null);
  const [isLogin, setIsLogin] = useState(true);
  const [showVerification, setShowVerification] = useState(false);
  const [verificationCode, setVerificationCode] = useState("");
  const [generatedCode, setGeneratedCode] = useState("");
  const [countdown, setCountdown] = useState(0);
  const [formData, setFormData] = useState({
    email: "",
    password: "",
    name: "",
    phone: "",
  });

  const handleSubmit = (e) => {
    e.preventDefault();
    if (isLogin) {
      // Giriş simülasyonu
      setUser({
        name:
          formData.email === "test@test.com"
            ? "Test Kullanıcı"
            : "Yeni Kullanıcı",
        email: formData.email,
        phone: "+90 555 123 45 67",
      });
    } else {
      // Kayıt için telefon doğrulama kodu gönder
      sendVerificationCode();
    }
  };

  const sendVerificationCode = () => {
    // 6 haneli rastgele kod oluştur
    const code = Math.floor(100000 + Math.random() * 900000).toString();
    setGeneratedCode(code);
    setShowVerification(true);
    setCountdown(60); // 60 saniye geri sayım

    // Konsola kodu yazdır (gerçek uygulamada SMS gönderilir)
    console.log(`Doğrulama kodu: ${code} - Telefon: ${formData.phone}`);

    // Alert'i daha sonra göster (DOM güncellemesi için)
    setTimeout(() => {
      alert(
        `🔐 DOĞRULAMA KODU\n\n${code}\n\n📱 Bu kod normalde ${formData.phone} numarasına SMS ile gönderilir.\n\n⚠️ Demo amaçlı burada gösteriliyor.`
      );
    }, 500);

    // Geri sayım başlat
    const timer = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          clearInterval(timer);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  };

  const handleVerificationSubmit = (e) => {
    e.preventDefault();
    if (verificationCode === generatedCode) {
      // Doğrulama başarılı, hesap oluştur
      setUser({
        name: formData.name,
        email: formData.email,
        phone: formData.phone,
      });
      setFormData({ email: "", password: "", name: "", phone: "" });
      setShowVerification(false);
      setVerificationCode("");
      setGeneratedCode("");
      alert("Hesabınız başarıyla oluşturuldu!");
    } else {
      alert("Doğrulama kodu hatalı! Lütfen tekrar deneyin.");
    }
  };

  const resendCode = () => {
    if (countdown === 0) {
      sendVerificationCode();
    }
  };

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleVerificationChange = (e) => {
    setVerificationCode(e.target.value);
  };

  const handleLogout = () => {
    setUser(null);
    setIsLogin(true);
    setShowVerification(false);
    setVerificationCode("");
    setGeneratedCode("");
    setCountdown(0);
  };

  if (user) {
    return (
      <div
        style={{
          minHeight: "100vh",
          background:
            "linear-gradient(135deg, #fff3e0 0%, #ffe0b2 50%, #ffcc80 100%)",
          paddingTop: "2rem",
          paddingBottom: "2rem",
        }}
      >
        <div className="container">
          <div className="row">
            <div className="col-md-8 mx-auto">
              <div
                className="card shadow-lg border-0"
                style={{ borderRadius: "20px" }}
              >
                <div
                  className="card-header text-white d-flex justify-content-between align-items-center border-0"
                  style={{
                    background:
                      "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                    borderTopLeftRadius: "20px",
                    borderTopRightRadius: "20px",
                    padding: "1.5rem 2rem",
                  }}
                >
                  <div>
                    <h3 className="mb-1 fw-bold">
                      <i className="fas fa-user-circle me-3"></i>Hesabım
                    </h3>
                    <p className="mb-0 opacity-75">Hoş geldiniz, {user.name}</p>
                  </div>
                  <button
                    onClick={handleLogout}
                    className="btn btn-outline-light btn-lg border-2 fw-bold"
                    style={{ borderRadius: "50px" }}
                  >
                    <i className="fas fa-sign-out-alt me-2"></i>Çıkış
                  </button>
                </div>
                <div className="card-body" style={{ padding: "2rem" }}>
                  <div className="row">
                    <div className="col-md-6 mb-4">
                      <h5 className="text-warning fw-bold mb-4">
                        <i className="fas fa-id-card me-2"></i>Profil Bilgileri
                      </h5>
                      <div
                        className="p-4 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          border: "1px solid #ffe0b2",
                        }}
                      >
                        <div className="mb-3 d-flex align-items-center">
                          <i className="fas fa-user text-warning me-3"></i>
                          <div>
                            <small className="text-muted">Ad Soyad</small>
                            <div className="fw-bold">{user.name}</div>
                          </div>
                        </div>
                        <div className="mb-3 d-flex align-items-center">
                          <i className="fas fa-envelope text-warning me-3"></i>
                          <div>
                            <small className="text-muted">E-posta</small>
                            <div className="fw-bold">{user.email}</div>
                          </div>
                        </div>
                        <div className="mb-0 d-flex align-items-center">
                          <i className="fas fa-phone text-warning me-3"></i>
                          <div>
                            <small className="text-muted">Telefon</small>
                            <div className="fw-bold">{user.phone}</div>
                          </div>
                        </div>
                      </div>
                    </div>
                    <div className="col-md-6">
                      <h5 className="text-warning fw-bold mb-4">
                        <i className="fas fa-bolt me-2"></i>Hızlı İşlemler
                      </h5>
                      <div className="d-grid gap-3">
                        <button
                          className="btn btn-lg text-warning fw-bold border-2 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderColor: "#ffcc80",
                            borderRadius: "15px",
                            padding: "1rem",
                          }}
                        >
                          <i className="fas fa-box me-3"></i>Siparişlerim
                        </button>
                        <button
                          className="btn btn-lg text-warning fw-bold border-2 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderColor: "#ffcc80",
                            borderRadius: "15px",
                            padding: "1rem",
                          }}
                        >
                          <i className="fas fa-map-marker-alt me-3"></i>
                          Adreslerim
                        </button>
                        <button
                          className="btn btn-lg text-warning fw-bold border-2 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderColor: "#ffcc80",
                            borderRadius: "15px",
                            padding: "1rem",
                          }}
                        >
                          <i className="fas fa-heart me-3"></i>Favorilerim
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div
      style={{
        minHeight: "100vh",
        background:
          "linear-gradient(135deg, #fff3e0 0%, #ffe0b2 50%, #ffcc80 100%)",
        paddingTop: "2rem",
        paddingBottom: "2rem",
      }}
    >
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-md-5">
            <div
              className="card shadow-lg border-0"
              style={{ borderRadius: "20px" }}
            >
              <div
                className="card-header text-white text-center border-0"
                style={{
                  background:
                    "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                  borderTopLeftRadius: "20px",
                  borderTopRightRadius: "20px",
                  padding: "1.5rem",
                }}
              >
                <div className="mb-2">
                  <i className="fas fa-user-circle fa-3x"></i>
                </div>
                <h3 className="mb-0 fw-bold">
                  {isLogin ? "Hoş Geldiniz" : "Aramıza Katılın"}
                </h3>
                <p className="mb-0 opacity-75">
                  {isLogin ? "Hesabınıza giriş yapın" : "Yeni hesap oluşturun"}
                </p>
              </div>
              <div className="card-body" style={{ padding: "2rem" }}>
                <div className="text-center mb-4">
                  <div
                    className="btn-group w-100 shadow-sm"
                    role="group"
                    style={{ borderRadius: "50px", overflow: "hidden" }}
                  >
                    <button
                      className={`btn border-0 py-3 fw-bold ${
                        isLogin ? "text-white" : "text-warning bg-light"
                      }`}
                      style={{
                        background: isLogin
                          ? "linear-gradient(45deg, #ff6f00, #ff8f00)"
                          : "transparent",
                        transition: "all 0.3s ease",
                      }}
                      onClick={() => setIsLogin(true)}
                    >
                      <i className="fas fa-sign-in-alt me-2"></i>Giriş Yap
                    </button>
                    <button
                      className={`btn border-0 py-3 fw-bold ${
                        !isLogin ? "text-white" : "text-warning bg-light"
                      }`}
                      style={{
                        background: !isLogin
                          ? "linear-gradient(45deg, #ff6f00, #ff8f00)"
                          : "transparent",
                        transition: "all 0.3s ease",
                      }}
                      onClick={() => setIsLogin(false)}
                    >
                      <i className="fas fa-user-plus me-2"></i>Hesap Oluştur
                    </button>
                  </div>
                </div>

                {!showVerification && (
                  <form onSubmit={handleSubmit}>
                    {!isLogin && (
                      <div className="mb-4">
                        <label className="form-label fw-bold text-warning">
                          <i className="fas fa-user me-2"></i>Ad Soyad
                        </label>
                        <input
                          type="text"
                          name="name"
                          className="form-control form-control-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          value={formData.name}
                          onChange={handleChange}
                          placeholder="Adınız ve soyadınız"
                          required
                        />
                      </div>
                    )}

                    <div className="mb-4">
                      <label className="form-label fw-bold text-warning">
                        <i className="fas fa-envelope me-2"></i>E-posta
                      </label>
                      <input
                        type="email"
                        name="email"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.email}
                        onChange={handleChange}
                        placeholder="ornek@email.com"
                        required
                      />
                    </div>

                    <div className="mb-4">
                      <label className="form-label fw-bold text-warning">
                        <i className="fas fa-lock me-2"></i>Şifre
                      </label>
                      <input
                        type="password"
                        name="password"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.password}
                        onChange={handleChange}
                        placeholder="Şifreniz"
                        required
                      />
                    </div>

                    {!isLogin && (
                      <div className="mb-4">
                        <label className="form-label fw-bold text-warning">
                          <i className="fas fa-phone me-2"></i>Telefon
                        </label>
                        <input
                          type="tel"
                          name="phone"
                          className="form-control form-control-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          value={formData.phone}
                          onChange={handleChange}
                          placeholder="+90 555 123 45 67"
                          required
                        />
                      </div>
                    )}

                    <button
                      type="submit"
                      className="btn btn-lg w-100 text-white fw-bold shadow-lg border-0"
                      style={{
                        background:
                          "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                        borderRadius: "50px",
                        padding: "1rem",
                        transition: "transform 0.2s ease",
                      }}
                      onMouseOver={(e) =>
                        (e.target.style.transform = "translateY(-2px)")
                      }
                      onMouseOut={(e) =>
                        (e.target.style.transform = "translateY(0)")
                      }
                    >
                      <i
                        className={`fas ${
                          isLogin ? "fa-sign-in-alt" : "fa-user-plus"
                        } me-2`}
                      ></i>
                      {isLogin
                        ? "Giriş Yap"
                        : showVerification
                        ? "Doğrulama Kodunu Girin"
                        : "Doğrulama Kodu Gönder"}
                    </button>
                  </form>
                )}

                {/* Telefon Doğrulama Ekranı */}
                {showVerification && (
                  <div className="mt-4">
                    <div
                      className="alert border-0 shadow-sm text-center"
                      style={{
                        backgroundColor: "#fff3e0",
                        borderRadius: "15px",
                        color: "#ef6c00",
                      }}
                    >
                      <i className="fas fa-mobile-alt fa-2x mb-3 text-warning"></i>
                      <h5 className="fw-bold mb-2">Telefon Doğrulama</h5>
                      <p className="mb-0">
                        <strong>{formData.phone}</strong> numarasına gönderilen
                        6 haneli kodu girin
                      </p>
                    </div>

                    {/* Demo için kod gösterimi */}
                    {generatedCode && (
                      <div
                        className="alert alert-warning border-0 shadow-sm text-center mt-3"
                        style={{
                          backgroundColor: "#fff8e1",
                          borderRadius: "15px",
                          border: "2px dashed #ffa000",
                        }}
                      >
                        <i className="fas fa-info-circle me-2"></i>
                        <strong>DEMO MODE:</strong> Doğrulama kodu ={" "}
                        <span
                          style={{
                            backgroundColor: "#ff6f00",
                            color: "white",
                            padding: "5px 15px",
                            borderRadius: "10px",
                            fontSize: "1.2rem",
                            fontWeight: "bold",
                            letterSpacing: "3px",
                          }}
                        >
                          {generatedCode}
                        </span>
                        <br />
                        <small className="text-muted mt-2 d-block">
                          Gerçek uygulamada bu kod SMS ile gönderilir
                        </small>
                      </div>
                    )}

                    <form onSubmit={handleVerificationSubmit} className="mt-4">
                      <div className="mb-4">
                        <label className="form-label fw-bold text-warning text-center d-block">
                          <i className="fas fa-key me-2"></i>Doğrulama Kodu
                        </label>
                        <input
                          type="text"
                          className="form-control form-control-lg border-0 shadow-sm text-center"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                            fontSize: "1.5rem",
                            letterSpacing: "0.5rem",
                          }}
                          value={verificationCode}
                          onChange={handleVerificationChange}
                          placeholder="000000"
                          maxLength="6"
                          required
                        />
                      </div>

                      <button
                        type="submit"
                        className="btn btn-lg w-100 text-white fw-bold shadow-lg border-0 mb-3"
                        style={{
                          background:
                            "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                          borderRadius: "50px",
                          padding: "1rem",
                        }}
                      >
                        <i className="fas fa-check me-2"></i>
                        Doğrula ve Hesap Oluştur
                      </button>

                      <div className="text-center">
                        {countdown > 0 ? (
                          <p className="text-muted">
                            <i className="fas fa-clock me-2"></i>
                            Yeni kod gönderebilmek için{" "}
                            <strong>{countdown}</strong> saniye bekleyin
                          </p>
                        ) : (
                          <button
                            type="button"
                            onClick={resendCode}
                            className="btn btn-outline-warning fw-bold"
                            style={{ borderRadius: "25px" }}
                          >
                            <i className="fas fa-redo me-2"></i>
                            Kodu Tekrar Gönder
                          </button>
                        )}
                      </div>
                    </form>
                  </div>
                )}

                {isLogin && !showVerification && (
                  <div className="text-center mt-4">
                    <div
                      className="alert border-0 shadow-sm"
                      style={{
                        backgroundColor: "#fff3e0",
                        borderRadius: "15px",
                        color: "#ef6c00",
                      }}
                    >
                      <i className="fas fa-info-circle me-2"></i>
                      <strong>Demo:</strong> test@test.com ile giriş
                      yapabilirsiniz
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AccountPage;
