import React, { useState, useEffect } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import addressService from "../services/addressService";

export default function Addresses() {
  const { user, loading: authLoading } = useAuth();
  const [addresses, setAddresses] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [loadError, setLoadError] = useState("");
  const [form, setForm] = useState({
    title: "",
    fullName: "",
    phone: "",
    city: "",
    district: "",
    address: "",
    postalCode: "",
  });

  useEffect(() => {
    if (user) {
      loadAddresses();
    }
  }, [user]);

  const loadAddresses = async () => {
    setLoadError("");
    try {
      const items = await addressService.getAll();
      setAddresses(items);
    } catch (err) {
      console.error("Adresler yüklenemedi:", err);
      if (err.status === 401) {
        setLoadError("Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.");
      } else {
        setLoadError(err.message || "Adresler yüklenemedi");
      }
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingId) {
        await addressService.update(editingId, form);
      } else {
        await addressService.create(form);
      }
      loadAddresses();
      resetForm();
    } catch (err) {
      alert("Adres kaydedilemedi: " + err.message);
    }
  };

  const handleEdit = (address) => {
    setForm({
      title: address.title || "",
      fullName: address.fullName || "",
      phone: address.phone || "",
      city: address.city || "",
      district: address.district || "",
      address: address.address || address.street || "",
      postalCode: address.postalCode || "",
    });
    setEditingId(address.id);
    setShowForm(true);
  };

  const handleDelete = async (id) => {
    if (window.confirm("Bu adresi silmek istediğinize emin misiniz?")) {
      try {
        await addressService.remove(id);
        loadAddresses();
      } catch (err) {
        alert("Adres silinemedi: " + err.message);
      }
    }
  };

  const resetForm = () => {
    setForm({
      title: "",
      fullName: "",
      phone: "",
      city: "",
      district: "",
      address: "",
      postalCode: "",
    });
    setEditingId(null);
    setShowForm(false);
  };

  if (authLoading) {
    return (
      <div className="container my-5 text-center">
        <div className="spinner-border text-warning" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/account" replace />;
  }

  return (
    <div className="container my-4 my-md-5 addresses-page">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 style={{ color: "#FF8C00", fontWeight: "bold" }}>📍 Adreslerim</h2>
        <button
          onClick={() => setShowForm(true)}
          className="btn"
          style={{
            background: "linear-gradient(135deg, #FF8C00 0%, #FFA500 100%)",
            color: "white",
            borderRadius: "25px",
            padding: "10px 25px",
            border: "none",
            fontWeight: "600",
            boxShadow: "0 4px 15px rgba(255, 140, 0, 0.3)",
          }}
        >
          + Yeni Adres Ekle
        </button>
      </div>

      {loadError && (
        <div className="alert alert-warning">{loadError}</div>
      )}

      {/* Adres Listesi */}
      <div className="row g-4 mb-4">
        {addresses.map((addr) => (
          <div key={addr.id} className="col-md-6 col-lg-4">
            <div
              className="card h-100"
              style={{
                borderRadius: "20px",
                border: "2px solid #FFE5CC",
                boxShadow: "0 4px 20px rgba(255, 140, 0, 0.1)",
                transition: "all 0.3s ease",
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = "translateY(-5px)";
                e.currentTarget.style.boxShadow =
                  "0 8px 30px rgba(255, 140, 0, 0.2)";
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = "translateY(0)";
                e.currentTarget.style.boxShadow =
                  "0 4px 20px rgba(255, 140, 0, 0.1)";
              }}
            >
              <div className="card-body">
                <div className="d-flex justify-content-between align-items-start mb-3">
                  <h5
                    className="card-title"
                    style={{ color: "#FF8C00", fontWeight: "bold" }}
                  >
                    {addr.title}
                  </h5>
                  <div className="d-flex gap-2">
                    <button
                      onClick={() => handleEdit(addr)}
                      className="btn btn-sm btn-outline-primary"
                      style={{ borderRadius: "15px" }}
                    >
                      ✏️ Düzenle
                    </button>
                    <button
                      onClick={() => handleDelete(addr.id)}
                      className="btn btn-sm btn-outline-danger"
                      style={{ borderRadius: "15px" }}
                    >
                      🗑️ Sil
                    </button>
                  </div>
                </div>
                <p className="mb-2">
                  <strong>👤 {addr.fullName}</strong>
                </p>
                <p className="mb-2">
                  <strong>📞</strong> {addr.phone}
                </p>
                <p className="mb-2">
                  <strong>📍</strong> {addr.address}
                </p>
                <p className="text-muted mb-0">
                  {addr.district} / {addr.city}{" "}
                  {addr.postalCode && `- ${addr.postalCode}`}
                </p>
              </div>
            </div>
          </div>
        ))}
      </div>

      {addresses.length === 0 && !loadError && (
        <div className="text-center text-muted py-4">
          Henüz kayıtlı adresiniz yok.
        </div>
      )}

      {/* Adres Formu Modal */}
      {showForm && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
          onClick={resetForm}
        >
          <div
            className="modal-dialog modal-dialog-centered"
            onClick={(e) => e.stopPropagation()}
          >
            <div
              className="modal-content"
              style={{
                borderRadius: "25px",
                border: "3px solid #FFE5CC",
                boxShadow: "0 10px 40px rgba(255, 140, 0, 0.3)",
              }}
            >
              <div
                className="modal-header"
                style={{
                  background:
                    "linear-gradient(135deg, #FF8C00 0%, #FFA500 100%)",
                  color: "white",
                  borderRadius: "22px 22px 0 0",
                }}
              >
                <h5 className="modal-title">
                  {editingId ? "✏️ Adresi Düzenle" : "➕ Yeni Adres Ekle"}
                </h5>
                <button
                  type="button"
                  className="btn-close btn-close-white"
                  onClick={resetForm}
                ></button>
              </div>
              <div className="modal-body p-4">
                <form onSubmit={handleSubmit}>
                  <div className="mb-3">
                    <label className="form-label fw-bold">Adres Başlığı</label>
                    <input
                      type="text"
                      value={form.title}
                      onChange={(e) =>
                        setForm({ ...form, title: e.target.value })
                      }
                      placeholder="Ev, İş vb."
                      className="form-control"
                      style={{
                        borderRadius: "15px",
                        border: "2px solid #FFE5CC",
                      }}
                      required
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label fw-bold">Ad Soyad</label>
                    <input
                      type="text"
                      value={form.fullName}
                      onChange={(e) =>
                        setForm({ ...form, fullName: e.target.value })
                      }
                      className="form-control"
                      style={{
                        borderRadius: "15px",
                        border: "2px solid #FFE5CC",
                      }}
                      required
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label fw-bold">Telefon</label>
                    <input
                      type="tel"
                      value={form.phone}
                      onChange={(e) =>
                        setForm({ ...form, phone: e.target.value })
                      }
                      className="form-control"
                      style={{
                        borderRadius: "15px",
                        border: "2px solid #FFE5CC",
                      }}
                      required
                    />
                  </div>
                  <div className="row mb-3">
                    <div className="col-6">
                      <label className="form-label fw-bold">İl</label>
                      <input
                        type="text"
                        value={form.city}
                        onChange={(e) =>
                          setForm({ ...form, city: e.target.value })
                        }
                        className="form-control"
                        style={{
                          borderRadius: "15px",
                          border: "2px solid #FFE5CC",
                        }}
                        required
                      />
                    </div>
                    <div className="col-6">
                      <label className="form-label fw-bold">İlçe</label>
                      <input
                        type="text"
                        value={form.district}
                        onChange={(e) =>
                          setForm({ ...form, district: e.target.value })
                        }
                        className="form-control"
                        style={{
                          borderRadius: "15px",
                          border: "2px solid #FFE5CC",
                        }}
                        required
                      />
                    </div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label fw-bold">Adres</label>
                    <textarea
                      value={form.address}
                      onChange={(e) =>
                        setForm({ ...form, address: e.target.value })
                      }
                      className="form-control"
                      style={{
                        borderRadius: "15px",
                        border: "2px solid #FFE5CC",
                      }}
                      rows="3"
                      required
                    />
                  </div>
                  <div className="mb-4">
                    <label className="form-label fw-bold">Posta Kodu</label>
                    <input
                      type="text"
                      value={form.postalCode}
                      onChange={(e) =>
                        setForm({ ...form, postalCode: e.target.value })
                      }
                      className="form-control"
                      style={{
                        borderRadius: "15px",
                        border: "2px solid #FFE5CC",
                      }}
                    />
                  </div>
                  <div className="d-flex gap-2">
                    <button
                      type="submit"
                      className="btn flex-fill"
                      style={{
                        background:
                          "linear-gradient(135deg, #FF8C00 0%, #FFA500 100%)",
                        color: "white",
                        borderRadius: "20px",
                        fontWeight: "600",
                        padding: "10px",
                      }}
                    >
                      💾 Kaydet
                    </button>
                    <button
                      type="button"
                      onClick={resetForm}
                      className="btn btn-secondary flex-fill"
                      style={{
                        borderRadius: "20px",
                        fontWeight: "600",
                        padding: "10px",
                      }}
                    >
                      ❌ İptal
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
