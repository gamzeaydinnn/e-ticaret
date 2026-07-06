import React, { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import { useLoginModal } from "../../contexts/LoginModalContext";
import { useFavorites } from "../../contexts/FavoriteContext";
import "./AccountHub.css";

const HubItem = ({
  icon,
  iconTone = "orange",
  title,
  subtitle,
  badge,
  onClick,
  primary = false,
  danger = false,
}) => (
  <button
    type="button"
    className={`account-hub-item${primary ? " account-hub-item--primary" : ""}${
      danger ? " account-hub-item--danger" : ""
    }`}
    onClick={onClick}
  >
    <div className={`account-hub-icon account-hub-icon--${iconTone}`}>
      <i className={`fas ${icon}`} />
    </div>
    <div className="account-hub-item-body">
      <span className="account-hub-item-title">{title}</span>
      {subtitle && (
        <span className="account-hub-item-subtitle">{subtitle}</span>
      )}
    </div>
    {badge > 0 && (
      <span className="account-hub-item-badge" aria-label={`${badge} ürün`}>
        {badge > 99 ? "99+" : badge}
      </span>
    )}
    <i className="fas fa-chevron-right account-hub-item-arrow" />
  </button>
);

const AccountHub = () => {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { user, logout } = useAuth();
  const { openLoginModal } = useLoginModal();
  const { favorites } = useFavorites();
  const [logoutLoading, setLogoutLoading] = useState(false);

  useEffect(() => {
    if (user) return;

    const mode = searchParams.get("mode");
    if (mode === "login" || mode === "register") {
      openLoginModal(mode);
      setSearchParams({}, { replace: true });
    }
  }, [user, searchParams, openLoginModal, setSearchParams]);

  const favoriteCount = favorites?.length ?? 0;
  const displayName =
    user?.name ||
    [user?.firstName, user?.lastName].filter(Boolean).join(" ") ||
    "Kullanıcı";

  const handleLogout = async () => {
    setLogoutLoading(true);
    try {
      await logout();
    } finally {
      setLogoutLoading(false);
    }
  };

  if (!user) {
    return (
      <div className="account-hub-page">
        <div className="account-hub-header">
          <h1>Hesabım</h1>
          <p>Giriş yapın, kayıt olun veya misafir olarak devam edin.</p>
        </div>

        <div className="account-hub-menu">
          <p className="account-hub-section-label">Hesap</p>
          <HubItem
            icon="fa-sign-in-alt"
            iconTone="orange"
            title="Giriş Yap"
            subtitle="Google, SMS ve e-posta ile"
            onClick={() => openLoginModal("login")}
            primary
          />
          <HubItem
            icon="fa-user-plus"
            iconTone="green"
            title="Kayıt Ol"
            subtitle="Hızlı SMS doğrulamalı kayıt"
            onClick={() => openLoginModal("register")}
          />

          <p className="account-hub-section-label">Misafir</p>
          <HubItem
            icon="fa-truck"
            iconTone="blue"
            title="Sipariş Takibi"
            subtitle="Sipariş numarası ile sorgula"
            onClick={() => navigate("/siparis-takibi")}
          />
          <HubItem
            icon="fa-heart"
            iconTone="red"
            title="Favorilerim"
            subtitle="Beğendiğin ürünleri gör"
            badge={favoriteCount}
            onClick={() => navigate("/favorites")}
          />
        </div>

        <p className="account-hub-footer-note">
          Google ile giriş, SMS doğrulama ve şifremi unuttum giriş penceresinde
          yer alır.
        </p>
      </div>
    );
  }

  return (
    <div className="account-hub-page">
      <div className="account-hub-user-card">
        <div className="account-hub-user-avatar">
          <i className="fas fa-user" />
        </div>
        <div>
          <div className="account-hub-user-name">{displayName}</div>
          {user.email && (
            <div className="account-hub-user-email">{user.email}</div>
          )}
        </div>
      </div>

      <div className="account-hub-menu">
        <p className="account-hub-section-label">Alışveriş</p>
        <HubItem
          icon="fa-heart"
          iconTone="red"
          title="Favorilerim"
          subtitle="Beğendiğin ürünler"
          badge={favoriteCount}
          onClick={() => navigate("/favorites")}
        />
        <HubItem
          icon="fa-box"
          iconTone="green"
          title="Siparişlerim"
          subtitle="Geçmiş ve aktif siparişler"
          onClick={() => navigate("/orders")}
        />
        <HubItem
          icon="fa-truck"
          iconTone="blue"
          title="Sipariş Takibi"
          subtitle="Kargo durumunu kontrol et"
          onClick={() => navigate("/siparis-takibi")}
        />
        <HubItem
          icon="fa-map-marker-alt"
          iconTone="orange"
          title="Adreslerim"
          subtitle="Teslimat adreslerini yönet"
          onClick={() => navigate("/addresses")}
        />

        <p className="account-hub-section-label">Hesap</p>
        <HubItem
          icon="fa-user-cog"
          iconTone="purple"
          title="Profil ve Şifre"
          subtitle="Bilgilerini güncelle, şifre değiştir"
          onClick={() => navigate("/profile")}
        />
        <HubItem
          icon="fa-undo"
          iconTone="gray"
          title="İade ve Değişim"
          subtitle="İade talebi oluştur"
          onClick={() => navigate("/iade-degisim")}
        />
        <HubItem
          icon="fa-envelope"
          iconTone="blue"
          title="İletişim"
          subtitle="Bize ulaşın"
          onClick={() => navigate("/iletisim")}
        />
        <HubItem
          icon="fa-sign-out-alt"
          iconTone="red"
          title={logoutLoading ? "Çıkış yapılıyor..." : "Çıkış Yap"}
          subtitle="Hesabından güvenle çık"
          onClick={handleLogout}
          danger
        />
      </div>
    </div>
  );
};

export default AccountHub;
