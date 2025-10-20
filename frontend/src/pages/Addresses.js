import React, { useState, useEffect } from "react";
import api from "../services/api";

export default function Addresses() {
  const [addresses, setAddresses] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
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
    loadAddresses();
  }, []);

  const loadAddresses = async () => {
    try {
      const res = await api.get("/addresses");
      setAddresses(res.data || []);
    } catch (err) {
      console.error("Adresler yüklenemedi:", err);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingId) {
        await api.put(`/addresses/${editingId}`, form);
      } else {
        await api.post("/addresses", form);
      }
      loadAddresses();
      resetForm();
    } catch (err) {
      alert("Adres kaydedilemedi: " + err.message);
    }
  };

  const handleEdit = (address) => {
    setForm(address);
    setEditingId(address.id);
    setShowForm(true);
  };

  const handleDelete = async (id) => {
    if (window.confirm("Bu adresi silmek istediğinize emin misiniz?")) {
      try {
        await api.delete(`/addresses/${id}`);
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

  return (
    <div className="container mx-auto px-4 py-8 max-w-6xl">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Adreslerim</h1>
        <button
          onClick={() => setShowForm(true)}
          className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
        >
          + Yeni Adres Ekle
        </button>
      </div>

      {/* Adres Listesi */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
        {addresses.map((addr) => (
          <div key={addr.id} className="bg-white p-4 rounded-lg shadow">
            <div className="flex justify-between items-start mb-2">
              <h3 className="font-semibold text-lg">{addr.title}</h3>
              <div className="flex gap-2">
                <button
                  onClick={() => handleEdit(addr)}
                  className="text-blue-600 hover:text-blue-800"
                >
                  Düzenle
                </button>
                <button
                  onClick={() => handleDelete(addr.id)}
                  className="text-red-600 hover:text-red-800"
                >
                  Sil
                </button>
              </div>
            </div>
            <p className="text-sm text-gray-600 mb-1">{addr.fullName}</p>
            <p className="text-sm text-gray-600 mb-1">{addr.phone}</p>
            <p className="text-sm text-gray-600 mb-1">{addr.address}</p>
            <p className="text-sm text-gray-600">
              {addr.district} / {addr.city} - {addr.postalCode}
            </p>
          </div>
        ))}
      </div>

      {/* Adres Formu Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <h2 className="text-xl font-semibold mb-4">
              {editingId ? "Adresi Düzenle" : "Yeni Adres Ekle"}
            </h2>
            <form onSubmit={handleSubmit}>
              <div className="mb-3">
                <label className="block text-sm font-medium mb-1">
                  Adres Başlığı
                </label>
                <input
                  type="text"
                  value={form.title}
                  onChange={(e) => setForm({ ...form, title: e.target.value })}
                  placeholder="Ev, İş vb."
                  className="w-full border rounded px-3 py-2"
                  required
                />
              </div>
              <div className="mb-3">
                <label className="block text-sm font-medium mb-1">
                  Ad Soyad
                </label>
                <input
                  type="text"
                  value={form.fullName}
                  onChange={(e) =>
                    setForm({ ...form, fullName: e.target.value })
                  }
                  className="w-full border rounded px-3 py-2"
                  required
                />
              </div>
              <div className="mb-3">
                <label className="block text-sm font-medium mb-1">
                  Telefon
                </label>
                <input
                  type="tel"
                  value={form.phone}
                  onChange={(e) => setForm({ ...form, phone: e.target.value })}
                  className="w-full border rounded px-3 py-2"
                  required
                />
              </div>
              <div className="grid grid-cols-2 gap-3 mb-3">
                <div>
                  <label className="block text-sm font-medium mb-1">İl</label>
                  <input
                    type="text"
                    value={form.city}
                    onChange={(e) => setForm({ ...form, city: e.target.value })}
                    className="w-full border rounded px-3 py-2"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">İlçe</label>
                  <input
                    type="text"
                    value={form.district}
                    onChange={(e) =>
                      setForm({ ...form, district: e.target.value })
                    }
                    className="w-full border rounded px-3 py-2"
                    required
                  />
                </div>
              </div>
              <div className="mb-3">
                <label className="block text-sm font-medium mb-1">Adres</label>
                <textarea
                  value={form.address}
                  onChange={(e) =>
                    setForm({ ...form, address: e.target.value })
                  }
                  className="w-full border rounded px-3 py-2"
                  rows="3"
                  required
                />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">
                  Posta Kodu
                </label>
                <input
                  type="text"
                  value={form.postalCode}
                  onChange={(e) =>
                    setForm({ ...form, postalCode: e.target.value })
                  }
                  className="w-full border rounded px-3 py-2"
                />
              </div>
              <div className="flex gap-2">
                <button
                  type="submit"
                  className="flex-1 bg-blue-600 text-white py-2 rounded hover:bg-blue-700"
                >
                  Kaydet
                </button>
                <button
                  type="button"
                  onClick={resetForm}
                  className="flex-1 bg-gray-300 text-gray-700 py-2 rounded hover:bg-gray-400"
                >
                  İptal
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
